using UnityEngine;

/// <summary>
/// 霜冻子弹 — 快速子弹，冰冻敌人 + 永久减速 + 霜伤
/// 从 DotProjectile.cs 拆分而来
/// </summary>
public class FrostBullet : MonoBehaviour
{
    private float _speed = 20f;
    private float _lifetime = 2f;
    private int _impactDamage = 6;
    private float _frostDps = 2f;
    private float _freezeDuration = 1f;
    private float _slowPercent = 0.3f;
    private float _damageMultiplier = 1f;
    private bool _canCrit; private float _critChance, _critMult;
    private Vector2 _direction;
    private float _spawnTime;

    public void Setup(float speed, int impactDmg, float frostDps, float freezeDuration,
        float slowPercent, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        _speed = speed; _impactDamage = impactDmg; _frostDps = frostDps;
        _freezeDuration = freezeDuration; _slowPercent = slowPercent;
        _damageMultiplier = dmgMult;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }
    public void SetDirection(Vector2 dir) { _direction = dir.normalized; }
    private void Start() { _spawnTime = Time.time; }
    private void Update() { if (Time.time - _spawnTime > _lifetime) Destroy(gameObject); }
    private void FixedUpdate() { GetComponent<Rigidbody2D>().linearVelocity = _direction * _speed; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        var dmg = other.GetComponent<Damageable>();
        if (dmg != null && dmg.CurrentHp > 0)
        {
            dmg.TakeDamage(Mathf.RoundToInt(_impactDamage * _damageMultiplier));
            DotBulletHelper.EnsureStatusEffectManager(other.gameObject);
            var frost = other.GetComponent<FrostEffect>();
            if (frost == null) frost = other.gameObject.AddComponent<FrostEffect>();
            frost.ApplyFreeze(_freezeDuration, _slowPercent, _frostDps * _damageMultiplier, _canCrit, _critChance, _critMult);
        }
        var penetrate = GetComponent<PenetrateHandler>();
        if (penetrate != null && penetrate.TryPenetrate(other)) return;
        var ricochet = GetComponent<RicochetHandler>();
        if (ricochet != null && ricochet.TryRicochet(transform.position, other)) return;
        Destroy(gameObject);
    }

    public static FrostBullet Create(Vector2 pos, Vector2 dir, float speed, int impactDmg,
        float frostDps, float freezeDuration, float slowPercent, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var go = new GameObject("FrostBullet");
        go.transform.position = pos; go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.Get(); sr.color = new Color(0.3f, 0.6f, 1f); sr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * 0.5f;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(0.4f, 0.2f);
        DotBulletVisualEffects.AttachFrostTrail(go);
        var b = go.AddComponent<FrostBullet>();
        b.Setup(speed, impactDmg, frostDps, freezeDuration, slowPercent, dmgMult, canCrit, critChance, critMult);
        b.SetDirection(dir);
        return b;
    }
}

/// <summary>
/// 霜冻效果 — 冰冻 + 永久减速（30%基础 + 每层额外5%，最低90%）
/// </summary>
public class FrostEffect : MonoBehaviour
{
    private float _freezeEndTime;
    public float _slowPercent;
    public float _frostDps;
    public bool _canCrit; public float _critChance, _critMult;
    private bool _frozen;
    private EnemyBase _enemyBase;
    private Damageable _damageable;
    private float _originalSpeed;
    private SpriteRenderer _sr;
    private Color _originalColor;
    private bool _speedCaptured;
    private int _frostStacks = 0;

    private const float BASE_SLOW = 0.30f;
    private const float PER_STACK_SLOW = 0.05f;
    private const float MAX_SLOW = 0.90f;

    public void ApplyFreeze(float freezeDuration, float slowPercent, float frostDps,
        bool canCrit, float critChance, float critMult)
    {
        _freezeEndTime = Time.time + freezeDuration;
        _frostDps = frostDps;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
        _frozen = true;
        _frostStacks++;
        _slowPercent = Mathf.Min(MAX_SLOW, BASE_SLOW + (_frostStacks - 1) * PER_STACK_SLOW);

        if (_enemyBase == null) _enemyBase = GetComponent<EnemyBase>();
        if (_enemyBase != null)
        {
            if (!_speedCaptured)
            {
                _originalSpeed = _enemyBase.MoveSpeed;
                _speedCaptured = true;
            }
            _enemyBase.MoveSpeed = 0f;
        }
    }

    public int FrostStacks => _frostStacks;

    private void OnEnable()
    {
        RestoreSpeed();
        _frozen = false;
        _freezeEndTime = 0f;
        _speedCaptured = false;
        _slowPercent = 0f;
        _frostStacks = 0;
    }

    private void Start()
    {
        if (_enemyBase == null) _enemyBase = GetComponent<EnemyBase>();
        _damageable = GetComponent<Damageable>();
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _originalColor = _sr.color;
    }

    private void Update()
    {
        if (_damageable == null || _damageable.CurrentHp <= 0) { Cleanup(); return; }

        if (_frozen && Time.time < _freezeEndTime)
        {
            if (_sr != null) _sr.color = new Color(0.3f, 0.5f, 1f);
            if (_enemyBase != null) _enemyBase.MoveSpeed = 0f;
        }
        else if (_frozen)
        {
            _frozen = false;
            if (_enemyBase != null)
                _enemyBase.MoveSpeed = _originalSpeed * (1f - _slowPercent);
            if (_sr != null) _sr.color = Color.Lerp(_originalColor, new Color(0.5f, 0.7f, 1f), 0.3f);
        }
    }

    private void Cleanup()
    {
        RestoreSpeed();
        if (_sr != null) _sr.color = _originalColor;
        Destroy(this);
    }

    private void RestoreSpeed()
    {
        if (_enemyBase != null && _speedCaptured)
        {
            _enemyBase.MoveSpeed = _originalSpeed;
            _speedCaptured = false;
        }
    }

    private void OnDisable() { RestoreSpeed(); }
    private void OnDestroy() { RestoreSpeed(); if (_sr != null) _sr.color = _originalColor; }
}