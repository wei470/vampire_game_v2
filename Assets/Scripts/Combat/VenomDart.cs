using UnityEngine;

/// <summary>
/// 毒镖 - 命中敌人后造成即时伤害+持续毒伤害。
/// 对应 Python: entities/projectiles.py 中的 VenomDart
/// 
/// 特点：直线飞行，命中后施加中毒DoT效果
/// 使用方式：由 Venom Dart 武器实例化
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class VenomDart : MonoBehaviour
{
    [Header("毒镖属性")]
    [SerializeField] private int _impactDamage = 8;
    [SerializeField] private float _dotDamage = 3f;         // 每秒持续伤害
    [SerializeField] private float _dotDuration = 3f;        // 持续时间
    [SerializeField] private float _speed = 14f;
    [SerializeField] private float _lifetime = 3f;
    [SerializeField] private int _pierce = 1;

    private Rigidbody2D _rb;
    private Vector2 _direction;
    private float _spawnTime;
    private float _damageMultiplier = 1f;
    private int _currentPierce;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _currentPierce = _pierce;

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Start()
    {
        _spawnTime = Time.time;
    }

    private void Update()
    {
        if (Time.time - _spawnTime > _lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void FixedUpdate()
    {
        _rb.linearVelocity = _direction * _speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            var dmg = other.GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp > 0)
            {
                // 即时伤害
                int finalDamage = Mathf.RoundToInt(_impactDamage * _damageMultiplier);
                dmg.TakeDamage(finalDamage);

                // 施加中毒效果
                ApplyPoison(other.gameObject);

                DebugHelper.Log($"[VenomDart] Hit {other.name} for {finalDamage} impact + poison DoT");
            }

            _currentPierce--;
            if (_currentPierce <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// 施加中毒DoT（可叠加层数）
    /// </summary>
    private void ApplyPoison(GameObject enemy)
    {
        var poison = enemy.GetComponent<PoisonStackEffect>();
        if (poison == null)
            poison = enemy.AddComponent<PoisonStackEffect>();
        // 每次命中叠加一层中毒，基础DPS 2，持续 5 秒
        poison.AddStack(_dotDamage * _damageMultiplier, _dotDuration, false, 0f, 2f);
    }

    public void SetDirection(Vector2 direction)
    {
        _direction = direction.normalized;
        _rb.linearVelocity = _direction * _speed;

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void Setup(int impactDamage, float dotDamage, float dotDuration, float speed, float lifetime, int pierce)
    {
        _impactDamage = impactDamage;
        _dotDamage = dotDamage;
        _dotDuration = dotDuration;
        _speed = speed;
        _lifetime = lifetime;
        _pierce = pierce;
        _currentPierce = pierce;
    }

    public void SetDamageMultiplier(float multiplier)
    {
        _damageMultiplier = multiplier;
    }

    public static VenomDart CreateDefault(Vector2 position, Vector2 direction, int impactDamage, float dotDamage, float dotDuration, float speed, float lifetime, int pierce)
    {
        var go = new GameObject("VenomDart");
        go.transform.position = position;
        go.tag = "Untagged";

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateDartSprite();
        sr.color = new Color(0.2f, 0.9f, 0.2f); // 毒绿色
        sr.sortingOrder = 15;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.6f, 0.25f);

        var dart = go.AddComponent<VenomDart>();
        dart.Setup(impactDamage, dotDamage, dotDuration, speed, lifetime, pierce);
        dart.SetDirection(direction);

        return dart;
    }

    private static Sprite _cachedDartSprite;
    private static Sprite CreateDartSprite()
    {
        if (_cachedDartSprite != null) return _cachedDartSprite;

        int w = 16, h = 6;
        var tex = new Texture2D(w, h);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                // 尖锐的飞镖形状
                float headT = Mathf.Clamp01((float)x / (w * 0.3f));
                float halfHeight = Mathf.Lerp(0.5f, 2.5f, headT);
                float cy = Mathf.Abs(y - (h - 1) / 2f);

                bool inBody = cy <= halfHeight;
                if (inBody)
                {
                    tex.SetPixel(x, y, Color.white);
                }
                else
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
        tex.Apply();
        _cachedDartSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 10f);
        return _cachedDartSprite;
    }
}

/// <summary>
/// 中毒效果 - 挂载到敌人身上，持续造成伤害
/// </summary>
public class PoisonEffect : MonoBehaviour
{
    private float _tickDamage;
    private float _duration;
    private float _startTime;
    private float _lastTick;
    private float _tickInterval = 0.5f;
    private Damageable _damageable;
    private SpriteRenderer _sr;
    private Color _originalColor;

    public void Setup(float tickDamage, float duration)
    {
        _tickDamage = tickDamage;
        _duration = duration;
        _startTime = Time.time;
        _lastTick = Time.time;
        _damageable = GetComponent<Damageable>();
        _sr = GetComponent<SpriteRenderer>();

        if (_sr != null)
        {
            _originalColor = _sr.color;
        }
    }

    public void Refresh(float tickDamage, float duration)
    {
        _tickDamage = Mathf.Max(_tickDamage, tickDamage);
        _duration = duration;
        _startTime = Time.time;
    }

    private void Update()
    {
        float elapsed = Time.time - _startTime;
        if (elapsed > _duration)
        {
            // 恢复颜色
            if (_sr != null)
            {
                _sr.color = _originalColor;
            }
            Destroy(this); // 只移除组件
            return;
        }

        // 绿色闪烁
        if (_sr != null)
        {
            float pulse = Mathf.Sin(Time.time * 8f) * 0.3f;
            _sr.color = new Color(
                _originalColor.r * 0.5f,
                Mathf.Min(1f, _originalColor.g + 0.3f + pulse),
                _originalColor.b * 0.5f
            );
        }

        // 持续伤害
        if (Time.time - _lastTick >= _tickInterval)
        {
            if (_damageable != null && _damageable.CurrentHp > 0)
            {
                int damage = Mathf.RoundToInt(_tickDamage * _tickInterval);
                _damageable.TakeDamage(Mathf.Max(1, damage));
            }
            _lastTick = Time.time;
        }
    }

    private void OnDestroy()
    {
        // 确保恢复颜色
        if (_sr != null)
        {
            _sr.color = _originalColor;
        }
    }
}