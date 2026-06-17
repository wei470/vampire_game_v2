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
                // 绿色
                new UpgradeEntry { upgradeId = "haste", upgradeName = "急速 (Haste)", description = "攻速+15%", category = CharacterUpgradeOption.UpgradeCategory.AttackSpeed, rarity = UpgradeRarity.Uncommon, value1 = 0.15f, maxStacks = 10 },
                new UpgradeEntry { upgradeId = "radiate", upgradeName = "辐射 (Radiation)", description = "引爆伤害×115%", category = CharacterUpgradeOption.UpgradeCategory.DetonateMultiplier, rarity = UpgradeRarity.Uncommon, value1 = 0.15f, maxStacks = 10 },
                // 蓝色
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
                new UpgradeEntry { upgradeId = "burn_cinders", upgradeName = "余烬", description = "燃烧结束时残留余烬", category = CharacterUpgradeOption.UpgradeCategory.BurnStorm, rarity = UpgradeRarity.Uncommon, value1 = 1f, maxStacks = 1 },
                // 蓝色
                new UpgradeEntry { upgradeId = "burn_melt", upgradeName = "融化精通", description = "融化DOT×3", category = CharacterUpgradeOption.UpgradeCategory.BurnMelt, rarity = UpgradeRarity.Rare, value1 = 3f, maxStacks = 1 },
                new UpgradeEntry { upgradeId = "burn_firmament", upgradeName = "焚天", description = "额外发射跟踪小火箭，命中叠1层燃烧，攻速为燃烧子弹的2倍", category = CharacterUpgradeOption.UpgradeCategory.BurnFirmament, rarity = UpgradeRarity.Rare, value1 = 1f, maxStacks = 1 },
                // 紫色
                new UpgradeEntry { upgradeId = "burn_burst", upgradeName = "炎爆", description = "燃烧满层爆炸", category = CharacterUpgradeOption.UpgradeCategory.BurnBurst, rarity = UpgradeRarity.Epic, value1 = 1f, maxStacks = 1 },

                // ═══ 霜冻专属 ═══
                // 灰色
                new UpgradeEntry { upgradeId = "frost_winter", upgradeName = "冬天", description = "霜冻减速上限+10%/层（敌人最低速40%→35%→30%）", category = CharacterUpgradeOption.UpgradeCategory.FrostMaxSlow, rarity = UpgradeRarity.Common, value1 = 0.05f, maxStacks = 3 },
                new UpgradeEntry { upgradeId = "frost_deep_winter", upgradeName = "寒冬", description = "冰冻子弹每层减速+1%", category = CharacterUpgradeOption.UpgradeCategory.FrostPerStackSlow, rarity = UpgradeRarity.Common, value1 = 0.01f, maxStacks = 5 },
                // 蓝色
                new UpgradeEntry { upgradeId = "frost_frozen_hands", upgradeName = "冻手", description = "被减速敌人射出的子弹弹速降低33%", category = CharacterUpgradeOption.UpgradeCategory.FrozenHands, rarity = UpgradeRarity.Rare, value1 = 0.33f, maxStacks = 1 },
                new UpgradeEntry { upgradeId = "frost_ice_blade", upgradeName = "冰刃", description = "每射出5发霜冻子弹追加一波5发散弹冰弹，速度2倍，命中叠1层霜冻", category = CharacterUpgradeOption.UpgradeCategory.IceBlade, rarity = UpgradeRarity.Rare, value1 = 5f, maxStacks = 1 },
                new UpgradeEntry { upgradeId = "frost_cold_embrace", upgradeName = "冷酷之拥", description = "冰冻增加DOT伤害，伤害为4×层数，参与引爆", category = CharacterUpgradeOption.UpgradeCategory.ColdEmbrace, rarity = UpgradeRarity.Rare, value1 = 4f, maxStacks = 1 },
                // 紫色
                new UpgradeEntry { upgradeId = "frost_snowy_day", upgradeName = "下雪天", description = "场地略微变蓝，每1秒落下冰雹，命中敌人施加2层霜冻", category = CharacterUpgradeOption.UpgradeCategory.SnowyDay, rarity = UpgradeRarity.Epic, value1 = 2f, maxStacks = 1 },
                new UpgradeEntry { upgradeId = "frost_cold_bullet", upgradeName = "冷弹", description = "霜冻子弹和冰刃可通过墙壁反弹2次，子弹存在时间+5秒", category = CharacterUpgradeOption.UpgradeCategory.ColdBullet, rarity = UpgradeRarity.Epic, value1 = 2f, maxStacks = 1 },

                // ═══ 雷电专属 ═══
                // 灰色
                new UpgradeEntry { upgradeId = "static_chain", upgradeName = "连锁强化", description = "连锁目标+1", category = CharacterUpgradeOption.UpgradeCategory.StaticChain, rarity = UpgradeRarity.Common, value1 = 1f, maxStacks = 3 },
                // 绿色
                new UpgradeEntry { upgradeId = "static_range", upgradeName = "雷电范围", description = "连锁范围+20%", category = CharacterUpgradeOption.UpgradeCategory.StaticRange, rarity = UpgradeRarity.Uncommon, value1 = 0.20f, maxStacks = 3 },
                // 紫色
                new UpgradeEntry { upgradeId = "storm_multi", upgradeName = "雷暴", description = "每发雷电子弹都会变成双发，延迟0.1秒", category = CharacterUpgradeOption.UpgradeCategory.StormMulti, rarity = UpgradeRarity.Epic, value1 = 2f, maxStacks = 1 },
                new UpgradeEntry { upgradeId = "storm_chain", upgradeName = "静电爆炸", description = "每射出5发雷电子弹，下一发命中后产生半个屏幕的爆炸，范围内所有敌人+1层雷电印记", category = CharacterUpgradeOption.UpgradeCategory.StormChain, rarity = UpgradeRarity.Epic, value1 = 2f, maxStacks = 1 },
                new UpgradeEntry { upgradeId = "paralysis", upgradeName = "瘫痪", description = "身上带有雷电层数的敌人受到的所有DOT伤害+10%", category = CharacterUpgradeOption.UpgradeCategory.Paralysis, rarity = UpgradeRarity.Rare, value1 = 0.10f, maxStacks = 1 },

                // ═══ 风专属 ═══
                // 绿色
                new UpgradeEntry { upgradeId = "wind_speed", upgradeName = "风速", description = "风弹速度+20%", category = CharacterUpgradeOption.UpgradeCategory.WindSpeed, rarity = UpgradeRarity.Uncommon, value1 = 0.20f, maxStacks = 3 },
                new UpgradeEntry { upgradeId = "wind_typhoon", upgradeName = "台风", description = "风子弹击退距离+20%", category = CharacterUpgradeOption.UpgradeCategory.Typhoon, rarity = UpgradeRarity.Uncommon, value1 = 0.20f, maxStacks = 1 },
                // 蓝色
                new UpgradeEntry { upgradeId = "wind_wild", upgradeName = "狂风", description = "DOT引爆时击退所有敌人", category = CharacterUpgradeOption.UpgradeCategory.WildWind, rarity = UpgradeRarity.Rare, value1 = 1f, maxStacks = 1 },
                new UpgradeEntry { upgradeId = "wind_swift", upgradeName = "速风", description = "30%概率子弹速度200%并穿透所有敌人", category = CharacterUpgradeOption.UpgradeCategory.SwiftWind, rarity = UpgradeRarity.Rare, value1 = 0.30f, maxStacks = 1 },
                // 紫色
                new UpgradeEntry { upgradeId = "wind_precision", upgradeName = "精准", description = "偏移角度缩小为5°", category = CharacterUpgradeOption.UpgradeCategory.WindTick, rarity = UpgradeRarity.Epic, value1 = 5f, maxStacks = 1 },
                new UpgradeEntry { upgradeId = "wind_tornado", upgradeName = "龙卷风", description = "20%概率替换为龙卷风，速度30%，击退200%", category = CharacterUpgradeOption.UpgradeCategory.Tornado, rarity = UpgradeRarity.Epic, value1 = 0.20f, maxStacks = 1 },
                new UpgradeEntry { upgradeId = "wind_storm", upgradeName = "暴风", description = "33%概率替换为三连发（延迟0.2秒）", category = CharacterUpgradeOption.UpgradeCategory.StormWind, rarity = UpgradeRarity.Epic, value1 = 0.33f, maxStacks = 1 },
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
