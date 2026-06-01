#pragma warning disable CS0414
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 统一输入处理器 — 集中管理游戏场景中的所有输入。
/// 
/// 职责：
/// - 跳波（T）、重开（R）、商店（Tab）、暂停（ESC）
/// - 武器切换（1-8 数字键 / Q 循环）— 从 WeaponController 迁移
/// - 鼠标位置查询（供 WeaponController 读取）
/// </summary>
public class GameInputHandler : MonoBehaviour
{
    private SpawnManager _spawnManager;
    private ShopUI _shopUI;
    private PauseMenuUI _pauseMenuUI;

    // 武器切换已删除 — 每局游戏锁定初始选择的武器

    /// <summary>
    /// 最近一次鼠标世界坐标（由 GameInputHandler 统一计算）
    /// </summary>
    public static Vector3 MouseWorldPosition { get; private set; }

    /// <summary>
    /// 鼠标是否有效（已按下/移动过）
    /// </summary>
    public static bool MouseValid { get; private set; }

    /// <summary>
    /// 注入依赖
    /// </summary>
    public void Setup(SpawnManager spawnManager)
    {
        _spawnManager = spawnManager;
    }

    /// <summary>
    /// 设置商店 UI 引用
    /// </summary>
    public void SetShopUI(ShopUI shopUI)
    {
        _shopUI = shopUI;
    }

    /// <summary>
    /// 设置暂停菜单 UI 引用
    /// </summary>
    public void SetPauseMenuUI(PauseMenuUI pauseMenuUI)
    {
        _pauseMenuUI = pauseMenuUI;
    }

    private void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        // 更新鼠标世界坐标（供 WeaponController 等组件使用）
        UpdateMouseWorldPosition();

        // T 键跳波（Skip 5 waves）
        if (kb.tKey.wasPressedThisFrame && _spawnManager != null && _spawnManager.WaveInProgress)
        {
            SkipWaves(5);
        }

        // R 键重新加载场景
        if (kb.rKey.wasPressedThisFrame)
        {
            EventManager.ClearAll();
            LevelUpUI.ResetMagnetMultiplier();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // Tab 键打开/关闭商店（避免与 WASD 的 S 键冲突）
        if (kb.tabKey.wasPressedThisFrame && _shopUI != null)
        {
            if (_shopUI.IsOpen)
            {
                _shopUI.CloseShop();
                if (GameManager.Instance != null)
                    GameManager.Instance.ChangeState(GameManager.GameState.Playing);
                Time.timeScale = 1f;
            }
            else if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            {
                _shopUI.OpenShop();
                GameManager.Instance.ChangeState(GameManager.GameState.Shop);
                Time.timeScale = 0f;
            }
        }

        // ESC 键暂停/恢复/关闭商店
        if (kb.escapeKey.wasPressedThisFrame)
        {
            if (_shopUI != null && _shopUI.IsOpen)
            {
                _shopUI.CloseShop();
                if (GameManager.Instance != null)
                    GameManager.Instance.ChangeState(GameManager.GameState.Playing);
                Time.timeScale = 1f;
            }
            else if (_pauseMenuUI != null)
            {
                if (_pauseMenuUI.IsPaused)
                    _pauseMenuUI.ResumeGame();
                else if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
                    _pauseMenuUI.ShowPause();
            }
            else if (GameManager.Instance != null)
            {
                if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
                    GameManager.Instance.PauseGame();
                else if (GameManager.Instance.CurrentState == GameManager.GameState.Paused)
                    GameManager.Instance.ResumeGame();
            }
        }
    }

    /// <summary>
    /// 统一计算鼠标世界坐标（使用缓存的 Camera 引用）
    /// </summary>
    private void UpdateMouseWorldPosition()
    {
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null) { MouseValid = false; return; }

        var cam = GameReferences.MainCamera;
        if (cam == null) { MouseValid = false; return; }

        Vector3 screenPos = mouse.position.ReadValue();
        screenPos.z = -cam.transform.position.z;
        MouseWorldPosition = cam.ScreenToWorldPoint(screenPos);
        MouseValid = true;
    }

    /// <summary>
    /// 跳过指定波次 — 杀死所有敌人，推进波次计数。
    /// </summary>
    private void SkipWaves(int count)
    {
        if (_spawnManager == null || !_spawnManager.WaveInProgress) return;

        DebugHelper.Log($"[GameInputHandler] Skipping {count} waves...");

        for (int i = 0; i < count; i++)
        {
            var enemies = FindObjectsByType<EnemyBase>();
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    var killRewarder = enemy.GetComponent<KillRewarder>();
                    if (killRewarder != null)
                    {
                        EventManager.TriggerEnemyKilled(enemy.transform.position, 10, 5);
                    }
                    Destroy(enemy.gameObject);
                }
            }
        }

        DebugHelper.Log($"[GameInputHandler] Skipped {count} waves. Current wave: {_spawnManager.CurrentWave}");
    }
}