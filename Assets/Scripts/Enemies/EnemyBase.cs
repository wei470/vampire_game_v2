using UnityEngine;

/// <summary>
/// 敌人基类，追踪玩家并造成碰撞伤害。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Damageable))]
[RequireComponent(typeof(KillRewarder))]
public class EnemyBase : BaseEntity
{
    [Header("敌人属性")]
    [SerializeField] private float _moveSpeed = 3f;
    [SerializeField] private int _contactDamage = 10;
    [SerializeField] private float _attackCooldown = 1f;

    private Rigidbody2D _rb;
    private Damageable _damageable;
    private KillRewarder _killRewarder;
    private Transform _target;
    private float _lastAttackTime;
    private EnemyHealthBar _healthBar;

    public float MoveSpeed { get => _moveSpeed; set => _moveSpeed = value; }
    public int ContactDamage => _contactDamage;

    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
        _damageable = GetComponent<Damageable>();
        _killRewarder = GetComponent<KillRewarder>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
        _rb.mass = 1f; // 敌人质量小，玩家可以轻松推开
    }

    /// <summary>
    /// 注册死亡事件。子类若重写 OnEnable，应调用 base.OnEnable()。
    /// </summary>
    protected virtual void OnEnable()
    {
        base.OnEnable(); // 重置 _alive = true
        RegisterDeathEvent();

        // 创建头顶血条
        if (_healthBar == null)
        {
            _healthBar = gameObject.AddComponent<EnemyHealthBar>();
        }
        if (_damageable == null)
            _damageable = GetComponent<Damageable>();
        _healthBar.Setup(_damageable);
    }

    /// <summary>
    /// 注销死亡事件。子类若重写 OnDisable，应调用 base.UnregisterDeathEvent()。
    /// </summary>
    protected virtual void OnDisable()
    {
        UnregisterDeathEvent();
    }

    /// <summary>
    /// 注册死亡事件到 BaseEntity.OnDeath
    /// </summary>
    protected void RegisterDeathEvent()
    {
        OnDeath += OnEnemyDeathHandler;
    }

    /// <summary>
    /// 注销 BaseEntity.OnDeath 事件
    /// </summary>
    protected void UnregisterDeathEvent()
    {
        OnDeath -= OnEnemyDeathHandler;
    }

    protected virtual void Start()
    {
        // 使用全局引用缓存，避免昂贵的 FindAnyObjectByType 调用
        var player = GameReferences.Player;
        if (player != null) _target = player.transform;
    }

    private void FixedUpdate()
    {
        // 安全检查：如果 HP 已归零但 _alive 标记仍为 true，强制触发死亡
        if (_damageable != null && !_damageable.IsAlive && Alive)
        {
            Die();
            return;
        }

        if (!Alive || _target == null) return;
        Vector2 direction = (_target.position - transform.position).normalized;
        _rb.linearVelocity = direction * _moveSpeed;
    }

    private void Update()
    {
        if (_target != null)
        {
            float dist = Vector3.Distance(transform.position, _target.position);
            // 使用配置值而非硬编码
            float despawnDist = ConfigLoader.Game != null ? ConfigLoader.Game.enemyDespawnDistance : 50f;
            if (dist > despawnDist)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!Alive) return;
        if (Time.time - _lastAttackTime < _attackCooldown) return;
        if (collision.gameObject.CompareTag("Player"))
        {
            var dmg = collision.gameObject.GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp > 0)
            {
                dmg.TakeDamage(_contactDamage);
                _lastAttackTime = Time.time;
            }
        }
    }

    /// <summary>
    /// 死亡事件处理（参数为死亡位置）。子类可覆盖 OnEnable/OnDisable 但应确保此方法被调用。
    /// </summary>
    protected void OnEnemyDeathHandler(Vector3 deathPosition)
    {
        _rb.linearVelocity = Vector2.zero;

        // 立即从 SpawnManager 活跃列表移除，避免 0.1s 延迟导致计数错误
        if (GameReferences.SpawnManager != null)
        {
            GameReferences.SpawnManager.RemoveEnemy(gameObject);
        }

        // 优先尝试对象池回收，否则立即销毁（避免延迟销毁导致事件重复触发）
        var dmg = GetComponent<Damageable>();
        if (dmg != null && !string.IsNullOrEmpty(dmg.PoolKey) && ObjectPool.Instance != null && ObjectPool.Instance.HasPool(dmg.PoolKey))
        {
            ObjectPool.Instance.Despawn(dmg.PoolKey, gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetTarget(Transform target) { _target = target; }

    public void Setup(float speed, int damage, int xpReward, int coinReward)
    {
        _moveSpeed = speed;
        _contactDamage = damage;
        _killRewarder.SetRewards(xpReward, coinReward);
    }
}