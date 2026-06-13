using UnityEngine;

/// <summary>
/// UI 字体统一提供器。
/// Unity 6 仅保留 LegacyRuntime.ttf 为内置字体，实际为动态 TrueType，
/// 配合 CanvasScaler 可在任意缩放比下清晰渲染。
/// </summary>
public static class UIFontProvider
{
    private static Font _cachedFont;

    /// <summary>
    /// 获取默认 UI 字体。
    /// 优先级：项目导入字体 > Unity 内置 LegacyRuntime（Unity 6 唯一内置）
    /// </summary>
    public static Font DefaultFont
    {
        get
        {
            if (_cachedFont == null)
            {
                // 1. 优先加载项目自定义字体（Assets/Fonts/Default.ttf 或 .otf）
                _cachedFont = Resources.Load<Font>("Fonts/Default");

                // 2. Unity 6 唯一内置字体（动态 TrueType，非位图）
                if (_cachedFont == null)
                    _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            return _cachedFont;
        }
    }

    /// <summary>
    /// 为动态创建的 Canvas 添加 CanvasScaler（参考 1920x1080）。
    /// 此步骤是解决字体模糊的关键——未配置 CanvasScaler 的 Canvas 在非参考分辨率下会缩放失真。
    /// </summary>
    public static void EnsureCanvasScaler(Canvas canvas)
    {
        if (canvas == null) return;
        if (canvas.GetComponent<UnityEngine.UI.CanvasScaler>() != null) return;

        var scaler = canvas.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }
}