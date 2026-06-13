using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 环境区域生成器 — 在地图上随机生成环境区域。
/// </summary>
public static class EnvironmentZoneSpawner
{
    private static readonly List<GameObject> _activeZones = new List<GameObject>();

    /// <summary>
    /// 在玩家周围生成环境区域（波次开始时调用）
    /// </summary>
    public static void SpawnZones(int wave, int count = 2)
    {
        if (wave < 5) return;

        var player = GameReferences.Player;
        if (player == null) return;

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(8f, 20f);
            Vector2 pos = (Vector2)player.transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

            var zone = EnvironmentZone.CreateDefault(pos, MapThemeData.EnvironmentZoneType.Damage, 4f, 0f);
            _activeZones.Add(zone.gameObject);
        }
    }

    public static void ClearAll()
    {
        foreach (var zone in _activeZones)
            if (zone != null) Object.Destroy(zone);
        _activeZones.Clear();
    }
}
