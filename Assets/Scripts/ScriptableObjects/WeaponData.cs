using UnityEngine;

/// <summary>
/// 武器数据 ScriptableObject，定义武器的所有属性。
/// 对应 Python: WEAPON_REGISTRY 中的武器配置
/// 
/// 使用方式：在 Assets/ScriptableObjects/Weapons/ 下创建资源
/// </summary>
[CreateAssetMenu(fileName = "NewWeapon", menuName = "VampireGame/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("基础信息")]
    public string weaponName = "New Weapon";
    public string description = "A weapon";
    public Sprite icon;
    public int weaponId;

    [Header("投射物类型")]
    public ProjectileType projectileType = ProjectileType.Bullet;

    [Header("攻击属性")]
    public int baseDamage = 10;
    public float cooldown = 0.5f;           // 攻击间隔（秒）
    public float projectileSpeed = 12f;
    public float projectileLifetime = 3f;
    public int pierce = 1;                   // 穿透次数
    public float knockbackForce = 2f;
    public float attackRange = 20f;

    [Header("特殊属性")]
    public int projectileCount = 1;          // 每次发射数量
    public float spreadAngle = 0f;           // 散射角度
    public float chainRadius = 5f;           // 链式跳跃半径
    public int chainCount = 3;               // 链式跳跃次数
    public float homingTurnSpeed = 5f;       // 追踪转向速度
    public float aoeRadius = 0f;             // AoE 半径
    public float zoneLifetime = 3f;          // 区域持续时间
    public float zoneRadius = 1.5f;          // 区域半径
    public float tickInterval = 0.5f;        // 区域伤害间隔
    public int mineCount = 3;                // 地雷数量
    public float slowAmount = 0f;            // 减速比例
    public float slowDuration = 0f;          // 减速持续时间
    public float dotDamage = 0f;             // 持续伤害
    public float dotDuration = 0f;           // 持续伤害时间
    public float frostRadius = 0f;           // 冰冻半径

    [Header("武器升级")]
    [SerializeField] private int _upgradeLevel = 0;

    /// <summary>
    /// 当前升级等级（0=基础, 1-4=强化等级）
    /// </summary>
    public int UpgradeLevel => _upgradeLevel;

    /// <summary>
    /// 最大升级等级
    /// </summary>
    public const int MAX_UPGRADE_LEVEL = 4;

    /// <summary>
    /// 武器升级类型枚举
    /// </summary>
    public enum WeaponUpgradeType
    {
        DamageUp,     // +25% 伤害
        PierceUp,     // +1 穿透
        CooldownDown, // -15% 冷却时间
        RangeUp       // +30% 攻击范围
    }

    /// <summary>
    /// 升级武器属性
    /// </summary>
    public bool ApplyUpgrade(WeaponUpgradeType upgradeType)
    {
        if (_upgradeLevel >= MAX_UPGRADE_LEVEL) return false;

        _upgradeLevel++;
        switch (upgradeType)
        {
            case WeaponUpgradeType.DamageUp:
                baseDamage = Mathf.RoundToInt(baseDamage * 1.25f);
                break;
            case WeaponUpgradeType.PierceUp:
                pierce += 1;
                break;
            case WeaponUpgradeType.CooldownDown:
                cooldown *= 0.85f;
                break;
            case WeaponUpgradeType.RangeUp:
                attackRange *= 1.3f;
                projectileSpeed *= 1.15f;
                break;
        }

        // 升级时颜色略微变亮
        projectileColor = Color.Lerp(projectileColor, Color.white, 0.1f);
        return true;
    }

    /// <summary>
    /// 获取当前等级的升级描述
    /// </summary>
    public string GetUpgradeDescription(WeaponUpgradeType type)
    {
        switch (type)
        {
            case WeaponUpgradeType.DamageUp:
                return $"+25% 伤害 ({baseDamage} → {Mathf.RoundToInt(baseDamage * 1.25f)})";
            case WeaponUpgradeType.PierceUp:
                return $"+1 穿透 ({pierce} → {pierce + 1})";
            case WeaponUpgradeType.CooldownDown:
                return $"-15% 冷却 ({cooldown:F2}s → {cooldown * 0.85f:F2}s)";
            case WeaponUpgradeType.RangeUp:
                return $"+30% 范围 & +15% 弹速";
            default:
                return "未知升级";
        }
    }

    [Header("视觉效果")]
    public Color projectileColor = Color.white;
    public float projectileScale = 1f;

    /// <summary>
    /// 投射物类型枚举
    /// </summary>
    public enum ProjectileType
    {
        Bullet,          // 基础子弹 - 直线飞行
        ChainLightning,  // 链式闪电 - 命中后跳跃
        Shockwave,       // 冲击波 - 圆形扩散
        HomingMissile,   // 追踪导弹 - 追踪敌人
        MineTrap,        // 地雷 - 落地后待机触发
        Flamethrower,    // 火焰喷射 - 创建火焰区域
        FrostOrb,        // 冰霜球 - 减速区域
        VenomDart        // 毒镖 - 持续伤害
    }
}