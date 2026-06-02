using UnityEngine;

/// <summary>
/// 远程敌人，保持一定距离后向玩家射击。
/// 对应 Python 版的远程敌人类型
/// 
/// 行为：追踪玩家 → 进入射程后停下 → 发射子弹 → 冷却后继续射击
/// 使用方式：替代 EnemyBase 使用，挂载到敌人 GameObject
/// </summary>
public class RangedEnemy : EnemyBase
{
    [Header("远程攻击")]
    [SerializeField] private float _attackRange = 8f;
    [SerializeField] private float _keepDistance = 5f;     // 保持与玩家的距离
    [SerializeField] private float _shootCooldown = 2f;
    [SerializeField] private int _bulletDamage = 15;
    [SerializeField] private float _bulletSpeed = 8f;
    [SerializeField] private float _bulletLifetime = 5f;

    private float _lastShootTime;
    private Transform _target;
    private Rigidbody2D _rb;

    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
    }

    private new void Start()
    {
        var player = GameReferences.Player;
        if (player != null) _target = player.transform;
    }

    private void FixedUpdate()
    {
        // 安全检查：如果 HP 已归零但 _alive 标记仍为 true，强制触发死亡
        var dmg = GetComponent<Damageable>();
        if (dmg != null && !dmg.IsAlive && Alive)
        {
            Die();
            return;
        }

        if (!Alive || _target == null) return;

        float dist = Vector3.Distance(transform.position, _target.position);

        if (dist > _attackRange)
        {
            // 太远，靠近玩家
            Vector2 dir = (_target.position - transform.position).normalized;
            _rb.linearVelocity = dir * MoveSpeed;
        }
        else if (dist < _keepDistance - 0.5f)
        {
            // 太近，后退
            Vector2 dir = (transform.position - _target.position).normalized;
            _rb.linearVelocity = dir * MoveSpeed * 0.5f;
        }
        else if (dist < _attackRange)
        {
            // 在射程内，停下射击
            _rb.linearVelocity = Vector2.zero;
        }
    }

    private void Update()
    {
        // 安全检查：如果 HP 已归零但 _alive 标记仍为 true，强制触发死亡
        var dmg = GetComponent<Damageable>();
        if (dmg != null && !dmg.IsAlive && Alive)
        {
            Die();
            return;
        }

        if (!Alive || _target == null) return;

        float dist = Vector3.Distance(transform.position, _target.position);

        // 在射程内且冷却结束，射击
        if (dist <= _attackRange && Time.time - _lastShootTime >= _shootCooldown)
        {
            Shoot();
            _lastShootTime = Time.time;
        }
    }

    /// <summary>
    /// 向玩家发射子弹
    /// </summary>
    private void Shoot()
    {
        Vector2 dir = (_target.position - transform.position).normalized;

        // 创建子弹
        var bulletGo = new GameObject("EnemyBullet");
        bulletGo.transform.position = transform.position;
        bulletGo.tag = "Untagged";

        bulletGo.transform.localScale = Vector3.one * 0.4f;

        var sr = bulletGo.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Circle;
        sr.color = new Color(1f, 0.3f, 0.3f); // 红色圆形子弹

        var rb = bulletGo.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearVelocity = dir * _bulletSpeed;

        var col = bulletGo.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        // 添加敌人子弹组件
        var bullet = bulletGo.AddComponent<EnemyBullet>();
        bullet.Setup(_bulletDamage, _bulletLifetime);

        DebugHelper.Log($"[RangedEnemy] {gameObject.name} shot a bullet towards player");
    }

    private new void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _keepDistance);
    }
}