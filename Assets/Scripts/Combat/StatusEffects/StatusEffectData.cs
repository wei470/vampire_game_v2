using UnityEngine;

/// <summary>
/// DOT 状态效果数据类型定义。
/// 从 StatusEffectSystem.cs 中提取，便于跨文件引用。
/// </summary>

/// <summary>
/// 状态效果类型枚举（14 种 Mage 专属升级对应）
/// </summary>
public enum StatusEffectType
{
    Bleed, Poison, Burn, Frostbite, Corrosion, Curse, Agony, Wither,
    Immolate, Radiate, Contaminate, Erosion, WindErosion, Rend, Static,
    Dark, Light
}

/// <summary>
/// 单个状态效果实例
/// </summary>
[System.Serializable]
public class StatusEffect
{
    public StatusEffectType type;
    public float damagePerSecond;
    public float remainingDuration;
    public float totalDuration;
    public int stackCount;
    public bool canCrit;
    public float critChance;
    public float critMultiplier;

    public void Refresh(float dps, float duration, bool stackDps = false)
    {
        if (stackDps) { stackCount++; damagePerSecond += dps; }
        else { damagePerSecond = Mathf.Max(damagePerSecond, dps); }
        remainingDuration = Mathf.Max(remainingDuration, duration);
        totalDuration = remainingDuration;
    }
}

/// <summary>
/// 引爆结果数据
/// </summary>
public struct DetonateResult
{
    public float totalDamage;
    public int poisonStacks, burnStacks, bleedStacks, frostStacks;
    public bool hadBurn, hadFrost, hadPoison;
}