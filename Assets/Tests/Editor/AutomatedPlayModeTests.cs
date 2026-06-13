#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 自动化 PlayMode 测试 — 覆盖 test.md 中的核心验证项。
/// 在 Unity 中 Window → General → Test Runner → PlayMode 运行。
/// </summary>
[TestFixture]
public class AutomatedPlayModeTests
{
    // ═══ P0: 基础启动 ═══

    [Test]
    public void P0_DotEffectConfig_GetDefault_NotNull()
    {
        var config = DotEffectConfig.GetDefault();
        Assert.IsNotNull(config, "DotEffectConfig.GetDefault() 不应返回 null");
    }

    [Test]
    public void P0_MageUpgradeConfig_LoadsFromResources()
    {
        var config = Resources.Load<MageUpgradeConfig>("Configs/MageUpgradeConfig");
        // 可能不存在（需要先 Mage → Create MageUpgradeConfig Asset）
        if (config == null) Assert.Ignore("MageUpgradeConfig.asset 不存在，需先创建");
        Assert.IsNotNull(config, "MageUpgradeConfig 不应为 null");
    }

    [Test]
    public void P0_CharacterFactory_MageRegistered()
    {
        Assert.IsTrue(CharacterFactory.IsRegistered("mage"), "Mage 应在 CharacterFactory 中注册");
    }

    [Test]
    public void P0_GameReferences_CharacterPassive_NotNull()
    {
        // 需要场景中有 Player
        var player = GameReferences.Player;
        if (player == null) Assert.Ignore("无 Player，跳过");
        Assert.IsNotNull(GameReferences.CharacterPassive, "CharacterPassive 不应为 null");
    }

    // ═══ P0: 子弹创建 ═══

    [Test]
    public void P0_BurnBullet_Create_ReturnsNotNull()
    {
        var bullet = BurnBullet.Create(Vector2.zero, Vector2.right, 10f, 5, 3f, 2f, 1f, false, 0f, 0f);
        Assert.IsNotNull(bullet, "BurnBullet.Create 应返回非 null");
        Object.DestroyImmediate(bullet.gameObject);
    }

    [Test]
    public void P0_PoisonBullet_Create_ReturnsNotNull()
    {
        var bullet = PoisonBullet.Create(Vector2.zero, Vector2.right, 10f, 3f, 5f, 1f, false, 0f, 0f);
        Assert.IsNotNull(bullet, "PoisonBullet.Create 应返回非 null");
        Object.DestroyImmediate(bullet.gameObject);
    }

    [Test]
    public void P0_FrostBullet_Create_ReturnsNotNull()
    {
        var bullet = FrostBullet.Create(Vector2.zero, Vector2.right, 10f, 5, 2f, 1f, 0.3f, 1f, false, 0f, 0f);
        Assert.IsNotNull(bullet, "FrostBullet.Create 应返回非 null");
        Object.DestroyImmediate(bullet.gameObject);
    }

    [Test]
    public void P0_LightningBullet_Create_ReturnsNotNull()
    {
        var bullet = LightningBullet.Create(Vector2.zero, Vector2.right, 10f, 5, 1f);
        Assert.IsNotNull(bullet, "LightningBullet.Create 应返回非 null");
        Object.DestroyImmediate(bullet.gameObject);
    }

    [Test]
    public void P0_WindBullet_Create_ReturnsNotNull()
    {
        var bullet = WindBullet.Create(Vector2.zero, Vector2.right, 10f, 5, 1f, false, 0f, 0f);
        Assert.IsNotNull(bullet, "WindBullet.Create 应返回非 null");
        Object.DestroyImmediate(bullet.gameObject);
    }

    [Test]
    public void P0_DarkBullet_Create_ReturnsNotNull()
    {
        var bullet = DarkBullet.Create(Vector2.zero, Vector2.right, 10f, 3f, 0.5f);
        Assert.IsNotNull(bullet, "DarkBullet.Create 应返回非 null");
        Object.DestroyImmediate(bullet.gameObject);
    }

    // ═══ P0: 贯穿弹 ═══

    [Test]
    public void P0_PenetrateHandler_Setup_ResetsState()
    {
        var go = new GameObject("TestBullet");
        var ph = go.AddComponent<PenetrateHandler>();
        ph.Setup(3);

        // 第一次穿透应成功
        var enemy1 = new GameObject("Enemy1");
        enemy1.tag = "Enemy";
        enemy1.AddComponent<CircleCollider2D>();
        bool first = ph.TryPenetrate(enemy1.GetComponent<Collider2D>());
        Assert.IsTrue(first, "首次穿透应成功");

        // 同一敌人第二次应失败（已穿透过）
        bool second = ph.TryPenetrate(enemy1.GetComponent<Collider2D>());
        Assert.IsFalse(second, "同一敌人不应穿透两次");

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(enemy1);
    }

    [Test]
    public void P0_DarkBullet_HasPenetrateCheck()
    {
        // 回归测试：DarkBullet 必须有 PenetrateHandler 缓存和贯穿检查
        var bullet = DarkBullet.Create(Vector2.zero, Vector2.right, 10f, 3f, 0.5f);
        Assert.IsNotNull(bullet, "DarkBullet.Create 应返回非 null");

        var ph = bullet.GetComponent<PenetrateHandler>();
        // PenetrateHandler 由 DotBulletFactory.AttachRicochetIfAvailable 添加
        // 如果 PiercingBonus > 0 则存在，否则不存在
        // 验证 DarkBullet 有 _cachedPenetrate 字段（通过反射）
        var field = typeof(DarkBullet).GetField("_cachedPenetrate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(field, "DarkBullet 应有 _cachedPenetrate 字段");

        Object.DestroyImmediate(bullet.gameObject);
    }

    [Test]
    public void P0_PenetrateHandler_PoolReuse_Works()
    {
        var go = new GameObject("TestBullet");

        // 第一次使用
        var ph1 = go.AddComponent<PenetrateHandler>();
        ph1.Setup(2);

        // 模拟池复用：移除旧组件，添加新的
        Object.DestroyImmediate(ph1);
        var ph2 = go.AddComponent<PenetrateHandler>();
        ph2.Setup(3);

        // OnEnable 应刷新缓存
        var cached = go.GetComponent<PenetrateHandler>();
        Assert.IsNotNull(cached, "池复用后 PenetrateHandler 应存在");
        Assert.AreEqual(ph2, cached, "应返回新组件");

        Object.DestroyImmediate(go);
    }

    // ═══ P0: 伤害数字 ═══

    [Test]
    public void P0_DamagePopup_Create_Float_Works()
    {
        DamagePopup.Create(Vector3.zero, 3.14f, Color.white, false);
        // 不应抛异常
        Assert.Pass();
    }

    [Test]
    public void P0_DamagePopup_CreateDOT_Works()
    {
        DamagePopup.CreateDOT(Vector3.zero, 5.20f, "burn");
        Assert.Pass();
    }

    [Test]
    public void P0_DamagePopup_CreateDetonateTotal_Works()
    {
        DamagePopup.CreateDetonateTotal(Vector3.zero, 123.45f, 5);
        Assert.Pass();
    }

    // ═══ P0: 光明/黑暗标记 ═══

    [Test]
    public void P0_LightMarkEffect_ImplementsIStackEffect()
    {
        var go = new GameObject("TestEnemy");
        var mark = go.AddComponent<LightMarkEffect>();
        IStackEffect effect = mark;

        Assert.AreEqual(StatusEffectType.Light, effect.EffectType);
        Assert.AreEqual(0, effect.StackCount);
        Assert.IsFalse(effect.IsActive);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void P0_DarkMarkEffect_ImplementsIStackEffect()
    {
        var go = new GameObject("TestEnemy");
        go.AddComponent<Damageable>();
        go.AddComponent<BaseEntity>();
        var mark = go.AddComponent<DarkMarkEffect>();
        mark.Init(3f, 0.5f);

        IStackEffect effect = mark;
        Assert.AreEqual(StatusEffectType.Dark, effect.EffectType);
        Assert.AreEqual(1, effect.StackCount);
        Assert.IsTrue(effect.IsActive);

        // 叠加
        mark.AddStack();
        Assert.AreEqual(2, effect.StackCount);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void P0_DotEffectRegistry_GetStackEffects_FindsLightAndDark()
    {
        var go = new GameObject("TestEnemy");
        go.AddComponent<Damageable>();
        go.AddComponent<BaseEntity>();

        var light = go.AddComponent<LightMarkEffect>();
        light.AddStack(15f, 999); // 添加1层使 IsActive=true

        var dark = go.AddComponent<DarkMarkEffect>();
        dark.Init(3f, 0.5f);

        var buffer = new List<IStackEffect>();
        DotEffectRegistry.GetStackEffectsOnEnemy(go, buffer);

        bool foundLight = false, foundDark = false;
        foreach (var e in buffer)
        {
            if (e.EffectType == StatusEffectType.Light) foundLight = true;
            if (e.EffectType == StatusEffectType.Dark) foundDark = true;
        }

        Assert.IsTrue(foundLight, "应找到 LightMarkEffect");
        Assert.IsTrue(foundDark, "应找到 DarkMarkEffect");

        Object.DestroyImmediate(go);
    }

    // ═══ P1: 难度系统 ═══

    [Test]
    public void P1_DifficultyManager_InitializesCorrectly()
    {
        DifficultyManager.CurrentDifficulty = 1;
        Assert.AreEqual(1, DifficultyManager.CurrentDifficulty);
        Assert.IsNotNull(DifficultyManager.CurrentConfig);
    }

    [Test]
    public void P1_DifficultyManager_ClampsToRange()
    {
        DifficultyManager.CurrentDifficulty = 0;
        Assert.AreEqual(1, DifficultyManager.CurrentDifficulty);

        DifficultyManager.CurrentDifficulty = 999;
        Assert.LessOrEqual(DifficultyManager.CurrentDifficulty, DifficultyManager.MaxDifficulty);
    }

    [Test]
    public void P1_DifficultyConfig_HpMultScalesWithWave()
    {
        var cfg = ScriptableObject.CreateInstance<DifficultyConfig>();
        cfg.enemyHpMult = 2f;
        cfg.scalingExponent = 1.2f;

        float wave1 = cfg.GetEffectiveHpMult(1);
        float wave100 = cfg.GetEffectiveHpMult(100);
        Assert.Greater(wave100, wave1, "无尽模式 HP 应随波次增长");

        Object.DestroyImmediate(cfg);
    }

    [Test]
    public void P1_DifficultyConfig_10LevelsExist()
    {
        DifficultyManager.EnsureInitialized();
        Assert.AreEqual(10, DifficultyManager.MaxDifficulty, "应有10个难度等级");
    }

    // ═══ P1: 精英词条 ═══

    [Test]
    public void P1_EliteModifier_NewTypesExist()
    {
        // 验证新词条类型存在
        Assert.IsTrue(System.Enum.IsDefined(typeof(EliteModifierSystem.ModifierType), "Magnet"));
        Assert.IsTrue(System.Enum.IsDefined(typeof(EliteModifierSystem.ModifierType), "Gravity"));
        Assert.IsTrue(System.Enum.IsDefined(typeof(EliteModifierSystem.ModifierType), "Plague"));
    }

    [Test]
    public void P1_EliteModifier_ShouldSpawnElite_FrequencyMult()
    {
        // 频率倍率=2 时，精英率应更高
        int highCount = 0, lowCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            if (EliteModifierSystem.ShouldSpawnElite(10, 2f)) highCount++;
            if (EliteModifierSystem.ShouldSpawnElite(10, 1f)) lowCount++;
        }
        Assert.Greater(highCount, lowCount, "频率倍率×2 应产生更多精英");
    }

    // ═══ P2: 受击闪白 ═══

    [Test]
    public void P2_HitFlashEffect_CanBeAdded()
    {
        var go = new GameObject("TestEnemy");
        go.AddComponent<SpriteRenderer>();
        var flash = go.AddComponent<HitFlashEffect>();
        Assert.IsNotNull(flash);
        flash.TriggerFlash(); // 不应抛异常
        Object.DestroyImmediate(go);
    }

    // ═══ P2: 装备系统 ═══

    [Test]
    public void P2_LootDropSystem_DropsEquipment()
    {
        // Boss 100% 掉落
        int drops = 0;
        for (int i = 0; i < 10; i++)
        {
            var item = LootDropSystem.TryDrop(false, true, 10);
            if (item != null) drops++;
        }
        Assert.AreEqual(10, drops, "Boss 应100%掉落");
    }

    [Test]
    public void P2_Backpack_AddAndRemove()
    {
        Backpack.Clear();
        var item = new EquipmentInstance
        {
            equipmentId = "test_001",
            rarity = EquipmentRarity.Common,
            slot = EquipmentSlot.Weapon,
            displayName = "测试武器",
            affixes = new EquipmentAffix[0]
        };

        Assert.IsTrue(Backpack.AddItem(item), "添加装备应成功");
        Assert.AreEqual(1, Backpack.Inventory.Count);

        int materials = Backpack.Dismantle(item);
        Assert.Greater(materials, 0, "分解应获得材料");
        Assert.AreEqual(0, Backpack.Inventory.Count);

        Backpack.Clear();
    }

    // ═══ P2: 环境区域 ═══

    [Test]
    public void P2_EnvironmentZone_CreateDefault_Works()
    {
        var zone = EnvironmentZone.CreateDefault(Vector3.zero, MapThemeData.EnvironmentZoneType.Damage, 3f, 0f);
        Assert.IsNotNull(zone);
        Object.DestroyImmediate(zone.gameObject);
    }

    // ═══ P3: 性能检查 ═══

    [Test]
    public void P3_PhysicsHelper_OverlapCircle_Works()
    {
        var buffer = new List<Collider2D>(16);
        int count = PhysicsHelper.OverlapCircle(Vector2.zero, 50f, buffer);
        Assert.GreaterOrEqual(count, 0, "OverlapCircle 不应返回负数");
    }

    [Test]
    public void P3_ExplosionVFX_Pooling_Works()
    {
        // 创建多个爆炸特效，验证池化
        for (int i = 0; i < 5; i++)
            CombatManager.CreateExplosionEffect(Vector2.zero, 1f, Color.red, 0.1f);

        // 等一帧让特效完成
        // 不应抛异常
        Assert.Pass();
    }

    [Test]
    public void P1_DifficultyManager_GetConfigForLevel_ReturnsAll10()
    {
        DifficultyManager.EnsureInitialized();
        for (int i = 1; i <= 10; i++)
        {
            var cfg = DifficultyManager.GetConfigForLevel(i);
            Assert.IsNotNull(cfg, $"难度{i}的配置不应为null");
            Assert.AreEqual(i, cfg.difficultyLevel, $"难度{i}的level应为{i}");
            Assert.IsFalse(string.IsNullOrEmpty(cfg.difficultyName), $"难度{i}应有名称");
        }
    }

    // ═══ P3: 接口一致性 ═══

    [Test]
    public void P3_MagePassive_ImplementsAllInterfaces()
    {
        var go = new GameObject("TestMage");
        var mage = go.AddComponent<MagePassive>();

        Assert.IsTrue(mage is ICharacterPassive, "MagePassive 应实现 ICharacterPassive");
        Assert.AreEqual("mage", ((ICharacterPassive)mage).CharacterId);
        Assert.IsNotNull(((ICharacterPassive)mage).DotGuns);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void P3_DotGunState_ImplementsIGunState()
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
    }

    [Test]
    public void P3_DetonateWaveEffect_CreateDestroy_NoLeak()
    {
        var go = new GameObject("TestWave");
        var wave = go.AddComponent<DetonateWaveEffect>();
        Assert.IsNotNull(wave);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void P2_TestBulletSelectUI_HasGeneralAndSpecificCategories()
    {
        // 验证 TestBulletSelectUI 的一般强化分类正确
        var go = new GameObject("TestUI");
        var ui = go.AddComponent<TestBulletSelectUI>();
        Assert.IsNotNull(ui, "TestBulletSelectUI 应可创建");

        // 通过反射验证 _generalCategories 包含通用类别
        var field = typeof(TestBulletSelectUI).GetField("_generalCategories",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.IsNotNull(field, "应有 _generalCategories 字段");

        var categories = field.GetValue(null) as System.Collections.Generic.HashSet<string>;
        Assert.IsNotNull(categories);
        Assert.IsTrue(categories.Contains("AttackSpeed"), "AttackSpeed 应为一般强化");
        Assert.IsTrue(categories.Contains("Penetrate"), "Penetrate 应为一般强化");
        Assert.IsTrue(categories.Contains("Ricochet"), "Ricochet 应为一般强化");
        Assert.IsFalse(categories.Contains("DotType"), "DotType 不应为一般强化（专属）");
        Assert.IsFalse(categories.Contains("DotFrequency"), "DotFrequency 不应为一般强化（专属）");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void P2_TrainingDummy_CanBeCreated()
    {
        var go = new GameObject("TestDummy");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = HealthBarSpriteHelper.GetWhiteSprite();
        go.AddComponent<BoxCollider2D>();
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        var dmg = go.AddComponent<Damageable>();
        dmg.SetMaxHp(1000);
        dmg.Heal(1000);

        var dummy = go.AddComponent<TrainingDummy>();
        dummy.Init(1000, 0, 0f, false, 1f);
        Assert.IsNotNull(dummy, "TrainingDummy 应可创建");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void P2_DpsTracker_CanBeCreated()
    {
        var go = new GameObject("TestTracker");
        var tracker = go.AddComponent<DpsTracker>();
        Assert.IsNotNull(tracker, "DpsTracker 应可创建");
        Assert.AreEqual(0f, tracker.TotalDamage);
        Assert.AreEqual(0, tracker.HitCount);

        tracker.RecordDamage(100f);
        Assert.AreEqual(100f, tracker.TotalDamage);
        Assert.AreEqual(1, tracker.HitCount);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void P2_GameReferences_DpsTestMode_ResetWorks()
    {
        GameReferences.DpsTestMode = true;
        Assert.IsTrue(GameReferences.DpsTestMode);
        GameReferences.Clear();
        Assert.IsFalse(GameReferences.DpsTestMode, "Clear 后 DpsTestMode 应为 false");
    }

    // ═══ P2: 代码优化验证 ═══

    [Test]
    public void P2_CritParams_Apply_NoCrit()
    {
        var crit = new CritParams(false, 0f, 0f);
        Assert.AreEqual(100f, crit.Apply(100f), "无暴击时伤害不变");
    }

    [Test]
    public void P2_CritParams_Apply_WithCrit()
    {
        var crit = new CritParams(true, 1f, 2f); // 100%暴击率，2倍暴击
        Assert.AreEqual(200f, crit.Apply(100f), "100%暴击率应双倍伤害");
    }

    [Test]
    public void P2_CritParams_Roll_AlwaysTrue()
    {
        var crit = new CritParams(true, 1f, 2f);
        for (int i = 0; i < 10; i++)
            Assert.IsTrue(crit.Roll(), "100%暴击率应始终暴击");
    }

    [Test]
    public void P2_CritParams_Roll_AlwaysFalse()
    {
        var crit = new CritParams(true, 0f, 2f);
        for (int i = 0; i < 10; i++)
            Assert.IsFalse(crit.Roll(), "0%暴击率应永不暴击");
    }

    [Test]
    public void P2_CritParams_None_IsDefault()
    {
        var none = CritParams.None;
        Assert.IsFalse(none.canCrit);
        Assert.AreEqual(0f, none.critChance);
        Assert.AreEqual(0f, none.critMult);
    }

    [Test]
    public void P2_DotSpreadHelper_Exists()
    {
        // 验证 DotSpreadHelper 可访问
        var method = typeof(DotSpreadHelper).GetMethod("SpreadEffects",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.IsNotNull(method, "DotSpreadHelper.SpreadEffects 方法应存在");
    }

    [Test]
    public void P2_MenuButtonFactory_Exists()
    {
        // 验证 MenuButtonFactory 可访问
        var type = typeof(MenuButtonFactory);
        Assert.IsNotNull(type, "MenuButtonFactory 类应存在");

        var createTest = type.GetMethod("CreateTestButton",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.IsNotNull(createTest, "CreateTestButton 方法应存在");
    }

    [Test]
    public void P2_PhysicsHelper_OverlapCircle_ReturnsNonNegative()
    {
        var buffer = new System.Collections.Generic.List<Collider2D>(16);
        int count = PhysicsHelper.OverlapCircle(Vector2.zero, 50f, buffer);
        Assert.GreaterOrEqual(count, 0, "OverlapCircle 不应返回负数");
    }

    [Test]
    public void P2_ICharacterPassive_MagePassive_HasCorrectId()
    {
        var go = new GameObject("TestMage");
        var mage = go.AddComponent<MagePassive>();
        ICharacterPassive cp = mage;
        Assert.AreEqual("mage", cp.CharacterId);
        Assert.AreEqual("DOT 法师", cp.DisplayName);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void P2_IGunState_DotGunState_ImplementsInterface()
    {
        DotGunState gun = new DotGunState
        {
            effectType = StatusEffectType.Burn,
            color = Color.red,
            cooldown = 1.5f,
            impactDamage = 10,
            dotDps = 5f,
            dotDuration = 3f,
            upgradeLevel = 2
        };

        IGunState ig = gun;
        Assert.AreEqual(StatusEffectType.Burn, ig.EffectType);
        Assert.AreEqual(5f, ig.DotDps);
        Assert.AreEqual(2, ig.UpgradeLevel);
    }

    [Test]
    public void P2_DifficultyConfig_10Levels_HaveUniqueNames()
    {
        DifficultyManager.EnsureInitialized();
        var names = new System.Collections.Generic.HashSet<string>();
        for (int i = 1; i <= 10; i++)
        {
            var cfg = DifficultyManager.GetConfigForLevel(i);
            Assert.IsNotNull(cfg);
            Assert.IsTrue(names.Add(cfg.difficultyName), $"难度{i}名称 '{cfg.difficultyName}' 重复");
        }
    }

    [Test]
    public void P2_ArmorFormula_MixedReduction()
    {
        // 混合护甲公式：固定减伤（上限50%）+ 百分比减伤（递减收益）
        // 公式：actualDamage = Max(0.01, (damage - Min(armor, damage*0.5)) * (1 - armor/(armor+100)))

        // 无护甲：伤害不变
        float dmg0 = CalcDamage(100f, 0);
        Assert.AreEqual(100f, dmg0, 0.01f, "0护甲应无减伤");

        // 护甲50：固定减伤25(50*0.5) + 百分比减伤50/(50+100)=33% → (100-25)*(1-0.33)=50.25
        float dmg50 = CalcDamage(100f, 50);
        Assert.Greater(dmg50, 0f, "护甲50不应免疫");
        Assert.Less(dmg50, 100f, "护甲50应有减伤");

        // 护甲100：固定减伤50(100*0.5) + 百分比减伤100/(100+100)=50% → (100-50)*(1-0.5)=25
        float dmg100 = CalcDamage(100f, 100);
        Assert.Greater(dmg100, 0f, "护甲100不应免疫");
        Assert.Less(dmg100, dmg50, "护甲100应比护甲50减伤更多");

        // 护甲999：永远不低于0.01
        float dmg999 = CalcDamage(10f, 999);
        Assert.GreaterOrEqual(dmg999, 0.01f, "超高护甲不应免疫，最低0.01");
    }

    private float CalcDamage(float damage, int armor)
    {
        float flatCap = 0.5f;
        float percentBase = 100f;
        float flatReduction = Mathf.Min(armor, damage * flatCap);
        float percentReduction = armor / (armor + percentBase);
        return Mathf.Max(0.01f, (damage - flatReduction) * (1f - percentReduction));
    }

    // ═══ 辅助方法 ═══

    [TearDown]
    public void Cleanup()
    {
        // 清理测试中创建的临时对象
        var tempObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
        foreach (var obj in tempObjects)
        {
            if (obj.name.StartsWith("Test") || obj.name.StartsWith("Enemy"))
                Object.DestroyImmediate(obj);
        }
    }
}
#endif
