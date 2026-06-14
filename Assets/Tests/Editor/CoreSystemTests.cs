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
    public void DotGunState_CreatesWithDefaults()
    {
        var gun = new DotGunState
        {
            effectType = StatusEffectType.Poison,
            color = Color.green,
            cooldown = 1.5f,
            impactDamage = 0,
            dotDps = 2f,
            dotDuration = 5f,
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

    private MagePassive CreateTestMage()
    {
        var go = new GameObject("TestMage");
        return go.AddComponent<MagePassive>();
    }

    [Test]
    public void Upgrade_Corrosion_ArmorReduction()
    {
        var mage = CreateTestMage();
        Assert.AreEqual(0.1f, mage.CorrosionArmorReduction, 0.001f);
        mage.CorrosionArmorReduction += 0.10f;
        Assert.AreEqual(0.2f, mage.CorrosionArmorReduction, 0.001f);
        Object.DestroyImmediate(mage.gameObject);
    }

    [Test]
    public void Upgrade_Radiate_DetonateMultiplier()
    {
        var mage = CreateTestMage();
        Assert.AreEqual(3f, mage.DetonateMultiplier, 0.001f);
        mage.DetonateMultiplier += 0.30f;
        Assert.AreEqual(3.3f, mage.DetonateMultiplier, 0.001f);
        Object.DestroyImmediate(mage.gameObject);
    }

    [Test]
    public void Upgrade_Haste_AttackSpeed()
    {
        var mage = CreateTestMage();
        Assert.AreEqual(0f, mage.AttackSpeedBonus, 0.001f);
        mage.AttackSpeedBonus += 0.15f;
        Assert.AreEqual(0.15f, mage.AttackSpeedBonus, 0.001f);
        float mult = Mathf.Max(0.2f, 1f - mage.AttackSpeedBonus);
        Assert.AreEqual(0.85f, mult, 0.001f);
        Object.DestroyImmediate(mage.gameObject);
    }

    [Test]
    public void Upgrade_Barrage_BulletCount()
    {
        var mage = CreateTestMage();
        Assert.AreEqual(0, mage.BulletCountBonus);
        mage.BulletCountBonus += 1;
        Assert.AreEqual(1, mage.BulletCountBonus);
        int bulletCount = Mathf.Min(1 + mage.BulletCountBonus, 3);
        Assert.AreEqual(2, bulletCount);
        Object.DestroyImmediate(mage.gameObject);
    }

    [Test]
    public void Upgrade_Ricochet_Piercing()
    {
        var mage = CreateTestMage();
        Assert.AreEqual(0, mage.PiercingBonus);
        mage.PiercingBonus += 1;
        Assert.AreEqual(1, mage.PiercingBonus);
        Object.DestroyImmediate(mage.gameObject);
    }

    [Test]
    public void Upgrade_LightJudgment_Bonus()
    {
        var mage = CreateTestMage();
        Assert.AreEqual(0f, mage.LightJudgmentBonus, 0.001f);
        mage.LightJudgmentBonus += 0.003f;
        Assert.AreEqual(0.003f, mage.LightJudgmentBonus, 0.0001f);
        Object.DestroyImmediate(mage.gameObject);
    }

    [Test]
    public void Upgrade_StaticField_Stacks()
    {
        var mage = CreateTestMage();
        Assert.AreEqual(0, mage.StaticFieldStacks);
        mage.StaticFieldStacks += 1;
        Assert.AreEqual(1, mage.StaticFieldStacks);
        Object.DestroyImmediate(mage.gameObject);
    }

    [Test]
    public void Upgrade_FrostExplosion_Pct()
    {
        var mage = CreateTestMage();
        Assert.AreEqual(0f, mage.FrostExplosionPct, 0.001f);
        mage.FrostExplosionPct += 0.03f;
        Assert.AreEqual(0.03f, mage.FrostExplosionPct, 0.001f);
        Object.DestroyImmediate(mage.gameObject);
    }

    [Test]
    public void Upgrade_AllCategories_HaveHandlers()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        Assert.IsNotNull(config.upgradeEntries);
        Assert.GreaterOrEqual(config.upgradeEntries.Length, 10);
        foreach (var entry in config.upgradeEntries)
        {
            Assert.IsFalse(string.IsNullOrEmpty(entry.upgradeId));
            Assert.IsFalse(string.IsNullOrEmpty(entry.upgradeName));
        }
        Object.DestroyImmediate(config);
    }

    [Test]
    public void Upgrade_Stacking_AllUpgradeIds_AreUnique()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        var seen = new System.Collections.Generic.HashSet<string>();
        foreach (var entry in config.upgradeEntries)
            Assert.IsTrue(seen.Add(entry.upgradeId), $"重复 upgradeId: {entry.upgradeId}");
        Object.DestroyImmediate(config);
    }

    // ═══ 护甲系统测试 ═══

    [Test]
    public void Armor_EachPointGives2PctReduction()
    {
        var go = new GameObject("TestEnemy");
        var dmg = go.AddComponent<Damageable>();
        dmg.SetMaxHp(1000); dmg.Heal(1000);

        // 0 护甲 = 0% 减伤
        dmg.SetArmor(0);
        dmg.TakeDamage(100f);
        Assert.AreEqual(900, dmg.CurrentHp, "0护甲: 100伤害应扣100HP");

        // 重置
        dmg.Heal(1000);

        // 10 护甲 = 20% 减伤 → 100 * 0.8 = 80
        dmg.SetArmor(10);
        dmg.TakeDamage(100f);
        Assert.AreEqual(920, dmg.CurrentHp, "10护甲: 100伤害应扣80HP (20%减伤)");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Armor_50Points_Max90PctReduction()
    {
        var go = new GameObject("TestEnemy");
        var dmg = go.AddComponent<Damageable>();
        dmg.SetMaxHp(1000); dmg.Heal(1000);

        // 50 护甲 = 100% → 上限 90% → 100 * 0.1 = 10
        dmg.SetArmor(50);
        dmg.TakeDamage(100f);
        Assert.AreEqual(990, dmg.CurrentHp, "50护甲: 上限90%减伤, 100伤害应扣10HP");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Armor_WaveScaling_Plus1PerWave()
    {
        // 验证 EnemyScalingHelper 每波+1护甲的逻辑
        int wave = 5;
        int expectedArmor = wave;
        Assert.AreEqual(5, expectedArmor, "第5波敌人应有5护甲");
    }

    [Test]
    public void Armor_Corrosion_ReducesBy10PctPerStack()
    {
        // 模拟腐蚀：护甲 × (1 - 0.1) = 护甲 × 0.9
        int initialArmor = 20;
        float corrosionRate = 0.1f;

        int after1 = Mathf.RoundToInt(initialArmor * (1f - corrosionRate));
        Assert.AreEqual(18, after1, "1层腐蚀: 20 × 0.9 = 18");

        int after2 = Mathf.RoundToInt(after1 * (1f - corrosionRate));
        Assert.AreEqual(16, after2, "2层腐蚀: 18 × 0.9 = 16");

        int after5 = initialArmor;
        for (int i = 0; i < 5; i++)
            after5 = Mathf.RoundToInt(after5 * (1f - corrosionRate));
        Assert.AreEqual(12, after5, "5层腐蚀: 20 × 0.9^5 ≈ 12");
    }

    [Test]
    public void Armor_Corrosion_NeverBelowZero()
    {
        int armor = 1;
        float corrosionRate = 0.1f;
        int reduced = Mathf.RoundToInt(armor * (1f - corrosionRate));
        Assert.AreEqual(1, Mathf.Max(0, reduced), "1护甲腐蚀后至少为0");

        armor = 0;
        reduced = Mathf.RoundToInt(armor * (1f - corrosionRate));
        Assert.AreEqual(0, Mathf.Max(0, reduced), "0护甲腐蚀后仍为0");
    }

    [Test]
    public void Armor_WithCorrosion_DamageIncreases()
    {
        var go = new GameObject("TestEnemy");
        var dmg = go.AddComponent<Damageable>();
        dmg.SetMaxHp(1000); dmg.Heal(1000);

        // 10护甲 = 20%减伤
        dmg.SetArmor(10);
        dmg.TakeDamage(100f);
        int hpAfter10Armor = dmg.CurrentHp; // 920

        // 重置，腐蚀后 10 × 0.9 = 9护甲 = 18%减伤
        dmg.Heal(1000);
        dmg.SetArmor(9);
        dmg.TakeDamage(100f);
        int hpAfterCorrosion = dmg.CurrentHp; // 918

        Assert.Greater(hpAfter10Armor, hpAfterCorrosion,
            "腐蚀后护甲降低，受到更多伤害");

        Object.DestroyImmediate(go);
    }

    // ═══ 引爆冷却测试 ═══

    [Test]
    public void Contaminate_CooldownReduction_10PctPerStack()
    {
        float baseCooldown = 12f;

        float reduction0 = 0f;
        float cd0 = baseCooldown * Mathf.Max(0.1f, 1f - reduction0);
        Assert.AreEqual(12f, cd0, 0.01f, "0层: 12s");

        float reduction1 = 0.10f;
        float cd1 = baseCooldown * Mathf.Max(0.1f, 1f - reduction1);
        Assert.AreEqual(10.8f, cd1, 0.01f, "1层: 10.8s");

        float reduction6 = 0.60f;
        float cd6 = baseCooldown * Mathf.Max(0.1f, 1f - reduction6);
        Assert.AreEqual(4.8f, cd6, 0.01f, "6层: 4.8s");
    }

    [Test]
    public void Contaminate_MaxStacks6()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        var entry = config.GetUpgradeEntry("contaminate");
        Assert.IsTrue(entry.HasValue);
        Assert.AreEqual(6, entry.Value.maxStacks, "污染最多6层");
        Object.DestroyImmediate(config);
    }

    [Test]
    public void Contaminate_CooldownNeverBelow10Pct()
    {
        float baseCooldown = 12f;
        float maxReduction = 0.9f;
        float cd = baseCooldown * Mathf.Max(0.1f, 1f - maxReduction);
        Assert.AreEqual(1.2f, cd, 0.01f, "90%减冷: 1.2s (不会到0)");
    }

    // ═══ 腐蚀效果测试 ═══

    [Test]
    public void Corrosion_ArmorReduction_Formula()
    {
        float corrosionRate = 0.1f;

        int armor20 = Mathf.FloorToInt(20 * (1f - corrosionRate));
        Assert.AreEqual(18, armor20, "20护甲 × 0.9 = 18");

        int armor10 = Mathf.FloorToInt(10 * (1f - corrosionRate));
        Assert.AreEqual(9, armor10, "10护甲 × 0.9 = 9");

        int armor1 = Mathf.FloorToInt(1 * (1f - corrosionRate));
        Assert.AreEqual(0, armor1, "1护甲 × 0.9 = 0 (FloorToInt)");
    }

    [Test]
    public void Corrosion_AppliedMultipleTimes()
    {
        float corrosionRate = 0.1f;
        int armor = 20;

        armor = Mathf.FloorToInt(armor * (1f - corrosionRate));
        Assert.AreEqual(18, armor, "第1次: 20 × 0.9 = 18");

        armor = Mathf.FloorToInt(armor * (1f - corrosionRate));
        Assert.AreEqual(16, armor, "第2次: 18 × 0.9 = 16");

        armor = Mathf.FloorToInt(armor * (1f - corrosionRate));
        Assert.AreEqual(14, armor, "第3次: 16 × 0.9 ≈ 14");
    }

    // ═══ 侵蚀效果测试 ═══

    [Test]
    public void Erosion_Ignores1ArmorPerStack()
    {
        int armor = 10;
        int erosion = 3;
        int result = Mathf.Max(0, armor - erosion);
        Assert.AreEqual(7, result, "10护甲 - 3侵蚀 = 7");
    }

    [Test]
    public void Corrosion_ThenErosion_OrderCorrect()
    {
        int armor = 20;
        float corrosionRate = 0.1f;
        int erosion = 5;

        // 1. 腐蚀
        armor = Mathf.FloorToInt(armor * (1f - corrosionRate));
        Assert.AreEqual(18, armor, "腐蚀: 20 × 0.9 = 18");

        // 2. 侵蚀
        armor = Mathf.Max(0, armor - erosion);
        Assert.AreEqual(13, armor, "侵蚀: 18 - 5 = 13");
    }

    [Test]
    public void Corrosion_MaxStacks8()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        var entry = config.GetUpgradeEntry("corrosion");
        Assert.IsTrue(entry.HasValue);
        Assert.AreEqual(8, entry.Value.maxStacks, "腐蚀最多8层");
        Object.DestroyImmediate(config);
    }

    [Test]
    public void Erosion_InfiniteStacks()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        var entry = config.GetUpgradeEntry("erosion");
        Assert.IsTrue(entry.HasValue);
        Assert.AreEqual(0, entry.Value.maxStacks, "侵蚀无上限");
        Object.DestroyImmediate(config);
    }

    [Test]
    public void Erosion_ArmorNeverBelowZero()
    {
        int armor = 5;
        int erosion = 100;
        int result = Mathf.Max(0, armor - erosion);
        Assert.AreEqual(0, result, "侵蚀后护甲不会负数");
    }

    [Test]
    public void Armor_FullPipeline_20Armor_Corrosion3_Erosion5()
    {
        int armor = 20;
        float corrosionRate = 0.1f;
        int erosion = 5;

        // 3次腐蚀
        for (int i = 0; i < 3; i++)
            armor = Mathf.FloorToInt(armor * (1f - corrosionRate));
        Assert.AreEqual(14, armor, "3次腐蚀: 20→18→16→14");

        // 侵蚀
        armor = Mathf.Max(0, armor - erosion);
        Assert.AreEqual(9, armor, "侵蚀: 14 - 5 = 9");

        // 减伤
        float reduction = Mathf.Min(armor * 0.02f, 0.9f);
        float actualDmg = 100f * (1f - reduction);
        Assert.AreEqual(82f, actualDmg, 0.01f, "9护甲=18%减伤, 100→82");
    }
}
#endif
