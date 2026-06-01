using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 暂停菜单 UI — ESC 暂停时显示的菜单面板。
/// 
/// 功能：
/// - 继续游戏
/// - 设置（预留）
/// - 返回主菜单
/// - 使用 IMGUI 渲染（与选择界面风格一致）
/// 
/// 使用方式：挂载到场景中的 GameObject 上，由 GameInputHandler 控制显示
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    private bool _isPaused = false;

    /// <summary>
    /// 是否处于暂停状态
    /// </summary>
    public bool IsPaused => _isPaused;

    private void OnEnable()
    {
        EventManager.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        EventManager.OnGameStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameManager.GameState oldState, GameManager.GameState newState)
    {
        _isPaused = (newState == GameManager.GameState.Paused);
    }

    /// <summary>
    /// 暂停游戏并显示菜单
    /// </summary>
    public void ShowPause()
    {
        _isPaused = true;
        if (GameManager.Instance != null)
            GameManager.Instance.ChangeState(GameManager.GameState.Paused);
        Time.timeScale = 0f;
    }

    /// <summary>
    /// 恢复游戏
    /// </summary>
    public void ResumeGame()
    {
        _isPaused = false;
        if (GameManager.Instance != null)
            GameManager.Instance.ChangeState(GameManager.GameState.Playing);
        Time.timeScale = 1f;
    }

    /// <summary>
    /// 返回主菜单
    /// </summary>
    public void ReturnToMenu()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        EventManager.ClearAll();
        LevelUpUI.ResetMagnetMultiplier();
        SceneManager.LoadScene("MenuScene");
    }

    /// <summary>
    /// 渲染暂停菜单（在 OnGUI 中调用）
    /// </summary>
    public void DrawPauseMenu()
    {
        if (!_isPaused) return;

        // 半透明全屏遮罩
        GUI.color = new Color(0, 0, 0, 0.6f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float panelW = 400;
        float panelH = 350;
        float panelX = (Screen.width - panelW) / 2f;
        float panelY = (Screen.height - panelH) / 2f;

        GUILayout.BeginArea(new Rect(panelX, panelY, panelW, panelH));

        // ── 标题 ──
        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 32,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        GUI.color = Color.white;
        GUILayout.Label("PAUSED", titleStyle);
        GUILayout.Space(30);

        // ── 按钮样式 ──
        var btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };

        // ── 继续游戏 ──
        GUI.color = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("▶ Resume", btnStyle, GUILayout.Height(50)))
        {
            ResumeGame();
        }
        GUILayout.Space(10);

        // ── 设置（预留）──
        GUI.color = new Color(0.3f, 0.7f, 0.9f);
        GUI.enabled = false; // 暂时禁用
        if (GUILayout.Button("⚙ Settings (Coming Soon)", btnStyle, GUILayout.Height(50)))
        {
            // TODO: 打开设置面板
        }
        GUI.enabled = true;
        GUILayout.Space(10);

        // ── 返回主菜单 ──
        GUI.color = new Color(0.9f, 0.3f, 0.3f);
        if (GUILayout.Button("✕ Return to Menu", btnStyle, GUILayout.Height(50)))
        {
            ReturnToMenu();
        }
        GUILayout.Space(20);

        // ── 提示 ──
        GUI.color = Color.gray;
        var hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        GUILayout.Label("Press ESC to resume", hintStyle);

        GUILayout.EndArea();
    }
}