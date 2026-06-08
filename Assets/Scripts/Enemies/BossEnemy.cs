#pragma warning disable CS0414
using UnityEngine;

/// <summary>
/// Boss 敌人 — 状态机 + 阶段管理（精简版）。
///
/// 拆分职责：
/// - BossFactory: 预制体创建 + 类型选择 + 颜色配置
/// - BossAbilities: 所有攻击技能实现（弹幕/召唤/冲锋/毒区/震波等）
/// - BossEnemy: 阶段状态机 + 类型配置 + Update 协调
/// </summary>
public class BossEnemy : EnemyBase
{
    /// <summary>#36 Boss 类型枚举</summary>
    public enum BossType
    {
        Juggernaut,
        Sorcerer,
        Phantom,
        Berserker
    }

    [Header("Boss 配置")]
    [SerializeField] internal int _bossHP = 500;
    [SerializeField] internal float _bossSpeed = 2.5f;
    [SerializeField] private int _bossContactDamage = 20;

    [Header("阶段 1 - 弹幕")]
    [SerializeField] private float _bulletBarrageInterval = 2f;
    [SerializeField] internal int _bulletBarrageCount = 8;
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
    [SerializeField] private BossType _bossType = BossType.Juggernaut;

    /// <summary>当前 Boss 类型</summary>
    public BossType Type => _bossType;
    public int CurrentPhase => _currentPhase;

    // ── 内部状态 ──
    private Damageable _bossDamageable;
    private int _currentPhase = 1;
    private float _lastBarrageTime, _lastSummonTime, _lastChargeTime;
    private float _lastPoisonTime, _lastShockwaveTime, _lastSplitTime;
    private int _enrageChargesLeft;
    private float _baseSpeed;
    private bool _isCharging = false;
    private Vector2 _chargeDirection;
    private float _chargeEndTime;
    private Transform _playerTransform;

    // #36 幽灵型
    private bool _isInvisible = false;
    private float _nextBlinkTime;
    private float _blinkCooldown = 5f;
    private float _invisibleDuration = 2f;

    // #36 狂战型
    private float _splitBulletInterval = 3f;

    // #37 阶段专属状态
    private bool _berserkerLeaveFireTrail = false;
    private bool _phase3AbilityUsed = false;
    private bool _phase5AbilityTriggered = false;
    private bool _berserkerUndyingActive = false;
    private float _berserkerUndyingEndTime;

    private int _lastReportedHP = -1;

    // ── 生命周期 ──

    protected override void Start()
    {
        base.Start();
        _bossDamageable = GetComponent<Damageable>();
        if (_bossDamageable != null)
        {
            _bossDamageable.SetMaxHp(_bossHP);
            _bossDamageable.Heal(_bossHP);
        }

        Setup(_bossSpeed, _bossContactDamage, 0, 50);

        var player = GameReferences.Player;
        if (player != null) _playerTransform = player.transform;

        _lastBarrageTime = _lastSummonTime = _lastChargeTime = Time.time;
        _lastPoisonTime = _lastShockwaveTime = _lastSplitTime = Time.time;
        _nextBlinkTime = Time.time + _blinkCooldown;
        _baseSpeed = _bossSpeed;

        ApplyBossTypeConfig();

        // Boss出场增强：屏幕震动+闪光+音效
        PlaySpawnEntrance();

        DebugHelper.Log($"[BossEnemy] Boss spawned! Type={_bossType}, HP={_bossHP}");
        EventManager.TriggerBossSpawn(_bossType.ToString(), _bossHP);
    }

    protected override void OnDisable()
    {
        // Boss 被回收或销毁时，确保通知 BossHealthBarUI 隐藏
        if (_bossDamageable != null && _bossDamageable.CurrentHp <= 0)
        {
            EventManager.TriggerBossDeath(_bossType.ToString());
        }
        base.OnDisable();
    }

    private void Update()
    {
        if (_bossDamageable == null || _bossDamageable.CurrentHp <= 0)
        {
            if (_bossDamageable != null && _bossDamageable.CurrentHp <= 0 && _lastReportedHP > 0)
            {
                _lastReportedHP = 0;
                EventManager.TriggerBossDeath(_bossType.ToString());
            }
            return;
        }

        if (_bossDamageable.CurrentHp != _lastReportedHP)
        {
            _lastReportedHP = _bossDamageable.CurrentHp;
            EventManager.TriggerBossHPChanged(_lastReportedHP, _bossDamageable.MaxHp);
        }

        UpdatePhase(_bossDamageable.HpPercent);

        if (_isCharging)
        {
            var rb = GetComponent<Rigidbody2D>();
            BossAbilities.UpdateCharge(rb, _chargeDirection, _chargeSpeed);
            if (Time.time > _chargeEndTime)
            {
                _isCharging = false;
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }
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

    // ── 阶段状态机 ──

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
            EventManager.TriggerBossPhaseChange(_currentPhase, 5);

            if (_currentPhase == 5)
            {
                _bossSpeed = _baseSpeed * _enrageSpeedMult;
                Setup(_bossSpeed, _bossContactDamage, 0, 50);
            }
        }
    }

    private void UpdatePhase1()
    {
        if (_bossType == BossType.Phantom) UpdatePhantomBlink();
        if (_bossType == BossType.Berserker) UpdateSplitBullets();

        if (Time.time - _lastBarrageTime > _bulletBarrageInterval)
        {
            BossAbilities.FireBarrage(transform.position, _bulletBarrageCount, _bulletSpeed, _bulletDamage, _playerTransform);
            _lastBarrageTime = Time.time;
        }
    }

    private void UpdatePhase2()
    {
        if (_bossType == BossType.Phantom) UpdatePhantomBlink();
        if (_bossType == BossType.Berserker) UpdateSplitBullets();

        if (Time.time - _lastBarrageTime > _bulletBarrageInterval * 0.7f)
        {
            BossAbilities.FireBarrage(transform.position, _bulletBarrageCount, _bulletSpeed, _bulletDamage, _playerTransform);
            _lastBarrageTime = Time.time;
        }
        if (Time.time - _lastSummonTime > _summonInterval)
        {
            BossAbilities.SummonMinions(transform.position, _summonCount, gameObject.layer);
            _lastSummonTime = Time.time;
        }
    }

    private void UpdatePhase3()
    {
        if (_bossType == BossType.Phantom) UpdatePhantomBlink();
        if (_bossType == BossType.Berserker) UpdateSplitBullets();

        if (Time.time - _lastBarrageTime > _bulletBarrageInterval * 0.5f)
        {
            BossAbilities.FireBarrage(transform.position, _bulletBarrageCount, _bulletSpeed, _bulletDamage, _playerTransform);
            _lastBarrageTime = Time.time;
        }
        if (Time.time - _lastChargeTime > _chargeInterval)
        {
            StartCharge();
            _lastChargeTime = Time.time;
        }
        if (!_phase3AbilityUsed)
        {
            _phase3AbilityUsed = true;
            ExecutePhase3Ability();
        }
    }

    private void UpdatePhase4()
    {
        if (_bossType == BossType.Phantom) UpdatePhantomBlink();
        if (_bossType == BossType.Berserker) UpdateSplitBullets();

        if (Time.time - _lastBarrageTime > _bulletBarrageInterval * 0.6f)
        {
            BossAbilities.FireBarrage(transform.position, _bulletBarrageCount, _bulletSpeed, _bulletDamage, _playerTransform);
            _lastBarrageTime = Time.time;
        }
        if (Time.time - _lastPoisonTime > _poisonZoneInterval)
        {
            BossAbilities.SpawnPoisonZone(transform.position, _poisonDamage, _poisonDuration, _poisonZoneRadius);
            _lastPoisonTime = Time.time;
        }
        if (Time.time - _lastShockwaveTime > _shockwaveInterval)
        {
            BossAbilities.FireShockwave(transform.position, _shockwaveDamage, _shockwaveRadius);
            _lastShockwaveTime = Time.time;
        }
    }

    private void UpdatePhase5()
    {
        if (!_phase5AbilityTriggered)
        {
            _phase5AbilityTriggered = true;
            ExecutePhase5Ability();
        }

        if (Time.time - _lastBarrageTime > _bulletBarrageInterval * _enrageAttackMult)
        {
            BossAbilities.FireBarrage(transform.position, _bulletBarrageCount, _bulletSpeed, _bulletDamage, _playerTransform);
            _lastBarrageTime = Time.time;
        }

        if (_enrageChargesLeft <= 0 && Time.time - _lastChargeTime > _chargeInterval * 0.8f)
            _enrageChargesLeft = _enrageChargeCount;
        if (_enrageChargesLeft > 0 && !_isCharging && Time.time - _lastChargeTime > 0.5f)
        {
            StartCharge();
            _lastChargeTime = Time.time;
            _enrageChargesLeft--;
        }

        if (Time.time - _lastPoisonTime > _poisonZoneInterval * 0.6f)
        {
            BossAbilities.SpawnPoisonZone(transform.position, _poisonDamage, _poisonDuration, _poisonZoneRadius);
            _lastPoisonTime = Time.time;
        }

        // Berserker 不死状态
        if (_bossType == BossType.Berserker && _bossDamageable != null && _bossDamageable.HpPercent < 0.2f && !_berserkerUndyingActive)
        {
            _berserkerUndyingActive = true;
            _berserkerUndyingEndTime = Time.time + 5f;
            DebugHelper.Log("[BossEnemy] Berserker Phase 5: UNDYING STATE!");
        }
        if (_berserkerUndyingActive)
        {
            if (_bossDamageable != null) _bossDamageable.Heal(1);
            if (Time.time >= _berserkerUndyingEndTime)
            {
                _berserkerUndyingActive = false;
                DebugHelper.Log("[BossEnemy] Berserker: Undying state ended!");
            }
        }
    }

    // ── 阶段专属技能（委托） ──

    private void ExecutePhase3Ability()
    {
        switch (_bossType)
        {
            case BossType.Juggernaut:
                BossAbilities.ExecuteJuggernautPhase3(transform.position, gameObject.layer);
                break;
            case BossType.Sorcerer:
                BossAbilities.ExecuteSorcererPhase3(transform.position);
                break;
            case BossType.Phantom:
                _invisibleDuration = 3f;
                _blinkCooldown = 3f;
                DebugHelper.Log("[BossEnemy] Phantom Phase 3: Enhanced stealth!");
                break;
            case BossType.Berserker:
                _berserkerLeaveFireTrail = true;
                DebugHelper.Log("[BossEnemy] Berserker Phase 3: Fire trail on charge!");
                break;
        }
    }

    private void ExecutePhase5Ability()
    {
        switch (_bossType)
        {
            case BossType.Juggernaut:
                _bossSpeed = _baseSpeed * 2f;
                _chargeInterval = 0.5f;
                Setup(_bossSpeed, _bossContactDamage, 0, 50);
                DebugHelper.Log("[BossEnemy] Juggernaut Phase 5: BERSERK!");
                break;
            case BossType.Sorcerer:
                BossAbilities.ExecuteSorcererPhase5(transform.position, _bulletBarrageCount, _bulletSpeed, _bulletDamage,
                    _playerTransform, _summonCount, _shockwaveDamage, _shockwaveRadius, gameObject.layer);
                break;
            case BossType.Phantom:
                BossAbilities.ExecutePhantomPhase5(this, _bossDamageable, _bossSpeed, _bulletBarrageCount);
                break;
            case BossType.Berserker:
                DebugHelper.Log("[BossEnemy] Berserker Phase 5: Undying mode ready!");
                break;
        }
    }

    // ── 类型配置 ──

    private void ApplyBossTypeConfig()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = BossFactory.BossColors[(int)_bossType];

        switch (_bossType)
        {
            case BossType.Juggernaut:
                _bossHP = Mathf.RoundToInt(_bossHP * 1.5f);
                _bossSpeed *= 0.8f;
                _chargeSpeed *= 1.3f;
                _chargeDamage = Mathf.RoundToInt(_chargeDamage * 1.3f);
                if (_bossDamageable != null) { _bossDamageable.SetMaxHp(_bossHP); _bossDamageable.Heal(_bossHP); }
                transform.localScale = Vector3.one * 2.5f;
                break;
            case BossType.Sorcerer:
                _bulletBarrageCount = 12;
                _bulletSpeed *= 1.3f;
                _summonCount = 5;
                _shockwaveRadius *= 1.3f;
                break;
            case BossType.Phantom:
                _bossSpeed *= 1.3f;
                _bulletBarrageCount = 16;
                _bossHP = Mathf.RoundToInt(_bossHP * 0.7f);
                if (_bossDamageable != null) { _bossDamageable.SetMaxHp(_bossHP); _bossDamageable.Heal(_bossHP); }
                if (sr != null) { var c = sr.color; c.a = 0.6f; sr.color = c; }
                break;
            case BossType.Berserker:
                _bossSpeed *= 1.5f;
                _chargeInterval *= 0.6f;
                _enrageSpeedMult = 2f;
                _enrageChargeCount = 5;
                _bossHP = Mathf.RoundToInt(_bossHP * 0.8f);
                if (_bossDamageable != null) { _bossDamageable.SetMaxHp(_bossHP); _bossDamageable.Heal(_bossHP); }
                break;
        }

        Setup(_bossSpeed, _bossContactDamage, 0, 50);
    }

    // ── 冲锋 ──

    private void StartCharge()
    {
        var result = BossAbilities.StartCharge(transform.position, _playerTransform);
        _chargeDirection = result.dir;
        _chargeEndTime = result.endTime;
        _isCharging = result.endTime > Time.time;
    }

    /// <summary>
    /// Boss出场效果：屏幕震动+红色闪光+专属音效+血条显示
    /// </summary>
    private void PlaySpawnEntrance()
    {
        // 屏幕震动
        var cam = GameReferences.MainCamera;
        if (cam != null)
        {
            var shake = cam.GetComponent<ScreenShake>();
            if (shake != null) shake.Shake(3f, 1f);
        }

        // 红色闪光
        DamageFlashEffect.Show(0.2f, new Color(1f, 0.1f, 0.1f, 0.4f));

        // Boss音效
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayLevelUp();

        // 浮字通知
        var player = GameReferences.Player;
        if (player != null)
        {
            DamagePopup.Create(
                player.transform.position + Vector3.up * 5f,
                0,
                BossFactory.BossColors[(int)_bossType],
                false,
                $"★ BOSS: {_bossType}!"
            );
        }

        // 通知小地图
        var minimap = FindAnyObjectByType<MinimapUI>();
        if (minimap != null) minimap.NotifyBossSpawned();
    }

    /// <summary>
    /// 阶段切换增强效果：屏幕震动+闪光+阶段提示
    /// </summary>
    private void PlayPhaseTransitionEffect(int phase)
    {
        // 屏幕震动（阶段越高震动越强）
        var cam = GameReferences.MainCamera;
        if (cam != null)
        {
            var shake = cam.GetComponent<ScreenShake>();
            if (shake != null)
            {
                float intensity = 1.5f + phase * 0.5f;
                float duration = 0.3f + phase * 0.1f;
                shake.Shake(intensity, duration);
            }
        }

        // 阶段闪光（不同阶段不同颜色）
        Color flashColor;
        switch (phase)
        {
            case 2: flashColor = new Color(1f, 0.5f, 0f, 0.3f); break;   // 橙色
            case 3: flashColor = new Color(1f, 0.2f, 0f, 0.35f); break;  // 红色
            case 4: flashColor = new Color(0.8f, 0f, 0.8f, 0.3f); break; // 紫色
            case 5: flashColor = new Color(1f, 0f, 0f, 0.5f); break;     // 深红（狂暴）
            default: flashColor = new Color(1f, 1f, 0f, 0.2f); break;    // 黄色
        }
        DamageFlashEffect.Show(0.15f, flashColor);

        // 浮字提示
        var player = GameReferences.Player;
        if (player != null)
        {
            string phaseText = phase == 5 ? "★ ENRAGE!" : $"Phase {phase}";
            DamagePopup.Create(
                player.transform.position + Vector3.up * 4f,
                0,
                flashColor,
                false,
                phaseText
            );
        }

        DebugHelper.Log($"[BossEnemy] Phase {phase} transition effect played");
    }

    /// <summary>
    /// Boss死亡增强效果：慢动作特写+大量粒子+金币爆发
    /// </summary>
    private void PlayDeathEffect()
    {
        // 慢动作特写（0.5秒慢动作）
        Time.timeScale = 0.3f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // 恢复正常速度
        StartCoroutine(RestoreTimeScaleAfterDelay(0.5f));

        // 大范围爆炸效果
        CombatManager.CreateExplosionEffect(transform.position, 8f,
            BossFactory.BossColors[(int)_bossType], 1f);

        // 屏幕震动
        var cam = GameReferences.MainCamera;
        if (cam != null)
        {
            var shake = cam.GetComponent<ScreenShake>();
            if (shake != null) shake.Shake(5f, 1.5f);
        }

        // 金色闪光
        DamageFlashEffect.Show(0.3f, new Color(1f, 0.85f, 0f, 0.5f));

        // Boss死亡音效
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayLevelUp();

        // 注：金币/经验掉落由 KillRewarder 统一处理
        DebugHelper.Log("[BossEnemy] Boss death effect played");
    }

    private System.Collections.IEnumerator RestoreTimeScaleAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    // ── 碰撞 ──

    private void OnCollisionStay2D(Collision2D collision)
    {
        BossAbilities.HandleCollision(collision, _bossContactDamage, _chargeDamage, _isCharging);
    }

    // ── 幽灵型闪现 ──

    private void UpdatePhantomBlink()
    {
        if (_isInvisible)
        {
            BossAbilities.UpdateInvisibleMovement(this, _playerTransform, _bossSpeed);
            return;
        }
        if (Time.time >= _nextBlinkTime)
        {
            _isInvisible = true;
            BossAbilities.StartBlink(this, _playerTransform, _invisibleDuration);
            Invoke(nameof(EndBlink), _invisibleDuration);
        }
    }

    private void EndBlink()
    {
        _isInvisible = false;
        BossAbilities.EndBlink(this);
        BossAbilities.FireBarrage(transform.position, _bulletBarrageCount, _bulletSpeed, _bulletDamage, _playerTransform);
        _nextBlinkTime = Time.time + _blinkCooldown;
    }

    // ── 狂战型分裂弹幕 ──

    private void UpdateSplitBullets()
    {
        if (Time.time - _lastSplitTime > _splitBulletInterval)
        {
            BossAbilities.FireSplitBarrage(transform.position, _playerTransform, _bulletSpeed, _bulletDamage);
            _lastSplitTime = Time.time;
        }
    }

    // ── 静态工厂方法（委托到 BossFactory，保持外部调用兼容） ──

    public static BossType SelectBossTypeForWave(int waveNumber)
        => BossFactory.SelectBossTypeForWave(waveNumber);

    public static BossEnemy CreateBoss(Vector3 position, int hp = 500)
        => BossFactory.CreateBoss(position, hp);
}