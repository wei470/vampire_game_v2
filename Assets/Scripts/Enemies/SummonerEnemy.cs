#pragma warning disable CS0414
using UnityEngine;

/// <summary>
/// 召唤敌人 - 周期性召唤小怪。
/// 对应 Python 版的 Summoner 敌人
/// 
/// 行为：追踪玩家 → 周期性召唤小型敌人 → 召唤时停下
/// </summary>
public class SummonerEnemy : EnemyBase
{
    [Header("召唤属性")]
    [SerializeField] private float _summonCooldown = 5f;
    [SerializeField] private int _maxSummons = 5;          // 最大存活召唤数
    [SerializeField] private float _summonRadius = 2f;     // 召唤半径
    [SerializeField] private int _summonHp = 20;
    [SerializeField] private float _summonSpeed = 4f;
    [SerializeField] private int _summonDamage = 5;
    [SerializeField] private Color _summonColor = new Color(0.6f, 0.2f, 0.8f);

    private float _lastSummonTime;
    private int _currentSummons = 0;
    private SpriteRenderer _sr;
    private Color _originalColor;

    protected override void Awake()
    {
        base.Awake();
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _originalColor = _sr.color;
    }

    protected override void Start()
    {
        base.Start();
        // 使用全局引用缓存
        var player = GameReferences.Player;
        if (player != null) _target = player.transform;
    }

    private void FixedUpdate()
    {
        if (!Alive || _target == null) return;
        Vector2 dir = (_target.position - transform.position).normalized;
        _rb.linearVelocity = dir * MoveSpeed * 0.4f;
    }

    private void Update()
    {
        if (!Alive) return;

        // #15 距离 LOD：远距离跳过召唤逻辑
        if (SkipSpecialAbility) return;

        // 清理已死亡的召唤物计数
        // (简化处理：通过检查子对象数量)

        if (Time.time - _lastSummonTime >= _summonCooldown && _currentSummons < _maxSummons)
        {
            Summon();
            _lastSummonTime = Time.time;
        }
    }

    private void Summon()
    {
        Vector2 offset = Random.insideUnitCircle.normalized * _summonRadius;
        Vector2 spawnPos = (Vector2)transform.position + offset;

        var go = new GameObject("SummonedMinion");
        go.transform.position = spawnPos;
        go.transform.localScale = Vector3.one * 0.6f;
        go.tag = "Enemy";
        go.layer = gameObject.layer;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateMinionSprite();
        sr.color = _summonColor;
        sr.sortingOrder = 6;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = Vector2.one * 0.7f;

        go.AddComponent<BaseEntity>();

        var dmg = go.AddComponent<Damageable>();
        dmg.SetMaxHp(_summonHp);

        var kr = go.AddComponent<KillRewarder>();
        kr.SetRewards(3, 1);

        var enemy = go.AddComponent<EnemyBase>();
        enemy.MoveSpeed = _summonSpeed;

        // 监听死亡，减少计数（统一使用 BaseEntity.OnDeath）
        _currentSummons++;
        var summonEntity = go.GetComponent<BaseEntity>();
        if (summonEntity != null)
        {
            summonEntity.OnDeath += (pos) => { _currentSummons = Mathf.Max(0, _currentSummons - 1); };
        }

        DebugHelper.Log($"[SummonerEnemy] {gameObject.name} summoned a minion (total: {_currentSummons})");

        if (_sr != null)
        {
            _sr.color = _summonColor;
            Invoke(nameof(RestoreColor), 0.3f);
        }
    }

    private void RestoreColor()
    {
        if (_sr != null) _sr.color = _originalColor;
    }

    private static Sprite _cachedMinionSprite;
    private static Sprite CreateMinionSprite()
    {
        if (_cachedMinionSprite != null) return _cachedMinionSprite;
        var tex = new Texture2D(8, 8);
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(3.5f, 3.5f)) / 3.5f;
                tex.SetPixel(x, y, dist <= 0.8f ? Color.white : new Color(0, 0, 0, 0));
            }
        tex.Apply();
        _cachedMinionSprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
        return _cachedMinionSprite;
    }
}