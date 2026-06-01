using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuUI : MonoBehaviour
{
    private bool _isPaused = false;
    private Texture2D _overlayTex;
    private Texture2D _resumeTex;
    private Texture2D _settingsTex;
    private Texture2D _quitTex;
    private Texture2D _hoverTex;

    public bool IsPaused => _isPaused;

    private void OnEnable() { EventManager.OnGameStateChanged += OnGameStateChanged; }
    private void OnDisable() { EventManager.OnGameStateChanged -= OnGameStateChanged; }
    private void OnGameStateChanged(GameManager.GameState oldState, GameManager.GameState newState)
    {
        _isPaused = (newState == GameManager.GameState.Paused);
    }

    public void ShowPause()
    {
        _isPaused = true;
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.Paused);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        _isPaused = false;
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.Playing);
        Time.timeScale = 1f;
    }

    public void ReturnToMenu()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        EventManager.ClearAll();
        LevelUpUI.ResetMagnetMultiplier();
        SceneManager.LoadScene("MenuScene");
    }

    private void InitTextures()
    {
        _overlayTex = UIColorTheme.MakeTexture(UIColorTheme.OverlayDark);
        _resumeTex = UIColorTheme.MakeTexture(UIColorTheme.AccentCyan);
        _settingsTex = UIColorTheme.MakeTexture(UIColorTheme.PanelBackground);
        _quitTex = UIColorTheme.MakeTexture(UIColorTheme.AccentMagenta);
        _hoverTex = UIColorTheme.MakeTexture(UIColorTheme.ButtonHover);
    }

    public void DrawPauseMenu()
    {
        if (!_isPaused) return;
        if (_overlayTex == null) InitTextures();

        GUI.color = UIColorTheme.OverlayDark;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlayTex);
        GUI.color = Color.white;

        float panelW = 400;
        float panelH = 350;
        float panelX = (Screen.width - panelW) / 2f;
        float panelY = (Screen.height - panelH) / 2f;

        GUILayout.BeginArea(new Rect(panelX, panelY, panelW, panelH));

        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 36,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = UIColorTheme.AccentCyan }
        };
        GUI.color = UIColorTheme.AccentCyan;
        GUILayout.Label("PAUSED", titleStyle);
        GUILayout.Space(30);

        var btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 24, fontStyle = FontStyle.Bold };

        btnStyle.normal.textColor = UIColorTheme.TextPrimary;
        btnStyle.normal.background = _resumeTex;
        btnStyle.hover.textColor = UIColorTheme.AccentCyan;
        btnStyle.hover.background = _hoverTex;
        GUI.color = Color.white;
        if (GUILayout.Button("▶ Resume", btnStyle, GUILayout.Height(50))) ResumeGame();
        GUILayout.Space(10);

        btnStyle.normal.textColor = UIColorTheme.TextSecondary;
        btnStyle.normal.background = _settingsTex;
        btnStyle.hover.textColor = UIColorTheme.AccentCyan;
        btnStyle.hover.background = _hoverTex;
        GUI.enabled = false;
        if (GUILayout.Button("⚙ Settings (Coming Soon)", btnStyle, GUILayout.Height(50))) { }
        GUI.enabled = true;
        GUILayout.Space(10);

        btnStyle.normal.textColor = UIColorTheme.TextPrimary;
        btnStyle.normal.background = _quitTex;
        btnStyle.hover.textColor = UIColorTheme.AccentPink;
        btnStyle.hover.background = _hoverTex;
        if (GUILayout.Button("✕ Return to Menu", btnStyle, GUILayout.Height(50))) ReturnToMenu();
        GUILayout.Space(20);

        var hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = UIColorTheme.TextSecondary }
        };
        GUI.color = UIColorTheme.TextSecondary;
        GUILayout.Label("Press ESC to resume", hintStyle);
        GUILayout.EndArea();
    }
}