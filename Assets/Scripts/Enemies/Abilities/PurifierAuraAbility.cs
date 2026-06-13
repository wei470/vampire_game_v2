using UnityEngine;

/// <summary>
/// 净化光环能力 — PurifierEnemy 专用。
/// 持续为范围内友军提供 DOT 抗性 +50%。
/// </summary>
public class PurifierAuraAbility : AuraAbility
{
    [Header("净化参数")]
    [SerializeField] private float _dotResistanceBonus = 0.5f;

    private float _pulseTimer;

    /// <summary>
    /// 净化效果是持续性的，不使用冷却机制。
    /// </summary>
    protected override void OnCooldownReady() { }

    /// <summary>
    /// 净化通过 OnUpdate 持续施加，此方法不使用。
    /// </summary>
    protected override bool ApplyEffectToAlly(Collider2D ally) => false;

    /// <summary>
    /// 每帧更新净化效果。
    /// </summary>
    protected override void OnUpdate()
    {
        // 呼吸动画
        _pulseTimer += Time.deltaTime * 2f;
        if (CachedSr != null)
        {
            float pulse = Mathf.Sin(_pulseTimer) * 0.1f + 0.9f;
            CachedSr.color = new Color(1f, 1f, 1f, pulse);
        }

        // 持续为范围内友军施加 DOT 抗性
        int count = FindNearbyAllies(_auraRadius);
        for (int i = 0; i < count; i++)
        { var hit = OverlapBuffer[i];
            if (!hit.CompareTag("Enemy")) continue;
            if (hit.gameObject == gameObject) continue;

            var resistance = hit.GetComponent<EnemyDotResistance>();
            if (resistance != null)
            {
                // 临时提升所有 DOT 抗性
                resistance.BleedResistance = Mathf.Min(1f, resistance.BleedResistance + _dotResistanceBonus * Time.deltaTime);
                resistance.PoisonResistance = Mathf.Min(1f, resistance.PoisonResistance + _dotResistanceBonus * Time.deltaTime);
                resistance.BurnResistance = Mathf.Min(1f, resistance.BurnResistance + _dotResistanceBonus * Time.deltaTime);
                resistance.FrostResistance = Mathf.Min(1f, resistance.FrostResistance + _dotResistanceBonus * Time.deltaTime);
            }
        }
    }
}