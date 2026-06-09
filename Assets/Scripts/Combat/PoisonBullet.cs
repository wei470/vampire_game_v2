using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 毒子弹 — 直线飞行，命中第一个敌人后范围爆炸，叠加中毒层数
/// Mage 默认攻击子弹。从 DotProjectile.cs 拆分而来。
/// </summary>
public class PoisonBullet : MonoBehaviour
{
    private float _speed = 14f;
    private float _poisonDps = 2f;
    private float _poisonDuration = 5f;
    private float _explosionRadius = 0.5f;
    private float _damageMultiplier = 1f;
    private bool _canCrit; private float _critChance, _critMult;
    private Vector2 _direction;
    private bool _exploded;
    private float _spawnTime;
    private float _lifetime = 5f;
    private Rigidbody2D _rb;
    private PenetrateHandler _cachedPenetrate;

    public void Setup(float speed, float poisonDps, float poisonDuration, float explosionRadius,
        float dmgMult, bool canCrit, float critChance, float critMult)
    {
        _speed = speed; _poisonDps = poisonDps; _poisonDuration = poisonDuration;
        _explosionRadius = explosionRadius; _damageMultiplier = dmgMult;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }
    public void SetDirection(Vector2 dir) { _direction = dir.normalized; RotateToDirection(); }
    private void RotateToDirection() { float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg; transform.rotation = Quaternion.Euler(0, 0, angle); }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _cachedPenetrate = GetComponent<PenetrateHandler>();
    }

    private void OnEnable()
    {
        _spawnTime = Time.time;
        _exploded = false;
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        if (_rb != null) _rb.linearVelocity = Vector2.zero;
    }

    private void Update() { if (!_exploded && Time.time - _spawnTime > _lifetime) DespawnSelf(); }
    private void FixedUpdate() { if (!_exploded && _rb != null) _rb.linearVelocity = _direction * _speed; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_exploded) return;
        if (!other.CompareTag("Enemy")) return;
        DotBulletHelper.EnsureStatusEffectManager(other.gameObject);

        // 穿透检查：先对当前敌人施加中毒DOT，然后检查是否可以继续穿透
        if (_cachedPenetrate != null && _cachedPenetrate.TryPenetrate(other))
        {
            // 穿透成功：对当前敌人施加中毒效果但不爆炸
            var dmg = other.GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp > 0)
            {
                var poison = other.GetComponent<PoisonStackEffect>();
                if (poison == null) poison = other.gameObject.AddComponent<PoisonStackEffect>();
                poison.AddStack(_poisonDps * _damageMultiplier, _poisonDuration, _canCrit, _critChance, _critMult);
            }
            return;
        }

        LeavePuddle(transform.position);
    }

    private void LeavePuddle(Vector2 center)
    {
        _exploded = true;
        if (_rb != null) _rb.linearVelocity = Vector2.zero;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, _explosionRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            var dmg = hit.GetComponent<Damageable>();
            if (dmg == null || dmg.CurrentHp <= 0) continue;
            var poison = hit.GetComponent<PoisonStackEffect>();
            if (poison == null) poison = hit.gameObject.AddComponent<PoisonStackEffect>();
            poison.AddStack(_poisonDps * _damageMultiplier, _poisonDuration, _canCrit, _critChance, _critMult);
        }
        PoisonPuddle.Create(center, 0.5f, 2f, _poisonDps * _damageMultiplier, _canCrit, _critChance, _critMult);
        DespawnSelf();
    }

    private void DespawnSelf()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.DOT_POISON_BULLET);
    }

    private static GameObject BuildTemplate()
    {
        var go = new GameObject("PoisonBullet");
        go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(0.1f, 0.9f, 0.2f); sr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * 0.25f;
        var rb = go.AddComponent<Rigidbody2D>(); rb.gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.25f;
        DotBulletVisualEffects.AttachTrail(go, new Color(0.1f, 0.9f, 0.2f, 0.6f), 0.4f, 0.03f);
        go.AddComponent<PoisonBullet>();
        return go;
    }

    public static PoisonBullet Create(Vector2 pos, Vector2 dir, float speed,
        float poisonDps, float poisonDuration, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        // 尝试从对象池取出
        var pool = ObjectPool.Instance;
        GameObject go = null;
        if (pool != null && pool.HasPool(PoolHelper.DOT_POISON_BULLET))
        {
            go = pool.Spawn(PoolHelper.DOT_POISON_BULLET, pos, Quaternion.identity);
        }
        else
        {
            // 首次：注册虚拟预制体
            PoolHelper.RegisterVirtualPrefab(PoolHelper.DOT_POISON_BULLET, BuildTemplate, 15);
            go = pool != null ? pool.Spawn(PoolHelper.DOT_POISON_BULLET, pos, Quaternion.identity) : null;
        }

        if (go == null)
        {
            // 池回退：直接创建
            go = new GameObject("PoisonBullet");
            go.tag = "Untagged";
            PhysicsLayerSetup.SetAsBullet(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(0.1f, 0.9f, 0.2f); sr.sortingOrder = 15;
            go.transform.localScale = Vector3.one * 0.25f;
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.25f;
            DotBulletVisualEffects.AttachTrail(go, new Color(0.1f, 0.9f, 0.2f, 0.6f), 0.4f, 0.03f);
            go.AddComponent<PoisonBullet>();
        }

        go.transform.position = pos;
        go.SetActive(true);
        var b = go.GetComponent<PoisonBullet>();
        b.Setup(speed, poisonDps, poisonDuration, 0.5f, dmgMult, canCrit, critChance, critMult);
        b.SetDirection(dir);
        // 刷新穿透缓存（可能被 AttachRicochetIfAvailable 动态添加）
        b._cachedPenetrate = go.GetComponent<PenetrateHandler>();
        return b;
    }
}

/// <summary>
/// 中毒药瓶 — 投掷后爆炸生成毒液池
/// </summary>
public class PoisonPotion : MonoBehaviour
{
    private float _speed = 10f;
    private float _lifetime = 3f;
    private float _puddleDuration = 5f;
    private float _puddleRadius = 1.5f;
    private float _baseDps = 3f;
    private float _damageMultiplier = 1f;
    private bool _canCrit; private float _critChance, _critMult;
    private Vector2 _targetPos;
    private float _spawnTime;
    private bool _exploded;

    public void Setup(float speed, float puddleDuration, float puddleRadius, float baseDps,
        float dmgMult, bool canCrit, float critChance, float critMult)
    {
        _speed = speed; _puddleDuration = puddleDuration; _puddleRadius = puddleRadius;
        _baseDps = baseDps; _damageMultiplier = dmgMult;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }
    public void SetTarget(Vector2 target) { _targetPos = target; }
    private void Start() { _spawnTime = Time.time; }
    private void Update() { if (Time.time - _spawnTime > _lifetime) Destroy(gameObject); }

    private void FixedUpdate()
    {
        if (_exploded) return;
        Vector2 dir = _targetPos - (Vector2)transform.position;
        if (dir.magnitude < 0.3f) { Explode(); return; }
        GetComponent<Rigidbody2D>().linearVelocity = dir.normalized * _speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_exploded) return;
        if (other.CompareTag("Enemy") || other.CompareTag("Untagged")) Explode();
    }

    private void Explode()
    {
        _exploded = true;
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        PoisonPuddle.Create(transform.position, _puddleRadius, _puddleDuration, _baseDps * _damageMultiplier, _canCrit, _critChance, _critMult);
        CombatManager.CreateExplosionEffect(transform.position, _puddleRadius, new Color(0.1f, 0.9f, 0.2f, 0.5f), 0.3f);
        Destroy(gameObject);
    }

    public static PoisonPotion Create(Vector2 pos, Vector2 target, float speed,
        float puddleDuration, float puddleRadius, float baseDps, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var go = new GameObject("PoisonPotion");
        go.transform.position = pos; go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.Get(); sr.color = new Color(0.1f, 0.8f, 0.1f); sr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * 0.8f;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.3f;
        DotBulletVisualEffects.AttachSpinEffect(go, 360f);
        var p = go.AddComponent<PoisonPotion>();
        p.Setup(speed, puddleDuration, puddleRadius, baseDps, dmgMult, canCrit, critChance, critMult);
        p.SetTarget(target);
        return p;
    }
}

/// <summary>
/// 毒液池 — 敌人站在上面会叠加中毒层数
/// </summary>
public class PoisonPuddle : MonoBehaviour
{
    private float _radius;
    private float _duration;
    private float _baseDps;
    private bool _canCrit; private float _critChance, _critMult;
    private float _spawnTime;
    private float _lastTick;
    private CircleCollider2D _cachedCol;
    private SpriteRenderer _cachedSr;

    public void Setup(float radius, float duration, float baseDps, bool canCrit, float critChance, float critMult)
    {
        _radius = radius; _duration = duration; _baseDps = baseDps;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
        // 立即应用半径到scale和collider（OnEnable时_radius可能为0）
        ApplyRadius();
    }

    private void ApplyRadius()
    {
        transform.localScale = Vector3.one * _radius;
        if (_cachedCol == null) _cachedCol = GetComponent<CircleCollider2D>();
        if (_cachedCol != null) { _cachedCol.isTrigger = true; _cachedCol.radius = _radius; }
    }

    private void Awake()
    {
        _cachedCol = GetComponent<CircleCollider2D>();
        _cachedSr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        _spawnTime = Time.time; _lastTick = Time.time - 0.5f;
        ApplyRadius();
        if (_cachedSr != null) _cachedSr.color = new Color(0.1f, 0.7f, 0.1f, 0.4f);
    }

    private void Update()
    {
        if (Time.time - _spawnTime > _duration) { DespawnSelf(); return; }
        if (Time.time - _lastTick < 0.5f) return;
        _lastTick = Time.time;
        ApplyPoisonToNearby();
    }

    // OnTriggerStay2D 已移除 — 避免与 Update 中 ApplyPoisonToNearby 重复叠毒

    private void ApplyPoisonToNearby()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _radius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            var dmg = hit.GetComponent<Damageable>();
            if (dmg == null || dmg.CurrentHp <= 0) continue;
            var poison = hit.GetComponent<PoisonStackEffect>();
            if (poison == null) poison = hit.gameObject.AddComponent<PoisonStackEffect>();
            poison.AddStack(_baseDps, _duration - (Time.time - _spawnTime), _canCrit, _critChance, _critMult);
        }
    }

    private void DespawnSelf()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.DOT_POISON_PUDDLE);
    }

    private static GameObject BuildPuddleTemplate()
    {
        var go = new GameObject("PoisonPuddle");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(0.1f, 0.7f, 0.1f, 0.4f); sr.sortingOrder = 1;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        go.AddComponent<PoisonPuddle>();
        return go;
    }

    public static PoisonPuddle Create(Vector2 pos, float radius, float duration, float baseDps,
        bool canCrit, float critChance, float critMult)
    {
        var pool = ObjectPool.Instance;
        GameObject go = null;
        if (pool != null && pool.HasPool(PoolHelper.DOT_POISON_PUDDLE))
        {
            go = pool.Spawn(PoolHelper.DOT_POISON_PUDDLE, pos, Quaternion.identity);
        }
        else
        {
            PoolHelper.RegisterVirtualPrefab(PoolHelper.DOT_POISON_PUDDLE, BuildPuddleTemplate, 8);
            go = pool != null ? pool.Spawn(PoolHelper.DOT_POISON_PUDDLE, pos, Quaternion.identity) : null;
        }

        if (go == null)
        {
            go = new GameObject("PoisonPuddle");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(0.1f, 0.7f, 0.1f, 0.4f); sr.sortingOrder = 1;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            go.AddComponent<PoisonPuddle>();
        }

        go.transform.position = pos;
        go.SetActive(true);
        var p = go.GetComponent<PoisonPuddle>();
        p.Setup(radius, duration, baseDps, canCrit, critChance, critMult);
        return p;
    }
}

/// <summary>
/// 中毒叠加效果 — 每tick掉2滴血，固定1秒间隔，每层+1伤害
/// </summary>
public class PoisonStackEffect : MonoBehaviour
{
    private int _stacks;
    public bool _canCrit; public float _critChance, _critMult;
    private float _tickAccumulator;
    private Damageable _damageable;
    private DotColorBlender _blender;
    private const float BASE_TICK_INTERVAL = 1f;
    private const float TICK_DECAY = 0.9f;
    private const float MIN_TICK_INTERVAL = 0.2f;
    private const int DAMAGE_PER_TICK = 2;
    private const int MAX_STACKS = 20;

    public int StackCount => _stacks;

    public void AddStack(float dps, float remainingTime, bool canCrit, float critChance, float critMult)
    {
        if (_stacks >= MAX_STACKS) return;
        _stacks++;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }

    private void Start()
    {
        _damageable = GetComponent<Damageable>();
        _blender = GetComponent<DotColorBlender>();
    }

    private void Update()
    {
        if (_stacks <= 0) { Cleanup(); return; }
        if (_damageable != null && _damageable.CurrentHp <= 0) { Cleanup(); return; }

        // 通过 DotColorBlender 更新中毒颜色贡献
        if (_blender == null) _blender = DotBulletHelper.EnsureColorBlender(gameObject);
        if (_blender != null)
        {
            float intensity = Mathf.Clamp01(_stacks / 10f);
            _blender.RegisterDot("poison", DotColorBlender.POISON_GREEN, intensity, 6f);
        }

        float tickInterval = BASE_TICK_INTERVAL;
        _tickAccumulator += Time.deltaTime;
        if (_tickAccumulator < tickInterval) return;
        _tickAccumulator -= tickInterval;

        if (_damageable != null && _damageable.CurrentHp > 0)
        {
            int dmg = DAMAGE_PER_TICK + (_stacks - 1);
            if (_canCrit && Random.value < _critChance) dmg = Mathf.RoundToInt(dmg * _critMult);
            _damageable.TakeDamage(dmg, new Color(0.1f, 0.8f, 0.1f));
        }
    }

    private void Cleanup() { UnregisterColor(); _stacks = 0; Destroy(this); }
    private void OnDestroy() { UnregisterColor(); }
    private void UnregisterColor() { if (_blender != null) _blender.UnregisterDot("poison"); }
}