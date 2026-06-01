#pragma warning disable CS0414
using UnityEngine;

/// <summary>
/// Boss 敌人 — 5 阶段行为，每 5 波出现。
/// 阶段 1（HP > 66%）：追踪 + 碰撞伤害 + 随机发射弹幕
/// 阶段 2（33% < HP <= 66%）：加速 + 召唤小怪
/// 阶段 3（15% < HP <= 33%）：狂暴模式 + 冲锋
/// 阶段 4（8% < HP <= 15%）：减速光环 + 地面毒区 + AoE 震波
/// 阶段 5（HP <= 8%）：全技能加速 + 连续冲锋 + 环形弹幕
/// 
/// 击败后掉落传说装备。
/// </summary>
public class BossEnemy : EnemyBase
{
    [Header("Boss 配置")]
    [SerializeField] private int _bossHP = 500;
    [SerializeField] private float _bossSpeed = 2.5f;
    [SerializeField] private int _bossContactDamage = 20;

    [Header("阶段 1 - 弹幕")]
    [SerializeField] private float _bulletBarrageInterval = 2f;
    [SerializeField] private int _bulletBarrageCount = 8;
    [SerializeField] private float _bulletSpeed = 6f;
    [SerializeField] private int _bulletDamage = 10;

    [Header("阶段 2 - 召唤")]
    [SerializeField] private float _summonInterval = 4f;
    [SerializeField] private int _summonCount = 3;

    [Header("阶段 3 - 冲锋")]
    [SerializeField] private float _chargeInterval = 3f;
    [SerializeField] private float _chargeSpeed = 15f;
    [SerializeField] private int _chargeDamage = 30;

    [Header("阶段 4 - 地形变化")]
    [SerializeField] private float _poisonZoneInterval = 5f;
    [SerializeField] private float _poisonZoneRadius = 3f;
    [SerializeField] private int _poisonDamage = 5;
    [SerializeField] private float _poisonDuration = 6f;
    [SerializeField] private float _shockwaveInterval = 4f;
    [SerializeField] private float _shockwaveRadius = 8f;
    [SerializeField] private int _shockwaveDamage = 20;

    [Header("阶段 5 - 狂暴")]
    [SerializeField] private float _enrageSpeedMult = 1.5f;
    [SerializeField] private float _enrageAttackMult = 0.5f;
    [SerializeField] private int _enrageChargeCount = 3;

    private Damageable _bossDamageable;
    private int _currentPhase = 1;
    private float _lastBarrageTime;
    private float _lastSummonTime;
    private float _lastChargeTime;
    private float _lastPoisonTime;
    private float _lastShockwaveTime;
    private int _enrageChargesLeft;
    private float _baseSpeed;
    private bool _isCharging = false;
    private Vector2 _chargeDirection;
    private float _chargeEndTime;
    private Transform _playerTransform;

    /// <summary>
    /// 当前 Boss 阶段 (1/2/3)
    /// </summary>
    public int CurrentPhase => _currentPhase;

    protected override void Start()
    {
        base.Start(); // 让 EnemyBase 设置追踪目标

        _bossDamageable = GetComponent<Damageable>();
        if (_bossDamageable != null)
        {
            _bossDamageable.SetMaxHp(_bossHP);
            _bossDamageable.Heal(_bossHP);
        }

        Setup(_bossSpeed, _bossContactDamage, 0, 50);

        var player = GameReferences.Player;
        if (player != null) _playerTransform = player.transform;

        _lastBarrageTime = Time.time;
        _lastSummonTime = Time.time;
        _lastChargeTime = Time.time;
        _lastPoisonTime = Time.time;
        _lastShockwaveTime = Time.time;
        _baseSpeed = _bossSpeed;

        DebugHelper.Log($"[BossEnemy] Boss spawned! HP={_bossHP}");
    }

    private void Update()
    {
        if (_bossDamageable == null || _bossDamageable.CurrentHp <= 0) return;

        float hpPercent = _bossDamageable.HpPercent;
        UpdatePhase(hpPercent);

        if (_isCharging)
        {
            UpdateCharge();
            return;
        }

        switch (_currentPhase)
        {
            case 1: UpdatePhase1(); break;
            case 2: UpdatePhase2(); break;
            case 3: UpdatePhase3(); break;
            case 4: UpdatePhase4(); break;
            case 5: UpdatePhase5(); break;
        }
    }

    private void UpdatePhase(float hpPercent)
    {
        int newPhase;
        if (hpPercent > 0.66f) newPhase = 1;
        else if (hpPercent > 0.33f) newPhase = 2;
        else if (hpPercent > 0.15f) newPhase = 3;
        else if (hpPercent > 0.08f) newPhase = 4;
        else newPhase = 5;

        if (newPhase != _currentPhase)
        {
            _currentPhase = newPhase;
            DebugHelper.Log($"[BossEnemy] Phase transition → Phase {_currentPhase}!");

            // 进入狂暴阶段时加速
            if (_currentPhase == 5)
            {
                _bossSpeed = _baseSpeed * _enrageSpeedMult;
                Setup(_bossSpeed, _bossContactDamage, 0, 50);
            }
        }
    }

    // ── 阶段 1：追踪 + 弹幕 ──
    private void UpdatePhase1()
    {
        if (Time.time - _lastBarrageTime > _bulletBarrageInterval)
        {
            FireBarrage();
            _lastBarrageTime = Time.time;
        }
    }

    // ── 阶段 2：追踪 + 弹幕 + 召唤小怪 ──
    private void UpdatePhase2()
    {
        if (Time.time - _lastBarrageTime > _bulletBarrageInterval * 0.7f)
        {
            FireBarrage();
            _lastBarrageTime = Time.time;
        }

        if (Time.time - _lastSummonTime > _summonInterval)
        {
            SummonMinions();
            _lastSummonTime = Time.time;
        }
    }

    // ── 阶段 3：狂暴 + 冲锋 + 弹幕 ──
    private void UpdatePhase3()
    {
        if (Time.time - _lastBarrageTime > _bulletBarrageInterval * 0.5f)
        {
            FireBarrage();
            _lastBarrageTime = Time.time;
        }

        if (Time.time - _lastChargeTime > _chargeInterval)
        {
            StartCharge();
            _lastChargeTime = Time.time;
        }
    }

    // ── 阶段 4：减速光环 + 毒区 + AoE 震波 ──
    private void UpdatePhase4()
    {
        // 继续弹幕
        if (Time.time - _lastBarrageTime > _bulletBarrageInterval * 0.6f)
        {
            FireBarrage();
            _lastBarrageTime = Time.time;
        }

        // 地面毒区
        if (Time.time - _lastPoisonTime > _poisonZoneInterval)
        {
            SpawnPoisonZone();
            _lastPoisonTime = Time.time;
        }

        // AoE 震波
        if (Time.time - _lastShockwaveTime > _shockwaveInterval)
        {
            FireShockwave();
            _lastShockwaveTime = Time.time;
        }
    }

    // ── 阶段 5：全技能加速 + 连续冲锋 + 环形弹幕 ──
    private void UpdatePhase5()
    {
        // 弹幕加速（环形弹幕数量翻倍）
        if (Time.time - _lastBarrageTime > _bulletBarrageInterval * _enrageAttackMult)
        {
            FireBarrage();
            _lastBarrageTime = Time.time;
        }

        // 连续冲锋
        if (_enrageChargesLeft <= 0 && Time.time - _lastChargeTime > _chargeInterval * 0.8f)
        {
            _enrageChargesLeft = _enrageChargeCount;
        }
        if (_enrageChargesLeft > 0 && !_isCharging && Time.time - _lastChargeTime > 0.5f)
        {
            StartCharge();
            _lastChargeTime = Time.time;
            _enrageChargesLeft--;
        }

        // 持续毒区
        if (Time.time - _lastPoisonTime > _poisonZoneInterval * 0.6f)
        {
            SpawnPoisonZone();
            _lastPoisonTime = Time.time;
        }
    }

    /// <summary>
    /// 在 Boss 周围生成毒区（持续伤害区域，改变地形外观）
    /// </summary>
    private void SpawnPoisonZone()
    {
        DebugHelper.Log($"[BossEnemy] Phase 4: Spawning poison zone!");

        Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle * 3f;
        var zone = new GameObject("BossPoisonZone");
        zone.transform.position = pos;
        zone.transform.localScale = Vector3.one * _poisonZoneRadius;

        var sr = zone.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = new Color(0.2f, 0.8f, 0f, 0.3f); // 半透明绿色毒区
        sr.sortingOrder = -1;

        var col = zone.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        var poissonComponent = zone.AddComponent<BossPoisonZone>();
        poissonComponent.Setup(_poisonDamage, _poisonDuration, 0.5f);

        Object.Destroy(zone, _poisonDuration);
    }

    /// <summary>
    /// 以自身为中心释放 AoE 震波
    /// </summary>
    private void FireShockwave()
    {
        DebugHelper.Log($"[BossEnemy] Phase 4: AoE Shockwave!");

        // 创建震波视觉效果
        var shockwave = new GameObject("BossShockwave");
        shockwave.transform.position = transform.position;
        shockwave.transform.localScale = Vector3.one * 0.5f;

        var sr = shockwave.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = new Color(1f, 0.5f, 0f, 0.6f);
        sr.sortingOrder = 1;

        var col = shockwave.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        var shockwaveComp = shockwave.AddComponent<BossShockwave>();
        shockwaveComp.Setup(_shockwaveDamage, _shockwaveRadius, 0.5f);

        Object.Destroy(shockwave, 0.8f);
    }

    // ── 弹幕攻击 ──
    private void FireBarrage()
    {
        if (_playerTransform == null) return;
        DebugHelper.Log($"[BossEnemy] Phase {_currentPhase}: Firing barrage ({_bulletBarrageCount} bullets)");

        float angleStep = 360f / _bulletBarrageCount;
        for (int i = 0; i < _bulletBarrageCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            var bulletGo = new GameObject($"BossBullet_{i}");
            bulletGo.transform.position = transform.position;
            bulletGo.tag = "Untagged"; // 敌人子弹不使用 Enemy tag

            var sr = bulletGo.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.color = new Color(1f, 0.2f, 0.8f); // 粉红色
            bulletGo.transform.localScale = Vector3.one * 0.3f;

            var rb = bulletGo.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearVelocity = dir * _bulletSpeed;

            var col = bulletGo.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.15f;

            // 敌人子弹伤害组件
            var eb = bulletGo.AddComponent<EnemyBullet>();
            // EnemyBullet uses default serialized damage (15)

            Object.Destroy(bulletGo, 8f);
        }
    }

    // ── 召唤小怪 ──
    private void SummonMinions()
    {
        DebugHelper.Log($"[BossEnemy] Phase 2: Summoning {_summonCount} minions!");
        for (int i = 0; i < _summonCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 2f;
            Vector2 spawnPos = (Vector2)transform.position + offset;

            var minion = new GameObject($"BossMinion_{i}");
            minion.transform.position = spawnPos;
            minion.tag = "Enemy";

            var sr = minion.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Square;
            sr.color = new Color(0.8f, 0f, 0.8f);

            var rb = minion.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = minion.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.6f, 0.6f);

            minion.AddComponent<BaseEntity>();
            var dmg = minion.AddComponent<Damageable>();
            dmg.SetMaxHp(30);
            dmg.Heal(30);
            minion.AddComponent<KillRewarder>();

            var enemyBase = minion.AddComponent<EnemyBase>();
            enemyBase.Setup(4f, 8, 1, 5);
            var player = GameReferences.Player;
            if (player != null) enemyBase.SetTarget(player.transform);
        }
    }

    // ── 冲锋 ──
    private void StartCharge()
    {
        if (_playerTransform == null) return;
        DebugHelper.Log($"[BossEnemy] Phase 3: Charging!");

        _isCharging = true;
        _chargeDirection = (_playerTransform.position - transform.position).normalized;
        _chargeEndTime = Time.time + 1f; // 冲锋持续 1 秒
    }

    private void UpdateCharge()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = _chargeDirection * _chargeSpeed;

        if (Time.time > _chargeEndTime)
        {
            _isCharging = false;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            DebugHelper.Log("[BossEnemy] Charge ended");
        }
    }

    // ── 碰撞伤害（冲锋时造成额外伤害）──
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var dmg = collision.gameObject.GetComponent<Damageable>();
            if (dmg != null)
            {
                int damage = _isCharging ? _chargeDamage : _bossContactDamage;
                dmg.TakeDamage(damage);
            }
        }
    }

    // ── Sprite 工具 ──
    private static Sprite _cachedCircleSprite;
    private static Sprite CreateCircleSprite()
    {
        if (_cachedCircleSprite != null) return _cachedCircleSprite;
        var tex = new Texture2D(8, 8);
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
            {
                float dx = x - 3.5f, dy = y - 3.5f;
                bool inside = (dx * dx + dy * dy) <= 16f;
                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        tex.Apply();
        _cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
        return _cachedCircleSprite;
    }

    // CreateSquareSprite 已迁移到 SpriteFactory.Square

    /// <summary>
    /// 静态工厂方法：在指定位置生成 Boss
    /// </summary>
    public static BossEnemy CreateBoss(Vector3 position, int hp = 500)
    {
        var go = new GameObject("BOSS");
        go.transform.position = position;
        go.tag = "Enemy";

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Square;
        sr.color = new Color(0.8f, 0f, 0f); // 深红色
        go.transform.localScale = Vector3.one * 2f; // Boss 更大

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 0.9f);

        go.AddComponent<BaseEntity>();
        var dmg = go.AddComponent<Damageable>();
        dmg.SetMaxHp(hp);
        dmg.Heal(hp);
        go.AddComponent<KillRewarder>();

        var boss = go.AddComponent<BossEnemy>();
        boss._bossHP = hp;

        return boss;
    }
}