using UnityEngine;

/// <summary>
/// 暴击参数结构体 — 替代散落在 10+ 文件中的 _canCrit/_critChance/_critMult 三字段。
///
/// 使用方式：
///   CritParams crit = new CritParams(canCrit, critChance, critMult);
///   float finalDmg = crit.Apply(baseDmg);
/// </summary>
[System.Serializable]
public struct CritParams
{
    public bool canCrit;
    public float critChance;
    public float critMult;

    public CritParams(bool canCrit, float critChance, float critMult)
    {
        this.canCrit = canCrit;
        this.critChance = critChance;
        this.critMult = critMult;
    }

    /// <summary>
    /// 对伤害值应用暴击判定
    /// </summary>
    public float Apply(float damage)
    {
        if (canCrit && Random.value < critChance)
            return damage * critMult;
        return damage;
    }

    /// <summary>
    /// 判断是否暴击（不修改伤害值）
    /// </summary>
    public bool Roll()
    {
        return canCrit && Random.value < critChance;
    }

    public static CritParams None => new CritParams(false, 0f, 0f);
}
