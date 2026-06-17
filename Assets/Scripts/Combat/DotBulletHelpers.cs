using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DOT 子弹通用工具类 — 确保 StatusEffectManager 存在，应用腐蚀/侵蚀。
/// </summary>
public static class DotBulletHelper
{
    public static DotColorBlender EnsureColorBlender(GameObject enemy)
    {
        var blender = enemy.GetComponent<DotColorBlender>();
        if (blender == null) blender = enemy.AddComponent<DotColorBlender>();
        return blender;
    }

    public static void EnsureStatusEffectManager(GameObject enemy)
    {
        var sem = enemy.GetComponent<StatusEffectManager>();
        if (sem == null) sem = enemy.AddComponent<StatusEffectManager>();

        var dotPassive = GameReferences.DotCharacterPassive;
        if (dotPassive != null)
        {
            sem.DotDurationMultiplier = dotPassive.GetDotDurationMultiplier();
            sem.WindErosionKnockback = dotPassive.GetKnockbackBonus();

            var mage = dotPassive as MagePassive;
            if (mage != null)
                sem.ParalysisActive = mage.Paralysis;
        }
    }
}

/// <summary>
/// 穿透处理器 — 挂在子弹上，命中敌人后可继续穿透
/// </summary>
public class PenetrateHandler : MonoBehaviour
{
    private int _remaining;
    private HashSet<Collider2D> _hitEnemies = new HashSet<Collider2D>();

    public void Setup(int penetrateCount)
    {
        _remaining = penetrateCount;
        _hitEnemies.Clear();
    }

    public bool TryPenetrate(Collider2D hitEnemy)
    {
        if (_hitEnemies.Contains(hitEnemy)) return false;
        _hitEnemies.Add(hitEnemy);
        if (_remaining <= 0) return false;
        _remaining--;
        return true;
    }
}

/// <summary>
/// 暴击参数结构体 — 替代散落在 10+ 文件中的 _canCrit/_critChance/_critMult 三字段。
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

    public float Apply(float damage)
    {
        if (canCrit && Random.value < critChance)
            return damage * critMult;
        return damage;
    }

    public bool Roll()
    {
        return canCrit && Random.value < critChance;
    }

    public static CritParams None => new CritParams(false, 0f, 0f);
}
