using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// #7 DOT 子弹工厂 — 根据 StatusEffectType 创建对应子弹
/// 从 DotProjectile.cs 拆分而来
/// </summary>
public static class DotBulletFactory
{
    public delegate GameObject BulletSpawner(
        Vector2 pos, Vector2 dir, DotGunState gun,
        float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult);

    private static readonly Dictionary<StatusEffectType, BulletSpawner> _spawners
        = new Dictionary<StatusEffectType, BulletSpawner>();

    // P2-1: 数据驱动配置（延迟加载，避免启动时依赖）
    private static DotEffectConfig _config;
    private static DotEffectConfig Config
    {
        get { if (_config == null) _config = DotEffectConfig.GetDefault(); return _config; }
    }

    static DotBulletFactory()
    {
        // 流血子弹已移除
        Register(StatusEffectType.Poison, SpawnPoison);
        Register(StatusEffectType.Burn, SpawnBurn);
        Register(StatusEffectType.Frostbite, SpawnFrost);
        Register(StatusEffectType.Static, SpawnStatic);
        Register(StatusEffectType.Dark, SpawnDark);
        Register(StatusEffectType.Light, SpawnLight);
        Register(StatusEffectType.WindErosion, SpawnWind);
        DotEffectConfig.OnConfigChanged += OnConfigChanged;
    }

    public static void Register(StatusEffectType type, BulletSpawner spawner)
    {
        _spawners[type] = spawner;
    }

    public static GameObject Create(StatusEffectType type, Vector2 pos, Vector2 dir,
        DotGunState gun, float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        if (_spawners.TryGetValue(type, out var spawner))
            return spawner(pos, dir, gun, bulletSpeedMult, durMult, dmgMult, canCrit, critChance, critMult);

        DebugHelper.LogWarning($"[DotBulletFactory] 未注册的子弹类型: {type}");
        return null;
    }

    // ── 默认创建方法 ──

    private static void OnConfigChanged()
    {
        _config = null;
    }

    private static GameObject SpawnPoison(Vector2 pos, Vector2 dir, DotGunState gun,
        float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var go = PoisonBullet.Create(pos, dir, Config.PoisonSpeed * bulletSpeedMult,
            gun.dotDps, gun.dotDuration * durMult, dmgMult,
            canCrit, critChance, critMult)?.gameObject;
        AttachRicochetIfAvailable(go);
        return go;
    }

    private static GameObject SpawnBurn(Vector2 pos, Vector2 dir, DotGunState gun,
        float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var go = BurnBullet.Create(pos, dir, Config.BurnSpeed * bulletSpeedMult, gun.impactDamage,
            gun.dotDps, gun.dotDuration * durMult, dmgMult,
            canCrit, critChance, critMult)?.gameObject;
        AttachRicochetIfAvailable(go);
        return go;
    }

    private static GameObject SpawnFrost(Vector2 pos, Vector2 dir, DotGunState gun,
        float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var go = FrostBullet.Create(pos, dir, Config.FrostSpeed * bulletSpeedMult, gun.impactDamage,
            gun.dotDps, Config.FrostFreezeDuration, Config.FrostBaseSlowPct, dmgMult,
            canCrit, critChance, critMult)?.gameObject;
        AttachRicochetIfAvailable(go);
        return go;
    }

    private static GameObject SpawnStatic(Vector2 pos, Vector2 dir, DotGunState gun,
        float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var go = LightningBullet.Create(pos, dir, Config.LightningSpeed * bulletSpeedMult, gun.impactDamage,
            dmgMult)?.gameObject;
        AttachRicochetIfAvailable(go);
        return go;
    }

    private static GameObject SpawnDark(Vector2 pos, Vector2 dir, DotGunState gun,
        float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        float speed = Config.DarkSpeed * bulletSpeedMult;
        float radius = Config.DarkBaseRadius + (gun.upgradeLevel - 1) * Config.DarkRadiusPerLevel;
        float efficiency = Config.DarkBaseEfficiency + (gun.upgradeLevel - 1) * Config.DarkEfficiencyPerLevel;
        var go = DarkBullet.Create(pos, dir, speed, radius, efficiency)?.gameObject;
        AttachRicochetIfAvailable(go);
        return go;
    }

    private static GameObject SpawnLight(Vector2 pos, Vector2 dir, DotGunState gun,
        float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        // 光明子弹是特殊的蓄力型定向激光，不走普通子弹路径
        float chargeDuration = Mathf.Max(Config.LightMinChargeDuration,
            Config.LightChargeDuration - (gun.upgradeLevel - 1) * Config.LightChargeReductionPerLevel);
        int laserDamage = Config.LightLaserDamage;
        float sweepAngle = Config.LightSweepAngle;
        float sweepDuration = Config.LightSweepDuration;
        float laserLength = Config.LightLaserLength;
        float laserWidth = Config.LightLaserWidth;
        float markDuration = Config.LightMarkDuration;
        int markMaxStacks = Config.LightMarkMaxStacks <= 0 ? 9999 : Config.LightMarkMaxStacks;

        var controller = LightBulletController.Create(pos);
        controller.Setup(chargeDuration, laserDamage, sweepAngle, sweepDuration,
            laserLength, laserWidth, markDuration, markMaxStacks, Config.LightTextureScale);
        controller.BeginCharge();
        return controller.gameObject;
    }

    private static GameObject SpawnWind(Vector2 pos, Vector2 dir, DotGunState gun,
        float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        float speed = Config.WindSpeed * bulletSpeedMult;
        // 风子弹散射：从配置读取角度数组
        float[] angles = Config.WindSpreadAngles != null && Config.WindSpreadAngles.Length > 0
            ? Config.WindSpreadAngles : new float[] { -15f, -7.5f, 0f, 7.5f, 15f };
        GameObject firstGo = null;
        for (int i = 0; i < angles.Length; i++)
        {
            float rad = angles[i] * Mathf.Deg2Rad;
            Vector2 spreadDir = new Vector2(
                dir.x * Mathf.Cos(rad) - dir.y * Mathf.Sin(rad),
                dir.x * Mathf.Sin(rad) + dir.y * Mathf.Cos(rad)
            ).normalized;
            var go = WindBullet.Create(pos, spreadDir, speed, gun.impactDamage,
                dmgMult, canCrit, critChance, critMult)?.gameObject;
            AttachRicochetIfAvailable(go);
            if (i == 0) firstGo = go;
        }
        return firstGo;
    }

    /// <summary>
    /// 统一子弹创建流程：池化 + 组件获取 + 初始化 + 方向设置
    /// </summary>
    public static T CreateBullet<T>(string poolKey, System.Func<GameObject> factory,
        Vector2 pos, Vector2 dir, System.Action<T> setup, int warmupCount = 15) where T : MonoBehaviour
    {
        var go = PoolHelper.SpawnOrFallback(poolKey, factory, () => {
            var g = new GameObject(typeof(T).Name);
            g.tag = "Untagged";
            PhysicsLayerSetup.SetAsBullet(g);
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = DotSpriteCache.CircleSprite();
            sr.sortingOrder = 15;
            g.transform.localScale = Vector3.one * 0.2f;
            g.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var col = g.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.25f;
            g.AddComponent<T>();
            return g;
        }, pos, warmupCount);

        var bullet = go.GetComponent<T>();
        if (bullet == null) bullet = go.AddComponent<T>();
        setup(bullet);
        return bullet;
    }

    /// <summary>
    /// #45 为子弹附加穿透处理器（基于贯穿弹升级 PiercingBonus）
    /// </summary>
    private static void AttachRicochetIfAvailable(GameObject bullet)
    {
        if (bullet == null) return;
        var mage = GameReferences.MagePassive;
        if (mage == null) return;

        int pierce = mage.PiercingBonus;
        if (pierce > 0)
        {
            var ph = bullet.GetComponent<PenetrateHandler>();
            if (ph == null) ph = bullet.AddComponent<PenetrateHandler>();
            ph.Setup(pierce);
        }
    }
}