using UnityEngine;

/// <summary>
/// 设置面板 UI — 游戏设置选项。
/// 
/// 功能：
/// - BGM 音量调节
/// - SE 音量调节
/// - 屏幕震动开关
/// - 伤害数字开关
/// 
/// 使用方式：IMGUI 渲染，由 PauseMenuUI 或独立菜单调用
/// </summary>
public class SettingsUI : MonoBehaviour
{
    [Header("设置项")]
    [SerializeField] private float _bgmVolume = 0.7f;
    [SerializeField] private float _seVolume = 0.8f;
    [SerializeField] private bool _screenShakeEnabled = true;
    [SerializeField] private bool _damagePopupEnabled = true;

    private bool _isOpen = false;

    /// <summary>
    /// 是否打开设置面板
    /// </summary>
    public bool IsOpen => _isOpen;

    // 静态属性供其他系统读取
    public static float BGMVolume { get; private set; } = 0.7f;
    public static float SEVolume { get; private set; } = 0.8f;
    public static bool ScreenShakeEnabled { get; private set; } = true;
    public static bool DamagePopupEnabled { get; private set; } = true;

    private void Awake()
    {
        // 从 PlayerPrefs 加载
        _bgmVolume = PlayerPrefs.GetFloat("Settings_BGM", 0.7f);
        _seVolume = PlayerPrefs.GetFloat("Settings_SE", 0.8f);
        _screenShakeEnabled = PlayerPrefs.GetInt("Settings_Shake", 1) == 1;
        _damagePopupEnabled = PlayerPrefs.GetInt("Settings_DmgPopup", 1) == 1;
        ApplySettings();
    }

    public void Open() { _isOpen = true; }
    public void Close() { _isOpen = false; SaveSettings(); }

    /// <summary>
    /// 渲染设置面板（在 OnGUI 中调用）
    /// </summary>
    public void DrawSettings()
    {
        if (!_isOpen) return;

        // 半透明遮罩
        GUI.color = new Color(0, 0, 0, 0.5f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float panelW = 500;
        float panelH = 400;
        float panelX = (Screen.width - panelW) / 2f;
        float panelY = (Screen.height - panelH) / 2f;

        GUILayout.BeginArea(new Rect(panelX, panelY, panelW, panelH));

        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        GUI.color = new Color(0.3f, 0.7f, 0.9f);
        GUILayout.Label("SETTINGS", titleStyle);
        GUILayout.Space(20);

        var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 18 };

        // BGM 音量
        GUI.color = Color.white;
        GUILayout.Label($"  BGM Volume: {_bgmVolume:P0}", labelStyle);
        _bgmVolume = GUILayout.HorizontalSlider(_bgmVolume, 0f, 1f);
        GUILayout.Space(10);

        // SE 音量
        GUILayout.Label($"  SE Volume: {_seVolume:P0}", labelStyle);
        _seVolume = GUILayout.HorizontalSlider(_seVolume, 0f, 1f);
        GUILayout.Space(10);

        // 屏幕震动
        _screenShakeEnabled = GUILayout.Toggle(_screenShakeEnabled, "  Screen Shake", new GUIStyle(GUI.skin.toggle) { fontSize = 18 });
        GUILayout.Space(5);

        // 伤害数字
        _damagePopupEnabled = GUILayout.Toggle(_damagePopupEnabled, "  Damage Popups", new GUIStyle(GUI.skin.toggle) { fontSize = 18 });
        GUILayout.Space(20);

        // 应用并关闭
        var btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
        GUI.color = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("Apply & Close", btnStyle, GUILayout.Height(45)))
        {
            ApplySettings();
            Close();
        }

        GUILayout.EndArea();
    }

    private void ApplySettings()
    {
        BGMVolume = _bgmVolume;
        SEVolume = _seVolume;
        ScreenShakeEnabled = _screenShakeEnabled;
        DamagePopupEnabled = _damagePopupEnabled;

        // 应用到 ScreenShake 组件
        var shake = FindAnyObjectByType<ScreenShake>();
        if (shake != null) shake.enabled = _screenShakeEnabled;

        DebugHelper.Log($"[SettingsUI] Settings applied: BGM={_bgmVolume:F2}, SE={_seVolume:F2}, Shake={_screenShakeEnabled}, DmgPopup={_damagePopupEnabled}");
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("Settings_BGM", _bgmVolume);
        PlayerPrefs.SetFloat("Settings_SE", _seVolume);
        PlayerPrefs.SetInt("Settings_Shake", _screenShakeEnabled ? 1 : 0);
        PlayerPrefs.SetInt("Settings_DmgPopup", _damagePopupEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}