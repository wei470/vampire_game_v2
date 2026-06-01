using UnityEngine;

/// <summary>
/// 连射敌人 - 停下后向玩家连续发射多发子弹。
/// 对应 Python 版的 Burst 敌人
/// 
/// 行为：追踪玩家 → 进入射程后停下 → 连续发射3-5发子弹 → 冷却后继续
/// </summary>
public class BurstEnemy : EnemyBase
{
    [Header("连射攻击")]
    [SerializeField] private float _attackRange = 7f;
    [SerializeField] private float _burstCooldown = 3f;
    [SerializeField] private int _burstCount = 4;           // 每次连射数量
    [SerializeField] private float _burstInterval = 0.15f;  // 连射间隔
    [SerializeField] private int _bulletDamage = 8;
    [SerializeField] private float _bulletSpeed = 10f;
    [SerializeField] private float _bulletLifetime = 4f;
    [SerializeField] private float _spreadAngle = 15f;      // 散射角度

    private float _lastBurstTime;
    private int _shotsRemaining;
    private float _nextShotTime;
    private Transform _target;
    private Rigidbody2D _rb;
    private bool _isBursting;

    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
    }

    private new void Start()
    {
        var player = GameReferences.Player;
        if (player != null) _target = player.transform;
    }

    private void FixedUpdate()
    {
        if (!Alive || _target == null || _isBursting) return;

        float dist = Vector3.Distance(transform.position, _target.position);
        if (dist > _attackRange)
        {
            Vector2 dir = (_target.position - transform.position).normalized;
            _rb.linearVelocity = dir * MoveSpeed;
        }
        else
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }

    private void Update()
    {
        if (!Alive || _target == null) return;

        float dist = Vector3.Distance(transform.position, _target.position);

        // 开始连射
        if (!_isBursting && dist <= _attackRange && Time.time - _lastBurstTime >= _burstCooldown)
        {
            StartBurst();
        }

        // 连射中
        if (_isBursting && Time.time >= _nextShotTime)
        {
            FireBurstShot();
            _shotsRemaining--;
            _nextShotTime = Time.time + _burstInterval;

            if (_shotsRemaining <= 0)
            {
                _isBursting = false;
                _lastBurstTime = Time.time;
            }
        }
    }

    private void StartBurst()
    {
        _isBursting = true;
        _shotsRemaining = _burstCount;
        _nextShotTime = Time.time;
        _rb.linearVelocity = Vector2.zero;
    }

    private void FireBurstShot()
    {
        if (_target == null) return;

        Vector2 baseDir = (_target.position - transform.position).normalized;

        // 添加轻微随机散射
        float randomSpread = Random.Range(-_spreadAngle * 0.5f, _spreadAngle * 0.5f);
        float rad = randomSpread * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        Vector2 dir = new Vector2(baseDir.x * cos - baseDir.y * sin, baseDir.x * sin + baseDir.y * cos);

        var go = new GameObject("BurstBullet");
        go.transform.position = transform.position;
        go.tag = "Untagged";

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSmallBulletSprite();
        sr.color = new Color(1f, 0.8f, 0.2f); // 橙黄色
        sr.sortingOrder = 12;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearVelocity = dir * _bulletSpeed;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.15f;

        var bullet = go.AddComponent<EnemyBullet>();
        bullet.Setup(_bulletDamage, _bulletLifetime);
    }

    private static Sprite _cachedSmallBulletSprite;
    private static Sprite CreateSmallBulletSprite()
    {
        if (_cachedSmallBulletSprite != null) return _cachedSmallBulletSprite;
        var tex = new Texture2D(4, 4);
        for (int x = 0; x < 4; x++)
            for (int y = 0; y < 4; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(1.5f, 1.5f)) / 1.5f;
                tex.SetPixel(x, y, dist <= 1f ? Color.white : new Color(0, 0, 0, 0));
            }
        tex.Apply();
        _cachedSmallBulletSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return _cachedSmallBulletSprite;
    }

    private new void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}