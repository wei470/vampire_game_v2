using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Mage 升级应用器 — 策略字典模式，根据升级类别分发到对应处理方法。
/// </summary>
public static class MageUpgradeApplier
{
    private static Damageable _cachedPlayerDmg;
    private static Damageable PlayerDmg
    {
        get
        {
            if (_cachedPlayerDmg == null)
            {
                var player = GameReferences.Player;
                if (player != null) _cachedPlayerDmg = player.GetComponent<Damageable>();
            }
            return _cachedPlayerDmg;
        }
    }

    private static readonly Dictionary<CharacterUpgradeOption.UpgradeCategory, System.Action<MagePassive, UpgradeEntry>> _appliers = new()
    {
        { CharacterUpgradeOption.UpgradeCategory.ArmorReduction, ApplyArmorReduction },
        { CharacterUpgradeOption.UpgradeCategory.ArmorPenetration, ApplyArmorPenetration },
        { CharacterUpgradeOption.UpgradeCategory.DetonateMultiplier, ApplyDetonateMultiplier },
        { CharacterUpgradeOption.UpgradeCategory.DetonateAbility, ApplyDetonateAbility },
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
        { CharacterUpgradeOption.UpgradeCategory.MoveSpeed, ApplyMoveSpeed },

        // 中毒专属
        { CharacterUpgradeOption.UpgradeCategory.PoisonDuration, ApplyPoisonDuration },
        { CharacterUpgradeOption.UpgradeCategory.PoisonDps, ApplyPoisonDps },
        { CharacterUpgradeOption.UpgradeCategory.PoisonPool, ApplyPoisonPool },
        { CharacterUpgradeOption.UpgradeCategory.PoisonTick, ApplyPoisonTick },
        { CharacterUpgradeOption.UpgradeCategory.PoisonCrit, ApplyPoisonCrit },
        { CharacterUpgradeOption.UpgradeCategory.PoisonSepsis, ApplyPoisonSepsis },
        { CharacterUpgradeOption.UpgradeCategory.PoisonPlague, ApplyPoisonPlague },
        { CharacterUpgradeOption.UpgradeCategory.PoisonLethal, ApplyPoisonLethal },

        // 燃烧专属
        { CharacterUpgradeOption.UpgradeCategory.BurnDuration, ApplyBurnDuration },
        { CharacterUpgradeOption.UpgradeCategory.BurnRadius, ApplyBurnRadius },
        { CharacterUpgradeOption.UpgradeCategory.BurnTick, ApplyBurnTick },
        { CharacterUpgradeOption.UpgradeCategory.BurnSlow, ApplyBurnSlow },
        { CharacterUpgradeOption.UpgradeCategory.BurnCrit, ApplyBurnCrit },
        { CharacterUpgradeOption.UpgradeCategory.BurnMelt, ApplyBurnMelt },
        { CharacterUpgradeOption.UpgradeCategory.BurnStorm, ApplyBurnStorm },
        { CharacterUpgradeOption.UpgradeCategory.BurnBurst, ApplyBurnBurst },

        // 霜冻专属
        { CharacterUpgradeOption.UpgradeCategory.FrostDuration, ApplyFrostDuration },
        { CharacterUpgradeOption.UpgradeCategory.FrostSlow, ApplyFrostSlow },
        { CharacterUpgradeOption.UpgradeCategory.FrostTick, ApplyFrostTick },
        { CharacterUpgradeOption.UpgradeCategory.FrostRange, ApplyFrostRange },
        { CharacterUpgradeOption.UpgradeCategory.FrostCrit, ApplyFrostCrit },
        { CharacterUpgradeOption.UpgradeCategory.FrostFreeze, ApplyFrostFreeze },
        { CharacterUpgradeOption.UpgradeCategory.FrostBlizzard, ApplyFrostBlizzard },
        { CharacterUpgradeOption.UpgradeCategory.FrostAbsolute, ApplyFrostAbsolute },

        // 雷电专属
        { CharacterUpgradeOption.UpgradeCategory.StaticDamage, ApplyStaticDamage },
        { CharacterUpgradeOption.UpgradeCategory.StaticChain, ApplyStaticChain },
        { CharacterUpgradeOption.UpgradeCategory.StaticTick, ApplyStaticTick },
        { CharacterUpgradeOption.UpgradeCategory.StaticRange, ApplyStaticRange },
        { CharacterUpgradeOption.UpgradeCategory.StaticCrit, ApplyStaticCrit },
        { CharacterUpgradeOption.UpgradeCategory.StaticOverload, ApplyStaticOverload },
        { CharacterUpgradeOption.UpgradeCategory.StormMulti, ApplyStormMulti },
        { CharacterUpgradeOption.UpgradeCategory.StormChain, ApplyStormChain },

        // 风专属
        { CharacterUpgradeOption.UpgradeCategory.WindDamage, ApplyWindDamage },
        { CharacterUpgradeOption.UpgradeCategory.WindSpeed, ApplyWindSpeed },
        { CharacterUpgradeOption.UpgradeCategory.WindTick, ApplyWindTick },
        { CharacterUpgradeOption.UpgradeCategory.WindPierce, ApplyWindPierce },
        { CharacterUpgradeOption.UpgradeCategory.WindCrit, ApplyWindCrit },
        { CharacterUpgradeOption.UpgradeCategory.WindStormEye, ApplyWindStormEye },
        { CharacterUpgradeOption.UpgradeCategory.WindHurricane, ApplyWindHurricane },
        { CharacterUpgradeOption.UpgradeCategory.WindLord, ApplyWindLord },
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
    }

    private static void ApplyArmorPenetration(MagePassive mage, UpgradeEntry ue)
    {
        mage.ErosionArmorPenetration += (int)ue.value1;
    }

    private static void ApplyDetonateMultiplier(MagePassive mage, UpgradeEntry ue)
    {
        mage.DetonateMultiplier *= (1f + ue.value1);
    }

    private static void ApplyNoop(MagePassive mage, UpgradeEntry ue) { }

    // ═══ 子弹增强 ═══

    private static void ApplyAttackSpeed(MagePassive mage, UpgradeEntry ue)
    {
        mage.AttackSpeedBonus += ue.value1;
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

    private static void ApplyDetonateAbility(MagePassive mage, UpgradeEntry ue)
    {
        mage.DetonateCooldownReduction += ue.value1;
        float mult = Mathf.Max(0.1f, 1f - mage.DetonateCooldownReduction);
        mage.DetonateCooldownValue = DotEffectConfig.GetDefault().DetonateCooldown * mult;
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
                var dmg = PlayerDmg;
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

    // ═══ 一般强化 ═══

    private static void ApplyMoveSpeed(MagePassive mage, UpgradeEntry ue)
    {
        var player = GameReferences.Player;
        if (player != null)
        {
            player.MoveSpeed *= (1f + ue.value1);
            DebugHelper.Log($"[MageUpgradeApplier] MoveSpeed +{ue.value1 * 100:F0}% (total: {player.MoveSpeed:F1})");
        }
    }

    // ═══ 中毒专属 ═══

    private static void ApplyPoisonDuration(MagePassive mage, UpgradeEntry ue) { mage.PoisonDurationBonus += ue.value1; DebugHelper.Log($"[Upgrade] PoisonDuration +{ue.value1} (total: {mage.PoisonDurationBonus})"); }
    private static void ApplyPoisonDps(MagePassive mage, UpgradeEntry ue) { mage.PoisonDpsBonus += ue.value1; DebugHelper.Log($"[Upgrade] PoisonDps +{ue.value1} (total: {mage.PoisonDpsBonus})"); }
    private static void ApplyPoisonPool(MagePassive mage, UpgradeEntry ue) { mage.PoisonPoolBonus += ue.value1; DebugHelper.Log($"[Upgrade] PoisonPool +{ue.value1} (total: {mage.PoisonPoolBonus})"); }
    private static void ApplyPoisonTick(MagePassive mage, UpgradeEntry ue) { mage.PoisonTickReduction += ue.value1; DebugHelper.Log($"[Upgrade] PoisonTick +{ue.value1} (total: {mage.PoisonTickReduction})"); }
    private static void ApplyPoisonCrit(MagePassive mage, UpgradeEntry ue) { mage.PoisonCritBonus += ue.value1; DebugHelper.Log($"[Upgrade] PoisonCrit +{ue.value1} (total: {mage.PoisonCritBonus})"); }
    private static void ApplyPoisonSepsis(MagePassive mage, UpgradeEntry ue) { mage.PoisonSepsis = true; DebugHelper.Log("[Upgrade] PoisonSepsis activated"); }
    private static void ApplyPoisonPlague(MagePassive mage, UpgradeEntry ue) { mage.PoisonPlague = true; DebugHelper.Log("[Upgrade] PoisonPlague activated"); }
    private static void ApplyPoisonLethal(MagePassive mage, UpgradeEntry ue) { mage.PoisonLethal = true; DebugHelper.Log("[Upgrade] PoisonLethal activated"); }

    // ═══ 燃烧专属 ═══

    private static void ApplyBurnDuration(MagePassive mage, UpgradeEntry ue) { mage.BurnDurationBonus += ue.value1; DebugHelper.Log($"[Upgrade] BurnDuration +{ue.value1} (total: {mage.BurnDurationBonus})"); }
    private static void ApplyBurnRadius(MagePassive mage, UpgradeEntry ue) { mage.BurnRadiusBonus += ue.value1; DebugHelper.Log($"[Upgrade] BurnRadius +{ue.value1} (total: {mage.BurnRadiusBonus})"); }
    private static void ApplyBurnTick(MagePassive mage, UpgradeEntry ue) { mage.BurnTickReduction += ue.value1; DebugHelper.Log($"[Upgrade] BurnTick +{ue.value1} (total: {mage.BurnTickReduction})"); }
    private static void ApplyBurnSlow(MagePassive mage, UpgradeEntry ue) { mage.BurnSlowBonus += ue.value1; DebugHelper.Log($"[Upgrade] BurnSlow +{ue.value1} (total: {mage.BurnSlowBonus})"); }
    private static void ApplyBurnCrit(MagePassive mage, UpgradeEntry ue) { mage.BurnCritBonus += ue.value1; DebugHelper.Log($"[Upgrade] BurnCrit +{ue.value1} (total: {mage.BurnCritBonus})"); }
    private static void ApplyBurnMelt(MagePassive mage, UpgradeEntry ue) { mage.BurnMeltMastery = true; DebugHelper.Log("[Upgrade] BurnMelt activated"); }
    private static void ApplyBurnStorm(MagePassive mage, UpgradeEntry ue) { mage.BurnStorm = true; DebugHelper.Log("[Upgrade] BurnStorm activated"); }
    private static void ApplyBurnBurst(MagePassive mage, UpgradeEntry ue) { mage.BurnBurst = true; DebugHelper.Log("[Upgrade] BurnBurst activated"); }

    // ═══ 霜冻专属 ═══

    private static void ApplyFrostDuration(MagePassive mage, UpgradeEntry ue) { mage.FrostDurationBonus += ue.value1; DebugHelper.Log($"[Upgrade] FrostDuration +{ue.value1} (total: {mage.FrostDurationBonus})"); }
    private static void ApplyFrostSlow(MagePassive mage, UpgradeEntry ue) { mage.FrostSlowBonus += ue.value1; DebugHelper.Log($"[Upgrade] FrostSlow +{ue.value1} (total: {mage.FrostSlowBonus})"); }
    private static void ApplyFrostTick(MagePassive mage, UpgradeEntry ue) { mage.FrostTickReduction += ue.value1; DebugHelper.Log($"[Upgrade] FrostTick +{ue.value1} (total: {mage.FrostTickReduction})"); }
    private static void ApplyFrostRange(MagePassive mage, UpgradeEntry ue) { mage.FrostRangeBonus += ue.value1; DebugHelper.Log($"[Upgrade] FrostRange +{ue.value1} (total: {mage.FrostRangeBonus})"); }
    private static void ApplyFrostCrit(MagePassive mage, UpgradeEntry ue) { mage.FrostCritBonus += ue.value1; DebugHelper.Log($"[Upgrade] FrostCrit +{ue.value1} (total: {mage.FrostCritBonus})"); }
    private static void ApplyFrostFreeze(MagePassive mage, UpgradeEntry ue) { mage.FrostFreeze = true; DebugHelper.Log("[Upgrade] FrostFreeze activated"); }
    private static void ApplyFrostBlizzard(MagePassive mage, UpgradeEntry ue) { mage.FrostBlizzard = true; DebugHelper.Log("[Upgrade] FrostBlizzard activated"); }
    private static void ApplyFrostAbsolute(MagePassive mage, UpgradeEntry ue) { mage.FrostAbsolute = true; DebugHelper.Log("[Upgrade] FrostAbsolute activated"); }

    // ═══ 雷电专属 ═══

    private static void ApplyStaticDamage(MagePassive mage, UpgradeEntry ue) { mage.StaticDamageBonus += ue.value1; DebugHelper.Log($"[Upgrade] StaticDamage +{ue.value1} (total: {mage.StaticDamageBonus})"); }
    private static void ApplyStaticChain(MagePassive mage, UpgradeEntry ue) { mage.StaticChainBonus += (int)ue.value1; DebugHelper.Log($"[Upgrade] StaticChain +{ue.value1} (total: {mage.StaticChainBonus})"); }
    private static void ApplyStaticTick(MagePassive mage, UpgradeEntry ue) { mage.StaticTickReduction += ue.value1; DebugHelper.Log($"[Upgrade] StaticTick +{ue.value1} (total: {mage.StaticTickReduction})"); }
    private static void ApplyStaticRange(MagePassive mage, UpgradeEntry ue) { mage.StaticRangeBonus += ue.value1; DebugHelper.Log($"[Upgrade] StaticRange +{ue.value1} (total: {mage.StaticRangeBonus})"); }
    private static void ApplyStaticCrit(MagePassive mage, UpgradeEntry ue) { mage.StaticCritBonus += ue.value1; DebugHelper.Log($"[Upgrade] StaticCrit +{ue.value1} (total: {mage.StaticCritBonus})"); }
    private static void ApplyStaticOverload(MagePassive mage, UpgradeEntry ue) { mage.StaticOverload = true; DebugHelper.Log("[Upgrade] StaticOverload activated"); }
    private static void ApplyStormMulti(MagePassive mage, UpgradeEntry ue) { mage.StormMulti = true; DebugHelper.Log("[Upgrade] StormMulti activated"); }
    private static void ApplyStormChain(MagePassive mage, UpgradeEntry ue) { mage.StormChain = true; DebugHelper.Log("[Upgrade] StormChain activated"); }

    // ═══ 风专属 ═══

    private static void ApplyWindDamage(MagePassive mage, UpgradeEntry ue) { mage.WindDamageBonus += ue.value1; DebugHelper.Log($"[Upgrade] WindDamage +{ue.value1} (total: {mage.WindDamageBonus})"); }
    private static void ApplyWindSpeed(MagePassive mage, UpgradeEntry ue) { mage.WindSpeedBonus += ue.value1; DebugHelper.Log($"[Upgrade] WindSpeed +{ue.value1} (total: {mage.WindSpeedBonus})"); }
    private static void ApplyWindTick(MagePassive mage, UpgradeEntry ue) { mage.WindTickReduction += ue.value1; DebugHelper.Log($"[Upgrade] WindTick +{ue.value1} (total: {mage.WindTickReduction})"); }
    private static void ApplyWindPierce(MagePassive mage, UpgradeEntry ue) { mage.WindPierceBonus += (int)ue.value1; DebugHelper.Log($"[Upgrade] WindPierce +{ue.value1} (total: {mage.WindPierceBonus})"); }
    private static void ApplyWindCrit(MagePassive mage, UpgradeEntry ue) { mage.WindCritBonus += ue.value1; DebugHelper.Log($"[Upgrade] WindCrit +{ue.value1} (total: {mage.WindCritBonus})"); }
    private static void ApplyWindStormEye(MagePassive mage, UpgradeEntry ue) { mage.WindStormEye = true; DebugHelper.Log("[Upgrade] WindStormEye activated"); }
    private static void ApplyWindHurricane(MagePassive mage, UpgradeEntry ue) { mage.WindHurricane = true; DebugHelper.Log("[Upgrade] WindHurricane activated"); }
    private static void ApplyWindLord(MagePassive mage, UpgradeEntry ue) { mage.WindLord = true; DebugHelper.Log("[Upgrade] WindLord activated"); }

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
