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
    [SerializeField] private float _moveSpeed = 0.75f;
    [SerializeField] private int _contactDamage = 10;
    [SerializeField] private float _attackCooldown = 1f;

    protected Rigidbody2D _rb;
    private Damageable _damageable;
    private KillRewarder _killRewarder;
    protected Transform _target;
    private float _lastAttackTime;
    private EnemyHealthBar _healthBar;

    public Damageable CachedDamageable => _damageable;
    public Rigidbody2D CachedRigidbody => _rb;

    public static readonly System.Collections.Generic.List<EnemyBase> AllAlive = new System.Collections.Generic.List<EnemyBase>(64);

    // ── #15 距离分级 LOD 系统 ──
    private static int _globalFrameCounter = 0;
    private int _aiUpdateInterval = 1;      // 每 N 帧更新一次 AI
    private int _lastAiUpdateFrame = -1;
    private bool _skipSpecialAbility = false; // 远距离跳过特殊能力

    /// <summary>
    /// 当前帧是否应更新 AI（由 FixedUpdate 中的距离检测设置）
    /// 子类在自己的 Update/FixedUpdate 中应检查此属性
    /// </summary>
    protected bool ShouldUpdateThisFrame { get; private set; } = true;

    /// <summary>
    /// 是否应跳过特殊能力更新（30 格以外）
    /// </summary>
    public bool SkipSpecialAbility => _skipSpecialAbility;

    public float MoveSpeed { get => _moveSpeed; set => _moveSpeed = value; }
    /// <summary>
    /// 基础移速（初始值），供DOT效果恢复使用，避免多效果叠加时速度错乱
    /// </summary>
    public float BaseMoveSpeed { get; private set; }
    public int ContactDamage => _contactDamage;

    // ── 集中速度管理（FrostEffect/StaticStackEffect 只设置这些标志，不直接改 MoveSpeed） ──
    /// <summary>霜冻减速乘数（0~1，1=无减速，0=完全停止）</summary>
    public float FrostSlowMultiplier { get; set; } = 1f;
    public float PoisonSwampMultiplier { get; set; } = 1f;
    /// <summary>是否处于静电硬直中</summary>
    public bool IsStaticStunned { get; set; } = false;

    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
        BaseMoveSpeed = _moveSpeed; // 缓存初始移速
        _damageable = GetComponent<Damageable>();
        _killRewarder = GetComponent<KillRewarder>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
        _rb.mass = 1f; // 敌人质量小，玩家可以轻松推开

        // 在 Awake 中一次性获取或创建 EnemyHealthBar，避免 OnEnable 中重复 AddComponent
        _healthBar = GetComponent<EnemyHealthBar>();
        if (_healthBar == null)
        {
            _healthBar = gameObject.AddComponent<EnemyHealthBar>();
        }
    }

    /// <summary>
    /// 注册死亡事件。子类若重写 OnEnable，应调用 base.OnEnable()。
    /// </summary>
    protected override void OnEnable()
    {
        PhysicsLayerSetup.SetAsEnemy(gameObject);
        base.OnEnable();
        RegisterDeathEvent();
        if (!AllAlive.Contains(this)) AllAlive.Add(this);

        _moveSpeed = BaseMoveSpeed;
        FrostSlowMultiplier = 1f;
        PoisonSwampMultiplier = 1f;
        IsStaticStunned = false;

        // 对象池回收时重置血条状态（组件已在 Awake 中创建）
        if (_damageable == null)
            _damageable = GetComponent<Damageable>();
        if (_healthBar == null)
            _healthBar = GetComponent<EnemyHealthBar>();

        // #33 Debug 面板：应用敌人血量倍率
        if (_damageable != null)
        {
            float hpMult = DebugConfigPanel.DebugEnemyHpMultiplier;
            if (Mathf.Abs(hpMult - 1f) > 0.001f)
            {
                int scaledMaxHp = Mathf.RoundToInt(_damageable.MaxHp * hpMult);
                _damageable.SetMaxHp(scaledMaxHp);
                _damageable.Heal(scaledMaxHp); // 回满
            }
        }

        if (_healthBar != null)
            _healthBar.Setup(_damageable);
    }

    /// <summary>
    /// 注销死亡事件。子类若重写 OnDisable，应调用 base.UnregisterDeathEvent()。
    /// </summary>
    protected virtual void OnDisable()
    {
        AllAlive.Remove(this);
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

        // #15 距离分级 LOD：根据与玩家的距离决定更新频率
        _globalFrameCounter++;
        float distSqr = (_target.position - transform.position).sqrMagnitude;

        // 距离分级（平方距离，避免开方）
        // 0-15 格：每帧更新 → interval=1
        // 15-30 格：每 3 帧更新 → interval=3
        // 30+ 格：每 10 帧更新 → interval=10
        if (distSqr > 900f)         // >30 格
        {
            _aiUpdateInterval = 10;
            _skipSpecialAbility = true;
        }
        else if (distSqr > 225f)    // >15 格
        {
            _aiUpdateInterval = 3;
            _skipSpecialAbility = false;
        }
        else
        {
            _aiUpdateInterval = 1;
            _skipSpecialAbility = false;
        }

        // 判断当前帧是否需要更新
        ShouldUpdateThisFrame = (_globalFrameCounter % _aiUpdateInterval == 0);

        // 远距离敌人仍然移动，但只在 AI 更新帧重新计算方向
        if (ShouldUpdateThisFrame || _lastAiUpdateFrame < 0)
        {
            Vector2 direction = (_target.position - transform.position).normalized;
            // 集中计算实际速度：基础速度 × 霜冻减速 × 静电硬直
            float effectiveSpeed = IsStaticStunned ? 0f : BaseMoveSpeed * FrostSlowMultiplier * PoisonSwampMultiplier;
            _rb.linearVelocity = direction * effectiveSpeed * DebugConfigPanel.DebugEnemySpeedMultiplier;
            _lastAiUpdateFrame = _globalFrameCounter;
        }
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
    /// #28 集成死亡动画特效（缩小+爆炸粒子）
    /// </summary>
    protected void OnEnemyDeathHandler(Vector3 deathPosition)
    {
        _rb.linearVelocity = Vector2.zero;

        // #28 播放死亡特效（在对象回收/销毁之前，因为需要读取颜色/Sprite）
        var sr = GetComponent<SpriteRenderer>();
        Color enemyColor = sr != null ? sr.color : Color.white;
        bool isBoss = GetComponent<BossEnemy>() != null;
        EnemyDeathEffect.PlayDeathEffect(gameObject, enemyColor, isBoss);

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
        BaseMoveSpeed = speed; // 同步基础移速，供DOT效果恢复使用
        _contactDamage = damage;
        _killRewarder.SetRewards(xpReward, coinReward);
    }
}