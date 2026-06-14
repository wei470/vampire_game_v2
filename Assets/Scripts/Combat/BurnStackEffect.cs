using UnityEngine;

public class BurnStackEffect : StackEffectBase
{
    private int _stacks;
    public float baseDps;
    public float duration;
    public override int StackCount => _stacks;
    public override StatusEffectType EffectType => StatusEffectType.Burn;
    public override bool IsActive => _stacks > 0;
    public float endTime;
    public bool canCrit; public float critChance, critMult;
    private DotColorBlender _blender;
    private float _tickAccumulator;
    private int _lastRegisteredStacks = -1;

    private float _baseTickInterval = 0.5f;
    private float _stackBonus = 0.1f;

    public void AddStack(float baseDps, float duration, bool canCrit, float critChance, float critMult)
    {
        _stacks++;
        this.baseDps = Mathf.Max(this.baseDps, baseDps);
        this.duration = duration;
        endTime = Time.time + duration;
        this.canCrit = canCrit; this.critChance = critChance; this.critMult = critMult;
    }

    public override bool ConsumeStack() => false;

    protected override void OnEnable()
    {
        base.OnEnable();
        _blender = GetComponent<DotColorBlender>();
        _tickAccumulator = 0f;
        _lastRegisteredStacks = -1;
        RefreshFromConfig();
    }

    protected override void RefreshFromConfig()
    {
        var cfg = DotEffectConfig.GetDefault();
        duration = cfg.BurnDuration;
        _baseTickInterval = cfg.BurnBaseTickInterval;
    }

    private void Update()
    {
        if (IsDead() || _stacks <= 0) { _stacks = 0; UnregisterColor(); Destroy(this); return; }

        if (_blender != null && _stacks != _lastRegisteredStacks)
        {
            _lastRegisteredStacks = _stacks;
            float intensity = Mathf.Clamp01(_stacks / 10f);
            _blender.RegisterDot("burn", DotColorBlender.BURN_ORANGE, intensity, 10f);
        }

        _tickAccumulator += Time.deltaTime;
        if (_tickAccumulator < _baseTickInterval) return;
        _tickAccumulator -= _baseTickInterval;

        float stackMultiplier = 1f + (_stacks - 1) * _stackBonus;
        float dmg = baseDps * _baseTickInterval * stackMultiplier;
        if (canCrit && Random.value < critChance) dmg *= critMult;
        _damageable.TakeDamage(Mathf.Max(0.01f, dmg), new Color(1f, 0.5f, 0f));
    }

    protected override void OnDestroy() { base.OnDestroy(); UnregisterColor(); }
    private void UnregisterColor() { if (_blender != null) _blender.UnregisterDot("burn"); }
}
