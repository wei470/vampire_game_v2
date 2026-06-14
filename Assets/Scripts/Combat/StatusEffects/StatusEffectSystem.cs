using UnityEngine;
using System.Collections.Generic;

#region 状态效果数据类型

public enum StatusEffectType
{
    Bleed, Poison, Burn, Frostbite, Corrosion, Curse, Agony, Wither,
    Immolate, Radiate, Contaminate, Erosion, WindErosion, Rend, Static,
    Dark, Light
}

[System.Serializable]
public class StatusEffect
{
    public StatusEffectType type;
    public float damagePerSecond;
    public float remainingDuration;
    public float totalDuration;
    public int stackCount;
    public bool canCrit;
    public float critChance;
    public float critMultiplier;

    public void Refresh(float dps, float duration, bool stackDps = false)
    {
        if (stackDps) { stackCount++; damagePerSecond += dps; }
        else { damagePerSecond = Mathf.Max(damagePerSecond, dps); }
        remainingDuration = Mathf.Max(remainingDuration, duration);
        totalDuration = remainingDuration;
    }
}

public struct DetonateResult
{
    public float totalDamage;
    public int poisonStacks, burnStacks, bleedStacks, frostStacks;
    public bool hadBurn, hadFrost, hadPoison;
}

#endregion

/// <summary>
/// 状态效果管理器 — 挂载到敌人身上，管理所有活跃 DOT/Debuff
/// 诅咒传播委托 CurseSpreadSystem，组合效果委托 DotComboSystem
/// 视觉效果委托 DotVisualEffectManager
/// </summary>
public class StatusEffectManager : MonoBehaviour
{
    private List<StatusEffect> _activeEffects = new List<StatusEffect>();
    private Damageable _damageable;
    private SpriteRenderer _sr;
    private Color _originalColor;
    private float _lastTickTime;
    private const float BASE_TICK_INTERVAL = 0.5f;
    private float _tickInterval = 0.5f;
    #pragma warning disable CS0414
    private int _erosionDotHitCount; // 侵蚀冲击计数器（保留用于未来扩展）
    private DotParticleVFX _dotVFX;
    private EnemyDotResistance _dotResistance;
    private DotComboSystem _comboSystem;
    private MeltEffect _cachedMeltEffect;
    private Rigidbody2D _cachedRb;
    private HashSet<StatusEffectType> _seenTypesCache = new HashSet<StatusEffectType>();

    // ═══ P0-2: 全局 DOT 伤害倍率版本号（懒更新机制）═══
    /// <summary>
    /// 全局版本号，由 MagePassive.SyncDotDamageMultiplierToAll() 递增。
    /// 每个 StatusEffectManager 在下次 tick 时检测版本变化并自动更新倍率。
    /// 替代之前遍历所有敌人 GetComponent 的 O(n) 开销。
    /// </summary>
    public static int GlobalDmgMultVersion { get; private set; } = 0;
    private int _localDmgMultVersion = 0;

    // 全局属性（由 MagePassive 设置）
    public float DotDurationMultiplier { get; set; } = 1f;
    public float DotDamageMultiplier { get; set; } = 1f;
    public float RendDamageBonus { get; set; } = 0f;
    public float CorrosionArmorReduction { get; set; } = 0f;
    public float CurseDamageAmplify { get; set; } = 0f;
    public float AgonyMissingHpScale { get; set; } = 0f;
    public bool WitherActive { get; set; } = false;
    public float WindErosionKnockback { get; set; } = 0f;
    public float ContaminateRange { get; set; } = 0f;
    public float RadiateRange { get; set; } = 0f;
    public float RadiateDamagePercent { get; set; } = 0f;

    // ── P0 新增强化 ──
    /// <summary>饱和：每种不同DOT伤害加成百分比</summary>
    public float DotSaturationBonus { get; set; } = 0f;
    /// <summary>吸血法术：DOT每次tick回复生命</summary>
    public float DotLifestealPerTick { get; set; } = 0f;

    // ── P2 新增强化 ──
    /// <summary>剧毒天赋：额外DOT暴击率</summary>
    public float ToxicologyCritBonus { get; set; } = 0f;
    /// <summary>腐化之触：DOT命中时降低敌人攻击力的百分比</summary>
    public float CorruptTouchDebuff { get; set; } = 0f;

    public List<StatusEffect> ActiveEffects => _activeEffects;
    public bool HasAnyDot => _activeEffects.Count > 0;

    public void ClearAllDotEffects()
    {
        _activeEffects.Clear();
        if (_sr != null && _originalColor != default) _sr.color = _originalColor;
    }

    private void Awake()
    {
        _damageable = GetComponent<Damageable>();
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _originalColor = _sr.color;
        _dotResistance = GetComponent<EnemyDotResistance>();
        _comboSystem = new DotComboSystem();
        _cachedMeltEffect = GetComponent<MeltEffect>();
        _cachedRb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        _activeEffects.Clear();
        _lastTickTime = Time.time;
        _erosionDotHitCount = 0;
        if (_sr != null) _originalColor = _sr.color;
        if (_cachedMeltEffect == null) _cachedMeltEffect = GetComponent<MeltEffect>();
        if (_cachedRb == null) _cachedRb = GetComponent<Rigidbody2D>();
        var entity = GetComponent<BaseEntity>();
        if (entity != null) entity.OnDeath += OnDeathHandler;
    }

    private void OnDisable()
    {
        var entity = GetComponent<BaseEntity>();
        if (entity != null) entity.OnDeath -= OnDeathHandler;
    }

    private void OnDeathHandler(Vector3 deathPos) { CurseSpreadSystem.SpreadContaminate(this); }

    public void ApplyEffect(StatusEffectType type, float dps, float duration,
        bool canCrit = false, float critChance = 0f, float critMult = 2f)
    {
        // 播放DOT子弹命中音效
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayDotElement(DotElementType.Bleed);
        }
        else
        {
            DebugHelper.LogWarning("[StatusEffectSystem] SFXManager.Instance is null in ApplyEffect!");
        }

        duration *= DotDurationMultiplier;
        StatusEffect existing = null;
        for (int i = 0; i < _activeEffects.Count; i++)
        { if (_activeEffects[i].type == type) { existing = _activeEffects[i]; break; } }

        if (existing != null)
        {
            existing.Refresh(dps * DotDamageMultiplier, duration, stackDps: (type == StatusEffectType.Bleed || type == StatusEffectType.Immolate));
        }
        else
        {
            if (_activeEffects.Count >= 10) _activeEffects.RemoveAt(0);
            _activeEffects.Add(new StatusEffect { type = type, damagePerSecond = dps * DotDamageMultiplier,
                remainingDuration = duration, totalDuration = duration, stackCount = 1,
                canCrit = canCrit, critChance = critChance, critMultiplier = critMult });
        }

        if (CorrosionArmorReduction > 0 && _damageable != null)
        {
            int currentArmor = _damageable.Armor;
            int reducedArmor = Mathf.FloorToInt(currentArmor * (1f - CorrosionArmorReduction));
            _damageable.SetArmor(Mathf.Max(0, reducedArmor));
        }
    }

    public void ApplyEffect(StatusEffectType type, float dps, float duration)
    { ApplyEffect(type, dps, duration, false, 0f, 2f); }

    private void Update()
    {
        // P0-2: 懒更新 DOT 伤害倍率（仅在版本号变化时从 IDotCharacterPassive 获取）
        if (_localDmgMultVersion != GlobalDmgMultVersion)
        {
            _localDmgMultVersion = GlobalDmgMultVersion;
            var dotPassive = GameReferences.DotCharacterPassive;
            if (dotPassive != null) DotDamageMultiplier = dotPassive.GetDotDamageMultiplier();
        }

        if (_activeEffects.Count == 0) return;

        bool isOffScreen = OffScreenCuller.IsOffScreen((Vector2)transform.position);
        float effectiveInterval = isOffScreen ? _tickInterval * 2f : _tickInterval;
        if (Time.time - _lastTickTime < effectiveInterval) return;
        _lastTickTime = Time.time;

        UpdateEffects(isOffScreen);
        UpdateVisuals(isOffScreen);
        UpdateParticles();
    }

    private void UpdateEffects(bool isOffScreen)
    {
        float totalTickDamage = 0f;
        float totalTickDamageForErosion = 0f; // 侵蚀基准：上一次dot总伤
        bool hasRadiate = false; float radiateDmg = 0f;

        // 计算饱和加成：统计不同DOT种类数
        int distinctDotCount = 0;
        if (DotSaturationBonus > 0)
        {
            _seenTypesCache.Clear();
            for (int k = 0; k < _activeEffects.Count; k++)
                if (_activeEffects[k].type != StatusEffectType.Radiate && _activeEffects[k].type != StatusEffectType.Wither)
                    _seenTypesCache.Add(_activeEffects[k].type);
            distinctDotCount = _seenTypesCache.Count;
        }

        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = _activeEffects[i];
            effect.remainingDuration -= _tickInterval;
            if (effect.remainingDuration <= 0) { _activeEffects.RemoveAt(i); continue; }

            float tickDmg = effect.damagePerSecond * _tickInterval;
            // 饱和加成
            if (DotSaturationBonus > 0 && distinctDotCount > 0)
                tickDmg *= (1f + distinctDotCount * DotSaturationBonus);
            // 应用DOT组合倍率
            if (effect.type == StatusEffectType.Bleed) tickDmg *= _comboSystem.GetBleedDamageMult();
            if (effect.type == StatusEffectType.Poison) tickDmg *= _comboSystem.GetPoisonDamageMult();
            if (effect.type == StatusEffectType.Static) tickDmg *= _comboSystem.SuperconductMult;
            if (_dotResistance != null) tickDmg *= _dotResistance.GetDamageMultiplier(effect.type);
            if (RendDamageBonus > 0) tickDmg *= (1f + RendDamageBonus);
            if (AgonyMissingHpScale > 0 && _damageable != null)
            { float miss = 1f - (float)_damageable.CurrentHp / _damageable.MaxHp; tickDmg *= (1f + miss * AgonyMissingHpScale); }
            // 剧毒天赋：额外暴击率 + 凋零暴击
            bool tickCrit = false;
            if (effect.canCrit && Random.value < (effect.critChance + ToxicologyCritBonus))
            {
                tickDmg *= effect.critMultiplier;
                tickCrit = true;
            }
            if (effect.type == StatusEffectType.Wither) WitherActive = true;
            if (effect.type == StatusEffectType.Radiate && RadiateRange > 0) { hasRadiate = true; radiateDmg = tickDmg * RadiateDamagePercent; }
            totalTickDamage += tickDmg;
            // 凋零暴击时显示大字体
            if (tickCrit && !isOffScreen)
                DamagePopup.Create(transform.position, tickDmg, DamagePopup.ColorCrit, true);
        }

        _comboSystem.CheckComboEffects(_activeEffects, gameObject, _sr, _originalColor, transform.position);

        // 融化反应：灼烧期间 DOT 伤害翻倍
        if (_cachedMeltEffect != null && _cachedMeltEffect.IsActive)
            totalTickDamage *= _cachedMeltEffect.DamageMultiplier;

        totalTickDamage *= DebugConfigPanel.DebugDotDamageMultiplier;

        totalTickDamageForErosion = totalTickDamage;

        if (totalTickDamage > 0 && _damageable != null && _damageable.CurrentHp > 0)
        {
            if (_comboSystem.ShatterActive) totalTickDamage *= 1f + DotComboSystem.COMBO_SHATTER_BLEED_MULT;
            float finalDmg = Mathf.Max(0.01f, totalTickDamage);
            _damageable.TakeDamage(finalDmg);
            // DOT伤害弹字（每tick显示）
            if (!isOffScreen)
            {
                StatusEffectType dotType = _activeEffects.Count > 0 ? _activeEffects[_activeEffects.Count - 1].type : StatusEffectType.Bleed;
                Color dotColor = GetDotColor(dotType);
                DamagePopup.Create(transform.position, finalDmg, dotColor, false);
            }
            if (_activeEffects.Count > 0) PlayElementSound(_activeEffects[_activeEffects.Count - 1].type);
            // 吸血法术：DOT每次tick回复生命
            if (DotLifestealPerTick > 0)
            {
                var player = GameReferences.Player;
                if (player != null)
                {
                    var playerDmg = player.GetComponent<Damageable>();
                    if (playerDmg != null && playerDmg.CurrentHp < playerDmg.MaxHp)
                        playerDmg.Heal(Mathf.RoundToInt(DotLifestealPerTick * _activeEffects.Count));
                }
            }
            if (DamageMeter.Instance != null && _activeEffects.Count > 0)
                DamageMeter.Instance.RecordDotDamage(_activeEffects[0].type, totalTickDamage);
        }

        // 风蚀击退
        if (WindErosionKnockback > 0)
        {
            if (_cachedRb != null)
            { var player = GameReferences.Player; if (player != null)
              { Vector2 dir = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;
                _cachedRb.linearVelocity += dir * WindErosionKnockback; } }
            TrySpawnVortex();
        }

        // 辐射
        if (hasRadiate && RadiateRange > 0)
        {
            float rSqr = RadiateRange * RadiateRange;
            var spawnMgr = GameReferences.SpawnManager;
            IReadOnlyList<GameObject> enemies = spawnMgr?.ActiveEnemies;
            if (enemies != null)
            { for (int j = 0; j < enemies.Count; j++)
              { var e = enemies[j]; if (e == null || e == gameObject || !e.activeInHierarchy) continue;
                if (((Vector2)(e.transform.position - transform.position)).sqrMagnitude > rSqr) continue;
                var hd = e.GetComponent<Damageable>(); if (hd != null && hd.CurrentHp > 0) hd.TakeDamage(Mathf.Max(0.01f, radiateDmg)); } }
        }
    }

    /// <summary>
    /// 引爆所有 DOT — Mage 专属能力
    /// </summary>
    public float Detonate(float multiplier, float critChance, float critMult, out DetonateResult result)
    {
        result = new DetonateResult();
        if (_activeEffects.Count == 0) return 0;
        int poisonS = 0, burnS = 0, bleedS = 0, frostS = 0;
        float totalDmg = 0f;
        foreach (var eff in _activeEffects)
        {
            switch (eff.type)
            { case StatusEffectType.Poison: poisonS += eff.stackCount; result.hadPoison = true; break;
              case StatusEffectType.Burn: burnS += eff.stackCount; result.hadBurn = true; break;
              case StatusEffectType.Bleed: bleedS += eff.stackCount; break;
              case StatusEffectType.Frostbite: frostS += eff.stackCount; result.hadFrost = true; break; }
            float baseDmg = eff.damagePerSecond * 5f * multiplier;
            if (eff.type == StatusEffectType.Poison && eff.stackCount > 1) baseDmg *= 1f + (eff.stackCount - 1) * 0.05f;
            if (eff.type == StatusEffectType.Bleed && eff.stackCount > 1) baseDmg *= 1f + (eff.stackCount - 1) * 0.03f;
            if (RendDamageBonus > 0) baseDmg *= (1f + RendDamageBonus);
            if (AgonyMissingHpScale > 0 && _damageable != null)
            { float m = 1f - (float)_damageable.CurrentHp / _damageable.MaxHp; baseDmg *= (1f + m * AgonyMissingHpScale); }
            if (Random.value < critChance) baseDmg *= critMult;
            totalDmg += baseDmg;
        }
        result.poisonStacks = poisonS; result.burnStacks = burnS; result.bleedStacks = bleedS; result.frostStacks = frostS;
        _activeEffects.Clear();
        float finalDmg = Mathf.Min(800f, Mathf.Max(0.01f, totalDmg));
        if (_damageable != null && _damageable.CurrentHp > 0) _damageable.TakeDamage(finalDmg);
        result.totalDamage = finalDmg; return finalDmg;
    }

    public float GetCurseAmplify() => CurseDamageAmplify;
    public bool IsWithered() => WitherActive;

    // 风蚀漩涡
    private float _lastVortexTime;
    private const float VORTEX_INTERVAL = 1.0f;
    private void TrySpawnVortex()
    { if (WindErosionKnockback <= 0 || Time.time - _lastVortexTime < VORTEX_INTERVAL) return;
      _lastVortexTime = Time.time; WindErosionVortex.Create(transform.position, 1.5f, 2f, 3f, 0.2f); }

    // 视觉更新（委托给 DotVisualEffectManager）
    /// <summary>
    /// 根据 DOT 类型获取颜色
    /// </summary>
    private static Color GetDotColor(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Bleed:    return DamagePopup.ColorBleed;
            case StatusEffectType.Poison:   return DamagePopup.ColorPoison;
            case StatusEffectType.Immolate: return DamagePopup.ColorBurn;
            case StatusEffectType.Frostbite:return DamagePopup.ColorFrost;
            case StatusEffectType.Static:   return DamagePopup.ColorLightning;
            case StatusEffectType.Dark:     return DamagePopup.ColorDark;
            case StatusEffectType.Light:    return DamagePopup.ColorLight;
            default:                        return Color.white;
        }
    }

    private void UpdateVisuals(bool isOffScreen)
    {
        if (isOffScreen) return;
        DotVisualEffectManager.UpdateVisual(_sr, _activeEffects, _originalColor);
    }

    private void UpdateParticles()
    {
        DotVisualEffectManager.UpdateDotParticles(gameObject, _activeEffects, ref _dotVFX);
    }

    /// <summary>
    /// 根据元素类型播放对应音效
    /// </summary>
    private void PlayElementSound(StatusEffectType type)
    {
        if (SFXManager.Instance == null) return;
        switch (type)
        {
            case StatusEffectType.Bleed:     SFXManager.Instance.PlayDotElement(DotElementType.Bleed); break;
            case StatusEffectType.Poison:    SFXManager.Instance.PlayDotElement(DotElementType.Poison); break;
            case StatusEffectType.Immolate:  SFXManager.Instance.PlayDotElement(DotElementType.Burn); break;
            case StatusEffectType.Frostbite: SFXManager.Instance.PlayDotElement(DotElementType.Frost); break;
            case StatusEffectType.Static:    SFXManager.Instance.PlayDotElement(DotElementType.Lightning); break;
            case StatusEffectType.Dark:      SFXManager.Instance.PlayDotElement(DotElementType.Dark); break;
            case StatusEffectType.Light:     SFXManager.Instance.PlayDotElement(DotElementType.Light); break;
            default:                         SFXManager.Instance.PlayDotElement(DotElementType.Bleed); break;
        }
    }

    private void OnDestroy()
    { if (_sr != null) _sr.color = _originalColor;
      if (_dotVFX != null) { Destroy(_dotVFX); _dotVFX = null; } }
}

#region DOT 组合系统

public class DotComboSystem
{
    public const float COMBO_SHATTER_BLEED_MULT = 0f;
    public bool ShatterActive => false;
    public float SuperconductMult => 1f;

    private static float _evolutionComboDamageMultiplier = 0f;

    public static void SetEvolutionComboMultiplier(float bonus) { _evolutionComboDamageMultiplier += bonus; }
    public static float GetEvolutionComboMult() => 1f + _evolutionComboDamageMultiplier;
    public static void ResetEvolutionComboMultiplier() { _evolutionComboDamageMultiplier = 0f; }

    public void CheckComboEffects(List<StatusEffect> activeEffects, GameObject go, SpriteRenderer sr, Color originalColor, Vector3 enemyPos)
    {
        // 组合系统已清空，不再触发任何效果
    }

    public float GetBleedDamageMult() => 1f;
    public float GetPoisonDamageMult() => 1f;
}

#endregion