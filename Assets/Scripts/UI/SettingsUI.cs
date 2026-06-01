using UnityEngine;

public class SettingsUI : MonoBehaviour
{
    [Header("设置项")]
    [SerializeField] private float _bgmVolume = 0.7f;
    [SerializeField] private float _seVolume = 0.8f;
    [SerializeField] private bool _screenShakeEnabled = true;
    [SerializeField] private bool _damagePopupEnabled = true;

    private bool _isOpen = false;
    private Texture2D _overlayTex;
    private Texture2D _panelBgTex;

    public bool IsOpen => _isOpen;
    public static float BGMVolume { get; private set; } = 0.7f;
    public static float SEVolume { get; private set; } = 0.8f;
    public static bool ScreenShakeEnabled { get; private set; } = true;
    public static bool DamagePopupEnabled { get; private set; } = true;

    private void Awake()
    {
        _bgmVolume = PlayerPrefs.GetFloat("Settings_BGM", 0.7f);
        _seVolume = PlayerPrefs.GetFloat("Settings_SE", 0.8f);
        _screenShakeEnabled = PlayerPrefs.GetInt("Settings_Shake", 1) == 1;
        _damagePopupEnabled = PlayerPrefs.GetInt("Settings_DmgPopup", 1) == 1;
        ApplySettings();
    }

    public void Open() { _isOpen = true; }
    public void Close() { _isOpen = false; SaveSettings(); }

    private void InitTextures() { if (_overlayTex == null) _overlayTex = UIColorTheme.MakeTexture(UIColorTheme.OverlayDark); if (_panelBgTex == null) _panelBgTex = UIColorTheme.MakeTexture(UIColorTheme.PanelBackground); }

    public void DrawSettings()
    {
        if (!_isOpen) return;
        InitTextures();

        GUI.color = UIColorTheme.OverlayDark; GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlayTex); GUI.color = Color.white;
        float panelW = 500, panelH = 400, panelX = (Screen.width - panelW) / 2f, panelY = (Screen.height - panelH) / 2f;
        GUI.color = UIColorTheme.PanelBackground; GUI.DrawTexture(new Rect(panelX, panelY, panelW, panelH), _panelBgTex); GUI.color = Color.white;
        GUILayout.BeginArea(new Rect(panelX, panelY, panelW, panelH));

        var titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 32, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = UIColorTheme.AccentCyan } };
        GUI.color = UIColorTheme.AccentCyan; GUILayout.Label("SETTINGS", titleStyle); GUILayout.Space(20);

        var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, normal = { textColor = UIColorTheme.TextPrimary } };
        GUI.color = UIColorTheme.TextPrimary;
        GUILayout.Label($"  BGM Volume: {_bgmVolume:P0}", labelStyle); _bgmVolume = GUILayout.HorizontalSlider(_bgmVolume, 0f, 1f); GUILayout.Space(10);
        GUILayout.Label($"  SE Volume: {_seVolume:P0}", labelStyle); _seVolume = GUILayout.HorizontalSlider(_seVolume, 0f, 1f); GUILayout.Space(10);
        _screenShakeEnabled = GUILayout.Toggle(_screenShakeEnabled, "  Screen Shake", new GUIStyle(GUI.skin.toggle) { fontSize = 22, normal = { textColor = UIColorTheme.TextPrimary } }); GUILayout.Space(5);
        _damagePopupEnabled = GUILayout.Toggle(_damagePopupEnabled, "  Damage Popups", new GUIStyle(GUI.skin.toggle) { fontSize = 22, normal = { textColor = UIColorTheme.TextPrimary } }); GUILayout.Space(20);

        var btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 24, fontStyle = FontStyle.Bold, normal = { textColor = UIColorTheme.TextPrimary, background = UIColorTheme.MakeTexture(UIColorTheme.AccentCyan) }, hover = { textColor = UIColorTheme.AccentCyan, background = UIColorTheme.MakeTexture(UIColorTheme.ButtonHover) } };
        GUI.color = Color.white;
        if (GUILayout.Button("Apply & Close", btnStyle, GUILayout.Height(45))) { ApplySettings(); Close(); }
        GUILayout.EndArea();
    }

    private void ApplySettings() { BGMVolume = _bgmVolume; SEVolume = _seVolume; ScreenShakeEnabled = _screenShakeEnabled; DamagePopupEnabled = _damagePopupEnabled; var s = FindAnyObjectByType<ScreenShake>(); if (s != null) s.enabled = _screenShakeEnabled; }
    private void SaveSettings() { PlayerPrefs.SetFloat("Settings_BGM", _bgmVolume); PlayerPrefs.SetFloat("Settings_SE", _seVolume); PlayerPrefs.SetInt("Settings_Shake", _screenShakeEnabled ? 1 : 0); PlayerPrefs.SetInt("Settings_DmgPopup", _damagePopupEnabled ? 1 : 0); PlayerPrefs.Save(); }
}