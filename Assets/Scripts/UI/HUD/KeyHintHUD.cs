using UnityEngine;

/// <summary>
/// #30 快捷键提示 HUD — 屏幕底部显示简化键位提示条。
/// 
/// 功能：
/// 1. 首次游戏自动显示 5 秒后淡出
/// 2. 按 H 键手动切换显示/隐藏
/// 3. 所有样式/纹理缓存，零每帧分配
/// </summary>
public class KeyHintHUD : MonoBehaviour
{
    public static KeyHintHUD Instance { get; private set; }

    // ── 显示状态 ──
    private bool _visible = true;
    private float _autoHideTime;
    private float _alpha = 1f;
    private bool _autoHiding = true;

    // ── 样式缓存 ──
    private GUIStyle _hintStyle;
    private Texture2D _bgTex;
    private bool _stylesInit;

    // ── 配置 ──
    private const float AUTO_SHOW_DURATION = 8f;
    private const float FADE_DURATION = 1f;

    // ── Mage 专属键位 ──
    private string _hintText = "WASD Move | Mouse Shoot | E Detonate | Tab Shop | ESC Pause";

    private void Awake()
    {
        Instance = this;
        _autoHideTime = Time.time + AUTO_SHOW_DURATION;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 设置为 Mage 专属键位（包含 E 引爆）
    /// </summary>
    public void SetMageKeys()
    {
        _hintText = "WASD Move | E Detonate | Tab Shop | ESC Pause";
    }

    private void Update()
    {
        // H 键切换显示
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.hKey.wasPressedThisFrame)
        {
            _visible = !_visible;
            _autoHiding = false;
            _alpha = _visible ? 1f : 0f;
        }

        // 自动隐藏淡出
        if (_autoHiding && _visible && Time.time > _autoHideTime)
        {
            float fadeElapsed = Time.time - _autoHideTime;
            _alpha = Mathf.Clamp01(1f - fadeElapsed / FADE_DURATION);
            if (_alpha <= 0f)
            {
                _visible = false;
                _autoHiding = false;
            }
        }
    }

    private void OnGUI()
    {
        if (!_visible || _alpha <= 0.01f) return;

        InitStyles();

        GUIScaleHelper.BeginScale();

        float sw = 1920f;
        float sh = 1080f;
        float barH = 35f;
        float barY = sh - barH - 10f;

        // 半透明背景条
        GUI.color = new Color(0f, 0f, 0f, 0.4f * _alpha);
        if (_bgTex == null) _bgTex = MakeTex(1, 1, Color.white);
        GUI.DrawTexture(new Rect(sw * 0.15f, barY, sw * 0.7f, barH), _bgTex);

        // 文字
        _hintStyle.normal.textColor = new Color(0.8f, 0.9f, 1f, 0.85f * _alpha);
        GUI.Label(new Rect(0, barY, sw, barH), _hintText, _hintStyle);

        // H 提示
        if (_autoHiding)
        {
            var smallStyle = new GUIStyle(_hintStyle) { fontSize = 14 };
            smallStyle.normal.textColor = new Color(0.6f, 0.7f, 0.8f, 0.5f * _alpha);
            GUI.Label(new Rect(sw - 120, barY - 18, 110, 18), "[H] Toggle", smallStyle);
        }

        GUI.color = Color.white;

        GUIScaleHelper.EndScale();
    }

    private void InitStyles()
    {
        if (_stylesInit) return;
        _stylesInit = true;

        _hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };
    }

    private Texture2D MakeTex(int w, int h, Color c)
    {
        var t = new Texture2D(w, h);
        for (int i = 0; i < w * h; i++) t.SetPixel(i % w, i / w, c);
        t.Apply();
        return t;
    }
}