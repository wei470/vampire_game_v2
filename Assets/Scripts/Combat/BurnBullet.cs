using UnityEngine;

/// <summary>
/// 燃烧子弹 — 慢速橙色子弹，命中叠加燃烧层数
/// 从 DotProjectile.cs 拆分而来
/// </summary>
public class BurnBullet : MonoBehaviour
{
    private float _speed = 12f;
    private float _lifetime = 4f;
    private int _impactDamage = 2;
    private float _burnDps = 2f;
    private float _burnDuration = 3f;
    private float _damageMultiplier = 1f;
    private bool _canCrit; private float _critChance, _critMult;
    private Vector2 _direction;
    private float _spawnTime;

    public void Setup(float speed, int impactDmg, float burnDps, float burnDuration,
        float dmgMult, bool canCrit, float critChance, float critMult)
    {
        _speed = speed; _impactDamage = impactDmg; _burnDps = burnDps;
        _burnDuration = burnDuration; _damageMultiplier = dmgMult;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }
    public void SetDirection(Vector2 dir) { _direction = dir.normalized; RotateToDirection(); }
    private void RotateToDirection() { float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg; transform.rotation = Quaternion.Euler(0, 0, angle); }
    private void Start() { _spawnTime = Time.time; }
    private void Update() { if (Time.time - _spawnTime > _lifetime) Destroy(gameObject); }
    private Rigidbody2D _rb;
    private void Awake() { _rb = GetComponent<Rigidbody2D>(); }
    private void FixedUpdate() { _rb.linearVelocity = _direction * _speed; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        var dmg = other.GetComponent<Damageable>();
        if (dmg != null && dmg.CurrentHp > 0)
        {
            dmg.TakeDamage(Mathf.RoundToInt(_impactDamage * _damageMultiplier));
            DotBulletHelper.EnsureStatusEffectManager(other.gameObject);
            var burn = other.GetComponent<BurnStackEffect>();
            if (burn == null) burn = other.gameObject.AddComponent<BurnStackEffect>();
            burn.AddStack(_burnDps * _damageMultiplier, _burnDuration, _canCrit, _critChance, _critMult);
        }
        var penetrate = GetComponent<PenetrateHandler>();
        if (penetrate != null && penetrate.TryPenetrate(other)) return;
        var ricochet = GetComponent<RicochetHandler>();
        if (ricochet != null && ricochet.TryRicochet(transform.position, other)) return;
        Destroy(gameObject);
    }

    public static BurnBullet Create(Vector2 pos, Vector2 dir, float speed, int impactDmg,
        float burnDps, float burnDuration, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var go = new GameObject("BurnBullet");
        go.transform.position = pos; go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(1f, 0.4f, 0f); sr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * 0.2f;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.25f;
        DotBulletVisualEffects.AttachFlameEffect(go);
        var b = go.AddComponent<BurnBullet>();
        b.Setup(speed, impactDmg, burnDps, burnDuration, dmgMult, canCrit, critChance, critMult);
        b.SetDirection(dir);
        return b;
    }
}

/// <summary>
/// 燃烧叠加效果 — 层数越高，tick间隔越短（最低0.2秒）
/// </summary>
public class BurnStackEffect : MonoBehaviour
{
    private int _stacks;
    public float _baseDps;
    public float _duration;
    public int StackCount => _stacks;
    public float _endTime;
    public bool _canCrit; public float _critChance, _critMult;
    private float _lastTick;
    private Damageable _damageable;
    private SpriteRenderer _sr;
    private Color _originalColor;
    private float _tickAccumulator;

    public void AddStack(float baseDps, float duration, bool canCrit, float critChance, float critMult)
    {
        _stacks++;
        _baseDps = Mathf.Max(_baseDps, baseDps);
        _duration = duration;
        _endTime = Time.time + duration;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }

    private void Start()
    {
        _damageable = GetComponent<Damageable>();
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _originalColor = _sr.color;
        _lastTick = Time.time;
    }

    private void Update()
    {
        // 永久持续，直到敌人死亡
        if (_damageable == null || _damageable.CurrentHp <= 0 || _stacks <= 0) { _stacks = 0; if (_sr != null) _sr.color = _originalColor; Destroy(this); return; }

        if (_sr != null)
        {
            float pulse = Mathf.Sin(Time.time * 10f) * 0.3f;
            _sr.color = Color.Lerp(_originalColor, new Color(1f, 0.5f + pulse, 0f), 0.6f);
        }

        float tickInterval = Mathf.Max(0.2f, 1.0f / _stacks);
        _tickAccumulator += Time.deltaTime;

        if (_tickAccumulator >= tickInterval)
        {
            _tickAccumulator -= tickInterval;
            if (_damageable != null && _damageable.CurrentHp > 0)
            {
                float dmg = _baseDps * tickInterval;
                if (_canCrit && Random.value < _critChance) dmg *= _critMult;
                _damageable.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(dmg)), new Color(1f, 0.5f, 0f));
            }
        }
    }

    private void OnDestroy()
    {
        if (_sr != null) _sr.color = _originalColor;
    }
}