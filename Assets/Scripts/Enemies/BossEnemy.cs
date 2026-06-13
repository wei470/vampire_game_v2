#pragma warning disable CS0414
using UnityEngine;

/// <summary>
/// Boss 敌人 — 状态机 + 阶段管理（精简版）。
///
/// 拆分职责：
/// - BossFactory: 预制体创建 + 类型选择 + 颜色配置
/// - BossAbilities: 所有攻击技能实现（弹幕/召唤/冲锋/毒区/震波等）
/// - BossPhaseHelper: 阶段更新 + 类型配置 + 视觉特效
/// - BossEnemy: 生命周期 + Update 协调 + 碰撞
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
    [SerializeField] internal int _bossContactDamage = 20;

    [Header("阶段 1 - 弹幕")]
    [SerializeField] internal float _bulletBarrageInterval = 2f;
    [SerializeField] internal int _bulletBarrageCount = 8;
    [SerializeField] internal float _bulletSpeed = 6f;
    [SerializeField] internal int _bulletDamage = 10;

    [Header("阶段 2 - 召唤")]
    [SerializeField] internal float _summonInterval = 4f;
    [SerializeField] internal int _summonCount = 3;

    [Header("阶段 3 - 冲锋")]
    [SerializeField] internal float _chargeInterval = 3f;
    [SerializeField] internal float _chargeSpeed = 15f;
    [SerializeField] internal int _chargeDamage = 30;

    [Header("阶段 4 - 地形变化")]
    [SerializeField] internal float _poisonZoneInterval = 5f;
    [SerializeField] internal float _poisonZoneRadius = 3f;
    [SerializeField] internal int _poisonDamage = 5;
    [SerializeField] internal float _poisonDuration = 6f;
    [SerializeField] internal float _shockwaveInterval = 4f;
    [SerializeField] internal float _shockwaveRadius = 8f;
    [SerializeField] internal int _shockwaveDamage = 20;

    [Header("阶段 5 - 狂暴")]
    [SerializeField] internal float _enrageSpeedMult = 1.5f;
    [SerializeField] internal float _enrageAttackMult = 0.5f;
    [SerializeField] internal int _enrageChargeCount = 3;

    [Header("#36 Boss 多样性")]
    [SerializeField] internal BossType _bossType = BossType.Juggernaut;

    /// <summary>当前 Boss 类型</summary>
    public BossType Type => _bossType;
    public int CurrentPhase => _currentPhase;

    // ── 内部状态（internal for BossPhaseHelper access） ──
    internal Damageable _bossDamageable;
    internal int _currentPhase = 1;
    internal float _lastBarrageTime, _lastSummonTime, _lastChargeTime;
    internal float _lastPoisonTime, _lastShockwaveTime, _lastSplitTime;
    internal int _enrageChargesLeft;
    internal float _baseSpeed;
    internal bool _isCharging = false;
    internal Vector2 _chargeDirection;
    internal float _chargeEndTime;
    internal Transform _playerTransform;

    // #36 幽灵型
    internal bool _isInvisible = false;
    internal float _nextBlinkTime;
    internal float _blinkCooldown = 5f;
    internal float _invisibleDuration = 2f;

    // #36 狂战型
    internal float _splitBulletInterval = 3f;

    internal bool _enraged = false;

    // #37 阶段专属状态
    internal bool _berserkerLeaveFireTrail = false;
    internal bool _phase3AbilityUsed = false;
    internal bool _phase5AbilityTriggered = false;
    internal bool _berserkerUndyingActive = false;
    internal float _berserkerUndyingEndTime;

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

        BossPhaseHelper.ApplyBossTypeConfig(this);
        BossPhaseHelper.PlayEntranceEffects(this);

        DebugHelper.Log($"[BossEnemy] Boss spawned! Type={_bossType}, HP={_bossHP}");
        EventManager.TriggerBossSpawn(_bossType.ToString(), _bossHP);
    }

    protected override void OnDisable()
    {
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

        BossPhaseHelper.UpdatePhase(this, _bossDamageable.HpPercent);
        BossPhaseHelper.CheckEnrage(this);

        if (_isCharging)
        {
            BossPhaseHelper.HandleCharging(this);
            return;
        }

        switch (_currentPhase)
        {
            case 1: BossPhaseHelper.HandlePhase1(this); break;
            case 2: BossPhaseHelper.HandlePhase2(this); break;
            case 3: BossPhaseHelper.HandlePhase3(this); break;
            case 4: BossPhaseHelper.HandlePhase4(this); break;
            case 5: BossPhaseHelper.HandlePhase5(this); break;
        }
    }

    // ── Invoke 目标（保留为实例方法，供 Invoke("EndBlink") 调用） ──

    internal void EndBlink()
    {
        BossPhaseHelper.EndBlink(this);
    }

    // ── 碰撞 ──

    private void OnCollisionStay2D(Collision2D collision)
    {
        BossAbilities.HandleCollision(collision, _bossContactDamage, _chargeDamage, _isCharging);
    }

    // ── 静态工厂方法（委托到 BossFactory，保持外部调用兼容） ──

    public static BossType SelectBossTypeForWave(int waveNumber)
        => BossFactory.SelectBossTypeForWave(waveNumber);

    public static BossEnemy CreateBoss(Vector3 position, int hp = 500)
        => BossFactory.CreateBoss(position, hp);
}
