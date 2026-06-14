using UnityEngine;

/// <summary>
/// 风化效果 — 挂载到敌人身上。
///
/// 命中追踪：
/// - 每被风子弹命中5次，叠加1层风化
/// - 每层风化：击退距离+5%（基础2f，每层额外+5%距离）
/// - 每施加1层风化造成5点伤害（可叠加）
///
/// 视觉：敌人颜色逐渐变为淡蓝白色，层数越高越明显
/// 叠层文字由 DotStatusBar 统一管理
/// </summary>
public class WindErosionEffect : StackEffectBase
{
    private int _hitCount = 0;
    private int _windStacks = 0;
    private SpriteRenderer _sr;
    private Rigidbody2D _rb;
    private Color _originalColor;
    private DotColorBlender _blender;

    private const int HITS_PER_STACK = 1;
    private const int DAMAGE_PER_STACK = 5;
    private float _knockbackDistance = 1f;
    private int _maxStacks = 999;
    private static readonly Color WIND_COLOR = new Color(0.7f, 0.85f, 1f);
    private static readonly Color WIND_POPUP_COLOR = new Color(0.7f, 0.85f, 1f);

    public override int StackCount => _windStacks;
    public override StatusEffectType EffectType => StatusEffectType.WindErosion;
    public override bool IsActive => _windStacks > 0;

    public int WindStacks => _windStacks;
    public int HitCount => _hitCount;

    public void RegisterHit()
    {
        _hitCount++;
        AddWindStack();
    }

    public override bool ConsumeStack()
    {
        if (_windStacks <= 0) return false;
        _windStacks--;
        DebugHelper.Log($"[WindErosion] Stack consumed! Remaining={_windStacks}");
        if (_windStacks <= 0) Cleanup();
        return true;
    }

    private void AddWindStack()
    {
        if (_windStacks >= _maxStacks) return;
        _windStacks++;

        if (_damageable == null) _damageable = GetComponent<Damageable>();
        if (_damageable != null && _damageable.CurrentHp > 0)
        {
            _damageable.TakeDamage(DAMAGE_PER_STACK, WIND_POPUP_COLOR);
        }

        ApplyKnockback();

        DebugHelper.Log($"[WindErosion] Stack added! Total={_windStacks}, Hits={_hitCount}, Knockback={GetKnockbackForce():F1}");
    }

    public float GetKnockbackForce()
    {
        return _knockbackDistance;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _hitCount = 0;
        _windStacks = 0;
        _lastRegisteredStacks = -1;
        _sr = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        if (_sr != null) _originalColor = _sr.color;
        _blender = GetComponent<DotColorBlender>();
        RefreshFromConfig();
    }

    private int _lastRegisteredStacks = -1;

    private void Update()
    {
        if (_windStacks <= 0) return;
        if (IsDead()) { Cleanup(); return; }

        if (_blender != null && _windStacks != _lastRegisteredStacks)
        {
            _lastRegisteredStacks = _windStacks;
            float intensity = Mathf.Clamp01(_windStacks / 10f);
            _blender.RegisterDot("wind", WIND_COLOR, intensity, 8f);
        }
    }

    private void ApplyKnockback()
    {
        if (IsDead()) return;

        var player = GameReferences.Player;
        if (player == null) return;

        Vector2 knockDir = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;
        float dist = GetKnockbackForce();
        if (_rb != null)
            _rb.MovePosition(_rb.position + knockDir * dist);
        else
            transform.position += (Vector3)(knockDir * dist);
    }

    private void Cleanup()
    {
        UnregisterColor();
        Destroy(this);
    }

    private void UnregisterColor()
    {
        if (_blender != null) _blender.UnregisterDot("wind");
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        UnregisterColor();
        _windStacks = 0;
        if (_sr != null) _sr.color = _originalColor;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        UnregisterColor();
        if (_sr != null) _sr.color = _originalColor;
    }

    protected override void RefreshFromConfig()
    {
        var cfg = DotEffectConfig.GetDefault();
        _knockbackDistance = cfg.WindErosionKnockbackDistance;
        _maxStacks = cfg.WindMaxStacks;
    }
}
