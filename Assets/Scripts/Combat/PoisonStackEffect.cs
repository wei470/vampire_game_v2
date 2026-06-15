using UnityEngine;

public class PoisonStackEffect : StackEffectBase
{
    private int _stacks;
    public bool canCrit; public float critChance, critMult;
    private float _tickAccumulator;
    private DotColorBlender _blender;
    private float _baseTickInterval = 1f;
    private int _damagePerTick = 2;
    private int _maxStacks = 999;
    private int _lastRegisteredStacks = -1;

    public override int StackCount => _stacks;
    public override StatusEffectType EffectType => StatusEffectType.Poison;
    public override bool IsActive => _stacks > 0;

    public void AddStack(float dps, float remainingTime, bool canCrit, float critChance, float critMult)
    {
        if (_stacks >= _maxStacks) return;
        _stacks++;
        this.canCrit = canCrit; this.critChance = critChance; this.critMult = critMult;
    }

    public override bool ConsumeStack() => false;

    protected override void OnEnable()
    {
        base.OnEnable();
        _blender = GetComponent<DotColorBlender>();
        _lastRegisteredStacks = -1;
        _tickAccumulator = 0f;
        RefreshFromConfig();
    }

    protected override void RefreshFromConfig()
    {
        var cfg = DotEffectConfig.GetDefault();
        _maxStacks = cfg.PoisonMaxStacks;
        _baseTickInterval = cfg.PoisonBaseTickInterval;

        // 应用毒素加速强化
        var mage = GameReferences.DotCharacterPassive as MagePassive;
        if (mage != null && mage.PoisonTickReduction > 0)
            _baseTickInterval *= (1f - mage.PoisonTickReduction);
    }

    private void Update()
    {
        if (_stacks <= 0) { Cleanup(); return; }
        if (IsDead()) { Cleanup(); return; }

        if (_blender != null && _stacks != _lastRegisteredStacks)
        {
            _lastRegisteredStacks = _stacks;
            float intensity = Mathf.Clamp01(_stacks / 10f);
            _blender.RegisterDot("poison", DotColorBlender.POISON_GREEN, intensity, 6f);
        }

        _tickAccumulator += Time.deltaTime;
        if (_tickAccumulator < _baseTickInterval) return;
        _tickAccumulator -= _baseTickInterval;

        float dmg = _damagePerTick + (_stacks - 1);
        if (canCrit && Random.value < critChance) dmg *= critMult;
        _damageable.TakeDamage(dmg, new Color(0.1f, 0.8f, 0.1f));
    }

    private void Cleanup() { UnregisterColor(); _stacks = 0; Destroy(this); }
    protected override void OnDestroy() { base.OnDestroy(); UnregisterColor(); }
    private void UnregisterColor() { if (_blender != null) _blender.UnregisterDot("poison"); }
}
