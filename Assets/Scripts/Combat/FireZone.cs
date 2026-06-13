#pragma warning disable CS0414
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 火焰区域 - 在指定位置创建持续伤害区域。
/// 对应 Python: entities/projectiles.py 中的 FireZone/FireBomb
/// 
/// 特点：落地后创建伤害区域，对区域内敌人持续造成伤害
/// 使用方式：由 Flamethrower 武器实例化
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class FireZone : MonoBehaviour
{
    [Header("火焰区域属性")]
    [SerializeField] private int _damagePerTick = 5;
    [SerializeField] private float _tickInterval = 0.5f;     // 伤害间隔
    [SerializeField] private float _zoneLifetime = 3f;        // 区域持续时间
    [SerializeField] private float _zoneRadius = 1.5f;        // 区域半径
    [SerializeField] private float _expandSpeed = 2f;         // 到达目标位置的速度
    [SerializeField] private float _moveDistance = 8f;        // 飞行距离

    private float _spawnTime;
    private float _lastTickTime;
    private Vector2 _targetPosition;
    private bool _hasLanded = false;
    private Rigidbody2D _rb;
    private static readonly List<Collider2D> _overlapBuffer = new List<Collider2D>(16);
    private HashSet<int> _hitEnemies = new HashSet<int>();
    private float _damageMultiplier = 1f;
    private SpriteRenderer _sr;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;

        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = _zoneRadius;
    }

    private void Start()
    {
        _spawnTime = Time.time;
        _lastTickTime = Time.time;

        _sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        float elapsed = Time.time - _spawnTime;

        // 超时销毁
        if (elapsed > _zoneLifetime)
        {
            Destroy(gameObject);
            return;
        }

        // 渐变效果
        if (_sr != null)
        {
            float alpha = Mathf.Lerp(0.8f, 0.1f, elapsed / _zoneLifetime);
            Color c = _sr.color;
            c.a = alpha;
            _sr.color = c;
        }

        // 持续伤害tick
        if (_hasLanded && Time.time - _lastTickTime >= _tickInterval)
        {
            ApplyZoneDamage();
            _lastTickTime = Time.time;
        }
    }

    private void FixedUpdate()
    {
        if (!_hasLanded)
        {
            // 向目标位置移动
            Vector2 dir = (_targetPosition - (Vector2)transform.position);
            if (dir.magnitude < 0.3f)
            {
                // 到达目标，停留
                _hasLanded = true;
                _rb.linearVelocity = Vector2.zero;

                // 扩大碰撞区域
                var col = GetComponent<CircleCollider2D>();
                if (col != null) col.radius = _zoneRadius;
            }
            else
            {
                _rb.linearVelocity = dir.normalized * _expandSpeed;
            }
        }
    }

    private void ApplyZoneDamage()
    {
        int count = PhysicsHelper.OverlapCircle(transform.position, _zoneRadius, _overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            var hit = _overlapBuffer[i];
            if (hit.CompareTag("Enemy"))
            {
                var dmg = hit.GetComponent<Damageable>();
                if (dmg != null && dmg.CurrentHp > 0)
                {
                    int finalDamage = Mathf.RoundToInt(_damagePerTick * _damageMultiplier);
                    dmg.TakeDamage(finalDamage);
                }
            }
        }
    }

    /// <summary>
    /// 设置火焰区域
    /// </summary>
    public void Setup(int damage, float lifetime, float radius, float tickInterval)
    {
        _damagePerTick = damage;
        _zoneLifetime = lifetime;
        _zoneRadius = radius;
        _tickInterval = tickInterval;

        var col = GetComponent<CircleCollider2D>();
        if (col != null) col.radius = _zoneRadius * 0.5f; // 初始较小
    }

    /// <summary>
    /// 设置飞行目标位置
    /// </summary>
    public void SetTargetPosition(Vector2 target)
    {
        _targetPosition = target;
    }

    /// <summary>
    /// 设置伤害倍率
    /// </summary>
    public void SetDamageMultiplier(float multiplier)
    {
        _damageMultiplier = multiplier;
    }

    /// <summary>
    /// 创建默认火焰区域（无预制体时）
    /// </summary>
    public static FireZone CreateDefault(Vector2 position, int damage, float lifetime, float radius, float tickInterval)
    {
        var go = new GameObject("FireZone");
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateFireSprite();
        sr.color = new Color(1f, 0.5f, 0f, 0.8f); // 橙色
        sr.sortingOrder = 5;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = radius;

        var zone = go.AddComponent<FireZone>();
        zone.Setup(damage, lifetime, radius, tickInterval);

        return zone;
    }

    private static Sprite _cachedFireSprite;
    private static Sprite CreateFireSprite()
    {
        if (_cachedFireSprite != null) return _cachedFireSprite;

        int size = 32;
        var tex = new Texture2D(size, size);
        float center = size / 2f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                if (dist <= 1f)
                {
                    float alpha = Mathf.Lerp(1f, 0f, dist);
                    tex.SetPixel(x, y, new Color(1f, 0.5f, 0f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
        tex.Apply();
        _cachedFireSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 10f);
        return _cachedFireSprite;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _zoneRadius);
    }
}