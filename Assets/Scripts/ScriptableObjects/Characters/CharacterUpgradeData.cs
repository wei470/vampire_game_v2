using UnityEngine;

/// <summary>
/// 角色专属升级选项数据。
/// 定义一个升级选项的名称、描述、图标和效果。
///
/// 使用方式：在 CharacterData 的 customUpgrades 数组中配置
/// </summary>
[System.Serializable]
public class CharacterUpgradeOption
{
    [Header("显示信息")]
    public string upgradeName = "Upgrade";
    public string upgradeId = "upgrade_id";
    [TextArea(2, 3)]
    public string description = "Upgrade description";
    public Sprite icon;
    public Color displayColor = Color.white;

    [Header("升级类型")]
    public UpgradeCategory category = UpgradeCategory.DotDamage;
    public UpgradeRarity rarity = UpgradeRarity.Common;

    [Header("数值参数")]
    public float value1;        // 主要数值（伤害倍率、概率等）
    public float value2;        // 次要数值（持续时间、范围等）
    public float value3;        // 第三数值
    public int maxStacks = 0;   // 最大叠加次数（0=无限）

    /// <summary>
    /// 升级类别枚举（通用分类，各角色可复用）
    /// </summary>
    public enum UpgradeCategory
    {
        DotDamage,          // DOT 伤害增强
        DotDuration,        // DOT 持续时间增强
        DotType,            // 新增 DOT 类型
        DetonateAbility,    // 引爆能力增强
        DetonateMultiplier, // 引爆倍率增强
        StatusEffect,       // 新增状态效果
        DamageMultiplier,   // 伤害倍率
        Survivability,      // 生存能力
        Utility,            // 实用能力
        Special,            // 特殊能力

        // ── Mage 专属扩展 ──
        ArmorReduction,     // 腐蚀 — 拥有DOT的敌人护甲降低
        ArmorPenetration,   // 侵蚀 — 无视敌人护甲
        DotFrequency,       // 痛苦 — DOT触发间隔缩短
        DotCritBurst,       // 凋零 — DOT有几率造成双倍伤害
        DotTrigger,         // 侵蚀 — 每N次DOT生效额外冲击
        AttackSpeed,        // 急速 — 攻速
        BulletCount,        // 弹幕 — 子弹数量增加
        Ricochet,           // 贯穿 — 子弹穿透
        BulletSize,         // 弹体 — 子弹碰撞体积

        // ── 一般强化（全角色通用）──
        MoveSpeed,          // 移速
        ArmorBonus,         // 护甲
        MaxHpBonus,         // 最大HP
        CritChanceBonus,    // 暴击率
        CritDamageBonus,    // 暴击伤害
        MagnetRange,        // 拾取范围
        HpRegen,            // HP回复
        Knockback,          // 击退

        // ── Mage 协同 ──
        LightJudgment,      // 光明审判
        StaticField,        // 静电领域
        FrostExplosion,     // 霜爆
        Penetrate,          // 贯穿弹
        ElementalShield,    // 元素护盾
        Doomsday,           // 末日审判
        EternalAgony,       // 永恒痛苦
        DotOverflow,        // 溢出弹

        // ── 子弹专属强化 ──
        PoisonDuration,     // 毒素持久
        PoisonDps,          // 毒素强化
        PoisonPool,         // 毒液扩散
        PoisonTick,         // 毒素加速
        PoisonCrit,         // 毒素暴击
        PoisonSepsis,       // 脓毒
        PoisonPlague,       // 瘟疫
        PoisonLethal,       // 猛毒

        BurnDuration,       // 火焰持久
        BurnRadius,         // 火场扩大
        BurnTick,           // 火焰加速
        BurnSlow,           // 火场减速
        BurnCrit,           // 燃烧暴击
        BurnMelt,           // 融化精通
        BurnStorm,          // 火焰风暴
        BurnBurst,          // 炎爆

        FrostDuration,      // 霜冻持久
        FrostSlow,          // 减速强化
        FrostTick,          // 霜冻加速
        FrostRange,         // 霜冻范围
        FrostCrit,          // 霜冻暴击
        FrostFreeze,        // 冰封
        FrostBlizzard,      // 暴风雪
        FrostAbsolute,      // 绝对零度

        StaticDamage,       // 雷电强化
        StaticChain,        // 连锁强化
        StaticTick,         // 雷电加速
        StaticRange,        // 雷电范围
        StaticCrit,         // 雷电暴击
        StaticOverload,     // 超载
        StormMulti,         // 雷暴
        StormChain,         // 万雷齐发

        WindDamage,         // 风刃强化
        WindSpeed,          // 风速
        WindTick,           // 风化加速
        WindPierce,         // 风刃穿透
        WindCrit,           // 风暴暴击
        WindStormEye,       // 风暴之眼
        WindHurricane,      // 飓风
        WindLord,           // 风暴领主
    }
}