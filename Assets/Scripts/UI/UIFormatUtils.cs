using UnityEngine;

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
