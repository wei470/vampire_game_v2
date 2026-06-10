using UnityEngine;

/// <summary>
/// 冲锋敌人 - 锁定玩家后快速冲锋。
/// 对应 Python 版的 Charger 敌人
/// 
/// 行为：慢速追踪 → 蓄力 → 高速冲锋 → 冲锋后短暂停顿
/// </summary>
public class ChargerEnemy : EnemyBase
{
    [Header("冲锋属性")]
    [SerializeField] private float _chargeRange = 8f;       // 触发冲锋距离
    [SerializeField] private float _chargeSpeed = 12f;      // 冲锋速度
    [SerializeField] private float _windupTime = 0.8f;      // 蓄力时间
    [SerializeField] private float _chargeDuration = 0.6f;  // 冲锋持续时间
    [SerializeField] private float _stunDuration = 1f;      // 冲锋后眩晕
    [SerializeField] private float _chargeCooldown = 3f;    // 冲锋冷却
    [SerializeField] private int _chargeDamage = 25;        // 冲锋碰撞伤害

    private enum ChargerState { Chase, Windup, Charging, Stunned }

    private ChargerState _state = ChargerState.Chase;
    private float _stateTimer;
    private float _lastChargeTime;
    private Vector2 _chargeDirection;
    private SpriteRenderer _sr;

    protected override void Awake()
    {
        base.Awake();
        _sr = GetComponent<SpriteRenderer>();
    }

    protected override void Start()
    {
        base.Start();
        var player = GameReferences.Player;
        if (player != null) _target = player.transform;
        _lastChargeTime = -_chargeCooldown;
    }

    private void FixedUpdate()
    {
        if (!Alive || _target == null) return;

        switch (_state)
        {
            case ChargerState.Chase:
                Vector2 dir = (_target.position - transform.position).normalized;
                _rb.linearVelocity = dir * MoveSpeed * 0.6f;

                float dist = Vector3.Distance(transform.position, _target.position);
                if (dist <= _chargeRange && Time.time - _lastChargeTime >= _chargeCooldown)
                {
                    _state = ChargerState.Windup;
                    _stateTimer = _windupTime;
                    _rb.linearVelocity = Vector2.zero;
                    _chargeDirection = (_target.position - transform.position).normalized;

                    if (_sr != null) _sr.color = Color.red;
                }
                break;

            case ChargerState.Windup:
                _rb.linearVelocity = Vector2.zero;
                _stateTimer -= Time.fixedDeltaTime;

                // 蓄力闪烁
                if (_sr != null)
                {
                    float flash = Mathf.Sin(Time.time * 20f) > 0 ? 1f : 0.3f;
                    _sr.color = new Color(1f, flash, flash);
                }

                if (_stateTimer <= 0)
                {
                    _state = ChargerState.Charging;
                    _stateTimer = _chargeDuration;
                }
                break;

            case ChargerState.Charging:
                _rb.linearVelocity = _chargeDirection * _chargeSpeed;
                _stateTimer -= Time.fixedDeltaTime;

                if (_stateTimer <= 0)
                {
                    _state = ChargerState.Stunned;
                    _stateTimer = _stunDuration;
                    _rb.linearVelocity = Vector2.zero;
                    _lastChargeTime = Time.time;
                    if (_sr != null) _sr.color = Color.gray;
                }
                break;

            case ChargerState.Stunned:
                _rb.linearVelocity = Vector2.zero;
                _stateTimer -= Time.fixedDeltaTime;

                if (_stateTimer <= 0)
                {
                    _state = ChargerState.Chase;
                    if (_sr != null) _sr.color = new Color(0.9f, 0.3f, 0.3f);
                }
                break;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_state != ChargerState.Charging) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            var dmg = collision.gameObject.GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp > 0)
            {
                dmg.TakeDamage(_chargeDamage);
                DebugHelper.Log($"[ChargerEnemy] Hit player for {_chargeDamage} charge damage!");
            }
            // 冲锋撞到玩家后停止
            _state = ChargerState.Stunned;
            _stateTimer = _stunDuration;
            _rb.linearVelocity = Vector2.zero;
            _lastChargeTime = Time.time;
        }
    }

    private new void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.9f, 0.3f, 0.3f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _chargeRange);
    }
}