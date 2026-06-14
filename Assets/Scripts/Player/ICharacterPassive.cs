using System.Collections.Generic;

/// <summary>
/// 角色被动能力接口 — 所有角色共享的能力抽象。
///
/// 替代 18 个文件中的 MagePassive 直接引用，
/// 使 GameReferences / DetonateSystem / LevelUpUI / EvolutionSystem 等
/// 系统可以服务于任意角色。
///
/// MagePassive 实现此接口并保留所有 Mage 专属属性。
/// </summary>
public interface ICharacterPassive
{
    // ── 角色标识 ──
    string CharacterId { get; }
    string DisplayName { get; }

    // ── 武器系统 ──
    List<DotGunState> DotGuns { get; }
    void UnlockDotGun(StatusEffectType type, UnityEngine.Color color,
        float cooldown, int impactDmg, float dotDps, float dotDuration);
    void ClearAllDotGuns();

    // ── 伤害倍率 ──
    float GetDotDamageMultiplier();
    float GetDotCritChance();
    float GetDotCritMultiplier();
    float GetDotDurationMultiplier();
    float GetAttackSpeedMultiplier();

    // ── 引爆系统 ──
    DetonateSystem GetDetonateSystem();

    // ── 升级系统 ──
    bool ApplyUpgrade(string upgradeId);
}
