using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 风蚀漩涡 — DOT 敌人移动时在脚下生成的微型漩涡
/// </summary>
public class WindErosionVortex : MonoBehaviour
{
    private float _radius;
    private float _duration;
    private float _tickDamage;
    private float _pullChance;
    private float _spawnTime;
    private float _lastTick;
    private SpriteRenderer _sr;

    public void Setup(float radius, float duration, float tickDamage, float pullChance)
    {
        _radius = radius; _duration = duration; _tickDamage = tickDamage; _pullChance = pullChance;
    }

    private void Start()
    {
        _spawnTime = Time.time;
        _lastTick = Time.time;
        _sr = GetComponent<SpriteRenderer>();
        transform.localScale = Vector3.one * _radius * 0.5f;
    }

    private static readonly List<Collider2D> _overlapBuffer = new List<Collider2D>(16);

    private void Update()
    {
        if (Time.time - _spawnTime > _duration) { Destroy(gameObject); return; }
        transform.Rotate(0, 0, 180f * Time.deltaTime);

        if (_sr != null)
        {
            float alpha = 1f - (Time.time - _spawnTime) / _duration;
            var c = _sr.color;
            c.a = alpha * 0.5f;
            _sr.color = c;
        }

        if (Time.time - _lastTick < 0.5f) return;
        _lastTick = Time.time;

        int count = PhysicsHelper.OverlapCircle(transform.position, _radius, _overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            var hit = _overlapBuffer[i];
            if (!hit.CompareTag("Enemy")) continue;
            var dmg = hit.GetComponent<Damageable>();
            if (dmg == null || dmg.CurrentHp <= 0) continue;
            dmg.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(_tickDamage)));
            if (Random.value < _pullChance)
            {
                var rb = hit.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 pullDir = ((Vector2)transform.position - (Vector2)hit.transform.position).normalized;
                    rb.linearVelocity += pullDir * 3f;
                }
            }
        }
    }

    public static WindErosionVortex Create(Vector2 pos, float radius, float duration, float tickDamage, float pullChance)
    {
        var go = new GameObject("WindErosionVortex");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite();
        sr.color = new Color(0.7f, 0.85f, 1f, 0.5f);
        sr.sortingOrder = 2;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true; col.radius = radius;
        var vortex = go.AddComponent<WindErosionVortex>();
        vortex.Setup(radius, duration, tickDamage, pullChance);
        return vortex;
    }
}
