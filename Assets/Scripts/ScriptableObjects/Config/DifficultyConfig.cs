using UnityEngine;

/// <summary>
/// 难度配置 — 定义每一阶难度的敌人倍率和特殊规则。
///
/// 10个难度等级，每个难度有独立的参数。
/// 通过 DifficultyManager 加载和应用。
/// </summary>
[CreateAssetMenu(menuName = "Configs/Difficulty Config")]
public class DifficultyConfig : ScriptableObject
{
    [Header("难度信息")]
    public int difficultyLevel = 1;
    public string difficultyName = "入门";
    public string difficultyDescription = "标准体验";
    public Color difficultyColor = Color.white;

    [Header("敌人基础倍率")]
    public float enemyHpMult = 1f;
    public float enemyDmgMult = 1f;
    public float enemySpeedMult = 1f;
    public float eliteFrequencyMult = 1f;
    public float spawnRateMult = 1f;

    [Header("奖励倍率")]
    public float xpMult = 1f;
    public float coinMult = 1f;
    public float dropMult = 1f;
    public float dropQualityBonus = 0f;

    [Header("特殊规则")]
    public bool enableAffixes = false;
    public bool enableRifts = false;
    public float healReduction = 0f;
    public float scalingExponent = 1f;
    public bool permanentDeath = false;

    [Header("精英词条池（难度2+）")]
    public bool expandElitePool = false;
    public string[] extraEliteModifiers = new string[0];

    /// <summary>
    /// 获取当前波次的有效HP倍率（无尽模式按指数增长）
    /// </summary>
    public float GetEffectiveHpMult(int wave)
    {
        if (scalingExponent > 1f && wave > 1)
            return enemyHpMult * Mathf.Pow(1f + wave * 0.01f, scalingExponent);
        return enemyHpMult;
    }

    /// <summary>
    /// 获取当前波次的有效伤害倍率
    /// </summary>
    public float GetEffectiveDmgMult(int wave)
    {
        if (scalingExponent > 1f && wave > 1)
            return enemyDmgMult * Mathf.Pow(1f + wave * 0.01f, scalingExponent);
        return enemyDmgMult;
    }
}
