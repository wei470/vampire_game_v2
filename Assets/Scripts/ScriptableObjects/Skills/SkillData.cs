using UnityEngine;

/// <summary>
/// 技能数据 ScriptableObject，定义技能的所有属性。
/// 对应 Python: skills 中的技能配置
/// 
/// 使用方式：在 Assets/ScriptableObjects/Skills/ 下创建资源
/// </summary>
[CreateAssetMenu(fileName = "NewSkill", menuName = "VampireGame/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("基础信息")]
    public string skillName = "New Skill";
    public string description = "A skill";
    public Sprite icon;
    public int skillId;

    [Header("技能类型")]
    public SkillType skillType = SkillType.Active;
    public SkillTarget targetType = SkillTarget.Self;

    [Header("冷却")]
    public float cooldown = 10f;              // 冷却时间（秒）

    [Header("持续时间")]
    public float duration = 0f;               // 技能持续时间（0=瞬发）

    [Header("伤害/效果")]
    public int baseDamage = 0;                // 基础伤害
    public float effectRadius = 3f;           // 效果范围
    public float effectStrength = 1f;         // 效果强度（buff倍率、减速比例等）
    public float effectDuration = 3f;         // 效果持续时间（buff/debuff）

    [Header("升级")]
    public int maxLevel = 5;                  // 最大等级
    public float damagePerLevel = 2f;         // 每级伤害增加
    public float cooldownReductionPerLevel = 0.5f; // 每级冷却减少
    public float effectPerLevel = 0.1f;       // 每级效果增强

    [Header("视觉")]
    public Color skillColor = Color.white;
    public float visualScale = 1f;

    /// <summary>
    /// 技能类型枚举
    /// </summary>
    public enum SkillType
    {
        Active,     // 主动技能（按 E 使用）
        Passive     // 被动技能（永久生效）
    }

    /// <summary>
    /// 技能目标类型
    /// </summary>
    public enum SkillTarget
    {
        Self,           // 对自身
        Area,           // 范围（以玩家为中心）
        Directional,    // 方向性（朝鼠标或移动方向）
        Random          // 随机目标
    }

    /// <summary>
    /// 获取指定等级的伤害值
    /// </summary>
    public int GetDamageAtLevel(int level)
    {
        return Mathf.RoundToInt(baseDamage + damagePerLevel * (level - 1));
    }

    /// <summary>
    /// 获取指定等级的冷却时间
    /// </summary>
    public float GetCooldownAtLevel(int level)
    {
        return Mathf.Max(1f, cooldown - cooldownReductionPerLevel * (level - 1));
    }

    /// <summary>
    /// 获取指定等级的效果强度
    /// </summary>
    public float GetEffectAtLevel(int level)
    {
        return effectStrength + effectPerLevel * (level - 1);
    }
}