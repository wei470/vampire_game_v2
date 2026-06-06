using UnityEngine;
using System.Collections.Generic;

// StatusEffectType, StatusEffect, DetonateResult → 见 StatusEffectData.cs

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
    private int _erosionDotHitCount;
    private DotParticleVFX _dotVFX;
    private EnemyDotResistance _dotResistance;
    private DotComboSystem _comboSystem;

    // DOT 频率加成（痛苦升级）
    public float DotFrequencyBonus
    {
        get => _dotFrequencyBonus;
        set { _dotFrequencyBonus = Mathf.Clamp01(value); _tickInterval = Mathf.Max(0.15f, BASE_TICK_INTERVAL * (1f - _dotFrequencyBonus)); }
    }
    private float _dotFrequencyBonus = 0f;

    // 全局属性（由 MagePassive 设置）
    public float DotDurationMultiplier { get; set; } = 1f;
    public float DotDamageMultiplier { get; set; } = 1f;
    public float RendDamageBonus { get; set; } = 0f;
    public float CorrosionArmorReduction { get; set; } = 0f;
    public float CurseDamageAmplify { get; set; } = 0f;
    public float AgonyMissingHpScale { get; set; } = 0f;
    public bool WitherActive { get; set; } = false;
    public float ErosionMaxHpReduce { get; set; } = 0f;
    public float WindErosionKnockback { get; set; } = 0f;
    public float ErosionDamagePercent { get; set; } = 0f;
    public int ErosionTriggerCount { get; set; } = 5;
    public float ContaminateRange { get; set; } = 0f;
    public float RadiateRange { get; set; } = 0f;
    public float RadiateDamagePercent { get; set; } = 0f;

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
    }

    private void OnEnable()
    {
        _activeEffects.Clear();
        _lastTickTime = Time.time;
        _erosionDotHitCount = 0;
        if (_sr != null) _originalColor = _sr.color;
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
        duration *= DotDurationMultiplier;
        StatusEffect existing = null;
        for (int i = 0; i < _activeEffects.Count; i++)
        { if (_activeEffects[i].type == type) { existing = _activeEffects[i]; break; } }

        if (existing != null)
            existing.Refresh(dps * DotDamageMultiplier, duration, stackDps: (type == StatusEffectType.Bleed || type == StatusEffectType.Immolate));
        else
        {
            if (_activeEffects.Count >= 10) _activeEffects.RemoveAt(0);
            _activeEffects.Add(new StatusEffect { type = type, damagePerSecond = dps * DotDamageMultiplier,
                remainingDuration = duration, totalDuration = duration, stackCount = 1,
                canCrit = canCrit, critChance = critChance, critMultiplier = critMult });
        }

        if (type == StatusEffectType.Corrosion && _damageable != null)
            _damageable.SetArmor(Mathf.Max(0, _damageable.Armor - Mathf.RoundToInt(CorrosionArmorReduction)));
    }

    public void ApplyEffect(StatusEffectType type, float dps, float duration)
    { ApplyEffect(type, dps, duration, false, 0f, 2f); }

    private void Update()
    {
        if (_activeEffects.Count == 0) return;

        bool isOffScreen = OffScreenCuller.IsOffScreen((Vector2)transform.position);
        float effectiveInterval = isOffScreen ? _tickInterval * 2f : _tickInterval;
        if (Time.time - _lastTickTime < effectiveInterval) return;
        _lastTickTime = Time.time;

        if (!isOffScreen) UpdateVisual();

        float totalTickDamage = 0f;
        bool hasRadiate = false; float radiateDmg = 0f;

        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = _activeEffects[i];
            effect.remainingDuration -= _tickInterval;
            if (effect.remainingDuration <= 0) { _activeEffects.RemoveAt(i); continue; }

            float tickDmg = effect.damagePerSecond * _tickInterval;
            if (_dotResistance != null) tickDmg *= _dotResistance.GetDamageMultiplier(effect.type);
            if (RendDamageBonus > 0) tickDmg *= (1f + RendDamageBonus);
            if (AgonyMissingHpScale > 0 && _damageable != null)
            { float miss = 1f - (float)_damageable.CurrentHp / _damageable.MaxHp; tickDmg *= (1f + miss * AgonyMissingHpScale); }
            if (effect.canCrit && Random.value < effect.critChance) tickDmg *= effect.critMultiplier;
            if (effect.type == StatusEffectType.Wither) WitherActive = true;
            if (effect.type == StatusEffectType.Radiate && RadiateRange > 0) { hasRadiate = true; radiateDmg = tickDmg * RadiateDamagePercent; }
            totalTickDamage += tickDmg;
        }

        _comboSystem.CheckComboEffects(_activeEffects, gameObject, _sr, _originalColor);
        UpdateDotParticles();
        totalTickDamage *= DebugConfigPanel.DebugDotDamageMultiplier;

        if (totalTickDamage > 0 && _damageable != null && _damageable.CurrentHp > 0)
        {
            if (_comboSystem.ShatterActive) totalTickDamage *= 1f + DotComboSystem.COMBO_SHATTER_BLEED_MULT;
            _damageable.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(totalTickDamage)));
            if (SFXManager.Instance != null) SFXManager.Instance.PlayDotTick();
            if (DamageMeter.Instance != null && _activeEffects.Count > 0)
                DamageMeter.Instance.RecordDotDamage(_activeEffects[0].type, Mathf.RoundToInt(totalTickDamage));
            // 侵蚀冲击
            if (ErosionDamagePercent > 0)
            {
                _erosionDotHitCount++;
                if (_erosionDotHitCount >= ErosionTriggerCount)
                {
                    _erosionDotHitCount = 0;
                    int eDmg = Mathf.Max(1, Mathf.RoundToInt(totalTickDamage * ErosionDamagePercent));
                    _damageable.TakeDamage(eDmg, new Color(0.7f, 0.8f, 0.2f));
                    DamagePopup.Create(transform.position, eDmg, new Color(0.7f, 0.9f, 0.1f), false);
                    CombatManager.CreateExplosionEffect(transform.position, 1.5f, new Color(0.6f, 0.8f, 0.2f), 0.3f);
                    if (_sr != null) _sr.color = Color.Lerp(_sr.color, new Color(0.8f, 1f, 0.2f), 0.7f);
                    DebugHelper.Log($"[DOT] EROSION! {eDmg} bonus damage");
                }
            }
        }

        // 风蚀击退
        if (WindErosionKnockback > 0)
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            { var player = GameReferences.Player; if (player != null)
              { Vector2 dir = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;
                rb.linearVelocity += dir * WindErosionKnockback; } }
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
                var hd = e.GetComponent<Damageable>(); if (hd != null && hd.CurrentHp > 0) hd.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(radiateDmg))); } }
        }

        // 侵蚀降最大生命
        if (ErosionMaxHpReduce > 0 && _damageable != null)
        { int r = Mathf.RoundToInt(_damageable.MaxHp * ErosionMaxHpReduce * _tickInterval);
          if (r > 0) _damageable.SetMaxHp(Mathf.Max(1, _damageable.MaxHp - r)); }
    }

    /// <summary>
    /// 引爆所有 DOT — Mage 专属能力
    /// </summary>
    public int Detonate(float multiplier, float critChance, float critMult, out DetonateResult result)
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
            float baseDmg = eff.damagePerSecond * eff.remainingDuration * multiplier;
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
        int finalDmg = Mathf.Max(1, Mathf.RoundToInt(totalDmg));
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
    private void UpdateVisual()
    {
        DotVisualEffectManager.UpdateVisual(_sr, _activeEffects, _originalColor);
    }

    // DOT粒子视觉（委托给 DotVisualEffectManager）
    private void UpdateDotParticles()
    {
        DotVisualEffectManager.UpdateDotParticles(gameObject, _activeEffects, ref _dotVFX);
    }

    private void OnDestroy()
    { if (_sr != null) _sr.color = _originalColor;
      if (_dotVFX != null) { Destroy(_dotVFX); _dotVFX = null; } }
}