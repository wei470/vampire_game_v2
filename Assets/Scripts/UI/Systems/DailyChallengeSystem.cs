using UnityEngine;

/// <summary>
/// #43 每日挑战模式 — 每天生成固定种子的挑战配置
/// 
/// 功能：
/// - 使用日期作为种子，同一天所有玩家面对相同挑战
/// - 每天随机生成3个特殊规则组合
/// - 挑战结果保存到 SaveManager
/// - 完成奖励额外金币
/// </summary>
public static class DailyChallengeSystem
{
    /// <summary>
    /// 挑战规则类型
    /// </summary>
    public enum ChallengeRule
    {
        DotOnly,           // 只有 DOT 伤害有效
        DetonateCdhalf,    // 引爆 CD 减半
        EnemySpeedx2,      // 敌人速度 ×2
        EnemyHpx1_5,       // 敌人 HP ×1.5
        NoHeal,            // 无法回血
        DoubleCoins,       // 金币获取翻倍
        HalfPlayerHp,      // 玩家 HP 减半
        TripleEnemies,     // 敌人数量 ×1.5
        BossRush,          // 每波都有 Boss
        GlassCannon,       // 伤害 ×3 但 HP ×0.3
    }

    /// <summary>
    /// 今日挑战配置
    /// </summary>
    public struct DailyChallengeConfig
    {
        public int seed;
        public ChallengeRule rule1;
        public ChallengeRule rule2;
        public ChallengeRule rule3;
        public string description;
    }

    // 规则中文描述
    private static readonly string[] _ruleDescriptions = new string[]
    {
        "只有 DOT 伤害有效",         // DotOnly
        "引爆 CD 减半",              // DetonateCdhalf
        "敌人速度 ×2",               // EnemySpeedx2
        "敌人 HP ×1.5",              // EnemyHpx1_5
        "无法回血",                  // NoHeal
        "金币获取翻倍",              // DoubleCoins
        "玩家 HP 减半",              // HalfPlayerHp
        "敌人数量 ×1.5",             // TripleEnemies
        "每波都有 Boss",             // BossRush
        "伤害 ×3 但 HP ×0.3",       // GlassCannon
    };

    /// <summary>
    /// 获取今日挑战配置
    /// </summary>
    public static DailyChallengeConfig GetTodayChallenge()
    {
        // 使用日期作为种子
        System.DateTime today = System.DateTime.UtcNow;
        int seed = today.Year * 10000 + today.Month * 100 + today.Day;

        var config = new DailyChallengeConfig();
        config.seed = seed;

        // 使用种子创建确定性随机
        var rng = new System.Random(seed);

        // 随机选择3个不重复的规则
        int ruleCount = System.Enum.GetValues(typeof(ChallengeRule)).Length;
        int r1 = rng.Next(ruleCount);
        int r2, r3;
        do { r2 = rng.Next(ruleCount); } while (r2 == r1);
        do { r3 = rng.Next(ruleCount); } while (r3 == r1 || r3 == r2);

        config.rule1 = (ChallengeRule)r1;
        config.rule2 = (ChallengeRule)r2;
        config.rule3 = (ChallengeRule)r3;

        // 生成描述
        config.description = $"📅 {today.Month}/{today.Day} 每日挑战\n" +
            $"  ✦ {GetRuleDescription(config.rule1)}\n" +
            $"  ✦ {GetRuleDescription(config.rule2)}\n" +
            $"  ✦ {GetRuleDescription(config.rule3)}";

        return config;
    }

    /// <summary>
    /// 获取规则中文描述
    /// </summary>
    public static string GetRuleDescription(ChallengeRule rule)
    {
        int idx = (int)rule;
        if (idx >= 0 && idx < _ruleDescriptions.Length)
            return _ruleDescriptions[idx];
        return "???";
    }

    /// <summary>
    /// 获取今日是否已完成挑战
    /// </summary>
    public static bool IsTodayCompleted()
    {
        if (SaveManager.Instance == null) return false;
        string key = GetTodayKey();
        return SaveManager.Instance.Data.upgrades.Get(key, 0) > 0;
    }

    /// <summary>
    /// 记录今日挑战完成
    /// </summary>
    public static void RecordTodayCompleted(int waveReached)
    {
        if (SaveManager.Instance == null) return;
        string key = GetTodayKey();
        SaveManager.Instance.Data.upgrades.Set(key, waveReached);
        SaveManager.Instance.Save();
    }

    /// <summary>
    /// 获取今日挑战的最佳波次
    /// </summary>
    public static int GetTodayBestWave()
    {
        if (SaveManager.Instance == null) return 0;
        string key = GetTodayKey();
        return SaveManager.Instance.Data.upgrades.Get(key, 0);
    }

    /// <summary>
    /// 获取累计完成的每日挑战次数
    /// </summary>
    public static int GetTotalCompletedCount()
    {
        if (SaveManager.Instance == null) return 0;
        return SaveManager.Instance.Data.upgrades.Get("daily_challenge_count", 0);
    }

    /// <summary>
    /// 生成今日挑战的种子（供 SpawnManager 等使用）
    /// </summary>
    public static int GetTodaySeed()
    {
        System.DateTime today = System.DateTime.UtcNow;
        return today.Year * 10000 + today.Month * 100 + today.Day;
    }

    /// <summary>
    /// 生成今日 key（格式：daily_YYYYMMDD）
    /// </summary>
    private static string GetTodayKey()
    {
        System.DateTime today = System.DateTime.UtcNow;
        return $"daily_{today.Year}{today.Month:D2}{today.Day:D2}";
    }

    // ════════════════════════════════════════════════════════════════
    // 运行时规则倍率查询（供 GameSceneBootstrap/SpawnManager 集成）
    // ════════════════════════════════════════════════════════════════

    private static DailyChallengeConfig? _activeConfig = null;
    private static bool _isDailyChallengeActive = false;

    /// <summary>是否正在每日挑战模式</summary>
    public static bool IsDailyChallengeActive => _isDailyChallengeActive;

    /// <summary>当前激活的每日挑战配置</summary>
    public static DailyChallengeConfig? ActiveConfig => _activeConfig;

    /// <summary>
    /// 激活每日挑战模式（在游戏开始前调用）
    /// </summary>
    public static void ActivateDailyChallenge()
    {
        _activeConfig = GetTodayChallenge();
        _isDailyChallengeActive = true;
    }

    /// <summary>
    /// 停止每日挑战模式
    /// </summary>
    public static void Deactivate()
    {
        _activeConfig = null;
        _isDailyChallengeActive = false;
    }

    /// <summary>
    /// 检查是否有指定规则
    /// </summary>
    public static bool HasRule(ChallengeRule rule)
    {
        if (!_isDailyChallengeActive || !_activeConfig.HasValue) return false;
        var c = _activeConfig.Value;
        return c.rule1 == rule || c.rule2 == rule || c.rule3 == rule;
    }

    /// <summary>
    /// 获取敌人 HP 倍率
    /// </summary>
    public static float GetEnemyHpMultiplier()
    {
        return HasRule(ChallengeRule.EnemyHpx1_5) ? 1.5f : 1f;
    }

    /// <summary>
    /// 获取敌人速度倍率
    /// </summary>
    public static float GetEnemySpeedMultiplier()
    {
        return HasRule(ChallengeRule.EnemySpeedx2) ? 2f : 1f;
    }

    /// <summary>
    /// 获取敌人数量倍率
    /// </summary>
    public static float GetEnemyCountMultiplier()
    {
        return HasRule(ChallengeRule.TripleEnemies) ? 1.5f : 1f;
    }

    /// <summary>
    /// 获取金币倍率
    /// </summary>
    public static float GetCoinMultiplier()
    {
        return HasRule(ChallengeRule.DoubleCoins) ? 2f : 1f;
    }

    /// <summary>
    /// 获取玩家 HP 倍率
    /// </summary>
    public static float GetPlayerHpMultiplier()
    {
        return HasRule(ChallengeRule.HalfPlayerHp) ? 0.5f : 1f;
    }

    /// <summary>
    /// 获取玩家伤害倍率
    /// </summary>
    public static float GetPlayerDamageMultiplier()
    {
        return HasRule(ChallengeRule.GlassCannon) ? 3f : 1f;
    }

    /// <summary>
    /// 获取玩家最终 HP 倍率（GlassCannon 时 HP ×0.3）
    /// </summary>
    public static float GetPlayerFinalHpMultiplier()
    {
        return HasRule(ChallengeRule.GlassCannon) ? 0.3f : GetPlayerHpMultiplier();
    }

    /// <summary>
    /// 是否每波都有 Boss
    /// </summary>
    public static bool IsBossRush()
    {
        return HasRule(ChallengeRule.BossRush);
    }

    /// <summary>
    /// 是否只有 DOT 伤害有效
    /// </summary>
    public static bool IsDotOnly()
    {
        return HasRule(ChallengeRule.DotOnly);
    }

    /// <summary>
    /// 是否无法回血
    /// </summary>
    public static bool IsNoHeal()
    {
        return HasRule(ChallengeRule.NoHeal);
    }

    /// <summary>
    /// 获取引爆 CD 倍率
    /// </summary>
    public static float GetDetonateCdMultiplier()
    {
        return HasRule(ChallengeRule.DetonateCdhalf) ? 0.5f : 1f;
    }
}