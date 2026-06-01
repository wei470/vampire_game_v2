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