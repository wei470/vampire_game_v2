using UnityEngine;

/// <summary>
/// UI 统一颜色主题 — 全局配色方案定义。
/// 所有 UI 组件必须从此处获取颜色，确保视觉统一。
/// 
/// 配色方案：
/// - #012326  深海暗青  — 主背景色 / 面板底色
/// - #025373  深蓝青    — 次级背景 / 卡片底色 / 未选中按钮
/// - #05F2DB  荧光青    — 主强调色 / 选中高亮 / 标题
/// - #D9048E  洋红      — 强调色 / 警告 / 敌人相关
/// - #F205CB  亮粉      — 超强调色 / 特殊高亮 / Boss 相关
/// </summary>
public static class UIColorTheme
{
    // ═══════════════════════════════════════════
    // 主色调定义
    // ═══════════════════════════════════════════

    /// <summary>深海暗青 — 主背景 (#012326)</summary>
    public static readonly Color DarkBackground = new Color(0.004f, 0.137f, 0.149f); // #012326

    /// <summary>深蓝青 — 面板/卡片底色 (#025373)</summary>
    public static readonly Color PanelBackground = new Color(0.008f, 0.325f, 0.451f); // #025373

    /// <summary>荧光青 — 主高亮/标题/选中 (#05F2DB)</summary>
    public static readonly Color AccentCyan = new Color(0.020f, 0.949f, 0.859f); // #05F2DB

    /// <summary>洋红 — 强调/警告 (#D9048E)</summary>
    public static readonly Color AccentMagenta = new Color(0.851f, 0.016f, 0.557f); // #D9048E

    /// <summary>亮粉 — 超强调/Boss/特殊 (#F205CB)</summary>
    public static readonly Color AccentPink = new Color(0.949f, 0.020f, 0.796f); // #F205CB

    // ═══════════════════════════════════════════
    // 衍生功能色
    // ═══════════════════════════════════════════

    /// <summary>半透明遮罩（暗青底）</summary>
    public static readonly Color OverlayDark = new Color(0f, 0.10f, 0.11f, 0.75f);

    /// <summary>半透明遮罩（更淡）</summary>
    public static readonly Color OverlayLight = new Color(0f, 0.08f, 0.09f, 0.55f);

    /// <summary>按钮正常态背景（深蓝青半透明）</summary>
    public static readonly Color ButtonNormal = new Color(0.008f, 0.325f, 0.451f, 0.85f);

    /// <summary>按钮悬停态背景（荧光青微亮）</summary>
    public static readonly Color ButtonHover = new Color(0.02f, 0.65f, 0.58f, 0.9f);

    /// <summary>按钮选中态背景（荧光青实色）</summary>
    public static readonly Color ButtonSelected = new Color(0.02f, 0.949f, 0.859f, 0.3f);

    /// <summary>按钮选中发光（荧光青半透明边框效果）</summary>
    public static readonly Color ButtonGlow = new Color(0.02f, 0.949f, 0.859f, 0.5f);

    /// <summary>文字主色（白色偏青）</summary>
    public static readonly Color TextPrimary = new Color(0.9f, 0.95f, 0.95f);

    /// <summary>文字次色（灰色偏青）</summary>
    public static readonly Color TextSecondary = new Color(0.5f, 0.6f, 0.62f);

    /// <summary>文字高亮（荧光青）</summary>
    public static readonly Color TextHighlight = new Color(0.02f, 0.949f, 0.859f);

    /// <summary>文字警告/敌人（洋红）</summary>
    public static readonly Color TextWarning = new Color(0.851f, 0.016f, 0.557f);

    /// <summary>文字特殊/Boss（亮粉）</summary>
    public static readonly Color TextSpecial = new Color(0.949f, 0.020f, 0.796f);

    /// <summary>成功/正面色（荧光青改）</summary>
    public static readonly Color SuccessGreen = new Color(0.02f, 0.85f, 0.7f);

    /// <summary>金币色（暖金色，与荧光青协调）</summary>
    public static readonly Color GoldText = new Color(0.02f, 0.949f, 0.6f);

    // ═══════════════════════════════════════════
    // #10 角色专属 UI 主题系统
    // ═══════════════════════════════════════════

    /// <summary>当前活跃的 UI 主题（默认=Default，Mage=Mage）</summary>
    private static string _activeTheme = "default";

    /// <summary>Mage 紫色边框</summary>
    public static readonly Color MagePanelBorder = new Color(0.55f, 0.15f, 0.85f);
    /// <summary>Mage 紫色背景</summary>
    public static readonly Color MageBackground = new Color(0.08f, 0.02f, 0.15f);
    /// <summary>Mage 绿色 HP</summary>
    public static readonly Color MageHpGreen = new Color(0.1f, 0.9f, 0.3f);
    /// <summary>Mage 紫色 XP</summary>
    public static readonly Color MageXpPurple = new Color(0.7f, 0.3f, 1f);
    /// <summary>Mage 强调色（DOT 绿）</summary>
    public static readonly Color MageAccentDot = new Color(0.2f, 1f, 0.5f);
    /// <summary>Mage 引爆色（紫红）</summary>
    public static readonly Color MageAccentDetonate = new Color(0.9f, 0.2f, 0.7f);

    /// <summary>
    /// #10 设置当前 UI 主题
    /// </summary>
    public static void SetTheme(string theme)
    {
        _activeTheme = theme ?? "default";
    }

    /// <summary>
    /// #10 获取当前主题名
    /// </summary>
    public static string ActiveTheme => _activeTheme;

    /// <summary>
    /// #10 是否是 Mage 主题
    /// </summary>
    public static bool IsMageTheme => _activeTheme == "mage";

    /// <summary>
    /// #10 获取当前主题的强调色（根据角色切换）
    /// Default: AccentCyan, Mage: MageAccentDetonate
    /// </summary>
    public static Color GetAccentColor()
    {
        return IsMageTheme ? MageAccentDetonate : AccentCyan;
    }

    /// <summary>
    /// #10 获取当前主题的高亮文字色
    /// Default: AccentCyan, Mage: MageAccentDot
    /// </summary>
    public static Color GetHighlightColor()
    {
        return IsMageTheme ? MageAccentDot : AccentCyan;
    }

    /// <summary>
    /// #10 获取当前主题的面板边框色
    /// </summary>
    public static Color GetPanelBorderColor()
    {
        return IsMageTheme ? MagePanelBorder : PanelBackground;
    }

    /// <summary>
    /// #10 获取当前主题的 HP 颜色
    /// </summary>
    public static Color GetHpColor()
    {
        return IsMageTheme ? MageHpGreen : AccentCyan;
    }

    /// <summary>
    /// #10 获取当前主题的 XP 颜色
    /// </summary>
    public static Color GetXpColor()
    {
        return IsMageTheme ? MageXpPurple : AccentMagenta;
    }

    // ═══════════════════════════════════════════
    // 工具方法
    // ═══════════════════════════════════════════

    /// <summary>
    /// 创建一个 1x1 纯色 Texture2D（用于 IMGUI 背景绘制）
    /// </summary>
    public static Texture2D MakeTexture(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// 创建 IMGUI 按钮的 GUIStyle（带荧光边框发光效果）
    /// </summary>
    /// <param name="fontSize">字体大小</param>
    /// <param name="isSelected">是否处于选中状态</param>
    public static GUIStyle CreateButtonStyle(int fontSize = 20, bool isSelected = false)
    {
        var style = new GUIStyle(GUI.skin.button)
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };

        // 正常态
        style.normal.textColor = TextPrimary;
        style.normal.background = MakeTexture(ButtonNormal);

        // 悬停态 — 荧光青边框
        Color hoverBg = ButtonHover;
        style.hover.textColor = AccentCyan;
        style.hover.background = MakeTexture(hoverBg);

        // 选中态 — 荧光青发光
        if (isSelected)
        {
            style.normal.textColor = AccentCyan;
            style.normal.background = MakeTexture(ButtonSelected);
        }

        return style;
    }

    /// <summary>
    /// 在 IMGUI 中绘制按钮发光边框效果（在按钮周围画荧光色矩形）
    /// </summary>
    public static void DrawButtonGlow(Rect buttonRect, float glowThickness = 2f)
    {
        var glowTex = MakeTexture(ButtonGlow);
        GUI.color = ButtonGlow;

        // 上边框
        GUI.DrawTexture(new Rect(buttonRect.x - glowThickness, buttonRect.y - glowThickness,
            buttonRect.width + glowThickness * 2, glowThickness), glowTex);
        // 下边框
        GUI.DrawTexture(new Rect(buttonRect.x - glowThickness, buttonRect.yMax,
            buttonRect.width + glowThickness * 2, glowThickness), glowTex);
        // 左边框
        GUI.DrawTexture(new Rect(buttonRect.x - glowThickness, buttonRect.y - glowThickness,
            glowThickness, buttonRect.height + glowThickness * 2), glowTex);
        // 右边框
        GUI.DrawTexture(new Rect(buttonRect.xMax, buttonRect.y - glowThickness,
            glowThickness, buttonRect.height + glowThickness * 2), glowTex);

        GUI.color = Color.white;
    }
}