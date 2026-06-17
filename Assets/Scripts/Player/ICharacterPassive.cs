using System.Collections.Generic;

/// <summary>
/// 角色被动能力接口 — 所有角色共享的能力抽象。
///
/// 通用接口：攻速/弹数/弹体/击退/贯穿/升级
/// DOT 子接口：IDotCharacterPassive（DOT枪/DOT倍率/引爆）
///
/// MagePassive 实现 IDotCharacterPassive，新角色只需实现 ICharacterPassive。
/// </summary>
public interface ICharacterPassive
{
    // ── 角色标识 ──
    string CharacterId { get; }
    string DisplayName { get; }

    // ── 通用武器属性 ──
    float GetAttackSpeedMultiplier();
    int GetBulletCountBonus();
    float GetBulletSizeBonus();
    float GetKnockbackBonus();
    int GetPenetrateCount();

    // ── 通用可写属性（升级/进化系统需要）──
    float AttackSpeedBonus { get; set; }
    int BulletCountBonus { get; set; }

    // ── 进化系统 ──
    float DotDamageMultiplier { get; set; }
    float CritChanceBonus { get; set; }

    // ── 升级系统 ──
    bool ApplyUpgrade(string upgradeId);
}

/// <summary>
/// DOT 角色被动子接口 — 仅 Mage 等 DOT 专属角色实现。
/// 包含 DOT 枪管理、DOT 伤害倍率、引爆系统。
/// </summary>
public interface IDotCharacterPassive : ICharacterPassive
{
    // ── DOT 武器系统 ──
    List<DotGunState> DotGuns { get; }
    void UnlockDotGun(StatusEffectType type, UnityEngine.Color color,
        float cooldown, int impactDmg, float dotDps, float dotDuration);
    void ClearAllDotGuns();

    // ── DOT 伤害倍率 ──
    float GetDotDamageMultiplier();
    float GetDotCritChance();
    float GetDotCritMultiplier();
    float GetDotDurationMultiplier();

    // ── DOT 增强属性（诅咒传播）──
    int CurseSpreadTargets { get; set; }

    // ── 引爆系统 ──
    DetonateSystem GetDetonateSystem();
    float DetonateMultiplier { get; set; }
    float DetonateCooldownValue { get; set; }
    float DetonateCooldownReduction { get; set; }

    // ── 蓄力属性 ──
    float ChargeSpeedBonus { get; set; }
    float ChargeDamageBonus { get; set; }

    // ── 协同/高级属性 ──
    float DoomsdayThreshold { get; set; }
    float FrostExplosionPct { get; set; }
    bool WildWind { get; set; }

    // ── DOT 持续时间 ──
    void AddDotDurationBonus(float bonus);
    void SyncDotDamageMultiplierToAll();
}
