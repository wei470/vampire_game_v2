using UnityEngine;

/// <summary>
/// 治疗光环能力 — HealerEnemy 专用。
/// 周期性治疗范围内友军。
/// </summary>
public class HealerAuraAbility : AuraAbility
{
    [Header("治疗参数")]
    [SerializeField] private int _healAmount = 15;

    protected override bool ApplyEffectToAlly(Collider2D ally)
    {
        var dmg = ally.GetComponent<Damageable>();
        if (dmg != null && dmg.CurrentHp > 0 && dmg.CurrentHp < dmg.MaxHp)
        {
            dmg.Heal(_healAmount);
            return true;
        }
        return false;
    }
}