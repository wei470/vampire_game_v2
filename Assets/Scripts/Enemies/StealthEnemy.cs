using UnityEngine;

/// <summary>
/// 隐身敌人 - 周期性隐身，接近玩家时现身攻击。
/// 对应 Python 版的 Stealth 敌人
/// 
/// 行为：追踪玩家 → 周期性隐身（半透明）→ 接近后现身 → 攻击造成高伤害
/// </summary>
public class StealthEnemy : EnemyBase
{
    [Header("隐身属性")]
    [SerializeField] private float _stealthCooldown = 6f;     // 隐身冷却
    [SerializeField] private float _stealthDuration = 3f;     // 隐身持续时间
    [SerializeField] private float _revealDistance = 2.5f;    // 现身距离
    [SerializeField] private int _stealthDamage = 20;         // 隐身攻击伤害
    [SerializeField] private float _stealthSpeedMult = 1.4f;  // 隐身时加速

    private enum StealthState { Visible, Stealthed, Attacking }

    private StealthState _state = StealthState.Visible;
    private Transform _target;
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private float _stateTimer;
    private float _lastStealthTime;
    private Color _originalColor;
    private float _originalSpeed;

    protected override void Awake()
    {
        base.Awake();
        // #11 StealthEnemy DOT 抗性预设：燃烧抗性+50%，中毒弱点-20%
        var res = GetComponent<EnemyDotResistance>();
        if (res == null) res = gameObject.AddComponent<EnemyDotResistance>();
        EnemyDotResistance.ApplyStealthPreset(res);
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _originalColor = _sr.color;
        _originalSpeed = MoveSpeed;
    }

    private new void Start()
    {
        var player = GameReferences.Player;
        if (player != null) _target = player.transform;
        _lastStealthTime = Time.time;
    }

    private void FixedUpdate()
    {
        if (!Alive || _target == null) return;

        Vector2 dir = (_target.position - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, _target.position);

        // #15 距离 LOD：远距离简化为普通移动，跳过隐身逻辑
        if (SkipSpecialAbility)
        {
            _rb.linearVelocity = dir * MoveSpeed;
            return;
        }

        switch (_state)
        {
            case StealthState.Visible:
                _rb.linearVelocity = dir * MoveSpeed;

                // 冷却结束后进入隐身
                if (Time.time - _lastStealthTime >= _stealthCooldown)
                {
                    EnterStealth();
                }
                break;

            case StealthState.Stealthed:
                _rb.linearVelocity = dir * MoveSpeed * _stealthSpeedMult;

                // 接近玩家时现身攻击
                if (dist <= _revealDistance)
                {
                    _state = StealthState.Attacking;
                    _stateTimer = 0.3f;

                    // 现身并攻击
                    if (_sr != null)
                    {
                        Color c = _originalColor;
                        c.a = 1f;
                        _sr.color = c;
                    }

                    var dmg = _target.GetComponent<Damageable>();
                    if (dmg != null && dmg.CurrentHp > 0)
                    {
                        dmg.TakeDamage(_stealthDamage);
                        DebugHelper.Log($"[StealthEnemy] {gameObject.name} attacked from stealth for {_stealthDamage} damage!");
                    }
                }

                // 隐身持续时间结束
                _stateTimer -= Time.fixedDeltaTime;
                if (_stateTimer <= 0)
                {
                    ExitStealth();
                }
                break;

            case StealthState.Attacking:
                _rb.linearVelocity = dir * MoveSpeed * 0.3f;
                _stateTimer -= Time.fixedDeltaTime;
                if (_stateTimer <= 0)
                {
                    _state = StealthState.Visible;
                    _lastStealthTime = Time.time;
                }
                break;
        }
    }

    private void EnterStealth()
    {
        _state = StealthState.Stealthed;
        _stateTimer = _stealthDuration;

        // 视觉：半透明
        if (_sr != null)
        {
            Color c = _originalColor;
            c.a = 0.2f;
            _sr.color = c;
        }

        DebugHelper.Log($"[StealthEnemy] {gameObject.name} entered stealth");
    }

    private void ExitStealth()
    {
        _state = StealthState.Visible;
        _lastStealthTime = Time.time;

        if (_sr != null) _sr.color = _originalColor;

        DebugHelper.Log($"[StealthEnemy] {gameObject.name} exited stealth");
    }
}