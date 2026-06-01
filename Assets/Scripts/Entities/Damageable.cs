using UnityEngine;
using System;

/// <summary>
/// 伤害组件，挂载到实体上使其可受伤。
/// 对应 Python: entities/base.py 中的 Damageable mixin
/// 
/// 使用方式：挂载到任何有 BaseEntity 组件的 GameObject 上
/// </summary>
public class Damageable : MonoBehaviour, IDamageable
{
    [Header("生命值")]
    [SerializeField] private int _maxHp = 100;
    [SerializeField] private int _currentHp;
    [SerializeField] private int _armor = 0;

    /// <summary>
    /// 当前 HP
    /// </summary>
    public int CurrentHp => _currentHp;

    /// <summary>
    /// 是否存活
    /// </summary>
    public bool IsAlive => _currentHp > 0;

    /// <summary>
    /// 最大 HP
    /// </summary>
    public int MaxHp => _maxHp;

    /// <summary>
    /// 护甲值
    /// </summary>
    public int Armor => _armor;

    /// <summary>
    /// 受伤事件，参数：(当前HP, 最大HP)
    /// </summary>
    public event Action<int, int> OnDamaged;

    /// <summary>
    /// 治疗事件，参数：(治疗量, 当前HP)
    /// </summary>
    public event Action<int, int> OnHealed;

    // 死亡事件已统一到 BaseEntity.OnDeath，不再在 Damageable 中重复定义

    [Header("对象池（可选）")]
    [SerializeField] private string _poolKey;

    /// <summary>
    /// 设置对象池键名（用于回收到 ObjectPool）
    /// </summary>
    public void SetPoolKey(string key) { _poolKey = key; }

    /// <summary>
    /// 获取对象池键名（用于回收判断）
    /// </summary>
    public string PoolKey => _poolKey;

    private BaseEntity _baseEntity;

    private void Awake()
    {
        _currentHp = _maxHp;
        _baseEntity = GetComponent<BaseEntity>();
    }

    /// <summary>
    /// 每次从对象池取出或首次激活时，重置 HP 到满血
    /// </summary>
    private void OnEnable()
    {
        _currentHp = _maxHp;
        _dead = false;
    }

    /// <summary>
    /// 标记是否已触发死亡流程（防止重复）
    /// </summary>
    private bool _dead = false;

    /// <summary>
    /// 每帧检查：如果 HP ≤ 0 但对象仍存活，强制触发死亡。
    /// 这是终极安全网，不依赖任何子类的 FixedUpdate/Update 调用链。
    /// </summary>
    private void LateUpdate()
    {
        if (!_dead && _currentHp <= 0 && gameObject.activeInHierarchy)
        {
            _dead = true;
            Die();
            // 如果 Die() 后对象仍未被销毁（事件链断裂），直接销毁
            if (gameObject.activeInHierarchy)
            {
                if (_baseEntity == null) _baseEntity = GetComponent<BaseEntity>();
                if (_baseEntity != null && _baseEntity.Alive)
                {
                    _baseEntity.Die();
                }
                if (gameObject.activeInHierarchy)
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    /// <summary>
    /// 受到伤害
    /// 伤害公式：actualDamage = max(1, damage - armor)
    /// </summary>
    /// <param name="damage">原始伤害值</param>
    public void TakeDamage(int damage)
    {
        if (_currentHp <= 0) return;

        // 闪避系统（P1-27）— 仅对玩家生效
        if (gameObject.CompareTag("Player"))
        {
            float dodgeChance = SaveManager.Instance?.GetPermanentBonus("dodge_chance") ?? 0f;
            if (dodgeChance > 0f && UnityEngine.Random.value < dodgeChance)
            {
                DebugHelper.Log($"[Damageable] {gameObject.name} dodged attack!");
                return;
            }
        }

        int actualDamage = Mathf.Max(1, damage - _armor);
        _currentHp = Mathf.Max(0, _currentHp - actualDamage);

        DebugHelper.Log($"[Damageable] {gameObject.name} took {actualDamage} damage (raw:{damage} - armor:{_armor}), HP: {_currentHp}/{_maxHp}");

        // 显示伤害数字（敌人受击时）
        if (!gameObject.CompareTag("Player"))
        {
            DamagePopup.Create(transform.position, actualDamage, false);
        }

        OnDamaged?.Invoke(_currentHp, _maxHp);

        // 仅玩家受伤时通知全局事件
        if (gameObject.CompareTag("Player"))
        {
            EventManager.TriggerPlayerDamaged(_currentHp, _maxHp);
        }

        if (_currentHp <= 0)
        {
            _dead = true; // 标记已死亡，防止 LateUpdate 重复触发
            Die();
        }
    }

    /// <summary>
    /// 治疗
    /// </summary>
    /// <param name="amount">治疗量</param>
    public void Heal(int amount)
    {
        if (_currentHp <= 0) return;

        int oldHp = _currentHp;
        _currentHp = Mathf.Min(_maxHp, _currentHp + amount);
        int actualHeal = _currentHp - oldHp;

        DebugHelper.Log($"[Damageable] {gameObject.name} healed {actualHeal} HP, now: {_currentHp}/{_maxHp}");

        // 显示治疗数字（玩家治疗时）
        if (gameObject.CompareTag("Player") && actualHeal > 0)
        {
            DamagePopup.Create(transform.position, actualHeal, false, true);
        }

        OnHealed?.Invoke(actualHeal, _currentHp);

        // 仅玩家治疗时通知全局事件
        if (gameObject.CompareTag("Player"))
        {
            EventManager.TriggerPlayerHealed(actualHeal, _currentHp);
        }
    }

    /// <summary>
    /// 设置最大 HP（不改变当前 HP 比例）
    /// </summary>
    public void SetMaxHp(int newMaxHp)
    {
        if (newMaxHp <= 0) newMaxHp = 1; // 防止除零

        if (_maxHp <= 0)
        {
            // 首次设置或旧值异常，直接赋满血
            _maxHp = newMaxHp;
            _currentHp = newMaxHp;
            return;
        }

        float ratio = (float)_currentHp / _maxHp;
        _maxHp = newMaxHp;
        _currentHp = Mathf.RoundToInt(_maxHp * ratio);
    }

    /// <summary>
    /// 设置护甲值
    /// </summary>
    public void SetArmor(int armor)
    {
        _armor = armor;
    }

    /// <summary>
    /// 死亡处理 — 统一由 BaseEntity.Die() 广播 OnDeath 事件
    /// </summary>
    private void Die()
    {
        DebugHelper.Log($"[Damageable] {gameObject.name} died!");

        // 懒加载 BaseEntity 引用（避免 SpawnCodeEnemy 中组件顺序导致 Awake 时取错实例）
        if (_baseEntity == null)
            _baseEntity = GetComponent<BaseEntity>();

        // 如果有 BaseEntity 组件，调用其 Die()（统一广播 OnDeath）
        if (_baseEntity != null)
        {
            _baseEntity.Die();
        }

        // 如果是玩家，通知全局
        if (gameObject.CompareTag("Player"))
        {
            EventManager.TriggerPlayerDeath();
        }
    }

    /// <summary>
    /// 重置 HP 到满
    /// </summary>
    public void ResetHp()
    {
        _currentHp = _maxHp;
    }

    /// <summary>
    /// HP 百分比（0-1）
    /// </summary>
    public float HpPercent => _maxHp > 0 ? (float)_currentHp / _maxHp : 0f;
}