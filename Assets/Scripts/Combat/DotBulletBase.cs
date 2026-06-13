using UnityEngine;

/// <summary>
/// DOT 子弹基类 — 提取所有 DOT 子弹的公共逻辑
/// 包含：速度/生命周期/方向管理/Rigidbody2D/穿透/反弹/自动销毁
/// 
/// 子类只需实现 OnHitEnemy() 来定义各自的 DOT 效果
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class DotBulletBase : MonoBehaviour
{
    [SerializeField] protected float _speed = 12f;
    [SerializeField] protected float _lifetime = 4f;
    protected int _impactDamage;
    protected float _damageMultiplier = 1f;
    protected bool _canCrit;
    protected float _critChance;
    protected float _critMult;
    protected Vector2 _direction;
    protected float _spawnTime;
    protected Rigidbody2D _rb;
    protected PenetrateHandler _cachedPenetrate;

    /// <summary>子弹 DOT 类型（用于日志/调试）</summary>
    protected abstract StatusEffectType EffectType { get; }

    /// <summary>命中敌人时应用 DOT 效果（子类必须实现）</summary>
    protected abstract void OnHitEnemy(GameObject enemy);

    /// <summary>可选：命中后的额外逻辑（如元素反应）</summary>
    protected virtual void OnHitExtra(Collider2D hitCollider) { }

    /// <summary>可选：子弹被回收前的清理</summary>
    protected virtual void OnBulletDespawn() { }

    /// <summary>
    /// 通用初始化方法 — 子类可调用或通过各自的 Create() 工厂方法调用
    /// </summary>
    public void SetupBullet(float speed, float lifetime, int impactDamage,
        float dmgMult, bool canCrit, float critChance, float critMult)
    {
        _speed = speed;
        _lifetime = lifetime;
        _impactDamage = impactDamage;
        _damageMultiplier = dmgMult;
        _canCrit = canCrit;
        _critChance = critChance;
        _critMult = critMult;
    }

    public void SetDirection(Vector2 dir)
    {
        _direction = dir.normalized;
        RotateToDirection();
    }

    protected void RotateToDirection()
    {
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _cachedPenetrate = GetComponent<PenetrateHandler>();
    }

    protected virtual void OnEnable()
    {
        _spawnTime = Time.time;
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        if (_rb != null) _rb.linearVelocity = Vector2.zero;
        _cachedPenetrate = GetComponent<PenetrateHandler>();
    }

    protected virtual void Update()
    {
        if (Time.time - _spawnTime > _lifetime) DespawnSelf();
    }

    protected virtual void FixedUpdate()
    {
        if (_rb != null) _rb.linearVelocity = _direction * _speed;
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        var dmg = other.GetComponent<Damageable>();
        if (dmg != null && dmg.CurrentHp > 0)
        {
            DotBulletHelper.EnsureStatusEffectManager(other.gameObject);
            OnHitEnemy(other.gameObject);
            OnHitExtra(other);
        }
        if (_cachedPenetrate != null && _cachedPenetrate.TryPenetrate(other)) return;
        // 懒加载回退：OnEnable 时 PenetrateHandler 可能还未添加
        if (_cachedPenetrate == null)
        {
            _cachedPenetrate = GetComponent<PenetrateHandler>();
            if (_cachedPenetrate != null && _cachedPenetrate.TryPenetrate(other)) return;
        }
        var ricochet = GetComponent<RicochetHandler>();
        if (ricochet != null && ricochet.TryRicochet(transform.position, other)) return;
        DespawnSelf();
    }

    protected void DespawnSelf()
    {
        OnBulletDespawn();
        if (gameObject.activeInHierarchy)
            gameObject.SetActive(false);
    }
}