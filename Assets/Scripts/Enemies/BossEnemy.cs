#pragma warning disable CS0414
using UnityEngine;

/// <summary>
/// Boss 敌人 — #36 多样化 Boss 系统。
///
/// 4 种 Boss 类型（根据波次自动选择）：
/// - Juggernaut（重装）：高血量、冲锋为主、地面毒区
/// - Sorcerer（法师）：弹幕为主、召唤小怪、AoE 震波
/// - Phantom（幽灵）：隐身闪现、环形弹幕、减速光环
/// - Berserker（狂战）：高速连续冲锋、狂暴加速、分裂弹幕
///
/// 每种类型从技能池中随机选取 2-3 个技能作为主动技能。
/// 击败后掉落传说装备。
/// </summary>
public class BossEnemy : EnemyBase
{
    /// <summary>
    /// #36 Boss 类型枚举
    /// </summary>
    public enum BossType
    {
        Juggernaut,  // 重装型：高HP、冲锋+毒区
        Sorcerer,    // 法师型：弹幕+召唤+震波
        Phantom,     // 幽灵型：隐身+闪现+环形弹幕
        Berserker    // 狂战型：高速冲锋+狂暴+分裂弹幕
    }

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

    [Header("#36 Boss 多样性")]
    [Tooltip("Boss 类型。0=自动根据波次选择")]
    [SerializeField] private BossType _bossType = BossType.Juggernaut;

    /// <summary>当前 Boss 类型</summary>
    public BossType Type => _bossType;

    // #36 幽灵型专用状态
    private bool _isInvisible = false;
    private float _nextBlinkTime;
    private float _blinkCooldown = 5f;
    private float _invisibleDuration = 2f;

    // #36 狂战型专用：分裂弹幕
    private float _splitBulletInterval = 3f;
    private float _lastSplitTime;

    // #37 Boss 阶段专属机制状态
    private bool _berserkerLeaveFireTrail = false;
    private bool _phase5AbilityTriggered = false;

    // #36 Boss 类型颜色映射
    private static readonly Color[] BossColors = new Color[]
    {
        new Color(0.6f, 0.1f, 0.1f),   // Juggernaut: 深红
        new Color(0.4f, 0.1f, 0.6f),   // Sorcerer: 深紫
        new Color(0.3f, 0.5f, 0.6f),   // Phantom: 暗青
        new Color(0.8f, 0.3f, 0f),     // Berserker: 橙红
    };

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
        _lastSplitTime = Time.time;
        _nextBlinkTime = Time.time + _blinkCooldown;
        _baseSpeed = _bossSpeed;

        // #36 应用 Boss 类型特定参数
        ApplyBossTypeConfig();

        DebugHelper.Log($"[BossEnemy] Boss spawned! Type={_bossType}, HP={_bossHP}");

        // #21 触发 Boss 出现事件（BossHealthBarUI 监听）
        EventManager.TriggerBossSpawn(_bossType.ToString(), _bossHP);
    }

    /// <summary>
    /// #36 根据波次自动选择 Boss 类型
    /// </summary>
    public static BossType SelectBossTypeForWave(int waveNumber)
    {
        // 每5波出现一次Boss，根据波次决定类型
        int bossIndex = (waveNumber / 5) % 4;
        return (BossType)bossIndex;
    }

    /// <summary>
    /// #36 应用 Boss 类型特定的参数和视觉效果
    /// </summary>
    private void ApplyBossTypeConfig()
    {
        // 应用类型颜色
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = BossColors[(int)_bossType];

        switch (_bossType)
        {
            case BossType.Juggernaut:
                _bossHP = Mathf.RoundToInt(_bossHP * 1.5f); // +50% HP
                _bossSpeed *= 0.8f; // 慢速
                _chargeSpeed *= 1.3f; // 冲锋更快
                _chargeDamage = Mathf.RoundToInt(_chargeDamage * 1.3f);
                if (_bossDamageable != null) { _bossDamageable.SetMaxHp(_bossHP); _bossDamageable.Heal(_bossHP); }
                transform.localScale = Vector3.one * 2.5f; // 更大
                break;

            case BossType.Sorcerer:
                _bulletBarrageCount = 12; // 更多弹幕
                _bulletSpeed *= 1.3f;
                _summonCount = 5; // 更多小怪
                _shockwaveRadius *= 1.3f;
                break;

            case BossType.Phantom:
                _bossSpeed *= 1.3f; // 快速
                _bulletBarrageCount = 16; // 环形弹幕更多
                _bossHP = Mathf.RoundToInt(_bossHP * 0.7f); // 较少HP
                if (_bossDamageable != null) { _bossDamageable.SetMaxHp(_bossHP); _bossDamageable.Heal(_bossHP); }
                if (sr != null) { var c = sr.color; c.a = 0.6f; sr.color = c; } // 半透明
                break;

            case BossType.Berserker:
                _bossSpeed *= 1.5f; // 最快
                _chargeInterval *= 0.6f; // 冲锋更频繁
                _enrageSpeedMult = 2f; // 狂暴更快
                _enrageChargeCount = 5; // 更多连续冲锋
                _bossHP = Mathf.RoundToInt(_bossHP * 0.8f);
                if (_bossDamageable != null) { _bossDamageable.SetMaxHp(_bossHP); _bossDamageable.Heal(_bossHP); }
                break;
        }

        Setup(_bossSpeed, _bossContactDamage, 0, 50);
    }

    private int _lastReportedHP = -1; // #21 避免每帧触发事件

    private void Update()
    {
        if (_bossDamageable == null || _bossDamageable.CurrentHp <= 0)
        {
            // #21 Boss 死亡时触发事件（仅触发一次）
            if (_bossDamageable != null && _bossDamageable.CurrentHp <= 0 && _lastReportedHP > 0)
            {
                _lastReportedHP = 0;
                EventManager.TriggerBossDeath(_bossType.ToString());
            }
            return;
        }

        // #21 每帧报告 Boss HP 变化（仅在 HP 实际变化时触发）
        if (_bossDamageable.CurrentHp != _lastReportedHP)
        {
            _lastReportedHP = _bossDamageable.CurrentHp;
            EventManager.TriggerBossHPChanged(_lastReportedHP, _bossDamageable.MaxHp);
        }

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

            // #21 触发阶段切换事件
            EventManager.TriggerBossPhaseChange(_currentPhase, 5);

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
        // #36 幽灵型：隐身闪现
        if (_bossType == BossType.Phantom) UpdatePhantomBlink();

        // #36 狂战型：分裂弹幕
        if (_bossType == BossType.Berserker) UpdateSplitBullets();

        if (Time.time - _lastBarrageTime > _bulletBarrageInterval)
        {
            FireBarrage();
            _lastBarrageTime = Time.time;
        }
    }

    // ── 阶段 2：追踪 + 弹幕 + 召唤小怪 ──
    private void UpdatePhase2()
    {
        if (_bossType == BossType.Phantom) UpdatePhantomBlink();
        if (_bossType == BossType.Berserker) UpdateSplitBullets();

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

    // ── 阶段 3：狂暴 + 冲锋 + 弹幕 + 阶段专属技能 ──
    private bool _phase3AbilityUsed = false;

    private void UpdatePhase3()
    {
        if (_bossType == BossType.Phantom) UpdatePhantomBlink();
        if (_bossType == BossType.Berserker) UpdateSplitBullets();

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

        // #37 阶段 3 专属技能（首次进入时触发一次，之后周期触发）
        if (!_phase3AbilityUsed)
        {
            _phase3AbilityUsed = true;
            ExecutePhase3Ability();
        }
    }

    /// <summary>
    /// #37 阶段 3 专属技能 — 根据 Boss 类型执行不同技能
    /// </summary>
    private void ExecutePhase3Ability()
    {
        switch (_bossType)
        {
            case BossType.Juggernaut:
                // 召唤 2 个 TankEnemy 作为护盾
                DebugHelper.Log("[Boss] Juggernaut Phase 3: Summoning 2 TankEnemy shields!");
                for (int i = 0; i < 2; i++)
                {
                    Vector2 offset = Random.insideUnitCircle.normalized * 2f;
                    var minion = new GameObject($"JuggernautShield_{i}");
                    minion.transform.position = (Vector2)transform.position + offset;
                    minion.tag = "Enemy";
                    minion.layer = gameObject.layer;
                    minion.transform.localScale = Vector3.one * 0.8f;
                    var sr = minion.AddComponent<SpriteRenderer>();
                    sr.sprite = SpriteFactory.Hexagon;
                    sr.color = new Color(0.5f, 0.5f, 0.5f);
                    sr.sortingOrder = 7;
                    var rb = minion.AddComponent<Rigidbody2D>();
                    rb.gravityScale = 0f; rb.freezeRotation = true;
                    minion.AddComponent<BoxCollider2D>().isTrigger = true;
                    minion.AddComponent<BaseEntity>();
                    var dmg = minion.AddComponent<Damageable>();
                    dmg.SetMaxHp(80);
                    minion.AddComponent<KillRewarder>().SetRewards(10, 5);
                    minion.AddComponent<TankEnemy>();
                }
                break;

            case BossType.Sorcerer:
                // 释放"反魔法区域" — 区域内标记，玩家技能 CD 翻倍
                DebugHelper.Log("[Boss] Sorcerer Phase 3: Anti-magic zone deployed!");
                var zone = new GameObject("AntiMagicZone");
                zone.transform.position = transform.position;
                zone.transform.localScale = Vector3.one * 6f;
                var zoneSr = zone.AddComponent<SpriteRenderer>();
                zoneSr.sprite = CreateCircleSprite();
                zoneSr.color = new Color(0.6f, 0f, 0.8f, 0.15f);
                zoneSr.sortingOrder = -1;
                var zoneCol = zone.AddComponent<CircleCollider2D>();
                zoneCol.isTrigger = true;
                zoneCol.radius = 0.5f;
                var antiMagic = zone.AddComponent<BossPoisonZone>();
                antiMagic.Setup(0, 10f, 0.5f); // 不造成伤害，纯控制区
                Object.Destroy(zone, 10f);
                break;

            case BossType.Phantom:
                // 隐身时间延长 + 闪现距离增大
                _invisibleDuration = 3f;
                _blinkCooldown = 3f;
                DebugHelper.Log("[Boss] Phantom Phase 3: Enhanced stealth (3s invis, faster blink)!");
                break;

            case BossType.Berserker:
                // 每次冲锋后留下火焰路径
                DebugHelper.Log("[Boss] Berserker Phase 3: Fire trail on charge!");
                _berserkerLeaveFireTrail = true;
                break;
        }
    }

    // ── 阶段 4：减速光环 + 毒区 + AoE 震波 ──
    private void UpdatePhase4()
    {
        if (_bossType == BossType.Phantom) UpdatePhantomBlink();
        if (_bossType == BossType.Berserker) UpdateSplitBullets();

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

    // ── 阶段 5：全技能加速 + 连续冲锋 + 环形弹幕 + 阶段专属技能 ──
    private void UpdatePhase5()
    {
        // #37 阶段 5 专属技能（首次进入时触发）
        if (!_phase5AbilityTriggered)
        {
            _phase5AbilityTriggered = true;
            ExecutePhase5Ability();
        }

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

        // #37 Berserker 阶段 5：血量低于 20% 时锁定 HP 为 1（5 秒不死）
        if (_bossType == BossType.Berserker && _bossDamageable != null && _bossDamageable.HpPercent < 0.2f && !_berserkerUndyingActive)
        {
            _berserkerUndyingActive = true;
            _berserkerUndyingEndTime = Time.time + 5f;
            DebugHelper.Log("[Boss] Berserker Phase 5: UNDYING STATE! HP locked at 1 for 5 seconds!");
        }
        if (_berserkerUndyingActive)
        {
            if (_bossDamageable != null) _bossDamageable.Heal(1); // 锁定 HP
            if (Time.time >= _berserkerUndyingEndTime)
            {
                _berserkerUndyingActive = false;
                DebugHelper.Log("[Boss] Berserker: Undying state ended!");
            }
        }
    }

    /// <summary>
    /// #37 阶段 5 专属技能 — 根据 Boss 类型执行不同终极技能
    /// </summary>
    private void ExecutePhase5Ability()
    {
        switch (_bossType)
        {
            case BossType.Juggernaut:
                // 狂暴状态：速度 ×2，冲锋无冷却
                _bossSpeed = _baseSpeed * 2f;
                _chargeInterval = 0.5f;
                Setup(_bossSpeed, _bossContactDamage, 0, 50);
                DebugHelper.Log("[Boss] Juggernaut Phase 5: BERSERK! Speed x2, charge no cooldown!");
                break;

            case BossType.Sorcerer:
                // 同时释放环形弹幕 + 召唤 + 震波
                FireBarrage();
                SummonMinions();
                FireShockwave();
                DebugHelper.Log("[Boss] Sorcerer Phase 5: BARRAGE + SUMMON + SHOCKWAVE simultaneously!");
                break;

            case BossType.Phantom:
                // 分裂为 2 个幽灵分身（血量各 50%）
                DebugHelper.Log("[Boss] Phantom Phase 5: SPLIT into 2 phantoms!");
                if (_bossDamageable != null)
                {
                    int splitHp = _bossDamageable.MaxHp / 2;
                    var clone = CreateBoss(transform.position + Vector3.right * 2f, splitHp);
                    clone._bossType = BossType.Phantom;
                    clone._bossHP = splitHp;
                    clone._bossSpeed = _bossSpeed * 1.2f;
                    clone._bulletBarrageCount = 12;
                    var sr = clone.GetComponent<SpriteRenderer>();
                    if (sr != null) { var c = sr.color; c.a = 0.5f; sr.color = c; }
                    // 本体也减半
                    _bossDamageable.SetMaxHp(splitHp);
                    _bossDamageable.Heal(splitHp);
                }
                break;

            case BossType.Berserker:
                // 不死状态在 UpdatePhase5 中持续检查
                DebugHelper.Log("[Boss] Berserker Phase 5: Undying mode ready (activates below 20% HP)!");
                break;
        }
    }

    // #37 Berserker 不死状态
    private bool _berserkerUndyingActive = false;
    private float _berserkerUndyingEndTime;

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

    // ═══ #36 幽灵型专属：隐身闪现 ═══

    /// <summary>
    /// #36 幽灵型 Boss：周期性隐身+闪现到玩家附近
    /// 隐身期间不可被攻击，闪现后释放环形弹幕
    /// </summary>
    private void UpdatePhantomBlink()
    {
        if (_isInvisible)
        {
            // 隐身期间缓慢接近玩家
            if (_playerTransform != null)
            {
                Vector2 dir = (_playerTransform.position - transform.position).normalized;
                var rb = GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = dir * _bossSpeed * 0.5f;
            }
            return;
        }

        if (Time.time >= _nextBlinkTime)
        {
            StartBlink();
        }
    }

    private void StartBlink()
    {
        _isInvisible = true;

        // 视觉效果：完全透明
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) { var c = sr.color; c.a = 0.1f; sr.color = c; }

        // 禁用碰撞
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        DebugHelper.Log("[BossEnemy] Phantom: Invisible blink!");

        // 闪现到玩家附近
        if (_playerTransform != null)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * 3f;
            transform.position = _playerTransform.position + (Vector3)offset;
        }

        // 隐身结束后恢复
        Invoke(nameof(EndBlink), _invisibleDuration);
    }

    private void EndBlink()
    {
        _isInvisible = false;

        // 恢复视觉
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) { var c = sr.color; c.a = 0.6f; sr.color = c; }

        // 恢复碰撞
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        // 闪现后释放环形弹幕
        FireBarrage();

        _nextBlinkTime = Time.time + _blinkCooldown;
        DebugHelper.Log("[BossEnemy] Phantom: Reappeared!");
    }

    // ═══ #36 狂战型专属：分裂弹幕 ═══

    /// <summary>
    /// #36 狂战型 Boss：周期性发射分裂弹幕
    /// 子弹飞行一段距离后分裂成 3 发小子弹
    /// </summary>
    private void UpdateSplitBullets()
    {
        if (Time.time - _lastSplitTime > _splitBulletInterval)
        {
            FireSplitBarrage();
            _lastSplitTime = Time.time;
        }
    }

    private void FireSplitBarrage()
    {
        if (_playerTransform == null) return;

        DebugHelper.Log("[BossEnemy] Berserker: Split barrage!");

        // 向玩家方向发射 3 发分裂弹
        Vector2 baseDir = (_playerTransform.position - transform.position).normalized;
        for (int i = -1; i <= 1; i++)
        {
            float angle = i * 20f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(
                baseDir.x * Mathf.Cos(angle) - baseDir.y * Mathf.Sin(angle),
                baseDir.x * Mathf.Sin(angle) + baseDir.y * Mathf.Cos(angle)
            ).normalized;

            var bulletGo = new GameObject("SplitBullet");
            bulletGo.transform.position = transform.position;
            bulletGo.tag = "Untagged";

            var sr = bulletGo.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.color = new Color(1f, 0.5f, 0f); // 橙色
            bulletGo.transform.localScale = Vector3.one * 0.4f;

            var rb = bulletGo.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearVelocity = dir * _bulletSpeed * 1.2f;

            var col = bulletGo.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.2f;

            bulletGo.AddComponent<EnemyBullet>();

            // 1.5秒后分裂成 3 发小子弹
            var splitInfo = bulletGo.AddComponent<BossSplitBullet>();
            splitInfo.Init(_bulletSpeed * 0.8f, _bulletDamage);

            Object.Destroy(bulletGo, 8f);
        }
    }

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