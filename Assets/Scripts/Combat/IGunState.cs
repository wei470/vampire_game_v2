using UnityEngine;

/// <summary>
/// 武器状态接口 — 所有角色的武器/枪械状态共享接口。
/// 替代 MagePassive.DotGunState 嵌套结构体，解除 DotBulletFactory/DotHomingBullet 的 Mage 耦合。
/// </summary>
public interface IGunState
{
    StatusEffectType EffectType { get; }
    Color GunColor { get; }
    float Cooldown { get; }
    int ImpactDamage { get; }
    float DotDps { get; }
    float DotDuration { get; }
    int UpgradeLevel { get; }
}

/// <summary>
/// DOT 枪械状态 — 法师的 DOT 子弹武器实现。
/// 从 MagePassive.DotGunState 提取为独立结构体。
/// </summary>
public struct DotGunState : IGunState
{
    public StatusEffectType effectType;
    public Color color;
    public float cooldown;
    public int impactDamage;
    public float dotDps;
    public float dotDuration;
    public float lastFireTime;
    public int upgradeLevel;

    // IGunState 显式实现
    public StatusEffectType EffectType => effectType;
    public Color GunColor => color;
    public float Cooldown => cooldown;
    public int ImpactDamage => impactDamage;
    public float DotDps => dotDps;
    public float DotDuration => dotDuration;
    public int UpgradeLevel => upgradeLevel;
}
