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
    private Rigidbody2D _rb;
    private PenetrateHandler _cachedPenetrate;

    public void Setup(float speed, int impactDmg, float frostDps, float freezeDuration,
        float slowPercent, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        _speed = speed; _impactDamage = impactDmg; _frostDps = frostDps;
        _freezeDuration = freezeDuration; _slowPercent = slowPercent;
        _damageMultiplier = dmgMult;
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
            var frost = other.GetComponent<FrostEffect>();
            if (frost == null) frost = other.gameObject.AddComponent<FrostEffect>();
            frost.ApplyFreeze(_freezeDuration, _slowPercent, 0f, false, 0f, 0f);
        }
        if (_cachedPenetrate != null && _cachedPenetrate.TryPenetrate(other)) return;
        var ricochet = GetComponent<RicochetHandler>();
        if (ricochet != null && ricochet.TryRicochet(transform.position, other)) return;
        DespawnSelf();
    }

    private void DespawnSelf()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.DOT_FROST_BULLET);
    }

    private static GameObject BuildTemplate()
    {
        var go = new GameObject("FrostBullet");
        go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.Get(); sr.color = new Color(0.3f, 0.6f, 1f); sr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * 0.5f;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(0.4f, 0.2f);
        DotBulletVisualEffects.AttachFrostTrail(go);
        go.AddComponent<FrostBullet>();
        return go;
    }

    public static FrostBullet Create(Vector2 pos, Vector2 dir, float speed, int impactDmg,
        float frostDps, float freezeDuration, float slowPercent, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var pool = ObjectPool.Instance;
        GameObject go = null;
        if (pool != null && pool.HasPool(PoolHelper.DOT_FROST_BULLET))
        {
            go = pool.Spawn(PoolHelper.DOT_FROST_BULLET, pos, Quaternion.identity);
        }
        else
        {
            PoolHelper.RegisterVirtualPrefab(PoolHelper.DOT_FROST_BULLET, BuildTemplate, 15);
            go = pool != null ? pool.Spawn(PoolHelper.DOT_FROST_BULLET, pos, Quaternion.identity) : null;
        }

        if (go == null)
        {
            go = new GameObject("FrostBullet");
            go.tag = "Untagged";
            PhysicsLayerSetup.SetAsBullet(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = DotSpriteCache.Get(); sr.color = new Color(0.3f, 0.6f, 1f); sr.sortingOrder = 15;
            go.transform.localScale = Vector3.one * 0.5f;
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(0.4f, 0.2f);
            DotBulletVisualEffects.AttachFrostTrail(go);
            go.AddComponent<FrostBullet>();
        }

        go.transform.position = pos;
        go.SetActive(true);
        var b = go.GetComponent<FrostBullet>();
        b.Setup(speed, impactDmg, frostDps, freezeDuration, slowPercent, dmgMult, canCrit, critChance, critMult);
        b.SetDirection(dir);
        b._cachedPenetrate = go.GetComponent<PenetrateHandler>();
        return b;
    }
}

/// <summary>
/// 霜冻效果 — 永久减速（30%基础 + 每层额外5%，最高90%减速），持续直到敌人死亡
/// 不再冻住敌人，只降低移动速度并叠层
/// </summary>
public class FrostEffect : MonoBehaviour
{
    public float _slowPercent;
    public float _frostDps;
    public bool _canCrit; public float _critChance, _critMult;
    private EnemyBase _enemyBase;
    private Damageable _damageable;
    private SpriteRenderer _sr;
    private Color _originalColor;
    private DotColorBlender _blender;
    private int _frostStacks = 0;

    private const float BASE_SLOW = 0.30f;
    private const float PER_STACK_SLOW = 0.05f;
    private const float MAX_SLOW = 0.90f;

    /// <summary>
    /// 施加霜冻减速 — 通过 EnemyBase.FrostSlowMultiplier 集中管理
    /// </summary>
    public void ApplyFreeze(float freezeDuration, float slowPercent, float frostDps,
        bool canCrit, float critChance, float critMult)
    {
        _frostDps = Mathf.Max(_frostDps, frostDps);
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
        _frostStacks++;
        _slowPercent = Mathf.Min(MAX_SLOW, BASE_SLOW + (_frostStacks - 1) * PER_STACK_SLOW);

        // 确保已捕获原始颜色（ApplyFreeze可能在Start之前被调用）
        EnsureColorCaptured();

        if (_enemyBase == null) _enemyBase = GetComponent<EnemyBase>();
        if (_enemyBase != null)
        {
            // 通过集中管理接口设置减速乘数
            _enemyBase.FrostSlowMultiplier = 1f - _slowPercent;
        }
    }

    public int FrostStacks => _frostStacks;

    private void OnEnable()
    {
        _slowPercent = 0f;
        _frostStacks = 0;
        _frostDps = 0f;
        // 恢复霜冻减速乘数（对象池复用时）
        if (_enemyBase == null) _enemyBase = GetComponent<EnemyBase>();
        if (_enemyBase != null) _enemyBase.FrostSlowMultiplier = 1f;
    }

    private void Start()
    {
        if (_enemyBase == null) _enemyBase = GetComponent<EnemyBase>();
        _damageable = GetComponent<Damageable>();
        _blender = GetComponent<DotColorBlender>();
        EnsureColorCaptured();
    }

    /// <summary>确保已捕获原始颜色（ApplyFreeze可能在Start之前被调用）</summary>
    private void EnsureColorCaptured()
    {
        if (_sr == null)
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _originalColor = _sr.color;
        }
    }

    private void Update()
    {
        // 敌人死亡时清理
        if (_damageable == null || _damageable.CurrentHp <= 0) { Cleanup(); return; }

        // 每帧同步霜冻减速乘数到 EnemyBase（由 EnemyBase.FixedUpdate 统一计算速度）
        if (_frostStacks > 0 && _enemyBase != null)
        {
            _enemyBase.FrostSlowMultiplier = 1f - _slowPercent;
        }
    }

    /// <summary>
    /// 在 LateUpdate 中设置颜色，确保在 DotColorBlender 之后执行
    /// 1层≈3%蓝，5层≈15%蓝，20层≈60%蓝，34层以上完全蓝色
    /// </summary>
    private void LateUpdate()
    {
        if (_frostStacks <= 0 || _sr == null) return;
        if (_damageable == null || _damageable.CurrentHp <= 0) return;

        // 注销 DotColorBlender 的 frost 注册（我们自己管理颜色）
        if (_blender != null) _blender.UnregisterDot("frost");

        float intensity = Mathf.Clamp01(_frostStacks / 34f);
        _sr.color = Color.Lerp(_originalColor, DotColorBlender.FROST_BLUE, intensity);
    }

    private void Cleanup()
    {
        // 恢复霜冻减速乘数
        if (_enemyBase != null) _enemyBase.FrostSlowMultiplier = 1f;
        // 恢复原始颜色
        if (_sr != null) _sr.color = _originalColor;
        UnregisterColor();
        Destroy(this);
    }
    private void UnregisterColor()
    {
        if (_blender != null) _blender.UnregisterDot("frost");
    }

    private void OnDisable()
    {
        // 恢复霜冻减速乘数
        if (_enemyBase != null && _frostStacks > 0) _enemyBase.FrostSlowMultiplier = 1f;
        // 恢复原始颜色
        if (_sr != null && _frostStacks > 0) _sr.color = _originalColor;
        UnregisterColor();
    }
    private void OnDestroy() { UnregisterColor(); }
}
