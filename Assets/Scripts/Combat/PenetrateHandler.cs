using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 穿透处理器 — 挂在 DOT 子弹上，命中敌人后可继续穿透
/// </summary>
public class PenetrateHandler : MonoBehaviour
{
    private int _remaining;
    private HashSet<Collider2D> _hitEnemies = new HashSet<Collider2D>();

    public void Setup(int penetrateCount)
    {
        _remaining = penetrateCount;
        _hitEnemies.Clear();
    }

    public bool TryPenetrate(Collider2D hitEnemy)
    {
        if (_hitEnemies.Contains(hitEnemy)) return false;
        _hitEnemies.Add(hitEnemy);
        if (_remaining <= 0) return false;
        _remaining--;
        return true;
    }
}
