using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Mage 角色升级配置 — 继承 CharacterUpgradeConfig，定义 Mage 专属升级数据。
///
/// 使用方式：在 ScriptableObjects/Config/ 目录下创建 .asset 文件
/// </summary>
[CreateAssetMenu(fileName = "MageUpgradeConfig", menuName = "VampireGame/Mage Upgrade Config")]
public class MageUpgradeConfig : CharacterUpgradeConfig
{
    private static readonly HashSet<StatusEffectType> _tempDotGunTypes = new HashSet<StatusEffectType>();

    private void OnEnable()
    {
        characterId = "mage";
        displayName = "DOT 法师";
        description = "DOT 大师 — 所有持续伤害时间延长 20%，DOT 可暴击，拥有专属引爆技能。升级时获得独特的 DOT 强化选项。";
        passiveDescription = "DOT 持续时间 +20%，DOT 可暴击，按 Q 引爆所有 DOT 造成巨额伤害（冷却 12s）";
        characterColor = new Color(0.6f, 0.2f, 0.9f);

        if (dotGunEntries == null || dotGunEntries.Length == 0)
        {
            dotGunEntries = new DotGunEntry[]
            {
                new DotGunEntry
                {
                    upgradeId = "poison", effectType = StatusEffectType.Poison,
                    displayName = "中毒 (Poison)", description = "获得中毒子弹",
                    color = new Color(0.1f, 0.9f, 0.2f), cooldown = 1.8f, impactDmg = 0, dotDps = 3f, dotDuration = 5f
                },
                new DotGunEntry
                {
                    upgradeId = "burn", effectType = StatusEffectType.Burn,
                    displayName = "燃烧 (Burn)", description = "射出缓慢移动的大型火场，区域内敌人每0.5秒叠层",
                    color = new Color(1f, 0.4f, 0f), cooldown = 5.0f, impactDmg = 0, dotDps = 2f, dotDuration = 3f
                },
                new DotGunEntry
                {
                    upgradeId = "frostbite", effectType = StatusEffectType.Frostbite,
                    displayName = "霜冻 (Frostbite)", description = "获得霜冻子弹",
                    color = new Color(0.3f, 0.6f, 1f), cooldown = 1.2f, impactDmg = 4, dotDps = 0f, dotDuration = 3f
                },
                new DotGunEntry
                {
                    upgradeId = "static", effectType = StatusEffectType.Static,
                    displayName = "雷电 (Static)", description = "获得雷电子弹",
                    color = new Color(0.3f, 0.8f, 1f), cooldown = 0.9f, impactDmg = 5, dotDps = 0f, dotDuration = 0f
                },
                new DotGunEntry
                {
                    upgradeId = "dark", effectType = StatusEffectType.Dark,
                    displayName = "黑暗 (Dark)", description = "获得黑暗子弹",
                    color = new Color(0.4f, 0.1f, 0.6f), cooldown = 2.5f, impactDmg = 0, dotDps = 0f, dotDuration = 0f
                },
                new DotGunEntry
                {
                    upgradeId = "light", effectType = StatusEffectType.Light,
                    displayName = "光明 (Light)", description = "获得光明子弹",
                    color = new Color(1f, 1f, 0.9f), cooldown = 4.0f, impactDmg = 0, dotDps = 0f, dotDuration = 0f
                },
                new DotGunEntry
                {
                    upgradeId = "wind", effectType = StatusEffectType.WindErosion,
                    displayName = "风 (Wind)", description = "获得风子弹（高速0.2s冷却，随机偏射±25°）",
                    color = new Color(0.7f, 0.85f, 1f), cooldown = 0.2f, impactDmg = 0, dotDps = 0f, dotDuration = 0f
                }
            };
        }

        if (upgradeEntries == null || upgradeEntries.Length == 0)
        {
            upgradeEntries = new UpgradeEntry[]
            {
                new UpgradeEntry { upgradeId = "corrosion", upgradeName = "腐蚀 (Corrosion)", description = "护甲×90%，最多8层", category = CharacterUpgradeOption.UpgradeCategory.ArmorReduction, value1 = 0.10f, maxStacks = 8 },
                new UpgradeEntry { upgradeId = "erosion", upgradeName = "侵蚀 (Erosion)", description = "无视敌人1点护甲，无限叠加", category = CharacterUpgradeOption.UpgradeCategory.ArmorPenetration, value1 = 1f, maxStacks = 0 },
                new UpgradeEntry { upgradeId = "radiate", upgradeName = "辐射 (Radiation)", description = "引爆伤害×115%，最多10层", category = CharacterUpgradeOption.UpgradeCategory.DetonateMultiplier, value1 = 0.15f, maxStacks = 10 },
                new UpgradeEntry { upgradeId = "contaminate", upgradeName = "污染 (Contaminate)", description = "引爆冷却-10%，最多6层", category = CharacterUpgradeOption.UpgradeCategory.DetonateAbility, value1 = 0.10f, maxStacks = 6 },
                new UpgradeEntry { upgradeId = "haste", upgradeName = "急速 (Haste)", description = "攻速+15%，最多10层", category = CharacterUpgradeOption.UpgradeCategory.AttackSpeed, value1 = 0.15f, maxStacks = 10 },
                new UpgradeEntry { upgradeId = "barrage", upgradeName = "弹幕 (Barrage)", description = "子弹+1，最多2层", category = CharacterUpgradeOption.UpgradeCategory.BulletCount, value1 = 1f, maxStacks = 2 },
                new UpgradeEntry { upgradeId = "ricochet", upgradeName = "贯穿弹 (Penetrate)", description = "穿透+1", category = CharacterUpgradeOption.UpgradeCategory.Ricochet, value1 = 1f, maxStacks = 3 },
                new UpgradeEntry { upgradeId = "move_speed", upgradeName = "移速 (Move Speed)", description = "移动速度+10%", category = CharacterUpgradeOption.UpgradeCategory.MoveSpeed, value1 = 0.10f, maxStacks = 0 },
            };
        }
    }

    /// <summary>
    /// 获取所有 DOT 子弹枪的 effectType 集合
    /// </summary>
    public HashSet<StatusEffectType> GetAllDotGunTypes()
    {
        _tempDotGunTypes.Clear();
        if (dotGunEntries != null)
        {
            for (int i = 0; i < dotGunEntries.Length; i++)
                _tempDotGunTypes.Add(dotGunEntries[i].effectType);
        }
        return _tempDotGunTypes;
    }
}
