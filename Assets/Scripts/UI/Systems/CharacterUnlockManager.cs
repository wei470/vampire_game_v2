using UnityEngine;

/// <summary>
/// #42 角色解锁管理器 — 追踪解锁进度，保存解锁状态到 SaveManager
/// 
/// 解锁条件：
/// - Warrior: 默认解锁
/// - Mage: 默认解锁
/// - Archer (Ranger): 存活 5 分钟后解锁
/// - Vampire: 单局击杀 300 敌人后解锁
/// - Assassin: 单局击杀 500 敌人后解锁
/// - Paladin: 承受 10000 伤害后解锁
/// - Necromancer: 击败 10 个 Boss 后解锁
/// - Berserker: 单局引爆 50 次后解锁
/// </summary>
public static class CharacterUnlockManager
{
    /// <summary>
    /// 角色解锁条件定义
    /// </summary>
    public struct UnlockCondition
    {
        public string characterId;
        public string description;
        public string statKey;
        public int requiredValue;
        public bool isDefault;
    }

    private static readonly UnlockCondition[] _conditions = new UnlockCondition[]
    {
        new UnlockCondition { characterId = "warrior", description = "默认解锁", isDefault = true },
        new UnlockCondition { characterId = "mage", description = "默认解锁", isDefault = true },
        new UnlockCondition { characterId = "ranger", description = "存活 5 分钟", statKey = "best_survival_time", requiredValue = 300 },
        new UnlockCondition { characterId = "vampire", description = "单局击杀 300 敌人", statKey = "best_kills", requiredValue = 300 },
        new UnlockCondition { characterId = "assassin", description = "单局击杀 500 敌人", statKey = "best_kills", requiredValue = 500 },
        new UnlockCondition { characterId = "paladin", description = "承受 10000 伤害", statKey = "total_damage_taken", requiredValue = 10000 },
        new UnlockCondition { characterId = "necromancer", description = "累计击败 10 个 Boss", statKey = "total_boss_kills", requiredValue = 10 },
        new UnlockCondition { characterId = "berserker", description = "单局引爆 50 次", statKey = "best_detonates", requiredValue = 50 },
    };

    public static UnlockCondition GetConditionForCharacter(string charId)
    {
        string lower = charId.ToLower();
        foreach (var cond in _conditions)
        {
            if (lower.Contains(cond.characterId) || cond.characterId.Contains(lower))
                return cond;
        }
        return new UnlockCondition { characterId = lower, description = "默认解锁", isDefault = true };
    }

    public static bool IsCharacterUnlocked(CharacterData character)
    {
        if (character == null) return true;
        var cond = GetConditionForCharacter(character.characterId ?? character.characterName);
        if (cond.isDefault) return true;

        if (SaveManager.Instance != null)
        {
            // 检查是否已手动解锁
            if (SaveManager.Instance.Data.upgrades.Get($"unlock_{cond.characterId}", 0) > 0)
                return true;

            // 检查统计值是否满足条件
            float statValue = GetStatValue(cond.statKey);
            return statValue >= cond.requiredValue;
        }

        return false;
    }

    public static float GetUnlockProgress(CharacterData character)
    {
        if (character == null) return 1f;
        var cond = GetConditionForCharacter(character.characterId ?? character.characterName);
        if (cond.isDefault) return 1f;

        if (SaveManager.Instance != null && SaveManager.Instance.Data.upgrades.Get($"unlock_{cond.characterId}", 0) > 0)
            return 1f;

        float current = GetStatValue(cond.statKey);
        return cond.requiredValue > 0 ? Mathf.Clamp01(current / cond.requiredValue) : 1f;
    }

    public static string GetUnlockConditionText(CharacterData character)
    {
        if (character == null) return "";
        var cond = GetConditionForCharacter(character.characterId ?? character.characterName);
        if (cond.isDefault) return "";
        float current = GetStatValue(cond.statKey);
        return $"🔒 {cond.description} ({(int)current}/{cond.requiredValue})";
    }

    public static bool TryUnlockCharacter(CharacterData character)
    {
        if (character == null) return false;
        if (IsCharacterUnlocked(character)) return false;

        var cond = GetConditionForCharacter(character.characterId ?? character.characterName);
        float statValue = GetStatValue(cond.statKey);
        if (statValue < cond.requiredValue) return false;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.upgrades.Set($"unlock_{cond.characterId}", 1);
            SaveManager.Instance.Save();
        }

        DebugHelper.Log($"[CharacterUnlockManager] Unlocked: {character.characterName} ({cond.description})");
        return true;
    }

    private static float GetStatValue(string statKey)
    {
        if (SaveManager.Instance == null || string.IsNullOrEmpty(statKey)) return 0f;
        // upgrades 使用 int 值，对于浮点统计用 int 乘以 100 存储
        // 或者直接用 int 值（存活时间秒数、击杀数等都是整数）
        return SaveManager.Instance.Data.upgrades.Get(statKey, 0);
    }

    /// <summary>
    /// 游戏结束时更新统计值
    /// </summary>
    public static void UpdateStatsOnGameEnd(float survivalTime, int kills, int bossKills, int detonateCount, int damageTaken)
    {
        if (SaveManager.Instance == null) return;

        var upgrades = SaveManager.Instance.Data.upgrades;

        // 更新最佳存活时间（秒）
        int currentTime = (int)survivalTime;
        int bestTime = upgrades.Get("best_survival_time", 0);
        if (currentTime > bestTime)
            upgrades.Set("best_survival_time", currentTime);

        // 更新最佳击杀
        int bestKills = upgrades.Get("best_kills", 0);
        if (kills > bestKills)
            upgrades.Set("best_kills", kills);

        // 累加 Boss 击杀
        int totalBossKills = upgrades.Get("total_boss_kills", 0);
        upgrades.Set("total_boss_kills", totalBossKills + bossKills);

        // 更新最佳引爆次数
        int bestDetonates = upgrades.Get("best_detonates", 0);
        if (detonateCount > bestDetonates)
            upgrades.Set("best_detonates", detonateCount);

        // 累加承受伤害
        int totalDmgTaken = upgrades.Get("total_damage_taken", 0);
        upgrades.Set("total_damage_taken", totalDmgTaken + damageTaken);

        // 自动保存
        SaveManager.Instance.Save();

        DebugHelper.Log($"[CharacterUnlockManager] Stats updated: time={currentTime}s, kills={kills}, bosses={bossKills}");
    }
}