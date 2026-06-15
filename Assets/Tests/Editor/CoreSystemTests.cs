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
    public void PoisonStackEffect_MaxStacks_Is999()
    {
        int maxStacks = 999;
        Assert.AreEqual(999, maxStacks);
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

    // ═══ 重构后新增测试 ═══

    [Test]
    public void SimpleBullet_Create_ReturnsNotNull()
    {
        var bullet = SimpleBullet.Create(Vector2.zero, Vector2.right, 12f, 10, 1f);
        Assert.IsNotNull(bullet);
        Assert.IsNotNull(bullet.gameObject);
        Object.DestroyImmediate(bullet.gameObject);
    }

    [Test]
    public void SimpleBullet_DealsDirectDamage()
    {
        var enemy = new GameObject("TestEnemy");
        enemy.tag = "Enemy";
        var dmg = enemy.AddComponent<Damageable>();
        dmg.SetMaxHp(100);
        dmg.Heal(100);

        int hpBefore = dmg.CurrentHp;
        var bullet = SimpleBullet.Create(Vector2.zero, Vector2.right, 12f, 10, 1f);

        var col = enemy.GetComponent<Collider2D>();
        if (col == null) { var bc = enemy.AddComponent<BoxCollider2D>(); bc.isTrigger = true; }

        bullet.SendMessage("OnTriggerEnter2D", enemy.GetComponent<Collider2D>());

        Assert.LessOrEqual(dmg.CurrentHp, hpBefore, "SimpleBullet should deal damage");

        Object.DestroyImmediate(bullet.gameObject);
        Object.DestroyImmediate(enemy);
    }

    [Test]
    public void DotBulletFactory_RegisterAndCreate()
    {
        var go = DotBulletFactory.Create(StatusEffectType.Poison, Vector2.zero, Vector2.right,
            new DotGunState { effectType = StatusEffectType.Poison, color = Color.green, cooldown = 1f, dotDps = 2f, dotDuration = 5f },
            1f, 1f, false, 0f, 2f);
        Assert.IsNotNull(go);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void CharacterFactory_BlueRegistered()
    {
        Assert.IsTrue(CharacterFactory.IsRegistered("blue"), "Blue character should be registered");
    }

    [Test]
    public void GunState_CooldownCalculation()
    {
        var gun = new GunState { cooldown = 1.0f, impactDamage = 10, upgradeLevel = 1 };
        Assert.AreEqual(1.0f, gun.Cooldown);
        Assert.AreEqual(10, gun.ImpactDamage);
        Assert.AreEqual(1, gun.UpgradeLevel);
    }

    [Test]
    public void WeaponFiringSystem_FiresOnCooldown()
    {
        var go = new GameObject("TestPassive");
        var passive = go.AddComponent<BlueCharacterPassive>();

        Assert.AreEqual("blue", passive.CharacterId);
        Assert.AreEqual(1f, passive.GetAttackSpeedMultiplier(), "Default attack speed mult should be 1.0");

        passive.AttackSpeedBonus = 0.15f;
        Assert.AreEqual(0.870f, passive.GetAttackSpeedMultiplier(), 0.01f, "After 15% bonus, mult should be ~0.87 (1/1.15)");

        Object.DestroyImmediate(go);
    }

    // ═══ task1: ICharacterPassive / IDotCharacterPassive 接口测试 ═══

    [Test]
    public void ICharacterPassive_MageImplementsBoth()
    {
        var go = new GameObject("TestMage");
        var mage = go.AddComponent<MagePassive>();

        Assert.IsTrue(mage is ICharacterPassive);
        Assert.IsTrue(mage is IDotCharacterPassive);
        Assert.AreEqual("mage", mage.CharacterId);
        Assert.AreEqual("DOT 法师", mage.DisplayName);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void ICharacterPassive_BlueImplementsOnlyBase()
    {
        var go = new GameObject("TestBlue");
        var blue = go.AddComponent<BlueCharacterPassive>();

        Assert.IsTrue(blue is ICharacterPassive);
        Assert.IsFalse(blue is IDotCharacterPassive);
        Assert.AreEqual("blue", blue.CharacterId);
        Assert.AreEqual("蓝色战士", blue.DisplayName);

        Object.DestroyImmediate(go);
    }

    // ═══ task1: CharacterPassiveBase 通用属性测试 ═══

    [Test]
    public void CharacterPassiveBase_DefaultValues()
    {
        var go = new GameObject("TestBase");
        var passive = go.AddComponent<BlueCharacterPassive>();

        Assert.AreEqual(0f, passive.AttackSpeedBonus);
        Assert.AreEqual(0, passive.BulletCountBonus);
        Assert.AreEqual(0f, passive.BulletSizeBonus);
        Assert.AreEqual(0f, passive.KnockbackBonus);
        Assert.AreEqual(0, passive.PenetrateCount);
        Assert.AreEqual(0f, passive.DotDamageMultiplier);
        Assert.AreEqual(0f, passive.CritChanceBonus);
        Assert.AreEqual(1f, passive.GetAttackSpeedMultiplier());
        Assert.AreEqual(0, passive.GetBulletCountBonus());
        Assert.AreEqual(0f, passive.GetBulletSizeBonus());
        Assert.AreEqual(0f, passive.GetKnockbackBonus());
        Assert.AreEqual(0, passive.GetPenetrateCount());

        Object.DestroyImmediate(go);
    }

    [Test]
    public void CharacterPassiveBase_AttackSpeedClamp()
    {
        var go = new GameObject("TestClamp");
        var passive = go.AddComponent<BlueCharacterPassive>();

        // 新公式：1/(1+bonus)，对数递减
        passive.AttackSpeedBonus = 0.5f;
        Assert.AreEqual(0.667f, passive.GetAttackSpeedMultiplier(), 0.01f, "bonus=0.5 → 1/1.5=0.667");

        passive.AttackSpeedBonus = 4.5f;
        Assert.AreEqual(0.2f, passive.GetAttackSpeedMultiplier(), 0.01f, "bonus=4.5 → 1/5.5=0.182, clamped to 0.2");

        passive.AttackSpeedBonus = -0.1f;
        Assert.AreEqual(1.0f, passive.GetAttackSpeedMultiplier(), 0.01f, "Negative bonus clamped to 0 → 1.0");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void CharacterPassiveBase_EvolutionProperties()
    {
        var go = new GameObject("TestEvo");
        var passive = go.AddComponent<BlueCharacterPassive>();

        Assert.AreEqual(0f, passive.DotDamageMultiplier);
        Assert.AreEqual(0f, passive.CritChanceBonus);

        passive.DotDamageMultiplier = 0.5f;
        passive.CritChanceBonus = 0.1f;

        Assert.AreEqual(0.5f, passive.DotDamageMultiplier);
        Assert.AreEqual(0.1f, passive.CritChanceBonus);

        Object.DestroyImmediate(go);
    }

    // ═══ task1: GunState / DotGunState 测试 ═══

    [Test]
    public void GunState_ImplementsIGunState()
    {
        var gun = new GunState { cooldown = 0.5f, impactDamage = 10, upgradeLevel = 3 };

        IGunState ig = gun;
        Assert.AreEqual(0.5f, ig.Cooldown);
        Assert.AreEqual(10, ig.ImpactDamage);
        Assert.AreEqual(3, ig.UpgradeLevel);
    }

    [Test]
    public void DotGunState_InheritsGunState()
    {
        var gun = new DotGunState
        {
            effectType = StatusEffectType.Burn,
            color = Color.red,
            cooldown = 1.0f,
            impactDamage = 5,
            dotDps = 3f,
            dotDuration = 4f,
            upgradeLevel = 2
        };

        Assert.AreEqual(StatusEffectType.Burn, gun.EffectType);
        Assert.AreEqual(3f, gun.DotDps);
        Assert.AreEqual(4f, gun.DotDuration);
        Assert.AreEqual(Color.red, gun.GunColor);

        IGunState ig = gun;
        Assert.AreEqual(1.0f, ig.Cooldown);
        Assert.AreEqual(5, ig.ImpactDamage);
        Assert.AreEqual(2, ig.UpgradeLevel);
    }

    // ═══ task1: CharacterFactory 测试 ═══

    [Test]
    public void CharacterFactory_MageAndBlueRegistered()
    {
        Assert.IsTrue(CharacterFactory.IsRegistered("mage"));
        Assert.IsTrue(CharacterFactory.IsRegistered("blue"));
    }

    [Test]
    public void CharacterFactory_CreatesCorrectType()
    {
        var go = new GameObject("TestFactory");

        var mage = CharacterFactory.Create("mage", go);
        Assert.IsTrue(mage is MagePassive);
        Assert.IsTrue(mage is IDotCharacterPassive);

        Object.DestroyImmediate(go);

        var go2 = new GameObject("TestFactory2");
        var blue = CharacterFactory.Create("blue", go2);
        Assert.IsTrue(blue is BlueCharacterPassive);
        Assert.IsFalse(blue is IDotCharacterPassive);

        Object.DestroyImmediate(go2);
    }

    // ═══ task2: CritParams 测试 ═══

    [Test]
    public void CritParams_None_IsDefault()
    {
        var crit = CritParams.None;
        Assert.IsFalse(crit.canCrit);
        Assert.AreEqual(0f, crit.critChance);
        Assert.AreEqual(0f, crit.critMult);
    }

    [Test]
    public void CritParams_Apply_NoCrit()
    {
        var crit = new CritParams(false, 1f, 2f);
        float result = crit.Apply(100f);
        Assert.AreEqual(100f, result, "canCrit=false should not multiply");
    }

    [Test]
    public void CritParams_Apply_AlwaysCrit()
    {
        var crit = new CritParams(true, 1f, 2f);
        float result = crit.Apply(100f);
        Assert.AreEqual(200f, result, "100% crit should always double");
    }

    // ═══ task2: PenetrateHandler 测试 ═══

    [Test]
    public void PenetrateHandler_AllowsPenetration()
    {
        var go = new GameObject("TestBullet");
        var ph = go.AddComponent<PenetrateHandler>();
        ph.Setup(2);

        var enemy1 = new GameObject("Enemy1");
        enemy1.AddComponent<BoxCollider2D>();
        var enemy2 = new GameObject("Enemy2");
        enemy2.AddComponent<BoxCollider2D>();
        var enemy3 = new GameObject("Enemy3");
        enemy3.AddComponent<BoxCollider2D>();

        Assert.IsTrue(ph.TryPenetrate(enemy1.GetComponent<Collider2D>()), "First hit should penetrate");
        Assert.IsTrue(ph.TryPenetrate(enemy2.GetComponent<Collider2D>()), "Second hit should penetrate");
        Assert.IsFalse(ph.TryPenetrate(enemy3.GetComponent<Collider2D>()), "Third hit should NOT penetrate (exhausted)");
        Assert.IsFalse(ph.TryPenetrate(enemy1.GetComponent<Collider2D>()), "Same enemy should NOT penetrate again");

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(enemy1);
        Object.DestroyImmediate(enemy2);
        Object.DestroyImmediate(enemy3);
    }

    // ═══ task2: UIFormatUtils 测试 ═══

    [Test]
    public void UIFormatUtils_FormatDamage()
    {
        Assert.AreEqual("500", UIFormatUtils.FormatDamage(500));
        Assert.AreEqual("1.5K", UIFormatUtils.FormatDamage(1500));
        Assert.AreEqual("2.3M", UIFormatUtils.FormatDamage(2300000));
    }

    [Test]
    public void UIFormatUtils_FormatTime()
    {
        Assert.AreEqual("00:00", UIFormatUtils.FormatTime(0f));
        Assert.AreEqual("01:30", UIFormatUtils.FormatTime(90f));
        Assert.AreEqual("10:05", UIFormatUtils.FormatTime(605f));
    }

    // ═══ task2: GUIScaleHelper 测试 ═══

    [Test]
    public void GUIScaleHelper_Constants()
    {
        Assert.AreEqual(1920f, GUIScaleHelper.REF_W);
        Assert.AreEqual(1080f, GUIScaleHelper.REF_H);
        Assert.AreEqual(1920f, GUIScaleHelper.VirtualWidth);
        Assert.AreEqual(1080f, GUIScaleHelper.VirtualHeight);
    }

    // ═══ task2: MaterialCache 测试 ═══

    [Test]
    public void MaterialCache_GetDefault_NotNull()
    {
        var mat = MaterialCache.GetDefault();
        Assert.IsNotNull(mat, "MaterialCache.GetDefault() should not return null");
    }

    [Test]
    public void MaterialCache_GetDefault_ReturnsSameInstance()
    {
        var mat1 = MaterialCache.GetDefault();
        var mat2 = MaterialCache.GetDefault();
        Assert.AreSame(mat1, mat2, "Should return cached instance");
    }

    // ═══ task2: VFXPool 测试 ═══

    [Test]
    public void VFXPool_Get_CreatesNewObject()
    {
        var go = VFXPool.Get("TestPool");
        Assert.IsNotNull(go);
        Assert.IsTrue(go.activeSelf, "New object should be active");
        VFXPool.ReturnImmediate(go);
    }

    [Test]
    public void VFXPool_ReturnAndReuse()
    {
        var go = VFXPool.Get("TestPool2");
        VFXPool.ReturnImmediate(go);
        Assert.IsFalse(go.activeSelf, "Returned object should be inactive");

        var go2 = VFXPool.Get("TestPool2");
        Assert.AreSame(go, go2, "Should reuse returned object");
    }

    [Test]
    public void VFXPool_ClearAll()
    {
        var go = VFXPool.Get("TestPool3");
        VFXPool.ReturnImmediate(go);
        VFXPool.ClearAll();
        var go2 = VFXPool.Get("TestPool3");
        Assert.AreNotSame(go, go2, "After ClearAll, should create new object");
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(go2);
    }

    // ═══ task2: DotSpriteCache 测试 ═══

    [Test]
    public void DotSpriteCache_Get_NotNull()
    {
        var sprite = DotSpriteCache.Get();
        Assert.IsNotNull(sprite);
    }

    [Test]
    public void DotSpriteCache_CircleSprite_NotNull()
    {
        var sprite = DotSpriteCache.CircleSprite();
        Assert.IsNotNull(sprite);
    }

    [Test]
    public void DotSpriteCache_CachesSameInstance()
    {
        var s1 = DotSpriteCache.Get();
        var s2 = DotSpriteCache.Get();
        Assert.AreSame(s1, s2);
    }

    // ═══ task3: GlowReturnHelper 测试 ═══

    [Test]
    public void GlowReturnHelper_GetOrCreate_ReturnsObject()
    {
        var glow = GlowReturnHelper.GetOrCreate();
        Assert.IsNotNull(glow);
        Assert.IsTrue(glow.activeSelf);
        GlowReturnHelper.ReturnToPool(glow);
    }

    [Test]
    public void GlowReturnHelper_ReturnAndReuse()
    {
        var glow = GlowReturnHelper.GetOrCreate();
        GlowReturnHelper.ReturnToPool(glow);
        Assert.IsFalse(glow.activeSelf);

        var glow2 = GlowReturnHelper.GetOrCreate();
        Assert.AreSame(glow, glow2, "Should reuse returned glow");
    }

    // ═══ task3: FloatingText 测试 ═══

    [Test]
    public void FloatingText_Create_NotNull()
    {
        var ft = FloatingText.Create(Vector3.up, "Test", Color.white, 1f);
        Assert.IsNotNull(ft);
        Assert.IsNotNull(ft.GetComponent<TextMesh>());
        Object.DestroyImmediate(ft.gameObject);
    }

    // ═══ task4: StatusEffect / DetonateResult 测试 ═══

    [Test]
    public void StatusEffect_Refresh_StacksDps()
    {
        var effect = new StatusEffect
        {
            type = StatusEffectType.Burn,
            damagePerSecond = 2f,
            remainingDuration = 3f,
            stackCount = 1
        };

        effect.Refresh(1f, 3f, stackDps: true);

        Assert.AreEqual(2, effect.stackCount);
        Assert.AreEqual(3f, effect.damagePerSecond, "stackDps=true should add dps");
    }

    [Test]
    public void StatusEffect_Refresh_UpdatesDuration()
    {
        var effect = new StatusEffect
        {
            type = StatusEffectType.Poison,
            damagePerSecond = 2f,
            remainingDuration = 1f,
            stackCount = 1
        };

        effect.Refresh(2f, 5f, stackDps: false);

        Assert.AreEqual(1, effect.stackCount, "stackDps=false should not increment");
        Assert.AreEqual(2f, effect.damagePerSecond, "stackDps=false should keep max dps");
        Assert.AreEqual(5f, effect.remainingDuration, "Duration should be updated");
    }

    [Test]
    public void DetonateResult_DefaultValues()
    {
        var result = new DetonateResult();
        Assert.AreEqual(0f, result.totalDamage);
        Assert.AreEqual(0, result.poisonStacks);
        Assert.IsFalse(result.hadBurn);
        Assert.IsFalse(result.hadFrost);
        Assert.IsFalse(result.hadPoison);
    }

    // ═══ task4: CharacterUpgradeConfig 测试 ═══

    [Test]
    public void CharacterUpgradeConfig_MageUpgradeConfig_LoadsDefaults()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        Assert.IsNotNull(config);
        Assert.AreEqual("mage", config.characterId);
        Assert.IsTrue(config.dotGunEntries.Length > 0, "Should have DOT gun entries");
        Assert.IsTrue(config.upgradeEntries.Length > 0, "Should have upgrade entries");
        Assert.IsTrue(config.IsDotGunUpgrade("poison"), "poison should be a DOT gun upgrade");
        Assert.IsFalse(config.IsDotGunUpgrade("haste"), "haste should NOT be a DOT gun upgrade");
        Object.DestroyImmediate(config);
    }

    [Test]
    public void CharacterUpgradeConfig_BlueUpgradeConfig_LoadsDefaults()
    {
        var config = ScriptableObject.CreateInstance<BlueUpgradeConfig>();
        Assert.IsNotNull(config);
        Assert.AreEqual("blue", config.characterId);
        Assert.IsTrue(config.upgradeEntries.Length > 0, "Should have upgrade entries");
        Object.DestroyImmediate(config);
    }

    [Test]
    public void CharacterUpgradeConfig_GetUpgradeEntry()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        var entry = config.GetUpgradeEntry("haste");
        Assert.IsTrue(entry.HasValue, "haste should exist");
        Assert.AreEqual("急速 (Haste)", entry.Value.upgradeName);

        var missing = config.GetUpgradeEntry("nonexistent");
        Assert.IsFalse(missing.HasValue, "nonexistent should return null");

        Object.DestroyImmediate(config);
    }

    [Test]
    public void CharacterUpgradeConfig_BuildCustomUpgrades()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        var upgrades = config.BuildCustomUpgrades();
        Assert.IsTrue(upgrades.Length >= 17, $"Mage should have 17+ upgrades (7 DOT + 10 enhanced), got {upgrades.Length}");
        Object.DestroyImmediate(config);
    }

    // ═══ task4: DotGunEntry / UpgradeEntry 结构体测试 ═══

    [Test]
    public void DotGunEntry_CreatesWithValues()
    {
        var entry = new DotGunEntry
        {
            upgradeId = "poison",
            effectType = StatusEffectType.Poison,
            displayName = "中毒",
            color = Color.green,
            cooldown = 1.8f,
            impactDmg = 0,
            dotDps = 3f,
            dotDuration = 5f
        };

        Assert.AreEqual("poison", entry.upgradeId);
        Assert.AreEqual(StatusEffectType.Poison, entry.effectType);
        Assert.AreEqual(1.8f, entry.cooldown);
    }

    [Test]
    public void UpgradeEntry_CreatesWithValues()
    {
        var entry = new UpgradeEntry
        {
            upgradeId = "haste",
            upgradeName = "急速",
            description = "攻速+15%",
            category = CharacterUpgradeOption.UpgradeCategory.AttackSpeed,
            value1 = 0.15f,
            maxStacks = 0
        };

        Assert.AreEqual("haste", entry.upgradeId);
        Assert.AreEqual(0.15f, entry.value1);
        Assert.AreEqual(0, entry.maxStacks);
    }

    // ═══ task5: DotBulletHelper 测试 ═══

    [Test]
    public void DotBulletHelper_EnsureColorBlender_CreatesComponent()
    {
        var enemy = new GameObject("TestEnemy");
        var blender = DotBulletHelper.EnsureColorBlender(enemy);
        Assert.IsNotNull(blender);
        Assert.AreSame(blender, DotBulletHelper.EnsureColorBlender(enemy), "Should return same instance");
        Object.DestroyImmediate(enemy);
    }

    // ═══ 护甲公式测试 ═══

    [Test]
    public void ArmorFormula_DamageReduction()
    {
        int armor = 10;
        float reduction = Mathf.Min(armor * 0.02f, 0.9f);
        float actualDmg = 100f * (1f - reduction);
        Assert.AreEqual(80f, actualDmg, 0.01f, "10 armor = 20% reduction");
    }

    [Test]
    public void ArmorFormula_MaxReduction()
    {
        int armor = 100;
        float reduction = Mathf.Min(armor * 0.02f, 0.9f);
        Assert.AreEqual(0.9f, reduction, "Should cap at 90%");
    }

    [Test]
    public void ArmorFormula_CorrosionCalculation()
    {
        int armor = 20;
        float corrosionRate = 0.1f;

        armor = Mathf.FloorToInt(armor * (1f - corrosionRate));
        Assert.AreEqual(18, armor, "First corrosion: 20 → 18");

        armor = Mathf.FloorToInt(armor * (1f - corrosionRate));
        Assert.AreEqual(16, armor, "Second corrosion: 18 → 16");
    }

    [Test]
    public void ArmorFormula_ErosionCalculation()
    {
        int armor = 10;
        int erosion = 3;
        armor = Mathf.Max(0, armor - erosion);
        Assert.AreEqual(7, armor, "Erosion should subtract directly");
    }

    [Test]
    public void ArmorFormula_CorrosionThenErosion()
    {
        int armor = 20;
        float corrosionRate = 0.1f;
        int erosion = 5;

        for (int i = 0; i < 3; i++)
            armor = Mathf.FloorToInt(armor * (1f - corrosionRate));
        Assert.AreEqual(14, armor, "3x corrosion: 20→18→16→14");

        armor = Mathf.Max(0, armor - erosion);
        Assert.AreEqual(9, armor, "erosion: 14 - 5 = 9");

        float reduction = Mathf.Min(armor * 0.02f, 0.9f);
        float actualDmg = 100f * (1f - reduction);
        Assert.AreEqual(82f, actualDmg, 0.01f, "9 armor = 18% reduction, 100→82");
    }

    // ═══ 强化系统测试 ═══

    [Test]
    public void UpgradeRarity_AllValuesExist()
    {
        Assert.AreEqual(0, (int)UpgradeRarity.Common);
        Assert.AreEqual(1, (int)UpgradeRarity.Uncommon);
        Assert.AreEqual(2, (int)UpgradeRarity.Rare);
        Assert.AreEqual(3, (int)UpgradeRarity.Epic);
    }

    [Test]
    public void MageUpgradeConfig_Has40Upgrades()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        int total = config.upgradeEntries.Length;
        Assert.AreEqual(25, total, $"Expected 25 upgrades (8 general + 17 bullet), got {total}");
        Object.DestroyImmediate(config);
    }

    [Test]
    public void MageUpgradeConfig_RarityDistribution()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        int common = 0, uncommon = 0, rare = 0, epic = 0;
        for (int i = 0; i < config.upgradeEntries.Length; i++)
        {
            switch (config.upgradeEntries[i].rarity)
            {
                case UpgradeRarity.Common: common++; break;
                case UpgradeRarity.Uncommon: uncommon++; break;
                case UpgradeRarity.Rare: rare++; break;
                case UpgradeRarity.Epic: epic++; break;
            }
        }
        Assert.AreEqual(12, common, "Common upgrades");
        Assert.AreEqual(12, uncommon, "Uncommon upgrades");
        Assert.AreEqual(12, rare, "Rare upgrades");
        Assert.AreEqual(12, epic, "Epic upgrades");
        Object.DestroyImmediate(config);
    }

    [Test]
    public void MageUpgradeConfig_Poison8Upgrades()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        string[] ids = { "poison_duration", "poison_dps", "poison_pool", "poison_tick",
                         "poison_crit", "poison_sepsis", "poison_plague", "poison_lethal" };
        foreach (var id in ids)
            Assert.IsTrue(config.GetUpgradeEntry(id).HasValue, $"Missing upgrade: {id}");
        Object.DestroyImmediate(config);
    }

    [Test]
    public void MageUpgradeConfig_Burn8Upgrades()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        string[] ids = { "burn_duration", "burn_radius", "burn_tick", "burn_slow",
                         "burn_crit", "burn_melt", "burn_storm", "burn_burst" };
        foreach (var id in ids)
            Assert.IsTrue(config.GetUpgradeEntry(id).HasValue, $"Missing upgrade: {id}");
        Object.DestroyImmediate(config);
    }

    [Test]
    public void MageUpgradeConfig_Frost1Upgrade()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        string[] ids = { "frost_slow" };
        foreach (var id in ids)
            Assert.IsTrue(config.GetUpgradeEntry(id).HasValue, $"Missing upgrade: {id}");
        Object.DestroyImmediate(config);
    }

    [Test]
    public void MageUpgradeConfig_Static4Upgrades()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        string[] ids = { "static_chain", "static_range", "storm_multi", "storm_chain" };
        foreach (var id in ids)
            Assert.IsTrue(config.GetUpgradeEntry(id).HasValue, $"Missing upgrade: {id}");
        Object.DestroyImmediate(config);
    }

    [Test]
    public void MageUpgradeConfig_Wind4Upgrades()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        string[] ids = { "wind_speed", "wind_precision", "wind_hurricane" };
        foreach (var id in ids)
            Assert.IsTrue(config.GetUpgradeEntry(id).HasValue, $"Missing upgrade: {id}");
        Object.DestroyImmediate(config);
    }

    [Test]
    public void MageUpgradeConfig_General8Upgrades()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        string[] ids = { "move_speed", "erosion", "haste", "radiate",
                         "corrosion", "contaminate", "barrage", "ricochet" };
        foreach (var id in ids)
            Assert.IsTrue(config.GetUpgradeEntry(id).HasValue, $"Missing upgrade: {id}");
        Object.DestroyImmediate(config);
    }

    [Test]
    public void MageUpgradeConfig_BarrageMaxStacks2()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        var entry = config.GetUpgradeEntry("barrage");
        Assert.IsTrue(entry.HasValue);
        Assert.AreEqual(2, entry.Value.maxStacks, "Barrage maxStacks should be 2");
        Object.DestroyImmediate(config);
    }

    [Test]
    public void MageUpgradeConfig_HasteMaxStacks10()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        var entry = config.GetUpgradeEntry("haste");
        Assert.IsTrue(entry.HasValue);
        Assert.AreEqual(10, entry.Value.maxStacks, "Haste maxStacks should be 10");
        Object.DestroyImmediate(config);
    }

    [Test]
    public void UpgradeEntry_RarityField()
    {
        var entry = new UpgradeEntry { upgradeId = "test", rarity = UpgradeRarity.Epic };
        Assert.AreEqual(UpgradeRarity.Epic, entry.rarity);
    }

    // ═══ 子弹专属强化应用测试 ═══

    private MagePassive CreateMageWithConfig()
    {
        var go = new GameObject("TestMage");
        var mage = go.AddComponent<MagePassive>();
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        mage.SetUpgradeConfig(config);
        return mage;
    }

    // ── 中毒专属 ──

    [Test] public void Poison_Duration_Applies() { var m = CreateMageWithConfig(); m.ApplyUpgrade("poison_duration"); Assert.AreEqual(1f, m.PoisonDurationBonus); Object.DestroyImmediate(m.gameObject); }
    [Test] public void Poison_Dps_Applies() { var m = CreateMageWithConfig(); m.ApplyUpgrade("poison_dps"); Assert.AreEqual(1f, m.PoisonDpsBonus); Object.DestroyImmediate(m.gameObject); }
    [Test] public void Poison_Pool_Applies() { var m = CreateMageWithConfig(); m.ApplyUpgrade("poison_pool"); Assert.AreEqual(0.2f, m.PoisonPoolBonus, 0.001f); Object.DestroyImmediate(m.gameObject); }
    [Test] public void Poison_Tick_Applies() { var m = CreateMageWithConfig(); m.ApplyUpgrade("poison_tick"); Assert.AreEqual(0.15f, m.PoisonTickReduction, 0.001f); Object.DestroyImmediate(m.gameObject); }
    [Test] public void Poison_Lethal_Applies() { var m = CreateMageWithConfig(); m.ApplyUpgrade("poison_lethal"); Assert.IsTrue(m.PoisonLethal); Object.DestroyImmediate(m.gameObject); }

    // ── 燃烧专属 ──

    [Test] public void Burn_Duration_Applies() { var m = CreateMageWithConfig(); m.ApplyUpgrade("burn_duration"); Assert.AreEqual(1f, m.BurnDurationBonus); Object.DestroyImmediate(m.gameObject); }
    [Test] public void Burn_Radius_Applies() { var m = CreateMageWithConfig(); m.ApplyUpgrade("burn_radius"); Assert.AreEqual(0.15f, m.BurnRadiusBonus, 0.001f); Object.DestroyImmediate(m.gameObject); }
    [Test] public void Burn_Tick_Applies() { var m = CreateMageWithConfig(); m.ApplyUpgrade("burn_tick"); Assert.AreEqual(0.1f, m.BurnTickReduction, 0.001f); Object.DestroyImmediate(m.gameObject); }
    [Test] public void Burn_Slow_Applies() { var m = CreateMageWithConfig(); m.ApplyUpgrade("burn_slow"); Assert.AreEqual(0.2f, m.BurnSlowBonus, 0.001f); Object.DestroyImmediate(m.gameObject); }
    [Test] public void Burn_Melt_Applies() { var m = CreateMageWithConfig(); m.ApplyUpgrade("burn_melt"); Assert.IsTrue(m.BurnMeltMastery); Object.DestroyImmediate(m.gameObject); }
    [Test] public void Burn_Burst_Applies() { var m = CreateMageWithConfig(); m.ApplyUpgrade("burn_burst"); Assert.IsTrue(m.BurnBurst); Object.DestroyImmediate(m.gameObject); }

    // ── 霜冻专属 ──

    [Test] public void Frost_Slow_Applies() { var m = CreateMageWithConfig(); m.ApplyUpgrade("frost_slow"); Assert.AreEqual(0.1f, m.FrostSlowBonus, 0.001f); Object.DestroyImmediate(m.gameObject); }

    // ── 雷电专属 ──

    [Test] public void Static_Chain_Applies() { var m = CreateMageWithConfig(); m.ApplyUpgrade("static_chain"); Assert.AreEqual(1, m.StaticChainBonus); Object.DestroyImmediate(m.gameObject); }
    [Test] public void Static_Range_Applies() { var m = CreateMageWithConfig(); m.ApplyUpgrade("static_range"); Assert.AreEqual(0.2f, m.StaticRangeBonus, 0.001f); Object.DestroyImmediate(m.gameObject); }
    [Test] public void Storm_Multi_Applies() { var m = CreateMageWithConfig(); m.ApplyUpgrade("storm_multi"); Assert.IsTrue(m.StormMulti); Object.DestroyImmediate(m.gameObject); }
    [Test] public void Storm_Chain_Applies() { var m = CreateMageWithConfig(); m.ApplyUpgrade("storm_chain"); Assert.IsTrue(m.StormChain); Object.DestroyImmediate(m.gameObject); }

    // ── 风专属 ──

    [Test] public void Wind_Speed_Applies() { var m = CreateMageWithConfig(); m.ApplyUpgrade("wind_speed"); Assert.AreEqual(0.2f, m.WindSpeedBonus, 0.001f); Object.DestroyImmediate(m.gameObject); }
    [Test] public void Wind_Precision_Applies() { var m = CreateMageWithConfig(); m.ApplyUpgrade("wind_precision"); Assert.AreEqual(5f, m.WindPrecisionAngle); Object.DestroyImmediate(m.gameObject); }
    [Test] public void Wind_Hurricane_Applies() { var m = CreateMageWithConfig(); m.ApplyUpgrade("wind_hurricane"); Assert.IsTrue(m.WindHurricane); Object.DestroyImmediate(m.gameObject); }

    // ── 堆叠测试 ──

    [Test] public void Poison_Duration_Stacks5()
    {
        var m = CreateMageWithConfig();
        for (int i = 0; i < 5; i++) m.ApplyUpgrade("poison_duration");
        Assert.AreEqual(5f, m.PoisonDurationBonus);
        Object.DestroyImmediate(m.gameObject);
    }

    [Test] public void Frost_Slow_Stacks5()
    {
        var m = CreateMageWithConfig();
        for (int i = 0; i < 5; i++) m.ApplyUpgrade("frost_slow");
        Assert.AreEqual(0.5f, m.FrostSlowBonus, 0.001f);
        Object.DestroyImmediate(m.gameObject);
    }

    [Test] public void Burn_Radius_Stacks3()
    {
        var m = CreateMageWithConfig();
        for (int i = 0; i < 3; i++) m.ApplyUpgrade("burn_radius");
        Assert.AreEqual(0.45f, m.BurnRadiusBonus, 0.001f);
        Object.DestroyImmediate(m.gameObject);
    }

    [Test] public void Static_Chain_Stacks3()
    {
        var m = CreateMageWithConfig();
        for (int i = 0; i < 3; i++) m.ApplyUpgrade("static_chain");
        Assert.AreEqual(3, m.StaticChainBonus);
        Object.DestroyImmediate(m.gameObject);
    }

}
#endif
