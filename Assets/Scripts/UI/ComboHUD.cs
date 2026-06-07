using UnityEngine;

/// <summary>
/// 连击数 HUD — 屏幕中央偏上显示 Combo 数字。
///
/// 显示规则：
///   - 有连击时显示 "Combo X" 大字体，带缩放动画
///   - 颜色随连击等级变化：白→黄→橙→红→紫
///   - 连击中断时数字缩小消失
///   - 使用 OnGUI（与项目其他 HUD 一致）
///
/// 使用方式：挂载到 GameSceneBootstrap 所在 GameObject 上
/// </summary>
public class ComboHUD : MonoBehaviour
{
    // ── 配置 ──
    [Header("HUD 位置")]
    [SerializeField] private float _screenOffsetY = 120f; // 距离屏幕顶部的偏移

    [Header("动画")]
    [SerializeField] private float _scaleAnimSpeed = 8f;
    [SerializeField] private float _popScale = 1.4f;     // 新增连击时的弹出缩放

    // ── 运行时状态 ──
    private int _displayCombo;
    private float _displayScale = 1f;
    private float _targetScale = 1f;
    private float _fadeTimer;
    private const float FADE_DURATION = 1f; // 消失动画持续时间
    private bool _visible;
    private GUIStyle _comboStyle;
    private GUIStyle _tierStyle;

    private void OnEnable()
    {
        EventManager.OnComboChanged += OnComboChanged;
        _displayCombo = 0;
        _displayScale = 1f;
        _targetScale = 1f;
        _fadeTimer = 0f;
        _visible = false;
    }

    private void OnDisable()
    {
        EventManager.OnComboChanged -= OnComboChanged;
    }

    private void Update()
    {
        // 缩放动画：弹出效果
        _displayScale = Mathf.Lerp(_displayScale, _targetScale, Time.unscaledDeltaTime * _scaleAnimSpeed);

        // 消失动画
        if (!_visible && _fadeTimer > 0f)
        {
            _fadeTimer -= Time.unscaledDeltaTime;
            if (_fadeTimer <= 0f)
            {
                _fadeTimer = 0f;
            }
        }
    }

    private void OnComboChanged(int combo)
    {
        if (combo > 0)
        {
            _displayCombo = combo;
            _visible = true;
            _fadeTimer = 0f;

            // 弹出缩放动画
            _displayScale = _popScale;
            _targetScale = 1f;
        }
        else
        {
            // 连击中断，开始消失动画
            _visible = false;
            _fadeTimer = FADE_DURATION;
            _targetScale = 0.5f;
        }
    }

    private void OnGUI()
    {
        // 计算可见性
        float alpha;
        if (_visible)
        {
            alpha = 1f;
        }
        else if (_fadeTimer > 0f)
        {
            alpha = _fadeTimer / FADE_DURATION;
        }
        else
        {
            return; // 完全不可见时不绘制
        }

        if (_displayCombo <= 0 && !_visible) return;

        // 获取连击系统
        var comboSystem = ComboSystem.Instance;
        int tier = comboSystem != null ? comboSystem.GetComboTier() : 0;
        Color tierColor = ComboSystem.GetComboColor(tier);

        // 初始化样式
        InitStyles();

        // 屏幕中央偏上
        float centerX = Screen.width / 2f;
        float topY = _screenOffsetY;

        // 缩放矩阵
        Vector2 pivot = new Vector2(centerX, topY);
        GUIUtility.ScaleAroundPivot(Vector2.one * _displayScale, pivot);

        // 设置颜色（带透明度）
        Color textColor = tierColor;
        textColor.a = alpha;
        _comboStyle.normal.textColor = textColor;

        // 主文字：Combo 数字
        string comboText = $"COMBO  {_displayCombo}";

        // 绘制阴影（深色描边效果）
        Color shadowColor = new Color(0f, 0f, 0f, alpha * 0.6f);
        _comboStyle.normal.textColor = shadowColor;
        GUI.Label(new Rect(centerX - 150f + 2f, topY + 2f, 300f, 60f), comboText, _comboStyle);

        // 绘制主文字
        _comboStyle.normal.textColor = textColor;
        GUI.Label(new Rect(centerX - 150f, topY, 300f, 60f), comboText, _comboStyle);

        // 连击等级文字
        if (tier > 0)
        {
            string tierText = GetTierText(tier);
            Color tierTextColor = tierColor;
            tierTextColor.a = alpha * 0.8f;

            if (_tierStyle == null) InitStyles();
            _tierStyle.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.5f);
            GUI.Label(new Rect(centerX - 100f + 1f, topY + 50f + 1f, 200f, 30f), tierText, _tierStyle);

            _tierStyle.normal.textColor = tierTextColor;
            GUI.Label(new Rect(centerX - 100f, topY + 50f, 200f, 30f), tierText, _tierStyle);
        }

        // 重置缩放
        GUIUtility.ScaleAroundPivot(Vector2.one, pivot);
    }

    private string GetTierText(int tier)
    {
        return tier switch
        {
            4 => "✦ LEGENDARY ✦",
            3 => "★ EPIC ★",
            2 => "◆ RARE ◆",
            1 => "● ACTIVE ●",
            _ => "",
        };
    }

    private void InitStyles()
    {
        if (_comboStyle != null) return;

        _comboStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };
        _comboStyle.normal.textColor = Color.white;

        _tierStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _tierStyle.normal.textColor = Color.white;
    }
}