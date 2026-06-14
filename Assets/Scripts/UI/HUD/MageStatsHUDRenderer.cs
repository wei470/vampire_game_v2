using UnityEngine;

/// <summary>
/// MageStatsHUD 的绘制逻辑拆分 — 包含简化版条和完整面板的绘制方法
/// 由 MageStatsHUD 在 OnGUI 中调用
/// </summary>
public static class MageStatsHUDRenderer
{
    // ════════════════════════════════════════════════════════════════
    // 颜色方案（紫色系 Mage 主题）
    // ════════════════════════════════════════════════════════════════

    internal static readonly Color PanelBg = new Color(0.08f, 0.05f, 0.15f, 0.92f);
    internal static readonly Color HeaderBg = new Color(0.25f, 0.1f, 0.4f, 0.95f);
    internal static readonly Color DividerColor = new Color(0.4f, 0.2f, 0.6f, 0.6f);
    internal static readonly Color TextPrimary = new Color(0.95f, 0.9f, 1f);
    internal static readonly Color TextSecondary = new Color(0.7f, 0.6f, 0.8f);
    internal static readonly Color AccentPurple = new Color(0.8f, 0.3f, 1f);
    internal static readonly Color AccentGold = new Color(1f, 0.85f, 0f);
    internal static readonly Color ProgressBg = new Color(0.15f, 0.08f, 0.25f);
    internal static readonly Color ProgressFill = new Color(0.5f, 0.2f, 0.8f);
    internal static readonly Color MilestoneComplete = new Color(1f, 0.85f, 0f);
    internal static readonly Color MilestoneIncomplete = new Color(0.3f, 0.2f, 0.4f);

    // ════════════════════════════════════════════════════════════════
    // 简化版（非暂停时）
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 绘制左下角简化版统计条
    /// </summary>
    public static void DrawCompactBar(ICharacterPassive passive, float marginLeft, float marginBottom,
        Texture2D panelBgTex, Texture2D dividerTex, Texture2D[] dotTextures,
        GUIStyle smallStyle)
    {
        float x = marginLeft;
        float y = Screen.height - marginBottom;
        float w = 280f;
        float h = 42f;

        // 背景
        GUI.color = PanelBg;
        GUI.DrawTexture(new Rect(x, y, w, h), panelBgTex);

        // 标题
        GUI.color = AccentPurple;
        GUI.Label(new Rect(x + 8, y + 2, 80, 18), "⚔ MAGE", smallStyle);

        // DPS
        float totalDps = MageStatsDataCollector.CalculateCurrentDPS(passive);
        GUI.color = TextPrimary;
        GUI.Label(new Rect(x + 80, y + 2, 100, 18), $"DPS: {totalDps:F1}", smallStyle);

        // 引爆 CD（仅 Mage 角色）
        if (passive is MagePassive mage)
        {
            float cdRemain = mage.DetonateCooldownRemaining;
            bool ready = mage.DetonateReady;
            if (ready)
            {
                GUI.color = AccentGold;
                GUI.Label(new Rect(x + 180, y + 2, 90, 18), "[E] READY", smallStyle);
            }
            else
            {
                GUI.color = AccentPurple;
                GUI.Label(new Rect(x + 180, y + 2, 90, 18), $"[E] {cdRemain:F1}s", smallStyle);
            }
        }

        // DOT 子弹图标条
        DrawDotGunIcons(passive, x + 8, y + 20, w - 16, dotTextures, smallStyle);

        // 边框
        GUI.color = DividerColor;
        DrawBorder(x, y, w, h, dividerTex);

        GUI.color = Color.white;
    }

    /// <summary>
    /// 简化版底部的 DOT 子弹小图标条
    /// </summary>
    private static void DrawDotGunIcons(ICharacterPassive passive, float x, float y, float maxW,
        Texture2D[] dotTextures, GUIStyle smallStyle)
    {
        var dotPassive = passive as IDotCharacterPassive;
        if (dotPassive == null) return;
        var guns = dotPassive.DotGuns;
        if (guns == null || guns.Count == 0) return;

        float iconSize = 14f;
        float curX = x;

        for (int i = 0; i < guns.Count; i++)
        {
            var gun = guns[i];
            if (curX + iconSize + 60 > x + maxW) break;

            Texture2D tex = GetDotTextureFromArray(dotTextures, gun.effectType);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(curX, y, iconSize, iconSize), tex);

            // 攻速显示（每秒攻击次数 = 1/cooldown）
            float atkSpeed = gun.cooldown > 0f ? 1f / gun.cooldown : 0f;
            GUI.color = TextSecondary;
            GUI.Label(new Rect(curX + iconSize + 2, y - 1, 45, iconSize), $"Lv{gun.upgradeLevel} {atkSpeed:F1}/s", smallStyle);

            curX += iconSize + 52;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // 完整面板（暂停时）
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 绘制暂停时的完整统计面板
    /// </summary>
    public static void DrawFullPanel(ICharacterPassive passive, float marginLeft, float marginBottom,
        float panelWidth, float panelPadding, float lineHeight, float headerHeight,
        Texture2D panelBgTex, Texture2D headerBgTex, Texture2D dividerTex,
        Texture2D progressBgTex, Texture2D progressFillTex,
        Texture2D milestoneBgTex, Texture2D milestoneFillTex,
        Texture2D[] dotTextures,
        GUIStyle headerStyle, GUIStyle labelStyle, GUIStyle valueStyle,
        GUIStyle smallStyle, GUIStyle titleStyle, GUIStyle milestoneLabelStyle)
    {
        float x = marginLeft;
        var dotPassive = passive as IDotCharacterPassive;
        float baseY = Screen.height - marginBottom;

        // 计算面板高度（动态）
        var guns = dotPassive?.DotGuns;
        int dotGunCount = guns != null ? guns.Count : 0;
        int lineCount = 6 + dotGunCount + 5;
        float panelH = panelPadding * 2 + headerHeight + lineCount * lineHeight + 20;
        float panelW = panelWidth;

        float y = baseY - panelH;

        // 面板背景
        GUI.color = PanelBg;
        GUI.DrawTexture(new Rect(x, y, panelW, panelH), panelBgTex);

        float cy = y + panelPadding;

        // ── 标题栏 ──
        GUI.color = HeaderBg;
        GUI.DrawTexture(new Rect(x, y, panelW, headerHeight + panelPadding), headerBgTex);
        GUI.color = AccentPurple;
        GUI.Label(new Rect(x, cy, panelW, headerHeight), "⚔ MAGE STATS ⚔", headerStyle);
        cy += headerHeight + 6;

        // ── 已解锁 DOT 子弹列表 ──
        cy = DrawSectionHeader(x, cy, panelW, "DOT GUNS", headerHeight, titleStyle);
        if (guns != null)
        {
            for (int i = 0; i < guns.Count; i++)
            {
                cy = DrawDotGunRow(x + 8, cy, panelW - 16, guns[i], passive,
                    lineHeight, dotTextures, labelStyle, valueStyle, smallStyle);
            }
        }
        if (guns == null || guns.Count == 0)
        {
            GUI.color = TextSecondary;
            GUI.Label(new Rect(x + 12, cy, panelW, lineHeight), "  (none)", smallStyle);
            cy += lineHeight;
        }

        cy = DrawDivider(x, cy, panelW, lineHeight, dividerTex);

        // ── 总览统计 ──
        cy = DrawSectionHeader(x, cy, panelW, "OVERVIEW", headerHeight, titleStyle);
        float totalDps = MageStatsDataCollector.CalculateCurrentDPS(passive);
        cy = DrawStatRow(x + 8, cy, panelW - 16, "Total DPS", $"{totalDps:F1}", AccentGold, lineHeight, labelStyle, valueStyle);
        cy = DrawStatRow(x + 8, cy, panelW - 16, "Crit Chance", $"{(dotPassive?.GetDotCritChance() ?? 0f) * 100:F1}%",
            Color.Lerp(TextPrimary, Color.red, 0.3f), lineHeight, labelStyle, valueStyle);
        cy = DrawStatRow(x + 8, cy, panelW - 16, "Crit Multiplier", $"{dotPassive?.GetDotCritMultiplier() ?? 1f:F1}x", TextPrimary, lineHeight, labelStyle, valueStyle);
        cy = DrawStatRow(x + 8, cy, panelW - 16, "DOT Duration +", $"{((dotPassive?.GetDotDurationMultiplier() ?? 1f) - 1f) * 100:F0}%", TextPrimary, lineHeight, labelStyle, valueStyle);

        cy = DrawDivider(x, cy, panelW, lineHeight, dividerTex);

        // ── 引爆参数（仅 Mage 角色） ──
        if (passive is MagePassive mage)
        {
            cy = DrawSectionHeader(x, cy, panelW, "DETONATE [E]", headerHeight, titleStyle);
            cy = DrawStatRow(x + 8, cy, panelW - 16, "Damage", $"x{mage.DetonateMultiplier:F1}", AccentPurple, lineHeight, labelStyle, valueStyle);
            cy = DrawStatRow(x + 8, cy, panelW - 16, "Cooldown", $"{mage.DetonateCooldown:F1}s", TextPrimary, lineHeight, labelStyle, valueStyle);
            float cdRemain = mage.DetonateCooldownRemaining;
            bool ready = mage.DetonateReady;
            cy = DrawStatRow(x + 8, cy, panelW - 16, "Status",
                ready ? "READY" : $"{cdRemain:F1}s",
                ready ? AccentGold : AccentPurple, lineHeight, labelStyle, valueStyle);

            cy = DrawDivider(x, cy, panelW, lineHeight, dividerTex);

            // ── DOT 增强属性 ──
            cy = DrawSectionHeader(x, cy, panelW, "DOT ENHANCEMENTS", headerHeight, titleStyle);
            cy = DrawStatRow(x + 8, cy, panelW - 16, "Corrosion", $"{mage.CorrosionArmorReduction * 100:F0}%/stack", TextPrimary, lineHeight, labelStyle, valueStyle);
            cy = DrawStatRow(x + 8, cy, panelW - 16, "Curse Spread", $"{mage.CurseSpreadTargets} targets", TextPrimary, lineHeight, labelStyle, valueStyle);

            // ── 子弹增强属性 ──
            cy = DrawStatRow(x + 8, cy, panelW - 16, "Attack Speed +", $"{mage.AttackSpeedBonus * 100:F0}%", TextPrimary, lineHeight, labelStyle, valueStyle);
            cy = DrawStatRow(x + 8, cy, panelW - 16, "Bullet Count", $"{1 + mage.BulletCountBonus}", TextPrimary, lineHeight, labelStyle, valueStyle);

            cy = DrawDivider(x, cy, panelW, lineHeight, dividerTex);

            // ── 里程碑进度 ──
            cy = DrawSectionHeader(x, cy, panelW, "MILESTONES", headerHeight, titleStyle);
            cy = DrawMilestone(x + 8, cy, panelW - 16,
                "★ Element Master",
                "All 4 DOT types unlocked → +20% DOT damage",
                guns != null ? guns.Count : 0, 4,
                mage.GetDotDamageMultiplier() > 1f,
                lineHeight, progressBgTex, progressFillTex, milestoneBgTex, milestoneFillTex, milestoneLabelStyle, smallStyle);

            cy = DrawMilestone(x + 8, cy, panelW - 16,
                "★ Chain Detonate",
                "Detonate hits 10+ enemies → x2 DOT for 3s",
                0, 1,
                mage.IsChainDetonateActive,
                lineHeight, progressBgTex, progressFillTex, milestoneBgTex, milestoneFillTex, milestoneLabelStyle, smallStyle);

            if (mage.IsChainDetonateActive)
            {
                GUI.color = AccentGold;
                GUI.Label(new Rect(x + 20, cy, panelW, lineHeight),
                    $"  Active: x2 DOT damage", smallStyle);
                cy += lineHeight;
            }
        }

        // ── 边框 ──
        GUI.color = DividerColor;
        DrawBorder(x, y, panelW, panelH, dividerTex);

        GUI.color = Color.white;
    }

    // ════════════════════════════════════════════════════════════════
    // 绘制辅助方法
    // ════════════════════════════════════════════════════════════════

    private static float DrawSectionHeader(float x, float y, float w, string title,
        float headerHeight, GUIStyle titleStyle)
    {
        GUI.color = AccentPurple;
        GUI.Label(new Rect(x + 4, y, w, headerHeight - 4), title, titleStyle);
        return y + headerHeight;
    }

    private static float DrawDotGunRow(float x, float y, float w, DotGunState gun,
        ICharacterPassive passive, float lineHeight, Texture2D[] dotTextures,
        GUIStyle labelStyle, GUIStyle valueStyle, GUIStyle smallStyle)
    {
        float iconSize = 16f;

        Texture2D tex = GetDotTextureFromArray(dotTextures, gun.effectType);
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(x, y + 2, iconSize, iconSize), tex);

        string typeName = MageStatsDataCollector.GetDotTypeName(gun.effectType);
        GUI.color = MageStatsDataCollector.GetDotColor(gun.effectType);
        GUI.Label(new Rect(x + iconSize + 4, y, 100, lineHeight), typeName, labelStyle);

        GUI.color = TextPrimary;
        GUI.Label(new Rect(x + 110, y, 60, lineHeight), $"DPS:{gun.dotDps:F1}", valueStyle);

        GUI.color = AccentGold;
        GUI.Label(new Rect(x + 175, y, 50, lineHeight), $"Lv{gun.upgradeLevel}", valueStyle);

        GUI.color = TextSecondary;
        float effectiveCd = gun.cooldown * passive.GetAttackSpeedMultiplier();
        GUI.Label(new Rect(x + 225, y, 60, lineHeight), $"CD:{effectiveCd:F2}s", valueStyle);

        return y + lineHeight;
    }

    private static float DrawStatRow(float x, float y, float w, string label, string value,
        Color valueColor, float lineHeight, GUIStyle labelStyle, GUIStyle valueStyle)
    {
        GUI.color = TextSecondary;
        GUI.Label(new Rect(x, y, w * 0.55f, lineHeight), label, labelStyle);

        GUI.color = valueColor;
        GUI.Label(new Rect(x + w * 0.55f, y, w * 0.45f, lineHeight), value, valueStyle);

        return y + lineHeight;
    }

    private static float DrawDivider(float x, float y, float w, float lineHeight, Texture2D dividerTex)
    {
        GUI.color = DividerColor;
        GUI.DrawTexture(new Rect(x + 8, y + lineHeight / 2 - 1, w - 16, 1), dividerTex);
        return y + lineHeight;
    }

    private static float DrawMilestone(float x, float y, float w, string title, string desc,
        int current, int target, bool completed, float lineHeight,
        Texture2D progressBgTex, Texture2D progressFillTex,
        Texture2D milestoneBgTex, Texture2D milestoneFillTex,
        GUIStyle milestoneLabelStyle, GUIStyle smallStyle)
    {
        GUI.color = completed ? MilestoneComplete : MilestoneIncomplete;
        GUI.DrawTexture(new Rect(x, y + 2, 12, 12), completed ? milestoneFillTex : milestoneBgTex);

        GUI.color = completed ? MilestoneComplete : TextPrimary;
        GUI.Label(new Rect(x + 16, y, w, lineHeight), title, milestoneLabelStyle);

        float barX = x + 16;
        float barY = y + lineHeight - 2;
        float barW = w - 32;
        float barH = 8f;

        GUI.color = ProgressBg;
        GUI.DrawTexture(new Rect(barX, barY, barW, barH), progressBgTex);

        float pct = Mathf.Clamp01((float)current / Mathf.Max(1, target));
        GUI.color = completed ? MilestoneComplete : ProgressFill;
        GUI.DrawTexture(new Rect(barX, barY, barW * pct, barH), completed ? milestoneFillTex : progressFillTex);

        GUI.color = TextSecondary;
        GUI.Label(new Rect(x + 16, y + lineHeight + 4, w, lineHeight), desc, smallStyle);

        return y + lineHeight * 2 + 6;
    }

    private static void DrawBorder(float x, float y, float w, float h, Texture2D dividerTex)
    {
        GUI.DrawTexture(new Rect(x, y, w, 1), dividerTex);
        GUI.DrawTexture(new Rect(x, y + h - 1, w, 1), dividerTex);
        GUI.DrawTexture(new Rect(x, y, 1, h), dividerTex);
        GUI.DrawTexture(new Rect(x + w - 1, y, 1, h), dividerTex);
    }

    // ════════════════════════════════════════════════════════════════
    // 辅助方法
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 根据 DOT 类型获取对应纹理（从纹理数组）
    /// 数组顺序：[0]=Bleed, [1]=Poison, [2]=Burn, [3]=Frost
    /// </summary>
    private static Texture2D GetDotTextureFromArray(Texture2D[] textures, StatusEffectType type)
    {
        if (textures == null || textures.Length < 4) return null;
        switch (type)
        {
            case StatusEffectType.Bleed: return textures[0];
            case StatusEffectType.Poison: return textures[1];
            case StatusEffectType.Burn: return textures[2];
            case StatusEffectType.Frostbite: return textures[3];
            default: return textures[1];
        }
    }
}