using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家控制器，使用 New Input System 处理输入和移动。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Damageable))]
[RequireComponent(typeof(BaseEntity))]
public class PlayerController : MonoBehaviour
{
    [Header("移动")]
    [SerializeField] private float _moveSpeed = 20f;

    [Header("碰撞")]
    [SerializeField] private float _contactDamageCooldown = 1f;
    [SerializeField] private int _contactDamageToEnemy = 10;

    private Rigidbody2D _rb;
    private Damageable _damageable;
    private BaseEntity _baseEntity;
    private Vector2 _moveInput;
    private Vector2 _lastMoveDirection = Vector2.right;
    private float _lastContactTime;
    private bool _isDead = false;

    // ── HP 自动回复系统（P1-24）──
    private float _hpRegen;
    private float _regenAccumulator;

    public float MoveSpeed { get => _moveSpeed; set => _moveSpeed = value; }
    public Vector2 LastMoveDirection => _lastMoveDirection;
    public Damageable Damageable => _damageable;

    private void Awake()
    {
        PhysicsLayerSetup.SetAsPlayer(gameObject); // #17 Player Layer
        _rb = GetComponent<Rigidbody2D>();
        _damageable = GetComponent<Damageable>();
        _baseEntity = GetComponent<BaseEntity>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
        _rb.mass = 100f; // 玩家质量大，敌人无法轻易推走
    }

    /// <summary>
    /// 初始化永久加成（选择完成后调用）
    /// </summary>
    public void InitPermanentBonuses()
    {
        if (SaveManager.Instance == null) return;
        _hpRegen = SaveManager.Instance.GetPermanentBonus("hp_regen");
    }

    private void OnEnable()
    {
        // 统一订阅 BaseEntity.OnDeath 事件
        _baseEntity.OnDeath += OnPlayerDeath;
    }

    private void OnDisable()
    {
        _baseEntity.OnDeath -= OnPlayerDeath;
    }

    private void Update()
    {
        if (_isDead) return;

        // 使用 New Input System 读取键盘输入
        var kb = Keyboard.current;
        if (kb == null) return;

        float h = 0f;
        float v = 0f;

        if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) h = -1f;
        if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) h = 1f;
        if (kb.downArrowKey.isPressed || kb.sKey.isPressed) v = -1f;
        if (kb.upArrowKey.isPressed || kb.wKey.isPressed) v = 1f;

        _moveInput = new Vector2(h, v);

        if (h != 0 || v != 0)
        {
            _lastMoveDirection = _moveInput.normalized;
        }

        // Debug: T 键攻击最近敌人
        if (kb.tKey.wasPressedThisFrame)
        {
            DebugAttackNearestEnemy();
        }
        // Debug: H 键治疗
        if (kb.hKey.wasPressedThisFrame)
        {
            _damageable.Heal(20);
        }

        // ── HP 自动回复（P1-24）──
        if (_hpRegen > 0 && _damageable.CurrentHp > 0 && _damageable.CurrentHp < _damageable.MaxHp)
        {
            _regenAccumulator += _hpRegen * Time.deltaTime;
            if (_regenAccumulator >= 1f)
            {
                int heal = Mathf.FloorToInt(_regenAccumulator);
                _damageable.Heal(heal);
                _regenAccumulator -= heal;
            }
        }
    }

    private void FixedUpdate()
    {
        if (_isDead) { _rb.linearVelocity = Vector2.zero; return; }

        // 暂停或游戏结束时停止移动
        if (GameManager.Instance != null && 
            (GameManager.Instance.CurrentState == GameManager.GameState.Paused ||
             GameManager.Instance.CurrentState == GameManager.GameState.GameOver))
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        _rb.linearVelocity = _moveInput * _moveSpeed;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (_isDead) return;
        if (Time.time - _lastContactTime < _contactDamageCooldown) return;
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // 使用敌人的 ContactDamage 而非硬编码值
            var enemyBase = collision.gameObject.GetComponent<EnemyBase>();
            int damage = enemyBase != null ? enemyBase.ContactDamage : 10;
            _damageable.TakeDamage(damage);
            _lastContactTime = Time.time;
        }
    }

    private void OnPlayerDeath(Vector3 deathPosition)
    {
        _isDead = true;
        _rb.linearVelocity = Vector2.zero;
        if (GameManager.Instance != null) GameManager.Instance.EndGame();
    }

    private void DebugAttackNearestEnemy()
    {
        var enemies = FindObjectsByType<EnemyBase>();
        EnemyBase nearest = null;
        float minDist = float.MaxValue;
        foreach (var enemy in enemies)
        {
            if (!enemy.Alive) continue;
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < minDist) { minDist = dist; nearest = enemy; }
        }
        if (nearest != null)
        {
            var dmg = nearest.GetComponent<Damageable>();
            if (dmg != null) dmg.TakeDamage(_contactDamageToEnemy);
        }
    }

    public float HpPercent => _damageable != null ? _damageable.HpPercent : 0f;
}