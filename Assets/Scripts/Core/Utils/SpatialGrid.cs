using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 简单网格空间分区系统 — 加速范围查询
/// 将世界空间划分为固定大小的网格，每个格子维护一个敌人列表。
/// 范围查询时只检查相邻格子，避免遍历全部敌人。
/// 
/// 使用方式：
///   1. 每帧调用 Rebuild(enemies) 重建网格（敌人移动时更新）
///   2. 调用 QueryRadius(center, radius) 获取范围内的敌人
/// 
/// 线程安全：非线程安全，仅在主线程使用
/// </summary>
public static class SpatialGrid
{
    // ── 配置 ──
    /// <summary>每个格子的世界尺寸（单位：Unity世界坐标）</summary>
    private const float CELL_SIZE = 10f;
    /// <summary>网格的半边长（格子数量），覆盖 -200 到 +200 的世界范围</summary>
    private const int GRID_HALF = 20;
    /// <summary>网格总边长 = GRID_HALF * 2</summary>
    private const int GRID_SIZE = GRID_HALF * 2;

    // ── 数据存储 ──
    // 使用一维数组模拟二维网格，索引 = (gy + GRID_HALF) * GRID_SIZE + (gx + GRID_HALF)
    private static readonly List<GameObject>[] _cells = new List<GameObject>[GRID_SIZE * GRID_SIZE];
    private static readonly List<GameObject> _queryResults = new List<GameObject>(64);

    // ── 初始化标记 ──
    private static bool _initialized = false;

    /// <summary>初始化所有格子列表（懒加载）</summary>
    private static void EnsureInitialized()
    {
        if (_initialized) return;
        for (int i = 0; i < _cells.Length; i++)
            _cells[i] = new List<GameObject>(8);
        _initialized = true;
    }

    /// <summary>
    /// 清空所有格子，准备下一次重建
    /// </summary>
    public static void Clear()
    {
        EnsureInitialized();
        for (int i = 0; i < _cells.Length; i++)
            _cells[i].Clear();
    }

    /// <summary>
    /// 重建空间分区网格（每帧调用一次）
    /// </summary>
    /// <param name="enemies">当前活跃敌人列表</param>
    public static void Rebuild(IReadOnlyList<GameObject> enemies)
    {
        EnsureInitialized();
        Clear();

        if (enemies == null) return;

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || !enemy.activeInHierarchy) continue;

            Vector2 pos = enemy.transform.position;
            int idx = WorldToCellIndex(pos);
            if (idx >= 0 && idx < _cells.Length)
                _cells[idx].Add(enemy);
        }
    }

    /// <summary>
    /// 查询以 center 为圆心、radius 为半径的范围内的所有敌人
    /// 结果存储在内部缓存中，调用方应立即使用或复制
    /// </summary>
    /// <param name="center">圆心位置</param>
    /// <param name="radius">半径</param>
    /// <returns>范围内的敌人列表（内部缓存，下次查询会清空）</returns>
    public static List<GameObject> QueryRadius(Vector2 center, float radius)
    {
        EnsureInitialized();
        _queryResults.Clear();

        float radiusSqr = radius * radius;
        float expandedRadius = radius + CELL_SIZE; // 扩展一个格子宽度确保边界敌人不遗漏

        // 计算需要检查的格子范围
        int minX = Mathf.FloorToInt((center.x - expandedRadius) / CELL_SIZE);
        int maxX = Mathf.FloorToInt((center.x + expandedRadius) / CELL_SIZE);
        int minY = Mathf.FloorToInt((center.y - expandedRadius) / CELL_SIZE);
        int maxY = Mathf.FloorToInt((center.y + expandedRadius) / CELL_SIZE);

        // 限制在网格范围内
        minX = Mathf.Max(minX, -GRID_HALF);
        maxX = Mathf.Min(maxX, GRID_HALF - 1);
        minY = Mathf.Max(minY, -GRID_HALF);
        maxY = Mathf.Min(maxY, GRID_HALF - 1);

        for (int gy = minY; gy <= maxY; gy++)
        {
            for (int gx = minX; gx <= maxX; gx++)
            {
                int idx = (gy + GRID_HALF) * GRID_SIZE + (gx + GRID_HALF);
                var cell = _cells[idx];
                for (int i = 0; i < cell.Count; i++)
                {
                    var enemy = cell[i];
                    if (enemy == null || !enemy.activeInHierarchy) continue;
                    Vector2 delta = (Vector2)enemy.transform.position - center;
                    if (delta.sqrMagnitude <= radiusSqr)
                        _queryResults.Add(enemy);
                }
            }
        }

        return _queryResults;
    }

    /// <summary>
    /// 查询以 center 为圆心、radiusSqr 为半径平方的范围内的所有敌人（避免开方）
    /// </summary>
    public static List<GameObject> QueryRadiusSqr(Vector2 center, float radiusSqr)
    {
        return QueryRadius(center, Mathf.Sqrt(radiusSqr));
    }

    /// <summary>
    /// 将世界坐标转换为格子索引
    /// </summary>
    private static int WorldToCellIndex(Vector2 worldPos)
    {
        int gx = Mathf.FloorToInt(worldPos.x / CELL_SIZE);
        int gy = Mathf.FloorToInt(worldPos.y / CELL_SIZE);

        // 超出网格范围的敌人放在边界格子
        gx = Mathf.Clamp(gx, -GRID_HALF, GRID_HALF - 1);
        gy = Mathf.Clamp(gy, -GRID_HALF, GRID_HALF - 1);

        return (gy + GRID_HALF) * GRID_SIZE + (gx + GRID_HALF);
    }

    /// <summary>
    /// 获取统计信息（用于 Debug）
    /// </summary>
    public static void GetStats(out int totalEnemies, out int usedCells)
    {
        EnsureInitialized();
        totalEnemies = 0;
        usedCells = 0;
        for (int i = 0; i < _cells.Length; i++)
        {
            if (_cells[i].Count > 0)
            {
                usedCells++;
                totalEnemies += _cells[i].Count;
            }
        }
    }
}