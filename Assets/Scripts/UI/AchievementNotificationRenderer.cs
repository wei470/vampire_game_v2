using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 成就通知渲染器 — 从 AchievementUI 拆分而来
/// 负责成就解锁通知的队列管理和绘制
/// </summary>
public static class AchievementNotificationRenderer
{
    private const float NOTIF_DISPLAY_TIME = 4f;
    private const float NOTIF_SLIDE_IN_TIME = 0.3f;
    private const float NOTIF_SLIDE_OUT_TIME = 0.5f;
    private const float NOTIF_WIDTH = 380f;
    private const float NOTIF_HEIGHT = 80f;

    /// <summary>
    /// 更新通知队列，返回是否有当前通知
    /// </summary>
    public static bool UpdateNotificationQueue(Queue<NotificationEntry> queue, ref NotificationEntry? current)
    {
        float now = Time.unscaledTime;

        if (current.HasValue)
        {
            var n = current.Value;
            float elapsed = now - n.showTime;
            float totalDuration = NOTIF_SLIDE_IN_TIME + NOTIF_DISPLAY_TIME + NOTIF_SLIDE_OUT_TIME;
            if (elapsed >= totalDuration)
            {
                current = null;
            }
            return current.HasValue;
        }

        if (queue.Count > 0)
        {
            var next = queue.Dequeue();
            next.showTime = now;
            current = next;
        }

        return current.HasValue;
    }

    /// <summary>
    /// 绘制成就通知卡片
    /// </summary>
    public static void DrawNotificationCard(NotificationEntry n, Texture2D notifBgTex, Texture2D notifBorderTex)
    {
        float elapsed = Time.unscaledTime - n.showTime;

        float slideX;
        float alpha;
        if (elapsed < NOTIF_SLIDE_IN_TIME)
        {
            float t = elapsed / NOTIF_SLIDE_IN_TIME;
            float eased = 1f - (1f - t) * (1f - t);
            slideX = (1f - eased) * NOTIF_WIDTH;
            alpha = eased;
        }
        else if (elapsed > NOTIF_SLIDE_IN_TIME + NOTIF_DISPLAY_TIME)
        {
            float outElapsed = elapsed - NOTIF_SLIDE_IN_TIME - NOTIF_DISPLAY_TIME;
            float t = outElapsed / NOTIF_SLIDE_OUT_TIME;
            float eased = t * t;
            slideX = 0f;
            alpha = 1f - eased;
        }
        else
        {
            slideX = 0f;
            alpha = 1f;
        }

        if (alpha <= 0.01f) return;

        float x = Screen.width - NOTIF_WIDTH - 15f + slideX;
        float y = 80f;

        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.DrawTexture(new Rect(x, y, NOTIF_WIDTH, NOTIF_HEIGHT), notifBgTex);

        DrawNotifBorder(new Rect(x, y, NOTIF_WIDTH, NOTIF_HEIGHT), alpha, notifBorderTex);

        GUI.color = new Color(1f, 0.85f, 0.2f, 0.9f * alpha);
        GUI.DrawTexture(new Rect(x, y, 4f, NOTIF_HEIGHT), notifBorderTex);

        var iconStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.85f, 0.2f, alpha) }
        };
        GUI.Label(new Rect(x + 10, y + 10, 40, 40), "🏆", iconStyle);

        var nameStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.95f, 0.6f, alpha) }
        };
        GUI.Label(new Rect(x + 55, y + 8, NOTIF_WIDTH - 70, 24), n.name, nameStyle);

        var descStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = new Color(0.7f, 0.8f, 0.9f, alpha) }
        };
        GUI.Label(new Rect(x + 55, y + 32, NOTIF_WIDTH - 70, 20), n.description, descStyle);

        if (!string.IsNullOrEmpty(n.bonusText))
        {
            var bonusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.3f, 1f, 0.5f, alpha) }
            };
            GUI.Label(new Rect(x + 55, y + 52, NOTIF_WIDTH - 70, 18), $"✨ {n.bonusText}", bonusStyle);
        }

        GUI.color = Color.white;
    }

    private static void DrawNotifBorder(Rect r, float alpha, Texture2D notifBorderTex)
    {
        GUI.color = new Color(1f, 0.85f, 0.2f, 0.4f * alpha);
        float t = 1f;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), notifBorderTex);
        GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), notifBorderTex);
        GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), notifBorderTex);
        GUI.color = Color.white;
    }

    /// <summary>
    /// 格式化永久加成描述文本
    /// </summary>
    public static string FormatBonusText(string bonusKey, float bonusValue)
    {
        if (string.IsNullOrEmpty(bonusKey) || bonusValue <= 0f) return "";

        switch (bonusKey)
        {
            case "crit_chance": return $"暴击率 +{bonusValue * 100:F0}%，永久生效";
            case "max_hp_bonus": return $"最大生命 +{bonusValue:F0}，永久生效";
            case "damage_bonus": return $"伤害 +{bonusValue * 100:F0}%，永久生效";
            case "dot_damage_bonus": return $"DOT伤害 +{bonusValue * 100:F0}%，永久生效";
            case "detonate_bonus": return $"引爆伤害 +{bonusValue * 100:F0}%，永久生效";
            case "coin_bonus": return $"金币获取 +{bonusValue * 100:F0}%，永久生效";
            default: return $"Bonus: +{bonusValue}，永久生效";
        }
    }
}

/// <summary>
/// 通知条目数据结构
/// </summary>
public struct NotificationEntry
{
    public string name;
    public string description;
    public string bonusText;
    public float showTime;
}