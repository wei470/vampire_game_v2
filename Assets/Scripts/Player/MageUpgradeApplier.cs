using UnityEngine;

/// <summary>
/// Mage 升级应用器 — 管理升级效果应用、协同检测、进化系统、里程碑。
/// 从 MagePassive 中提取，减少主文件行数。
///
/// 职责：
/// - ApplyUpgrade: 根据 upgradeId 应用升级效果
/// - 协同系统: 检测并激活4种协同
/// - 进化系统: DOT枪满级时触发进化
/// - 里程碑: 4种DOT枪全解锁时触发 Element Master
/// </summary>
public static class MageUpgradeApplier
{
    // ═══ 统一升级应用接口 ═══

    /// <summary>
    /// 应用升级效果，返回是否成功
    /// </summary>
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
        switch (ue.category)
        {
            case CharacterUpgradeOption.UpgradeCategory.ArmorReduction: mage.CorrosionArmorReduction += ue.value1; break;
            case CharacterUpgradeOption.UpgradeCategory.DotSpread: mage.CurseSpreadTargets += (int)ue.value1; break;
            case CharacterUpgradeOption.UpgradeCategory.DotFrequency: mage.DotFrequencyBonus += ue.value1; break;
            case CharacterUpgradeOption.UpgradeCategory.DotCritBurst: mage.DotCritBurstChance += ue.value1; break;
            case CharacterUpgradeOption.UpgradeCategory.DetonateMultiplier: mage.DetonateMultiplier += ue.value1; break;
            case CharacterUpgradeOption.UpgradeCategory.DetonateAbility: mage.DetonateCooldownValue *= (1f - ue.value1); break;
            case CharacterUpgradeOption.UpgradeCategory.DotTrigger:
                mage.ErosionTriggerCount = Mathf.Max(2, mage.ErosionTriggerCount - (int)ue.value1);
                mage.ErosionDamagePercent += ue.value2;
                break;
            case CharacterUpgradeOption.UpgradeCategory.AttackSpeed:
                mage.AttackSpeedBonus += ue.value1;
                mage.BulletSpeedBonus += ue.value2;
                break;
            case CharacterUpgradeOption.UpgradeCategory.BulletCount: mage.BulletCountBonus += (int)ue.value1; break;
            case CharacterUpgradeOption.UpgradeCategory.Ricochet:
                mage.RicochetChance += ue.value1;
                if (mage.RicochetChance > 1f) { mage.RicochetMaxBounces += 1; mage.RicochetChance -= 1f; }
                break;
            case CharacterUpgradeOption.UpgradeCategory.BulletSize:
                mage.BulletSizeBonus += ue.value1;
                mage.KnockbackBonus += ue.value2;
                break;
        }

        CheckSynergies(mage);
        return true;
    }

    // ═══ 进化系统 ═══

    /// <summary>
    /// 检查并触发进化（DOT枪达到5级时）
    /// </summary>
    public static void CheckEvolution(MagePassive mage, StatusEffectType type)
    {
        if (mage.IsEvolved(type)) return;
        mage.EvolvedTypes.Add(type);
        var evo = GetEvolutionInfo(type);
        DebugHelper.Log($"[MageUpgradeApplier] ✦ EVOLVED: {evo.name}!");
        var player = GameReferences.Player;
        if (player != null)
            DamagePopup.Create(player.transform.position + Vector3.up * 3f, 0, new Color(1f, 0.85f, 0f), false, $"✦ EVOLVED: {evo.name}!");
        if (SFXManager.Instance != null) SFXManager.Instance.PlayLevelUp();
        ApplyEvolutionBonus(mage, type);
    }

    private static (string name, string description) GetEvolutionInfo(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Bleed: return ("血之狂潮 (Blood Tide)", "流血 DPS x2");
            case StatusEffectType.Poison: return ("瘟疫之源 (Plague Source)", "中毒子弹 DPS +50%");
            case StatusEffectType.Burn: return ("地狱之火 (Hellfire)", "燃烧 DPS +80%");
            case StatusEffectType.Frostbite: return ("绝对零度 (Absolute Zero)", "霜冻 DPS +100%");
            default: return ("Unknown", "");
        }
    }

    private static void ApplyEvolutionBonus(MagePassive mage, StatusEffectType type)
    {
        float mult = 1f;
        switch (type)
        {
            case StatusEffectType.Bleed: mult = 2f; break;
            case StatusEffectType.Poison: mult = 1.5f; break;
            case StatusEffectType.Burn: mult = 1.8f; break;
            case StatusEffectType.Frostbite: mult = 2f; break;
        }
        var guns = mage.DotGuns;
        for (int i = 0; i < guns.Count; i++)
        {
            var gun = guns[i];
            if (gun.effectType == type) { gun.dotDps *= mult; guns[i] = gun; break; }
        }
    }

    // ═══ 里程碑 ═══

    /// <summary>
    /// 检查里程碑（4种DOT枪全解锁时触发 Element Master）
    /// </summary>
    public static void CheckMilestones(MagePassive mage)
    {
        if (!mage.ElementMasterTriggered && mage.DotGuns.Count >= 4)
        {
            mage.ElementMasterTriggered = true;
            DebugHelper.Log("[MageUpgradeApplier] ★ MILESTONE: Element Master! All DOT damage +20%");
            var player = GameReferences.Player;
            if (player != null) DamagePopup.Create(player.transform.position + Vector3.up * 2f, 0, new Color(1f, 0.85f, 0f), false, "★ ELEMENT MASTER");
            mage.SyncDotDamageMultiplierToAll();
        }
    }

    // ═══ 协同系统 ═══

    private enum SynergyType { Plague, FrozenBlade, BulletStorm, JudgmentDay }

    /// <summary>
    /// 检测并激活协同效果
    /// </summary>
    public static void CheckSynergies(MagePassive mage)
    {
        var guns = mage.DotGuns;
        bool hasPoison = false, hasBurn = false, hasCorrosion = false;
        for (int i = 0; i < guns.Count; i++)
        {
            if (guns[i].effectType == StatusEffectType.Poison) hasPoison = true;
            if (guns[i].effectType == StatusEffectType.Burn) hasBurn = true;
        }
        hasCorrosion = mage.CorrosionArmorReduction > 0.101f;
        TryActivateSynergy(mage, "plague", SynergyType.Plague, hasPoison && hasBurn && hasCorrosion);

        bool hasBleed = false, hasFrost = false, hasCurse = false;
        for (int i = 0; i < guns.Count; i++)
        {
            if (guns[i].effectType == StatusEffectType.Bleed) hasBleed = true;
            if (guns[i].effectType == StatusEffectType.Frostbite) hasFrost = true;
        }
        hasCurse = mage.CurseSpreadTargets > 1;
        TryActivateSynergy(mage, "frozen_blade", SynergyType.FrozenBlade, hasBleed && hasFrost && hasCurse);

        bool hasBarrage = mage.BulletCountBonus > 0;
        bool hasHaste = mage.AttackSpeedBonus > 0.001f;
        bool hasRicochet = mage.RicochetChance > 0.001f;
        TryActivateSynergy(mage, "bullet_storm", SynergyType.BulletStorm, hasBarrage && hasHaste && hasRicochet);

        bool hasRadiate = mage.DetonateMultiplier > 3.01f;
        bool hasContaminate = mage.DetonateCooldownValue < 11.99f;
        bool hasErosion = mage.ErosionDamagePercent > 0.001f;
        TryActivateSynergy(mage, "judgment_day", SynergyType.JudgmentDay, hasRadiate && hasContaminate && hasErosion);
    }

    private static void TryActivateSynergy(MagePassive mage, string synergyId, SynergyType type, bool conditionMet)
    {
        if (!conditionMet || mage.ActiveSynergies.Contains(synergyId)) return;
        mage.ActiveSynergies.Add(synergyId);
        var player = GameReferences.Player;
        if (player != null)
        {
            string name = GetSynergyName(type);
            Color color = GetSynergyColor(type);
            DamagePopup.Create(player.transform.position + Vector3.up * 2.5f, 0, color, false, $"✦ SYNERGY: {name}!");
        }
        if (SFXManager.Instance != null) SFXManager.Instance.PlayLevelUp();
        DebugHelper.Log($"[MageUpgradeApplier] ✦ SYNERGY ACTIVATED: {GetSynergyName(type)}!");
    }

    private static string GetSynergyName(SynergyType type)
    {
        switch (type) { case SynergyType.Plague: return "瘟疫"; case SynergyType.FrozenBlade: return "冰封血刃"; case SynergyType.BulletStorm: return "弹雨风暴"; case SynergyType.JudgmentDay: return "末日审判"; default: return "Unknown"; }
    }

    private static Color GetSynergyColor(SynergyType type)
    {
        switch (type) { case SynergyType.Plague: return new Color(0.1f, 0.9f, 0.3f); case SynergyType.FrozenBlade: return new Color(0.4f, 0.7f, 1f); case SynergyType.BulletStorm: return new Color(1f, 0.9f, 0.2f); case SynergyType.JudgmentDay: return new Color(1f, 0.3f, 0.1f); default: return Color.white; }
    }
}