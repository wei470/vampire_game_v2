using UnityEngine;

/// <summary>
/// 护盾光环能力 — ShielderEnemy 专用。
/// 持续为范围内友军提供伤害减免，自身有呼吸式脉冲动画。
/// </summary>
public class ShielderAuraAbility : AuraAbility
{
    [Header("护盾参数")]
    [SerializeField] private float _damageReduction = 0.5f;

    private float _shieldPulse;

    /// <summary>
    /// 护盾效果是持续性的，不使用冷却机制。
    /// </summary>
    protected override void OnCooldownReady() { }

    /// <summary>
    /// 护盾通过 OnUpdate 持续施加，此方法不使用。
    /// </summary>
    protected override bool ApplyEffectToAlly(Collider2D ally) => false;

    /// <summary>
    /// 每帧更新护盾效果和视觉。
    /// </summary>
    protected override void OnUpdate()
    {
        // 自身护盾呼吸动画
        _shieldPulse += Time.deltaTime * 3f;
        if (CachedSr != null)
        {
            float alpha = Mathf.Sin(_shieldPulse) * 0.15f + 0.85f;
            CachedSr.color = new Color(_effectColor.r, _effectColor.g, _effectColor.b, alpha);
        }

        // 持续为范围内友军施加护盾
        int count = FindNearbyAllies(_auraRadius);
        for (int i = 0; i < count; i++)
        { var hit = OverlapBuffer[i];
            if (!hit.CompareTag("Enemy")) continue;
            if (hit.gameObject == gameObject) continue;

            var dmg = hit.GetComponent<Damageable>();
            if (dmg != null)
            {
                int extraArmor = Mathf.RoundToInt(10 * _damageReduction);
                dmg.SetArmor(extraArmor);
            }
        }
    }
}