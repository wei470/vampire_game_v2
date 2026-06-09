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
        AttackSpeed,        // 急速 — 攻速+子弹速度
        BulletCount,        // 弹幕 — 子弹数量增加
        Ricochet,           // 反弹 — 子弹反弹几率
        BulletSize,         // 共振 — 子弹碰撞体积+击退

        // ── P0 新增强化类别 ──
        DotSaturation,      // 饱和 — 同一敌人每种DOT伤害+%
        DetonateExtra,      // 元素引爆 — 引爆时每种DOT额外固定伤害
        DotLifesteal,       // 吸血法术 — DOT每次伤害回复生命
        DotOverflow,        // 溢出弹 — DOT枪命中已有同DOT敌人时额外叠层

        // ── P1 深度玩法类别 ──
        DotPandemic,        // [已弃用]蔓延 — DOT传播效率+15%
        ChainReaction,      // 连锁反应 — 引爆杀死敌人时二次引爆
        DualWield,          // [已弃用]双持 — 随机DOT枪射速+15%

        // ── P2 协同/趣味类别 ──
        DotResonance,       // 共鸣 — DOT触发10%不消耗持续时间
        Toxicology,         // [已弃用]剧毒天赋 — DOT暴击率+8%
        CorruptTouch,       // 腐化之触 — DOT命中时弱化debuff
        ElementalStorm,     // 元素风暴 — 3种以上DOT时全局被动伤害
        ShadowLink,         // 暗影链接 — 黑暗标记传播范围+1，效率+10%
        LightJudgment,      // 光明审判 — 光明标记每层加成提升
        StaticField,        // 静电领域 — 静电控制敌人周围也获得静电
        FrostExplosion,     // 霜爆 — 霜冻减速80%+敌人引爆时额外冰霜伤害

        // ── 子弹增强扩展类别 ──
        AmmoMastery,        // 弹药精通 — 子弹速度+20%
        ElementalAffinity,  // 元素亲和 — 每种DOT枪为其他DOT枪提供+3%伤害
        Penetrate,          // 贯穿弹 — DOT枪子弹穿透+1

        // ── 生存向类别 ──
        ElementalShield,    // 元素护盾 — 每种DOT枪+2最大生命
        PhaseShift,         // 相位移动 — 引爆后2秒免疫碰撞
        SoulSiphon,         // 灵魂虹吸 — DOT敌人死亡时回血+移速

        // ── P3 终极/高级类别 ──
        ElementalMaster,    // 元素大师 — 5种以上DOT枪时DOT持续+25%，引爆CD-20%
        Doomsday,           // 末日审判 — 引爆时3种以上DOT，15%HP以下秒杀
        EternalAgony,       // 永恒痛苦 — DOT持续时间×2，单次伤害-15%
        AnnihilationZone,   // 湮灭领域 — 引爆后留下元素领域
        EmberBoost,         // 余烬强化 — 余烬伤害+25%，持续+1秒
        ShatterBoost        // [已弃用]碎裂强化 — 碎片+2 伤害+15%
    }
}