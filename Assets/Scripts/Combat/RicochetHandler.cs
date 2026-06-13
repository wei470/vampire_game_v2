using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 反弹处理器 — 挂在 DOT 子弹上，命中敌人后有概率弹射到最近的另一个敌人
/// </summary>
public class RicochetHandler : MonoBehaviour
{
    private float _chance;
    private int _maxBounces;
    private int _bounces;
    private float _damageDecay = 0.8f;

    private static readonly List<Collider2D> _overlapBuffer = new List<Collider2D>(16);

    public void Setup(float chance, int extraMaxBounces)
    {
        _chance = chance;
        _maxBounces = Mathf.FloorToInt(chance) + extraMaxBounces;
        _bounces = 0;
    }

    public bool TryRicochet(Vector2 currentPosition, Collider2D hitEnemy)
    {
        if (_bounces >= _maxBounces) return false;

        int guaranteed = Mathf.FloorToInt(_chance);
        float extra = _chance - guaranteed;
        if (_bounces >= guaranteed && Random.value >= extra) return false;

        _overlapBuffer.Clear();
        int count = PhysicsHelper.OverlapCircle(currentPosition, 20f, _overlapBuffer);
        Transform nearest = null;
        float nearestDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var col = _overlapBuffer[i];
            if (!col.CompareTag("Enemy") || col == hitEnemy) continue;
            var d = col.GetComponent<Damageable>();
            if (d == null || d.CurrentHp <= 0) continue;
            float dist = Vector2.Distance(currentPosition, col.transform.position);
            if (dist < nearestDist) { nearestDist = dist; nearest = col.transform; }
        }

        if (nearest == null) return false;
        _bounces++;

        Vector2 dir = ((Vector2)nearest.position - currentPosition).normalized;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float speed = rb.linearVelocity.magnitude;
            rb.linearVelocity = dir * speed;
        }

        CombatManager.CreateExplosionEffect(currentPosition, 0.5f, Color.white, 0.2f);
        DebugHelper.Log($"[RicochetHandler] Bounce #{_bounces} → {nearest.name}");
        return true;
    }

    public float GetDamageMultiplier() => Mathf.Pow(_damageDecay, _bounces);
}
