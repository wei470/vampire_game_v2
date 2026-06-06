using UnityEngine;

/// <summary>
/// 流血子弹 — 命中后附加流血被动效果（敌人移动时受伤）
/// 从 DotProjectile.cs 拆分而来
/// </summary>
public class BleedBullet : MonoBehaviour
{
    private float _speed = 14f;
    private float _lifetime = 3f;
    private int _impactDamage = 3;
    private float _bleedDps = 2f;
    private float _bleedDuration = 4f;
    private float _damageMultiplier = 1f;
    private bool _canCrit;
    private float _critChance, _critMultiplier;
    private Vector2 _direction;
    private float _spawnTime;

    public void Setup(float speed, int impactDmg, float bleedDps, float bleedDuration,
        float dmgMult, bool canCrit, float critChance, float critMult)
    {
        _speed = speed; _impactDamage = impactDmg; _bleedDps = bleedDps;
        _bleedDuration = bleedDuration; _damageMultiplier = dmgMult;
        _canCrit = canCrit; _critChance = critChance; _critMultiplier = critMult;
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
            var bleed = other.GetComponent<BleedEffect>();
            if (bleed == null) bleed = other.gameObject.AddComponent<BleedEffect>();
            bleed.Refresh(_bleedDps * _damageMultiplier, _bleedDuration, _canCrit, _critChance, _critMultiplier);
        }
        var penetrate = GetComponent<PenetrateHandler>();
        if (penetrate != null && penetrate.TryPenetrate(other)) return;
        var ricochet = GetComponent<RicochetHandler>();
        if (ricochet != null && ricochet.TryRicochet(transform.position, other)) return;
        Destroy(gameObject);
    }

    public static BleedBullet Create(Vector2 pos, Vector2 dir, float speed, int impactDmg,
        float bleedDps, float bleedDuration, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var go = new GameObject("BleedBullet");
        go.transform.position = pos;
        go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.Get(); sr.color = new Color(0.9f, 0.1f, 0.1f); sr.sortingOrder = 15;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(0.5f, 0.25f);
        DotBulletVisualEffects.AttachTrail(go, new Color(0.9f, 0.1f, 0.1f, 0.8f), 0.6f, 0.04f);
        var b = go.AddComponent<BleedBullet>();
        b.Setup(speed, impactDmg, bleedDps, bleedDuration, dmgMult, canCrit, critChance, critMult);
        b.SetDirection(dir);
        return b;
    }
}

/// <summary>
/// 流血被动效果 — 挂载到敌人身上，敌人移动时受伤
/// </summary>
public class BleedEffect : MonoBehaviour
{
    public float _dps;
    public float _duration;
    private float _startTime;
    public bool _canCrit; public float _critChance, _critMult;
    private Vector3 _lastPosition;
    private float _damageAccumulator;
    private const float MOVE_THRESHOLD = 0.1f;
    private Damageable _damageable;

    /// <summary>
    /// #19 脓毒组合加成
    /// </summary>
    [System.NonSerialized] public float _comboSepsisBonus = 0f;

    public void Refresh(float dps, float duration, bool canCrit, float critChance, float critMult)
    {
        _dps = Mathf.Max(_dps, dps);
        _duration = duration; // 保留参数兼容，但不用于超时判断
        _startTime = Time.time;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }

    private void Start()
    {
        _startTime = Time.time;
        _lastPosition = transform.position;
        _damageable = GetComponent<Damageable>();
    }

    private void Update()
    {
        // 永久持续，直到敌人死亡
        if (_damageable == null || _damageable.CurrentHp <= 0) { Destroy(this); return; }

        float moved = Vector3.Distance(transform.position, _lastPosition);
        _lastPosition = transform.position;

        if (moved > MOVE_THRESHOLD && _damageable != null && _damageable.CurrentHp > 0)
        {
            float effectiveDps = _dps * (1f + _comboSepsisBonus);
            float dmg = effectiveDps * Time.deltaTime * 3f;
            if (_canCrit && Random.value < _critChance) dmg *= _critMult;
            _damageAccumulator += dmg;

            if (_damageAccumulator >= 1f)
            {
                int intDmg = Mathf.FloorToInt(_damageAccumulator);
                _damageable.TakeDamage(intDmg, new Color(0.9f, 0.15f, 0.15f));
                _damageAccumulator -= intDmg;
            }
        }
    }

    private void OnDestroy()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;
    }
}