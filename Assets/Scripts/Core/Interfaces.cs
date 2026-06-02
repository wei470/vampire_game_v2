/// <summary>
/// 通用游戏接口定义 — 为常用交互提供类型约束。
/// </summary>

/// <summary>
/// 可受伤害接口 — 所有可被攻击的对象应实现此接口
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 受到伤害
    /// </summary>
    void TakeDamage(int damage);

    /// <summary>
    /// 当前 HP
    /// </summary>
    int CurrentHp { get; }

    /// <summary>
    /// 最大 HP
    /// </summary>
    int MaxHp { get; }

    /// <summary>
    /// 是否存活
    /// </summary>
    bool IsAlive { get; }
}

/// <summary>
/// 可池化接口 — 所有可被对象池管理的对象应实现此接口
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// 对象池键名
    /// </summary>
    string PoolKey { get; }

    /// <summary>
    /// 从池中取出时调用
    /// </summary>
    void OnSpawnFromPool();

    /// <summary>
    /// 归还到池中时调用
    /// </summary>
    void OnDespawnToPool();
}

/// <summary>
/// 可奖励接口 — 所有提供击杀奖励的对象应实现此接口
/// </summary>
public interface IRewardable
{
    /// <summary>
    /// 经验奖励
    /// </summary>
    int XpReward { get; }

    /// <summary>
    /// 金币奖励
    /// </summary>
    int CoinReward { get; }
}

// ═══════════════════════════════════════════════════════════════
// #39 新增接口 — 统一 DOT 效果、升级逻辑抽象
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// DOT 效果接口 — 统一所有 DOT 效果的生命周期
/// 适用于 BleedEffect、BurnStackEffect、PoisonStackEffect、FrostEffect 等
/// </summary>
public interface IDotEffect
{
    /// <summary>
    /// 效果类型
    /// </summary>
    StatusEffectType EffectType { get; }

    /// <summary>
    /// 当前是否激活（有持续中的效果）
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// 施加/刷新效果
    /// </summary>
    void Apply(float dps, float duration, bool canCrit = false, float critChance = 0f, float critMult = 2f);

    /// <summary>
    /// 清除所有效果
    /// </summary>
    void Clear();

    /// <summary>
    /// 获取当前总 DPS（用于引爆计算）
    /// </summary>
    float GetCurrentDps();
}

/// <summary>
/// 可升级接口 — 统一所有可升级系统的升级逻辑
/// 适用于技能系统、武器系统等
/// </summary>
public interface IUpgradable
{
    /// <summary>
    /// 当前等级
    /// </summary>
    int CurrentLevel { get; }

    /// <summary>
    /// 最大等级
    /// </summary>
    int MaxLevel { get; }

    /// <summary>
    /// 是否已满级
    /// </summary>
    bool IsMaxLevel { get; }

    /// <summary>
    /// 升级到下一级
    /// </summary>
    void Upgrade();
}
