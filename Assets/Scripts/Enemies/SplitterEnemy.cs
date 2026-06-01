using UnityEngine;

/// <summary>
/// 分裂敌人 - 死亡时分裂成多个小敌人。
/// 对应 Python 版的 Splitter 敌人
/// 
/// 行为：普通追踪 → 死亡时生成2-3个小分裂体
/// </summary>
public class SplitterEnemy : EnemyBase
{
    [Header("分裂属性")]
    [SerializeField] private int _splitCount = 3;           // 分裂个数
    [SerializeField] private float _splitScale = 0.5f;       // 分裂体大小
    [SerializeField] private float _splitSpeedMult = 1.3f;   // 分裂体速度倍率
    [SerializeField] private float _splitHpMult = 0.4f;      // 分裂体HP倍率
    [SerializeField] private bool _isSplit = false;          // 是否已是分裂体

    private Damageable _damageable;

    protected override void Awake()
    {
        base.Awake();
        _damageable = GetComponent<Damageable>();
    }

    protected override void OnEnable()
    {
        // 调用基类注册死亡事件
        base.OnEnable();
        // 额外订阅分裂逻辑
        OnDeath += OnSplitterDeathHandler;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        OnDeath -= OnSplitterDeathHandler;
    }

    private void OnSplitterDeathHandler(Vector3 deathPosition)
    {
        if (_isSplit) return; // 分裂体不再分裂

        DebugHelper.Log($"[SplitterEnemy] {gameObject.name} splitting into {_splitCount} pieces");

        for (int i = 0; i < _splitCount; i++)
        {
            // 随机偏移位置
            Vector2 offset = Random.insideUnitCircle * 1f;
            Vector2 spawnPos = (Vector2)transform.position + offset;

            var go = new GameObject($"SplitPiece_{i}");
            go.transform.position = spawnPos;
            go.transform.localScale = Vector3.one * _splitScale;
            go.tag = "Enemy";
            go.layer = gameObject.layer;

            // SpriteRenderer
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSplitSprite();
            sr.color = new Color(0.8f, 0.4f, 0.8f); // 紫色
            sr.sortingOrder = 7;

            // Physics
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one * 0.8f;

            // Damageable
            var dmg = go.AddComponent<Damageable>();
            // HP 会通过 SetMaxHp 设置

            // KillRewarder
            var kr = go.AddComponent<KillRewarder>();
            kr.SetRewards(5, 2); // 分裂体奖励少

            // BaseEntity
            go.AddComponent<BaseEntity>();

            // SplitterEnemy (标记为分裂体)
            var splitter = go.AddComponent<SplitterEnemy>();
            splitter._isSplit = true;
            splitter._splitCount = 0; // 不再分裂

            // 设置属性
            var dmgComp = go.GetComponent<Damageable>();
            if (dmgComp != null)
            {
                int splitHp = Mathf.Max(1, Mathf.RoundToInt(_damageable.MaxHp * _splitHpMult));
                dmgComp.SetMaxHp(splitHp);
            }

            var enemyBase = go.GetComponent<EnemyBase>();
            if (enemyBase != null)
            {
                enemyBase.MoveSpeed = MoveSpeed * _splitSpeedMult;
            }
        }
    }

    private static Sprite _cachedSplitSprite;
    private static Sprite CreateSplitSprite()
    {
        if (_cachedSplitSprite != null) return _cachedSplitSprite;
        var tex = new Texture2D(8, 8);
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(3.5f, 3.5f)) / 3.5f;
                tex.SetPixel(x, y, dist <= 1f ? Color.white : new Color(0, 0, 0, 0));
            }
        tex.Apply();
        _cachedSplitSprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
        return _cachedSplitSprite;
    }
}