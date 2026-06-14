using UnityEngine;

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

public class DotGunState : IGunState
{
    public StatusEffectType effectType;
    public Color color;
    public float cooldown;
    public int impactDamage;
    public float dotDps;
    public float dotDuration;
    public float nextAllowedFireTime;
    public int upgradeLevel;

    public StatusEffectType EffectType => effectType;
    public Color GunColor => color;
    public float Cooldown => cooldown;
    public int ImpactDamage => impactDamage;
    public float DotDps => dotDps;
    public float DotDuration => dotDuration;
    public int UpgradeLevel => upgradeLevel;
}
