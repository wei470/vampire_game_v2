using UnityEngine;

/// <summary>
/// 霜冻效果 — 永久减速（30%基础 + 每层额外5%，最高90%减速），持续直到敌人死亡
/// 不再冻住敌人，只降低移动速度并叠层
/// </summary>
public class FrostEffect : StackEffectBase
{
    public float slowPercent;
    public float frostDps;
    public bool canCrit; public float critChance, critMult;
    private EnemyBase _enemyBase;
    private SpriteRenderer _sr;
    private Color _originalColor;
    private DotColorBlender _blender;
    private int _frostStacks = 0;
    private bool _blenderUnregistered;

    private float _baseSlow = 0.05f;
    private float _perStackSlow = 0.05f;
    private float _maxSlow = 0.50f;
    private int _maxStacks = 10;
    private float _coldEmbraceDps;
    private float _frostTickAccumulator;
    private const float FROST_TICK_INTERVAL = 1f;

    public float EffectiveFrostDps => frostDps > 0f ? frostDps : _coldEmbraceDps;

    public override int StackCount => _frostStacks;
    public override StatusEffectType EffectType => StatusEffectType.Frostbite;
    public override bool IsActive => _frostStacks > 0;

    /// <summary>
    /// 施加霜冻减速 — 通过 EnemyBase.FrostSlowMultiplier 集中管理
    /// </summary>
    public void ApplyFreeze(float freezeDuration, float slowPercent, float frostDps,
        bool canCrit, float critChance, float critMult)
    {
        if (_frostStacks >= _maxStacks) return;
        this.frostDps = Mathf.Max(this.frostDps, frostDps);
        this.canCrit = canCrit; this.critChance = critChance; this.critMult = critMult;
        _frostStacks++;
        this.slowPercent = Mathf.Min(_maxSlow, _baseSlow + (_frostStacks - 1) * _perStackSlow);

        EnsureColorCaptured();

        if (_enemyBase == null) _enemyBase = GetComponent<EnemyBase>();
        if (_enemyBase != null)
        {
            _enemyBase.FrostSlowMultiplier = 1f - this.slowPercent;
        }

        if (GetComponent<EnemySpeedBar>() == null) gameObject.AddComponent<EnemySpeedBar>();
    }

    public int FrostStacks => _frostStacks;

    /// <summary>
    /// 消耗一层霜冻（用于元素反应：霜电）。返回是否成功消耗。
    /// </summary>
    public override bool ConsumeStack()
    {
        if (_frostStacks <= 0) return false;
        _frostStacks--;
        slowPercent = Mathf.Min(_maxSlow, _baseSlow + (_frostStacks - 1) * _perStackSlow);
        if (_frostStacks <= 0) slowPercent = 0f;
        DebugHelper.Log($"[FrostEffect] Stack consumed! Remaining={_frostStacks}");
        return true;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        slowPercent = 0f;
        _frostStacks = 0;
        frostDps = 0f;
        _coldEmbraceDps = 0f;
        _frostTickAccumulator = 0f;
        _lastSyncedStacks = -1;
        _blenderUnregistered = false;
        if (_enemyBase == null) _enemyBase = GetComponent<EnemyBase>();
        if (_enemyBase != null) _enemyBase.FrostSlowMultiplier = 1f;
        _blender = GetComponent<DotColorBlender>();
        EnsureColorCaptured();
        RefreshFromConfig();
    }

    private void EnsureColorCaptured()
    {
        if (_sr == null)
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _originalColor = _sr.color;
        }
    }

    private int _lastSyncedStacks = -1;

    private void Update()
    {
        if (IsDead()) { Cleanup(); return; }

        if (_frostStacks > 0 && _enemyBase != null && _frostStacks != _lastSyncedStacks)
        {
            _lastSyncedStacks = _frostStacks;
            _enemyBase.FrostSlowMultiplier = 1f - slowPercent;
        }

        // 绝对零度（frost_absolute）：frostDps > 0 时霜冻才造成伤害，每层叠加
        float activeFrostDps = frostDps > 0f ? frostDps : _coldEmbraceDps;
        if (activeFrostDps > 0f && _frostStacks > 0 && _damageable != null)
        {
            _frostTickAccumulator += Time.deltaTime;
            if (_frostTickAccumulator >= FROST_TICK_INTERVAL)
            {
                _frostTickAccumulator -= FROST_TICK_INTERVAL;
                float dmg = activeFrostDps * _frostStacks;
                if (canCrit && Random.value < critChance) dmg *= critMult;
                _damageable.TakeDamage(Mathf.Max(0.01f, dmg), DotColorBlender.FROST_BLUE);
            }
        }
    }

    private void LateUpdate()
    {
        if (_frostStacks <= 0 || _sr == null) return;
        if (IsDead()) return;

        if (!_blenderUnregistered && _blender != null)
        {
            _blender.UnregisterDot("frost");
            _blenderUnregistered = true;
        }

        float intensity = Mathf.Clamp01(_frostStacks / 34f);
        _sr.color = Color.Lerp(_originalColor, DotColorBlender.FROST_BLUE, intensity);
    }

    private void Cleanup()
    {
        if (_enemyBase != null) _enemyBase.FrostSlowMultiplier = 1f;
        if (_sr != null) _sr.color = _originalColor;
        UnregisterColor();
        Destroy(this);
    }
    private void UnregisterColor() { if (_blender != null) _blender.UnregisterDot("frost"); }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (_enemyBase != null && _frostStacks > 0) _enemyBase.FrostSlowMultiplier = 1f;
        if (_sr != null && _frostStacks > 0) _sr.color = _originalColor;
        UnregisterColor();
    }

    protected override void OnDestroy() { base.OnDestroy(); UnregisterColor(); }

    protected override void RefreshFromConfig()
    {
        var cfg = DotEffectConfig.GetDefault();
        _maxSlow = cfg.FrostMaxSlow;
        _baseSlow = cfg.FrostBaseSlowPct;
        _perStackSlow = cfg.FrostSlowPerStack;
        _maxStacks = cfg.FrostMaxStacks;

        // 应用减速强化
        var mage = GameReferences.DotCharacterPassive as MagePassive;
        if (mage != null)
        {
            if (mage.FrostMaxSlowBonus > 0)
                _maxSlow = Mathf.Min(0.95f, _maxSlow + mage.FrostMaxSlowBonus);
            if (mage.FrostPerStackSlowBonus > 0)
                _perStackSlow += mage.FrostPerStackSlowBonus;
            if (mage.ColdEmbrace)
                _coldEmbraceDps = 3f;
        }
    }
}
