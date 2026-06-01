#pragma warning disable CS0618
using UnityEngine;

/// <summary>
/// 链式治疗敌人 - 治疗链跳跃到多个友军。
/// 对应 Python 版的 ChainHealer 敌人
/// 
/// 行为：追踪玩家 → 周期性发射治疗链 → 治疗链在多个友军间跳跃
/// </summary>
public class ChainHealerEnemy : EnemyBase
{
    [Header("链式治疗属性")]
    [SerializeField] private float _healRange = 6f;
    [SerializeField] private float _healCooldown = 4f;
    [SerializeField] private int _healAmount = 10;
    [SerializeField] private int _chainCount = 3;           // 治疗链跳跃次数
    [SerializeField] private float _chainRadius = 5f;       // 每次跳跃搜索半径
    [SerializeField] private float _healDecay = 0.8f;       // 每次跳跃治疗衰减

    private float _lastHealTime;
    private Transform _target;
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Color _originalColor;

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

    private void FixedUpdate()
    {
        if (!Alive || _target == null) return;
        Vector2 dir = (_target.position - transform.position).normalized;
        _rb.linearVelocity = dir * MoveSpeed * 0.5f;
    }

    private void Update()
    {
        if (!Alive) return;

        if (Time.time - _lastHealTime >= _healCooldown)
        {
            ChainHeal();
            _lastHealTime = Time.time;
        }
    }

    private void ChainHeal()
    {
        // 从自身开始治疗链
        float currentHeal = _healAmount;
        Vector2 currentPos = transform.position;
        System.Collections.Generic.HashSet<int> healed = new System.Collections.Generic.HashSet<int>();
        healed.Add(gameObject.GetInstanceID()); // 不重复治疗自己

        int totalHealed = 0;

        for (int i = 0; i < _chainCount; i++)
        {
            // 寻找最近的受伤友军
            Collider2D[] nearby = Physics2D.OverlapCircleAll(currentPos, _chainRadius);
            Transform bestTarget = null;
            float bestDist = float.MaxValue;

            foreach (var col in nearby)
            {
                if (!col.CompareTag("Enemy")) continue;
                int id = col.gameObject.GetInstanceID();
                if (healed.Contains(id)) continue;

                var dmg = col.GetComponent<Damageable>();
                if (dmg == null || dmg.CurrentHp <= 0 || dmg.CurrentHp >= dmg.MaxHp) continue;

                float dist = Vector2.Distance(currentPos, col.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestTarget = col.transform;
                }
            }

            if (bestTarget == null) break;

            // 治疗目标
            int targetId = bestTarget.gameObject.GetInstanceID();
            healed.Add(targetId);

            var targetDmg = bestTarget.GetComponent<Damageable>();
            if (targetDmg != null)
            {
                int heal = Mathf.RoundToInt(currentHeal);
                targetDmg.Heal(heal);
                totalHealed++;

                // 视觉反馈 - 治疗线
                CombatManager.CreateLightningLine(currentPos, bestTarget.position, 0.2f);
            }

            currentPos = bestTarget.position;
            currentHeal *= _healDecay;
        }

        // 也治疗自己
        var selfDmg = GetComponent<Damageable>();
        if (selfDmg != null && selfDmg.CurrentHp < selfDmg.MaxHp)
        {
            selfDmg.Heal(_healAmount);
        }

        if (totalHealed > 0)
        {
            DebugHelper.Log($"[ChainHealerEnemy] {gameObject.name} chain healed {totalHealed} allies");

            if (_sr != null)
            {
                _sr.color = new Color(0.5f, 1f, 0.5f);
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
        Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _healRange);
        Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, _chainRadius);
    }
}