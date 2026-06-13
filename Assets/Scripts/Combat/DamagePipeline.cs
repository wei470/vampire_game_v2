using UnityEngine;

/// <summary>
/// 伤害管线 — 统一伤害计算流程，消除 39 个调用点的重复逻辑。
///
/// 使用方式：
///   float dmg = DamagePipeline.Calculate(baseDmg, multiplier, canCrit, critChance, critMult);
///   target.TakeDamage(dmg, color);
/// </summary>
public static class DamagePipeline
{
    public static float Calculate(float baseDamage, float multiplier = 1f,
        bool canCrit = false, float critChance = 0f, float critMult = 1f)
    {
        float dmg = baseDamage * multiplier;
        if (canCrit && Random.value < critChance)
            dmg *= critMult;
        return Mathf.Max(0.01f, dmg);
    }

    public static float CalculateWithArmor(float baseDamage, float multiplier, int armor,
        bool canCrit = false, float critChance = 0f, float critMult = 1f)
    {
        float dmg = Calculate(baseDamage, multiplier, canCrit, critChance, critMult);
        return Mathf.Max(0.01f, dmg - armor);
    }
}
