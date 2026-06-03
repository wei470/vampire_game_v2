using UnityEngine;

/// <summary>
/// 治疗敌人 - 治疗附近的其他敌人。
/// 对应 Python 版的 Healer 敌人
/// 
/// 行为：追踪玩家 → 周期性治疗附近敌人 → 治疗时短暂停下
/// </summary>
public class HealerEnemy : EnemyBase
{
    [Header("治疗属性")]
    [SerializeField] private float _healRadius = 4f;
    [SerializeField] private float _healCooldown = 3f;
    [SerializeField] private int _healAmount = 15;
    [SerializeField] private Color _healColor = new Color(0.3f, 1f, 0.3f);

    private float _lastHealTime;
    private Transform _target;
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Color _originalColor;
    private GameObject _healAuraGo; // 绿色治疗光环

    protected override void Awake()
    {
        base.Awake();
        // #11 HealerEnemy DOT 抗性预设：中毒抗性+40%，燃烧弱点-30%
        var res = GetComponent<EnemyDotResistance>();
        if (res == null) res = gameObject.AddComponent<EnemyDotResistance>();
        EnemyDotResistance.ApplyHealerPreset(res);
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
        _healAuraGo = EnemyEffectHelper.UpdateCircleAura(
            transform, _healAuraGo, "HealAura",
            _healColor, _healRadius, alpha: 0.15f, sortingOrder: 4);
    }

    private void FixedUpdate()
    {
        if (!Alive || _target == null) return;
        Vector2 dir = (_target.position - transform.position).normalized;
        _rb.linearVelocity = dir * MoveSpeed * 0.6f; // 治疗者移动较慢
    }

    private void Update()
    {
        if (!Alive) return;

        if (Time.time - _lastHealTime >= _healCooldown)
        {
            HealNearby();
            _lastHealTime = Time.time;
        }
    }

    protected override void OnDisable()
    {
        if (_healAuraGo != null) { Destroy(_healAuraGo); _healAuraGo = null; }
        base.OnDisable();
    }

    private void HealNearby()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _healRadius);
        int healedCount = 0;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            if (hit.gameObject == gameObject) continue; // 不治疗自己

            var dmg = hit.GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp > 0 && dmg.CurrentHp < dmg.MaxHp)
            {
                dmg.Heal(_healAmount);
                healedCount++;
            }
        }

        if (healedCount > 0)
        {
            DebugHelper.Log($"[HealerEnemy] {gameObject.name} healed {healedCount} allies");
            // 视觉反馈 — 脉冲特效
            EnemyEffectHelper.CreatePulseEffect(transform.position, _healColor, _healRadius * 0.5f, 0.5f);
            if (_sr != null)
            {
                _sr.color = _healColor;
                Invoke(nameof(RestoreColor), 0.3f);
            }
        }
    }

    private void RestoreColor()
    {
        if (_sr != null) _sr.color = _originalColor;
    }

    private new void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _healRadius);
    }
}