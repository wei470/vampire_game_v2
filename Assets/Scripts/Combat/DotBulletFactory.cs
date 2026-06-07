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

    static DotBulletFactory()
    {
        Register(StatusEffectType.Bleed, SpawnBleed);
        Register(StatusEffectType.Poison, SpawnPoison);
        Register(StatusEffectType.Burn, SpawnBurn);
        Register(StatusEffectType.Frostbite, SpawnFrost);
        Register(StatusEffectType.Static, SpawnStatic);
        Register(StatusEffectType.Dark, SpawnDark);
        Register(StatusEffectType.Light, SpawnLight);
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

    private static GameObject SpawnBleed(Vector2 pos, Vector2 dir, MagePassive.DotGunState gun,
        float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var go = BleedBullet.Create(pos, dir, 14f * bulletSpeedMult, gun.impactDamage,
            gun.dotDps, gun.dotDuration * durMult, dmgMult,
            canCrit, critChance, critMult)?.gameObject;
        AttachRicochetIfAvailable(go);
        return go;
    }

    private static GameObject SpawnPoison(Vector2 pos, Vector2 dir, MagePassive.DotGunState gun,
        float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var go = PoisonBullet.Create(pos, dir, 14f * bulletSpeedMult,
            gun.dotDps, gun.dotDuration * durMult, dmgMult,
            canCrit, critChance, critMult)?.gameObject;
        return go;
    }

    private static GameObject SpawnBurn(Vector2 pos, Vector2 dir, MagePassive.DotGunState gun,
        float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var go = BurnBullet.Create(pos, dir, 12f * bulletSpeedMult, gun.impactDamage,
            gun.dotDps, gun.dotDuration * durMult, dmgMult,
            canCrit, critChance, critMult)?.gameObject;
        AttachRicochetIfAvailable(go);
        return go;
    }

    private static GameObject SpawnFrost(Vector2 pos, Vector2 dir, MagePassive.DotGunState gun,
        float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var go = FrostBullet.Create(pos, dir, 20f * bulletSpeedMult, gun.impactDamage,
            gun.dotDps, 1f, 0.3f, dmgMult,
            canCrit, critChance, critMult)?.gameObject;
        AttachRicochetIfAvailable(go);
        return go;
    }

    private static GameObject SpawnStatic(Vector2 pos, Vector2 dir, MagePassive.DotGunState gun,
        float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var go = LightningBullet.Create(pos, dir, 16f * bulletSpeedMult, gun.impactDamage,
            dmgMult)?.gameObject;
        AttachRicochetIfAvailable(go);
        return go;
    }

    private static GameObject SpawnDark(Vector2 pos, Vector2 dir, MagePassive.DotGunState gun,
        float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        float speed = 6f * bulletSpeedMult;
        float radius = 3f + (gun.upgradeLevel - 1) * 0.5f; // 升级增加传播范围
        float efficiency = 0.5f + (gun.upgradeLevel - 1) * 0.05f; // 升级增加传播效率
        var go = DarkBullet.Create(pos, dir, speed, radius, efficiency)?.gameObject;
        AttachRicochetIfAvailable(go);
        return go;
    }

    private static GameObject SpawnLight(Vector2 pos, Vector2 dir, MagePassive.DotGunState gun,
        float bulletSpeedMult, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        // 光明子弹是特殊的蓄力型定向激光，不走普通子弹路径
        float chargeDuration = Mathf.Max(1.5f, 3f - (gun.upgradeLevel - 1) * 0.3f);
        int laserDamage = 1;
        float sweepAngle = 45f;
        float sweepDuration = 0.4f;
        float laserLength = 25f;
        float laserWidth = 1.5f;
        float markDuration = 15f;
        int markMaxStacks = 9999; // 无上限

        var controller = LightBulletController.Create(pos);
        controller.Setup(chargeDuration, laserDamage, sweepAngle, sweepDuration,
            laserLength, laserWidth, markDuration, markMaxStacks, 0.7f);
        controller.BeginCharge();
        return controller.gameObject;
    }

    /// <summary>
    /// #16 如果 MagePassive 有反弹加成，为子弹附加 RicochetHandler
    /// #45 如果子弹速度加成>100%，为子弹附加 PenetrateHandler
    /// </summary>
    private static void AttachRicochetIfAvailable(GameObject bullet)
    {
        if (bullet == null) return;
        var mage = GameReferences.Player?.GetComponent<MagePassive>();
        if (mage == null) return;

        if (mage.RicochetChance > 0f)
        {
            var rh = bullet.AddComponent<RicochetHandler>();
            rh.Setup(mage.RicochetChance, mage.RicochetMaxBounces);
        }

        if (mage.BulletSpeedBonus > 1f)
        {
            int penetrateCount = Mathf.FloorToInt(mage.BulletSpeedBonus);
            var ph = bullet.AddComponent<PenetrateHandler>();
            ph.Setup(penetrateCount);
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

        switch (_gun.effectType)
        {
            case StatusEffectType.Bleed:
                var bleed = enemy.GetComponent<BleedEffect>();
                if (bleed == null) bleed = enemy.AddComponent<BleedEffect>();
                bleed.Refresh(_gun.dotDps * _dmgMult, _gun.dotDuration * _durMult, _canCrit, _critChance, _critMult);
                break;
            case StatusEffectType.Poison:
                var poison = enemy.GetComponent<PoisonStackEffect>();
                if (poison == null) poison = enemy.AddComponent<PoisonStackEffect>();
                poison.AddStack(_gun.dotDps * _dmgMult, _gun.dotDuration * _durMult, _canCrit, _critChance, _critMult);
                break;
            case StatusEffectType.Burn:
                var burn = enemy.GetComponent<BurnStackEffect>();
                if (burn == null) burn = enemy.AddComponent<BurnStackEffect>();
                burn.AddStack(_gun.dotDps * _dmgMult, _gun.dotDuration * _durMult, _canCrit, _critChance, _critMult);
                break;
            case StatusEffectType.Frostbite:
                var frost = enemy.GetComponent<FrostEffect>();
                if (frost == null) frost = enemy.AddComponent<FrostEffect>();
                // 霜冻不造成伤害，只施加减速
                frost.ApplyFreeze(1f, 0.3f, 0f, false, 0f, 0f);
                break;
        }
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