using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Physics2D 零分配查询工具 — 避免 OverlapCircleAll 每次分配 Collider2D[] 数组。
///
/// 使用方式：
///   int count = PhysicsHelper.OverlapCircle(center, radius, _buffer);
///   for (int i = 0; i < count; i++) { var col = _buffer[i]; ... }
/// </summary>
public static class PhysicsHelper
{
    private static ContactFilter2D _defaultFilter;
    private static bool _filterInitialized = false;

    public static int OverlapCircle(Vector2 center, float radius, List<Collider2D> buffer)
    {
        if (!_filterInitialized)
        {
            _defaultFilter = new ContactFilter2D();
            _defaultFilter.useTriggers = true;
            _defaultFilter.useLayerMask = false;
            _filterInitialized = true;
        }
        return Physics2D.OverlapCircle(center, radius, _defaultFilter, buffer);
    }

    /// <summary>
    /// 零分配 OverlapCircle — 指定 LayerMask
    /// </summary>
    public static int OverlapCircle(Vector2 center, float radius, List<Collider2D> buffer, LayerMask mask)
    {
        var filter = new ContactFilter2D();
        filter.SetLayerMask(mask);
        filter.useTriggers = true;
        return Physics2D.OverlapCircle(center, radius, filter, buffer);
    }
}
