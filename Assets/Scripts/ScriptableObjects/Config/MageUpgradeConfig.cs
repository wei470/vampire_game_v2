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
                // ═══ 通用强化 ═══
                // 灰色
                new UpgradeEntry { upgradeId = "move_speed", upgradeName = "移速 (Move Speed)", description = "移动速度+10%", category = CharacterUpgradeOption.UpgradeCategory.MoveSpeed, rarity = UpgradeRarity.Common, value1 = 0.10f, maxStacks = 5 },
                new UpgradeEntry { upgradeId = "erosion", upgradeName = "侵蚀 (Erosion)", description = "无视敌人1点护甲", category = CharacterUpgradeOption.UpgradeCategory.ArmorPenetration, rarity = UpgradeRarity.Common, value1 = 1f, maxStacks = 0 },
                // 绿色
                new UpgradeEntry { upgradeId = "haste", upgradeName = "急速 (Haste)", description = "攻速+15%", category = CharacterUpgradeOption.UpgradeCategory.AttackSpeed, rarity = UpgradeRarity.Uncommon, value1 = 0.15f, maxStacks = 10 },
                new UpgradeEntry { upgradeId = "radiate", upgradeName = "辐射 (Radiation)", description = "引爆伤害×115%", category = CharacterUpgradeOption.UpgradeCategory.DetonateMultiplier, rarity = UpgradeRarity.Uncommon, value1 = 0.15f, maxStacks = 10 },
                // 蓝色
                new UpgradeEntry { upgradeId = "corrosion", upgradeName = "腐蚀 (Corrosion)", description = "护甲×90%", category = CharacterUpgradeOption.UpgradeCategory.ArmorReduction, rarity = UpgradeRarity.Rare, value1 = 0.10f, maxStacks = 8 },
                new UpgradeEntry { upgradeId = "contaminate", upgradeName = "污染 (Contaminate)", description = "引爆冷却-10%", category = CharacterUpgradeOption.UpgradeCategory.DetonateAbility, rarity = UpgradeRarity.Rare, value1 = 0.10f, maxStacks = 6 },
                // 紫色
                new UpgradeEntry { upgradeId = "barrage", upgradeName = "弹幕 (Barrage)", description = "子弹+1", category = CharacterUpgradeOption.UpgradeCategory.BulletCount, rarity = UpgradeRarity.Epic, value1 = 1f, maxStacks = 2 },
                new UpgradeEntry { upgradeId = "ricochet", upgradeName = "贯穿弹 (Penetrate)", description = "穿透+1", category = CharacterUpgradeOption.UpgradeCategory.Ricochet, rarity = UpgradeRarity.Epic, value1 = 1f, maxStacks = 3 },

                // ═══ 中毒专属 ═══
                // 灰色
                new UpgradeEntry { upgradeId = "poison_duration", upgradeName = "毒素持久", description = "毒素持续时间+1s", category = CharacterUpgradeOption.UpgradeCategory.PoisonDuration, rarity = UpgradeRarity.Common, value1 = 1f, maxStacks = 5 },
                new UpgradeEntry { upgradeId = "poison_dps", upgradeName = "毒素强化", description = "毒素DPS+1", category = CharacterUpgradeOption.UpgradeCategory.PoisonDps, rarity = UpgradeRarity.Common, value1 = 1f, maxStacks = 5 },
                // 绿色
                new UpgradeEntry { upgradeId = "poison_pool", upgradeName = "毒液扩散", description = "毒液池范围+20%", category = CharacterUpgradeOption.UpgradeCategory.PoisonPool, rarity = UpgradeRarity.Uncommon, value1 = 0.20f, maxStacks = 3 },
                new UpgradeEntry { upgradeId = "poison_tick", upgradeName = "毒素加速", description = "毒素叠层间隔-15%", category = CharacterUpgradeOption.UpgradeCategory.PoisonTick, rarity = UpgradeRarity.Uncommon, value1 = 0.15f, maxStacks = 3 },
                // 紫色
                new UpgradeEntry { upgradeId = "poison_lethal", upgradeName = "猛毒", description = "毒素DPS×2", category = CharacterUpgradeOption.UpgradeCategory.PoisonLethal, rarity = UpgradeRarity.Epic, value1 = 2f, maxStacks = 1 },

                // ═══ 燃烧专属 ═══
                // 灰色
                new UpgradeEntry { upgradeId = "burn_duration", upgradeName = "火焰持久", description = "燃烧持续时间+1s", category = CharacterUpgradeOption.UpgradeCategory.BurnDuration, rarity = UpgradeRarity.Common, value1 = 1f, maxStacks = 5 },
                new UpgradeEntry { upgradeId = "burn_radius", upgradeName = "火场扩大", description = "火场范围+15%", category = CharacterUpgradeOption.UpgradeCategory.BurnRadius, rarity = UpgradeRarity.Common, value1 = 0.15f, maxStacks = 3 },
                // 绿色
                new UpgradeEntry { upgradeId = "burn_tick", upgradeName = "火焰加速", description = "火场叠层间隔-0.1s", category = CharacterUpgradeOption.UpgradeCategory.BurnTick, rarity = UpgradeRarity.Uncommon, value1 = 0.1f, maxStacks = 3 },
                new UpgradeEntry { upgradeId = "burn_slow", upgradeName = "火场减速", description = "火场内敌人移速-20%", category = CharacterUpgradeOption.UpgradeCategory.BurnSlow, rarity = UpgradeRarity.Uncommon, value1 = 0.20f, maxStacks = 3 },
                // 蓝色
                new UpgradeEntry { upgradeId = "burn_melt", upgradeName = "融化精通", description = "融化DOT×3", category = CharacterUpgradeOption.UpgradeCategory.BurnMelt, rarity = UpgradeRarity.Rare, value1 = 3f, maxStacks = 1 },
                // 紫色
                new UpgradeEntry { upgradeId = "burn_burst", upgradeName = "炎爆", description = "燃烧满层爆炸", category = CharacterUpgradeOption.UpgradeCategory.BurnBurst, rarity = UpgradeRarity.Epic, value1 = 1f, maxStacks = 1 },

                // ═══ 霜冻专属 ═══
                // 灰色
                new UpgradeEntry { upgradeId = "frost_slow", upgradeName = "减速强化", description = "霜冻减速+10%", category = CharacterUpgradeOption.UpgradeCategory.FrostSlow, rarity = UpgradeRarity.Common, value1 = 0.10f, maxStacks = 5 },

                // ═══ 雷电专属 ═══
                // 灰色
                new UpgradeEntry { upgradeId = "static_chain", upgradeName = "连锁强化", description = "连锁目标+1", category = CharacterUpgradeOption.UpgradeCategory.StaticChain, rarity = UpgradeRarity.Common, value1 = 1f, maxStacks = 3 },
                // 绿色
                new UpgradeEntry { upgradeId = "static_range", upgradeName = "雷电范围", description = "连锁范围+20%", category = CharacterUpgradeOption.UpgradeCategory.StaticRange, rarity = UpgradeRarity.Uncommon, value1 = 0.20f, maxStacks = 3 },
                // 紫色
                new UpgradeEntry { upgradeId = "storm_multi", upgradeName = "雷暴", description = "连续发射两枚雷电子弹", category = CharacterUpgradeOption.UpgradeCategory.StormMulti, rarity = UpgradeRarity.Epic, value1 = 2f, maxStacks = 1 },
                new UpgradeEntry { upgradeId = "storm_chain", upgradeName = "万雷齐发", description = "连锁目标×2", category = CharacterUpgradeOption.UpgradeCategory.StormChain, rarity = UpgradeRarity.Epic, value1 = 2f, maxStacks = 1 },

                // ═══ 风专属 ═══
                // 绿色
                new UpgradeEntry { upgradeId = "wind_speed", upgradeName = "风速", description = "风弹速度+20%", category = CharacterUpgradeOption.UpgradeCategory.WindSpeed, rarity = UpgradeRarity.Uncommon, value1 = 0.20f, maxStacks = 3 },
                // 紫色
                new UpgradeEntry { upgradeId = "wind_precision", upgradeName = "精准", description = "偏移角度缩小为5°", category = CharacterUpgradeOption.UpgradeCategory.WindTick, rarity = UpgradeRarity.Epic, value1 = 5f, maxStacks = 1 },
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
