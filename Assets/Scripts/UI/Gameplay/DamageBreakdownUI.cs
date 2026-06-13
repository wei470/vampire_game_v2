using UnityEngine;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// 伤害统计面板 UI 渲染工具。
/// 从 DamageMeter 中提取 OnGUI 绘制逻辑和格式化方法，减少主类代码量。
/// </summary>
public static class DamageBreakdownUI
{
    private static GUIStyle _titleStyle;
    private static GUIStyle _timeStyle;
    private static GUIStyle _labelStyle;
    private static GUIStyle _totalStyle;
    private static bool _stylesInit;

    private static readonly List<KeyValuePair<DamageMeter.DamageSource, DamageMeter.SourceStats>> _tempSorted
        = new List<KeyValuePair<DamageMeter.DamageSource, DamageMeter.SourceStats>>(16);

    private static void EnsureStyles()
    {
        if (_stylesInit) return;
        _stylesInit = true;

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal = { textColor = UIColorTheme.AccentCyan },
            alignment = TextAnchor.MiddleCenter
        };

        _timeStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = UIColorTheme.TextSecondary },
            alignment = TextAnchor.MiddleCenter
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        _totalStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = UIColorTheme.GoldText },
            alignment = TextAnchor.MiddleCenter
        };
    }

    /// <summary>
    /// 绘制伤害统计 IMGUI 面板
    /// </summary>
    public static void DrawPanel(Dictionary<DamageMeter.DamageSource, DamageMeter.SourceStats> stats,
                                  float combatStartTime, bool isTracking, long totalDamage)
    {
        if (stats.Count == 0) return;
        EnsureStyles();

        float panelW = 320f;
        float panelH = 400f;
        float margin = 20f;
        float panelX = Screen.width - panelW - margin;
        float panelY = margin;

        // 背景
        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(new Rect(panelX, panelY, panelW, panelH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(panelX, panelY + 5, panelW, 30), "⚔ DAMAGE METER", _titleStyle);

        // 统计时间
        float elapsed = isTracking ? (Time.time - combatStartTime) : 0f;
        GUI.Label(new Rect(panelX, panelY + 30, panelW, 20), $"Combat Time: {FormatTime(elapsed)}", _timeStyle);

        // 各来源统计
        float y = panelY + 55f;
        float totalDps = elapsed > 0 ? totalDamage / elapsed : 0f;

        // 按总伤害排序
        _tempSorted.Clear();
        foreach (var kvp in stats)
            _tempSorted.Add(kvp);
        _tempSorted.Sort((a, b) => b.Value.totalDamage.CompareTo(a.Value.totalDamage));

        foreach (var kvp in _tempSorted)
        {
            var source = kvp.Key;
            var s = kvp.Value;
            float percent = totalDamage > 0 ? (float)s.totalDamage / totalDamage * 100f : 0f;
            float dps = elapsed > 0 ? s.totalDamage / elapsed : 0f;

            var sourceName = GetSourceName(source);
            var sourceColor = GetSourceColor(source);

            // 进度条背景
            float barW = panelW - 20f;
            float barH = 18f;
            float barX = panelX + 10f;

            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);
            GUI.DrawTexture(new Rect(barX, y, barW, barH), Texture2D.whiteTexture);

            // 进度条填充
            float fillW = barW * (percent / 100f);
            GUI.color = new Color(sourceColor.r, sourceColor.g, sourceColor.b, 0.7f);
            GUI.DrawTexture(new Rect(barX, y, fillW, barH), Texture2D.whiteTexture);

            // 文字
            GUI.color = sourceColor;
            GUI.Label(new Rect(barX + 5f, y, barW - 10f, barH),
                $"{sourceName}: {FormatDamage(s.totalDamage)} ({percent:F1}%) | {FormatDamage((long)dps)}/s",
                _labelStyle);

            y += barH + 4f;
        }

        // 底部总统计
        y += 10f;
        GUI.Label(new Rect(panelX, y, panelW, 25), $"Total: {FormatDamage(totalDamage)} | {FormatDamage((long)totalDps)}/s", _totalStyle);

        GUI.color = Color.white;
    }

    /// <summary>
    /// 获取伤害来源显示名称
    /// </summary>
    public static string GetSourceName(DamageMeter.DamageSource source)
    {
        return source switch
        {
            DamageMeter.DamageSource.Bleed => "🔴 Bleed",
            DamageMeter.DamageSource.Poison => "🟢 Poison",
            DamageMeter.DamageSource.Burn => "🟠 Burn",
            DamageMeter.DamageSource.Frostbite => "🔵 Frost",
            DamageMeter.DamageSource.Detonate => "💥 Detonate",
            DamageMeter.DamageSource.Bullet => "⚡ Bullet",
            DamageMeter.DamageSource.Skill => "✦ Skill",
            _ => "? Other"
        };
    }

    /// <summary>
    /// 获取伤害来源颜色
    /// </summary>
    public static Color GetSourceColor(DamageMeter.DamageSource source)
    {
        return source switch
        {
            DamageMeter.DamageSource.Bleed => new Color(0.9f, 0.1f, 0.1f),
            DamageMeter.DamageSource.Poison => new Color(0.1f, 0.9f, 0.2f),
            DamageMeter.DamageSource.Burn => new Color(1f, 0.5f, 0f),
            DamageMeter.DamageSource.Frostbite => new Color(0.3f, 0.6f, 1f),
            DamageMeter.DamageSource.Detonate => new Color(1f, 0.3f, 0.8f),
            DamageMeter.DamageSource.Bullet => new Color(0.3f, 0.8f, 1f),
            DamageMeter.DamageSource.Skill => new Color(1f, 1f, 0.3f),
            _ => Color.gray
        };
    }

    /// <summary>
    /// 格式化伤害数字（1K/1M）
    /// </summary>
    public static string FormatDamage(long dmg)
    {
        if (dmg >= 1_000_000) return $"{dmg / 1_000_000f:F1}M";
        if (dmg >= 1_000) return $"{dmg / 1_000f:F1}K";
        return dmg.ToString();
    }

    /// <summary>
    /// 格式化时间（MM:SS）
    /// </summary>
    public static string FormatTime(float seconds)
    {
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{mins:00}:{secs:00}";
    }
}