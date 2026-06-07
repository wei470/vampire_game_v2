using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DOT 元素融合系统 — 当玩家同时拥有 2 种特定 DOT 子弹时，可触发融合进化。
///
/// 融合规则：
///   - 拥有两种特定 DOT 后，融合选项出现在升级选项中
///   - 选择融合后，替换两种原始 DOT 为融合子弹
///   - 融合子弹继承两种 DOT 的效果 + 额外行为
///
/// 使用方式：由 MagePassive 调用 CheckFusions() / ApplyFusion()
/// </summary>
public static class DotFusionSystem
{
    /// <summary>
    /// 融合定义
    /// </summary>
    public class FusionDef
    {
        public string fusionId;          // 融合子弹 ID
        public string displayName;       // 显示名称
        public string description;       // 描述
        public StatusEffectType required1; // 需要的第一种 DOT
        public StatusEffectType required2; // 需要的第二种 DOT
        public Color fusionColor;        // 融合子弹颜色
        public float cooldown;           // 射速
        public int impactDmg;            // 命中伤害
        public float dotDps;             // DOT DPS
        public float dotDuration;        // DOT 持续时间
    }

    /// <summary>
    /// 所有融合配方
    /// </summary>
    public static readonly List<FusionDef> AllFusions = new List<FusionDef>
    {
        // 流血 + 燃烧 → 熔岩弹
        new FusionDef
        {
            fusionId = "fusion_lava",
            displayName = "🌋 熔岩弹",
            description = "子弹变为AOE，命中留岩浆池，持续灼烧",
            required1 = StatusEffectType.Bleed,
            required2 = StatusEffectType.Burn,
            fusionColor = new Color(1f, 0.4f, 0.1f),
            cooldown = 0.8f,
            impactDmg = 3,
            dotDps = 4f,
            dotDuration = 6f
        },
        // 中毒 + 霜冻 → 毒冰弹
        new FusionDef
        {
            fusionId = "fusion_frost_poison",
            displayName = "💎 毒冰弹",
            description = "子弹穿透所有敌人，留下冰毒轨迹",
            required1 = StatusEffectType.Poison,
            required2 = StatusEffectType.Frostbite,
            fusionColor = new Color(0.3f, 0.8f, 0.7f),
            cooldown = 0.7f,
            impactDmg = 0,
            dotDps = 3f,
            dotDuration = 8f
        },
        // 燃烧 + 雷电 → 等离子弹
        new FusionDef
        {
            fusionId = "fusion_plasma",
            displayName = "⚡ 等离子弹",
            description = "子弹命中后分裂为3道等离子束",
            required1 = StatusEffectType.Burn,
            required2 = StatusEffectType.Static,
            fusionColor = new Color(0.5f, 0.8f, 1f),
            cooldown = 0.6f,
            impactDmg = 5,
            dotDps = 3f,
            dotDuration = 4f
        },
        // 霜冻 + 雷电 → 电磁弹
        new FusionDef
        {
            fusionId = "fusion_electromagnetic",
            displayName = "🔋 电磁弹",
            description = "命中后产生电磁场，持续吸引+伤害周围敌人",
            required1 = StatusEffectType.Frostbite,
            required2 = StatusEffectType.Static,
            fusionColor = new Color(0.3f, 0.5f, 1f),
            cooldown = 1f,
            impactDmg = 2,
            dotDps = 2f,
            dotDuration = 5f
        },
        // 流血 + 中毒 → 腐蚀弹
        new FusionDef
        {
            fusionId = "fusion_corrosive",
            displayName = "☣️ 腐蚀弹",
            description = "降低敌人护甲，叠加越多降越多",
            required1 = StatusEffectType.Bleed,
            required2 = StatusEffectType.Poison,
            fusionColor = new Color(0.2f, 0.7f, 0.3f),
            cooldown = 0.9f,
            impactDmg = 1,
            dotDps = 4f,
            dotDuration = 7f
        }
    };

    /// <summary>
    /// 检查当前拥有的 DOT 子弹中，有哪些融合可以触发
    /// </summary>
    public static List<FusionDef> GetAvailableFusions(List<MagePassive.DotGunState> dotGuns, HashSet<string> completedFusions)
    {
        var result = new List<FusionDef>();
        var ownedTypes = new HashSet<StatusEffectType>();
        foreach (var gun in dotGuns)
            ownedTypes.Add(gun.effectType);

        foreach (var fusion in AllFusions)
        {
            // 跳过已完成的融合
            if (completedFusions.Contains(fusion.fusionId)) continue;

            // 检查是否同时拥有两种所需 DOT
            if (ownedTypes.Contains(fusion.required1) && ownedTypes.Contains(fusion.required2))
            {
                result.Add(fusion);
            }
        }

        return result;
    }

    /// <summary>
    /// 获取融合的两种原始 DOT 的颜色混合（用于 UI 显示）
    /// </summary>
    public static Color GetFusionBlendColor(FusionDef fusion, List<MagePassive.DotGunState> dotGuns)
    {
        Color c1 = Color.white, c2 = Color.white;
        foreach (var gun in dotGuns)
        {
            if (gun.effectType == fusion.required1) c1 = gun.color;
            if (gun.effectType == fusion.required2) c2 = gun.color;
        }
        return Color.Lerp(c1, c2, 0.5f);
    }
}