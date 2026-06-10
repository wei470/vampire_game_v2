using UnityEngine;

/// <summary>
/// 投掷敌人 - 远距离向玩家投掷抛物线弹丸。
/// 对应 Python 版的 Thrower 敌人
/// 
/// 行为：追踪玩家 → 进入射程后停下 → 抛物线投掷 → 冷却后继续
/// </summary>
public class ThrowerEnemy : EnemyBase
{
    [Header("投掷攻击")]
    [SerializeField] private float _attackRange = 10f;
    [SerializeField] private float _throwCooldown = 2.5f;
    [SerializeField] private int _throwDamage = 20;
    [SerializeField] private float _throwSpeed = 6f;
    [SerializeField] private float _throwArcHeight = 3f;

    private float _lastThrowTime;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        var player = GameReferences.Player;
        if (player != null) _target = player.transform;
    }

    private void FixedUpdate()
    {
        if (!Alive || _target == null) return;

        float dist = Vector3.Distance(transform.position, _target.position);
        if (dist > _attackRange)
        {
            Vector2 dir = (_target.position - transform.position).normalized;
            _rb.linearVelocity = dir * MoveSpeed;
        }
        else
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }

    private void Update()
    {
        if (!Alive || _target == null) return;

        // #15 距离 LOD：远距离跳过投掷逻辑
        if (SkipSpecialAbility) return;

        float dist = Vector3.Distance(transform.position, _target.position);
        if (dist <= _attackRange && Time.time - _lastThrowTime >= _throwCooldown)
        {
            Throw();
            _lastThrowTime = Time.time;
        }
    }

    private void Throw()
    {
        Vector2 dir = (_target.position - transform.position).normalized;

        var go = new GameObject("ThrowBomb");
        go.transform.position = transform.position;

        go.transform.localScale = Vector3.one * 0.5f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Circle;
        sr.color = new Color(0.8f, 0.4f, 0.1f);
        sr.sortingOrder = 12;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0.5f; // 轻微重力产生抛物线
        rb.linearVelocity = dir * _throwSpeed + Vector2.up * _throwArcHeight;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        var bullet = go.AddComponent<EnemyBullet>();
        bullet.Setup(_throwDamage, 4f);

        DebugHelper.Log($"[ThrowerEnemy] {gameObject.name} threw a bomb");
    }
}
