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
            description = "🟢 药瓶爆炸生成毒液池 | 持续5秒\n叠加层数越高伤害越高 | 射速：慢",
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
            description = "🟠 快速橙色子弹 | DPS:2/s | 持续3秒\n叠加层数加速燃烧频率 | 射速：极快",
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
            description = "🔵 冰霜子弹 | 永久减速30%，每层额外-5%\n最低降至90%减速 | 射速：中",
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
            description = "⚡ 连锁闪电 | 命中敌人后连锁附近最多3个敌人\n被连锁的敌人获得静电层数\n暂停移动0.5秒 | 射速：中快",
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
            description = "🟣 暗紫色子弹 | 命中施加黑暗标记（永久）\n敌人死亡时所有DOT按50%传播 | 射速：极慢",
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
            description = "⚪ 蓄力型定向激光 | 蓄力后朝鼠标发射激光\n顺时针扫45度，帧伤1点/次\n命中施加光明标记：每层受伤+1% | 射速：最慢",
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

        // ═══ P0 新增强化（4 种）═══
        new UpgradeEntry
        {
            upgradeId = "saturation",
            upgradeName = "饱和 (Saturation)",
            description = "同一敌人身上每有1种不同DOT\n所有DOT伤害+5%，可无限叠加",
            category = CharacterUpgradeOption.UpgradeCategory.DotSaturation,
            value1 = 0.05f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "elemental_burst",
            upgradeName = "元素引爆 (Elemental Burst)",
            description = "引爆时，敌人身上每种不同DOT\n额外造成8点固定伤害",
            category = CharacterUpgradeOption.UpgradeCategory.DetonateExtra,
            value1 = 8f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "vampiric_spell",
            upgradeName = "吸血法术 (Vampiric Spell)",
            description = "DOT每次造成伤害时\n回复0.3点生命",
            category = CharacterUpgradeOption.UpgradeCategory.DotLifesteal,
            value1 = 0.3f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "overflow",
            upgradeName = "溢出弹 (Overflow)",
            description = "DOT枪子弹命中已有同类型DOT的敌人时\n额外叠1层DOT，最多叠3层",
            category = CharacterUpgradeOption.UpgradeCategory.DotOverflow,
            value1 = 1f, value2 = 3f, value3 = 0f, maxStacks = 3
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
            upgradeName = "贯穿弹 (Penetrate)",
            description = "子弹穿透敌人\n每级+1穿透数",
            category = CharacterUpgradeOption.UpgradeCategory.Ricochet,
            value1 = 1f, value2 = 0f, value3 = 0f, maxStacks = 3
        },

        // ═══ P1 深度玩法（4 种）═══
        new UpgradeEntry
        {
            upgradeId = "pandemic",
            upgradeName = "蔓延 (Pandemic)",
            description = "DOT传播效率+15%\n最高100%完整传播\n需至少1种DOT子弹",
            category = CharacterUpgradeOption.UpgradeCategory.DotPandemic,
            value1 = 0.15f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "chain_reaction",
            upgradeName = "连锁反应 (Chain Reaction)",
            description = "引爆杀死敌人时\n触发50%伤害的二次引爆\n可叠加增加次数",
            category = CharacterUpgradeOption.UpgradeCategory.ChainReaction,
            value1 = 1f, value2 = 0.5f, value3 = 0f, maxStacks = 3
        },
        new UpgradeEntry
        {
            upgradeId = "dual_wield",
            upgradeName = "双持 (Dual Wield)",
            description = "随机一把已拥有的DOT枪\n射速+15%，单枪上限+60%",
            category = CharacterUpgradeOption.UpgradeCategory.DualWield,
            value1 = 0.15f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "charge_mastery",
            upgradeName = "蓄力精通 (Charge Mastery)",
            description = "引爆蓄力速度+20%\n满蓄力引爆伤害+15%\n蓄力速度上限+60%",
            category = CharacterUpgradeOption.UpgradeCategory.ChargeMastery,
            value1 = 0.20f, value2 = 0.15f, value3 = 0f, maxStacks = 0
        },

        // ═══ P2 协同/趣味（8 种）═══
        new UpgradeEntry
        {
            upgradeId = "resonance",
            upgradeName = "共鸣 (Resonance)",
            description = "DOT触发时10%几率\n不消耗持续时间\n上限50%",
            category = CharacterUpgradeOption.UpgradeCategory.DotResonance,
            value1 = 0.10f, value2 = 0f, value3 = 0f, maxStacks = 5
        },
        new UpgradeEntry
        {
            upgradeId = "toxicology",
            upgradeName = "剧毒天赋 (Toxicology)",
            description = "DOT暴击率额外+8%\n可与凋零叠加",
            category = CharacterUpgradeOption.UpgradeCategory.Toxicology,
            value1 = 0.08f, value2 = 0f, value3 = 0f, maxStacks = 5
        },
        new UpgradeEntry
        {
            upgradeId = "corrupt_touch",
            upgradeName = "腐化之触 (Corrupt Touch)",
            description = "DOT子弹命中时\n施加2秒弱化debuff\n敌人攻击力-10%，上限50%",
            category = CharacterUpgradeOption.UpgradeCategory.CorruptTouch,
            value1 = 0.10f, value2 = 2f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "elemental_storm",
            upgradeName = "元素风暴 (Elemental Storm)",
            description = "同时3种以上DOT活跃时\n每2秒对所有DOT敌人\n造成5点元素伤害",
            category = CharacterUpgradeOption.UpgradeCategory.ElementalStorm,
            value1 = 5f, value2 = 2f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "shadow_link",
            upgradeName = "暗影链接 (Shadow Link)",
            description = "黑暗标记传播范围+1\n传播效率+10%\n范围上限8，效率上限100%",
            category = CharacterUpgradeOption.UpgradeCategory.ShadowLink,
            value1 = 1f, value2 = 0.10f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "light_judgment",
            upgradeName = "光明审判 (Light Judgment)",
            description = "光明标记每层加成\n从+0.5%提升到+0.8%\n每层最高+2.7%",
            category = CharacterUpgradeOption.UpgradeCategory.LightJudgment,
            value1 = 0.003f, value2 = 0f, value3 = 0f, maxStacks = 5
        },
        new UpgradeEntry
        {
            upgradeId = "static_field",
            upgradeName = "静电领域 (Static Field)",
            description = "被静电控制的敌人\n周围1.5范围的其他敌人\n也获得1层静电",
            category = CharacterUpgradeOption.UpgradeCategory.StaticField,
            value1 = 1.5f, value2 = 1f, value3 = 0f, maxStacks = 3
        },
        new UpgradeEntry
        {
            upgradeId = "frost_explosion",
            upgradeName = "霜爆 (Frost Explosion)",
            description = "霜冻减速80%以上的敌人\n引爆时额外造成\n该敌人最大生命3%的冰霜伤害",
            category = CharacterUpgradeOption.UpgradeCategory.FrostExplosion,
            value1 = 0.03f, value2 = 0.80f, value3 = 0f, maxStacks = 0
        },

        // ═══ 子弹增强扩展（3 种）═══
        new UpgradeEntry
        {
            upgradeId = "ammo_mastery",
            upgradeName = "弹药精通 (Ammo Mastery)",
            description = "所有DOT枪子弹速度+20%\n射程+15%",
            category = CharacterUpgradeOption.UpgradeCategory.AmmoMastery,
            value1 = 0.20f, value2 = 0.15f, value3 = 0f, maxStacks = 5
        },
        new UpgradeEntry
        {
            upgradeId = "elemental_affinity",
            upgradeName = "元素亲和 (Elemental Affinity)",
            description = "拥有的每种DOT枪\n为其他DOT枪提供+3%伤害\n7种时每枪+18%",
            category = CharacterUpgradeOption.UpgradeCategory.ElementalAffinity,
            value1 = 0.03f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "penetrate",
            upgradeName = "贯穿弹 (Penetrate)",
            description = "DOT枪子弹穿透+1个敌人\n最多3层",
            category = CharacterUpgradeOption.UpgradeCategory.Penetrate,
            value1 = 1f, value2 = 0f, value3 = 0f, maxStacks = 3
        },

        // ═══ 生存向（4 种）═══
        new UpgradeEntry
        {
            upgradeId = "elemental_shield",
            upgradeName = "元素护盾 (Elemental Shield)",
            description = "每拥有一种DOT枪\n获得+2最大生命\n可无限叠加",
            category = CharacterUpgradeOption.UpgradeCategory.ElementalShield,
            value1 = 2f, value2 = 0f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "phase_shift",
            upgradeName = "相位移动 (Phase Shift)",
            description = "引爆后2秒内\n免疫碰撞伤害\n每层+1秒，最多3层",
            category = CharacterUpgradeOption.UpgradeCategory.PhaseShift,
            value1 = 2f, value2 = 0f, value3 = 0f, maxStacks = 3
        },
        new UpgradeEntry
        {
            upgradeId = "soul_siphon",
            upgradeName = "灵魂虹吸 (Soul Siphon)",
            description = "带DOT的敌人死亡时\n回复1点生命+0.5秒\n30%移速加成",
            category = CharacterUpgradeOption.UpgradeCategory.SoulSiphon,
            value1 = 1f, value2 = 0.5f, value3 = 0.30f, maxStacks = 0
        },

        // ═══ P3 终极/高级（6 种）═══
        new UpgradeEntry
        {
            upgradeId = "ember_boost",
            upgradeName = "余烬强化 (Ember Boost)",
            description = "引爆的余烬伤害+25%\n余烬持续时间+1秒",
            category = CharacterUpgradeOption.UpgradeCategory.EmberBoost,
            value1 = 0.25f, value2 = 1f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "shatter_boost",
            upgradeName = "碎裂强化 (Shatter Boost)",
            description = "引爆的碎裂碎片数量+2\n碎片伤害+15%",
            category = CharacterUpgradeOption.UpgradeCategory.ShatterBoost,
            value1 = 2f, value2 = 0.15f, value3 = 0f, maxStacks = 0
        },
        new UpgradeEntry
        {
            upgradeId = "elemental_master",
            upgradeName = "元素大师 (Elemental Master)",
            description = "需5种以上DOT枪\n所有DOT持续时间+25%\n引爆冷却-20%\n不可重复",
            category = CharacterUpgradeOption.UpgradeCategory.ElementalMaster,
            value1 = 0.25f, value2 = 0.20f, value3 = 0f, maxStacks = 1
        },
        new UpgradeEntry
        {
            upgradeId = "doomsday",
            upgradeName = "末日审判 (Doomsday)",
            description = "引爆时3种以上DOT\n直接秒杀HP低于15%的敌人\n每层+5%阈值，最高30%",
            category = CharacterUpgradeOption.UpgradeCategory.Doomsday,
            value1 = 0.15f, value2 = 0.05f, value3 = 0f, maxStacks = 3
        },
        new UpgradeEntry
        {
            upgradeId = "eternal_agony",
            upgradeName = "永恒痛苦 (Eternal Agony)",
            description = "DOT持续时间×2\n但单次伤害-15%\n不可重复",
            category = CharacterUpgradeOption.UpgradeCategory.EternalAgony,
            value1 = 2f, value2 = 0.15f, value3 = 0f, maxStacks = 1
        },
        new UpgradeEntry
        {
            upgradeId = "annihilation_zone",
            upgradeName = "湮灭领域 (Annihilation Zone)",
            description = "引爆后原地留下\n持续3秒的元素领域\n每秒5点伤害+30%减速",
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