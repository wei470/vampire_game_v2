using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DOT 组合效果系统 — 已清空所有组合逻辑
/// 保留类结构以兼容引用，所有方法返回中性值
/// </summary>
public class DotComboSystem
{
    public const float COMBO_SHATTER_BLEED_MULT = 0f;

    public bool ShatterActive => false;
    public float SuperconductMult => 1f;

    // ── 进化系统：全局组合伤害倍率 ──
    private static float _evolutionComboDamageMultiplier = 0f;

    public static void SetEvolutionComboMultiplier(float bonus) { _evolutionComboDamageMultiplier += bonus; }
    public static float GetEvolutionComboMult() => 1f + _evolutionComboDamageMultiplier;
    public static void ResetEvolutionComboMultiplier() { _evolutionComboDamageMultiplier = 0f; }

    /// <summary>
    /// 检测并应用 DOT 组合效果 — 已禁用
    /// </summary>
    public void CheckComboEffects(List<StatusEffect> activeEffects, GameObject go, SpriteRenderer sr, Color originalColor, Vector3 enemyPos)
    {
        // 组合系统已清空，不再触发任何效果
    }

    public float GetBleedDamageMult() => 1f;
    public float GetPoisonDamageMult() => 1f;
}