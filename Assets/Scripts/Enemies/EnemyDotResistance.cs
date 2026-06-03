using UnityEngine;

/// <summary>
/// #11 敌人 DOT 抗性系统 — 每种敌人对 4 种 DOT 类型有不同抗性/弱点。
///
/// 抗性值范围：-1.0 ~ 1.0
/// - 正值 = 抗性（减少该类型 DOT 伤害）
///   0.5 = 50% 抗性（DOT 伤害减半）
///   1.0 = 免疫（完全不受该类型 DOT 影响）
/// - 负值 = 弱点（增加该类型 DOT 伤害）
///   -0.5 = 50% 弱点（DOT 伤害增加 50%）
///   0 = 无特殊抗性
///
/// 预设：
/// - TankEnemy：流血抗性 +50%（高甲减少物理DOT），霜冻弱点 -30%
/// - FastEnemy：霜冻抗性 +30%（高速目标难以冻结），流血弱点 -20%
/// - HealerEnemy：中毒抗性 +40%（自然抗性），燃烧弱点 -30%
/// - StealthEnemy：燃烧抗性 +50%（暗影灭火），中毒弱点 -20%
/// </summary>
public class EnemyDotResistance : MonoBehaviour
{
    [Header("DOT 抗性（正值=抗性，负值=弱点）")]
    [SerializeField, Range(-1f, 1f)] private float _bleedResistance = 0f;
    [SerializeField, Range(-1f, 1f)] private float _poisonResistance = 0f;
    [SerializeField, Range(-1f, 1f)] private float _burnResistance = 0f;
    [SerializeField, Range(-1f, 1f)] private float _frostResistance = 0f;

    // ── 公共属性 ──
    public float BleedResistance { get => _bleedResistance; set => _bleedResistance = Mathf.Clamp(value, -1f, 1f); }
    public float PoisonResistance { get => _poisonResistance; set => _poisonResistance = Mathf.Clamp(value, -1f, 1f); }
    public float BurnResistance { get => _burnResistance; set => _burnResistance = Mathf.Clamp(value, -1f, 1f); }
    public float FrostResistance { get => _frostResistance; set => _frostResistance = Mathf.Clamp(value, -1f, 1f); }

    /// <summary>
    /// 获取指定 DOT 类型的伤害倍率（1 - 抗性）
    /// 抗性 0.5 → 倍率 0.5（减半）
    /// 弱点 -0.5 → 倍率 1.5（加半）
    /// 免疫 1.0 → 倍率 0（无伤）
    /// </summary>
    public float GetDamageMultiplier(StatusEffectType type)
    {
        float resistance = GetResistance(type);
        return Mathf.Max(0f, 1f - resistance);
    }

    /// <summary>
    /// 获取指定 DOT 类型的抗性值
    /// </summary>
    public float GetResistance(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Bleed: return _bleedResistance;
            case StatusEffectType.Poison: return _poisonResistance;
            case StatusEffectType.Burn:
            case StatusEffectType.Immolate: return _burnResistance;
            case StatusEffectType.Frostbite: return _frostResistance;
            default: return 0f;
        }
    }

    /// <summary>
    /// 检查是否对该类型完全免疫
    /// </summary>
    public bool IsImmune(StatusEffectType type)
    {
        return GetResistance(type) >= 0.99f;
    }

    /// <summary>
    /// 检查是否对该类型有弱点（抗性<0）
    /// </summary>
    public bool HasWeakness(StatusEffectType type)
    {
        return GetResistance(type) < -0.01f;
    }

    /// <summary>
    /// 获取主要抗性类型（用于 EnemyHealthBar 显示标记）
    /// 返回抗性最高的 DOT 类型，如果没有抗性返回 null
    /// </summary>
    public StatusEffectType? GetPrimaryResistance()
    {
        float max = 0f;
        StatusEffectType? result = null;

        if (_bleedResistance > max) { max = _bleedResistance; result = StatusEffectType.Bleed; }
        if (_poisonResistance > max) { max = _poisonResistance; result = StatusEffectType.Poison; }
        if (_burnResistance > max) { max = _burnResistance; result = StatusEffectType.Burn; }
        if (_frostResistance > max) { max = _frostResistance; result = StatusEffectType.Frostbite; }

        return result;
    }

    /// <summary>
    /// 获取主要弱点类型（用于 EnemyHealthBar 显示标记）
    /// 返回弱点最深的 DOT 类型，如果没有弱点返回 null
    /// </summary>
    public StatusEffectType? GetPrimaryWeakness()
    {
        float min = 0f;
        StatusEffectType? result = null;

        if (_bleedResistance < min) { min = _bleedResistance; result = StatusEffectType.Bleed; }
        if (_poisonResistance < min) { min = _poisonResistance; result = StatusEffectType.Poison; }
        if (_burnResistance < min) { min = _burnResistance; result = StatusEffectType.Burn; }
        if (_frostResistance < min) { min = _frostResistance; result = StatusEffectType.Frostbite; }

        return result;
    }

    // ═══ 预设工厂方法 ═══

    /// <summary>
    /// 预设：TankEnemy — 流血抗性+50%（高甲减物理DOT），霜冻弱点-30%（笨重易冻）
    /// </summary>
    public static void ApplyTankPreset(EnemyDotResistance res)
    {
        if (res == null) return;
        res._bleedResistance = 0.5f;
        res._poisonResistance = 0f;
        res._burnResistance = 0.1f;
        res._frostResistance = -0.3f;
    }

    /// <summary>
    /// 预设：FastEnemy — 霜冻抗性+30%（高速难冻），流血弱点-20%（薄皮易出血）
    /// </summary>
    public static void ApplyFastPreset(EnemyDotResistance res)
    {
        if (res == null) return;
        res._bleedResistance = -0.2f;
        res._poisonResistance = 0f;
        res._burnResistance = 0f;
        res._frostResistance = 0.3f;
    }

    /// <summary>
    /// 预设：HealerEnemy — 中毒抗性+40%（自然抗性），燃烧弱点-30%（怕火）
    /// </summary>
    public static void ApplyHealerPreset(EnemyDotResistance res)
    {
        if (res == null) return;
        res._bleedResistance = 0f;
        res._poisonResistance = 0.4f;
        res._burnResistance = -0.3f;
        res._frostResistance = 0f;
    }

    /// <summary>
    /// 预设：StealthEnemy — 燃烧抗性+50%（暗影灭火），中毒弱点-20%
    /// </summary>
    public static void ApplyStealthPreset(EnemyDotResistance res)
    {
        if (res == null) return;
        res._bleedResistance = 0f;
        res._poisonResistance = -0.2f;
        res._burnResistance = 0.5f;
        res._frostResistance = 0.1f;
    }

    /// <summary>
    /// 预设：BossEnemy — 全 DOT 抗性+20%（Boss 有额外抗性）
    /// </summary>
    public static void ApplyBossPreset(EnemyDotResistance res)
    {
        if (res == null) return;
        res._bleedResistance = 0.2f;
        res._poisonResistance = 0.2f;
        res._burnResistance = 0.2f;
        res._frostResistance = 0.2f;
    }
}