#pragma warning disable CS0618
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 追踪弹 - 自动追踪最近敌人的投射物。
/// 对应 Python: entities/projectiles.py 中的 HomingProjectile
/// 
/// 特点：发射后自动追踪最近敌人，命中后造成伤害
/// 使用方式：由 Homing Missile 武器实例化
/// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class HomingProjectile : MonoBehaviour
    {
        [Header("追踪属性")]
        [SerializeField] private int _damage = 15;
        [SerializeField] private float _speed = 10f;
        [SerializeField] private float _turnSpeed = 5f;         // 转向速度（越大越灵活）
        [SerializeField] private float _lifetime = 5f;
        [SerializeField] private float _searchRadius = 15f;      // 搜索半径
        [SerializeField] private float _knockbackForce = 3f;

        private Rigidbody2D _rb;
        private Transform _target;
        private float _spawnTime;
        private float _damageMultiplier = 1f;
        private Vector2 _initialDirection;
        private float _initialAngle;
        private SpriteRenderer _sr;
        private HashSet<int> _hitEnemies = new HashSet<int>(); // 防止重复命中

        private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

        private void OnEnable()
        {
            _spawnTime = Time.time;
            _hitEnemies.Clear();
            _sr = GetComponent<SpriteRenderer>();

            // 设置初始方向
            _rb.linearVelocity = _initialDirection * _speed;

            // 寻找最近目标
            FindNearestTarget();

            // 注册到屏幕外裁剪器
            if (OffScreenCuller.Instance != null)
            {
                OffScreenCuller.TrackProjectile(gameObject, "HomingProjectile");
            }
        }

        private void OnDisable()
        {
            OffScreenCuller.Untrack(gameObject);
        }

        private void Update()
        {
            // 超时回收
            if (Time.time - _spawnTime > _lifetime)
            {
                DespawnSelf();
                return;
            }

        // 目标丢失时重新搜索
        if (_target == null)
        {
            FindNearestTarget();
        }

        // 旋转朝向飞行方向
        if (_rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            float angle = Mathf.Atan2(_rb.linearVelocity.y, _rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        // 拖尾粒子效果（简化 - 颜色脉冲）
        if (_sr != null)
        {
            float pulse = Mathf.Sin(Time.time * 15f) * 0.2f + 0.8f;
            _sr.color = new Color(1f, 0.3f, 0.3f, pulse);
        }
    }

    private void FixedUpdate()
    {
        if (_target != null)
        {
            // 计算朝向目标的方向
            Vector2 dirToTarget = ((Vector2)_target.position - (Vector2)transform.position).normalized;

            // 平滑转向
            Vector2 currentDir = _rb.linearVelocity.normalized;
            Vector2 newDir = Vector2.Lerp(currentDir, dirToTarget, _turnSpeed * Time.fixedDeltaTime).normalized;

            _rb.linearVelocity = newDir * _speed;
        }
        else
        {
            // 无目标时保持直线飞行
            if (_rb.linearVelocity.sqrMagnitude < 0.1f)
            {
                _rb.linearVelocity = _initialDirection * _speed;
            }
        }
    }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Enemy"))
            {
                // 防止重复命中
                int id = other.gameObject.GetInstanceID();
                if (_hitEnemies.Contains(id)) return;
                _hitEnemies.Add(id);

                var dmg = other.GetComponent<Damageable>();
                if (dmg != null && dmg.CurrentHp > 0)
                {
                    int finalDamage = Mathf.RoundToInt(_damage * _damageMultiplier);
                    dmg.TakeDamage(finalDamage);
                    DebugHelper.Log($"[HomingProjectile] Hit {other.name} for {finalDamage} damage");

                    // 击退效果
                    if (_knockbackForce > 0)
                    {
                        var rb = other.GetComponent<Rigidbody2D>();
                        if (rb != null)
                        {
                            Vector2 knockback = (other.transform.position - transform.position).normalized * _knockbackForce;
                            rb.linearVelocity += knockback;
                        }
                    }
                }

                // #15 DOT 追踪弹桥接：命中时附加 DOT 效果
                var dotHoming = GetComponent<DotHomingBullet>();
                if (dotHoming != null)
                {
                    dotHoming.OnHitEnemy(other.gameObject);
                }

                DespawnSelf();
            }
        }

        /// <summary>
        /// 回收自身（优先使用对象池，否则 Destroy）
        /// </summary>
        private void DespawnSelf()
        {
            if (ObjectPool.Instance != null && ObjectPool.Instance.HasPool("HomingProjectile"))
            {
                ObjectPool.Instance.Despawn("HomingProjectile", gameObject);
                return;
            }
            Destroy(gameObject);
        }

    /// <summary>
    /// 寻找最近的敌人
    /// </summary>
    private void FindNearestTarget()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, _searchRadius);
        float nearestDist = float.MaxValue;
        Transform nearest = null;

        foreach (var col in enemies)
        {
            if (!col.CompareTag("Enemy")) continue;

            float dist = Vector2.Distance(transform.position, col.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = col.transform;
            }
        }

        _target = nearest;
    }

    /// <summary>
    /// 设置初始方向
    /// </summary>
    public void SetDirection(Vector2 direction)
    {
        _initialDirection = direction.normalized;
        if (_rb != null)
        {
            _rb.linearVelocity = _initialDirection * _speed;
        }

        float angle = Mathf.Atan2(_initialDirection.y, _initialDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// 设置追踪弹属性
    /// </summary>
    public void Setup(int damage, float speed, float turnSpeed, float lifetime)
    {
        _damage = damage;
        _speed = speed;
        _turnSpeed = turnSpeed;
        _lifetime = lifetime;
    }

    /// <summary>
    /// 设置伤害倍率
    /// </summary>
    public void SetDamageMultiplier(float multiplier)
    {
        _damageMultiplier = multiplier;
    }

    /// <summary>
    /// 设置击退力
    /// </summary>
    public void SetKnockback(float force)
    {
        _knockbackForce = force;
    }

    /// <summary>
    /// 创建默认追踪弹（无预制体时）
    /// </summary>
    public static HomingProjectile CreateDefault(Vector2 position, Vector2 direction, int damage, float speed, float turnSpeed, float lifetime)
    {
        var go = new GameObject("HomingProjectile");
        go.transform.position = position;
        go.tag = "Untagged";

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateHomingSprite();
        sr.color = new Color(1f, 0.3f, 0.3f); // 红色
        sr.sortingOrder = 16;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.6f, 0.4f);

        var homing = go.AddComponent<HomingProjectile>();
        homing.Setup(damage, speed, turnSpeed, lifetime);
        homing.SetDirection(direction);

        return homing;
    }

    private static Sprite _cachedHomingSprite;
    private static Sprite CreateHomingSprite()
    {
        if (_cachedHomingSprite != null) return _cachedHomingSprite;

        int w = 20, h = 10;
        var tex = new Texture2D(w, h);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                // 导弹形状 - 前端尖锐
                float headT = Mathf.Clamp01((float)x / (w * 0.3f));
                float bodyRadius = Mathf.Lerp(0.2f, 1f, headT);
                float cy = Mathf.Abs(y - (h - 1) / 2f) / ((h - 1) / 2f);

                bool inBody = cy <= bodyRadius && x < w * 0.8f;
                bool inFin = (x >= w * 0.7f) && (cy <= 1f) && (cy >= 0.5f || cy <= -0.5f);

                if (inBody || inFin)
                {
                    tex.SetPixel(x, y, Color.white);
                }
                else
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
        tex.Apply();
        _cachedHomingSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 10f);
        return _cachedHomingSprite;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, _searchRadius);
    }
}