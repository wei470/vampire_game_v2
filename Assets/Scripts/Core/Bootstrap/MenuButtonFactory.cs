using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 菜单按钮工厂 — 负责动态创建菜单按钮的 UI 元素
/// </summary>
public static class MenuButtonFactory
{
    /// <summary>
    /// 动态创建 Test Mode 按钮
    /// </summary>
    public static Button CreateTestButton(Canvas canvas)
    {
        var btnObj = new GameObject("TestButton");
        btnObj.transform.SetParent(canvas.transform, false);

        var img = btnObj.AddComponent<Image>();
        img.color = UIColorTheme.AccentPink;

        var btn = btnObj.AddComponent<Button>();
        var cb = btn.colors;
        cb.normalColor = UIColorTheme.AccentPink;
        cb.highlightedColor = UIColorTheme.ButtonHover;
        cb.pressedColor = new Color(UIColorTheme.AccentPink.r, UIColorTheme.AccentPink.g, UIColorTheme.AccentPink.b, 0.7f);
        btn.colors = cb;

        var rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.18f);
        rect.anchorMax = new Vector2(0.5f, 0.18f);
        rect.sizeDelta = new Vector2(280, 50);

        var textObj = new GameObject("ButtonText");
        textObj.transform.SetParent(btnObj.transform, false);
        var text = textObj.AddComponent<Text>();
        text.text = "Test Mode (T)";
        text.font = UIFontProvider.DefaultFont;
        text.fontSize = 24;
        text.color = UIColorTheme.TextPrimary;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        DebugHelper.Log("[MenuButtonFactory] Test Mode button created");
        return btn;
    }

    /// <summary>
    /// 动态创建 Boss Test 按钮
    /// </summary>
    public static Button CreateBossTestButton(Canvas canvas)
    {
        var bossBtnObj = new GameObject("BossTestButton");
        bossBtnObj.transform.SetParent(canvas.transform, false);

        var bossImg = bossBtnObj.AddComponent<Image>();
        bossImg.color = new Color(0.8f, 0.2f, 0.2f);

        var bossBtn = bossBtnObj.AddComponent<Button>();
        var rect = bossBtnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.18f);
        rect.anchorMax = new Vector2(0.5f, 0.18f);
        rect.sizeDelta = new Vector2(280, 50);
        rect.anchoredPosition = new Vector2(0f, -60f);

        var textObj = new GameObject("ButtonText");
        textObj.transform.SetParent(bossBtnObj.transform, false);
        var text = textObj.AddComponent<Text>();
        text.text = "Boss Test (B)";
        text.font = UIFontProvider.DefaultFont;
        text.fontSize = 24;
        text.color = UIColorTheme.TextPrimary;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        DebugHelper.Log("[MenuButtonFactory] Boss Test button created");
        return bossBtn;
    }

    /// <summary>
    /// 动态创建 DPS Test 按钮
    /// </summary>
    public static Button CreateDpsTestButton(Canvas canvas)
    {
        var dpsBtnObj = new GameObject("DpsTestButton");
        dpsBtnObj.transform.SetParent(canvas.transform, false);

        var dpsImg = dpsBtnObj.AddComponent<Image>();
        dpsImg.color = new Color(0.9f, 0.6f, 0.1f);

        var dpsBtn = dpsBtnObj.AddComponent<Button>();
        var rect = dpsBtnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.18f);
        rect.anchorMax = new Vector2(0.5f, 0.18f);
        rect.sizeDelta = new Vector2(280, 50);
        rect.anchoredPosition = new Vector2(0f, -120f);

        var textObj = new GameObject("ButtonText");
        textObj.transform.SetParent(dpsBtnObj.transform, false);
        var text = textObj.AddComponent<Text>();
        text.text = "DPS Test (G)";
        text.font = UIFontProvider.DefaultFont;
        text.fontSize = 24;
        text.color = UIColorTheme.TextPrimary;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        DebugHelper.Log("[MenuButtonFactory] DPS Test button created");
        return dpsBtn;
    }
}
