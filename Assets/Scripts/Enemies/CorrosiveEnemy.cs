using UnityEngine;

/// <summary>
/// 腐蚀敌人 — 反 DOT 型敌人。接触玩家后施加 DOT 免疫护盾，死亡时释放毒雾清除范围内敌人的 DOT。
/// 
/// 行为：追踪玩家 → 碰撞施加 DOT 免疫护盾 → 死亡释放毒雾
/// 形状：八角形，颜色：暗绿色
/// 第 10+ 波开始出现
/// </summary>
public class CorrosiveEnemy : EnemyBase
{
    [Header("腐蚀属性")]
    #pragma warning disable CS0414
    [SerializeField] [HideInInspector] private float _dotImmuneDuration = 3f;
    #pragma warning restore CS0414
    [SerializeField] private float _deathFogRadius = 2f;
    [SerializeField] private float _deathFogDuration = 2f;
    [SerializeField] private Color _corrosiveColor = new Color(0.2f, 0.4f, 0.1f);

    private Damageable _damageable;
    private GameObject _fogAuraGo;

    protected override void Awake()
    {
        base.Awake();
        _damageable = GetComponent<Damageable>();

        // DOT 抗性预设：对所有 DOT 有 50% 抗性
        var res = GetComponent<EnemyDotResistance>();
        if (res == null) res = gameObject.AddComponent<EnemyDotResistance>();
        // 自带 DOT 抗性
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        // 创建腐蚀光环视觉
        _fogAuraGo = EnemyEffectHelper.UpdateCircleAura(
            transform, _fogAuraGo, "CorrosiveAura",
            _corrosiveColor, 1.5f, alpha: 0.08f, sortingOrder: 3);

        // 订阅死亡事件
        OnDeath += OnCorrosiveDeath;
    }

    protected override void OnDisable()
    {
        if (_fogAuraGo != null) { Destroy(_fogAuraGo); _fogAuraGo = null; }
        OnDeath -= OnCorrosiveDeath;
        base.OnDisable();
    }

    /// <summary>
    /// 碰撞玩家时施加 DOT 免疫护盾。
    /// </summary>
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!Alive) return;
        if (collision.gameObject.CompareTag("Player"))
        {
            var sem = collision.gameObject.GetComponent<StatusEffectManager>();
            if (sem != null)
            {
                // 清除所有 DOT 效果
                sem.ClearAllDotEffects();
                DebugHelper.Log($"[CorrosiveEnemy] {gameObject.name} cleared player DOTs");
            }
        }
    }

    /// <summary>
    /// 死亡时释放毒雾，清除范围内所有敌人的 DOT。
    /// </summary>
    private void OnCorrosiveDeath(Vector3 deathPosition)
    {
        // 创建毒雾视觉效果
        EnemyEffectHelper.CreatePulseEffect(deathPosition, _corrosiveColor, _deathFogRadius, _deathFogDuration);

        // 清除范围内所有敌人的 DOT
        Collider2D[] hits = Physics2D.OverlapCircleAll(deathPosition, _deathFogRadius);
        int clearedCount = 0;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            var sem = hit.GetComponent<StatusEffectManager>();
            if (sem != null)
            {
                sem.ClearAllDotEffects();
                clearedCount++;
            }
        }

        if (clearedCount > 0)
        {
            DebugHelper.Log($"[CorrosiveEnemy] Death fog cleared DOTs from {clearedCount} enemies");
        }
    }

    private new void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(_corrosiveColor.r, _corrosiveColor.g, _corrosiveColor.b, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _deathFogRadius);
    }
}