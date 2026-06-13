using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 引爆子效果 — 余烬火焰区域 + 霜爆碎裂（静态工具类）
/// 从 DetonateSystem 提取，减少主类职责
/// </summary>
public static class DetonateSubEffects
{
    /// <summary>
    /// 余烬：在范围内敌人脚下生成火焰区域
    /// </summary>
    public static void SpawnEmberFireZones(Vector2 center, float radius, int maxZones, float duration, float zoneRadius, int burnStacks)
    {
        int emberDmg = Mathf.Max(1, burnStacks);
        int zonesCreated = 0;

        var nearbyEnemies = SpatialGrid.QueryRadius(center, radius);
        for (int i = 0; i < nearbyEnemies.Count && zonesCreated < maxZones; i++)
        {
            var enemy = nearbyEnemies[i];
            if (enemy == null || !enemy.activeInHierarchy) continue;
            FireZone.CreateDefault(enemy.transform.position, emberDmg, duration, zoneRadius, 0.5f);
            zonesCreated++;
        }
        if (zonesCreated > 0)
            DebugHelper.Log($"[DetonateSubEffects] EMBER! Created {zonesCreated} fire zones");
    }

    /// <summary>
    /// 霜爆碎裂：对范围内冰冻敌人造成碎裂伤害
    /// </summary>
    public static void TriggerFrostShatter(Vector2 center, float radius, int frostStacks, int dmgPerStack, float critChance, float critMult)
    {
        float shatterDmg = Mathf.Max(0.01f, frostStacks * dmgPerStack);
        int targetsHit = 0;

        var nearbyEnemies = SpatialGrid.QueryRadius(center, radius);
        for (int i = 0; i < nearbyEnemies.Count; i++)
        {
            var enemy = nearbyEnemies[i];
            if (enemy == null || !enemy.activeInHierarchy) continue;
            if (!enemy.TryGetComponent<Damageable>(out var d) || d.CurrentHp <= 0) continue;
            float finalDmg = shatterDmg;
            if (Random.value < critChance) finalDmg *= critMult;
            d.TakeDamage(finalDmg);
            targetsHit++;
            if (!enemy.TryGetComponent<StatusEffectManager>(out var sem))
                sem = enemy.gameObject.AddComponent<StatusEffectManager>();
            sem.ApplyEffect(StatusEffectType.Frostbite, 1f, 2f);
        }
        if (targetsHit > 0)
        {
            CombatManager.CreateExplosionEffect(center, radius, new Color(0.4f, 0.7f, 1f), 0.5f);
            DebugHelper.Log($"[DetonateSubEffects] FROST SHATTER! {targetsHit} targets");
        }
    }
}
