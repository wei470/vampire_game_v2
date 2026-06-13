using UnityEngine;

/// <summary>
/// 光明标记效果 — 挂载到敌人身上
/// 每层受到伤害增加0.5%，无上限
/// 叠层文字由 DotStatusBar 统一管理
/// </summary>
public class LightMarkEffect : MonoBehaviour, IStackEffect
{
    private float _duration = 15f;
    private int _stackCount = 0;
    private float _lastStackTime;
    private Damageable _damageable;
    private SpriteRenderer _sr;
    private float _damagePerStack = 0.005f;

    public int StackCount => _stackCount;
    public bool ConsumeStack() => false;
    public StatusEffectType EffectType => StatusEffectType.Light;
    public bool IsActive => _stackCount > 0;

    public void AddStack(float duration, int maxStacks)
    {
        _duration = duration;
        _lastStackTime = Time.time;
        _stackCount++;
        _damageable = GetComponent<Damageable>();

        if (_sr != null)
        {
            float brightness = Mathf.Min(0.3f, _stackCount * 0.02f);
            _sr.color = Color.Lerp(_sr.color, Color.white, brightness);
        }
    }

    public float GetDamageMultiplier()
    {
        if (_stackCount <= 0) return 1f;
        return 1f + _stackCount * _damagePerStack;
    }

    private void Start()
    {
        _damageable = GetComponent<Damageable>();
        _sr = GetComponent<SpriteRenderer>();
        _lastStackTime = Time.time;
        RefreshFromConfig();
        DotEffectConfig.OnConfigChanged += RefreshFromConfig;
    }

    private void LateUpdate()
    {
        if (_stackCount > 0 && Time.time - _lastStackTime > _duration)
        {
            _stackCount = 0;
            if (_sr != null) _sr.color = Color.white;
            Destroy(this);
            return;
        }
        if (_damageable != null && _damageable.CurrentHp <= 0)
        {
            Destroy(this);
        }
    }

    private void OnDisable()
    {
        DotEffectConfig.OnConfigChanged -= RefreshFromConfig;
        _stackCount = 0;
        if (_sr != null) _sr.color = Color.white;
    }

    private void OnDestroy()
    {
        if (_sr != null) _sr.color = Color.white;
    }

    private void RefreshFromConfig()
    {
        var cfg = DotEffectConfig.GetDefault();
        _damagePerStack = cfg.LightMarkDamagePerStack;
    }
}
