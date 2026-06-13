using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 难度管理器 — 管理难度选择、参数应用、解锁检查。
///
/// 10个难度等级，每阶第20波解锁下一阶。
/// 敌人属性通过 DifficultyConfig 倍率控制。
/// </summary>
public static class DifficultyManager
{
    private static DifficultyConfig[] _configs;
    private static int _currentDifficulty = 1;
    private static bool _initialized = false;

    /// <summary>当前难度等级（1~10）</summary>
    public static int CurrentDifficulty
    {
        get => _currentDifficulty;
        set => _currentDifficulty = Mathf.Clamp(value, 1, MaxDifficulty);
    }

    /// <summary>最大难度等级</summary>
    public static int MaxDifficulty => _configs != null ? _configs.Length : 10;

    /// <summary>已解锁的最高难度</summary>
    public static int MaxUnlockedDifficulty
    {
        get => SaveManager.Instance?.GetPermanentInt("max_difficulty") ?? 1;
        set
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SetPermanentInt("max_difficulty", value);
                SaveManager.Instance.Save();
            }
        }
    }

    /// <summary>当前难度配置</summary>
    public static DifficultyConfig CurrentConfig
    {
        get
        {
            EnsureInitialized();
            int idx = Mathf.Clamp(_currentDifficulty - 1, 0, _configs.Length - 1);
            return _configs[idx];
        }
    }

    /// <summary>
    /// 获取指定难度的配置（供 DifficultySelectUI 使用）
    /// </summary>
    public static DifficultyConfig GetConfigForLevel(int level)
    {
        EnsureInitialized();
        int idx = Mathf.Clamp(level - 1, 0, _configs.Length - 1);
        return _configs[idx];
    }

    // ═══ 初始化 ═══

    public static void EnsureInitialized()
    {
        if (_initialized && _configs != null) return;
        _initialized = true;

        var loaded = Resources.LoadAll<DifficultyConfig>("Configs/Difficulties");
        if (loaded != null && loaded.Length > 0)
        {
            _configs = loaded;
            System.Array.Sort(_configs, (a, b) => a.difficultyLevel.CompareTo(b.difficultyLevel));
        }
        else
        {
            _configs = CreateDefaultConfigs();
        }
    }

    // ═══ 解锁检查 ═══

    /// <summary>
    /// 波次完成时调用 — 检查是否解锁新难度
    /// </summary>
    /// <returns>解锁的新难度等级，0=未解锁</returns>
    public static int CheckDifficultyUnlock(int currentDifficulty, int currentWave)
    {
        if (currentWave >= 20 && currentDifficulty == MaxUnlockedDifficulty && currentDifficulty < MaxDifficulty)
        {
            int newDifficulty = currentDifficulty + 1;
            MaxUnlockedDifficulty = newDifficulty;
            DebugHelper.Log($"[DifficultyManager] ★ Difficulty {newDifficulty} UNLOCKED!");
            return newDifficulty;
        }
        return 0;
    }

    // ═══ 存档 ═══

    public static int GetHighestWave(int difficulty)
    {
        return SaveManager.Instance?.GetPermanentInt($"wave_diff_{difficulty}") ?? 0;
    }

    public static void RecordWaveResult(int difficulty, int wave, int kills)
    {
        if (SaveManager.Instance == null) return;

        int highest = GetHighestWave(difficulty);
        if (wave > highest)
            SaveManager.Instance.SetPermanentInt($"wave_diff_{difficulty}", wave);

        // 更新角色存档
        var characterId = GameSceneBootstrap.CurrentCharacter?.characterId;
        if (!string.IsNullOrEmpty(characterId))
            SaveManager.Instance.UpdateCharacterStats(characterId, wave, kills);

        SaveManager.Instance.Save();
    }

    // ═══ 敌人倍率查询 ═══

    public static float GetEnemyHpMult(int wave) => CurrentConfig.GetEffectiveHpMult(wave);
    public static float GetEnemyDmgMult(int wave) => CurrentConfig.GetEffectiveDmgMult(wave);
    public static float GetEnemySpeedMult() => CurrentConfig.enemySpeedMult;
    public static float GetEliteFrequencyMult() => CurrentConfig.eliteFrequencyMult;
    public static float GetSpawnRateMult() => CurrentConfig.spawnRateMult;
    public static float GetXpMult() => CurrentConfig.xpMult;
    public static float GetCoinMult() => CurrentConfig.coinMult;
    public static float GetDropMult() => CurrentConfig.dropMult;
    public static float GetHealReduction() => CurrentConfig.healReduction;
    public static bool IsAffixEnabled() => CurrentConfig.enableAffixes;
    public static bool IsRiftEnabled() => CurrentConfig.enableRifts;

    // ═══ 默认配置生成 ═══

    private static DifficultyConfig[] CreateDefaultConfigs()
    {
        var configs = new DifficultyConfig[10];

        configs[0] = CreateConfig(1, "入门", "标准体验", Color.white,
            1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 0f, false, false, 0f, 1f, false);

        configs[1] = CreateConfig(2, "精英", "精英怪频率×2，词条池扩大", new Color(0.2f, 0.8f, 0.2f),
            1f, 1f, 1f, 2f, 1f, 1.25f, 1f, 1f, 0f, false, false, 0f, 1f, false);

        configs[2] = CreateConfig(3, "暴走", "敌人移速+30%，攻击更频繁", new Color(1f, 0.8f, 0f),
            1f, 1f, 1.3f, 1f, 1f, 1f, 1f, 1f, 0f, false, false, 0f, 1f, false);

        configs[3] = CreateConfig(4, "坚韧", "敌人HP×2，击杀奖励+50%", new Color(0.3f, 0.5f, 1f),
            2f, 1f, 1f, 1f, 1f, 1.5f, 1.5f, 1.5f, 0f, false, false, 0f, 1f, false);

        configs[4] = CreateConfig(5, "狂暴", "敌人攻击×2，掉落品质提升", new Color(1f, 0.2f, 0.2f),
            1f, 2f, 1f, 1f, 1f, 1f, 1f, 2f, 1f, false, false, 0f, 1f, false);

        configs[5] = CreateConfig(6, "腐化", "每波随机负面词缀生效", new Color(0.6f, 0.1f, 0.6f),
            1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 0f, true, false, 0f, 1f, false);

        configs[6] = CreateConfig(7, "裂隙", "裂隙传送门，击杀×3奖励", new Color(0f, 0.8f, 1f),
            1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 0f, false, true, 0f, 1f, false);

        configs[7] = CreateConfig(8, "混沌", "难度2~7全部叠加", new Color(1f, 0f, 0.5f),
            2f, 2f, 1.3f, 2f, 1f, 2f, 2f, 2f, 1f, true, true, 0f, 1f, false);

        configs[8] = CreateConfig(9, "无尽", "敌人属性按波次指数增长", new Color(0.5f, 0f, 1f),
            1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 0f, false, false, 0f, 1.2f, false);

        configs[9] = CreateConfig(10, "传说", "治疗-50%，永久死亡，传说装备", new Color(1f, 0.84f, 0f),
            1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 0f, false, false, 0.5f, 1f, true);

        return configs;
    }

    private static DifficultyConfig CreateConfig(int level, string name, string desc, Color color,
        float hp, float dmg, float speed, float elite, float spawn,
        float xp, float coin, float drop, float quality,
        bool affix, bool rift, float heal, float scaling, bool permaDeath)
    {
        var cfg = ScriptableObject.CreateInstance<DifficultyConfig>();
        cfg.difficultyLevel = level;
        cfg.difficultyName = name;
        cfg.difficultyDescription = desc;
        cfg.difficultyColor = color;
        cfg.enemyHpMult = hp;
        cfg.enemyDmgMult = dmg;
        cfg.enemySpeedMult = speed;
        cfg.eliteFrequencyMult = elite;
        cfg.spawnRateMult = spawn;
        cfg.xpMult = xp;
        cfg.coinMult = coin;
        cfg.dropMult = drop;
        cfg.dropQualityBonus = quality;
        cfg.enableAffixes = affix;
        cfg.enableRifts = rift;
        cfg.healReduction = heal;
        cfg.scalingExponent = scaling;
        cfg.permanentDeath = permaDeath;
        return cfg;
    }
}
