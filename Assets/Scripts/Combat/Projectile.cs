#pragma warning disable CS0618
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 玩家投射物基类，自动向前移动，碰撞敌人造成伤害。
/// 对应 Python: entities/projectiles.py 中的投射物
/// 
/// 使用方式：由 WeaponController 实例化
/// 支持对象池回收 + 屏幕外自动裁剪
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("投射物属性")]
    [SerializeField] private int _damage = 10;
    [SerializeField] private float _speed = 12f;
    [SerializeField] private float _lifetime = 3f;
    [SerializeField] private int _pierce = 1;           // 穿透次数（1 = 碰到第一个敌人后销毁）
    [SerializeField] private float _knockbackForce = 2f; // 击退力

    private Rigidbody2D _rb;
    private Vector2 _direction;
    private float _spawnTime;
    private int _currentPierce;
    private HashSet<int> _hitEnemies = new HashSet<int>(); // 防止重复命中同一敌人
    private float _damageMultiplier = 1f;

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
    }

    private void OnEnable()
    {
        _spawnTime = Time.time;
        _currentPierce = _pierce;
        _hitEnemies.Clear();
        _damageMultiplier = 1f;

        // 确保 Collider 是触发器
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        // 注册到屏幕外裁剪器
        if (OffScreenCuller.Instance != null)
        {
            OffScreenCuller.TrackProjectile(gameObject, GetPoolKey());
        }
    }

    private void OnDisable()
    {
        // 注销屏幕外裁剪
        OffScreenCuller.Untrack(gameObject);
    }

    private void Update()
    {
        // 超时回收
        if (Time.time - _spawnTime > _lifetime)
        {
            DespawnSelf();
        }
    }

    private void FixedUpdate()
    {
        // 持续向前移动
        _rb.linearVelocity = _direction * _speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 碰到敌人
        if (other.CompareTag("Enemy"))
        {
            var dmg = other.GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp > 0)
            {
                // 使用 CombatManager 统一处理穿透逻辑
                bool shouldDespawn = CombatManager.ProcessPierce(
                    ref _currentPierce, dmg, _damage, _damageMultiplier,
                    _hitEnemies, _knockbackForce, transform);

                DebugHelper.Log($"[Projectile] Hit {other.name} for {Mathf.RoundToInt(_damage * _damageMultiplier)} damage (pierce: {_currentPierce})");

                if (shouldDespawn)
                {
                    DespawnSelf();
                }
            }
        }
    }

    /// <summary>
    /// 回收自身（优先使用对象池，否则 Destroy）
    /// </summary>
    protected void DespawnSelf()
    {
        if (ObjectPool.Instance != null)
        {
            string poolKey = GetPoolKey();
            if (poolKey != null && ObjectPool.Instance.HasPool(poolKey))
            {
                ObjectPool.Instance.Despawn(poolKey, gameObject);
                return;
            }
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// 获取对象池键名（子类可重写）
    /// </summary>
    protected virtual string GetPoolKey()
    {
        return PoolHelper.GetPoolKey(this);
    }

    /// <summary>
    /// 设置投射物方向（在实例化后调用）
    /// </summary>
    public void SetDirection(Vector2 direction)
    {
        _direction = direction.normalized;
        _rb.linearVelocity = _direction * _speed;

        // 旋转投射物朝向飞行方向
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// 设置投射物属性
    /// </summary>
    public void Setup(int damage, float speed, float lifetime, int pierce)
    {
        _damage = damage;
        _speed = speed;
        _lifetime = lifetime;
        _pierce = pierce;
        _currentPierce = pierce;
    }

    /// <summary>
    /// 设置伤害倍率（来自升级加成）
    /// </summary>
    public void SetDamageMultiplier(float multiplier)
    {
        _damageMultiplier = multiplier;
    }

    /// <summary>
    /// 设置击退力
    /// </summary>
    public void SetKnockback(float force)
    {
        _knockbackForce = force;
    }
}

