using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Mage 升级应用器 — 策略字典模式，根据升级类别分发到对应处理方法。
/// </summary>
public static class MageUpgradeApplier
{
    private static readonly Dictionary<CharacterUpgradeOption.UpgradeCategory, System.Action<MagePassive, UpgradeEntry>> _appliers = new()
    {
        { CharacterUpgradeOption.UpgradeCategory.ArmorReduction, ApplyArmorReduction },
        { CharacterUpgradeOption.UpgradeCategory.DotFrequency, ApplyDotFrequency },
        { CharacterUpgradeOption.UpgradeCategory.DotCritBurst, ApplyDotCritBurst },
        { CharacterUpgradeOption.UpgradeCategory.DetonateMultiplier, ApplyDetonateMultiplier },
        { CharacterUpgradeOption.UpgradeCategory.DetonateAbility, ApplyNoop },
        { CharacterUpgradeOption.UpgradeCategory.DotTrigger, ApplyNoop },
        { CharacterUpgradeOption.UpgradeCategory.AttackSpeed, ApplyAttackSpeed },
        { CharacterUpgradeOption.UpgradeCategory.BulletCount, ApplyBulletCount },
        { CharacterUpgradeOption.UpgradeCategory.Ricochet, ApplyRicochet },
        { CharacterUpgradeOption.UpgradeCategory.BulletSize, ApplyBulletSize },
        { CharacterUpgradeOption.UpgradeCategory.DotOverflow, ApplyNoop },
        { CharacterUpgradeOption.UpgradeCategory.LightJudgment, ApplyLightJudgment },
        { CharacterUpgradeOption.UpgradeCategory.StaticField, ApplyStaticField },
        { CharacterUpgradeOption.UpgradeCategory.FrostExplosion, ApplyFrostExplosion },
        { CharacterUpgradeOption.UpgradeCategory.Penetrate, ApplyPenetrate },
        { CharacterUpgradeOption.UpgradeCategory.ElementalShield, ApplyElementalShield },
        { CharacterUpgradeOption.UpgradeCategory.Doomsday, ApplyDoomsday },
        { CharacterUpgradeOption.UpgradeCategory.EternalAgony, ApplyEternalAgony },

        // 一般强化
        { CharacterUpgradeOption.UpgradeCategory.MoveSpeed, ApplyMoveSpeed },
        { CharacterUpgradeOption.UpgradeCategory.ArmorBonus, ApplyArmorBonus },
        { CharacterUpgradeOption.UpgradeCategory.MaxHpBonus, ApplyMaxHpBonus },
        { CharacterUpgradeOption.UpgradeCategory.CritChanceBonus, ApplyCritChanceBonus },
        { CharacterUpgradeOption.UpgradeCategory.CritDamageBonus, ApplyCritDamageBonus },
        { CharacterUpgradeOption.UpgradeCategory.MagnetRange, ApplyMagnetRange },
        { CharacterUpgradeOption.UpgradeCategory.HpRegen, ApplyHpRegen },
        { CharacterUpgradeOption.UpgradeCategory.BulletSpeed, ApplyBulletSpeed },
        { CharacterUpgradeOption.UpgradeCategory.Knockback, ApplyKnockback },
    };

    public static bool ApplyUpgrade(MagePassive mage, string upgradeId)
    {
        var config = mage.GetUpgradeConfig();
        if (config == null) { DebugHelper.LogError("[MageUpgradeApplier] MageUpgradeConfig not set!"); return false; }

        var dotGunEntry = config.GetDotGunEntry(upgradeId);
        if (dotGunEntry.HasValue)
        {
            var dg = dotGunEntry.Value;
            mage.UnlockDotGun(dg.effectType, dg.color, dg.cooldown, dg.impactDmg, dg.dotDps, dg.dotDuration);
            CheckSynergies(mage);
            return true;
        }

        var upgradeEntry = config.GetUpgradeEntry(upgradeId);
        if (!upgradeEntry.HasValue) { DebugHelper.LogWarning($"[MageUpgradeApplier] Unknown upgradeId '{upgradeId}'"); return false; }

        var ue = upgradeEntry.Value;
        if (_appliers.TryGetValue(ue.category, out var applier))
        {
            applier(mage, ue);
            CheckSynergies(mage);
            DebugHelper.Log($"[MageUpgradeApplier] Applied {ue.upgradeName}");
            return true;
        }

        DebugHelper.LogWarning($"[MageUpgradeApplier] No handler for category {ue.category}");
        return false;
    }

    private static void CheckSynergies(MagePassive mage) { }

    // ═══ DOT 增强 ═══

    private static void ApplyArmorReduction(MagePassive mage, UpgradeEntry ue)
    {
        mage.CorrosionArmorReduction += ue.value1;
        DebugHelper.Log($"[MageUpgradeApplier] Corrosion: armor -{ue.value1:P0}");
    }

    private static void ApplyDotFrequency(MagePassive mage, UpgradeEntry ue)
    {
        mage.DotFrequencyBonus += ue.value1;
        DebugHelper.Log($"[MageUpgradeApplier] Agony DOT frequency: +{ue.value1:P0}");
    }

    private static void ApplyDotCritBurst(MagePassive mage, UpgradeEntry ue)
    {
        mage.DotCritBurstChance += ue.value1;
        DebugHelper.Log($"[MageUpgradeApplier] Wither DOT crit: +{ue.value1:P0}");
    }

    private static void ApplyDetonateMultiplier(MagePassive mage, UpgradeEntry ue)
    {
        mage.DetonateMultiplier += ue.value1;
    }

    private static void ApplyNoop(MagePassive mage, UpgradeEntry ue) { }

    // ═══ 子弹增强 ═══

    private static void ApplyAttackSpeed(MagePassive mage, UpgradeEntry ue)
    {
        mage.AttackSpeedBonus += ue.value1;
        mage.BulletSpeedBonus += ue.value2;
    }

    private static void ApplyBulletCount(MagePassive mage, UpgradeEntry ue)
    {
        mage.BulletCountBonus += (int)ue.value1;
    }

    private static void ApplyRicochet(MagePassive mage, UpgradeEntry ue)
    {
        mage.PiercingBonus += (int)ue.value1;
    }

    private static void ApplyBulletSize(MagePassive mage, UpgradeEntry ue)
    {
        mage.BulletSizeBonus += ue.value1;
    }

    private static void ApplyPenetrate(MagePassive mage, UpgradeEntry ue)
    {
        mage.PiercingBonus += (int)ue.value1;
    }

    // ═══ 协同强化 ═══

    private static void ApplyLightJudgment(MagePassive mage, UpgradeEntry ue)
    {
        mage.LightJudgmentBonus += ue.value1;
    }

    private static void ApplyStaticField(MagePassive mage, UpgradeEntry ue)
    {
        mage.StaticFieldStacks += (int)ue.value2;
    }

    private static void ApplyFrostExplosion(MagePassive mage, UpgradeEntry ue)
    {
        mage.FrostExplosionPct += ue.value1;
    }

    private static void ApplyElementalShield(MagePassive mage, UpgradeEntry ue)
    {
        float newShieldHp = mage.DotGuns.Count * ue.value1;
        if (newShieldHp > mage.ElementalShieldHp)
        {
            float hpGain = newShieldHp - mage.ElementalShieldHp;
            mage.ElementalShieldHp = newShieldHp;
            var player = GameReferences.Player;
            if (player != null)
            {
                var dmg = player.GetComponent<Damageable>();
                if (dmg != null)
                {
                    dmg.SetMaxHp(dmg.MaxHp + Mathf.RoundToInt(hpGain));
                    dmg.Heal(Mathf.RoundToInt(hpGain));
                }
            }
        }
    }

    private static void ApplyDoomsday(MagePassive mage, UpgradeEntry ue)
    {
        mage.DoomsdayThreshold += ue.value2;
    }

    private static void ApplyEternalAgony(MagePassive mage, UpgradeEntry ue)
    {
        mage.EternalAgonyActive = true;
    }

    // ═══ 一般强化 ═══

    private static void ApplyMoveSpeed(MagePassive mage, UpgradeEntry ue)
    {
        // 移速暂不实现，BaseEntity 无 MoveSpeed 属性
    }

    private static void ApplyArmorBonus(MagePassive mage, UpgradeEntry ue)
    {
        var player = GameReferences.Player;
        if (player != null)
        {
            var dmg = player.GetComponent<Damageable>();
            if (dmg != null) dmg.SetArmor(dmg.Armor + (int)ue.value1);
        }
    }

    private static void ApplyMaxHpBonus(MagePassive mage, UpgradeEntry ue)
    {
        var player = GameReferences.Player;
        if (player != null)
        {
            var dmg = player.GetComponent<Damageable>();
            if (dmg != null)
            {
                dmg.SetMaxHp(dmg.MaxHp + (int)ue.value1);
                dmg.Heal((int)ue.value1);
            }
        }
    }

    private static void ApplyCritChanceBonus(MagePassive mage, UpgradeEntry ue)
    {
        mage.CritChanceBonus += ue.value1;
    }

    private static void ApplyCritDamageBonus(MagePassive mage, UpgradeEntry ue)
    {
        // 暴击倍率通过 DotEffectConfig 调节，此处暂时 noop
    }

    private static void ApplyMagnetRange(MagePassive mage, UpgradeEntry ue)
    {
        // 拾取范围由 MagnetMultiplierSystem 管理
    }

    private static void ApplyHpRegen(MagePassive mage, UpgradeEntry ue)
    {
        var player = GameReferences.Player;
        if (player != null)
        {
            var dmg = player.GetComponent<Damageable>();
            if (dmg != null) dmg.HpRegenPerSecond += ue.value1;
        }
    }

    private static void ApplyBulletSpeed(MagePassive mage, UpgradeEntry ue)
    {
        mage.BulletSpeedBonus += ue.value1;
    }

    private static void ApplyKnockback(MagePassive mage, UpgradeEntry ue)
    {
        mage.KnockbackBonus += ue.value1;
    }

    // ═══ 进化兼容 ═══

    public static void ApplyEvolutionUpgrade(MagePassive mage, string upgradeId, float value)
    {
        if (upgradeId == "dot_damage_bonus") mage.DotDamageMultiplier += value;
        else if (upgradeId == "dot_duration_bonus") { }
        else if (upgradeId == "dot_combo_damage_bonus") { }
    }

    public static void CheckEvolution(MagePassive mage, StatusEffectType type) { }
    public static void CheckMilestones(MagePassive mage) { }
}
