using UnityEngine;

/// <summary>
/// 墙壁反弹组件 — 挂载在子弹上，碰到屏幕边缘时反弹方向。
/// 支持最大反弹次数，耗尽后子弹正常飞行。
/// </summary>
public class WallBounceHandler : MonoBehaviour
{
    private int _remainingBounces;
    private Vector2 _lastPos;
    private ProjectileBase _projectile;

    public void Setup(int maxBounces, float lifetimeBonus = 0f)
    {
        _remainingBounces = maxBounces;
        if (lifetimeBonus > 0f)
        {
            var proj = GetComponent<ProjectileBase>();
            if (proj != null) proj.AddLifetime(lifetimeBonus);
        }
    }

    private void OnEnable()
    {
        _projectile = GetComponent<ProjectileBase>();
        _lastPos = transform.position;
    }

    private void Update()
    {
        if (_remainingBounces <= 0) return;

        var cam = Camera.main;
        if (cam == null) return;

        float h = cam.orthographicSize;
        float w = h * cam.aspect;
        Vector3 cp = cam.transform.position;
        Vector2 pos = transform.position;

        float minX = cp.x - w;
        float maxX = cp.x + w;
        float minY = cp.y - h;
        float maxY = cp.y + h;

        bool bounced = false;
        Vector2 dir = _projectile != null ? _projectile.GetDirection() : Vector2.zero;
        if (dir.sqrMagnitude < 0.001f) return;

        if (pos.x <= minX && dir.x < 0f) { dir.x = -dir.x; bounced = true; }
        else if (pos.x >= maxX && dir.x > 0f) { dir.x = -dir.x; bounced = true; }

        if (pos.y <= minY && dir.y < 0f) { dir.y = -dir.y; bounced = true; }
        else if (pos.y >= maxY && dir.y > 0f) { dir.y = -dir.y; bounced = true; }

        if (bounced)
        {
            _remainingBounces--;
            _projectile.SetDirection(dir.normalized);
        }
    }
}
