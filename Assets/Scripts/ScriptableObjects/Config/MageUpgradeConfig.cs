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
            upgradeId = "bleed",
            effectType = StatusEffectType.Bleed,
            displayName = "流血 (Bleed)",
            description = "🔴 红色子弹 | DPS:3/s | 持续4秒\n移动越快受伤越频繁",
            color = new Color(0.9f, 0.1f, 0.1f),
            cooldown = 1.0f,
            impactDmg = 3,
            dotDps = 3f,
            dotDuration = 4f
        },
        new DotGunEntry
        {
            upgradeId = "poison",
            effectType = StatusEffectType.Poison,
            displayName = "中毒 (Poison)",
            description = "🟢 药瓶爆炸生成毒液池 | 持续5秒\n叠加层数越高伤害越高",
            color = new Color(0.1f, 0.9f, 0.2f),
            cooldown = 2.0f,
            impactDmg = 0,
            dotDps = 3f,
            dotDuration = 5f
        },
        new DotGunEntry
        {
            upgradeId = "burn",
            effectType = StatusEffectType.Burn,
            displayName = "燃烧 (Burn)",
            description = "🟠 快速橙色子弹 | DPS:2/s | 持续3秒\n叠加层数加速燃烧频率",
            color = new Color(1f, 0.4f, 0f),
            cooldown = 1.0f,
            impactDmg = 2,
            dotDps = 2f,
            dotDuration = 3f
        },
        new DotGunEntry
        {
            upgradeId = "frostbite",
            effectType = StatusEffectType.Frostbite,
            displayName = "霜冻 (Frostbite)",
            description = "🔵 快速冰霜子弹 | 冰冻1秒\n永久减速30%，每层额外-5%\n最低降至90%减速",
            color = new Color(0.3f, 0.6f, 1f),
            cooldown = 1.0f,
            impactDmg = 4,
            dotDps = 0f,
            dotDuration = 3f
        },
        new DotGunEntry
        {
            upgradeId = "static",
            effectType = StatusEffectType.Static,
            displayName = "雷电 (Static)",
            description = "⚡ 连锁闪电 | 命中敌人后连锁附近最多3个敌人\n被连锁的敌人获得静电层数\n每层降低0.1秒触发间隔（初始5秒，最低2秒）\n暂停移动0.5秒",
            color = new Color(0.3f, 0.8f, 1f),
            cooldown = 1.0f,
            impactDmg = 5,
            dotDps = 0f,
            dotDuration = 0f
        },
        new DotGunEntry
        {
            upgradeId = "dark",
            effectType = StatusEffectType.Dark,
            displayName = "黑暗 (Dark)",
            description = "🟣 缓慢暗紫色子弹 | 3秒/发\n命中敌人施加黑暗标记（永久）\n敌人死亡时所有DOT按50%效果传播给周围敌人\n黑暗标记本身不被传播",
            color = new Color(0.4f, 0.1f, 0.6f),
            cooldown = 3.0f,
            impactDmg = 0,
            dotDps = 0f,
            dotDuration = 0f
        },
        new DotGunEntry
        {
            upgradeId = "light",
            effectType = StatusEffectType.Light,
            displayName = "光明 (Light)",
            description = "⚪ 蓄力型定向激光 | 5秒/发\n蓄力3秒后朝鼠标方向发射激光\n顺时针扫45度，帧伤1点/次\n命中施加光明标记：每层受伤+1%，无上限\n敌人身上显示层数文字",
            color = new Color(1f, 1f, 0.9f),
            cooldown = 5.0f,
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
            description = "破甲：DOT敌人护甲-10%\n可无限叠加，越打越疼",
            category = CharacterUpgradeOption.UpgradeCategory.ArmorReduction,
            value1 = 0.10f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "curse",
            upgradeName = "诅咒 (Curse)",
            description = "传染：DOT敌人死亡时\n扩散所有DOT给附近1个敌人\n每层+1目标",
            category = CharacterUpgradeOption.UpgradeCategory.DotSpread,
            value1 = 1f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "agony",
            upgradeName = "痛苦 (Agony)",
            description = "频率：DOT触发间隔-10%\n可无限叠加，总伤不变但节奏更快",
            category = CharacterUpgradeOption.UpgradeCategory.DotFrequency,
            value1 = 0.10f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "wither",
            upgradeName = "凋零 (Wither)",
            description = "暴击：DOT生效时10%几率双倍伤害\n超过100%后暴击倍率+100%",
            category = CharacterUpgradeOption.UpgradeCategory.DotCritBurst,
            value1 = 0.10f, value2 = 0f, value3 = 0f, maxStacks = 0
        },

        // ═══ 引爆增强（2 种）═══
        new UpgradeEntry
        {
            upgradeId = "radiate",
            upgradeName = "辐射 (Radiation)",
            description = "引爆伤害 +30%，可无限叠加",
            category = CharacterUpgradeOption.UpgradeCategory.DetonateMultiplier,
            value1 = 0.30f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "contaminate",
            upgradeName = "污染 (Contaminate)",
            description = "引爆冷却 -30%，可无限叠加",
            category = CharacterUpgradeOption.UpgradeCategory.DetonateAbility,
            value1 = 0.30f, value2 = 0f, value3 = 0f, maxStacks = 0
        },

        // ═══ DOT 时间增强（1 种）═══
        new UpgradeEntry
        {
            upgradeId = "erosion",
            upgradeName = "侵蚀 (Erosion)",
            description = "DOT每生效5次额外冲击\n造成单跳总伤50%瞬间伤害\n每层触发次数-1（最低2次）",
            category = CharacterUpgradeOption.UpgradeCategory.DotTrigger,
            value1 = 1f, value2 = 0.50f, value3 = 0f, maxStacks = 0
        },

        // ═══ 子弹增强（3 种）═══
        new UpgradeEntry
        {
            upgradeId = "haste",
            upgradeName = "急速 (Haste)",
            description = "攻速+15% 子弹速度+10%\n速度超100%获得穿透+1",
            category = CharacterUpgradeOption.UpgradeCategory.AttackSpeed,
            value1 = 0.15f, value2 = 0.10f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "barrage",
            upgradeName = "弹幕 (Barrage)",
            description = "子弹数量+1\n超过5发自动转为追踪弹",
            category = CharacterUpgradeOption.UpgradeCategory.BulletCount,
            value1 = 1f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "ricochet",
            upgradeName = "反弹 (Ricochet)",
            description = "子弹30%几率反弹\n超100%增加反弹次数并移除衰减",
            category = CharacterUpgradeOption.UpgradeCategory.Ricochet,
            value1 = 0.30f, value2 = 0f, value3 = 0f, maxStacks = 0
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

        // 先添加 DOT 子弹枪（4种）
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