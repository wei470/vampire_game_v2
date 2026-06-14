using UnityEngine;

/// <summary>
/// DOT 叠层效果统一接口。
/// 所有 StackEffect（Burn/Poison/Frost/Static/Wind/Dark/Light）实现此接口，
/// 使 DotStatusBar 可通过统一查询获取叠层信息。
/// </summary>
public interface IStackEffect
{
    int StackCount { get; }
    bool ConsumeStack();
    StatusEffectType EffectType { get; }
    bool IsActive { get; }
}

/// <summary>
/// DOT 叠层效果抽象基类 — 提供统一的生命周期管理。
///
/// 共享逻辑：
/// - OnEnable: 注册 DotEffectRegistry + 订阅 OnConfigChanged
/// - OnDisable: 取消订阅 OnConfigChanged
/// - OnDestroy: 注销 DotEffectRegistry
/// - 缓存 Damageable 引用
///
/// 子类职责：
/// - 实现具体的伤害/控制逻辑
/// - 管理自己的视觉效果（DotColorBlender）
/// - 实现 IStackEffect 接口方法
/// </summary>
public abstract class StackEffectBase : MonoBehaviour, IStackEffect
{
    protected Damageable _damageable;
    public float FrequencyMultiplier { get; set; } = 1f;

    public abstract int StackCount { get; }
    public abstract StatusEffectType EffectType { get; }
    public virtual bool IsActive => StackCount > 0;

    public abstract bool ConsumeStack();

    protected virtual void OnEnable()
    {
        _damageable = GetComponent<Damageable>();
        DotEffectRegistry.RegisterEffect(this);
        DotEffectConfig.OnConfigChanged += RefreshFromConfig;
    }

    protected virtual void OnDisable()
    {
        DotEffectConfig.OnConfigChanged -= RefreshFromConfig;
    }

    protected virtual void OnDestroy()
    {
        DotEffectRegistry.UnregisterEffect(this);
    }

    protected virtual void RefreshFromConfig() { }

    protected bool IsDead()
    {
        return _damageable != null && _damageable.CurrentHp <= 0;
    }
}
