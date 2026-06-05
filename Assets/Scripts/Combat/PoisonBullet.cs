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

    public void Setup(float speed, float poisonDps, float poisonDuration, float explosionRadius,
        float dmgMult, bool canCrit, float critChance, float critMult)
    {
        _speed = speed; _poisonDps = poisonDps; _poisonDuration = poisonDuration;
        _explosionRadius = explosionRadius; _damageMultiplier = dmgMult;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }
    public void SetDirection(Vector2 dir) { _direction = dir.normalized; }
    private void Start() { _spawnTime = Time.time; }
    private void Update() { if (!_exploded && Time.time - _spawnTime > _lifetime) Destroy(gameObject); }
    private void FixedUpdate() { if (!_exploded) GetComponent<Rigidbody2D>().linearVelocity = _direction * _speed; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_exploded) return;
        if (!other.CompareTag("Enemy")) return;
        DotBulletHelper.EnsureStatusEffectManager(other.gameObject);
        LeavePuddle(transform.position);
    }

    private void LeavePuddle(Vector2 center)
    {
        _exploded = true;
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
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
        Destroy(gameObject);
    }

    public static PoisonBullet Create(Vector2 pos, Vector2 dir, float speed,
        float poisonDps, float poisonDuration, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var go = new GameObject("PoisonBullet");
        go.transform.position = pos; go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(0.1f, 0.9f, 0.2f); sr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * 0.25f;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.25f;
        DotBulletVisualEffects.AttachTrail(go, new Color(0.1f, 0.9f, 0.2f, 0.6f), 0.4f, 0.03f);
        var b = go.AddComponent<PoisonBullet>();
        b.Setup(speed, poisonDps, poisonDuration, 0.5f, dmgMult, canCrit, critChance, critMult);
        b.SetDirection(dir);
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

    public void Setup(float radius, float duration, float baseDps, bool canCrit, float critChance, float critMult)
    {
        _radius = radius; _duration = duration; _baseDps = baseDps;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }

    private void Start()
    {
        _spawnTime = Time.time; _lastTick = Time.time - 0.5f;
        transform.localScale = Vector3.one * _radius;
        var col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true; col.radius = _radius;
    }

    private void Update()
    {
        if (Time.time - _spawnTime > _duration) { Destroy(gameObject); return; }
        if (Time.time - _lastTick < 0.5f) return;
        _lastTick = Time.time;
        ApplyPoisonToNearby();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        if (Time.time - _lastTick < 0.5f) return;
        var dmg = other.GetComponent<Damageable>();
        if (dmg == null || dmg.CurrentHp <= 0) return;
        var poison = other.GetComponent<PoisonStackEffect>();
        if (poison == null) poison = other.gameObject.AddComponent<PoisonStackEffect>();
        poison.AddStack(_baseDps, _duration - (Time.time - _spawnTime), _canCrit, _critChance, _critMult);
    }

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

    public static PoisonPuddle Create(Vector2 pos, float radius, float duration, float baseDps,
        bool canCrit, float critChance, float critMult)
    {
        var go = new GameObject("PoisonPuddle");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(0.1f, 0.7f, 0.1f, 0.4f); sr.sortingOrder = 1;
        var p = go.AddComponent<PoisonPuddle>();
        p.Setup(radius, duration, baseDps, canCrit, critChance, critMult);
        return p;
    }
}

/// <summary>
/// 中毒叠加效果 — 每tick掉2滴血，tick间隔随层数加速
/// </summary>
public class PoisonStackEffect : MonoBehaviour
{
    private int _stacks;
    public bool _canCrit; public float _critChance, _critMult;
    private float _tickAccumulator;
    private Damageable _damageable;
    private SpriteRenderer _sr;
    private Color _originalColor;
    private const float BASE_TICK_INTERVAL = 1f;
    private const float TICK_DECAY = 0.9f;
    private const float MIN_TICK_INTERVAL = 0.2f;
    private const int DAMAGE_PER_TICK = 2;

    public int StackCount => _stacks;

    public void AddStack(float dps, float remainingTime, bool canCrit, float critChance, float critMult)
    {
        _stacks++;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }

    private void Start()
    {
        _damageable = GetComponent<Damageable>();
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _originalColor = _sr.color;
    }

    private void Update()
    {
        if (_stacks <= 0) { Cleanup(); return; }
        if (_damageable != null && _damageable.CurrentHp <= 0) { Cleanup(); return; }

        if (_sr != null)
        {
            float pulse = Mathf.Sin(Time.time * 8f) * 0.3f;
            _sr.color = Color.Lerp(_originalColor, new Color(0.1f, 0.8f, 0.1f), 0.5f + pulse * 0.2f);
        }

        float tickInterval = Mathf.Max(MIN_TICK_INTERVAL, BASE_TICK_INTERVAL * Mathf.Pow(TICK_DECAY, _stacks - 1));
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

    private void Cleanup() { if (_sr != null) _sr.color = _originalColor; _stacks = 0; Destroy(this); }
    private void OnDestroy() { if (_sr != null) _sr.color = _originalColor; }
}