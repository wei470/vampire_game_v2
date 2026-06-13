/// <summary>
/// 大招系统接口 — 每个角色的大招/终极技能实现此接口。
///
/// Mage 的引爆、Warrior 的血祭、Summoner 的亡灵大军
/// 都通过此接口统一管理冷却和触发。
/// </summary>
public interface IDetonatable
{
    /// <summary>大招名称（用于 HUD 显示）</summary>
    string AbilityName { get; }

    /// <summary>是否可以释放（冷却完毕/资源足够）</summary>
    bool CanDetonate { get; }

    /// <summary>释放大招</summary>
    void Detonate();

    /// <summary>获取冷却时间（秒）</summary>
    float GetCooldown();

    /// <summary>获取冷却剩余时间（秒）</summary>
    float GetCooldownRemaining();

    /// <summary>冷却进度（0~1，1=就绪）</summary>
    float CooldownProgress { get; }
}
