#pragma warning disable CS0414
using UnityEngine;

/// <summary>
/// 增强敌人 - 增强附近的其他敌人（攻击力+移速提升）。
/// 对应 Python 版的 Enhancer 敌人
/// 
/// 行为：追踪玩家 → 周期性增强附近敌人 → 增强时发光
/// </summary>
public class EnhancerEnemy : EnemyBase
{
    [Header("增强属性")]
    [SerializeField] private float _enhanceRadius = 5f;
    [SerializeField] private float _enhanceCooldown = 4f;
    [SerializeField] private float _damageBoost = 1.3f;  // 30% 攻击力提升
    [SerializeField] private float _speedBoost = 1.2f;   // 20% 移速提升
    [SerializeField] private float _enhanceDuration = 3f;
    [SerializeField] private Color _enhanceColor = new Color(1f, 0.5f, 0f);

    private float _lastEnhanceTime;
    private Transform _target;
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Color _originalColor;
    private GameObject _enhanceAuraGo; // 橙色增强光环

    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _originalColor = _sr.color;
    }

    private new void Start()
    {
        var player = GameReferences.Player;
        if (player != null) _target = player.transform;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _enhanceAuraGo = EnemyEffectHelper.UpdateCircleAura(
            transform, _enhanceAuraGo, "EnhanceAura",
            _enhanceColor, _enhanceRadius, alpha: 0.12f, sortingOrder: 4);
    }

    private void FixedUpdate()
    {
        if (!Alive || _target == null) return;
        Vector2 dir = (_target.position - transform.position).normalized;
        _rb.linearVelocity = dir * MoveSpeed * 0.5f; // 增强者移动很慢
    }

    private void Update()
    {
        if (!Alive) return;

        if (Time.time - _lastEnhanceTime >= _enhanceCooldown)
        {
            EnhanceNearby();
            _lastEnhanceTime = Time.time;
        }
    }

    protected override void OnDisable()
    {
        if (_enhanceAuraGo != null) { Destroy(_enhanceAuraGo); _enhanceAuraGo = null; }
        base.OnDisable();
    }

    private void EnhanceNearby()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _enhanceRadius);
        int enhancedCount = 0;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            if (hit.gameObject == gameObject) continue;

            var enemy = hit.GetComponent<EnemyBase>();
            if (enemy != null && enemy.Alive)
            {
                // 临时加速
                float originalSpeed = enemy.MoveSpeed;
                enemy.MoveSpeed *= _speedBoost;
                StartCoroutine(RestoreSpeed(enemy, originalSpeed));
                enhancedCount++;

                // 视觉反馈 - 闪橙色
                var enemySr = hit.GetComponent<SpriteRenderer>();
                if (enemySr != null)
                {
                    enemySr.color = _enhanceColor;
                    StartCoroutine(RestoreEnemyColor(enemySr, enemySr.color));
                }
            }
        }

        if (enhancedCount > 0)
        {
            DebugHelper.Log($"[EnhancerEnemy] {gameObject.name} enhanced {enhancedCount} allies");
            // 橙色脉冲特效
            EnemyEffectHelper.CreatePulseEffect(transform.position, _enhanceColor, _enhanceRadius * 0.4f, 0.5f);
            if (_sr != null)
            {
                _sr.color = _enhanceColor;
                Invoke(nameof(RestoreColor), 0.5f);
            }
        }
    }

    private System.Collections.IEnumerator RestoreSpeed(EnemyBase enemy, float originalSpeed)
    {
        yield return new WaitForSeconds(_enhanceDuration);
        if (enemy != null) enemy.MoveSpeed = originalSpeed;
    }

    private System.Collections.IEnumerator RestoreEnemyColor(SpriteRenderer sr, Color original)
    {
        yield return new WaitForSeconds(_enhanceDuration);
        if (sr != null) sr.color = original;
    }

    private void RestoreColor()
    {
        if (_sr != null) _sr.color = _originalColor;
    }

    private new void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _enhanceRadius);
    }
}