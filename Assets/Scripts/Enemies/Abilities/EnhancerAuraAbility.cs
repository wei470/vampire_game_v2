using UnityEngine;

/// <summary>
/// 增强光环能力 — EnhancerEnemy 专用。
/// 周期性增强范围内友军的移速。
/// </summary>
public class EnhancerAuraAbility : AuraAbility
{
    [Header("增强参数")]
    [SerializeField] private float _speedBoost = 1.2f;
    [SerializeField] private float _enhanceDuration = 3f;

    protected override bool ApplyEffectToAlly(Collider2D ally)
    {
        var enemy = ally.GetComponent<EnemyBase>();
        if (enemy != null && enemy.Alive)
        {
            float originalSpeed = enemy.MoveSpeed;
            enemy.MoveSpeed *= _speedBoost;
            StartCoroutine(RestoreSpeedCoroutine(enemy, originalSpeed, _enhanceDuration));

            // 闪橙色
            var sr = ally.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                StartCoroutine(RestoreColorCoroutine(sr, sr.color, _enhanceDuration));
                sr.color = _effectColor;
            }
            return true;
        }
        return false;
    }
}