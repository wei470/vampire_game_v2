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
        DotSpread,          // 诅咒 — DOT敌人死亡时传播DOT
        DotFrequency,       // 痛苦 — DOT触发间隔缩短
        DotCritBurst,       // 凋零 — DOT有几率造成双倍伤害
        DotTrigger,         // 侵蚀 — 每N次DOT生效额外冲击
        WindVortex,         // 风蚀 — DOT敌人移动时生成漩涡
        AttackSpeed,        // 急速 — 攻速+子弹速度
        BulletCount,        // 弹幕 — 子弹数量增加
        Ricochet,           // 反弹 — 子弹反弹几率
        BulletSize          // 共振 — 子弹碰撞体积+击退
    }
}