using UnityEngine;

/// <summary>
/// 增强敌人 - 增强附近的其他敌人（移速提升）。
/// 
/// 行为：追踪玩家 → 周期性增强附近敌人 → 增强时发光
/// 能力逻辑委托给 EnhancerAuraAbility 组件。
/// </summary>
public class EnhancerEnemy : EnemyBase
{
    protected override void Awake()
    {
        base.Awake();

        // 确保有 EnhancerAuraAbility 组件
        if (GetComponent<EnhancerAuraAbility>() == null)
            gameObject.AddComponent<EnhancerAuraAbility>();
    }
}