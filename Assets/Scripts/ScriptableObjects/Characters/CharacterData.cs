using UnityEngine;

/// <summary>
/// 角色数据 ScriptableObject，定义角色的所有属性和被动技能。
/// 对应 Python: characters.py 中的角色配置
/// 
/// 使用方式：在 Assets/ScriptableObjects/Characters/ 下创建资源
/// </summary>
[CreateAssetMenu(fileName = "NewCharacter", menuName = "VampireGame/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("基础信息")]
    public string characterName = "New Character";
    public string characterId = "new_character";
    [TextArea(2, 4)]
    public string description = "A character";
    public string passiveDescription = "No passive";
    public Sprite icon;
    public Color characterColor = Color.white;

    [Header("基础属性")]
    public int maxHP = 1000;
    public float moveSpeed = 3.5f;
    public int armor = 0;

    [Header("攻击属性")]
    public int attackDamage = 10;
    public float attackCooldown = 0.5f;       // 攻击间隔（秒）
    public float attackRange = 20f;
    public int pierce = 0;                     // 穿透次数
    public float critChance = 0.05f;           // 暴击率 (0~1)
    public float critMultiplier = 0f;          // 额外暴击伤害加成 (0=默认2倍)

    [Header("防御属性")]
    public float dodgeChance = 0f;             // 闪避率 (0~1)
    public int thornsDamage = 0;               // 荆棘伤害
    public float thornsRange = 0f;             // 荆棘范围

    [Header("恢复属性")]
    public float hpRegen = 0f;                 // 每秒回血
    public float lifesteal = 0f;               // 生命偷取 (0~1)

    [Header("特殊属性")]
    public float auraDamage = 0f;              // 光环伤害（每秒）
    public float auraRadius = 0f;              // 光环范围
    public int onKillExplosionDamage = 0;      // 击杀爆炸伤害
    public float onKillExplosionRange = 0f;    // 击杀爆炸范围

    [Header("默认技能")]
    public SkillData defaultSkill;             // 角色初始技能

    [Header("角色专属升级")]
    [Tooltip("如果为空，使用默认升级选项池（攻击/生命/速度/护甲/磁铁）")]
    public CharacterUpgradeOption[] customUpgrades;  // 角色专属升级选项

    [Tooltip("是否使用通用升级（false = 只使用 customUpgrades）")]
    public bool useGenericUpgrades = true;     // true: 通用+专属; false: 只用专属

    /// <summary>
    /// 获取暴击倍率（基础2倍 + 额外加成）
    /// </summary>
    public float GetCritMultiplier()
    {
        return 2f + critMultiplier;
    }

    /// <summary>
    /// 计算暴击伤害
    /// </summary>
    public int CalculateCritDamage(int baseDamage)
    {
        if (Random.value <= critChance)
        {
            return Mathf.RoundToInt(baseDamage * GetCritMultiplier());
        }
        return baseDamage;
    }

    /// <summary>
    /// 是否触发闪避
    /// </summary>
    public bool CheckDodge()
    {
        return Random.value <= dodgeChance;
    }
}