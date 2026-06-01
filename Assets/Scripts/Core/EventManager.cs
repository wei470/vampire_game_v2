using UnityEngine;
using System;

/// <summary>
/// 全局事件总线，管理所有游戏事件的订阅和触发。
/// 对应 Python: game/events.py 中的 EventManager
/// 
/// 使用方式：
///   订阅: EventManager.OnEnemyKilled += MyHandler;
///   触发: EventManager.TriggerEnemyKilled(position, xp, coin);
///   取消: EventManager.OnEnemyKilled -= MyHandler;
/// </summary>
public static class EventManager
{
    // ============================================================
    // 战斗相关事件
    // ============================================================

    /// <summary>
    /// 敌人死亡事件。参数：(死亡位置, 经验奖励, 金币奖励)
    /// </summary>
    public static event Action<Vector3, int, int> OnEnemyKilled;

    /// <summary>
    /// 伤害事件。参数：(目标GameObject, 伤害值, 伤害来源位置)
    /// </summary>
    public static event Action<GameObject, int, Vector3> OnDamage;

    // ============================================================
    // 玩家相关事件
    // ============================================================

    /// <summary>
    /// 玩家获得经验事件。参数：(经验值)
    /// </summary>
    public static event Action<int> OnXPGained;

    /// <summary>
    /// 玩家升级事件。参数：(新等级)
    /// </summary>
    public static event Action<int> OnLevelUp;

    /// <summary>
    /// 玩家死亡事件。无参数。
    /// </summary>
    public static event Action OnPlayerDeath;

    /// <summary>
    /// 玩家受伤事件。参数：(当前HP, 最大HP)
    /// </summary>
    public static event Action<int, int> OnPlayerDamaged;

    /// <summary>
    /// 玩家治疗事件。参数：(治疗量, 当前HP)
    /// </summary>
    public static event Action<int, int> OnPlayerHealed;

    // ============================================================
    // 物品相关事件
    // ============================================================

    /// <summary>
    /// 物品拾取事件。参数：(物品类型, 数量)
    /// </summary>
    public static event Action<string, int> OnItemPicked;

    /// <summary>
    /// 金币变化事件。参数：(当前金币总数)
    /// </summary>
    public static event Action<int> OnCoinChanged;

    // ============================================================
    // 波次相关事件
    // ============================================================

    /// <summary>
    /// 波次开始事件。参数：(波次号)
    /// </summary>
    public static event Action<int> OnWaveStart;

    /// <summary>
    /// 波次完成事件。参数：(波次号)
    /// </summary>
    public static event Action<int> OnWaveComplete;

    /// <summary>
    /// 主题切换事件。参数：(新主题名称)
    /// </summary>
    public static event Action<string> OnThemeChanged;

    // ============================================================
    // 选择流程事件
    // ============================================================

    /// <summary>
    /// 选择流程完成事件。参数：(角色数据, 武器数据, 技能数据)
    /// </summary>
    public static event Action<CharacterData, WeaponData, SkillData> OnSelectionComplete;

    /// <summary>
    /// 角色选择事件。参数：(角色数据)
    /// </summary>
    public static event Action<CharacterData> OnCharacterSelected;

    // ============================================================
    // 游戏流程事件
    // ============================================================

    /// <summary>
    /// 游戏状态变化事件。参数：(旧状态, 新状态)
    /// </summary>
    public static event Action<GameManager.GameState, GameManager.GameState> OnGameStateChanged;

    /// <summary>
    /// 连击变化事件。参数：(当前连击数)
    /// </summary>
    public static event Action<int> OnComboChanged;

    // ============================================================
    // 触发方法（Trigger Methods）
    // ============================================================

    public static void TriggerEnemyKilled(Vector3 position, int xpReward, int coinReward)
    {
        OnEnemyKilled?.Invoke(position, xpReward, coinReward);
    }

    public static void TriggerDamage(GameObject target, int damage, Vector3 sourcePosition)
    {
        OnDamage?.Invoke(target, damage, sourcePosition);
    }


    public static void TriggerXPGained(int xp)
    {
        OnXPGained?.Invoke(xp);
    }

    public static void TriggerLevelUp(int newLevel)
    {
        OnLevelUp?.Invoke(newLevel);
    }

    public static void TriggerPlayerDeath()
    {
        OnPlayerDeath?.Invoke();
    }

    public static void TriggerPlayerDamaged(int currentHP, int maxHP)
    {
        OnPlayerDamaged?.Invoke(currentHP, maxHP);
    }

    public static void TriggerPlayerHealed(int healAmount, int currentHP)
    {
        OnPlayerHealed?.Invoke(healAmount, currentHP);
    }

    public static void TriggerItemPicked(string itemType, int amount)
    {
        OnItemPicked?.Invoke(itemType, amount);
    }

    public static void TriggerCoinChanged(int totalCoins)
    {
        OnCoinChanged?.Invoke(totalCoins);
    }

    public static void TriggerWaveStart(int waveNumber)
    {
        OnWaveStart?.Invoke(waveNumber);
    }

    public static void TriggerWaveComplete(int waveNumber)
    {
        OnWaveComplete?.Invoke(waveNumber);
    }

    public static void TriggerThemeChanged(string themeName)
    {
        OnThemeChanged?.Invoke(themeName);
    }

    public static void TriggerGameStateChanged(GameManager.GameState oldState, GameManager.GameState newState)
    {
        OnGameStateChanged?.Invoke(oldState, newState);
    }

    public static void TriggerComboChanged(int comboCount)
    {
        OnComboChanged?.Invoke(comboCount);
    }

    public static void TriggerSelectionComplete(CharacterData character, WeaponData weapon, SkillData skill)
    {
        OnSelectionComplete?.Invoke(character, weapon, skill);
    }

    public static void TriggerCharacterSelected(CharacterData character)
    {
        OnCharacterSelected?.Invoke(character);
    }

    // ============================================================
    // 工具方法
    // ============================================================

    /// <summary>
    /// 清除所有事件订阅（场景切换或游戏重启时调用）
    /// </summary>
    public static void ClearAll()
    {
        OnEnemyKilled = null;
        OnDamage = null;
        OnXPGained = null;
        OnLevelUp = null;
        OnPlayerDeath = null;
        OnPlayerDamaged = null;
        OnPlayerHealed = null;
        OnItemPicked = null;
        OnCoinChanged = null;
        OnWaveStart = null;
        OnWaveComplete = null;
        OnThemeChanged = null;
        OnGameStateChanged = null;
        OnComboChanged = null;
        OnSelectionComplete = null;
        OnCharacterSelected = null;

        DebugHelper.Log("[EventManager] All events cleared.");
    }
}