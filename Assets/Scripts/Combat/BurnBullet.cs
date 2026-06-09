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
    private Rigidbody2D _rb;
    private PenetrateHandler _cachedPenetrate;

    public void Setup(float speed, int impactDmg, float burnDps, float burnDuration,
        float dmgMult, bool canCrit, float critChance, float critMult)
    {
        _speed = speed; _impactDamage = impactDmg; _burnDps = burnDps;
        _burnDuration = burnDuration; _damageMultiplier = dmgMult;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }
    public void SetDirection(Vector2 dir) { _direction = dir.normalized; RotateToDirection(); }
    private void RotateToDirection() { float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg; transform.rotation = Quaternion.Euler(0, 0, angle); }

    private void Awake() { _rb = GetComponent<Rigidbody2D>(); _cachedPenetrate = GetComponent<PenetrateHandler>(); }
    private void OnEnable()
    {
        _spawnTime = Time.time;
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        if (_rb != null) _rb.linearVelocity = Vector2.zero;
    }
    private void Update() { if (Time.time - _spawnTime > _lifetime) DespawnSelf(); }
    private void FixedUpdate() { if (_rb != null) _rb.linearVelocity = _direction * _speed; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        var dmg = other.GetComponent<Damageable>();
        if (dmg != null && dmg.CurrentHp > 0)
        {
            DotBulletHelper.EnsureStatusEffectManager(other.gameObject);
            var burn = other.GetComponent<BurnStackEffect>();
            if (burn == null) burn = other.gameObject.AddComponent<BurnStackEffect>();
            burn.AddStack(_burnDps * _damageMultiplier, _burnDuration, _canCrit, _critChance, _critMult);
        }
        if (_cachedPenetrate != null && _cachedPenetrate.TryPenetrate(other)) return;
        var ricochet = GetComponent<RicochetHandler>();
        if (ricochet != null && ricochet.TryRicochet(transform.position, other)) return;
        DespawnSelf();
    }

    private void DespawnSelf()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.DOT_BURN_BULLET);
    }

    private static GameObject BuildTemplate()
    {
        var go = new GameObject("BurnBullet");
        go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(1f, 0.4f, 0f); sr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * 0.2f;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.25f;
        DotBulletVisualEffects.AttachFlameEffect(go);
        go.AddComponent<BurnBullet>();
        return go;
    }

    public static BurnBullet Create(Vector2 pos, Vector2 dir, float speed, int impactDmg,
        float burnDps, float burnDuration, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var pool = ObjectPool.Instance;
        GameObject go = null;
        if (pool != null && pool.HasPool(PoolHelper.DOT_BURN_BULLET))
        {
            go = pool.Spawn(PoolHelper.DOT_BURN_BULLET, pos, Quaternion.identity);
        }
        else
        {
            PoolHelper.RegisterVirtualPrefab(PoolHelper.DOT_BURN_BULLET, BuildTemplate, 15);
            go = pool != null ? pool.Spawn(PoolHelper.DOT_BURN_BULLET, pos, Quaternion.identity) : null;
        }

        if (go == null)
        {
            go = new GameObject("BurnBullet");
            go.tag = "Untagged";
            PhysicsLayerSetup.SetAsBullet(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(1f, 0.4f, 0f); sr.sortingOrder = 15;
            go.transform.localScale = Vector3.one * 0.2f;
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.25f;
            DotBulletVisualEffects.AttachFlameEffect(go);
            go.AddComponent<BurnBullet>();
        }

        go.transform.position = pos;
        go.SetActive(true);
        var b = go.GetComponent<BurnBullet>();
        b.Setup(speed, impactDmg, burnDps, burnDuration, dmgMult, canCrit, critChance, critMult);
        b.SetDirection(dir);
        b._cachedPenetrate = go.GetComponent<PenetrateHandler>();
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
    private int _lastVisualStacks = -1; // 只在层数变化时更新视觉
    private float _lastVisualUpdate;
    private const float VISUAL_UPDATE_INTERVAL = 0.15f;

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
        if (_damageable == null || _damageable.CurrentHp <= 0 || _stacks <= 0) { _stacks = 0; if (_sr != null) _sr.color = _originalColor; Destroy(this); return; }

        // 只在层数变化时更新视觉，降频到0.15s
        if (_sr != null && (_stacks != _lastVisualStacks || Time.time - _lastVisualUpdate >= VISUAL_UPDATE_INTERVAL))
        {
            _lastVisualUpdate = Time.time;
            _lastVisualStacks = _stacks;
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
