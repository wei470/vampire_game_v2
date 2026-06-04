using UnityEngine;

/// <summary>
/// 治疗敌人 - 治疗附近的其他敌人。
/// 
/// 行为：追踪玩家 → 周期性治疗附近敌人 → 治疗时短暂停下
/// 能力逻辑委托给 HealerAuraAbility 组件。
/// </summary>
public class HealerEnemy : EnemyBase
{
    protected override void Awake()
    {
        base.Awake();
        // #11 HealerEnemy DOT 抗性预设：中毒抗性+40%，燃烧弱点-30%
        var res = GetComponent<EnemyDotResistance>();
        if (res == null) res = gameObject.AddComponent<EnemyDotResistance>();
        EnemyDotResistance.ApplyHealerPreset(res);

        // 确保有 HealerAuraAbility 组件
        if (GetComponent<HealerAuraAbility>() == null)
            gameObject.AddComponent<HealerAuraAbility>();
    }
}