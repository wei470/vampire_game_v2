using UnityEngine;

/// <summary>
/// 敌人生成与属性配置 — 统一管理所有敌人属性、波次生成、LOD、精英修饰器等参数。
/// 替代之前散落在 SpawnManager、EnemyBase、EliteModifierSystem 等文件中的硬编码数值。
///
/// 使用方式：
/// 1. 在 Project 中右键 → Create → Config → Enemy Spawn Config 创建实例
/// 2. 放置到 Resources/Configs/ 目录下
/// 3. 运行时通过 EnemySpawnConfig.GetDefault() 访问
/// </summary>
[CreateAssetMenu(fileName = "EnemySpawnConfig", menuName = "Config/Enemy Spawn Config")]
public class EnemySpawnConfig : ScriptableObject
{
    // ══════════════════════════════════════════════════════════════
    // 通用敌人基础属性
    // 所有敌人类型共用的基础默认值，各敌人子类可在预制体上覆盖。
    // ══════════════════════════════════════════════════════════════
    [Header("通用敌人基础属性")]
    [Tooltip("敌人的默认移动速度（单位：Unity 场景单位/秒）。\n" +
             "各敌人子类可在预制体上通过 SerializeField 覆盖此值。\n" +
             "默认值：0.75")]
    public float defaultMoveSpeed = 0.75f;

    [Tooltip("敌人接触玩家时的默认碰撞伤害（整数）。\n" +
             "默认值：10")]
    public int defaultContactDamage = 10;

    [Tooltip("敌人碰撞攻击的冷却时间（单位：秒）。\n" +
             "连续碰撞伤害的最小间隔。\n" +
             "默认值：1")]
    public float defaultAttackCooldown = 1f;

    [Tooltip("敌人与玩家超过此距离自动销毁/回收（单位：Unity 场景单位）。\n" +
             "默认值：50")]
    public float despawnDistance = 50f;

    // ══════════════════════════════════════════════════════════════
    // 波次生成参数
    // 控制每波敌人的数量、生成间隔和生成半径。
    // ══════════════════════════════════════════════════════════════
    [Header("波次生成参数")]
    [Tooltip("第 1 波的敌人基础数量（整数）。\n" +
             "公式：当前波总数 = baseEnemyCount + (波次-1) × enemiesPerWave\n" +
             "默认值：3")]
    public int baseEnemyCount = 3;

    [Tooltip("每波次额外增加的敌人数量（整数/波次）。\n" +
             "默认值：2")]
    public int enemiesPerWave = 2;

    [Tooltip("同一波内敌人之间的生成间隔（单位：秒）。\n" +
             "数值越小，敌人生成越密集。\n" +
             "默认值：0.5")]
    public float spawnInterval = 0.5f;

    [Tooltip("敌人在屏幕边缘外的生成半径（单位：Unity 场景单位）。\n" +
             "敌人环绕玩家在此距离处生成。\n" +
             "默认值：15")]
    public float spawnRadius = 15f;

    // ══════════════════════════════════════════════════════════════
    // 难度倍率（每波递增）
    // 敌人 HP 和伤害随波次递增的缩放因子。
    // ══════════════════════════════════════════════════════════════
    [Header("难度倍率（每波递增）")]
    [Tooltip("每波敌人伤害的递增百分比（小数形式）。\n" +
             "敌人伤害 = 基础伤害 × (1 + 当前波次 × damageScalingPerWave)\n" +
             "默认值：0.1（每波+10%）")]
    public float damageScalingPerWave = 0.1f;

    [Tooltip("每波敌人生命值的递增百分比（小数形式）。\n" +
             "默认值：0.15（每波+15%）")]
    public float hpScalingPerWave = 0.15f;

    [Tooltip("每波敌人移动速度的递增百分比（小数形式）。\n" +
             "默认值：0.03（每波+3%）")]
    public float speedScalingPerWave = 0.03f;

    // ══════════════════════════════════════════════════════════════
    // Boss 配置
    // ══════════════════════════════════════════════════════════════
    [Header("Boss 配置")]
    [Tooltip("Boss 出现的波次间隔。\n" +
             "例如 5 表示每 5 波出现一个 Boss。\n" +
             "默认值：5")]
    public int bossWaveInterval = 5;

    [Tooltip("Boss 的基础生命值。\n" +
             "实际HP = bossBaseHP + 波次 × bossHPPerWave\n" +
             "默认值：300")]
    public int bossBaseHP = 300;

    [Tooltip("Boss 每波次额外增加的生命值。\n" +
             "默认值：100")]
    public int bossHPPerWave = 100;

    [Tooltip("Boss 波次中伴随 Boss 的小兵数量。\n" +
             "默认值：3")]
    public int bossMinionsPerWave = 3;

    // ══════════════════════════════════════════════════════════════
    // LOD 距离分级系统
    // 根据敌人与玩家的距离降低 AI 更新频率，优化性能。
    // ══════════════════════════════════════════════════════════════
    [Header("LOD 距离分级系统")]
    [Tooltip("近距离范围上限（单位：场景单位，实际比较用平方值）。\n" +
             "在此范围内的敌人每帧更新 AI。\n" +
             "默认值：15（平方 = 225）")]
    public float lodNearRange = 15f;

    [Tooltip("中距离范围上限（单位：场景单位）。\n" +
             "在此范围内的敌人每 N 帧更新 AI（N=lodMediumInterval）。\n" +
             "默认值：30（平方 = 900）")]
    public float lodMediumRange = 30f;

    [Tooltip("中距离敌人 AI 更新间隔（每 N 帧更新一次）。\n" +
             "默认值：3")]
    public int lodMediumInterval = 3;

    [Tooltip("远距离敌人 AI 更新间隔（每 N 帧更新一次）。\n" +
             "远距离敌人同时跳过特殊能力。\n" +
             "默认值：10")]
    public int lodFarInterval = 10;

    // ══════════════════════════════════════════════════════════════
    // 精英修饰器参数
    // 精英敌人在特定波次后出现，拥有额外属性和特殊能力。
    // ══════════════════════════════════════════════════════════════
    [Header("精英修饰器参数")]
    [Tooltip("精英敌人开始出现的最低波次。\n" +
             "默认值：5")]
    public int eliteMinWave = 5;

    [Tooltip("每波触发精英修饰器的概率（0~1）。\n" +
             "默认值：0.3（30%）")]
    [Range(0f, 1f)]
    public float eliteChance = 0.3f;

    [Tooltip("精英敌人 HP 倍率。\n" +
             "默认值：2.0（双倍血量）")]
    public float eliteHpMultiplier = 2.0f;

    [Tooltip("精英敌人伤害倍率。\n" +
             "默认值：1.5")]
    public float eliteDamageMultiplier = 1.5f;

    // ── 精英子能力：再生 ──
    [Tooltip("再生精英的回血间隔（单位：秒）。\n" +
             "默认值：1")]
    public float eliteRegenInterval = 1f;

    [Tooltip("再生精英每次回血的百分比（占最大HP）。\n" +
             "默认值：0.02（2%）")]
    public float eliteRegenPercent = 0.02f;

    // ── 精英子能力：护盾 ──
    [Tooltip("护盾精英的护盾刷新间隔（单位：秒）。\n" +
             "默认值：5")]
    public float eliteShieldInterval = 5f;

    [Tooltip("护盾精英每次生成的护盾值。\n" +
             "默认值：50")]
    public float eliteShieldAmount = 50f;

    // ── 精英子能力：隐身 ──
    [Tooltip("隐身精英的隐身周期（单位：秒）。\n" +
             "默认值：8")]
    public float eliteInvisCycle = 8f;

    [Tooltip("隐身精英的隐身持续时间（单位：秒）。\n" +
             "默认值：3")]
    public float eliteInvisDuration = 3f;

    // ── 精英子能力：召唤 ──
    [Tooltip("召唤精英的召唤间隔（单位：秒）。\n" +
             "默认值：10")]
    public float eliteSummonInterval = 10f;

    // ── 精英子能力：急速光环 ──
    [Tooltip("急速光环精英的光环刷新间隔（单位：秒）。\n" +
             "默认值：0.5")]
    public float eliteHasteAuraInterval = 0.5f;

    [Tooltip("急速光环的影响半径（单位：场景单位）。\n" +
             "默认值：5")]
    public float eliteHasteAuraRadius = 5f;

    [Tooltip("急速光环的速度加成百分比。\n" +
             "默认值：0.3（+30%）")]
    public float eliteHasteAuraBonus = 0.3f;

    // ── 精英子能力：狂暴 ──
    [Tooltip("狂暴精英触发的 HP 阈值（占最大HP百分比）。\n" +
             "低于此值时进入狂暴状态。\n" +
             "默认值：0.3（30%）")]
    public float eliteBerserkHpThreshold = 0.3f;

    [Tooltip("狂暴状态的伤害倍率。\n" +
             "默认值：2.0（双倍伤害）")]
    public float eliteBerserkDamageMult = 2.0f;

    // ── 精英子能力：荆棘 ──
    [Tooltip("荆棘精英的反伤百分比。\n" +
             "受击时反弹此比例的伤害给攻击者。\n" +
             "默认值：0.3（30%）")]
    public float eliteThornsReflectPercent = 0.3f;

    // ── 精英子能力：分裂 ──
    [Tooltip("分裂精英死亡时分裂出的 HP 百分比。\n" +
             "默认值：0.5（50%）")]
    public float eliteSplitHpPercent = 0.5f;

    // ══════════════════════════════════════════════════════════════
    // 死亡特效参数
    // ══════════════════════════════════════════════════════════════
    [Header("死亡特效参数")]
    [Tooltip("敌人死亡时的缩小动画持续时间（单位：秒）。\n" +
             "默认值：0.2")]
    public float deathShrinkDuration = 0.2f;

    [Tooltip("Boss 死亡时的慢动作持续时间（单位：秒）。\n" +
             "默认值：0.5")]
    public float bossSlowmoDuration = 0.5f;

    [Tooltip("Boss 死亡时的慢动作时间缩放（0~1）。\n" +
             "0.3 表示时间流速变为正常的 30%。\n" +
             "默认值：0.3")]
    public float bossSlowmoScale = 0.3f;

    // ══════════════════════════════════════════════════════════════
    // 特殊波次配置
    // ══════════════════════════════════════════════════════════════
    [Header("特殊波次配置")]
    [Tooltip("是否启用特殊波次事件（布尔开关）。\n" +
             "默认值：开启")]
    public bool enableSpecialWaves = true;

    [Tooltip("特殊波次开始出现的最低波次。\n" +
             "默认值：10")]
    public int specialWaveMinStart = 10;

    [Tooltip("每波触发特殊波次的概率（0~1）。\n" +
             "默认值：0.2（20%）")]
    [Range(0f, 1f)]
    public float specialWaveChance = 0.2f;

    // ── 变更通知 ──
    /// <summary>Inspector 修改值时触发，各系统订阅此事件刷新缓存</summary>
    public static System.Action OnConfigChanged;

#if UNITY_EDITOR
    private void OnValidate()
    {
        OnConfigChanged?.Invoke();
    }
#endif

    // ── 单例访问 ──
    private static EnemySpawnConfig _instance;

    /// <summary>
    /// 获取全局配置实例（从 Resources 加载）
    /// </summary>
    public static EnemySpawnConfig GetDefault()
    {
        if (_instance == null)
        {
            _instance = Resources.Load<EnemySpawnConfig>("Configs/EnemySpawnConfig");
            if (_instance == null)
            {
                _instance = CreateInstance<EnemySpawnConfig>();
                DebugHelper.LogWarning("[EnemySpawnConfig] 未找到 Resources/Configs/EnemySpawnConfig.asset，使用默认值");
            }
        }
        return _instance;
    }
}