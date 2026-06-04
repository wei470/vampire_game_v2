using UnityEngine;

/// <summary>
/// 净化敌人 — 光环范围内友军获得 DOT 抗性 +50%。
/// 
/// 行为：追踪玩家 → 持续为范围内友军提供 DOT 抗性增强
/// 使用 AuraAbility 框架。
/// 形状：十字形，颜色：白色
/// 第 12+ 波开始出现
/// </summary>
public class PurifierEnemy : EnemyBase
{
    protected override void Awake()
    {
        base.Awake();
        // 确保有 PurifierAuraAbility 组件
        if (GetComponent<PurifierAuraAbility>() == null)
            gameObject.AddComponent<PurifierAuraAbility>();
    }
}