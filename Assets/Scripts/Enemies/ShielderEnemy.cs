using UnityEngine;

/// <summary>
/// 护盾敌人 - 为自己和附近敌人提供伤害减免。
/// 对应 Python 版的 Shielder 敌人
/// 
/// 行为：追踪玩家 → 范围内敌人获得50%伤害减免 → 护盾有CD
/// </summary>
public class ShielderEnemy : EnemyBase
{
    [Header("护盾属性")]
    [SerializeField] private float _shieldRadius = 4f;
    [SerializeField] private float _damageReduction = 0.5f; // 50% 减伤
    [SerializeField] private Color _shieldColor = new Color(0.3f, 0.6f, 1f);

    private Transform _target;
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private float _shieldPulse;

    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
    }

    private new void Start()
    {
        var player = GameReferences.Player;
        if (player != null) _target = player.transform;
    }

    private void FixedUpdate()
    {
        if (!Alive || _target == null) return;
        Vector2 dir = (_target.position - transform.position).normalized;
        _rb.linearVelocity = dir * MoveSpeed * 0.7f;
    }

    private void Update()
    {
        if (!Alive) return;

        // 自身护盾视觉
        _shieldPulse += Time.deltaTime * 3f;
        if (_sr != null)
        {
            float alpha = Mathf.Sin(_shieldPulse) * 0.15f + 0.85f;
            _sr.color = new Color(_shieldColor.r, _shieldColor.g, _shieldColor.b, alpha);
        }

        // 为范围内友军提供护盾（通过修改 Damageable 的护甲来实现）
        ApplyShieldToNearby();
    }

    private void ApplyShieldToNearby()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _shieldRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            if (hit.gameObject == gameObject) continue;

            var dmg = hit.GetComponent<Damageable>();
            if (dmg != null)
            {
                // 简化：临时增加护甲值来模拟减伤
                int extraArmor = Mathf.RoundToInt(10 * _damageReduction); // 模拟50%减伤
                dmg.SetArmor(extraArmor);
            }
        }
    }

    private new void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _shieldRadius);
    }
}