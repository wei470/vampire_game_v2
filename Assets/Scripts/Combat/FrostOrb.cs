#pragma warning disable CS0618
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 冰霜球 - 飞行到目标位置后创建减速区域。
/// 对应 Python: entities/projectiles.py 中的 FrostOrb
/// 
/// 特点：飞行到指定位置，创建减速区域，对区域内敌人减速+伤害
/// 使用方式：由 Frost Orb 武器实例化
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class FrostOrb : MonoBehaviour
{
    [Header("冰霜属性")]
    [SerializeField] private int _damage = 8;
    [SerializeField] private float _tickDamage = 3;
    [SerializeField] private float _tickInterval = 0.5f;
    [SerializeField] private float _slowAmount = 0.5f;      // 减速比例（0.5 = 50%减速）
    [SerializeField] private float _slowDuration = 1.5f;
    [SerializeField] private float _zoneLifetime = 4f;
    [SerializeField] private float _zoneRadius = 2.5f;
    [SerializeField] private float _moveSpeed = 8f;

    private Rigidbody2D _rb;
    private Vector2 _targetPosition;
    private bool _hasLanded = false;
    private float _spawnTime;
    private float _lastTickTime;
    private float _damageMultiplier = 1f;
    private SpriteRenderer _sr;
    private static readonly List<Collider2D> _overlapBuffer = new List<Collider2D>(16);
    private HashSet<int> _slowedEnemies = new HashSet<int>();

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;

        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.3f; // 初始较小（飞行中）
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
        if (elapsed > _zoneLifetime)
        {
            Destroy(gameObject);
            return;
        }

        // 区域中的持续效果
        if (_hasLanded)
        {
            // 脉冲动画
            if (_sr != null)
            {
                float pulse = Mathf.Sin(Time.time * 4f) * 0.1f + 0.7f;
                _sr.color = new Color(0.4f, 0.7f, 1f, pulse);
            }

            // Tick伤害
            if (Time.time - _lastTickTime >= _tickInterval)
            {
                ApplyFrostDamage();
                _lastTickTime = Time.time;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!_hasLanded)
        {
            Vector2 dir = _targetPosition - (Vector2)transform.position;
            if (dir.magnitude < 0.3f)
            {
                _hasLanded = true;
                _rb.linearVelocity = Vector2.zero;

                // 扩大到区域大小
                var col = GetComponent<CircleCollider2D>();
                if (col != null) col.radius = _zoneRadius;

                // 更新视觉
                transform.localScale = Vector3.one * _zoneRadius * 2f;
                if (_sr != null)
                {
                    _sr.color = new Color(0.4f, 0.7f, 1f, 0.7f);
                }
            }
            else
            {
                _rb.linearVelocity = dir.normalized * _moveSpeed;
            }
        }
    }

    private void ApplyFrostDamage()
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
                    int finalDamage = Mathf.RoundToInt(_tickDamage * _damageMultiplier);
                    dmg.TakeDamage(finalDamage);
                }

                // 应用减速
                ApplySlow(hit.gameObject);
            }
        }
    }

    private void ApplySlow(GameObject enemy)
    {
        var enemyBase = enemy.GetComponent<EnemyBase>();
        if (enemyBase != null)
        {
            int id = enemy.GetInstanceID();
            if (!_slowedEnemies.Contains(id))
            {
                _slowedEnemies.Add(id);
                // 临时减速
                float originalSpeed = enemyBase.MoveSpeed;
                enemyBase.MoveSpeed *= (1f - _slowAmount);

                // 恢复速度（延迟）
                StartCoroutine(RestoreSpeed(enemyBase, originalSpeed, id));
            }
        }
    }

    private System.Collections.IEnumerator RestoreSpeed(EnemyBase enemy, float originalSpeed, int id)
    {
        yield return new WaitForSeconds(_slowDuration);
        if (enemy != null)
        {
            enemy.MoveSpeed = originalSpeed;
        }
        _slowedEnemies.Remove(id);
    }

    /// <summary>
    /// 碰撞检测 - 初始飞行中的命中
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasLanded) return; // 区域模式下不处理碰撞

        if (other.CompareTag("Enemy"))
        {
            var dmg = other.GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp > 0)
            {
                int finalDamage = Mathf.RoundToInt(_damage * _damageMultiplier);
                dmg.TakeDamage(finalDamage);
            }
            // 命中后不销毁，继续飞行到目标
        }
    }

    public void SetTargetPosition(Vector2 target)
    {
        _targetPosition = target;
    }

    public void Setup(int damage, float tickDamage, float slowAmount, float slowDuration, float zoneLifetime, float zoneRadius)
    {
        _damage = damage;
        _tickDamage = tickDamage;
        _slowAmount = slowAmount;
        _slowDuration = slowDuration;
        _zoneLifetime = zoneLifetime;
        _zoneRadius = zoneRadius;
    }

    public void SetDamageMultiplier(float multiplier)
    {
        _damageMultiplier = multiplier;
    }

    public static FrostOrb CreateDefault(Vector2 position, Vector2 targetPos, int damage, float tickDamage, float slowAmount, float slowDuration, float zoneLifetime, float zoneRadius)
    {
        var go = new GameObject("FrostOrb");
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateFrostSprite();
        sr.color = new Color(0.4f, 0.7f, 1f, 0.9f); // 冰蓝色
        sr.sortingOrder = 10;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;

        var orb = go.AddComponent<FrostOrb>();
        orb.Setup(damage, tickDamage, slowAmount, slowDuration, zoneLifetime, zoneRadius);
        orb.SetTargetPosition(targetPos);

        return orb;
    }

    private static Sprite _cachedFrostSprite;
    private static Sprite CreateFrostSprite()
    {
        if (_cachedFrostSprite != null) return _cachedFrostSprite;

        int size = 32;
        var tex = new Texture2D(size, size);
        float center = size / 2f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                if (dist <= 1f)
                {
                    float alpha = Mathf.Lerp(0.9f, 0.1f, dist);
                    // 冰霜纹理
                    float noise = Mathf.PerlinNoise(x * 0.5f, y * 0.5f) * 0.3f;
                    tex.SetPixel(x, y, new Color(0.5f + noise, 0.8f + noise * 0.5f, 1f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
        tex.Apply();
        _cachedFrostSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 10f);
        return _cachedFrostSprite;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _zoneRadius);
    }
}