using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 地雷 - 落地后待机，敌人靠近时引爆造成AoE伤害。
/// 对应 Python: entities/projectiles.py 中的 MineTrap
/// 
/// 特点：落地后静止，检测范围内敌人后爆炸造成范围伤害
/// 使用方式：由 Mine Trap 武器实例化
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class MineTrap : MonoBehaviour
{
    [Header("地雷属性")]
    [SerializeField] private int _damage = 25;
    [SerializeField] private float _triggerRadius = 2f;      // 触发半径
    [SerializeField] private float _explosionRadius = 3f;    // 爆炸半径
    [SerializeField] private float _lifetime = 10f;          // 存活时间
    [SerializeField] private float _armDelay = 0.5f;         // 部署延迟（部署后多久可触发）
    [SerializeField] private float _knockbackForce = 5f;

    private float _spawnTime;
    private float _armTime;
    private bool _isArmed = false;
    private bool _hasExploded = false;
    private float _damageMultiplier = 1f;
    private SpriteRenderer _sr;
    private CircleCollider2D _triggerCol;

    private void Awake()
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        _triggerCol = GetComponent<CircleCollider2D>();
        _triggerCol.isTrigger = true;
        _triggerCol.radius = _triggerRadius;
    }

    private void Start()
    {
        _spawnTime = Time.time;
        _armTime = _spawnTime + _armDelay;
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

        // 部署延迟后武装
        if (!_isArmed && Time.time >= _armTime)
        {
            _isArmed = true;
            // 视觉提示 - 颜色变化
            if (_sr != null)
            {
                _sr.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            }
        }

        // 闪烁效果（武装后）
        if (_isArmed && _sr != null)
        {
            float pulse = Mathf.Sin(Time.time * 3f) * 0.15f + 0.85f;
            Color c = _sr.color;
            c.a = pulse;
            _sr.color = c;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!_isArmed || _hasExploded) return;

        if (other.CompareTag("Enemy"))
        {
            // 检测到敌人，引爆
            Explode();
        }
    }

    /// <summary>
    /// 爆炸 - 造成AoE伤害
    /// </summary>
    private void Explode()
    {
        _hasExploded = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _explosionRadius);
        int hitCount = 0;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                var dmg = hit.GetComponent<Damageable>();
                if (dmg != null && dmg.CurrentHp > 0)
                {
                    int finalDamage = Mathf.RoundToInt(_damage * _damageMultiplier);
                    dmg.TakeDamage(finalDamage);
                    hitCount++;

                    // 击退
                    var rb = hit.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        Vector2 pushDir = (hit.transform.position - transform.position).normalized;
                        rb.linearVelocity += pushDir * _knockbackForce;
                    }
                }
            }
        }

        DebugHelper.Log($"[MineTrap] Exploded! Hit {hitCount} enemies for {_damage} damage each");

        // 爆炸视觉效果
        CreateExplosionEffect();

        Destroy(gameObject);
    }

    /// <summary>
    /// 创建爆炸视觉效果
    /// </summary>
    private void CreateExplosionEffect()
    {
        var explosionGo = new GameObject("Explosion");
        explosionGo.transform.position = transform.position;

        var sr = explosionGo.AddComponent<SpriteRenderer>();
        sr.sprite = CreateExplosionSprite();
        sr.color = new Color(1f, 0.8f, 0.2f, 1f);
        sr.sortingOrder = 25;

        var explosion = explosionGo.AddComponent<ExplosionEffect>();
        explosion.Setup(_explosionRadius, 0.3f);
    }

    public void Setup(int damage, float triggerRadius, float explosionRadius, float lifetime)
    {
        _damage = damage;
        _triggerRadius = triggerRadius;
        _explosionRadius = explosionRadius;
        _lifetime = lifetime;

        _triggerCol = GetComponent<CircleCollider2D>();
        if (_triggerCol != null) _triggerCol.radius = _triggerRadius;
    }

    public void SetDamageMultiplier(float multiplier)
    {
        _damageMultiplier = multiplier;
    }

    public static MineTrap CreateDefault(Vector2 position, int damage, float triggerRadius, float explosionRadius, float lifetime)
    {
        var go = new GameObject("MineTrap");
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateMineSprite();
        sr.color = new Color(0.8f, 0.2f, 0.2f, 0.6f); // 暗红（未武装）
        sr.sortingOrder = 3;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = triggerRadius;

        var mine = go.AddComponent<MineTrap>();
        mine.Setup(damage, triggerRadius, explosionRadius, lifetime);

        return mine;
    }

    private static Sprite _cachedMineSprite;
    private static Sprite CreateMineSprite()
    {
        if (_cachedMineSprite != null) return _cachedMineSprite;

        int size = 16;
        var tex = new Texture2D(size, size);
        float center = size / 2f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                // 地雷形状 - 中心圆点
                if (dist <= 0.4f)
                {
                    tex.SetPixel(x, y, Color.white);
                }
                else if (dist <= 0.6f)
                {
                    tex.SetPixel(x, y, new Color(0.6f, 0.6f, 0.6f));
                }
                else
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
        tex.Apply();
        _cachedMineSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 10f);
        return _cachedMineSprite;
    }

    private static Sprite _cachedExplosionSprite;
    private static Sprite CreateExplosionSprite()
    {
        if (_cachedExplosionSprite != null) return _cachedExplosionSprite;

        int size = 64;
        var tex = new Texture2D(size, size);
        float center = size / 2f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                if (dist <= 1f)
                {
                    float alpha = Mathf.Lerp(1f, 0f, dist);
                    tex.SetPixel(x, y, new Color(1f, 0.8f, 0.2f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
        tex.Apply();
        _cachedExplosionSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 10f);
        return _cachedExplosionSprite;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _triggerRadius);
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}

/// <summary>
/// 爆炸视觉效果
/// </summary>
public class ExplosionEffect : MonoBehaviour
{
    private float _maxRadius;
    private float _duration;
    private float _spawnTime;
    private SpriteRenderer _sr;

    public void Setup(float maxRadius, float duration)
    {
        _maxRadius = maxRadius;
        _duration = duration;
        _spawnTime = Time.time;
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        float elapsed = Time.time - _spawnTime;
        if (elapsed > _duration)
        {
            Destroy(gameObject);
            return;
        }

        float t = elapsed / _duration;
        float scale = Mathf.Lerp(0.5f, _maxRadius * 2f, t);
        transform.localScale = Vector3.one * scale;

        if (_sr != null)
        {
            Color c = _sr.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            _sr.color = c;
        }
    }
}