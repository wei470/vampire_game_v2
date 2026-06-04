using UnityEngine;

/// <summary>
/// 护盾敌人 - 为自己和附近敌人提供伤害减免。
/// 
/// 行为：追踪玩家 → 范围内敌人获得50%伤害减免 → 护盾有呼吸动画
/// 能力逻辑委托给 ShielderAuraAbility 组件。
/// </summary>
public class ShielderEnemy : EnemyBase
{
    protected override void Awake()
    {
        base.Awake();

        // 确保有 ShielderAuraAbility 组件
        if (GetComponent<ShielderAuraAbility>() == null)
            gameObject.AddComponent<ShielderAuraAbility>();
    }
}