using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 统一状态效果（DOT/Debuff）系统。
/// 替代分散的 PoisonEffect / FireZone / FrostOrb 各自的 DOT 实现。
///
/// 使用方式：StatusEffectManager 挂载到敌人身上，管理所有活跃效果。
/// </summary>

/// <summary>
/// 状态效果类型枚举（14 种 Mage 专属升级对应）
/// </summary>
public enum StatusEffectType
{
    Bleed,          // 流血 — 物理 DOT
    Poison,         // 中毒 — 自然 DOT
    Burn,           // 燃烧 — 火焰 DOT
    Frostbite,      // 霜冻 — 冰霜 DOT + 减速
    Corrosion,      // 腐蚀 — 降低护甲
    Curse,          // 诅咒 — 受到伤害增加
    Agony,          // 痛苦 — DOT 随目标已损生命增强
    Wither,         // 凋零 — 禁止回血
    Immolate,       // 献祭 — 高伤害 DOT
    Radiate,        // 辐射 — DOT 对周围造成伤害
    Contaminate,    // 污染 — 死亡时传播 DOT
    Erosion,        // 侵蚀 — 降低最大生命
    WindErosion,    // 风蚀 — DOT + 击退
    Rend            // 撕裂 — 增强所有 DOT 伤害
}

/// <summary>
/// 单个状态效果实例
/// </summary>
[System.Serializable]
public class StatusEffect
{
    public StatusEffectType type;
    public float damagePerSecond;
    public float remainingDuration;
    public float totalDuration;
    public int stackCount;
    public bool canCrit;              // 是否可暴击（Mage 被动）
    public float critChance;
    public float critMultiplier;

    /// <summary>
    /// 刷新效果（叠加或刷新持续时间）
    /// </summary>
    public void Refresh(float dps, float duration, bool stackDps = false)
    {
        if (stackDps)
        {
            stackCount++;
            damagePerSecond += dps;
        }
        else
        {
            damagePerSecond = Mathf.Max(damagePerSecond, dps);
        }
        remainingDuration = Mathf.Max(remainingDuration, duration);
        totalDuration = remainingDuration;
    }
}

/// <summary>
/// 状态效果管理器 — 挂载到敌人身上，管理所有活跃 DOT/Debuff
/// </summary>
public class StatusEffectManager : MonoBehaviour
{
    private List<StatusEffect> _activeEffects = new List<StatusEffect>();
    private Damageable _damageable;
    private SpriteRenderer _sr;
    private Color _originalColor;
    private float _lastTickTime;
    private const float TICK_INTERVAL = 0.5f;

    // 全局属性（由 MagePassive 等设置）
    public float DotDurationMultiplier { get; set; } = 1f;
    public float DotDamageMultiplier { get; set; } = 1f;
    public float RendDamageBonus { get; set; } = 0f;     // 撕裂加成
    public float CorrosionArmorReduction { get; set; } = 0f; // 腐蚀护甲减少
    public float CurseDamageAmplify { get; set; } = 0f;  // 诅咒伤害增幅
    public float AgonyMissingHpScale { get; set; } = 0f; // 痛苦已损生命系数
    public bool WitherActive { get; set; } = false;       // 凋零禁回血
    public float ErosionMaxHpReduce { get; set; } = 0f;  // 侵蚀降最大生命
    public float WindErosionKnockback { get; set; } = 0f; // 风蚀击退
    public float ContaminateRange { get; set; } = 0f;     // 污染传播范围
    public float RadiateRange { get; set; } = 0f;         // 辐射范围
    public float RadiateDamagePercent { get; set; } = 0f; // 辐射伤害比例

    /// <summary>
    /// 获取当前所有活跃效果
    /// </summary>
    public List<StatusEffect> ActiveEffects => _activeEffects;

    /// <summary>
    /// 是否有任何 DOT 效果
    /// </summary>
    public bool HasAnyDot => _activeEffects.Count > 0;

    private void Awake()
    {
        _damageable = GetComponent<Damageable>();
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _originalColor = _sr.color;
    }

    private void OnEnable()
    {
        _activeEffects.Clear();
        _lastTickTime = Time.time;
        if (_sr != null) _originalColor = _sr.color;
    }

    /// <summary>
    /// 施加一个状态效果
    /// </summary>
    public void ApplyEffect(StatusEffectType type, float dps, float duration,
        bool canCrit = false, float critChance = 0f, float critMult = 2f)
    {
        // 应用持续时间倍率
        duration *= DotDurationMultiplier;

        // 查找已有同类效果
        var existing = _activeEffects.Find(e => e.type == type);
        if (existing != null)
        {
            existing.Refresh(dps * DotDamageMultiplier, duration, stackDps: (type == StatusEffectType.Bleed || type == StatusEffectType.Immolate));
        }
        else
        {
            _activeEffects.Add(new StatusEffect
            {
                type = type,
                damagePerSecond = dps * DotDamageMultiplier,
                remainingDuration = duration,
                totalDuration = duration,
                stackCount = 1,
                canCrit = canCrit,
                critChance = critChance,
                critMultiplier = critMult
            });
        }

        // 应用腐蚀护甲减少
        if (type == StatusEffectType.Corrosion && _damageable != null)
        {
            _damageable.SetArmor(Mathf.Max(0, _damageable.Armor - Mathf.RoundToInt(CorrosionArmorReduction)));
        }
    }

    /// <summary>
    /// 施加通用效果（不带暴击参数）
    /// </summary>
    public void ApplyEffect(StatusEffectType type, float dps, float duration)
    {
        ApplyEffect(type, dps, duration, false, 0f, 2f);
    }

    private void Update()
    {
        if (_activeEffects.Count == 0) return;
        if (Time.time - _lastTickTime < TICK_INTERVAL) return;
        _lastTickTime = Time.time;

        // 颜色闪烁（取最优先的效果颜色）
        UpdateVisual();

        float totalTickDamage = 0f;
        bool hasRadiate = false;
        float radiateDmg = 0f;

        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = _activeEffects[i];
            effect.remainingDuration -= TICK_INTERVAL;

            if (effect.remainingDuration <= 0)
            {
                _activeEffects.RemoveAt(i);
                continue;
            }

            // 计算基础 DOT 伤害
            float tickDmg = effect.damagePerSecond * TICK_INTERVAL;

            // 撕裂加成
            if (RendDamageBonus > 0)
                tickDmg *= (1f + RendDamageBonus);

            // 痛苦加成（已损生命越多伤害越高）
            if (AgonyMissingHpScale > 0 && _damageable != null)
            {
                float missingPercent = 1f - (float)_damageable.CurrentHp / _damageable.MaxHp;
                tickDmg *= (1f + missingPercent * AgonyMissingHpScale);
            }

            // 暴击判定（Mage 被动）
            if (effect.canCrit && Random.value < effect.critChance)
            {
                tickDmg *= effect.critMultiplier;
            }

            // 凋零：禁止回血（在 Damageable 中检查）
            if (effect.type == StatusEffectType.Wither)
                WitherActive = true;

            // 辐射检测
            if (effect.type == StatusEffectType.Radiate && RadiateRange > 0)
            {
                hasRadiate = true;
                radiateDmg = tickDmg * RadiateDamagePercent;
            }

            totalTickDamage += tickDmg;
        }

        // 对敌人造成 DOT 伤害
        if (totalTickDamage > 0 && _damageable != null && _damageable.CurrentHp > 0)
        {
            // 凋零禁回血检查在 Damageable.Heal() 中处理
            _damageable.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(totalTickDamage)));
        }

        // 风蚀击退
        if (WindErosionKnockback > 0)
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                var player = GameReferences.Player;
                if (player != null)
                {
                    Vector2 pushDir = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;
                    rb.linearVelocity += pushDir * WindErosionKnockback;
                }
            }
        }

        // 辐射：对周围敌人造成伤害
        if (hasRadiate && RadiateRange > 0)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, RadiateRange);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                if (!hit.CompareTag("Enemy")) continue;
                var hitDmg = hit.GetComponent<Damageable>();
                if (hitDmg != null && hitDmg.CurrentHp > 0)
                {
                    hitDmg.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(radiateDmg)));
                }
            }
        }

        // 侵蚀：降低最大生命
        if (ErosionMaxHpReduce > 0 && _damageable != null)
        {
            int reduce = Mathf.RoundToInt(_damageable.MaxHp * ErosionMaxHpReduce * TICK_INTERVAL);
            if (reduce > 0)
            {
                int newMax = Mathf.Max(1, _damageable.MaxHp - reduce);
                _damageable.SetMaxHp(newMax);
            }
        }
    }

    /// <summary>
    /// 引爆所有 DOT — Mage 专属能力
    /// 返回引爆总伤害
    /// </summary>
    public int Detonate(float multiplier, float critChance, float critMult)
    {
        if (_activeEffects.Count == 0) return 0;

        float totalDamage = 0f;
        foreach (var effect in _activeEffects)
        {
            // 剩余时间 × 每秒伤害 × 引爆倍率
            float baseDmg = effect.damagePerSecond * effect.remainingDuration * multiplier;

            // 撕裂加成
            if (RendDamageBonus > 0)
                baseDmg *= (1f + RendDamageBonus);

            // 痛苦加成
            if (AgonyMissingHpScale > 0 && _damageable != null)
            {
                float missingPercent = 1f - (float)_damageable.CurrentHp / _damageable.MaxHp;
                baseDmg *= (1f + missingPercent * AgonyMissingHpScale);
            }

            // 暴击
            if (Random.value < critChance)
            {
                baseDmg *= critMult;
            }

            totalDamage += baseDmg;
        }

        // 清除所有效果
        _activeEffects.Clear();

        // 造成引爆伤害
        int finalDmg = Mathf.Max(1, Mathf.RoundToInt(totalDamage));
        if (_damageable != null && _damageable.CurrentHp > 0)
        {
            _damageable.TakeDamage(finalDmg);
        }

        return finalDmg;
    }

    /// <summary>
    /// 污染传播 — 敌人死亡时调用
    /// </summary>
    public void OnEnemyDeath_SpreadContaminate()
    {
        if (ContaminateRange <= 0 || _activeEffects.Count == 0) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, ContaminateRange);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (!hit.CompareTag("Enemy")) continue;

            var otherManager = hit.GetComponent<StatusEffectManager>();
            if (otherManager == null)
                otherManager = hit.gameObject.AddComponent<StatusEffectManager>();

            // 传播所有 DOT（持续时间减半）
            foreach (var effect in _activeEffects)
            {
                otherManager.ApplyEffect(effect.type, effect.damagePerSecond, effect.remainingDuration * 0.5f,
                    effect.canCrit, effect.critChance, effect.critMultiplier);
            }
        }
    }

    /// <summary>
    /// 获取诅咒伤害增幅
    /// </summary>
    public float GetCurseAmplify()
    {
        return CurseDamageAmplify;
    }

    /// <summary>
    /// 是否处于凋零状态（禁止回血）
    /// </summary>
    public bool IsWithered()
    {
        return WitherActive;
    }

    private void UpdateVisual()
    {
        if (_sr == null) return;

        // 根据效果类型变色
        Color effectColor = _originalColor;
        if (_activeEffects.Count > 0)
        {
            var primary = _activeEffects[0];
            switch (primary.type)
            {
                case StatusEffectType.Bleed:
                case StatusEffectType.Rend:
                    effectColor = Color.Lerp(_originalColor, new Color(0.8f, 0.1f, 0.1f), 0.6f);
                    break;
                case StatusEffectType.Poison:
                    effectColor = Color.Lerp(_originalColor, new Color(0.1f, 0.9f, 0.1f), 0.6f);
                    break;
                case StatusEffectType.Burn:
                case StatusEffectType.Immolate:
                    effectColor = Color.Lerp(_originalColor, new Color(1f, 0.4f, 0f), 0.6f);
                    break;
                case StatusEffectType.Frostbite:
                    effectColor = Color.Lerp(_originalColor, new Color(0.3f, 0.6f, 1f), 0.6f);
                    break;
                case StatusEffectType.Corrosion:
                case StatusEffectType.Erosion:
                    effectColor = Color.Lerp(_originalColor, new Color(0.5f, 0.8f, 0.2f), 0.6f);
                    break;
                case StatusEffectType.Curse:
                case StatusEffectType.Wither:
                    effectColor = Color.Lerp(_originalColor, new Color(0.4f, 0f, 0.6f), 0.6f);
                    break;
                case StatusEffectType.Agony:
                    effectColor = Color.Lerp(_originalColor, new Color(0.6f, 0f, 0.3f), 0.6f);
                    break;
                case StatusEffectType.Radiate:
                    effectColor = Color.Lerp(_originalColor, new Color(0f, 1f, 0.5f), 0.6f);
                    break;
                case StatusEffectType.Contaminate:
                    effectColor = Color.Lerp(_originalColor, new Color(0.3f, 0.5f, 0.3f), 0.6f);
                    break;
                case StatusEffectType.WindErosion:
                    effectColor = Color.Lerp(_originalColor, new Color(0.7f, 0.85f, 1f), 0.6f);
                    break;
            }
            float pulse = Mathf.Sin(Time.time * 6f) * 0.15f;
            effectColor = Color.Lerp(effectColor, _originalColor, 0.3f + pulse);
        }
        _sr.color = effectColor;
    }

    private void OnDestroy()
    {
        if (_sr != null)
            _sr.color = _originalColor;
    }
}