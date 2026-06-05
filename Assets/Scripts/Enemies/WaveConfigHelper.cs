using UnityEngine;

/// <summary>
/// 波次配置助手 — 负责波次配置加载和难度倍率计算
/// 从 SpawnManager 拆分而来，职责单一：配置管理和数学计算
/// </summary>
public class WaveConfigHelper
{
    private EnemyWaveConfig _waveConfig;

    /// <summary>
    /// 当前波次配置
    /// </summary>
    public EnemyWaveConfig WaveConfig => _waveConfig;

    /// <summary>
    /// 加载波次配置
    /// </summary>
    public void LoadWaveConfig()
    {
        if (ConfigLoader.Wave != null)
        {
            _waveConfig = ConfigLoader.Wave;
            DebugHelper.Log("[WaveConfigHelper] Wave config loaded from EnemyWaveConfig");
        }
    }

    /// <summary>
    /// 获取波次敌人数量
    /// </summary>
    public int GetEnemyCountForWave(int wave, int baseEnemyCount, int enemiesPerWave)
    {
        if (_waveConfig != null)
            return _waveConfig.GetEnemyCountForWave(wave);
        return baseEnemyCount + (wave - 1) * enemiesPerWave;
    }

    /// <summary>
    /// 获取伤害倍率
    /// </summary>
    public float GetDamageMultiplier(int wave)
    {
        if (_waveConfig != null)
            return _waveConfig.GetDamageMultiplier(wave);
        return CalculateSCurveFallback(wave, 0.08f);
    }

    /// <summary>
    /// 获取 HP 倍率
    /// </summary>
    public float GetHpMultiplier(int wave)
    {
        if (_waveConfig != null)
            return _waveConfig.GetHpMultiplier(wave);
        return CalculateSCurveFallback(wave, 0.12f);
    }

    /// <summary>
    /// 获取 Boss HP
    /// </summary>
    public int GetBossHP(int wave, float hpMultiplier)
    {
        if (_waveConfig != null)
            return _waveConfig.GetBossHP(wave);
        return Mathf.RoundToInt((300 + wave * 60) * hpMultiplier);
    }

    /// <summary>
    /// 获取 Boss 随从数量
    /// </summary>
    public int GetBossMinions(int wave, int enemyCount)
    {
        if (_waveConfig != null)
            return Mathf.Min(enemyCount, _waveConfig.bossMinionsPerWave);
        return Mathf.Min(enemyCount, 3);
    }

    /// <summary>
    /// 是否是 Boss 波
    /// </summary>
    public bool IsBossWave(int wave)
    {
        if (_waveConfig != null)
            return _waveConfig.IsBossWave(wave);
        return wave % 5 == 0;
    }

    /// <summary>
    /// 获取特殊波次类型
    /// </summary>
    public EnemyWaveConfig.SpecialWaveType GetSpecialWaveType(int wave)
    {
        if (_waveConfig != null)
            return _waveConfig.GetSpecialWaveType(wave);
        return EnemyWaveConfig.SpecialWaveType.None;
    }

    /// <summary>
    /// 获取特殊波次敌人数量
    /// </summary>
    public int GetSpecialWaveEnemyCount(int baseCount, EnemyWaveConfig.SpecialWaveType type)
    {
        if (_waveConfig != null)
            return _waveConfig.GetSpecialWaveEnemyCount(baseCount, type);
        return baseCount;
    }

    /// <summary>
    /// 获取基础配置值
    /// </summary>
    public void GetBaseConfig(ref int baseEnemyCount, ref int enemiesPerWave, ref float spawnInterval, ref float spawnRadius)
    {
        if (_waveConfig != null)
        {
            baseEnemyCount = _waveConfig.baseEnemyCount;
            enemiesPerWave = _waveConfig.enemiesPerWave;
            spawnInterval = _waveConfig.spawnInterval;
            spawnRadius = _waveConfig.spawnRadius;
        }
    }

    /// <summary>
    /// 回退 S 曲线（无 EnemyWaveConfig 时使用，保持向后兼容）
    /// </summary>
    private static float CalculateSCurveFallback(int wave, float baseRate)
    {
        if (wave <= 1) return 1f;
        float w = wave - 1;
        float midpoint = 15f;
        float steepness = 0.2f;
        float sCurve = 1f / (1f + Mathf.Exp(-steepness * (w - midpoint)));
        float sCurveMin = 1f / (1f + Mathf.Exp(steepness * midpoint));
        float sCurveMax = 1f / (1f + Mathf.Exp(-steepness * (50f - midpoint)));
        float normalized = Mathf.Clamp01((sCurve - sCurveMin) / (sCurveMax - sCurveMin));
        float maxMult = 1f + 50f * baseRate * 1.5f;
        return 1f + (maxMult - 1f) * normalized;
    }
}