using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 敌人AI行为系统 — 根据条件切换行为模式。
/// 挂在敌人上，覆盖 EnemyBase 的默认移动。
/// 非 Chase 行为通过直接设置 Rigidbody2D.linearVelocity 覆盖 EnemyBase.FixedUpdate 的移动。
/// </summary>
public class EnemyAIBehavior : MonoBehaviour
{
    public enum Behavior { Chase, Flee, Charge, Circle }

    [Header("调试")]
    [SerializeField] private Behavior _current = Behavior.Chase;

    private EnemyBase _enemyBase;
    private Damageable _damageable;
    private Rigidbody2D _rb;
    private float _chargeEndTime;
    private float _chargeCooldownEnd;
    private float _circleAngle;
    private float _lastFleeHeal;
    private bool _isCharging;
    private float _chargeDirX;
    private float _chargeDirY;

    private const float FLEE_HP_THRESHOLD = 0.2f;
    private const float CHARGE_MIN_DIST = 5f;
    private const float CHARGE_MAX_DIST = 10f;
    private const float CHARGE_DURATION = 2f;
    private const float CHARGE_COOLDOWN = 5f;
    private const float CHARGE_SPEED_MULT = 2f;
    private const float CIRCLE_RADIUS = 6f;
    private const float CIRCLE_SPEED = 90f;
    private const int CIRCLE_MIN_ENEMIES = 3;
    private const float NEARBY_CHECK_RADIUS = 5f;
    private const float FLEE_SPEED_MULT = 0.8f;

    private static readonly List<Collider2D> _overlapBuffer = new List<Collider2D>(16);

    private void Awake()
    {
        _enemyBase = GetComponent<EnemyBase>();
        _damageable = GetComponent<Damageable>();
        _rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        _current = Behavior.Chase;
        _isCharging = false;
        _circleAngle = 0f;
        _lastFleeHeal = 0f;
        _chargeCooldownEnd = 0f;
    }

    private void FixedUpdate()
    {
        if (_enemyBase == null || _damageable == null || _rb == null) return;
        if (_damageable.CurrentHp <= 0) return;

        var player = GameReferences.Player;
        if (player == null) return;

        Vector2 pos = transform.position;
        Vector2 playerPos = player.transform.position;
        float dist = Vector2.Distance(pos, playerPos);
        float hpPct = (float)_damageable.CurrentHp / _damageable.MaxHp;

        UpdateBehavior(dist, hpPct);
        ApplyBehavior(playerPos, dist);
    }

    private void UpdateBehavior(float dist, float hpPct)
    {
        // Priority: Flee > Charge > Circle > Chase
        if (_isCharging)
        {
            if (Time.time >= _chargeEndTime)
            {
                _isCharging = false;
                _current = Behavior.Chase;
            }
            return;
        }

        if (hpPct < FLEE_HP_THRESHOLD)
        {
            _current = Behavior.Flee;
        }
        else if (dist >= CHARGE_MIN_DIST && dist <= CHARGE_MAX_DIST
                 && Time.time >= _chargeCooldownEnd)
        {
            _current = Behavior.Charge;
            _isCharging = true;
            _chargeEndTime = Time.time + CHARGE_DURATION;
            _chargeCooldownEnd = Time.time + CHARGE_COOLDOWN + CHARGE_DURATION;

            var player = GameReferences.Player;
            if (player != null)
            {
                Vector2 dir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
                _chargeDirX = dir.x;
                _chargeDirY = dir.y;
            }
        }
        else if (CountNearbyEnemies() >= CIRCLE_MIN_ENEMIES)
        {
            _current = Behavior.Circle;
        }
        else
        {
            _current = Behavior.Chase;
        }
    }

    private void ApplyBehavior(Vector2 playerPos, float dist)
    {
        switch (_current)
        {
            case Behavior.Chase:
                // EnemyBase.FixedUpdate handles default chase movement
                break;
            case Behavior.Flee:
                ApplyFlee(playerPos);
                break;
            case Behavior.Charge:
                ApplyCharge();
                break;
            case Behavior.Circle:
                ApplyCircle(playerPos);
                break;
        }
    }

    private void ApplyFlee(Vector2 playerPos)
    {
        Vector2 dir = ((Vector2)transform.position - playerPos).normalized;
        _rb.linearVelocity = dir * _enemyBase.BaseMoveSpeed * FLEE_SPEED_MULT;

        if (Time.time - _lastFleeHeal >= 1f)
        {
            _lastFleeHeal = Time.time;
            int heal = Mathf.Max(1, _damageable.MaxHp / 100);
            _damageable.Heal(heal);
        }
    }

    private void ApplyCharge()
    {
        _rb.linearVelocity = new Vector2(_chargeDirX, _chargeDirY)
                             * _enemyBase.BaseMoveSpeed * CHARGE_SPEED_MULT;
    }

    private void ApplyCircle(Vector2 playerPos)
    {
        _circleAngle += CIRCLE_SPEED * Time.fixedDeltaTime;
        float rad = _circleAngle * Mathf.Deg2Rad;
        Vector2 target = playerPos + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * CIRCLE_RADIUS;
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        _rb.linearVelocity = dir * _enemyBase.BaseMoveSpeed;
    }

    private int CountNearbyEnemies()
    {
        _overlapBuffer.Clear();
        int count = PhysicsHelper.OverlapCircle(transform.position, NEARBY_CHECK_RADIUS, _overlapBuffer);
        int result = 0;
        for (int i = 0; i < count; i++)
        {
            var col = _overlapBuffer[i];
            if (col.CompareTag("Enemy") && col.gameObject != gameObject) result++;
        }
        return result;
    }
}
