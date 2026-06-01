using UnityEngine;

/// <summary>
/// 被动技能数据 — 升级时可选择的永久加成。
/// 
/// 与主动技能（SkillData）不同，被动技能不需要手动释放，
/// 选择后立即生效并永久叠加。
/// 
/// 使用方式：在 LevelUpUI 中作为升级选项出现
/// </summary>
[CreateAssetMenu(fileName = "NewPassiveSkill", menuName = "VampireGame/Passive Skill")]
public class PassiveSkillData : ScriptableObject
{
    [Header("基础信息")]
    public string skillName = "New Passive";
    public string description = "A passive skill";
    public Sprite icon;

    [Header("加成类型")]
    public PassiveType passiveType = PassiveType.MaxHpUp;
    public float bonusValue = 0.1f; // 加成数值（百分比或固定值）

    /// <summary>
    /// 被动技能类型
    /// </summary>
    public enum PassiveType
    {
        MaxHpUp,          // +最大HP
        ArmorUp,          // +护甲
        MoveSpeedUp,      // +移动速度
        AttackDamageUp,   // +攻击力
        CritChanceUp,     // +暴击率
        CritDamageUp,     // +暴击伤害
        CooldownReduction, // +冷却缩减
        AttackRangeUp,    // +攻击范围
        MagnetRangeUp,    // +拾取范围
        LifestealUp       // +吸血
    }

    /// <summary>
    /// 应用被动技能到玩家
    /// </summary>
    public void Apply(PlayerController player)
    {
        if (player == null) return;

        var dmg = player.Damageable;
        if (dmg == null) return;

        switch (passiveType)
        {
            case PassiveType.MaxHpUp:
                int newMaxHp = Mathf.RoundToInt(dmg.MaxHp * (1f + bonusValue));
                dmg.SetMaxHp(newMaxHp);
                dmg.Heal(Mathf.RoundToInt(dmg.MaxHp * bonusValue));
                break;

            case PassiveType.ArmorUp:
                dmg.SetArmor(dmg.Armor + Mathf.RoundToInt(bonusValue));
                break;

            case PassiveType.MoveSpeedUp:
                player.MoveSpeed *= (1f + bonusValue);
                break;

            case PassiveType.AttackDamageUp:
                var wc = player.GetComponent<WeaponController>();
                if (wc != null) wc.DamageMultiplier *= (1f + bonusValue);
                break;

            case PassiveType.CritChanceUp:
            case PassiveType.CritDamageUp:
            case PassiveType.CooldownReduction:
            case PassiveType.AttackRangeUp:
            case PassiveType.MagnetRangeUp:
            case PassiveType.LifestealUp:
                // 这些通过 SaveManager 永久加成系统生效
                if (SaveManager.Instance != null)
                {
                    string key = passiveType.ToString().ToLower();
                    SaveManager.Instance.AddPermanentBonus(key, bonusValue);
                }
                break;
        }

        DebugHelper.Log($"[PassiveSkill] Applied {skillName} ({passiveType} +{bonusValue:P0})");
    }

    /// <summary>
    /// 获取被动技能的显示描述
    /// </summary>
    public string GetDisplayDescription()
    {
        switch (passiveType)
        {
            case PassiveType.MaxHpUp:          return $"+{bonusValue:P0} 最大HP";
            case PassiveType.ArmorUp:           return $"+{bonusValue:F0} 护甲";
            case PassiveType.MoveSpeedUp:       return $"+{bonusValue:P0} 移动速度";
            case PassiveType.AttackDamageUp:    return $"+{bonusValue:P0} 攻击力";
            case PassiveType.CritChanceUp:      return $"+{bonusValue:P0} 暴击率";
            case PassiveType.CritDamageUp:      return $"+{bonusValue:P0} 暴击伤害";
            case PassiveType.CooldownReduction: return $"-{bonusValue:P0} 冷却时间";
            case PassiveType.AttackRangeUp:     return $"+{bonusValue:P0} 攻击范围";
            case PassiveType.MagnetRangeUp:     return $"+{bonusValue:P0} 拾取范围";
            case PassiveType.LifestealUp:       return $"+{bonusValue:P0} 吸血";
            default: return description;
        }
    }
}