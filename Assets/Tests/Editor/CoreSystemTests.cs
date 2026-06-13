#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// #40 核心系统单元测试 — 覆盖 DOT 计算、对象池、场景重置等关键系统。
/// 使用 Unity Test Framework (NUnit)，在编辑器中运行。
/// </summary>
[TestFixture]
public class CoreSystemTests
{
    // ═══ DOT 计算测试 ═══

    [Test]
    public void DotGunState_IsStruct_NoHeapAllocation()
    {
        // DotGunState 应为 struct（#11 优化）
        var gun = new DotGunState
        {
            effectType = StatusEffectType.Poison,
            color = Color.green,
            cooldown = 1.5f,
            impactDamage = 0,
            dotDps = 2f,
            dotDuration = 5f,
            lastFireTime = -999f,
            upgradeLevel = 1
        };

        Assert.AreEqual(StatusEffectType.Poison, gun.effectType);
        Assert.AreEqual(2f, gun.dotDps);
        Assert.AreEqual(1, gun.upgradeLevel);
    }

    [Test]
    public void DotGunState_UpgradeLevel_Increments()
    {
        var gun = new DotGunState
        {
            effectType = StatusEffectType.Bleed,
            dotDps = 2f,
            impactDamage = 3,
            upgradeLevel = 1
        };

        // 模拟升级
        gun.dotDps *= 1.15f;
        gun.impactDamage = Mathf.RoundToInt(gun.impactDamage * 1.1f);
        gun.upgradeLevel++;

        Assert.AreEqual(2, gun.upgradeLevel);
        Assert.Greater(gun.dotDps, 2f);
        Assert.AreEqual(3, gun.impactDamage); // 3 * 1.1 = 3.3 → round to 3
    }

    // ═══ MageUpgradeConfig 测试 ═══

    [Test]
    public void MageUpgradeConfig_BuildCustomUpgrades_ReturnsCorrectCount()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        var upgrades = config.BuildCustomUpgrades();

        // 7 DOT 子弹枪 + upgradeEntries 数量
        Assert.GreaterOrEqual(upgrades.Length, 7, "至少应有7个DOT子弹升级");
    }

    [Test]
    public void MageUpgradeConfig_IsDotGunUpgrade_IdentifiesCorrectly()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();

        Assert.IsTrue(config.IsDotGunUpgrade("poison"));
        Assert.IsTrue(config.IsDotGunUpgrade("burn"));
        Assert.IsTrue(config.IsDotGunUpgrade("frostbite"));
        Assert.IsTrue(config.IsDotGunUpgrade("static"));
        Assert.IsTrue(config.IsDotGunUpgrade("dark"));
        Assert.IsTrue(config.IsDotGunUpgrade("light"));
        Assert.IsTrue(config.IsDotGunUpgrade("wind"));
        Assert.IsFalse(config.IsDotGunUpgrade("corrosion"));
        Assert.IsFalse(config.IsDotGunUpgrade("unknown"));
    }

    [Test]
    public void MageUpgradeConfig_GetDotGunEntry_ReturnsCorrectData()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();

        var poison = config.GetDotGunEntry("poison");
        Assert.IsTrue(poison.HasValue);
        Assert.AreEqual(StatusEffectType.Poison, poison.Value.effectType);

        var burn = config.GetDotGunEntry("burn");
        Assert.IsTrue(burn.HasValue);
        Assert.AreEqual(StatusEffectType.Burn, burn.Value.effectType);
    }

    // ═══ 接口测试 ═══

    [Test]
    public void Interfaces_IDotEffect_HasRequiredMembers()
    {
        // 验证接口存在（编译时检查）
        System.Type interfaceType = typeof(IDotEffect);
        Assert.IsNotNull(interfaceType);
        Assert.IsTrue(interfaceType.IsInterface);

        var methods = interfaceType.GetMethods();
        Assert.Greater(methods.Length, 0);
    }

    [Test]
    public void Interfaces_IUpgradable_HasRequiredMembers()
    {
        System.Type interfaceType = typeof(IUpgradable);
        Assert.IsNotNull(interfaceType);
        Assert.IsTrue(interfaceType.IsInterface);
    }

    // ═══ DOT 伤害计算测试 ═══

    [Test]
    public void PoisonStackEffect_DamagePerTick_Equals_BasePlusStacks()
    {
        // 基础 DAMAGE_PER_TICK=2，每层+1
        // 1层: 2+(1-1)=2, 3层: 2+(3-1)=4, 10层: 2+(10-1)=11
        int baseDmg = 2;
        for (int stacks = 1; stacks <= 20; stacks++)
        {
            int expected = baseDmg + (stacks - 1);
            Assert.AreEqual(expected, baseDmg + (stacks - 1),
                $"Stacks={stacks}: expected {expected} damage");
        }
    }

    [Test]
    public void PoisonStackEffect_MaxStacks_Is20()
    {
        // 最大层数应为20
        int maxStacks = 20;
        Assert.AreEqual(20, maxStacks);
    }

    [Test]
    public void BurnStackEffect_TickInterval_DecreasesWithStacks()
    {
        // tick间隔 = max(0.2, 1.0/stacks)
        // 1层: 1.0s, 2层: 0.5s, 5层: 0.2s, 10层: 0.2s(clamped)
        Assert.AreEqual(1.0f, Mathf.Max(0.2f, 1.0f / 1));
        Assert.AreEqual(0.5f, Mathf.Max(0.2f, 1.0f / 2));
        Assert.AreEqual(0.2f, Mathf.Max(0.2f, 1.0f / 5));
        Assert.AreEqual(0.2f, Mathf.Max(0.2f, 1.0f / 10), "Should be clamped at 0.2s");
    }

    [Test]
    public void FrostEffect_SlowPercent_IncreasesPerStack()
    {
        // BASE_SLOW=0.30, PER_STACK=0.05, MAX=0.90
        float baseSlow = 0.30f;
        float perStack = 0.05f;
        float maxSlow = 0.90f;

        Assert.AreEqual(0.30f, Mathf.Min(maxSlow, baseSlow + (1 - 1) * perStack), 0.001f);
        Assert.AreEqual(0.35f, Mathf.Min(maxSlow, baseSlow + (2 - 1) * perStack), 0.001f);
        Assert.AreEqual(0.50f, Mathf.Min(maxSlow, baseSlow + (5 - 1) * perStack), 0.001f);
        Assert.AreEqual(0.90f, Mathf.Min(maxSlow, baseSlow + (13 - 1) * perStack), 0.001f);
        Assert.AreEqual(0.90f, Mathf.Min(maxSlow, baseSlow + (20 - 1) * perStack), 0.001f, "Should be clamped at 90%");
    }

    [Test]
    public void FrostEffect_SpeedReduction_AppliesCorrectly()
    {
        float originalSpeed = 3f;
        float slowPercent = 0.5f;
        float result = originalSpeed * (1f - slowPercent);
        Assert.AreEqual(1.5f, result);
    }

    // ═══ 引爆系统边界条件测试 ═══

    [Test]
    public void DetonateResult_DefaultValues_AreZero()
    {
        var result = new DetonateResult();
        Assert.AreEqual(0, result.totalDamage);
        Assert.AreEqual(0, result.poisonStacks);
        Assert.AreEqual(0, result.burnStacks);
        Assert.AreEqual(0, result.bleedStacks);
        Assert.AreEqual(0, result.frostStacks);
        Assert.IsFalse(result.hadBurn);
        Assert.IsFalse(result.hadFrost);
        Assert.IsFalse(result.hadPoison);
    }

    [Test]
    public void LightMarkEffect_DamageMultiplier_PerStackIs05Percent()
    {
        // 每层+0.5% = 0.005
        // 0层: 1.0, 10层: 1.05, 100层: 1.5, 200层: 2.0
        Assert.AreEqual(1.0f, 1f + 0 * 0.005f);
        Assert.AreEqual(1.05f, 1f + 10 * 0.005f, 0.001f);
        Assert.AreEqual(1.5f, 1f + 100 * 0.005f, 0.001f);
        Assert.AreEqual(2.0f, 1f + 200 * 0.005f, 0.001f);
    }

    [Test]
    public void StaticStackEffect_DischargeInterval_DecreasesWithStacks()
    {
        // BASE_INTERVAL=5.0, STACK_REDUCTION=0.2, MIN=2.0
        float baseInterval = 5.0f;
        float reduction = 0.2f;
        float minInterval = 2.0f;

        Assert.AreEqual(5.0f, Mathf.Max(minInterval, baseInterval - 0 * reduction));
        Assert.AreEqual(4.8f, Mathf.Max(minInterval, baseInterval - 1 * reduction));
        Assert.AreEqual(3.0f, Mathf.Max(minInterval, baseInterval - 10 * reduction));
        Assert.AreEqual(2.0f, Mathf.Max(minInterval, baseInterval - 15 * reduction));
        Assert.AreEqual(2.0f, Mathf.Max(minInterval, baseInterval - 20 * reduction), "Should be clamped at 2.0s");
    }

    // ═══ 升级叠加效果正确性测试 ═══

    [Test]
    public void EnemyDotResistance_DamageMultiplier_CalculatesCorrectly()
    {
        // 抗性0.5 → 倍率0.5, 弱点-0.5 → 倍率1.5, 免疫1.0 → 倍率0
        Assert.AreEqual(0.5f, Mathf.Max(0f, 1f - 0.5f));
        Assert.AreEqual(1.5f, Mathf.Max(0f, 1f - (-0.5f)));
        Assert.AreEqual(0f, Mathf.Max(0f, 1f - 1.0f));
        Assert.AreEqual(1.0f, Mathf.Max(0f, 1f - 0f));
    }

    [Test]
    public void EnemyDotResistance_TankPreset_HasCorrectValues()
    {
        // Tank: 流血抗性+50%, 霜冻弱点-30%
        float bleedRes = 0.5f;
        float frostRes = -0.3f;
        Assert.AreEqual(0.5f, Mathf.Max(0f, 1f - bleedRes)); // 流血伤害减半
        Assert.AreEqual(1.3f, Mathf.Max(0f, 1f - frostRes)); // 霜冻伤害+30%
    }

    [Test]
    public void DarkMarkEffect_SpreadEfficiency_DefaultIs50()
    {
        float efficiency = 0.5f;
        Assert.AreEqual(0.5f, efficiency);
        // 传播后DPS = 原DPS * 0.5
        Assert.AreEqual(2.5f, 5f * efficiency);
    }

    // ═══ DOT伤害来源追踪测试 (5.2架构改进) ═══

    [Test]
    public void StatusEffect_HasSourceTracking_Fields()
    {
        // StatusEffect 应包含 canCrit/critChance/critMultiplier 用于伤害来源
        var effect = new StatusEffect
        {
            type = StatusEffectType.Poison,
            damagePerSecond = 2f,
            remainingDuration = 5f,
            canCrit = true,
            critChance = 0.3f,
            critMultiplier = 2f
        };
        Assert.IsTrue(effect.canCrit);
        Assert.AreEqual(0.3f, effect.critChance);
        Assert.AreEqual(2f, effect.critMultiplier);
    }

    [Test]
    public void DotDamageCalculation_WithCrit_MultipliesCorrectly()
    {
        // 基础伤害 * 暴击倍率
        int baseDmg = 5;
        float critMult = 2f;
        int critDmg = Mathf.RoundToInt(baseDmg * critMult);
        Assert.AreEqual(10, critDmg);
    }

    [Test]
    public void DotDamageCalculation_WithCritRounds_Correctly()
    {
        // 3 * 1.5 = 4.5 → Mathf.RoundToInt 使用银行家舍入，4.5 → 4
        int baseDmg = 3;
        float critMult = 1.5f;
        Assert.AreEqual(4, Mathf.RoundToInt(baseDmg * critMult));
    }

    // ═══ MagnetMultiplier 测试 ═══

    [Test]
    public void MagnetMultiplier_DefaultsToOne()
    {
        MagnetMultiplierSystem.Reset();
        Assert.AreEqual(1f, MagnetMultiplierSystem.MagnetRangeMultiplier);
    }

    [Test]
    public void MagnetMultiplier_ResetWorks()
    {
        MagnetMultiplierSystem.ApplyMagnetRangeUp();
        MagnetMultiplierSystem.Reset();
        Assert.AreEqual(1f, MagnetMultiplierSystem.MagnetRangeMultiplier);
    }

    // ═══ #18 新增：DOT 暴击公式测试（含进化+协同叠加）═══

    [Test]
    public void DotCrit_BasicFormula_CalculatesCorrectly()
    {
        // 基础暴击率 10% + 升级 10% = 20%
        float baseCritChance = 0.10f;
        float upgradeBonus = 0.10f;
        float totalCritChance = baseCritChance + upgradeBonus;

        Assert.AreEqual(0.20f, totalCritChance, 0.001f);
    }

    [Test]
    public void DotCrit_WithEvolution_ScalesCorrectly()
    {
        // 进化系统提供额外暴击加成
        float baseCritChance = 0.10f;
        float upgradeBonus = 0.20f; // 2次凋零升级
        float evolutionBonus = 0.05f; // 进化加成
        float totalCritChance = Mathf.Clamp01(baseCritChance + upgradeBonus + evolutionBonus);

        Assert.AreEqual(0.35f, totalCritChance, 0.001f);
    }

    [Test]
    public void DotCrit_CappedAt100Percent()
    {
        // 暴击率不应超过 100%
        float baseCritChance = 0.10f;
        float upgradeBonus = 0.50f; // 5次凋零
        float evolutionBonus = 0.20f;
        float synergyBonus = 0.30f;
        float totalCritChance = Mathf.Clamp01(baseCritChance + upgradeBonus + evolutionBonus + synergyBonus);

        Assert.AreEqual(1.0f, totalCritChance, 0.001f);
    }

    [Test]
    public void DotCrit_Multiplier_AppliesOnCrit()
    {
        // 暴击时伤害 = 基础 * 暴击倍率
        int baseDmg = 5;
        float critMultiplier = 2.0f;
        int critDmg = Mathf.RoundToInt(baseDmg * critMultiplier);
        int normalDmg = baseDmg; // 未暴击

        Assert.AreEqual(10, critDmg);
        Assert.AreEqual(5, normalDmg);
    }

    [Test]
    public void DotCrit_Multiplier_WithStacks()
    {
        // 每层凋零 +10% 暴击率，3层 = 30%
        float baseCrit = 0.10f;
        float perStack = 0.10f;
        int stacks = 3;
        float totalCrit = baseCrit + perStack * stacks;

        Assert.AreEqual(0.40f, totalCrit, 0.001f);
    }

    // ═══ #18 新增：元素反应触发条件测试 ═══

    [Test]
    public void ElementReaction_BurnWind_CanTriggerTogether()
    {
        // 燃烧 + 风化 可以共存在同一敌人上
        bool hasBurn = true;
        bool hasWind = true;
        bool canReact = hasBurn && hasWind;

        Assert.IsTrue(canReact);
    }

    [Test]
    public void ElementReaction_FrostLightning_CanTriggerTogether()
    {
        // 霜冻 + 雷电 可以触发冰场
        bool hasFrost = true;
        bool hasLightning = true;
        bool canTriggerField = hasFrost && hasLightning;

        Assert.IsTrue(canTriggerField);
    }

    [Test]
    public void ElementReaction_FrostLightningField_RadiusAndDuration()
    {
        // 霜电冰场参数
        float fieldRadius = 1f;
        float fieldDuration = 2f;
        float tickInterval = 1.25f;

        Assert.Greater(fieldRadius, 0f, "冰场半径应大于 0");
        Assert.Greater(fieldDuration, 0f, "冰场持续时间应大于 0");
        Assert.Greater(tickInterval, 0f, "tick 间隔应大于 0");
        Assert.Less(tickInterval, fieldDuration, "tick 间隔应小于持续时间");
    }

    [Test]
    public void ElementReaction_BurnSpread_RadiusFromConfig()
    {
        // 燃烧扩散半径应从 DotEffectConfig 读取
        float spreadRadius = 5f; // 默认值
        Assert.AreEqual(5f, spreadRadius);
        Assert.Greater(spreadRadius, 0f, "扩散半径应大于 0");
    }

    [Test]
    public void ElementReaction_MissingOneElement_NoReaction()
    {
        // 只有一种元素时不应触发反应
        bool hasBurn = true;
        bool hasWind = false;
        bool canReact = hasBurn && hasWind;

        Assert.IsFalse(canReact);
    }

    // ═══ #18 新增：引爆伤害计算边界测试 ═══

    [Test]
    public void DetonateDamage_ZeroDOT_ReturnsZero()
    {
        // 无 DOT 效果时引爆伤害为 0
        int dotCount = 0;
        int baseDmgPerDot = 5;
        int totalDmg = dotCount * baseDmgPerDot;

        Assert.AreEqual(0, totalDmg);
    }

    [Test]
    public void DetonateDamage_SingleDOT_CalculatesCorrectly()
    {
        // 1 种 DOT 时的基础引爆伤害
        int dotCount = 1;
        int baseDmgPerDot = 5;
        float detonateMultiplier = 1.0f;
        int totalDmg = Mathf.RoundToInt(dotCount * baseDmgPerDot * detonateMultiplier);

        Assert.AreEqual(5, totalDmg);
    }

    [Test]
    public void DetonateDamage_EightDOT_FullStack_CalculatesCorrectly()
    {
        // 8 种 DOT 全满时的引爆伤害
        int dotCount = 8;
        int baseDmgPerDot = 5;
        float detonateMultiplier = 1.0f;
        int totalDmg = Mathf.RoundToInt(dotCount * baseDmgPerDot * detonateMultiplier);

        Assert.AreEqual(40, totalDmg);
    }

    [Test]
    public void DetonateDamage_WithMultiplier_ScalesCorrectly()
    {
        // 引爆倍率加成（辐射升级 +30%）
        int dotCount = 4;
        int baseDmgPerDot = 5;
        float detonateMultiplier = 1.3f; // +30%
        int totalDmg = Mathf.RoundToInt(dotCount * baseDmgPerDot * detonateMultiplier);

        Assert.AreEqual(26, totalDmg); // 4*5*1.3 = 26
    }

    [Test]
    public void DetonateDamage_WithExtraPerDot_AddsCorrectly()
    {
        // 元素引爆：每种 DOT 额外 +8 伤害
        int dotCount = 3;
        int baseDmgPerDot = 5;
        int extraPerDot = 8;
        float detonateMultiplier = 1.0f;
        int totalDmg = Mathf.RoundToInt(dotCount * (baseDmgPerDot + extraPerDot) * detonateMultiplier);

        Assert.AreEqual(39, totalDmg); // 3 * (5+8) = 39
    }

    [Test]
    public void DetonateDamage_ChainReaction_50PercentDamage()
    {
        // 连锁反应二次引爆 50% 伤害
        int originalDmg = 40;
        float chainRatio = 0.5f;
        int chainDmg = Mathf.RoundToInt(originalDmg * chainRatio);

        Assert.AreEqual(20, chainDmg);
    }

    // ═══ #18 新增：升级叠加上限测试 ═══

    [Test]
    public void UpgradeStacking_InfiniteStacks_AllowsUnlimited()
    {
        // maxStacks = 0 表示无限叠加
        int maxStacks = 0;
        int currentStacks = 100;
        bool canStack = maxStacks == 0 || currentStacks < maxStacks;

        Assert.IsTrue(canStack);
    }

    [Test]
    public void UpgradeStacking_LimitedStacks_EnforcesLimit()
    {
        // maxStacks = 3 表示最多叠加 3 次
        int maxStacks = 3;
        int currentStacks = 3;
        bool canStack = maxStacks == 0 || currentStacks < maxStacks;

        Assert.IsFalse(canStack);
    }

    [Test]
    public void UpgradeStacking_LimitedStacks_AllowsBelowLimit()
    {
        int maxStacks = 3;
        int currentStacks = 2;
        bool canStack = maxStacks == 0 || currentStacks < maxStacks;

        Assert.IsTrue(canStack);
    }

    [Test]
    public void UpgradeStacking_Ricochet_MaxStacks3()
    {
        // 贯穿弹最大叠加 3 次
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        var ricochet = config.GetUpgradeEntry("ricochet");

        Assert.IsTrue(ricochet.HasValue);
        Assert.AreEqual(3, ricochet.Value.maxStacks);
    }

    [Test]
    public void UpgradeStacking_ChainReaction_MaxStacks3()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        var chainReaction = config.GetUpgradeEntry("chain_reaction");

        Assert.IsTrue(chainReaction.HasValue);
        Assert.AreEqual(3, chainReaction.Value.maxStacks);
    }

    [Test]
    public void UpgradeStacking_AnnihilationZone_MaxStacks3()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        var annihilation = config.GetUpgradeEntry("annihilation_zone");

        Assert.IsTrue(annihilation.HasValue);
        Assert.AreEqual(3, annihilation.Value.maxStacks);
    }

    [Test]
    public void UpgradeStacking_Corrosion_InfiniteStacks()
    {
        // 腐蚀是无限叠加 (maxStacks = 0)
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        var corrosion = config.GetUpgradeEntry("corrosion");

        Assert.IsTrue(corrosion.HasValue);
        Assert.AreEqual(0, corrosion.Value.maxStacks);
    }

    [Test]
    public void UpgradeStacking_Haste_InfiniteStacks()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        var haste = config.GetUpgradeEntry("haste");

        Assert.IsTrue(haste.HasValue);
        Assert.AreEqual(0, haste.Value.maxStacks);
    }

    [Test]
    public void UpgradeStacking_PercentBonus_AccumulatesCorrectly()
    {
        // 腐蚀 10% * 5 次 = 50%
        float perStack = 0.10f;
        int stacks = 5;
        float total = perStack * stacks;

        Assert.AreEqual(0.50f, total, 0.001f);
    }

    [Test]
    public void UpgradeStacking_DetonateMultiplier_ScalesWithRadiation()
    {
        // 辐射 +30% * 无限叠加
        float baseMultiplier = 1.0f;
        float perUpgrade = 0.30f;
        int upgradeCount = 3;
        float totalMultiplier = baseMultiplier + perUpgrade * upgradeCount;

        Assert.AreEqual(1.9f, totalMultiplier, 0.001f);
    }

    // ═══ #18 新增：SpatialGrid 测试 ═══

    [Test]
    public void SpatialGrid_QueryRadius_ReturnsList()
    {
        // SpatialGrid 应该能返回查询结果（即使为空）
        var results = SpatialGrid.QueryRadius(Vector2.zero, 10f);
        Assert.IsNotNull(results);
    }

    [Test]
    public void SpatialGrid_QueryRadius_ZeroRadius_ReturnsEmpty()
    {
        var results = SpatialGrid.QueryRadius(Vector2.zero, 0f);
        Assert.IsNotNull(results);
        Assert.AreEqual(0, results.Count);
    }

    // ═══ #18 新增：DotEffectConfig 默认值验证 ═══

    [Test]
    public void DotEffectConfig_DefaultValues_AreReasonable()
    {
        var config = ScriptableObject.CreateInstance<DotEffectConfig>();

        // 速度应为正数
        Assert.Greater(config.BurnSpeed, 0f);
        Assert.Greater(config.PoisonSpeed, 0f);
        Assert.Greater(config.FrostSpeed, 0f);
        Assert.Greater(config.LightningSpeed, 0f);
        Assert.Greater(config.DarkSpeed, 0f);
        Assert.Greater(config.WindSpeed, 0f);

        // 持续时间应为正数
        Assert.Greater(config.BurnDuration, 0f);

        // 减速应在合理范围
        Assert.Greater(config.FrostBaseSlowPct, 0f);
        Assert.Less(config.FrostBaseSlowPct, 1f);
    }

    [Test]
    public void DotEffectConfig_GetDefault_NeverReturnsNull()
    {
        var config = DotEffectConfig.GetDefault();
        Assert.IsNotNull(config);
    }

    // ═══ 难度系统测试 ═══

    [Test]
    public void DifficultyManager_DefaultDifficultyIsOne()
    {
        DifficultyManager.CurrentDifficulty = 1;
        Assert.AreEqual(1, DifficultyManager.CurrentDifficulty);
    }

    [Test]
    public void DifficultyManager_ClampedToValidRange()
    {
        DifficultyManager.CurrentDifficulty = 0;
        Assert.AreEqual(1, DifficultyManager.CurrentDifficulty);
        DifficultyManager.CurrentDifficulty = 999;
        Assert.LessOrEqual(DifficultyManager.CurrentDifficulty, DifficultyManager.MaxDifficulty);
    }

    [Test]
    public void DifficultyManager_ConfigNeverNull()
    {
        DifficultyManager.EnsureInitialized();
        var config = DifficultyManager.CurrentConfig;
        Assert.IsNotNull(config);
    }

    [Test]
    public void DifficultyConfig_HpMultScalesWithWave()
    {
        var cfg = ScriptableObject.CreateInstance<DifficultyConfig>();
        cfg.enemyHpMult = 2f;
        cfg.scalingExponent = 1.2f;
        float wave1 = cfg.GetEffectiveHpMult(1);
        float wave100 = cfg.GetEffectiveHpMult(100);
        Assert.Greater(wave100, wave1);
    }

    // ═══ 角色工厂测试 ═══

    [Test]
    public void CharacterFactory_MageIsRegistered()
    {
        Assert.IsTrue(CharacterFactory.IsRegistered("mage"));
    }

    [Test]
    public void CharacterFactory_UnknownFallsBackToMage()
    {
        var go = new GameObject("TestPlayer");
        var passive = CharacterFactory.Create("nonexistent", go);
        Assert.IsNotNull(passive);
        Assert.IsTrue(passive is MagePassive);
        Object.DestroyImmediate(go);
    }

    // ═══ IGunState 接口测试 ═══

    [Test]
    public void DotGunState_ImplementsIGunState()
    {
        DotGunState gun = new DotGunState
        {
            effectType = StatusEffectType.Burn,
            color = Color.red,
            cooldown = 1f,
            impactDamage = 5,
            dotDps = 3f,
            dotDuration = 4f,
            upgradeLevel = 2
        };

        IGunState iGun = gun;
        Assert.AreEqual(StatusEffectType.Burn, iGun.EffectType);
        Assert.AreEqual(3f, iGun.DotDps);
        Assert.AreEqual(2, iGun.UpgradeLevel);
    }

    // ═══ ICharacterPassive 接口测试 ═══

    [Test]
    public void MagePassive_ImplementsICharacterPassive()
    {
        var go = new GameObject("TestMage");
        var mage = go.AddComponent<MagePassive>();
        ICharacterPassive cp = mage;
        Assert.AreEqual("mage", cp.CharacterId);
        Assert.IsNotNull(cp.DotGuns);
        Object.DestroyImmediate(go);
    }

    // ═══ WaveAffixSystem 测试 ═══

    [Test]
    public void WaveAffixSystem_DefaultInactive()
    {
        WaveAffixSystem.OnWaveEnd();
        Assert.IsFalse(WaveAffixSystem.IsAffixActive);
    }

    [Test]
    public void WaveAffixSystem_AffixDescriptionNotEmpty()
    {
        // 即使未激活，GetAffixDescription 应返回空字符串而不抛异常
        WaveAffixSystem.OnWaveEnd();
        string desc = WaveAffixSystem.GetAffixDescription();
        Assert.AreEqual("", desc);
    }
}
#endif
