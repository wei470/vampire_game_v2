using UnityEngine;

/// <summary>
/// 敌人属性缩放助手 — 根据波次、挑战、永久升级缩放敌人 HP/速度/护甲。
/// 从 SpawnManager 中提取，减少主文件行数。
/// </summary>
public static class EnemyScalingHelper
{
    /// <summary>
    /// 应用普通波次敌人的属性缩放
    /// </summary>
    public static void ApplyScaling(GameObject enemy, Transform playerTransform,
        float hpMultiplier, float weakenMultiplier, float challengeHpMult,
        float challengeSpeedMult, int eliteArmor, int currentWave = 0)
    {
        var enemyBase = enemy.GetComponent<EnemyBase>();
        if (enemyBase == null) return;

        var dmg = enemy.GetComponent<Damageable>();
        if (dmg != null)
        {
            int scaledMaxHp = Mathf.RoundToInt(dmg.MaxHp * hpMultiplier * weakenMultiplier * challengeHpMult);
            dmg.SetMaxHp(scaledMaxHp);
            // 每波+1护甲
            int waveArmor = currentWave;
            dmg.SetArmor(dmg.Armor + waveArmor + eliteArmor);
        }

        enemyBase.MoveSpeed *= weakenMultiplier * 0.5f * challengeSpeedMult;
        enemyBase.SetTarget(playerTransform);
    }

    /// <summary>
    /// 应用特殊波次敌人的属性缩放（含速度加成）
    /// </summary>
    public static void ApplySpecialScaling(GameObject enemy, Transform playerTransform,
        float hpMultiplier, float weakenMultiplier, bool isSpeedSurge)
    {
        var enemyBase = enemy.GetComponent<EnemyBase>();
        if (enemyBase == null) return;

        var dmg = enemy.GetComponent<Damageable>();
        if (dmg != null)
        {
            int scaledMaxHp = Mathf.RoundToInt(dmg.MaxHp * hpMultiplier * weakenMultiplier);
            dmg.SetMaxHp(scaledMaxHp);
        }

        float speedMult = weakenMultiplier * 0.5f;
        if (isSpeedSurge) speedMult *= 2f;
        enemyBase.MoveSpeed *= speedMult;
        enemyBase.SetTarget(playerTransform);
    }
}