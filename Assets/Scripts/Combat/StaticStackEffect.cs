using UnityEngine;

/// <summary>
/// 静电效果组件 — 挂在敌人身上，管理静电层数
///
/// 命中逻辑：
/// - 无层数时：立刻触发1秒静电 + 施加1层
/// - 有层数时：触发0.1秒静电 + 施加1层 + 重新计算冷却
///
/// 定时逻辑：
/// - 基础每5秒触发一次静电放电
/// - 每层减少0.2秒，最低2秒（15层叠满）
/// - 伤害：固定为0（纯控制效果）
/// </summary>
public class StaticStackEffect : StackEffectBase
{
    private int _stackCount = 0;
    private float _baseInterval = 5.0f;
    private float _stackReduction = 0.2f;
    private float _minInterval = 2.0f;
    private float _stunDurationHit = 0.1f;
    private float _stunDurationDischarge = 0.5f;
    private float _stunDurationFirst = 1.0f;
    private int _maxStacks = 999;
    private float _lastTickTime;
    private float _stunEndTime;
    private EnemyBase _enemyBase;
    private SpriteRenderer _sr;
    private Color _originalColor;
    private DotColorBlender _blender;

    public override int StackCount => _stackCount;
    public override StatusEffectType EffectType => StatusEffectType.Static;
    public override bool IsActive => _stackCount > 0;

    public override bool ConsumeStack()
    {
        if (_stackCount <= 0) return false;
        _stackCount--;
        if (_stackCount <= 0)
        {
            if (_enemyBase == null) _enemyBase = GetComponent<EnemyBase>();
            if (_enemyBase != null) _enemyBase.IsStaticStunned = false;
            _stunEndTime = 0f;
        }
        DebugHelper.Log($"[StaticStackEffect] Stack consumed! Remaining={_stackCount}");
        return true;
    }

    public bool HasStun() => Time.time < _stunEndTime;

    public void AddStack()
    {
        bool isFirstStack = (_stackCount == 0);
        _stackCount = Mathf.Min(_stackCount + 1, _maxStacks);

        if (isFirstStack)
            ApplyStun(_stunDurationFirst);
        else
            ApplyStun(_stunDurationHit);

        CombatManager.CreateExplosionEffect(transform.position, isFirstStack ? 1.2f : 0.6f, 
            new Color(0.4f, 0.8f, 1f), isFirstStack ? 0.4f : 0.2f);

        _lastTickTime = Time.time;
        DebugHelper.Log($"[StaticStackEffect] Stack added! Total={_stackCount}, Interval={GetInterval():F1}s, First={isFirstStack}");
    }

    public float GetInterval()
    {
        return Mathf.Max(_minInterval, _baseInterval - _stackCount * _stackReduction);
    }

    private void ApplyStun(float duration)
    {
        _stunEndTime = Time.time + duration;
        if (_enemyBase == null) _enemyBase = GetComponent<EnemyBase>();
        if (_enemyBase != null)
        {
            _enemyBase.IsStaticStunned = true;
        }
    }

    private void RestoreSpeedAfterStun()
    {
        if (_enemyBase == null) return;
        _enemyBase.IsStaticStunned = false;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _stackCount = 0;
        _lastTickTime = Time.time;
        _stunEndTime = 0f;
        if (_enemyBase == null) _enemyBase = GetComponent<EnemyBase>();
        if (_enemyBase != null) _enemyBase.IsStaticStunned = false;
        RefreshFromConfig();
    }

    private void Start()
    {
        if (_enemyBase == null) _enemyBase = GetComponent<EnemyBase>();
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _originalColor = _sr.color;
        _blender = GetComponent<DotColorBlender>();
        _lastTickTime = Time.time;
    }

    private void Update()
    {
        if (_stackCount <= 0) return;
        if (IsDead()) { Cleanup(); return; }

        if (_blender == null) _blender = GetComponent<DotColorBlender>();
        if (_blender != null)
        {
            float intensity = Mathf.Clamp01(_stackCount / 10f);
            float pulseSpeed = (Time.time < _stunEndTime) ? 20f : 12f;
            _blender.RegisterDot("static", DotColorBlender.STATIC_CYAN, intensity, pulseSpeed);
        }

        if (Time.time < _stunEndTime)
        {
            if (_enemyBase != null) _enemyBase.IsStaticStunned = true;
        }
        else
        {
            if (_enemyBase != null && _enemyBase.IsStaticStunned)
            {
                RestoreSpeedAfterStun();
            }
        }

        float interval = GetInterval();
        if (Time.time - _lastTickTime >= interval)
        {
            _lastTickTime = Time.time;
            TriggerStaticDischarge();
        }
    }

    private void TriggerStaticDischarge()
    {
        if (IsDead()) return;

        ApplyStun(_stunDurationDischarge);
        CombatManager.CreateExplosionEffect(transform.position, 1f, new Color(0.4f, 0.8f, 1f), 0.3f);
        DebugHelper.Log($"[StaticStackEffect] Static discharge! stacks={_stackCount}, next in {GetInterval():F1}s");
    }

    private void Cleanup()
    {
        if (_enemyBase != null) RestoreSpeedAfterStun();
        UnregisterColor();
        Destroy(this);
    }
    private void UnregisterColor() { if (_blender != null) _blender.UnregisterDot("static"); }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (_enemyBase != null && _stackCount > 0) RestoreSpeedAfterStun();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_enemyBase != null && _stackCount > 0) RestoreSpeedAfterStun();
        UnregisterColor();
    }

    protected override void RefreshFromConfig()
    {
        var cfg = DotEffectConfig.GetDefault();
        _baseInterval = cfg.StaticBaseInterval;
        _stackReduction = cfg.StaticStackReduction;
        _minInterval = cfg.StaticMinInterval;
        _stunDurationHit = cfg.StaticStunOnHit;
        _stunDurationDischarge = cfg.StaticStunOnDischarge;
        _stunDurationFirst = cfg.StaticStunOnFirstStack;
        _maxStacks = cfg.StaticMaxStacks;
    }
}
