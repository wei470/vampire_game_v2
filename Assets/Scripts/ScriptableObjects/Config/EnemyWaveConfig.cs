using UnityEngine;

/// <summary>
/// 敌人波次配置 — 定义每波的敌人数量和类型分布。
/// 通过 ScriptableObject 数据驱动。
/// </summary>
[CreateAssetMenu(fileName = "EnemyWaveConfig", menuName = "Vampire/Enemy Wave Config")]
public class EnemyWaveConfig : ScriptableObject
{
    [System.Serializable]
    public class EnemySpawnEntry
    {
        public string enemyType;
        public int weight;
        public int minWave;
    }

    [Header("基础波次参数")]
    public int baseEnemyCount = 3;
    public int enemiesPerWave = 2;
    public float spawnInterval = 0.5f;
    public float spawnRadius = 15f;

    [Header("敌人类型分布")]
    public EnemySpawnEntry[] enemyTypes = new EnemySpawnEntry[]
    {
        new EnemySpawnEntry { enemyType = "Basic",     weight = 40, minWave = 1 },
        new EnemySpawnEntry { enemyType = "Ranged",    weight = 15, minWave = 3 },
        new EnemySpawnEntry { enemyType = "Fast",      weight = 15, minWave = 3 },
        new EnemySpawnEntry { enemyType = "Tank",      weight = 8,  minWave = 5 },
        new EnemySpawnEntry { enemyType = "Thrower",   weight = 8,  minWave = 5 },
        new EnemySpawnEntry { enemyType = "Healer",    weight = 5,  minWave = 8 },
        new EnemySpawnEntry { enemyType = "Enhancer",  weight = 5,  minWave = 8 },
        new EnemySpawnEntry { enemyType = "Splitter",  weight = 5,  minWave = 8 },
        new EnemySpawnEntry { enemyType = "Summoner",  weight = 5,  minWave = 8 },
        new EnemySpawnEntry { enemyType = "Charger",   weight = 5,  minWave = 8 },
        new EnemySpawnEntry { enemyType = "Shielder",  weight = 5,  minWave = 8 },
        new EnemySpawnEntry { enemyType = "Stealth",   weight = 5,  minWave = 8 },
        new EnemySpawnEntry { enemyType = "Burst",     weight = 5,  minWave = 8 },
        new EnemySpawnEntry { enemyType = "ChainHealer", weight = 3, minWave = 8 },
    };

    [Header("Boss 配置")]
    public int bossWaveInterval = 5;
    public int bossBaseHP = 300;
    public int bossHPPerWave = 100;
    public int bossMinionsPerWave = 3;

    /// <summary>
    /// 获取指定波次的敌人总数量
    /// </summary>
    public int GetEnemyCountForWave(int wave)
    {
        return baseEnemyCount + (wave - 1) * enemiesPerWave;
    }

    /// <summary>
    /// 根据波次选择一个敌人类型（加权随机）
    /// </summary>
    public string ChooseEnemyType(int wave)
    {
        int totalWeight = 0;
        foreach (var entry in enemyTypes)
        {
            if (wave >= entry.minWave)
                totalWeight += entry.weight;
        }

        if (totalWeight <= 0) return "Basic";

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;
        foreach (var entry in enemyTypes)
        {
            if (wave < entry.minWave) continue;
            cumulative += entry.weight;
            if (roll < cumulative) return entry.enemyType;
        }

        return "Basic";
    }

    /// <summary>
    /// 是否为 Boss 波
    /// </summary>
    public bool IsBossWave(int wave)
    {
        return wave > 0 && wave % bossWaveInterval == 0;
    }

    /// <summary>
    /// 获取指定波次的 Boss HP
    /// </summary>
    public int GetBossHP(int wave)
    {
        return bossBaseHP + wave * bossHPPerWave;
    }
}