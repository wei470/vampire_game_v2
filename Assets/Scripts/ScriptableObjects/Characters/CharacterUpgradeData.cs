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
        Special             // 特殊能力
    }
}