using UnityEngine;

/// <summary>
/// 升级选项工具提示 — 鼠标悬停时显示详细信息。
/// 包含：标题、描述、当前值→升级后值预览、协同提示。
/// </summary>
public static class UpgradeTooltip
{
    private static bool _visible;
    private static string _title = "";
    private static string _description = "";
    private static string _valuePreview = "";
    private static string _synergyHint = "";
    private static Rect _position;

    public static void Show(Rect rect, string title, string description, string valuePreview = "", string synergyHint = "")
    {
        _visible = true;
        _position = rect;
        _title = title;
        _description = description;
        _valuePreview = valuePreview;
        _synergyHint = synergyHint;
    }

    public static void Hide()
    {
        _visible = false;
    }

    public static void Draw()
    {
        if (!_visible) return;

        float tooltipW = 300;
        float tooltipH = 100;
        float x = _position.x + _position.width + 10;
        float y = _position.y;

        // 防止超出屏幕
        if (x + tooltipW > Screen.width) x = _position.x - tooltipW - 10;
        if (y + tooltipH > Screen.height) y = Screen.height - tooltipH - 10;

        GUI.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        GUI.DrawTexture(new Rect(x, y, tooltipW, tooltipH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float lineY = y + 5;
        GUI.Label(new Rect(x + 10, lineY, tooltipW - 20, 20), _title);
        lineY += 22;

        GUI.color = new Color(0.8f, 0.8f, 0.8f);
        GUI.Label(new Rect(x + 10, lineY, tooltipW - 20, 40), _description);
        lineY += 42;

        if (!string.IsNullOrEmpty(_valuePreview))
        {
            GUI.color = new Color(0.3f, 1f, 0.3f);
            GUI.Label(new Rect(x + 10, lineY, tooltipW - 20, 18), _valuePreview);
            lineY += 20;
        }

        if (!string.IsNullOrEmpty(_synergyHint))
        {
            GUI.color = new Color(1f, 1f, 0.3f);
            GUI.Label(new Rect(x + 10, lineY, tooltipW - 20, 18), _synergyHint);
        }

        GUI.color = Color.white;
        _visible = false; // 每帧自动隐藏，需要持续调用 Show
    }
}
