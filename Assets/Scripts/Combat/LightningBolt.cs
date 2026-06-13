#pragma warning disable CS0618
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 闪电链 - 命中敌人后跳跃到附近其他敌人。
/// 对应 Python: entities/projectiles.py 中的 LightningBolt
/// 
/// 特点：命中一个敌人后链式传递到附近敌人，造成递减伤害
/// 使用方式：由 Chain Lightning 武器实例化
/// </summary>
public class LightningBolt : MonoBehaviour
{
    [Header("闪电属性")]
    [SerializeField] private int _damage = 20;
    [SerializeField] private int _maxChainCount = 3;       // 最大链式次数
    [SerializeField] private float _chainRadius = 5f;       // 链式跳跃半径
    [SerializeField] private float _chainDelay = 0.15f;     // 每次跳跃延迟
    [SerializeField] private float _damageDecay = 0.7f;     // 每次跳跃伤害衰减
    [SerializeField] private float _lifetime = 2f;          // 总存在时间
    [SerializeField] private float _moveSpeed = 20f;        // 移动速度

    private Rigidbody2D _rb;
    private Vector2 _direction;
    private float _spawnTime;
    private int _currentChainCount;
    private float _currentDamage;
    private float _damageMultiplier = 1f;
    private static readonly List<Collider2D> _overlapBuffer = new List<Collider2D>(16);
    private HashSet<int> _hitEnemies = new HashSet<int>();
    private bool _isChaining = false;
    private SpriteRenderer _sr;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody2D>();
        }
        _rb.gravityScale = 0f;

        _currentChainCount = _maxChainCount;
        _currentDamage = _damage;

        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.5f, 0.5f);
        }
        else
        {
            col.isTrigger = true;
        }
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
        }

        // 闪烁效果
        if (_sr != null)
        {
            float flash = Mathf.Sin(Time.time * 30f) * 0.3f + 0.7f;
            Color c = _sr.color;
            c.a = flash;
            _sr.color = c;
        }
    }

    private void FixedUpdate()
    {
        if (!_isChaining)
        {
            _rb.linearVelocity = _direction * _moveSpeed;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isChaining) return;

        if (other.CompareTag("Enemy"))
        {
            int id = other.gameObject.GetInstanceID();
            if (_hitEnemies.Contains(id)) return;

            // 命中敌人
            var dmg = other.GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp > 0)
            {
                int finalDamage = Mathf.RoundToInt(_currentDamage * _damageMultiplier);
                dmg.TakeDamage(finalDamage);
                _hitEnemies.Add(id);

                DebugHelper.Log($"[LightningBolt] Hit {other.name} for {finalDamage} damage, chains left: {_currentChainCount}");

                // 闪烁敌人（视觉反馈）
                StartCoroutine(FlashEnemy(other.GetComponent<SpriteRenderer>()));

                // 链式跳跃
                _currentChainCount--;
                if (_currentChainCount > 0)
                {
                    _currentDamage *= _damageDecay;
                    StartCoroutine(ChainToNext(other.transform.position));
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    /// <summary>
    /// 链式跳跃到下一个最近的敌人
    /// </summary>
    private IEnumerator ChainToNext(Vector2 fromPosition)
    {
        _isChaining = true;
        _rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(_chainDelay);

        // 寻找最近的未命中敌人
        int count = PhysicsHelper.OverlapCircle(fromPosition, _chainRadius, _overlapBuffer);
        Transform nearest = null;
        float nearestDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var col = _overlapBuffer[i];
            if (!col.CompareTag("Enemy")) continue;
            int id = col.gameObject.GetInstanceID();
            if (_hitEnemies.Contains(id)) continue;

            float dist = Vector2.Distance(fromPosition, col.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = col.transform;
            }
        }

        if (nearest != null)
        {
            // 绘制闪电线效果
            yield return StartCoroutine(DrawLightningLine(fromPosition, nearest.position));

            // 移动到下一个敌人
            transform.position = nearest.position;
            _isChaining = false;

            // 触发碰撞检测
            var col = nearest.GetComponent<Collider2D>();
            if (col != null)
            {
                OnTriggerEnter2D(col);
            }
        }
        else
        {
            // 没有更多目标
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 绘制闪电线（简化版 - 使用 LineRenderer）
    /// </summary>
    private IEnumerator DrawLightningLine(Vector2 from, Vector2 to)
    {
        var lineGo = new GameObject("LightningLine");
        var lr = lineGo.AddComponent<LineRenderer>();
        lr.startWidth = 0.1f;
        lr.endWidth = 0.05f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(0.8f, 0.8f, 1f, 1f);
        lr.endColor = new Color(0.5f, 0.5f, 1f, 0.5f);
        lr.sortingOrder = 20;

        // 生成锯齿形闪电路径
        int segments = 5;
        lr.positionCount = segments + 1;
        lr.SetPosition(0, from);
        for (int i = 1; i < segments; i++)
        {
            float t = (float)i / segments;
            Vector2 point = Vector2.Lerp(from, to, t);
            point += Random.insideUnitCircle * 0.3f; // 随机偏移
            lr.SetPosition(i, point);
        }
        lr.SetPosition(segments, to);

        yield return new WaitForSeconds(0.1f);
        Destroy(lineGo);
    }

    /// <summary>
    /// 闪烁敌人（视觉反馈）
    /// </summary>
    private IEnumerator FlashEnemy(SpriteRenderer sr)
    {
        if (sr == null) yield break;
        Color original = sr.color;
        sr.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        if (sr != null) sr.color = original;
    }

    /// <summary>
    /// 设置闪电方向
    /// </summary>
    public void SetDirection(Vector2 direction)
    {
        _direction = direction.normalized;
        _rb.linearVelocity = _direction * _moveSpeed;

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// 设置闪电属性
    /// </summary>
    public void Setup(int damage, int chainCount, float chainRadius, float lifetime)
    {
        _damage = damage;
        _maxChainCount = chainCount;
        _chainRadius = chainRadius;
        _lifetime = lifetime;
        _currentChainCount = chainCount;
        _currentDamage = damage;
    }

    /// <summary>
    /// 设置伤害倍率
    /// </summary>
    public void SetDamageMultiplier(float multiplier)
    {
        _damageMultiplier = multiplier;
    }

    /// <summary>
    /// 创建默认闪电（无预制体时）
    /// </summary>
    public static LightningBolt CreateDefault(Vector2 position, Vector2 direction, int damage, int chainCount, float chainRadius, float lifetime)
    {
        var go = new GameObject("LightningBolt");
        go.transform.position = position;
        go.tag = "Untagged";

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateLightningSprite();
        sr.color = new Color(0.7f, 0.7f, 1f); // 浅紫色
        sr.sortingOrder = 18;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.5f, 0.5f);

        var bolt = go.AddComponent<LightningBolt>();
        bolt.Setup(damage, chainCount, chainRadius, lifetime);
        bolt.SetDirection(direction);

        return bolt;
    }

    private static Sprite _cachedLightningSprite;
    private static Sprite CreateLightningSprite()
    {
        if (_cachedLightningSprite != null) return _cachedLightningSprite;

        int w = 16, h = 16;
        var tex = new Texture2D(w, h);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                // 闪电形状 - 菱形
                float cx = Mathf.Abs(x - 7.5f) / 7.5f;
                float cy = Mathf.Abs(y - 7.5f) / 7.5f;
                float dist = cx + cy; // 菱形距离
                tex.SetPixel(x, y, dist <= 1f ? Color.white : new Color(0, 0, 0, 0));
            }
        tex.Apply();
        _cachedLightningSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 10f);
        return _cachedLightningSprite;
    }
}