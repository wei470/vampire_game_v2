using UnityEngine;

/// <summary>
/// 通用武器状态接口 — 所有角色共享
/// </summary>
public interface IGunState
{
    float Cooldown { get; }
    int ImpactDamage { get; }
    int UpgradeLevel { get; }
    float NextAllowedFireTime { get; set; }
}

/// <summary>
/// 通用武器状态 — 不包含 DOT 字段，非 DOT 角色使用
/// </summary>
public class GunState : IGunState
{
    public float cooldown;
    public int impactDamage;
    public int upgradeLevel;
    public float nextAllowedFireTime;

    public float Cooldown => cooldown;
    public int ImpactDamage => impactDamage;
    public int UpgradeLevel => upgradeLevel;
    public float NextAllowedFireTime { get => nextAllowedFireTime; set => nextAllowedFireTime = value; }
}

/// <summary>
/// DOT 武器状态 — 在通用基础上扩展 DotDps/DotDuration/EffectType/Color
/// </summary>
public class DotGunState : GunState
{
    public StatusEffectType effectType;
    public Color color;
    public float dotDps;
    public float dotDuration;

    public StatusEffectType EffectType => effectType;
    public Color GunColor => color;
    public float DotDps => dotDps;
    public float DotDuration => dotDuration;
}
