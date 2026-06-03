using UnityEngine;

/// <summary>
/// 波次间歇期 UI — 每波结束后显示统计信息和下一波预告。
/// 
/// 功能：
/// 1. 屏幕中央显示 "Wave N Complete!" + 本波统计（击杀数、XP、金币）
/// 2. 显示下一波预告（"Next: Wave N+1" + 波次类型提示）
/// 3. 自动淡入淡出，间歇期结束后消失
/// 4. 间歇期期间显示升级提示（如有可选升级）
/// </summary>
public class WaveIntermissionUI : MonoBehaviour
{
    public static WaveIntermissionUI Instance { get; private set; }

    // ── 波次统计追踪 ──
    private int _waveKills;
    private int _waveXP;
    private int _waveCoins;
    private int _currentWave;

    // ── 显示状态 ──
    private bool _showing;
    private float _showStartTime;
    private float _showDuration = 4f; // 显示持续时间（与间歇期同步）
    private float _alpha;

    // ── 下一波预告 ──
    private string _nextWaveHint = "";
    private bool _isNextBossWave;

    // ── GUI 样式缓存 ──
    private GUIStyle _overlayStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _statLabelStyle;
    private GUIStyle _statValueStyle;
    private GUIStyle _hintStyle;
    private GUIStyle _nextWaveStyle;
    private GUIStyle _upgradeHintStyle;
    private bool _stylesInitialized;

    // ── 纹理缓存 ──
    private Texture2D _overlayTex;
    private Texture2D _dividerTex;
    private Texture2D _statBgTex;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        EventManager.OnEnemyKilled += OnEnemyKilled;
        EventManager.OnWaveComplete += OnWaveComplete;
        EventManager.OnWaveStart += OnWaveStart;
    }

    private void OnDisable()
    {
        EventManager.OnEnemyKilled -= OnEnemyKilled;
        EventManager.OnWaveComplete -= OnWaveComplete;
        EventManager.OnWaveStart -= OnWaveStart;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 设置间歇期持续时间（由 SpawnManager 调用）
    /// </summary>
    public void SetRestDuration(float duration)
    {
        _showDuration = Mathf.Max(1f, duration);
    }

    /// <summary>
    /// 设置下一波预告信息（由 SpawnManager 调用）
    /// </summary>
    public void SetNextWavePreview(int nextWave, bool isBoss, string specialType = null)
    {
        _isNextBossWave = isBoss;
        if (isBoss)
            _nextWaveHint = "⚠ BOSS WAVE";
        else if (!string.IsNullOrEmpty(specialType) && specialType != "None")
            _nextWaveHint = $"★ {specialType}";
        else
            _nextWaveHint = $"Wave {nextWave}";
    }

    // ── 事件处理 ──

    private void OnEnemyKilled(Vector3 pos, int xp, int coin)
    {
        if (!_showing) // 只在波次进行中统计
        {
            _waveKills++;
            _waveXP += xp;
            _waveCoins += coin;
        }
    }

    private void OnWaveComplete(int wave)
    {
        _currentWave = wave;
        _showing = true;
        _showStartTime = Time.unscaledTime;
        _alpha = 0f;
    }

    private void OnWaveStart(int wave)
    {
        // 新波次开始，重置统计
        _waveKills = 0;
        _waveXP = 0;
        _waveCoins = 0;
        _showing = false;
    }

    private void Update()
    {
        if (!_showing) return;

        float elapsed = Time.unscaledTime - _showStartTime;
        float fadeInDuration = 0.3f;
        float fadeOutDuration = 0.5f;

        // 淡入
        if (elapsed < fadeInDuration)
            _alpha = elapsed / fadeInDuration;
        // 淡出
        else if (elapsed > _showDuration - fadeOutDuration)
            _alpha = Mathf.Clamp01((_showDuration - elapsed) / fadeOutDuration);
        else
            _alpha = 1f;

        // 超时隐藏
        if (elapsed >= _showDuration)
        {
            _showing = false;
            _alpha = 0f;
        }
    }

    private void OnGUI()
    {
        if (!_showing || _alpha <= 0.01f) return;

        InitStyles();

        GUIScaleHelper.BeginScale();

        // 在 GUIScaleHelper 缩放范围内，坐标基于 1920×1080 参考分辨率
        float sw = 1920f;
        float sh = 1080f;

        // 半透明遮罩（仅中央区域）
        Color bgColor = new Color(0f, 0f, 0f, 0.6f * _alpha);
        GUI.color = bgColor;
        float overlayH = 280f;
        float overlayY = (sh - overlayH) / 2f - 20f;
        if (_overlayTex == null) _overlayTex = MakeTex(1, 1, Color.white);
        GUI.DrawTexture(new Rect(0, overlayY, sw, overlayH), _overlayTex);
        GUI.color = Color.white;

        // 设置整体 alpha
        Color c = GUI.color;
        c.a = _alpha;
        GUI.color = c;

        float centerX = sw / 2f;
        float panelW = 500f;
        float panelX = centerX - panelW / 2f;
        float y = overlayY + 15f;

        // ── 标题：Wave N Complete! ──
        _titleStyle.normal.textColor = new Color(0.3f, 1f, 0.5f, _alpha);
        GUI.Label(new Rect(panelX, y, panelW, 45f), $"✦ Wave {_currentWave} Complete!", _titleStyle);
        y += 50f;

        // ── 分隔线 ──
        DrawDivider(panelX + 50f, y, panelW - 100f);
        y += 12f;

        // ── 统计数据 ──
        float statW = panelW / 3f;
        DrawStat(panelX, y, statW, "KILLS", _waveKills.ToString(), new Color(1f, 0.4f, 0.4f, _alpha));
        DrawStat(panelX + statW, y, statW, "XP", $"+{_waveXP}", new Color(0.3f, 0.9f, 0.3f, _alpha));
        DrawStat(panelX + statW * 2f, y, statW, "COINS", $"+{_waveCoins}", new Color(1f, 0.85f, 0f, _alpha));
        y += 65f;

        // ── 分隔线 ──
        DrawDivider(panelX + 50f, y, panelW - 100f);
        y += 12f;

        // ── 下一波预告 ──
        Color nextColor = _isNextBossWave
            ? new Color(1f, 0.3f, 0.3f, _alpha)
            : new Color(0.9f, 0.8f, 0.3f, _alpha);
        _nextWaveStyle.normal.textColor = nextColor;
        string nextLabel = _isNextBossWave
            ? $"⚠ Next: {_nextWaveHint}"
            : $"Next: {_nextWaveHint}";
        GUI.Label(new Rect(panelX, y, panelW, 35f), nextLabel, _nextWaveStyle);
        y += 38f;

        // ── 升级提示 ──
        var levelUpUI = FindAnyObjectByType<LevelUpUI>();
        if (levelUpUI != null && levelUpUI.HasPendingOptions())
        {
            _upgradeHintStyle.normal.textColor = new Color(1f, 0.9f, 0.3f, _alpha * (0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 4f)));
            GUI.Label(new Rect(panelX, y, panelW, 30f), "⬆ Level Up available!", _upgradeHintStyle);
        }

        // 重置 alpha
        GUI.color = Color.white;

        GUIScaleHelper.EndScale();
    }

    // ── 绘制辅助 ──

    private void DrawStat(float x, float y, float w, string label, string value, Color color)
    {
        float labelH = 22f;
        float valueH = 35f;

        _statLabelStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f, _alpha);
        GUI.Label(new Rect(x, y, w, labelH), label, _statLabelStyle);

        _statValueStyle.normal.textColor = color;
        GUI.Label(new Rect(x, y + labelH, w, valueH), value, _statValueStyle);
    }

    private void DrawDivider(float x, float y, float w)
    {
        Color lineColor = new Color(0.4f, 0.8f, 0.6f, 0.5f * _alpha);
        if (_dividerTex == null) _dividerTex = MakeTex(1, 1, Color.white);
        GUI.color = lineColor;
        GUI.DrawTexture(new Rect(x, y, w, 2f), _dividerTex);
        GUI.color = Color.white;
    }

    private void InitStyles()
    {
        if (_stylesInitialized) return;
        _stylesInitialized = true;

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 36,
            fontStyle = FontStyle.Bold,
            font = Font.CreateDynamicFontFromOSFont("Arial", 36)
        };

        _statLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = 16,
            fontStyle = FontStyle.Normal
        };

        _statValueStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = 30,
            fontStyle = FontStyle.Bold
        };

        _hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Italic
        };

        _nextWaveStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };

        _upgradeHintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };
    }

    private Texture2D MakeTex(int w, int h, Color c)
    {
        var tex = new Texture2D(w, h);
        for (int i = 0; i < w * h; i++) tex.SetPixel(i % w, i / w, c);
        tex.Apply();
        return tex;
    }
}