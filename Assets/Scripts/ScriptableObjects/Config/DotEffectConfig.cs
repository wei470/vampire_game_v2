using UnityEngine;

/// <summary>
/// P2-1: DOT 子弹数据驱动配置 — 统一管理所有 DOT 子弹的参数
/// 替代之前在各子弹类中硬编码的速度/伤害/持续时间等数值
/// 
/// 使用方式：
/// 1. 在 Project 中右键 → Create → Mage → Dot Bullet Config 创建实例
/// 2. 通过 GameConfig 或 MageUpgradeConfig 引用此配置
/// 3. 各子弹 Create() 方法从此配置读取参数
/// </summary>
[CreateAssetMenu(fileName = "DotEffectConfig", menuName = "Configs/Dot Effect Config")]
public class DotEffectConfig : ScriptableObject
{
    // ══════════════════════════════════════════════════════════════
    // 通用子弹参数
    // ══════════════════════════════════════════════════════════════
    [Header("通用子弹参数")]
    [Tooltip("弹幕散射角度（单位：度）。\n" +
             "多发子弹时每发之间的角度间隔。\n" +
             "默认值：15")]
    public float BarrageSpreadAngle = 15f;

    // ══════════════════════════════════════════════════════════════
    // 燃烧火场（BurnFireZone）
    // 射出一个缓慢移动的大型火场，处于火场内的敌人每 0.5 秒受到一次叠层。
    // 与风化效果触发"燃烧扩散"元素反应，与霜冻触发"融化"。
    // ══════════════════════════════════════════════════════════════
    [Header("燃烧火场（BurnFireZone）")]
    [Tooltip("火场子弹的飞行速度（缓慢移动）。默认值：4")]
    public float BurnSpeed = 4f;

    [Tooltip("火场子弹自身在场景中的存活时间。默认值：8")]
    public float BurnLifetime = 8f;

    [Tooltip("燃烧效果的基础每秒伤害（DPS）。默认值：2")]
    public float BurnBaseDps = 2f;

    [Tooltip("燃烧状态效果的持续时间。默认值：3")]
    public float BurnDuration = 3f;

    [Tooltip("火场对区域内敌人的叠层间隔。默认值：0.5")]
    public float BurnBaseTickInterval = 0.5f;

    [Tooltip("火场的碰撞半径。默认值：2.5")]
    public float BurnFireZoneRadius = 2.5f;

    [Tooltip("火场子弹的缩放。默认值：2.0")]
    public float BurnFireZoneScale = 2.0f;

    // ══════════════════════════════════════════════════════════════
    // 毒液子弹（PoisonBullet）
    // 无限射程的特殊子弹，命中后在地面留下毒液池，
    // 对经过的敌人造成持续中毒伤害。
    // ══════════════════════════════════════════════════════════════
    [Header("毒液子弹（PoisonBullet）")]
    [Tooltip("毒液子弹的飞行速度（单位：Unity 场景单位/秒）。\n" +
             "毒液子弹具有无限射程，不会因距离自动销毁。\n" +
             "默认值：14")]
    public float PoisonSpeed = 14f;

    [Tooltip("毒液子弹命中时的爆炸半径（单位：Unity 场景单位）。\n" +
             "命中后在此范围内产生毒液溅射效果。\n" +
             "默认值：0.5")]
    public float PoisonExplosionRadius = 0.5f;

    [Tooltip("毒液池的半径（单位：Unity 场景单位）。\n" +
             "子弹命中地面后生成的毒液区域大小。\n" +
             "默认值：1.5")]
    public float PoisonPuddleRadius = 1.5f;

    [Tooltip("毒液池的持续时间（单位：秒）。\n" +
             "毒液池在此时间内对经过的敌人持续造成中毒伤害。\n" +
             "默认值：5")]
    public float PoisonPuddleDuration = 5f;

    [Tooltip("毒液池的基础 tick 间隔（单位：秒）。\n" +
             "每经过此时间对区域内敌人造成一次伤害。\n" +
             "默认值：1")]
    public float PoisonPuddleTickInterval = 1f;

    [Tooltip("毒液池 tick 间隔的衰减系数（0~1）。\n" +
             "每次 tick 后间隔乘以此系数，使毒液池越到后面伤害越密集。\n" +
             "默认值：0.9")]
    public float PoisonPuddleTickDecay = 0.9f;

    [Tooltip("毒液池 tick 间隔的最小值（单位：秒）。\n" +
             "间隔不会衰减到低于此值。\n" +
             "默认值：0.2")]
    public float PoisonPuddleMinTickInterval = 0.2f;

    // ══════════════════════════════════════════════════════════════
    // 中毒叠加效果（PoisonStackEffect）
    // 每 tick 造成固定伤害，每层 +1 伤害，tick 间隔可衰减加速。
    // ══════════════════════════════════════════════════════════════
    [Tooltip("中毒效果的基础 tick 间隔（单位：秒）。\n" +
             "每经过此时间对敌人造成一次中毒伤害。\n" +
             "默认值：1")]
    public float PoisonBaseTickInterval = 1f;

    [Tooltip("中毒 tick 间隔的衰减系数（0~1）。\n" +
             "每次 tick 后间隔乘以此系数，使中毒越到后面伤害越密集。\n" +
             "默认值：0.9")]
    public float PoisonTickDecay = 0.9f;

    [Tooltip("中毒 tick 间隔的最小值（单位：秒）。\n" +
             "间隔不会衰减到低于此值。\n" +
             "默认值：0.2")]
    public float PoisonMinTickInterval = 0.2f;

    [Tooltip("中毒效果每次 tick 的基础伤害值（整数）。\n" +
             "实际伤害 = PoisonDamagePerTick + (层数 - 1)，每层 +1。\n" +
             "默认值：2")]
    public int PoisonDamagePerTick = 2;

    [Tooltip("中毒效果的最大叠加层数（整数）。\n" +
             "层数超过此值后不再增加。\n" +
             "默认值：20")]
    public int PoisonMaxStacks = 20;

    // ══════════════════════════════════════════════════════════════
    // 霜冻子弹（FrostBullet）
    // 命中后施加永久减速效果：基础 30%，每层额外 +5%，上限 90%。
    // 与静电效果触发"霜电冰场"元素反应（范围冰冻区域）。
    // ══════════════════════════════════════════════════════════════
    [Header("霜冻子弹（FrostBullet）")]
    [Tooltip("霜冻子弹的飞行速度（单位：Unity 场景单位/秒）。\n" +
             "霜冻子弹速度较快，有利于快速命中多个敌人。\n" +
             "默认值：20")]
    public float FrostSpeed = 20f;

    [Tooltip("每层霜冻效果额外增加的减速百分比（小数形式）。\n" +
             "例如 0.05 表示每层 +5% 减速。\n" +
             "减速效果可以无限叠加，但总减速不超过上限 90%。\n" +
             "默认值：0.05（即 5%）")]
    public float FrostSlowPerStack = 0.05f;

    [Tooltip("首次命中敌人时施加的基础减速百分比（小数形式）。\n" +
             "例如 0.3 表示首次命中即减速 30%。\n" +
             "后续每层在基础上叠加 FrostSlowPerStack。\n" +
             "默认值：0.3（即 30%）")]
    public float FrostBaseSlowPct = 0.3f;

    [Tooltip("霜冻效果每次施加时的持续时间（单位：秒）。\n" +
             "命中后在此时间内减速生效，超时后减速消失。\n" +
             "注意：霜冻本身是可刷新的，持续命中可维持减速。\n" +
             "默认值：1")]
    public float FrostFreezeDuration = 1f;

    [Tooltip("霜冻减速效果的上限（小数形式）。\n" +
             "即使叠加再多层，总减速也不会超过此值。\n" +
             "默认值：0.9（即最高减速 90%）")]
    public float FrostMaxSlow = 0.9f;

    // ══════════════════════════════════════════════════════════════
    // 雷电子弹（LightningBullet）
    // 命中后可连锁弹射到附近敌人，每层叠加静电标记，
    // 定时触发放电造成额外控制/伤害效果。
    // 与霜冻效果触发"霜电冰场"元素反应。
    // ══════════════════════════════════════════════════════════════
    [Header("雷电子弹（LightningBullet）")]
    [Tooltip("雷电子弹的飞行速度（单位：Unity 场景单位/秒）。\n" +
             "默认值：16")]
    public float LightningSpeed = 16f;

    [Tooltip("雷电子弹命中后可连锁弹射的目标数量。\n" +
             "例如 3 表示子弹命中第一个敌人后，会继续弹射到附近最多 3 个敌人。\n" +
             "默认值：3")]
    public int LightningChainCount = 3;

    [Tooltip("雷电子弹的连锁弹射搜索半径（单位：Unity 场景单位）。\n" +
             "命中敌人后，在此半径内寻找下一个弹射目标。\n" +
             "默认值：8")]
    public float LightningChainRadius = 8f;

    [Tooltip("静电效果的放电基础间隔（单位：秒）。\n" +
             "静电标记每经过此时间触发放电一次。\n" +
             "默认值：5")]
    public float StaticBaseInterval = 5f;

    [Tooltip("静电每层减少的放电间隔（单位：秒/层）。\n" +
             "层数越高放电越频繁。\n" +
             "公式：放电间隔 = max(StaticMinInterval, StaticBaseInterval - 层数 × StaticStackReduction)\n" +
             "默认值：0.2")]
    public float StaticStackReduction = 0.2f;

    [Tooltip("静电放电间隔的最小值（单位：秒）。\n" +
             "无论叠多少层，放电间隔不会低于此值。\n" +
             "默认值：2")]
    public float StaticMinInterval = 2f;

    [Tooltip("静电命中时的硬直时间（单位：秒）。\n" +
             "敌人被雷电子弹击中时的短暂控制效果。\n" +
             "默认值：0.1")]
    public float StaticStunOnHit = 0.1f;

    [Tooltip("静电放电时的硬直时间（单位：秒）。\n" +
             "静电标记定时放电时对敌人造成的控制效果。\n" +
             "默认值：0.5")]
    public float StaticStunOnDischarge = 0.5f;

    [Tooltip("静电首次叠加时的硬直时间（单位：秒）。\n" +
             "敌人首次被施加静电标记时的强控制效果（较长）。\n" +
             "默认值：1")]
    public float StaticStunOnFirstStack = 1f;

    [Tooltip("静电的最大叠加层数（整数）。\n" +
             "层数超过此值后不再增加。\n" +
             "默认值：15")]
    public int StaticMaxStacks = 15;

    // ══════════════════════════════════════════════════════════════
    // 暗影子弹（DarkBullet）
    // 命中后对敌人施加永久暗影标记。敌人死亡时，DOT 按传播效率
    // （默认 50%）传播到周围所有敌人，形成连锁衰减扩散。
    // ══════════════════════════════════════════════════════════════
    [Header("暗影子弹（DarkBullet）")]
    [Tooltip("暗影子弹的飞行速度（单位：Unity 场景单位/秒）。\n" +
             "暗影子弹速度较慢，但具有永久标记和死亡传播能力。\n" +
             "默认值：6")]
    public float DarkSpeed = 6f;

    [Tooltip("暗影标记在敌人死亡时的 DOT 传播基础半径（单位：Unity 场景单位）。\n" +
             "敌人死亡时，暗影 DOT 会传播到此半径内的所有敌人。\n" +
             "可通过升级（DarkRadiusPerLevel）逐步扩大。\n" +
             "默认值：3")]
    public float DarkBaseRadius = 3f;

    [Tooltip("暗影 DOT 死亡传播的基础效率（小数形式）。\n" +
             "例如 0.5 表示传播时保留原 DOT 伤害的 50%。\n" +
             "每次传播都会衰减，避免无限连锁过强。\n" +
             "可通过升级（DarkEfficiencyPerLevel）逐步提升。\n" +
             "默认值：0.5（即 50%）")]
    public float DarkBaseEfficiency = 0.3f;

    [Tooltip("暗影标记每层增加的传播效率（小数形式/层）。\n" +
             "传播效率 = DarkBaseEfficiency + 层数 × DarkPerStackEfficiency。\n" +
             "默认值：0.05（即每层 +5%）")]
    public float DarkPerStackEfficiency = 0.05f;

    [Tooltip("暗影每升一级增加的传播半径（单位：Unity 场景单位/级）。\n" +
             "与 DarkBaseRadius 叠加：当前半径 = DarkBaseRadius + 等级 × DarkRadiusPerLevel。\n" +
             "默认值：0.5")]
    public float DarkRadiusPerLevel = 0.5f;

    [Tooltip("暗影每升一级增加的传播效率（小数形式/级）。\n" +
             "与 DarkBaseEfficiency 叠加：当前效率 = DarkBaseEfficiency + 等级 × DarkEfficiencyPerLevel。\n" +
             "默认值：0.05（即每级 +5%）")]
    public float DarkEfficiencyPerLevel = 0.05f;

    [Tooltip("暗影标记的伤害加深百分比（小数形式）。\n" +
             "每层暗影标记使该敌人受到的伤害增加此比例。\n" +
             "默认值：0.01（即每层 +1%）")]
    public float DarkMarkDamageBonus = 0.01f;

    // ══════════════════════════════════════════════════════════════
    // 光明子弹（LightBulletController）
    // 需要蓄力的特殊子弹：蓄力完成后发射激光扫射，
    // 对命中的敌人施加光明标记，每层标记使该敌人受到的伤害 +0.5%，
    // 标记层数无上限，适合长期战斗中持续放大伤害。
    // ══════════════════════════════════════════════════════════════
    [Header("光明子弹（LightBulletController）")]
    [Tooltip("光明子弹的基础蓄力时间（单位：秒）。\n" +
             "蓄力期间无法发射，蓄力完成后才释放激光。\n" +
             "可通过升级（LightChargeReductionPerLevel）缩短。\n" +
             "默认值：3")]
    public float LightChargeDuration = 3f;

    [Tooltip("光明每升一级减少的蓄力时间（单位：秒/级）。\n" +
             "与 LightChargeDuration 叠加，但不低于 LightMinChargeDuration。\n" +
             "默认值：0.3")]
    public float LightChargeReductionPerLevel = 0.3f;

    [Tooltip("光明子弹蓄力时间的下限值（单位：秒）。\n" +
             "无论升级多少级，蓄力时间不会低于此值。\n" +
             "默认值：1.5")]
    public float LightMinChargeDuration = 1.5f;

    [Tooltip("光明激光单次命中的基础伤害值（整数）。\n" +
             "激光为扫射型，可在扫射过程中命中多个敌人。\n" +
             "默认值：1")]
    public int LightLaserDamage = 1;

    [Tooltip("光明激光的扫射角度范围（单位：度）。\n" +
             "激光从初始方向向两侧各扫射一半角度。\n" +
             "例如 45° 表示向左右各扫 22.5°，总共覆盖 45° 扇形区域。\n" +
             "默认值：45")]
    public float LightSweepAngle = 45f;

    [Tooltip("光明激光完成一次扫射的持续时间（单位：秒）。\n" +
             "扫射期间激光会旋转覆盖整个角度范围。\n" +
             "默认值：0.4")]
    public float LightSweepDuration = 0.4f;

    [Tooltip("光明激光的长度（单位：Unity 场景单位）。\n" +
             "决定激光能打到多远的敌人。\n" +
             "默认值：25")]
    public float LightLaserLength = 25f;

    [Tooltip("光明激光的宽度/粗细（单位：Unity 场景单位）。\n" +
             "影响激光的命中判定和视觉粗细。\n" +
             "默认值：1.5")]
    public float LightLaserWidth = 1.5f;

    [Tooltip("光明标记在敌人身上的持续时间（单位：秒）。\n" +
             "超时后标记消失，失去对应的伤害加成。\n" +
             "默认值：15")]
    public float LightMarkDuration = 15f;

    [Tooltip("光明标记的最大叠加层数。0 表示无上限。\n" +
             "每层标记使该敌人受到的所有伤害 +0.5%。\n" +
             "无上限意味着长期战斗中伤害可无限放大。\n" +
             "默认值：0（无上限）")]
    public int LightMarkMaxStacks = 0;

    [Tooltip("光明标记每层增加的受伤百分比（小数形式）。\n" +
             "例如 0.005 表示每层 +0.5% 受伤。\n" +
             "默认值：0.005（即 0.5%）")]
    public float LightMarkDamagePerStack = 0.005f;

    [Tooltip("光明子弹/激光贴图的缩放比例。\n" +
             "影响视觉大小，不影响伤害判定。\n" +
             "默认值：0.7")]
    public float LightTextureScale = 0.7f;

    [Tooltip("光明激光扫射结束后到下一次开始蓄力的等待时间（单位：秒）。\n" +
             "设为 0 表示激光扫射结束后立即开始下一次蓄力。\n" +
             "实际射击周期 = 蓄力时间 + 扫射时间 + 此延迟。\n" +
             "默认值：0（立即重新蓄力）")]
    public float LightPostFireDelay = 0f;

    // ══════════════════════════════════════════════════════════════
    // 风蚀子弹（WindBullet）
    // 高速单发，固定向鼠标方向，命中叠加风化层数，
    // 每层增加伤害和击退距离。与燃烧效果触发"燃烧扩散"元素反应。
    // ══════════════════════════════════════════════════════════════
    [Header("风蚀子弹（WindBullet）")]
    [Tooltip("风蚀子弹的飞行速度（单位：Unity 场景单位/秒）。\n" +
             "高速子弹，快速命中目标。\n" +
             "默认值：60")]
    public float WindSpeed = 60f;

    [Tooltip("风蚀每命中几次敌人叠加一层风化效果。\n" +
             "例如 1 表示每次命中都叠加一层，2 表示每 2 次命中叠一层。\n" +
             "层数越高，伤害和击退效果越强。\n" +
             "默认值：1（每次命中叠加）")]
    public int WindHitsPerStack = 1;

    [Tooltip("风蚀效果的基础击退距离（单位：Unity 场景单位）。\n" +
             "命中敌人时将其推开的距离，可叠加层数增强。\n" +
             "默认值：0.6")]
    public float WindKnockbackDistance = 0.6f;

    [Tooltip("风蚀效果的实际击退距离（单位：Unity 场景单位）。\n" +
             "敌人被风蚀子弹命中后被推开的距离。\n" +
             "注意：这与 WindKnockbackDistance 不同，此为风化标记的固定击退值。\n" +
             "默认值：1")]
    public float WindErosionKnockbackDistance = 1f;

    [Tooltip("风蚀效果的最大叠加层数（整数）。\n" +
             "层数超过此值后不再增加。\n" +
             "默认值：999（实际上限）")]
    public int WindMaxStacks = 999;

    // ══════════════════════════════════════════════════════════════
    // 元素反应参数
    // 两种 DOT 效果同时作用于同一敌人时触发特殊反应：
    //   • 燃烧 × 风化 → 燃烧扩散：消耗一层风化，将燃烧传播到周围所有敌人
    //   • 霜冻 × 静电 → 霜电冰场：在敌人位置生成冰冻区域，持续减速+伤害
    // ══════════════════════════════════════════════════════════════
    [Header("元素反应参数")]
    [Tooltip("燃烧扩散反应的传播半径（单位：Unity 场景单位）。\n" +
             "触发条件：敌人同时拥有燃烧和风化效果。\n" +
             "触发后消耗一层风化，将燃烧效果传播到此半径内的所有敌人。\n" +
             "默认值：5")]
    public float BurnSpreadRadius = 5f;

    [Tooltip("霜电冰场的覆盖半径（单位：Unity 场景单位）。\n" +
             "触发条件：敌人同时拥有霜冻和静电效果。\n" +
             "在敌人位置生成冰冻区域，对区域内敌人持续造成霜冻效果。\n" +
             "默认值：1")]
    public float FrostLightningFieldRadius = 1f;

    [Tooltip("霜电冰场的存在时间（单位：秒）。\n" +
             "冰场生成后在此时间内持续生效，超时自动消失。\n" +
             "默认值：2")]
    public float FrostLightningFieldDuration = 2f;

    [Tooltip("霜电冰场对区域内敌人施加霜冻效果的时间间隔（单位：秒）。\n" +
             "每隔此时间对冰场内所有敌人施加一次霜冻减速。\n" +
             "默认值：1.25")]
    public float FrostLightningTickInterval = 1.25f;

    [Tooltip("融化反应的灼烧持续时间（单位：秒）。\n" +
             "触发条件：燃烧子弹命中拥有霜冻层数的敌人。\n" +
             "消耗一层霜冻，使敌人在持续时间内受到的所有 DOT 伤害翻倍。\n" +
             "默认值：1")]
    public float MeltDuration = 1f;

    [Tooltip("融化反应的 DOT 伤害倍率。\n" +
             "灼烧期间，敌人受到的所有 DOT 伤害乘以此值。\n" +
             "默认值：2（即伤害翻倍）")]
    public float MeltDamageMultiplier = 2f;

    // ══════════════════════════════════════════════════════════════
    // 通用参数
    // 追踪子弹（HomingProjectile）的共享配置，
    // 可被多种需要追踪能力的子弹复用。
    // ══════════════════════════════════════════════════════════════
    [Header("通用参数（追踪子弹）")]
    [Tooltip("追踪子弹的飞行速度（单位：Unity 场景单位/秒）。\n" +
             "追踪子弹会自动寻找并跟踪最近的敌人。\n" +
             "默认值：10")]
    public float HomingSpeed = 10f;

    [Tooltip("追踪子弹的最长存活时间（单位：秒）。\n" +
             "超过此时间未命中敌人则自动回收。\n" +
             "默认值：5")]
    public float HomingLifetime = 5f;

    [Tooltip("追踪子弹搜索目标的范围半径（单位：Unity 场景单位）。\n" +
             "在此半径内寻找最近的敌人作为追踪目标。\n" +
             "超出此范围则直线飞行。\n" +
             "默认值：6")]
    public float HomingTargetRadius = 6f;

    // ══════════════════════════════════════════════════════════════
    // 引爆设置（DetonateSystem）
    // 按 E 键蓄力引爆，从玩家身上炸出红色冲击波，
    // 接触到的敌人触发引爆伤害（仅一次）。
    // ══════════════════════════════════════════════════════════════
    [Header("引爆设置")]

    [Tooltip("引爆技能的冷却时间（单位：秒）。\n" +
             "每次引爆后需等待此时间才能再次使用。\n" +
             "默认值：12")]
    public float DetonateCooldown = 12f;

    [Tooltip("引爆的基础伤害倍率。\n" +
             "引爆伤害 = 敌人DOT层数 × 此倍率。\n" +
             "默认值：3")]
    public float DetonateMultiplier = 3f;

    [Tooltip("引爆冲击波的最大半径（单位：Unity 场景单位）。\n" +
             "冲击波从玩家位置扩展到此半径。\n" +
             "默认值：50")]
    public float DetonateRadius = 50f;

    [Tooltip("引爆冲击波的扩散持续时间（单位：秒）。\n" +
             "冲击波从玩家扩展到最大半径所需时间。\n" +
             "越短扩散越快，总伤害数字显示越快。\n" +
             "默认值：0.3")]
    public float DetonateWaveDuration = 0.3f;

    [Tooltip("引爆冲击波期间是否时停（游戏暂停，冲击波继续扩散）。\n" +
             "时停期间只有冲击波和屏幕晃动生效，其他游戏逻辑冻结。\n" +
             "冲击波结束后自动恢复。\n" +
             "默认值：true")]
    public bool DetonateTimeStop = true;

    [Tooltip("引爆时屏幕晃动强度。\n" +
             "默认值：2.5")]
    public float DetonateShakeIntensity = 2.5f;

    [Tooltip("引爆时屏幕晃动持续时间（单位：秒）。\n" +
             "默认值：0.5")]
    public float DetonateShakeDuration = 0.5f;

    [Tooltip("引爆对流血敌人的额外伤害（基于最大HP的百分比）。\n" +
             "默认值：0.2（即 20% MaxHP）")]
    public float DetonateBleedHpPct = 0.2f;

    [Tooltip("引爆对燃烧敌人的额外伤害（基于最大HP的百分比）。\n" +
             "默认值：0.15（即 15% MaxHP）")]
    public float DetonateBurnHpPct = 0.15f;

    [Tooltip("引爆对中毒敌人的额外伤害（基于最大HP的百分比）。\n" +
             "默认值：0.15（即 15% MaxHP）")]
    public float DetonatePoisonHpPct = 0.15f;

    [Tooltip("连锁引爆的最大次数。\n" +
             "引爆命中后，可触发此数量的连锁引爆。\n" +
             "默认值：3")]
    public int DetonateMaxChainCount = 3;

    [Tooltip("连锁引爆的搜索半径（单位：Unity 场景单位）。\n" +
             "默认值：10")]
    public float DetonateChainRadius = 10f;

    [Tooltip("连锁引爆的伤害衰减比例（0~1）。\n" +
             "每次连锁伤害 = 基础引爆伤害 × 此比例。\n" +
             "默认值：0.5（即 50%）")]
    public float DetonateChainDamageRatio = 0.5f;

    [Tooltip("蓄力时的移动速度惩罚（0~1）。\n" +
             "例如 0.5 表示蓄力时移速降低 50%。\n" +
             "默认值：0.5")]
    public float DetonateChargeMoveSpeedPenalty = 0.5f;

    [Tooltip("最大蓄力时间（单位：秒）。\n" +
             "超过此时间自动释放引爆。\n" +
             "默认值：3")]
    public float DetonateChargeMaxTime = 3f;

    [Tooltip("余烬效果触发的最低燃烧层数。\n" +
             "燃烧叠层超过此值时，在敌人位置生成火焰区域。\n" +
             "默认值：10")]
    public int DetonateEmberThreshold = 10;

    [Tooltip("余烬火焰区域的最大生成数量。\n" +
             "默认值：5")]
    public int DetonateEmberMaxZones = 5;

    [Tooltip("余烬火焰区域的持续时间（单位：秒）。\n" +
             "默认值：3")]
    public float DetonateEmberDuration = 3f;

    [Tooltip("余烬火焰区域的半径（单位：Unity 场景单位）。\n" +
             "默认值：1.5")]
    public float DetonateEmberRadius = 1.5f;

    [Tooltip("霜爆触发的最低减速百分比（0~1）。\n" +
             "霜冻减速超过此值时触发霜爆。\n" +
             "默认值：0.8（即 80%）")]
    public float DetonateFrostShatterThreshold = 0.8f;

    [Tooltip("霜爆的范围半径（单位：Unity 场景单位）。\n" +
             "默认值：4")]
    public float DetonateFrostShatterRadius = 4f;

    [Tooltip("霜爆每层霜冻的伤害倍率。\n" +
             "霜爆伤害 = 霜冻层数 × 此值。\n" +
             "默认值：5")]
    public int DetonateFrostShatterDmgPerStack = 5;

    [Tooltip("触发高命中连锁引爆的最低命中敌人数量。\n" +
             "命中超过此数量后，获得短暂的连锁引爆窗口。\n" +
             "默认值：10")]
    public int DetonateHighHitThreshold = 10;

    [Tooltip("高命中连锁引爆的窗口持续时间（单位：秒）。\n" +
             "默认值：3")]
    public float DetonateHighHitWindow = 3f;

    [Tooltip("连锁反应的间隔时间（单位：秒）。\n" +
             "每次连锁反应之间的延迟。\n" +
             "默认值：0.3")]
    public float DetonateChainReactionInterval = 0.3f;

    [Tooltip("连锁反应的伤害衰减系数。\n" +
             "每次连锁后伤害乘以此系数。\n" +
             "默认值：0.5")]
    public float DetonateChainReactionDecay = 0.5f;

    // ── 变更通知 ──
    /// <summary>Inspector 修改值时触发，各系统订阅此事件刷新缓存</summary>
    public static System.Action OnConfigChanged;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Editor 中修改值时通知所有订阅者
        OnConfigChanged?.Invoke();
    }
#endif

    // ── 单例访问 ──
    private static DotEffectConfig _instance;

    /// <summary>
    /// 获取全局配置实例（从 Resources 加载）
    /// 如果没有找到配置文件，返回默认值的临时实例
    /// </summary>
    public static DotEffectConfig GetDefault()
    {
        if (_instance == null)
        {
            _instance = Resources.Load<DotEffectConfig>("Configs/DotEffectConfig");
            if (_instance == null)
            {
                // 回退：创建临时实例使用默认值
                _instance = CreateInstance<DotEffectConfig>();
                DebugHelper.LogWarning("[DotEffectConfig] 未找到 Resources/Configs/DotEffectConfig.asset，使用默认值");
            }
        }
        return _instance;
    }
}