using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// #7 DOT 子弹工厂 — 根据 StatusEffectType 创建对应子弹
/// 从 DotProjectile.cs 拆分而来
/// </summary>
public static class DotBulletFactory
{
    public delegate GameObject BulletSpawner(
        Vector2 pos, Vector2 dir, MagePassive.DotGunState gun,
        float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult);

    private static readonly Dictionary<StatusEffectType, BulletSpawner> _spawners
        = new Dictionary<StatusEffectType, BulletSpawner>();

    // P2-1: 数据驱动配置（延迟加载，避免启动时依赖）
    private static DotBulletConfig _config;
    private static DotBulletConfig Config
    {
        get { if (_config == null) _config = DotBulletConfig.GetDefault(); return _config; }
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
    }

    public static void Register(StatusEffectType type, BulletSpawner spawner)
    {
        _spawners[type] = spawner;
    }

    public static GameObject Create(StatusEffectType type, Vector2 pos, Vector2 dir,
        MagePassive.DotGunState gun, float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        if (_spawners.TryGetValue(type, out var spawner))
            return spawner(pos, dir, gun, bulletSpeedMult, durMult, dmgMult, canCrit, critChance, critMult);

        DebugHelper.LogWarning($"[DotBulletFactory] 未注册的子弹类型: {type}");
        return null;
    }

    // ── 默认创建方法 ──

    private static GameObject SpawnPoison(Vector2 pos, Vector2 dir, MagePassive.DotGunState gun,
        float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var go = PoisonBullet.Create(pos, dir, Config.PoisonSpeed * bulletSpeedMult,
            gun.dotDps, gun.dotDuration * durMult, dmgMult,
            canCrit, critChance, critMult)?.gameObject;
        AttachRicochetIfAvailable(go);
        return go;
    }

    private static GameObject SpawnBurn(Vector2 pos, Vector2 dir, MagePassive.DotGunState gun,
        float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var go = BurnBullet.Create(pos, dir, Config.BurnSpeed * bulletSpeedMult, gun.impactDamage,
            gun.dotDps, gun.dotDuration * durMult, dmgMult,
            canCrit, critChance, critMult)?.gameObject;
        AttachRicochetIfAvailable(go);
        return go;
    }

    private static GameObject SpawnFrost(Vector2 pos, Vector2 dir, MagePassive.DotGunState gun,
        float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var go = FrostBullet.Create(pos, dir, Config.FrostSpeed * bulletSpeedMult, gun.impactDamage,
            gun.dotDps, Config.FrostFreezeDuration, Config.FrostBaseSlowPct, dmgMult,
            canCrit, critChance, critMult)?.gameObject;
        AttachRicochetIfAvailable(go);
        return go;
    }

    private static GameObject SpawnStatic(Vector2 pos, Vector2 dir, MagePassive.DotGunState gun,
        float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var go = LightningBullet.Create(pos, dir, Config.LightningSpeed * bulletSpeedMult, gun.impactDamage,
            dmgMult)?.gameObject;
        AttachRicochetIfAvailable(go);
        return go;
    }

    private static GameObject SpawnDark(Vector2 pos, Vector2 dir, MagePassive.DotGunState gun,
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

    private static GameObject SpawnLight(Vector2 pos, Vector2 dir, MagePassive.DotGunState gun,
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

    private static GameObject SpawnWind(Vector2 pos, Vector2 dir, MagePassive.DotGunState gun,
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
            var ph = bullet.AddComponent<PenetrateHandler>();
            ph.Setup(pierce);
        }
    }
}

/// <summary>
/// #45 穿透处理器 — 挂在 DOT 子弹上，子弹速度加成>100%时穿透额外敌人
/// </summary>
public class PenetrateHandler : MonoBehaviour
{
    private int _remaining;
    private HashSet<Collider2D> _hitEnemies = new HashSet<Collider2D>();

    public void Setup(int penetrateCount)
    {
        _remaining = penetrateCount;
        _hitEnemies.Clear();
    }

    public bool TryPenetrate(Collider2D hitEnemy)
    {
        if (_hitEnemies.Contains(hitEnemy)) return false;
        _hitEnemies.Add(hitEnemy);
        if (_remaining <= 0) return false;
        _remaining--;
        return true;
    }
}

/// <summary>
/// #16 反弹处理器 — 挂在 DOT 子弹上，命中敌人后有概率弹射到最近的另一个敌人
/// </summary>
public class RicochetHandler : MonoBehaviour
{
    private float _chance;
    private int _maxBounces;
    private int _bounces;
    private float _damageDecay = 0.8f;

    public void Setup(float chance, int extraMaxBounces)
    {
        _chance = chance;
        _maxBounces = Mathf.FloorToInt(chance) + extraMaxBounces;
        _bounces = 0;
    }

    public bool TryRicochet(Vector2 currentPosition, Collider2D hitEnemy)
    {
        if (_bounces >= _maxBounces) return false;

        int guaranteed = Mathf.FloorToInt(_chance);
        float extra = _chance - guaranteed;
        if (_bounces >= guaranteed && Random.value >= extra) return false;

        Collider2D[] nearby = Physics2D.OverlapCircleAll(currentPosition, 20f);
        Transform nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var col in nearby)
        {
            if (!col.CompareTag("Enemy") || col == hitEnemy) continue;
            var d = col.GetComponent<Damageable>();
            if (d == null || d.CurrentHp <= 0) continue;
            float dist = Vector2.Distance(currentPosition, col.transform.position);
            if (dist < nearestDist) { nearestDist = dist; nearest = col.transform; }
        }

        if (nearest == null) return false;
        _bounces++;

        Vector2 dir = ((Vector2)nearest.position - currentPosition).normalized;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float speed = rb.linearVelocity.magnitude;
            rb.linearVelocity = dir * speed;
        }

        CombatManager.CreateExplosionEffect(currentPosition, 0.5f, Color.white, 0.2f);
        DebugHelper.Log($"[RicochetHandler] Bounce #{_bounces} → {nearest.name}");
        return true;
    }

    public float GetDamageMultiplier() => Mathf.Pow(_damageDecay, _bounces);
}

/// <summary>
/// #15 DOT 追踪弹桥接组件 — 挂在 HomingProjectile 上，命中敌人时附加 DOT 效果
/// #22 优化：使用策略字典替代 switch-case，消除分支
/// </summary>
public class DotHomingBullet : MonoBehaviour
{
    private MagePassive.DotGunState _gun;
    private float _durMult;
    private float _dmgMult;
    private bool _canCrit;
    private float _critChance;
    private float _critMult;
    private bool _initialized;

    // #22: 策略字典 — 每种 StatusEffectType 对应一个命中处理委托
    // 参数: (enemy, gun, durMult, dmgMult, canCrit, critChance, critMult)
    private static readonly Dictionary<StatusEffectType, System.Action<GameObject, MagePassive.DotGunState, float, float, bool, float, float>> _hitHandlers
        = new Dictionary<StatusEffectType, System.Action<GameObject, MagePassive.DotGunState, float, float, bool, float, float>>
    {
        { StatusEffectType.Poison, ApplyPoison },
        { StatusEffectType.Burn, ApplyBurn },
        { StatusEffectType.Frostbite, ApplyFrost },
    };

    public void Init(MagePassive.DotGunState gun, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        _gun = gun;
        _durMult = durMult;
        _dmgMult = dmgMult;
        _canCrit = canCrit;
        _critChance = critChance;
        _critMult = critMult;
        _initialized = true;
    }

    public void OnHitEnemy(GameObject enemy)
    {
        if (!_initialized || enemy == null) return;
        DotBulletHelper.EnsureStatusEffectManager(enemy);

        if (_hitHandlers.TryGetValue(_gun.effectType, out var handler))
        {
            handler(enemy, _gun, _durMult, _dmgMult, _canCrit, _critChance, _critMult);
        }
    }

    // ── 策略方法（静态，无实例状态）──

    private static void ApplyPoison(GameObject enemy, MagePassive.DotGunState gun, float durMult, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var poison = enemy.GetComponent<PoisonStackEffect>();
        if (poison == null) poison = enemy.AddComponent<PoisonStackEffect>();
        poison.AddStack(gun.dotDps * dmgMult, gun.dotDuration * durMult, canCrit, critChance, critMult);
    }

    private static void ApplyBurn(GameObject enemy, MagePassive.DotGunState gun, float durMult, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var burn = enemy.GetComponent<BurnStackEffect>();
        if (burn == null) burn = enemy.AddComponent<BurnStackEffect>();
        burn.AddStack(gun.dotDps * dmgMult, gun.dotDuration * durMult, canCrit, critChance, critMult);
    }

    private static void ApplyFrost(GameObject enemy, MagePassive.DotGunState gun, float durMult, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var frost = enemy.GetComponent<FrostEffect>();
        if (frost == null) frost = enemy.AddComponent<FrostEffect>();
        frost.ApplyFreeze(1f, 0.3f, 0f, false, 0f, 0f);
    }
}

/// <summary>
/// #14 风蚀漩涡 — DOT 敌人移动时在脚下生成的微型漩涡
/// </summary>
public class WindErosionVortex : MonoBehaviour
{
    private float _radius;
    private float _duration;
    private float _tickDamage;
    private float _pullChance;
    private float _spawnTime;
    private float _lastTick;
    private SpriteRenderer _sr;

    public void Setup(float radius, float duration, float tickDamage, float pullChance)
    {
        _radius = radius; _duration = duration; _tickDamage = tickDamage; _pullChance = pullChance;
    }

    private void Start()
    {
        _spawnTime = Time.time;
        _lastTick = Time.time;
        _sr = GetComponent<SpriteRenderer>();
        transform.localScale = Vector3.one * _radius * 0.5f;
    }

    private void Update()
    {
        if (Time.time - _spawnTime > _duration) { Destroy(gameObject); return; }
        transform.Rotate(0, 0, 180f * Time.deltaTime);

        if (_sr != null)
        {
            float alpha = 1f - (Time.time - _spawnTime) / _duration;
            var c = _sr.color;
            c.a = alpha * 0.5f;
            _sr.color = c;
        }

        if (Time.time - _lastTick < 0.5f) return;
        _lastTick = Time.time;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _radius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            var dmg = hit.GetComponent<Damageable>();
            if (dmg == null || dmg.CurrentHp <= 0) continue;
            dmg.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(_tickDamage)));
            if (Random.value < _pullChance)
            {
                var rb = hit.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 pullDir = ((Vector2)transform.position - (Vector2)hit.transform.position).normalized;
                    rb.linearVelocity += pullDir * 3f;
                }
            }
        }
    }

    public static WindErosionVortex Create(Vector2 pos, float radius, float duration, float tickDamage, float pullChance)
    {
        var go = new GameObject("WindErosionVortex");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite();
        sr.color = new Color(0.7f, 0.85f, 1f, 0.5f);
        sr.sortingOrder = 2;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true; col.radius = radius;
        var vortex = go.AddComponent<WindErosionVortex>();
        vortex.Setup(radius, duration, tickDamage, pullChance);
        return vortex;
    }
}