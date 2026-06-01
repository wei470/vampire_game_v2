#pragma warning disable CS0618
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 冲击波 - 以玩家为中心向外扩散的圆形波。
/// 对应 Python: entities/projectiles.py 中的 ShockwaveRing
/// 
/// 特点：圆形扩散，命中范围内所有敌人，穿透所有敌人
/// 使用方式：由 Shockwave Ring 武器实例化
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class ShockwaveProjectile : MonoBehaviour
{
    [Header("冲击波属性")]
    [SerializeField] private int _damage = 12;
    [SerializeField] private float _expandSpeed = 8f;       // 扩散速度
    [SerializeField] private float _maxRadius = 5f;         // 最大半径
    [SerializeField] private float _lifetime = 2f;

    private float _currentRadius = 0.5f;
    private float _spawnTime;
    private float _damageMultiplier = 1f;
    private HashSet<int> _hitEnemies = new HashSet<int>();
    private SpriteRenderer _sr;
    private CircleCollider2D _circleCol;

    private void Awake()
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        _circleCol = GetComponent<CircleCollider2D>();
        _circleCol.isTrigger = true;
        _circleCol.radius = _currentRadius;
    }

    private void Start()
    {
        _spawnTime = Time.time;
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        // 超时销毁
        if (Time.time - _spawnTime > _lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // 扩大碰撞范围
        _currentRadius += _expandSpeed * Time.deltaTime;
        if (_currentRadius > _maxRadius)
        {
            _currentRadius = _maxRadius;
        }
        _circleCol.radius = _currentRadius;

        // 更新视觉大小
        if (_sr != null)
        {
            transform.localScale = Vector3.one * _currentRadius * 2f;
            // 渐变透明
            float alpha = Mathf.Lerp(0.8f, 0f, (_currentRadius - 0.5f) / (_maxRadius - 0.5f));
            Color c = _sr.color;
            c.a = alpha;
            _sr.color = c;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            int id = other.gameObject.GetInstanceID();
            if (_hitEnemies.Contains(id)) return;
            _hitEnemies.Add(id);

            var dmg = other.GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp > 0)
            {
                int finalDamage = Mathf.RoundToInt(_damage * _damageMultiplier);
                dmg.TakeDamage(finalDamage);
                DebugHelper.Log($"[Shockwave] Hit {other.name} for {finalDamage} damage");

                // 击退效果 - 向外推
                var rb = other.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 pushDir = (other.transform.position - transform.position).normalized;
                    rb.linearVelocity += pushDir * 5f;
                }
            }
        }
    }

    /// <summary>
    /// 设置冲击波属性
    /// </summary>
    public void Setup(int damage, float expandSpeed, float maxRadius, float lifetime)
    {
        _damage = damage;
        _expandSpeed = expandSpeed;
        _maxRadius = maxRadius;
        _lifetime = lifetime;
    }

    public void SetDamageMultiplier(float multiplier)
    {
        _damageMultiplier = multiplier;
    }

    /// <summary>
    /// 创建默认冲击波（无预制体时）
    /// </summary>
    public static ShockwaveProjectile CreateDefault(Vector2 position, int damage, float expandSpeed, float maxRadius, float lifetime)
    {
        var go = new GameObject("Shockwave");
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateRingSprite();
        sr.color = new Color(0.5f, 1f, 0.8f, 0.8f); // 青色
        sr.sortingOrder = 8;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;

        var shockwave = go.AddComponent<ShockwaveProjectile>();
        shockwave.Setup(damage, expandSpeed, maxRadius, lifetime);

        return shockwave;
    }

    private static Sprite _cachedRingSprite;
    private static Sprite CreateRingSprite()
    {
        if (_cachedRingSprite != null) return _cachedRingSprite;

        int size = 64;
        var tex = new Texture2D(size, size);
        float center = size / 2f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                // 环形
                if (dist >= 0.8f && dist <= 1f)
                {
                    float alpha = 1f - Mathf.Abs(dist - 0.9f) / 0.1f;
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
        tex.Apply();
        _cachedRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 10f);
        return _cachedRingSprite;
    }
}