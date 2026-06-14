using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DOT 子弹基类 — 继承 ProjectileBase，添加 DOT 专属逻辑。
///
/// 复用 ProjectileBase 的速度/碰撞/穿透/反弹逻辑，
/// 子类只需实现 OnHitEnemy() 来定义各自的 DOT 效果。
/// 池回收时自动重置 SpriteRenderer 和 TrailRenderer 颜色。
/// </summary>
public abstract class DotBulletBase : ProjectileBase
{
    public static readonly List<MonoBehaviour> ActiveDotBullets = new List<MonoBehaviour>();

    /// <summary>子弹 DOT 类型（用于日志/调试）</summary>
    protected abstract StatusEffectType EffectType { get; }

    /// <summary>子弹默认颜色，子类必须提供以支持池回收颜色重置</summary>
    protected abstract Color DefaultBulletColor { get; }

    /// <summary>拖尾默认颜色（可选重写）</summary>
    protected virtual Color DefaultTrailStartColor => DefaultBulletColor;
    protected virtual Color DefaultTrailEndColor => new Color(DefaultBulletColor.r, DefaultBulletColor.g, DefaultBulletColor.b, 0f);

    protected override void OnEnable()
    {
        base.OnEnable();
        ActiveDotBullets.Add(this);

        // 池回收时重置颜色，防止旧子弹颜色残留
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = DefaultBulletColor;

        var trail = GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.startColor = DefaultTrailStartColor;
            trail.endColor = DefaultTrailEndColor;
            trail.Clear();
        }
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
