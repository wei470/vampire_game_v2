using UnityEngine;
using System;

/// <summary>
/// 游戏状态管理器，管理游戏全局状态机。
/// 对应 Python: game/state.py 中的 GameState
/// </summary>
public class GameManager : Singleton<GameManager>
{
    /// <summary>
    /// 游戏状态枚举
    /// </summary>
    public enum GameState
    {
        Menu,       // 主菜单
        Loading,    // 加载中
        Selecting,  // 角色/武器/技能选择
        Playing,    // 游戏中
        Paused,     // 暂停
        Shop,       // 商店
        Prepare,    // 波次准备阶段
        GameOver    // 游戏结束
    }

    [Header("当前游戏状态")]
    [SerializeField] private GameState _currentState = GameState.Menu;
    
    /// <summary>
    /// 当前游戏状态（只读）
    /// </summary>
    public GameState CurrentState => _currentState;

    /// <summary>
    /// 状态变化事件，参数：(旧状态, 新状态)
    /// </summary>
    public static event Action<GameState, GameState> OnStateChanged;

    protected override void Awake()
    {
        base.Awake();
        DebugHelper.Log($"[GameManager] Initialized, state: {_currentState}");
    }

    /// <summary>
    /// 切换游戏状态
    /// </summary>
    /// <param name="newState">目标状态</param>
    public void ChangeState(GameState newState)
    {
        if (_currentState == newState)
        {
            DebugHelper.LogWarning($"[GameManager] Already in state: {newState}");
            return;
        }

        GameState oldState = _currentState;
        _currentState = newState;

        DebugHelper.Log($"[GameManager] State changed: {oldState} → {newState}");

        // 处理状态切换的副作用（先退出旧状态，再进入新状态）
        OnExitState(oldState);
        OnEnterState(newState);

        // 广播状态变化事件（内部 + 全局）
        OnStateChanged?.Invoke(oldState, newState);
        EventManager.TriggerGameStateChanged(oldState, newState);
    }

    /// <summary>
    /// 进入新状态时的处理
    /// </summary>
    private void OnEnterState(GameState state)
    {
        switch (state)
        {
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            case GameState.Playing:
            case GameState.Menu:
                Time.timeScale = 1f;
                break;
        }
    }

    /// <summary>
    /// 离开旧状态时的处理
    /// </summary>
    private void OnExitState(GameState state)
    {
        // 预留：离开状态时的清理逻辑
    }

    /// <summary>
    /// 暂停游戏
    /// </summary>
    public void PauseGame()
    {
        if (_currentState == GameState.Playing)
        {
            ChangeState(GameState.Paused);
        }
    }

    /// <summary>
    /// 恢复游戏
    /// </summary>
    public void ResumeGame()
    {
        if (_currentState == GameState.Paused)
        {
            ChangeState(GameState.Playing);
        }
    }

    /// <summary>
    /// 游戏结束
    /// </summary>
    public void EndGame()
    {
        ChangeState(GameState.GameOver);
    }

    /// <summary>
    /// 返回主菜单
    /// </summary>
    public void ReturnToMenu()
    {
        ChangeState(GameState.Menu);
        Time.timeScale = 1f;
    }

    /// <summary>
    /// 检查是否在游戏中（包括暂停、商店、准备阶段）
    /// </summary>
    public bool IsInGame()
    {
        return _currentState == GameState.Playing 
            || _currentState == GameState.Paused 
            || _currentState == GameState.Shop 
            || _currentState == GameState.Prepare;
    }
}