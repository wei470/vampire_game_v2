using UnityEngine;

/// <summary>
/// 武器系统接口 — 每个角色的武器/攻击方式实现此接口。
///
/// Mage 的 DOT 子弹、Warrior 的近战挥砍、Arsenal 的多武器切换
/// 都通过此接口统一管理。
/// </summary>
public interface IWeaponSystem
{
    /// <summary>武器类型标识</summary>
    string WeaponId { get; }

    /// <summary>向指定方向开火</summary>
    void Fire(Vector2 direction);

    /// <summary>升级时回调（调整射速/伤害/弹道等）</summary>
    void OnUpgrade(int level);

    /// <summary>获取当前 DPS 估算值（用于 HUD 显示）</summary>
    float GetDPS();

    /// <summary>是否可以开火（冷却/弹药检查）</summary>
    bool CanFire { get; }

    /// <summary>武器伤害倍率（全局加成）</summary>
    float DamageMultiplier { get; set; }
}
