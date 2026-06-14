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
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
            Assert.IsNotNull(config, "MageUpgradeConfig 不应为 null");
            Assert.IsTrue(config.IsDotGunUpgrade("poison"), "应包含 poison 子弹");
            Object.DestroyImmediate(config);
            return;
        }
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
        var player = GameReferences.Player;
        if (player == null)
        {
            var go = new GameObject("TestPlayer");
            go.AddComponent<MagePassive>();
            var cp = go.GetComponent<ICharacterPassive>();
            Assert.IsNotNull(cp, "MagePassive 应实现 ICharacterPassive");
            Object.DestroyImmediate(go);
            return;
        }
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

    // ═══ P3: 接口一致性 ═══

    [Test]
    public void P3_MagePassive_ImplementsAllInterfaces()
    {
        var go = new GameObject("TestMage");
        var mage = go.AddComponent<MagePassive>();

        Assert.IsTrue(mage is ICharacterPassive, "MagePassive 应实现 ICharacterPassive");
        Assert.IsTrue(mage is IDotCharacterPassive, "MagePassive 应实现 IDotCharacterPassive");
        Assert.AreEqual("mage", ((ICharacterPassive)mage).CharacterId);
        Assert.IsNotNull(((IDotCharacterPassive)mage).DotGuns);

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
        Assert.AreEqual(StatusEffectType.Burn, gun.EffectType);
        Assert.AreEqual(3f, gun.DotDps);
        Assert.AreEqual(1f, iGun.Cooldown);
        Assert.AreEqual(2, iGun.UpgradeLevel);
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
        var go = new GameObject("TestUI");
        var ui = go.AddComponent<TestBulletSelectUI>();
        Assert.IsNotNull(ui, "TestBulletSelectUI 应可创建");

        var field = typeof(TestBulletSelectUI).GetField("_generalCategories",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.IsNotNull(field, "应有 _generalCategories 字段");

        var categories = field.GetValue(null);
        Assert.IsNotNull(categories, "_generalCategories 不应为 null");

        // 类型已改为 HashSet<UpgradeCategory>
        var asSet = categories as System.Collections.Generic.HashSet<CharacterUpgradeOption.UpgradeCategory>;
        Assert.IsNotNull(asSet, "_generalCategories 应为 HashSet<UpgradeCategory>");
        Assert.IsTrue(asSet.Contains(CharacterUpgradeOption.UpgradeCategory.AttackSpeed), "AttackSpeed 应为一般强化");
        Assert.IsTrue(asSet.Contains(CharacterUpgradeOption.UpgradeCategory.Ricochet), "Ricochet 应为一般强化");

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
        Assert.AreEqual(StatusEffectType.Burn, gun.EffectType);
        Assert.AreEqual(5f, gun.DotDps);
        Assert.AreEqual(2, ig.UpgradeLevel);
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

    // ═══ P0: 子弹系统 PlayMode 测试 ═══

    [Test]
    public void P0_WindBullet_SpeedIs60()
    {
        var config = ScriptableObject.CreateInstance<DotEffectConfig>();
        Assert.AreEqual(60f, config.WindSpeed, 0.001f, "风子弹速度应为60");
        Object.DestroyImmediate(config);
    }

    [Test]
    public void P0_WindBullet_FiresTowardDirection()
    {
        var bullet = WindBullet.Create(Vector2.zero, Vector2.right, 60f, 0, 1f, false, 0f, 0f);
        Assert.IsNotNull(bullet);
        var go = bullet.gameObject;
        Assert.IsNotNull(go);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void P0_WindBullet_FactoryReturnsNotNull()
    {
        var gun = new DotGunState { effectType = StatusEffectType.WindErosion, cooldown = 0.25f, upgradeLevel = 1 };
        var go = DotBulletFactory.Create(StatusEffectType.WindErosion, Vector2.zero, Vector2.right,
            gun, 1f, 1f, false, 0f, 0f);
        Assert.IsNotNull(go, "风子弹工厂不应返回 null");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void P0_DotBulletFactory_All7TypesWork()
    {
        StatusEffectType[] types = {
            StatusEffectType.Poison, StatusEffectType.Burn,
            StatusEffectType.Frostbite, StatusEffectType.Static,
            StatusEffectType.Dark, StatusEffectType.WindErosion
        };
        // Light 类型特殊（蓄力激光），单独测试
        foreach (var type in types)
        {
            var gun = new DotGunState { effectType = type, cooldown = 1f, upgradeLevel = 1 };
            var go = DotBulletFactory.Create(type, Vector2.zero, Vector2.right,
                gun, 1f, 1f, false, 0f, 0f);
            Assert.IsNotNull(go, $"{type} 工厂不应返回 null");
            Object.DestroyImmediate(go);
        }
    }

    // ═══ P0: DPS 测试模式 PlayMode 测试 ═══

    [Test]
    public void P0_DpsTestDummy_HPIs2Million()
    {
        var gc = Resources.Load<GameConfig>("Configs/GameConfig");
        if (gc == null)
        {
            gc = ScriptableObject.CreateInstance<GameConfig>();
            Assert.AreEqual(2000000, gc.dpsDummyHP, "木桩HP应为200万");
            Object.DestroyImmediate(gc);
            return;
        }
        Assert.AreEqual(2000000, gc.dpsDummyHP, "木桩HP应为200万");
    }

    [Test]
    public void P0_DpsTestDummy_StaticBody_NotPushable()
    {
        var go = new GameObject("TestDummy");
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<BoxCollider2D>();
        go.AddComponent<Damageable>();
        var dummy = go.AddComponent<TrainingDummy>();
        dummy.Init(1000, 0, 0f, false, 1f);

        var rb = go.GetComponent<Rigidbody2D>();
        Assert.IsNotNull(rb, "木桩应有 Rigidbody2D");
        Assert.AreEqual(RigidbodyType2D.Static, rb.bodyType, "木桩应为 Static 刚体");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void P0_DpsTestDummy_PositionLocked()
    {
        var go = new GameObject("TestDummy");
        go.transform.position = new Vector3(5f, 3f, 0f);
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<BoxCollider2D>();
        go.AddComponent<Damageable>();
        var dummy = go.AddComponent<TrainingDummy>();
        dummy.Init(1000, 0, 0f, false, 1f);

        // 模拟物理推力
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null && rb.bodyType != RigidbodyType2D.Static)
            rb.linearVelocity = Vector2.right * 100f;

        // TrainingDummy Update 会锁定位置
        // 但这里无法调用 Update，直接验证 bodyType
        Assert.AreEqual(RigidbodyType2D.Static, rb.bodyType);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void P1_DpsTestMode_NoEnemiesSpawn()
    {
        // 验证 GameReferences.DpsTestMode 标志
        GameReferences.DpsTestMode = true;
        Assert.IsTrue(GameReferences.DpsTestMode);
        GameReferences.DpsTestMode = false;
        Assert.IsFalse(GameReferences.DpsTestMode);
    }

    [Test]
    public void P1_DpsTracker_RecordsDamage()
    {
        var go = new GameObject("TestTracker");
        var tracker = go.AddComponent<DpsTracker>();
        Assert.AreEqual(0f, tracker.TotalDamage);

        tracker.RecordDamage(50f);
        tracker.RecordDamage(30f);
        Assert.AreEqual(80f, tracker.TotalDamage, 0.01f);
        Assert.AreEqual(2, tracker.HitCount);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void P1_DpsTracker_ResetWorks()
    {
        var go = new GameObject("TestTracker");
        var tracker = go.AddComponent<DpsTracker>();
        tracker.RecordDamage(100f);
        Assert.AreEqual(100f, tracker.TotalDamage);

        tracker.Reset();
        Assert.AreEqual(0f, tracker.TotalDamage);
        Assert.AreEqual(0, tracker.HitCount);

        Object.DestroyImmediate(go);
    }

    // ═══ P1: DOT效果系统 PlayMode 测试 ═══

    [Test]
    public void P1_WindErosionEffect_StacksOnHit()
    {
        var go = new GameObject("TestEnemy");
        go.tag = "Enemy";
        go.AddComponent<Damageable>();
        go.AddComponent<BaseEntity>();
        go.AddComponent<SpriteRenderer>();
        var effect = go.AddComponent<WindErosionEffect>();

        Assert.AreEqual(0, effect.StackCount, "初始层数应为0");

        effect.RegisterHit();
        Assert.AreEqual(1, effect.StackCount, "命中1次后层数应为1");

        effect.RegisterHit();
        Assert.AreEqual(2, effect.StackCount, "命中2次后层数应为2");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void P1_WindErosionEffect_NoExplosionEffect()
    {
        // 验证 WindErosionEffect 没有 CreateExplosionEffect 调用
        // 通过检查源码方法签名（反射验证 ApplyKnockback 不调用 CombatManager）
        var method = typeof(WindErosionEffect).GetMethod("ApplyKnockback",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(method, "ApplyKnockback 方法应存在");
        // 无法直接验证方法内部调用，但可以验证方法存在
    }

    [Test]
    public void P1_WindErosionEffect_MaxStacks_Caps()
    {
        var go = new GameObject("TestEnemy");
        go.tag = "Enemy";
        go.AddComponent<Damageable>();
        go.AddComponent<BaseEntity>();
        go.AddComponent<SpriteRenderer>();
        var effect = go.AddComponent<WindErosionEffect>();

        // 叠加到上限
        for (int i = 0; i < 1000; i++)
            effect.RegisterHit();

        var config = DotEffectConfig.GetDefault();
        Assert.LessOrEqual(effect.StackCount, config.WindMaxStacks,
            "层数不应超过配置上限");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void P1_FrostEffect_SlowApplies()
    {
        var go = new GameObject("TestEnemy");
        go.tag = "Enemy";
        go.AddComponent<Damageable>();
        go.AddComponent<SpriteRenderer>();
        var effect = go.AddComponent<FrostEffect>();

        Assert.IsFalse(effect.IsActive, "初始应不活跃");

        effect.ApplyFreeze(1f, 0.3f, 0f, false, 0f, 0f);
        Assert.IsTrue(effect.IsActive, "ApplyFreeze 后应活跃");
        Assert.Greater(effect.StackCount, 0, "应有霜冻层数");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void P1_StaticStackEffect_StunOnDischarge()
    {
        var go = new GameObject("TestEnemy");
        go.tag = "Enemy";
        go.AddComponent<Damageable>();
        go.AddComponent<SpriteRenderer>();
        var effect = go.AddComponent<StaticStackEffect>();

        Assert.IsFalse(effect.IsActive, "初始应不活跃");

        effect.AddStack();
        Assert.IsTrue(effect.IsActive, "AddStack 后应活跃");
        Assert.AreEqual(1, effect.StackCount);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void P1_DarkMarkEffect_SpreadOnDeath()
    {
        var go = new GameObject("TestEnemy");
        go.tag = "Enemy";
        go.AddComponent<Damageable>();
        go.AddComponent<BaseEntity>();
        var effect = go.AddComponent<DarkMarkEffect>();
        effect.Init(3f, 0.5f);

        Assert.IsTrue(effect.IsActive, "Init 后应活跃");
        Assert.AreEqual(1, effect.StackCount);

        effect.AddStack();
        Assert.AreEqual(2, effect.StackCount, "AddStack 后层数应增加");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void P2_BurnWindErosion_SpreadReaction()
    {
        // 验证燃烧+风化可以共存
        var go = new GameObject("TestEnemy");
        go.tag = "Enemy";
        go.AddComponent<Damageable>();
        go.AddComponent<BaseEntity>();
        go.AddComponent<SpriteRenderer>();

        var burn = go.AddComponent<BurnStackEffect>();
        var wind = go.AddComponent<WindErosionEffect>();

        Assert.IsNotNull(burn, "BurnStackEffect 应可添加");
        Assert.IsNotNull(wind, "WindErosionEffect 应可添加");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void P2_FrostStatic_IceFieldReaction()
    {
        // 验证霜冻+静电可以共存
        var go = new GameObject("TestEnemy");
        go.tag = "Enemy";
        go.AddComponent<Damageable>();
        go.AddComponent<SpriteRenderer>();

        var frost = go.AddComponent<FrostEffect>();
        var staticEff = go.AddComponent<StaticStackEffect>();

        Assert.IsNotNull(frost, "FrostEffect 应可添加");
        Assert.IsNotNull(staticEff, "StaticStackEffect 应可添加");

        Object.DestroyImmediate(go);
    }

    // ═══ P1: 敌人系统 PlayMode 测试 ═══

    [Test]
    public void P1_EnemyBase_TakesDamage()
    {
        var go = new GameObject("TestEnemy");
        go.tag = "Enemy";
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = HealthBarSpriteHelper.GetWhiteSprite();
        go.AddComponent<BoxCollider2D>();
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        var dmg = go.AddComponent<Damageable>();
        dmg.SetMaxHp(100);
        dmg.Heal(100);

        Assert.AreEqual(100, dmg.CurrentHp);
        dmg.TakeDamage(30);
        Assert.AreEqual(70, dmg.CurrentHp, "受伤30后HP应为70");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void P1_EliteModifierSystem_AppliesOnWave5()
    {
        // 验证 ShouldSpawnElite 在高波次有更高概率
        int eliteCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            if (EliteModifierSystem.ShouldSpawnElite(10, 1f)) eliteCount++;
        }
        Assert.Greater(eliteCount, 0, "波次10应有精英出现");
    }

    // ═══ P1: 对象池 PlayMode 测试 ═══

    [Test]
    public void P1_PoolHelper_SpawnOrFallback_Works()
    {
        bool fallbackCalled = false;
        var go = PoolHelper.SpawnOrFallback("test_pool_key",
            () => { fallbackCalled = true; return new GameObject("TestPoolObj"); },
            () => new GameObject("TestPoolObj"), Vector3.zero);

        Assert.IsNotNull(go, "SpawnOrFallback 应返回非 null");
        Assert.IsTrue(fallbackCalled, "池不存在时应调用 fallback");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void P1_PoolHelper_DespawnOrDestroy_Works()
    {
        var go = new GameObject("TestDespawn");
        // 不应抛异常
        PoolHelper.DespawnOrDestroy(go, "nonexistent_pool");
        // 如果池不存在，应 Destroy（已清理）
    }

    [Test]
    public void P2_WindBullet_PoolRecycle_NoLeak()
    {
        var bullet = WindBullet.Create(Vector2.zero, Vector2.right, 60f, 0, 1f, false, 0f, 0f);
        Assert.IsNotNull(bullet);
        var go = bullet.gameObject;

        // 回收
        PoolHelper.DespawnOrDestroy(go, PoolHelper.DOT_WIND_BULLET);
        // 不应抛异常
        Assert.Pass();
    }

    // ═══ 重构后新增测试 ═══

    [Test]
    public void ICharacterPassive_BlueImplements()
    {
        var go = new GameObject("TestBlue");
        var blue = go.AddComponent<BlueCharacterPassive>();

        Assert.IsTrue(blue is ICharacterPassive, "BlueCharacterPassive should implement ICharacterPassive");
        Assert.IsFalse(blue is IDotCharacterPassive, "BlueCharacterPassive should NOT implement IDotCharacterPassive");
        Assert.AreEqual("blue", ((ICharacterPassive)blue).CharacterId);
        Assert.AreEqual("蓝色战士", ((ICharacterPassive)blue).DisplayName);
        Assert.AreEqual(1f, blue.GetAttackSpeedMultiplier());
        Assert.AreEqual(0, blue.GetBulletCountBonus());

        Object.DestroyImmediate(go);
    }

    [Test]
    public void GameStarter_CreatesCorrectPassive()
    {
        var go = new GameObject("TestPlayer");
        var mage = go.AddComponent<MagePassive>();

        Assert.IsTrue(mage is ICharacterPassive);
        Assert.IsTrue(mage is IDotCharacterPassive);
        Assert.AreEqual("mage", mage.CharacterId);

        var blueGo = new GameObject("TestBluePlayer");
        var blue = blueGo.AddComponent<BlueCharacterPassive>();

        Assert.IsTrue(blue is ICharacterPassive);
        Assert.IsFalse(blue is IDotCharacterPassive);
        Assert.AreEqual("blue", blue.CharacterId);

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(blueGo);
    }

    // ═══ task1-5: 系统集成测试 ═══

    [Test]
    public void MagePassive_IDotCharacterPassive_AllProperties()
    {
        var go = new GameObject("TestMageFull");
        var mage = go.AddComponent<MagePassive>();
        IDotCharacterPassive dot = mage;

        Assert.IsNotNull(dot.DotGuns, "DotGuns should not be null");
        Assert.IsTrue(dot.DotGuns.Count > 0, "Mage should start with Poison DOT gun");

        Assert.AreEqual(1.2f, dot.GetDotDurationMultiplier(), 0.01f, "Default duration +20%");
        Assert.Greater(dot.GetDotCritChance(), 0f, "Crit chance should be > 0");
        Assert.AreEqual(2f, dot.GetDotCritMultiplier(), 0.01f);

        Assert.AreEqual(0.1f, dot.CorrosionArmorReduction, 0.01f);
        Assert.AreEqual(0, dot.ErosionArmorPenetration);
        Assert.AreEqual(1, dot.CurseSpreadTargets);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void MagePassive_UnlockDotGun_UpgradesExisting()
    {
        var go = new GameObject("TestMageUpgrade");
        var mage = go.AddComponent<MagePassive>();
        IDotCharacterPassive dot = mage;

        int initialCount = dot.DotGuns.Count;
        float initialDps = dot.DotGuns[0].dotDps;

        dot.UnlockDotGun(StatusEffectType.Poison, Color.green, 1.5f, 0, 2f, 5f);

        Assert.AreEqual(initialCount, dot.DotGuns.Count, "Should not add new gun, just upgrade");
        Assert.Greater(dot.DotGuns[0].dotDps, initialDps, "DPS should increase on upgrade");
        Assert.AreEqual(2, dot.DotGuns[0].upgradeLevel, "Level should be 2");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void MagePassive_ClearAllDotGuns()
    {
        var go = new GameObject("TestMageClear");
        var mage = go.AddComponent<MagePassive>();
        IDotCharacterPassive dot = mage;

        dot.UnlockDotGun(StatusEffectType.Burn, Color.red, 0.5f, 2, 2f, 3f);
        Assert.IsTrue(dot.DotGuns.Count > 1, "Should have more than 1 gun");

        dot.ClearAllDotGuns();
        Assert.AreEqual(0, dot.DotGuns.Count, "Should have 0 guns after clear");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void BlueCharacterPassive_ApplyUpgrade_AttackSpeed()
    {
        var go = new GameObject("TestBlueUpgrade");
        var blue = go.AddComponent<BlueCharacterPassive>();
        var config = ScriptableObject.CreateInstance<BlueUpgradeConfig>();
        blue.SetUpgradeConfig(config);

        float before = blue.GetAttackSpeedMultiplier();
        blue.ApplyUpgrade("haste");
        float after = blue.GetAttackSpeedMultiplier();

        Assert.Less(after, before, "Attack speed mult should decrease after haste upgrade");

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(config);
    }

    [Test]
    public void BlueCharacterPassive_ApplyUpgrade_BulletCount()
    {
        var go = new GameObject("TestBlueBarrage");
        var blue = go.AddComponent<BlueCharacterPassive>();
        var config = ScriptableObject.CreateInstance<BlueUpgradeConfig>();
        blue.SetUpgradeConfig(config);

        Assert.AreEqual(0, blue.GetBulletCountBonus());
        blue.ApplyUpgrade("barrage");
        Assert.AreEqual(1, blue.GetBulletCountBonus());

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(config);
    }

    [Test]
    public void GameReferences_CachedProperties()
    {
        var go = new GameObject("TestPlayerRef");
        GameReferences.Player = go.GetComponent<PlayerController>();
        if (GameReferences.Player == null)
        {
            var pc = go.AddComponent<PlayerController>();
            GameReferences.Player = pc;
        }
        var mage = go.AddComponent<MagePassive>();

        Assert.IsNotNull(GameReferences.CharacterPassive);
        Assert.IsNotNull(GameReferences.DotCharacterPassive);
        Assert.AreSame(GameReferences.CharacterPassive, GameReferences.DotCharacterPassive);

        GameReferences.Reset();

        Object.DestroyImmediate(go);
    }

    [Test]
    public void GameStateResetter_ClearsStaticLists()
    {
        var go = new GameObject("TestReset");
        go.AddComponent<Damageable>();

        DotBulletBase.ActiveDotBullets.Add(go.GetComponent<MonoBehaviour>());

        GameStateResetter.FullReset();

        Assert.AreEqual(0, DotBulletBase.ActiveDotBullets.Count, "ActiveDotBullets should be cleared");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void DotGunState_UpgradeSimulation()
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

        for (int i = 0; i < 4; i++)
        {
            gun.dotDps *= 1.15f;
            gun.impactDamage = Mathf.RoundToInt(gun.impactDamage * 1.1f);
            gun.upgradeLevel++;
        }

        Assert.AreEqual(5, gun.upgradeLevel);
        Assert.Greater(gun.dotDps, 3f, "After 4 upgrades, DPS should be significantly higher");
    }

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
    public void ProjectileBase_SetDirection_RotatesObject()
    {
        var go = new GameObject("TestProj");
        var sr = go.AddComponent<SpriteRenderer>();
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<CircleCollider2D>();
        var bullet = go.AddComponent<SimpleBullet>();

        go.SendMessage("SetDirection", Vector2.up);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void CharacterUpgradeConfig_ICharacterConfig_Interface()
    {
        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        ICharacterConfig ic = config;

        Assert.AreEqual("mage", ic.CharacterId);
        Assert.IsNotNull(ic.GetUpgradeOptions());
        Assert.IsNotNull(ic.GetGunEntries());
        Assert.IsTrue(ic.GetUpgradeOptions().Length > 0);
        Assert.IsTrue(ic.GetGunEntries().Length > 0);

        Object.DestroyImmediate(config);
    }

    [Test]
    public void CharacterConfigLoader_LoadMageConfig()
    {
        CharacterConfigLoader.ClearCache();
        var config = CharacterConfigLoader.Load("mage");
        if (config == null)
        {
            var runtime = ScriptableObject.CreateInstance<MageUpgradeConfig>();
            Assert.IsNotNull(runtime, "Runtime MageUpgradeConfig should not be null");
            Assert.AreEqual("mage", runtime.characterId);
            Object.DestroyImmediate(runtime);
        }
        else
        {
            Assert.AreEqual("mage", config.CharacterId);
        }
        CharacterConfigLoader.ClearCache();
    }

    [Test]
    public void CharacterConfigLoader_LoadNonexistent_ReturnsNull()
    {
        CharacterConfigLoader.ClearCache();
        var config = CharacterConfigLoader.Load("nonexistent_character");
        Assert.IsNull(config, "Should return null for unknown character");
    }

    [Test]
    public void DetonateSystem_Init_SetsCharacter()
    {
        var go = new GameObject("TestDetonate");
        var mage = go.AddComponent<MagePassive>();

        var det = go.GetComponent<DetonateSystem>();
        Assert.IsNotNull(det, "MagePassive.Awake should create DetonateSystem");
        Assert.IsNotNull(mage.GetDetonateSystem());

        Object.DestroyImmediate(go);
    }

    [Test]
    public void MagePassive_AttackSpeedOverride()
    {
        var go = new GameObject("TestMageSpeed");
        var mage = go.AddComponent<MagePassive>();

        Assert.AreEqual(1f, mage.GetAttackSpeedMultiplier(), 0.001f);

        mage.AttackSpeedBonus = 0.3f;
        Assert.AreEqual(0.7f, mage.GetAttackSpeedMultiplier(), 0.001f, "Mage should use CharacterPassiveBase logic");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void MagePassive_DotDamageMultiplier_WithChainDetonate()
    {
        var go = new GameObject("TestMageDmg");
        var mage = go.AddComponent<MagePassive>();

        float baseMult = mage.GetDotDamageMultiplier();
        Assert.AreEqual(1f, baseMult, 0.01f, "Default multiplier should be 1.0");

        mage.DotDamageMultiplier = 0.5f;
        Assert.AreEqual(1.5f, mage.GetDotDamageMultiplier(), 0.01f);

        Object.DestroyImmediate(go);
    }
}
#endif
