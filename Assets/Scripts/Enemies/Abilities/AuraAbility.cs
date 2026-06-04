using UnityEngine;

/// <summary>
/// 光环型能力基类 — 封装 Healer/Enhancer/Shielder 的通用光环模式。
/// 
/// 通用行为：
/// - 创建光环视觉效果（EnemyEffectHelper.UpdateCircleAura）
/// - 周期性对范围内友军施加效果
/// - 施放时闪光
/// - 自动清理光环
/// 
/// 子类只需实现 ApplyEffectToAlly() 和 OnAuraUpdate()。
/// </summary>
public abstract class AuraAbility : EnemyAbilityBase
{
    [Header("光环参数")]
    [SerializeField] protected float _auraRadius = 4f;
    [SerializeField] protected float _auraAlpha = 0.15f;
    [SerializeField] protected float _moveSpeedMultiplier = 0.6f;

    private GameObject _auraGo;
    private string _auraName;
    private float _originalMoveSpeed;
    private bool _speedModified;

    protected override void Awake()
    {
        base.Awake();
        _auraName = GetType().Name + "Aura";
    }

    protected void OnEnable()
    {
        // 光环创建（对象池兼容：在 OnEnable 中）
        _auraGo = EnemyEffectHelper.UpdateCircleAura(
            transform, _auraGo, _auraName,
            _effectColor, _auraRadius, alpha: _auraAlpha, sortingOrder: 4);

        // 降低移动速度（通过修改 EnemyBase.MoveSpeed）
        if (Owner != null && !_speedModified)
        {
            _originalMoveSpeed = Owner.MoveSpeed;
            Owner.MoveSpeed *= _moveSpeedMultiplier;
            _speedModified = true;
        }
    }

    private void OnDisable()
    {
        if (_auraGo != null) { Destroy(_auraGo); _auraGo = null; }

        // 恢复移动速度
        if (Owner != null && _speedModified)
        {
            Owner.MoveSpeed = _originalMoveSpeed;
            _speedModified = false;
        }
    }

    /// <summary>
    /// 冷却就绪时执行光环效果。
    /// </summary>
    protected override void OnCooldownReady()
    {
        Collider2D[] hits = FindNearbyAllies(_auraRadius);
        int affectedCount = 0;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            if (hit.gameObject == gameObject) continue;

            if (ApplyEffectToAlly(hit))
                affectedCount++;
        }

        if (affectedCount > 0)
        {
            DebugHelper.Log($"[{GetType().Name}] {gameObject.name} affected {affectedCount} allies");
            EnemyEffectHelper.CreatePulseEffect(transform.position, _effectColor, _auraRadius * 0.4f, 0.5f);
            FlashColor(_effectColor);
        }
    }

    /// <summary>
    /// 对单个友军施加效果。子类实现具体逻辑。
    /// </summary>
    /// <returns>是否成功施加效果</returns>
    protected abstract bool ApplyEffectToAlly(Collider2D ally);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(_effectColor.r, _effectColor.g, _effectColor.b, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _auraRadius);
    }
}