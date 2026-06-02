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

    [Header("难度曲线（S 曲线参数）")]
    [Tooltip("S 曲线中点波次 — 难度增长最快的波次")]
    public float sCurveMidpoint = 15f;
    [Tooltip("S 曲线陡峭程度 — 值越大过渡越陡")]
    public float sCurveSteepness = 0.2f;
    [Tooltip("HP 倍率基础增长率")]
    public float hpBaseRate = 0.12f;
    [Tooltip("伤害倍率基础增长率")]
    public float dmgBaseRate = 0.08f;
    [Tooltip("最大难度倍率乘数（相对于基础）")]
    public float maxMultiplierScale = 1.5f;

    [Header("特殊波次事件")]
    [Tooltip("启用特殊波次事件（如全是 Tank、速度提升等）")]
    public bool enableSpecialWaves = true;
    [Tooltip("特殊波次开始的最低波次")]
    public int specialWaveMinStart = 10;
    [Tooltip("特殊波次出现概率（每波检测）")]
    [Range(0f, 1f)]
    public float specialWaveChance = 0.2f;

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

    /// <summary>
    /// S 曲线成长公式 — 前期缓慢增长，中期加速，后期给玩家喘息空间
    /// 参数全部由 ScriptableObject 配置
    /// </summary>
    public float CalculateSCurveMultiplier(int wave, float baseRate)
    {
        if (wave <= 1) return 1f;

        float w = wave - 1;
        // 标准 S 曲线：1 / (1 + e^(-k*(x-midpoint)))
        float sCurve = 1f / (1f + Mathf.Exp(-sCurveSteepness * (w - sCurveMidpoint)));
        float sCurveMin = 1f / (1f + Mathf.Exp(sCurveSteepness * sCurveMidpoint));
        float sCurveMax = 1f / (1f + Mathf.Exp(-sCurveSteepness * (50f - sCurveMidpoint)));

        // 归一化到 [0, 1]
        float normalized = Mathf.Clamp01((sCurve - sCurveMin) / (sCurveMax - sCurveMin));

        // 最大倍率 = 1 + 50波 * baseRate * scale
        float maxMult = 1f + 50f * baseRate * maxMultiplierScale;
        return 1f + (maxMult - 1f) * normalized;
    }

    /// <summary>
    /// 获取 HP 倍率
    /// </summary>
    public float GetHpMultiplier(int wave) => CalculateSCurveMultiplier(wave, hpBaseRate);

    /// <summary>
    /// 获取伤害倍率
    /// </summary>
    public float GetDamageMultiplier(int wave) => CalculateSCurveMultiplier(wave, dmgBaseRate);

    /// <summary>
    /// 特殊波次类型枚举
    /// </summary>
    public enum SpecialWaveType
    {
        None,
        TankRush,       // 全是 Tank
        SpeedSurge,     // 速度提升波
        SwarmWave,      // 大量弱敌
        EliteWave,      // 少量精英敌
        HealerArmy,     // 治疗军团
        BossRush,       // 连续小 Boss
    }

    /// <summary>
    /// 判断当前波次是否为特殊波次，返回特殊波次类型
    /// </summary>
    public SpecialWaveType GetSpecialWaveType(int wave)
    {
        if (!enableSpecialWaves || wave < specialWaveMinStart)
            return SpecialWaveType.None;

        // Boss 波不触发特殊波次
        if (IsBossWave(wave))
            return SpecialWaveType.None;

        // 概率检测
        if (Random.value > specialWaveChance)
            return SpecialWaveType.None;

        // 根据波次选择特殊波次类型
        float roll = Random.value;
        if (wave >= 30 && roll < 0.15f) return SpecialWaveType.BossRush;
        if (roll < 0.20f) return SpecialWaveType.TankRush;
        if (roll < 0.40f) return SpecialWaveType.SpeedSurge;
        if (roll < 0.55f) return SpecialWaveType.SwarmWave;
        if (roll < 0.70f) return SpecialWaveType.EliteWave;
        if (roll < 0.85f) return SpecialWaveType.HealerArmy;
        return SpecialWaveType.SpeedSurge; // 默认速度提升
    }

    /// <summary>
    /// 获取特殊波次的敌人数量修正
    /// </summary>
    public int GetSpecialWaveEnemyCount(int baseCount, SpecialWaveType type)
    {
        switch (type)
        {
            case SpecialWaveType.SwarmWave: return baseCount * 3;   // 3倍数量弱敌
            case SpecialWaveType.EliteWave: return Mathf.Max(3, baseCount / 3); // 1/3数量精英
            case SpecialWaveType.TankRush: return Mathf.Max(5, baseCount / 2);
            default: return baseCount;
        }
    }
}