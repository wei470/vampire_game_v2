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
public class MageUpgradeConfig : ScriptableObject, ICharacterConfig
{
    [Header("角色信息覆盖")]
    public string characterId = "mage";
    public string displayName = "DOT 法师";
    public string description = "DOT 大师 — 所有持续伤害时间延长 20%，DOT 可暴击，拥有专属引爆技能。升级时获得独特的 DOT 强化选项。";
    public string passiveDescription = "DOT 持续时间 +20%，DOT 可暴击，按 Q 引爆所有 DOT 造成巨额伤害（冷却 12s）";
    public Color characterColor = new Color(0.6f, 0.2f, 0.9f); // 紫色

    // ── ICharacterConfig 实现 ──
    string ICharacterConfig.CharacterId => characterId;
    CharacterUpgradeOption[] ICharacterConfig.GetUpgradeOptions() => BuildCustomUpgrades();
    DotGunEntry[] ICharacterConfig.GetGunEntries() => dotGunEntries;

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
        },
        new DotGunEntry
        {
            upgradeId = "wind",
            effectType = StatusEffectType.WindErosion,
            displayName = "风 (Wind)",
            description = "获得风子弹（固定3发，0.5s冷却）",
            color = new Color(0.7f, 0.85f, 1f),
            cooldown = 0.5f,
            impactDmg = 0,
            dotDps = 0f,
            dotDuration = 0f
        }
    };

    [Header("升级选项配置")]
    public UpgradeEntry[] upgradeEntries = new UpgradeEntry[]
    {
        // ═══ DOT 增强（3 种）═══
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

        // ═══ 协同强化（3 种）═══
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

        // ═══ 一般强化（10种，全角色通用）═══
        new UpgradeEntry
        {
            upgradeId = "move_speed",
            upgradeName = "移速 (Move Speed)",
            description = "移动速度+10%",
            category = CharacterUpgradeOption.UpgradeCategory.MoveSpeed,
            value1 = 0.10f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "armor_bonus",
            upgradeName = "护甲 (Armor)",
            description = "护甲+5",
            category = CharacterUpgradeOption.UpgradeCategory.ArmorBonus,
            value1 = 5f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "max_hp",
            upgradeName = "生命 (Max HP)",
            description = "最大HP+20",
            category = CharacterUpgradeOption.UpgradeCategory.MaxHpBonus,
            value1 = 20f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "crit_chance",
            upgradeName = "暴击率 (Crit Chance)",
            description = "暴击率+5%",
            category = CharacterUpgradeOption.UpgradeCategory.CritChanceBonus,
            value1 = 0.05f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "crit_damage",
            upgradeName = "暴击伤害 (Crit Damage)",
            description = "暴击倍率+20%",
            category = CharacterUpgradeOption.UpgradeCategory.CritDamageBonus,
            value1 = 0.20f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "magnet_range",
            upgradeName = "磁力 (Magnet Range)",
            description = "拾取范围+30%",
            category = CharacterUpgradeOption.UpgradeCategory.MagnetRange,
            value1 = 0.30f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "hp_regen",
            upgradeName = "回复 (HP Regen)",
            description = "每秒回复1%HP",
            category = CharacterUpgradeOption.UpgradeCategory.HpRegen,
            value1 = 0.01f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "bullet_speed",
            upgradeName = "弹速 (Bullet Speed)",
            description = "子弹飞行速度+20%",
            category = CharacterUpgradeOption.UpgradeCategory.BulletSpeed,
            value1 = 0.20f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "knockback",
            upgradeName = "击退 (Knockback)",
            description = "击退距离+20%",
            category = CharacterUpgradeOption.UpgradeCategory.Knockback,
            value1 = 0.20f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "bullet_size",
            upgradeName = "弹体 (Bullet Size)",
            description = "子弹碰撞体积+15%",
            category = CharacterUpgradeOption.UpgradeCategory.BulletSize,
            value1 = 0.15f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
    };

    private static readonly HashSet<StatusEffectType> _tempDotGunTypes = new HashSet<StatusEffectType>();
    private static readonly List<CharacterUpgradeOption> _tempUpgradeList = new List<CharacterUpgradeOption>(32);

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
        _tempDotGunTypes.Clear();
        if (dotGunEntries != null)
        {
            for (int i = 0; i < dotGunEntries.Length; i++)
                _tempDotGunTypes.Add(dotGunEntries[i].effectType);
        }
        return _tempDotGunTypes;
    }

    /// <summary>
    /// 生成完整的 CharacterUpgradeOption[] 数组（供 CharacterData.customUpgrades 使用）
    /// 包含 DOT 子弹枪 + 增强升级，共 14 项
    /// </summary>
    public CharacterUpgradeOption[] BuildCustomUpgrades()
    {
        _tempUpgradeList.Clear();

        // 先添加 DOT 子弹枪（7种）
        if (dotGunEntries != null)
        {
            for (int i = 0; i < dotGunEntries.Length; i++)
            {
                var dg = dotGunEntries[i];
                _tempUpgradeList.Add(new CharacterUpgradeOption
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
                _tempUpgradeList.Add(new CharacterUpgradeOption
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

        return _tempUpgradeList.ToArray();
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