using UnityEngine;

/// <summary>
/// UI 工具集 — 合并 UIFormatUtils / UIFontProvider / GUIScaleHelper。
/// </summary>

#region 格式化

public static class UIFormatUtils
{
    public static string FormatDamage(long dmg)
    {
        if (dmg >= 1_000_000) return $"{dmg / 1_000_000f:F1}M";
        if (dmg >= 1_000) return $"{dmg / 1_000f:F1}K";
        return dmg.ToString();
    }

    public static string FormatTime(float seconds)
    {
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{mins:00}:{secs:00}";
    }
}

#endregion

#region 字体

public static class UIFontProvider
{
    private static Font _cachedFont;

    public static Font DefaultFont
    {
        get
        {
            if (_cachedFont == null)
            {
                _cachedFont = Resources.Load<Font>("Fonts/Default");
                if (_cachedFont == null)
                    _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            return _cachedFont;
        }
    }

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

#endregion

#region IMGUI 缩放

public static class GUIScaleHelper
{
    public const float REF_W = 1920f;
    public const float REF_H = 1080f;

    private static Matrix4x4 _originalMatrix;
    private static bool _scaling = false;

    public static void BeginScale()
    {
        if (_scaling) return;
        float scale = Mathf.Min(Screen.width / REF_W, Screen.height / REF_H);
        _originalMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
        _scaling = true;
    }

    public static void EndScale()
    {
        if (!_scaling) return;
        GUI.matrix = _originalMatrix;
        _scaling = false;
    }

    public static float ScaleFactor => Mathf.Min(Screen.width / REF_W, Screen.height / REF_H);
    public static float VirtualWidth => REF_W;
    public static float VirtualHeight => REF_H;
}

#endregion
