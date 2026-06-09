using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Mage 角色升级配置 — 集中管理所有 Mage 专属升级数据和 DOT 子弹枪配置。
///
/// 解决问题：原先升级数据散落在 GameSceneBootstrap、LevelUpUI、MagePassive 三个文件中。
/// 使用此 ScriptableObject 后，新增/修改升级只需编辑此配置文件。
///
/// 使用方式：在 ScriptableObjects/Config/ 目录下创建 .asset 文件
/// </summary>
[CreateAssetMenu(fileName = "MageUpgradeConfig", menuName = "VampireGame/Mage Upgrade Config")]
public class MageUpgradeConfig : ScriptableObject
{
    [Header("角色信息覆盖")]
    public string description = "DOT 大师 — 所有持续伤害时间延长 20%，DOT 可暴击，拥有专属引爆技能。升级时获得独特的 DOT 强化选项。";
    public string passiveDescription = "DOT 持续时间 +20%，DOT 可暴击，按 Q 引爆所有 DOT 造成巨额伤害（冷却 12s）";
    public Color characterColor = new Color(0.6f, 0.2f, 0.9f); // 紫色

    [Header("DOT 子弹枪配置")]
    public DotGunEntry[] dotGunEntries = new DotGunEntry[]
    {
        new DotGunEntry
        {
            upgradeId = "poison",
            effectType = StatusEffectType.Poison,
            displayName = "中毒 (Poison)",
            description = "获得中毒子弹",
            color = new Color(0.1f, 0.9f, 0.2f),
            cooldown = 1.8f,
            impactDmg = 0,
            dotDps = 3f,
            dotDuration = 5f
        },
        new DotGunEntry
        {
            upgradeId = "burn",
            effectType = StatusEffectType.Burn,
            displayName = "燃烧 (Burn)",
            description = "获得燃烧子弹",
            color = new Color(1f, 0.4f, 0f),
            cooldown = 0.5f,
            impactDmg = 2,
            dotDps = 2f,
            dotDuration = 3f
        },
        new DotGunEntry
        {
            upgradeId = "frostbite",
            effectType = StatusEffectType.Frostbite,
            displayName = "霜冻 (Frostbite)",
            description = "获得霜冻子弹",
            color = new Color(0.3f, 0.6f, 1f),
            cooldown = 1.2f,
            impactDmg = 4,
            dotDps = 0f,
            dotDuration = 3f
        },
        new DotGunEntry
        {
            upgradeId = "static",
            effectType = StatusEffectType.Static,
            displayName = "雷电 (Static)",
            description = "获得雷电子弹",
            color = new Color(0.3f, 0.8f, 1f),
            cooldown = 0.9f,
            impactDmg = 5,
            dotDps = 0f,
            dotDuration = 0f
        },
        new DotGunEntry
        {
            upgradeId = "dark",
            effectType = StatusEffectType.Dark,
            displayName = "黑暗 (Dark)",
            description = "获得黑暗子弹",
            color = new Color(0.4f, 0.1f, 0.6f),
            cooldown = 2.5f,
            impactDmg = 0,
            dotDps = 0f,
            dotDuration = 0f
        },
        new DotGunEntry
        {
            upgradeId = "light",
            effectType = StatusEffectType.Light,
            displayName = "光明 (Light)",
            description = "获得光明子弹",
            color = new Color(1f, 1f, 0.9f),
            cooldown = 4.0f,
            impactDmg = 0,
            dotDps = 0f,
            dotDuration = 0f
        }
    };

    [Header("升级选项配置")]
    public UpgradeEntry[] upgradeEntries = new UpgradeEntry[]
    {
        // ═══ DOT 增强（4 种）═══
        new UpgradeEntry
        {
            upgradeId = "corrosion",
            upgradeName = "腐蚀 (Corrosion)",
            description = "护甲-10%",
            category = CharacterUpgradeOption.UpgradeCategory.ArmorReduction,
            value1 = 0.10f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "curse",
            upgradeName = "诅咒 (Curse)",
            description = "DOT传播+1目标",
            category = CharacterUpgradeOption.UpgradeCategory.DotSpread,
            value1 = 1f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "agony",
            upgradeName = "痛苦 (Agony)",
            description = "DOT频率+10%",
            category = CharacterUpgradeOption.UpgradeCategory.DotFrequency,
            value1 = 0.10f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "wither",
            upgradeName = "凋零 (Wither)",
            description = "DOT暴击+10%",
            category = CharacterUpgradeOption.UpgradeCategory.DotCritBurst,
            value1 = 0.10f, value2 = 0f, value3 = 0f, maxStacks = 0
        },

        // ═══ 引爆增强（2 种）═══
        new UpgradeEntry
        {
            upgradeId = "radiate",
            upgradeName = "辐射 (Radiation)",
            description = "引爆伤害+30%",
            category = CharacterUpgradeOption.UpgradeCategory.DetonateMultiplier,
            value1 = 0.30f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "contaminate",
            upgradeName = "污染 (Contaminate)",
            description = "引爆冷却-30%",
            category = CharacterUpgradeOption.UpgradeCategory.DetonateAbility,
            value1 = 0.30f, value2 = 0f, value3 = 0f, maxStacks = 0
        },

        // ═══ P0 新增强化（3 种）═══
        new UpgradeEntry
        {
            upgradeId = "saturation",
            upgradeName = "饱和 (Saturation)",
            description = "每种DOT伤害+5%",
            category = CharacterUpgradeOption.UpgradeCategory.DotSaturation,
            value1 = 0.05f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "elemental_burst",
            upgradeName = "元素引爆 (Elemental Burst)",
            description = "每种DOT+8伤害",
            category = CharacterUpgradeOption.UpgradeCategory.DetonateExtra,
            value1 = 8f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "vampiric_spell",
            upgradeName = "吸血法术 (Vampiric Spell)",
            description = "每次DOT回0.3血",
            category = CharacterUpgradeOption.UpgradeCategory.DotLifesteal,
            value1 = 0.3f, value2 = 0f, value3 = 0f, maxStacks = 0
        },

        // ═══ 子弹增强（3 种）═══
        new UpgradeEntry
        {
            upgradeId = "haste",
            upgradeName = "急速 (Haste)",
            description = "攻速+15% 速度+10%",
            category = CharacterUpgradeOption.UpgradeCategory.AttackSpeed,
            value1 = 0.15f, value2 = 0.10f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "barrage",
            upgradeName = "弹幕 (Barrage)",
            description = "子弹+1",
            category = CharacterUpgradeOption.UpgradeCategory.BulletCount,
            value1 = 1f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "ricochet",
            upgradeName = "贯穿弹 (Penetrate)",
            description = "穿透+1",
            category = CharacterUpgradeOption.UpgradeCategory.Ricochet,
            value1 = 1f, value2 = 0f, value3 = 0f, maxStacks = 3
        },

        // ═══ P1 深度玩法（3 种）═══
        new UpgradeEntry
        {
            upgradeId = "pandemic",
            upgradeName = "蔓延 (Pandemic)",
            description = "传播效率+15%",
            category = CharacterUpgradeOption.UpgradeCategory.DotPandemic,
            value1 = 0.15f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "chain_reaction",
            upgradeName = "连锁反应 (Chain Reaction)",
            description = "二次引爆50%伤害",
            category = CharacterUpgradeOption.UpgradeCategory.ChainReaction,
            value1 = 1f, value2 = 0.5f, value3 = 0f, maxStacks = 3
        },
        new UpgradeEntry
        {
            upgradeId = "dual_wield",
            upgradeName = "双持 (Dual Wield)",
            description = "DOT枪射速+15%",
            category = CharacterUpgradeOption.UpgradeCategory.DualWield,
            value1 = 0.15f, value2 = 0f, value3 = 0f, maxStacks = 0
        },

        // ═══ P2 协同/趣味（8 种）═══
        new UpgradeEntry
        {
            upgradeId = "resonance",
            upgradeName = "共鸣 (Resonance)",
            description = "DOT触发10%不消耗持续",
            category = CharacterUpgradeOption.UpgradeCategory.DotResonance,
            value1 = 0.10f, value2 = 0f, value3 = 0f, maxStacks = 5
        },
        new UpgradeEntry
        {
            upgradeId = "toxicology",
            upgradeName = "剧毒天赋 (Toxicology)",
            description = "DOT暴击率+8%",
            category = CharacterUpgradeOption.UpgradeCategory.Toxicology,
            value1 = 0.08f, value2 = 0f, value3 = 0f, maxStacks = 5
        },
        new UpgradeEntry
        {
            upgradeId = "corrupt_touch",
            upgradeName = "腐化之触 (Corrupt Touch)",
            description = "敌人攻击力-10%",
            category = CharacterUpgradeOption.UpgradeCategory.CorruptTouch,
            value1 = 0.10f, value2 = 2f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "elemental_storm",
            upgradeName = "元素风暴 (Elemental Storm)",
            description = "3种DOT时每2秒5点伤害",
            category = CharacterUpgradeOption.UpgradeCategory.ElementalStorm,
            value1 = 5f, value2 = 2f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "shadow_link",
            upgradeName = "暗影链接 (Shadow Link)",
            description = "传播范围+1 效率+10%",
            category = CharacterUpgradeOption.UpgradeCategory.ShadowLink,
            value1 = 1f, value2 = 0.10f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "light_judgment",
            upgradeName = "光明审判 (Light Judgment)",
            description = "光明标记每层+0.3%",
            category = CharacterUpgradeOption.UpgradeCategory.LightJudgment,
            value1 = 0.003f, value2 = 0f, value3 = 0f, maxStacks = 5
        },
        new UpgradeEntry
        {
            upgradeId = "static_field",
            upgradeName = "静电领域 (Static Field)",
            description = "静电扩散1.5范围+1层",
            category = CharacterUpgradeOption.UpgradeCategory.StaticField,
            value1 = 1.5f, value2 = 1f, value3 = 0f, maxStacks = 3
        },
        new UpgradeEntry
        {
            upgradeId = "frost_explosion",
            upgradeName = "霜爆 (Frost Explosion)",
            description = "减速80%+引爆3%最大生命",
            category = CharacterUpgradeOption.UpgradeCategory.FrostExplosion,
            value1 = 0.03f, value2 = 0.80f, value3 = 0f, maxStacks = 0
        },

        // ═══ 子弹增强扩展（3 种）═══
        new UpgradeEntry
        {
            upgradeId = "ammo_mastery",
            upgradeName = "弹药精通 (Ammo Mastery)",
            description = "子弹速度+20% 范围+15%",
            category = CharacterUpgradeOption.UpgradeCategory.AmmoMastery,
            value1 = 0.20f, value2 = 0.15f, value3 = 0f, maxStacks = 5
        },
        new UpgradeEntry
        {
            upgradeId = "elemental_affinity",
            upgradeName = "元素亲和 (Elemental Affinity)",
            description = "每种DOT枪+3%伤害",
            category = CharacterUpgradeOption.UpgradeCategory.ElementalAffinity,
            value1 = 0.03f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "penetrate",
            upgradeName = "贯穿弹 (Penetrate)",
            description = "DOT枪穿透+1",
            category = CharacterUpgradeOption.UpgradeCategory.Penetrate,
            value1 = 1f, value2 = 0f, value3 = 0f, maxStacks = 3
        },

        // ═══ 生存向（4 种）═══
        new UpgradeEntry
        {
            upgradeId = "phase_shift",
            upgradeName = "相位移动 (Phase Shift)",
            description = "引爆后无敌2秒",
            category = CharacterUpgradeOption.UpgradeCategory.PhaseShift,
            value1 = 2f, value2 = 0f, value3 = 0f, maxStacks = 3
        },
        new UpgradeEntry
        {
            upgradeId = "soul_siphon",
            upgradeName = "灵魂虹吸 (Soul Siphon)",
            description = "击杀回1血+30%移速",
            category = CharacterUpgradeOption.UpgradeCategory.SoulSiphon,
            value1 = 1f, value2 = 0.5f, value3 = 0.30f, maxStacks = 0
        },

        // ═══ P3 终极/高级（6 种）═══
        new UpgradeEntry
        {
            upgradeId = "ember_boost",
            upgradeName = "余烬强化 (Ember Boost)",
            description = "余烬伤害+25% 持续+1秒",
            category = CharacterUpgradeOption.UpgradeCategory.EmberBoost,
            value1 = 0.25f, value2 = 1f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "shatter_boost",
            upgradeName = "碎裂强化 (Shatter Boost)",
            description = "碎片+2 伤害+15%",
            category = CharacterUpgradeOption.UpgradeCategory.ShatterBoost,
            value1 = 2f, value2 = 0.15f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "elemental_master",
            upgradeName = "元素大师 (Elemental Master)",
            description = "DOT持续+25% 引爆CD-20%",
            category = CharacterUpgradeOption.UpgradeCategory.ElementalMaster,
            value1 = 0.25f, value2 = 0.20f, value3 = 0f, maxStacks = 1
        },
        new UpgradeEntry
        {
            upgradeId = "annihilation_zone",
            upgradeName = "湮灭领域 (Annihilation Zone)",
            description = "引爆后5点/秒+30%减速",
            category = CharacterUpgradeOption.UpgradeCategory.AnnihilationZone,
            value1 = 5f, value2 = 3f, value3 = 0.30f, maxStacks = 3
        }
    };

    // ═══ 运行时查询 API ═══

    /// <summary>
    /// 获取 DOT 子弹枪配置（根据 upgradeId）
    /// </summary>
    public DotGunEntry? GetDotGunEntry(string upgradeId)
    {
        if (dotGunEntries == null) return null;
        for (int i = 0; i < dotGunEntries.Length; i++)
        {
            if (dotGunEntries[i].upgradeId == upgradeId)
                return dotGunEntries[i];
        }
        return null;
    }

    /// <summary>
    /// 获取升级配置（根据 upgradeId）
    /// </summary>
    public UpgradeEntry? GetUpgradeEntry(string upgradeId)
    {
        if (upgradeEntries == null) return null;
        for (int i = 0; i < upgradeEntries.Length; i++)
        {
            if (upgradeEntries[i].upgradeId == upgradeId)
                return upgradeEntries[i];
        }
        return null;
    }

    /// <summary>
    /// 判断 upgradeId 是否是 DOT 子弹枪类型
    /// </summary>
    public bool IsDotGunUpgrade(string upgradeId)
    {
        return GetDotGunEntry(upgradeId).HasValue;
    }

    /// <summary>
    /// 获取所有 DOT 子弹枪的 effectType 集合（用于快速查找）
    /// </summary>
    public HashSet<StatusEffectType> GetAllDotGunTypes()
    {
        var set = new HashSet<StatusEffectType>();
        if (dotGunEntries != null)
        {
            for (int i = 0; i < dotGunEntries.Length; i++)
                set.Add(dotGunEntries[i].effectType);
        }
        return set;
    }

    /// <summary>
    /// 生成完整的 CharacterUpgradeOption[] 数组（供 CharacterData.customUpgrades 使用）
    /// 包含 DOT 子弹枪 + 增强升级，共 14 项
    /// </summary>
    public CharacterUpgradeOption[] BuildCustomUpgrades()
    {
        var list = new List<CharacterUpgradeOption>();

        // 先添加 DOT 子弹枪（7种）
        if (dotGunEntries != null)
        {
            for (int i = 0; i < dotGunEntries.Length; i++)
            {
                var dg = dotGunEntries[i];
                list.Add(new CharacterUpgradeOption
                {
                    upgradeId = dg.upgradeId,
                    upgradeName = dg.displayName,
                    description = dg.description,
                    category = CharacterUpgradeOption.UpgradeCategory.DotType,
                    value1 = 0f, value2 = 0f, value3 = 0f,
                    maxStacks = 0
                });
            }
        }

        // 再添加增强升级（10种）
        if (upgradeEntries != null)
        {
            for (int i = 0; i < upgradeEntries.Length; i++)
            {
                var ue = upgradeEntries[i];
                list.Add(new CharacterUpgradeOption
                {
                    upgradeId = ue.upgradeId,
                    upgradeName = ue.upgradeName,
                    description = ue.description,
                    category = ue.category,
                    value1 = ue.value1,
                    value2 = ue.value2,
                    value3 = ue.value3,
                    maxStacks = ue.maxStacks
                });
            }
        }

        return list.ToArray();
    }
}

/// <summary>
/// DOT 子弹枪配置条目
/// </summary>
[System.Serializable]
public struct DotGunEntry
{
    [Tooltip("唯一标识符，如 bleed/poison/burn/frostbite")]
    public string upgradeId;

    [Tooltip("状态效果类型")]
    public StatusEffectType effectType;

    [Tooltip("显示名称")]
    public string displayName;

    [TextArea(2, 3)]
    [Tooltip("升级描述")]
    public string description;

    [Tooltip("子弹颜色")]
    public Color color;

    [Tooltip("射击冷却时间")]
    public float cooldown;

    [Tooltip("命中即时伤害")]
    public int impactDmg;

    [Tooltip("DOT 每秒伤害")]
    public float dotDps;

    [Tooltip("DOT 持续时间（秒）")]
    public float dotDuration;
}

/// <summary>
/// 增强升级配置条目
/// </summary>
[System.Serializable]
public struct UpgradeEntry
{
    [Tooltip("唯一标识符")]
    public string upgradeId;

    [Tooltip("显示名称")]
    public string upgradeName;

    [TextArea(2, 3)]
    [Tooltip("升级描述")]
    public string description;

    [Tooltip("升级类别")]
    public CharacterUpgradeOption.UpgradeCategory category;

    [Tooltip("主要数值")]
    public float value1;

    [Tooltip("次要数值")]
    public float value2;

    [Tooltip("第三数值")]
    public float value3;

    [Tooltip("最大叠加次数（0=无限）")]
    public int maxStacks;
}