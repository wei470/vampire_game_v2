#pragma warning disable CS0414
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 敌人生成预警 UI — 屏幕边缘显示敌人生成方向指示器。
///
/// 功能：
/// 1. Boss 出现前 3 秒：屏幕中央红色警告文字 + 警报音效
/// 2. 特殊波次出现前显示波次类型提示
/// 3. 屏幕边缘显示敌人生成方向箭头（红色三角）
///
/// 由 SpawnManager 调用 ShowWarning() 触发。
/// </summary>
public class SpawnWarningUI : MonoBehaviour
{
    // ════════════════════════════════════════════════════════════════
    // 单例
    // ════════════════════════════════════════════════════════════════

    private static SpawnWarningUI _instance;
    public static SpawnWarningUI Instance => _instance;

    // ════════════════════════════════════════════════════════════════
    // 配置
    // ════════════════════════════════════════════════════════════════

    [Header("Boss 预警")]
    [SerializeField] private float _bossWarningDuration = 3f;

    [Header("方向指示器")]
    [SerializeField] private float _arrowSize = 24f;
    [SerializeField] private float _edgeMargin = 40f;
    [SerializeField] private float _arrowFadeTime = 2f;

    // ════════════════════════════════════════════════════════════════
    // 运行时状态
    // ════════════════════════════════════════════════════════════════

    // Boss 预警
    private bool _showingBossWarning = false;
    private float _bossWarningEndTime = 0f;
    private string _bossWarningText = "⚠ BOSS INCOMING ⚠";

    // 波次类型提示
    private bool _showingWaveHint = false;
    private float _waveHintEndTime = 0f;
    private string _waveHintText = "";

    // 方向箭头
    private struct ArrowIndicator
    {
        public Vector2 worldPos;
        public float endTime;
    }
    private List<ArrowIndicator> _arrows = new List<ArrowIndicator>();

    // 纹理缓存
    private Texture2D _redTex;
    private Texture2D _goldTex;
    private Texture2D _darkTex;
    private Texture2D _orangeTex;

    // 样式缓存
    private GUIStyle _bossTextStyle;
    private GUIStyle _waveHintStyle;
    private GUIStyle _arrowLabelStyle;
    private bool _stylesInit;

    // ════════════════════════════════════════════════════════════════
    // 生命周期
    // ════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _instance = this;
        _redTex = MakeTex(new Color(0.9f, 0.15f, 0.15f, 0.9f));
        _goldTex = MakeTex(new Color(1f, 0.85f, 0f, 0.9f));
        _darkTex = MakeTex(new Color(0.1f, 0.05f, 0.05f, 0.85f));
        _orangeTex = MakeTex(new Color(1f, 0.5f, 0f, 0.8f));
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void InitStyles()
    {
        if (_stylesInit) return;
        _stylesInit = true;

        _bossTextStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 36,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.2f, 0.2f) }
        };

        _waveHintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.85f, 0f) }
        };

        _arrowLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.3f, 0.3f) }
        };
    }

    // ════════════════════════════════════════════════════════════════
    // 公共 API
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 显示 Boss 预警（Boss 出现前 3 秒调用）
    /// </summary>
    public void ShowBossWarning(string bossName = null)
    {
        _showingBossWarning = true;
        _bossWarningEndTime = Time.time + _bossWarningDuration;
        _bossWarningText = bossName != null
            ? $"⚠ {bossName} INCOMING ⚠"
            : "⚠ BOSS INCOMING ⚠";

        // 播放警报音效
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayDotTick(); // 复用现有音效

        DebugHelper.Log($"[SpawnWarningUI] Boss warning: {_bossWarningText}");
    }

    /// <summary>
    /// 显示波次类型提示
    /// </summary>
    public void ShowWaveHint(string hintText, float duration = 3f)
    {
        _showingWaveHint = true;
        _waveHintEndTime = Time.time + duration;
        _waveHintText = hintText;

        DebugHelper.Log($"[SpawnWarningUI] Wave hint: {hintText}");
    }

    /// <summary>
    /// 添加敌人生成方向指示器
    /// </summary>
    public void AddSpawnArrow(Vector2 worldPosition, float duration = 2f)
    {
        _arrows.Add(new ArrowIndicator
        {
            worldPos = worldPosition,
            endTime = Time.time + duration
        });
    }

    // ════════════════════════════════════════════════════════════════
    // 绘制
    // ════════════════════════════════════════════════════════════════

    private void OnGUI()
    {
        InitStyles();

        // 清理过期箭头
        for (int i = _arrows.Count - 1; i >= 0; i--)
        {
            if (Time.time >= _arrows[i].endTime)
                _arrows.RemoveAt(i);
        }

        // 检查是否需要绘制
        bool anyActive = _showingBossWarning || _showingWaveHint || _arrows.Count > 0;
        if (!anyActive) return;

        // 检查超时
        if (_showingBossWarning && Time.time >= _bossWarningEndTime)
            _showingBossWarning = false;
        if (_showingWaveHint && Time.time >= _waveHintEndTime)
            _showingWaveHint = false;

        GUIScaleHelper.BeginScale();

        // ── Boss 预警 ──
        if (_showingBossWarning)
            DrawBossWarning();

        // ── 波次类型提示 ──
        if (_showingWaveHint)
            DrawWaveHint();

        // ── 方向箭头 ──
        if (_arrows.Count > 0)
            DrawArrows();

        GUIScaleHelper.EndScale();
    }

    private void DrawBossWarning()
    {
        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f - 100f;
        float elapsed = _bossWarningEndTime - Time.time;

        // 闪烁效果：alpha 随时间脉冲
        float alpha = 0.6f + Mathf.Sin(Time.time * 8f) * 0.4f;
        alpha = Mathf.Clamp01(alpha);

        // 半透明背景条
        GUI.color = new Color(0.1f, 0f, 0f, 0.7f * alpha);
        GUI.DrawTexture(new Rect(centerX - 300, centerY - 30, 600, 70), _darkTex);

        // 文字
        GUI.color = new Color(1f, 0.2f, 0.2f, alpha);
        GUI.Label(new Rect(centerX - 300, centerY - 25, 600, 60), _bossWarningText, _bossTextStyle);

        // 倒计时秒数
        GUI.color = new Color(1f, 0.8f, 0.3f, alpha);
        var countdownStyle = new GUIStyle(_bossTextStyle) { fontSize = 20 };
        GUI.Label(new Rect(centerX - 100, centerY + 30, 200, 30),
            $"{elapsed:F1}s", countdownStyle);

        GUI.color = Color.white;
    }

    private void DrawWaveHint()
    {
        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f - 60f;
        float remaining = _waveHintEndTime - Time.time;

        // 淡入淡出
        float alpha = Mathf.Clamp01(remaining / 0.5f); // 最后 0.5 秒淡出

        // 背景
        GUI.color = new Color(0.1f, 0.05f, 0f, 0.6f * alpha);
        GUI.DrawTexture(new Rect(centerX - 250, centerY - 20, 500, 50), _darkTex);

        // 文字
        GUI.color = new Color(1f, 0.85f, 0f, alpha);
        GUI.Label(new Rect(centerX - 250, centerY - 15, 500, 40), _waveHintText, _waveHintStyle);

        GUI.color = Color.white;
    }

    private void DrawArrows()
    {
        var cam = GameReferences.MainCamera ?? Camera.main;
        if (cam == null) return;

        float screenW = Screen.width;
        float screenH = Screen.height;
        float halfW = screenW / 2f;
        float halfH = screenH / 2f;
        float safeMargin = _edgeMargin;

        for (int i = 0; i < _arrows.Count; i++)
        {
            var arrow = _arrows[i];
            float remaining = arrow.endTime - Time.time;
            if (remaining <= 0) continue;

            // 将世界坐标转为屏幕坐标
            Vector3 screenPos = cam.WorldToScreenPoint(arrow.worldPos);
            if (screenPos.z < 0) continue; // 在摄像机后面

            // 判断是否在屏幕外
            bool isOffScreen = screenPos.x < 0 || screenPos.x > screenW ||
                               screenPos.y < 0 || screenPos.y > screenH;

            if (!isOffScreen) continue; // 屏幕内不需要箭头

            // 计算方向（从屏幕中心指向敌人位置）
            Vector2 dir = new Vector2(screenPos.x - halfW, screenPos.y - halfH).normalized;

            // 箭头位置：屏幕边缘
            float arrowX = Mathf.Clamp(halfW + dir.x * (halfW - safeMargin), safeMargin, screenW - safeMargin);
            float arrowY = Mathf.Clamp(halfH + dir.y * (halfH - safeMargin), safeMargin, screenH - safeMargin);

            // 透明度随时间衰减
            float alpha = Mathf.Clamp01(remaining / 0.5f);

            // 绘制红色圆点
            GUI.color = new Color(1f, 0.2f, 0.2f, alpha * 0.8f);
            float dotSize = _arrowSize;
            GUI.DrawTexture(new Rect(arrowX - dotSize / 2, arrowY - dotSize / 2, dotSize, dotSize), _redTex);

            // 绘制方向指示文字
            GUI.color = new Color(1f, 1f, 1f, alpha * 0.7f);
            GUI.Label(new Rect(arrowX - 20, arrowY + dotSize / 2, 40, 20), "●", _arrowLabelStyle);
        }

        GUI.color = Color.white;
    }

    // ════════════════════════════════════════════════════════════════
    // 辅助
    // ════════════════════════════════════════════════════════════════

    private static Texture2D MakeTex(Color color)
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return tex;
    }

    /// <summary>
    /// 重置状态（场景重置时调用）
    /// </summary>
    public void ResetState()
    {
        _showingBossWarning = false;
        _showingWaveHint = false;
        _arrows.Clear();
    }
}