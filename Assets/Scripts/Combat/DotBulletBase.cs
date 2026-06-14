using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DOT 子弹基类 — 继承 ProjectileBase，添加 DOT 专属逻辑。
///
/// 复用 ProjectileBase 的速度/碰撞/穿透/反弹逻辑，
/// 子类只需实现 OnHitEnemy() 来定义各自的 DOT 效果。
/// </summary>
public abstract class DotBulletBase : ProjectileBase
{
    public static readonly List<MonoBehaviour> ActiveDotBullets = new List<MonoBehaviour>();

    /// <summary>子弹 DOT 类型（用于日志/调试）</summary>
    protected abstract StatusEffectType EffectType { get; }

    protected override void OnEnable()
    {
        base.OnEnable();
        ActiveDotBullets.Add(this);
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        var dmg = other.GetComponent<Damageable>();
        if (dmg != null && dmg.CurrentHp > 0)
        {
            OnHitEnemy(other.gameObject);
            OnHitExtra(other);
            DotBulletHelper.EnsureStatusEffectManager(other.gameObject);
        }
        if (_cachedPenetrate != null && _cachedPenetrate.TryPenetrate(other)) return;
        if (_cachedPenetrate == null)
        {
            _cachedPenetrate = GetComponent<PenetrateHandler>();
            if (_cachedPenetrate != null && _cachedPenetrate.TryPenetrate(other)) return;
        }
        var ricochet = GetComponent<RicochetHandler>();
        if (ricochet != null && ricochet.TryRicochet(transform.position, other)) return;
        DespawnSelf();
    }

    protected override void OnBulletDespawn()
    {
        ActiveDotBullets.Remove(this);
    }
}
