using UnityEngine;
using System;

/// <summary>
/// 全局事件总线 — 管理所有游戏事件的订阅和触发。
/// 泛型事件系统委托给 GenericEventBus。
///
/// 使用方式：
///   订阅: EventManager.OnEnemyKilled += MyHandler;
///   触发: EventManager.TriggerEnemyKilled(position, xp, coin);
///   取消: EventManager.OnEnemyKilled -= MyHandler;
///   泛型: EventManager.Subscribe<MyEvent>(handler);
/// </summary>
public static class EventManager
{
    // ── 泛型事件（委托给 GenericEventBus）──

    public static void Subscribe<T>(Action<T> handler)
    {
        GenericEventRegistry.EnsureRegistered<T>();
        GenericEventBus<T>.Subscribe(handler);
    }

    public static void Unsubscribe<T>(Action<T> handler) => GenericEventBus<T>.Unsubscribe(handler);
    public static void Publish<T>(T data) => GenericEventBus<T>.Publish(data);
    public static void ClearGeneric<T>() => GenericEventBus<T>.Clear();

    // ── 战斗事件 ──
    public static event Action<Vector3, int, int> OnEnemyKilled;
    public static event Action<GameObject, int, Vector3> OnDamage;

    // ── 玩家事件 ──
    public static event Action<int> OnXPGained;
    public static event Action<int> OnLevelUp;
    public static event Action OnPlayerDeath;
    public static event Action<int, int> OnPlayerDamaged;
    public static event Action<int, int> OnPlayerHealed;

    // ── 物品事件 ──
    public static event Action<string, int> OnItemPicked;
    public static event Action<int> OnCoinChanged;

    // ── 波次事件 ──
    public static event Action<int> OnWaveStart;
    public static event Action<int> OnWaveComplete;
    public static event Action<string> OnThemeChanged;

    // ── 选择流程事件 ──
    public static event Action<CharacterData, WeaponData, SkillData> OnSelectionComplete;
    public static event Action<CharacterData> OnCharacterSelected;

    // ── Boss 事件 ──
    public static event Action<string, int> OnBossSpawn;
    public static event Action<int, int> OnBossPhaseChange;
    public static event Action<string> OnBossDeath;
    public static event Action<int, int> OnBossHPChanged;

    // ── 游戏流程事件 ──
    public static event Action<GameManager.GameState, GameManager.GameState> OnGameStateChanged;
    public static event Action<int> OnComboChanged;

    // ── 触发方法 ──

    public static void TriggerEnemyKilled(Vector3 position, int xp, int coin) => OnEnemyKilled?.Invoke(position, xp, coin);
    public static void TriggerDamage(GameObject target, int damage, Vector3 src) => OnDamage?.Invoke(target, damage, src);
    public static void TriggerXPGained(int xp) => OnXPGained?.Invoke(xp);
    public static void TriggerLevelUp(int level) => OnLevelUp?.Invoke(level);
    public static void TriggerPlayerDeath() => OnPlayerDeath?.Invoke();
    public static void TriggerPlayerDamaged(int hp, int max) => OnPlayerDamaged?.Invoke(hp, max);
    public static void TriggerPlayerHealed(int heal, int hp) => OnPlayerHealed?.Invoke(heal, hp);
    public static void TriggerItemPicked(string type, int amount) => OnItemPicked?.Invoke(type, amount);
    public static void TriggerCoinChanged(int total) => OnCoinChanged?.Invoke(total);
    public static void TriggerWaveStart(int wave) => OnWaveStart?.Invoke(wave);
    public static void TriggerWaveComplete(int wave) => OnWaveComplete?.Invoke(wave);
    public static void TriggerThemeChanged(string name) => OnThemeChanged?.Invoke(name);
    public static void TriggerGameStateChanged(GameManager.GameState old, GameManager.GameState nw) => OnGameStateChanged?.Invoke(old, nw);
    public static void TriggerComboChanged(int count) => OnComboChanged?.Invoke(count);
    public static void TriggerSelectionComplete(CharacterData c, WeaponData w, SkillData s) => OnSelectionComplete?.Invoke(c, w, s);
    public static void TriggerCharacterSelected(CharacterData c) => OnCharacterSelected?.Invoke(c);
    public static void TriggerBossSpawn(string name, int hp) => OnBossSpawn?.Invoke(name, hp);
    public static void TriggerBossPhaseChange(int cur, int max) => OnBossPhaseChange?.Invoke(cur, max);
    public static void TriggerBossDeath(string name) => OnBossDeath?.Invoke(name);
    public static void TriggerBossHPChanged(int hp, int max) => OnBossHPChanged?.Invoke(hp, max);

    /// <summary>
    /// 清除所有事件订阅（场景切换/游戏重启时调用）
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
        OnBossSpawn = null;
        OnBossPhaseChange = null;
        OnBossDeath = null;
        OnBossHPChanged = null;

        // 泛型事件清理
        GenericEventRegistry.ClearAll();

        DebugHelper.Log("[EventManager] All events cleared.");
    }
}