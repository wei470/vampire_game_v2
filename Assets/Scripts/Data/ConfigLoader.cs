using UnityEngine;

/// <summary>
/// 配置加载器 — 运行时加载和管理所有 ScriptableObject 配置。
/// 单例模式，自动从 Resources 或 Inspector 引用加载。
/// </summary>
public class ConfigLoader : Singleton<ConfigLoader>
{
    [Header("配置资源引用（Inspector 拖入，优先级高于 Resources）")]
    [SerializeField] private GameConfig _gameConfig;
    [SerializeField] private EnemyWaveConfig _enemyWaveConfig;

    /// <summary>
    /// 全局游戏配置
    /// </summary>
    public static GameConfig Game
    {
        get
        {
            if (Instance != null && Instance._gameConfig != null)
                return Instance._gameConfig;
            // 自动从 Resources 加载
            var config = Resources.Load<GameConfig>("Configs/GameConfig");
            if (config == null)
            {
                // Asset 不在 Resources 目录时静默创建默认配置
                config = ScriptableObject.CreateInstance<GameConfig>();
            }
            if (Instance != null) Instance._gameConfig = config;
            return config;
        }
    }

    /// <summary>
    /// 敌人波次配置
    /// </summary>
    public static EnemyWaveConfig Wave
    {
        get
        {
            if (Instance != null && Instance._enemyWaveConfig != null)
                return Instance._enemyWaveConfig;
            var config = Resources.Load<EnemyWaveConfig>("Configs/EnemyWaveConfig");
            if (config == null)
            {
                DebugHelper.LogWarning("[ConfigLoader] EnemyWaveConfig not found, creating default");
                config = ScriptableObject.CreateInstance<EnemyWaveConfig>();
            }
            if (Instance != null) Instance._enemyWaveConfig = config;
            return config;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        DebugHelper.Log($"[ConfigLoader] Loaded: GameConfig={(_gameConfig != null ? "OK" : "Default")}, " +
                  $"EnemyWaveConfig={(_enemyWaveConfig != null ? "OK" : "Default")}");
    }

    /// <summary>
    /// 输出所有配置到控制台（调试用）
    /// </summary>
    public static void DumpConfig()
    {
        var gc = Game;
        var wc = Wave;
        DebugHelper.Log("═════════ Config Dump ═════════");
        DebugHelper.Log($"[GameConfig] Player HP={gc.basePlayerHP}, SPD={gc.basePlayerSpeed}, ARM={gc.basePlayerArmor}");
        DebugHelper.Log($"[GameConfig] Exp Base={gc.baseLevelUpExp}+{gc.expPerLevel}/lvl");
        DebugHelper.Log($"[GameConfig] Difficulty: DMG×{gc.damageScalingPerWave}/wave, HP×{gc.hpScalingPerWave}/wave");
        DebugHelper.Log($"[GameConfig] Boss: interval={gc.bossWaveInterval}, baseHP={gc.bossBaseHP}");
        DebugHelper.Log($"[WaveConfig] Base enemies={wc.baseEnemyCount}, +{wc.enemiesPerWave}/wave");
        DebugHelper.Log($"[WaveConfig] Spawn radius={wc.spawnRadius}, interval={wc.spawnInterval}s");
        DebugHelper.Log($"[WaveConfig] Enemy types: {wc.enemyTypes.Length}");
        DebugHelper.Log("═══════════════════════════════");
    }
}