using UnityEngine;

/// <summary>
/// #21 Boss 战专属 UI — 屏幕顶部显示巨型 Boss 血条 + 阶段指示器
/// 通过 EventManager 订阅 Boss 事件，自动显示/隐藏。
/// 使用 OnGUI 实现，无需 Canvas。
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    // ════════════════════════════════════════════════════════════════
    // 配置
    // ════════════════════════════════════════════════════════════════

    [Header("血条尺寸")]
    [SerializeField] private float _barWidthRatio = 0.7f;   // 血条宽度占屏幕比例
    [SerializeField] private float _barHeight = 28f;         // 血条高度（像素）
    [SerializeField] private float _topOffset = 40f;         // 距屏幕顶部偏移

    [Header("颜色")]
    [SerializeField] private Color _bgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    [SerializeField] private Color _hpColor = new Color(0.8f, 0.1f, 0.1f, 1f);
    [SerializeField] private Color _hpColorLow = new Color(1f, 0.3f, 0f, 1f); // 低血量橙色
    [SerializeField] private Color _borderColor = new Color(0.4f, 0.05f, 0.05f, 1f);
    [SerializeField] private Color _textColor = new Color(1f, 0.9f, 0.7f, 1f);

    // ════════════════════════════════════════════════════════════════
    // 运行时状态
    // ════════════════════════════════════════════════════════════════

    private static BossHealthBarUI _instance;

    private string _bossName = "";
    private int _bossMaxHP = 1;
    private int _bossCurrentHP = 1;
    private int _currentPhase = 1;
    private int _maxPhase = 5;
    private bool _isVisible = false;
    private float _fadeAlpha = 0f;            // 渐入渐出 alpha
    private float _flashTimer = 0f;           // 阶段切换闪烁计时器
    private const float FLASH_DURATION = 0.5f;

    private GUIStyle _nameStyle;
    private GUIStyle _hpTextStyle;
    private GUIStyle _phaseStyle;
    private bool _stylesInitialized = false;

    // ════════════════════════════════════════════════════════════════
    // 生命周期
    // ════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _instance = this;
    }

    private void OnEnable()
    {
        EventManager.OnBossSpawn += OnBossSpawn;
        EventManager.OnBossPhaseChange += OnBossPhaseChange;
        EventManager.OnBossDeath += OnBossDeath;
        EventManager.OnBossHPChanged += OnBossHPChanged;
    }

    private void OnDisable()
    {
        EventManager.OnBossSpawn -= OnBossSpawn;
        EventManager.OnBossPhaseChange -= OnBossPhaseChange;
        EventManager.OnBossDeath -= OnBossDeath;
        EventManager.OnBossHPChanged -= OnBossHPChanged;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    // ════════════════════════════════════════════════════════════════
    // 事件回调
    // ════════════════════════════════════════════════════════════════

    private void OnBossSpawn(string bossTypeName, int maxHP)
    {
        _bossName = bossTypeName;
        _bossMaxHP = maxHP;
        _bossCurrentHP = maxHP;
        _currentPhase = 1;
        _isVisible = true;
        _flashTimer = FLASH_DURATION;
        DebugHelper.Log($"[BossHealthBarUI] Boss spawned: {bossTypeName}, HP={maxHP}");
    }

    private void OnBossPhaseChange(int phase, int maxPhase)
    {
        _currentPhase = phase;
        _maxPhase = maxPhase;
        _flashTimer = FLASH_DURATION; // 阶段切换时闪烁
        DebugHelper.Log($"[BossHealthBarUI] Phase changed: {phase}/{maxPhase}");
    }

    private void OnBossDeath(string bossTypeName)
    {
        _isVisible = false;
        DebugHelper.Log($"[BossHealthBarUI] Boss defeated: {bossTypeName}");
    }

    private void OnBossHPChanged(int currentHP, int maxHP)
    {
        _bossCurrentHP = currentHP;
        _bossMaxHP = maxHP;
    }

    // ════════════════════════════════════════════════════════════════
    // 渲染
    // ════════════════════════════════════════════════════════════════

    private void InitStyles()
    {
        if (_stylesInitialized) return;
        _stylesInitialized = true;

        _nameStyle = new GUIStyle();
        _nameStyle.fontSize = 18;
        _nameStyle.fontStyle = FontStyle.Bold;
        _nameStyle.normal.textColor = _textColor;
        _nameStyle.alignment = TextAnchor.MiddleCenter;

        _hpTextStyle = new GUIStyle();
        _hpTextStyle.fontSize = 14;
        _hpTextStyle.normal.textColor = Color.white;
        _hpTextStyle.alignment = TextAnchor.MiddleCenter;

        _phaseStyle = new GUIStyle();
        _phaseStyle.fontSize = 14;
        _phaseStyle.fontStyle = FontStyle.Bold;
        _phaseStyle.normal.textColor = new Color(1f, 0.8f, 0f);
        _phaseStyle.alignment = TextAnchor.MiddleCenter;
    }

    private void Update()
    {
        // 渐入渐出
        float targetAlpha = _isVisible ? 1f : 0f;
        _fadeAlpha = Mathf.MoveTowards(_fadeAlpha, targetAlpha, Time.unscaledDeltaTime * 4f);

        // 阶段闪烁计时
        if (_flashTimer > 0f)
            _flashTimer -= Time.unscaledDeltaTime;
    }

    private void OnGUI()
    {
        if (_fadeAlpha <= 0.01f) return;

        InitStyles();

        GUI.depth = -500;

        float screenW = Screen.width;
        float barW = screenW * _barWidthRatio;
        float barX = (screenW - barW) * 0.5f;
        float barY = _topOffset;

        // 保存原始 alpha
        Color origColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, _fadeAlpha);

        // ── Boss 名称 ──
        Rect nameRect = new Rect(barX, barY - 24f, barW, 22f);
        GUI.Label(nameRect, $"☠ {_bossName}", _nameStyle);

        // ── 血条背景 ──
        GUI.color = new Color(_bgColor.r, _bgColor.g, _bgColor.b, _bgColor.a * _fadeAlpha);
        GUI.DrawTexture(new Rect(barX, barY, barW, _barHeight), Texture2D.whiteTexture);

        // ── 血条填充 ──
        float hpRatio = _bossMaxHP > 0 ? (float)_bossCurrentHP / _bossMaxHP : 0f;
        Color fillHp = hpRatio < 0.3f ? _hpColorLow : _hpColor;

        // 阶段闪烁效果
        if (_flashTimer > 0f)
        {
            float flashT = _flashTimer / FLASH_DURATION;
            fillHp = Color.Lerp(fillHp, Color.white, flashT * 0.5f);
        }

        GUI.color = new Color(fillHp.r, fillHp.g, fillHp.b, fillHp.a * _fadeAlpha);
        GUI.DrawTexture(new Rect(barX + 2f, barY + 2f, (barW - 4f) * hpRatio, _barHeight - 4f), Texture2D.whiteTexture);

        // ── 血条边框 ──
        GUI.color = new Color(_borderColor.r, _borderColor.g, _borderColor.b, _borderColor.a * _fadeAlpha);
        DrawBorder(new Rect(barX, barY, barW, _barHeight), 2f);

        // ── HP 文字 ──
        GUI.color = new Color(1f, 1f, 1f, _fadeAlpha);
        Rect hpTextRect = new Rect(barX, barY, barW, _barHeight);
        GUI.Label(hpTextRect, $"{_bossCurrentHP} / {_bossMaxHP}", _hpTextStyle);

        // ── 阶段指示器（5 个菱形） ──
        float diamondSize = 14f;
        float diamondSpacing = 4f;
        float totalDiamondsW = _maxPhase * diamondSize + (_maxPhase - 1) * diamondSpacing;
        float diamondStartX = barX + (barW - totalDiamondsW) * 0.5f;
        float diamondY = barY + _barHeight + 6f;

        for (int i = 0; i < _maxPhase; i++)
        {
            float dx = diamondStartX + i * (diamondSize + diamondSpacing);
            Rect dRect = new Rect(dx, diamondY, diamondSize, diamondSize);

            if (i < _currentPhase)
            {
                // 当前/已完成阶段：金色高亮
                GUI.color = new Color(1f, 0.8f, 0f, _fadeAlpha);
            }
            else
            {
                // 未达到阶段：暗灰色
                GUI.color = new Color(0.3f, 0.3f, 0.3f, _fadeAlpha);
            }

            // 绘制菱形（旋转45度的正方形）
            GUIUtility.RotateAroundPivot(45f, new Vector2(dx + diamondSize * 0.5f, diamondY + diamondSize * 0.5f));
            GUI.DrawTexture(dRect, Texture2D.whiteTexture);
            GUI.matrix = Matrix4x4.identity; // 重置旋转
        }

        // ── 阶段文字 ──
        GUI.color = new Color(_phaseStyle.normal.textColor.r, _phaseStyle.normal.textColor.g,
            _phaseStyle.normal.textColor.b, _fadeAlpha);
        Rect phaseTextRect = new Rect(barX, diamondY + diamondSize + 2f, barW, 18f);
        GUI.Label(phaseTextRect, $"Phase {_currentPhase} / {_maxPhase}", _phaseStyle);

        // 恢复原始颜色
        GUI.color = origColor;
    }

    /// <summary>
    /// 绘制矩形边框（4 条线）
    /// </summary>
    private void DrawBorder(Rect rect, float thickness)
    {
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);                    // 上
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);      // 下
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);                    // 左
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);     // 右
    }

    // ════════════════════════════════════════════════════════════════
    // 公共 API
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static BossHealthBarUI Instance => _instance;

    /// <summary>
    /// 手动隐藏（用于场景重置）
    /// </summary>
    public void Hide()
    {
        _isVisible = false;
    }
}