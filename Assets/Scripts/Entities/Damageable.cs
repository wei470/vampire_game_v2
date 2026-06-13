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
    [SerializeField] private float _hpRegenPerSecond = 0f;

    // 护甲公式常量
    private const float FLAT_CAP = 0.5f;        // 固定减伤最多减 50%
    private const float PERCENT_BASE = 100f;     // 百分比减伤基数

    /// <summary>每秒HP回复（可被升级修改）</summary>
    public float HpRegenPerSecond
    {
        get => _hpRegenPerSecond;
        set => _hpRegenPerSecond = Mathf.Max(0f, value);
    }

    [Header("无敌帧（仅玩家）")]
    [SerializeField] private float _invincibleDuration = 0.5f;  // 无敌帧持续时间
    private float _invincibleUntil = -999f;                      // 无敌帧结束时间
    private SpriteRenderer _playerSr;                            // 缓存玩家 SpriteRenderer
    private float _flashTimer = 0f;                              // 闪烁计时器
    private const float FLASH_INTERVAL = 0.05f;                  // 闪烁间隔

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
    private LightMarkEffect _cachedLightMark;
    private DarkMarkEffect _cachedDarkMark;
    private HitFlashEffect _cachedHitFlash;

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
        _cachedLightMark = GetComponent<LightMarkEffect>();
        _cachedDarkMark = GetComponent<DarkMarkEffect>();
        _cachedHitFlash = GetComponent<HitFlashEffect>();
    }

    /// <summary>
    /// 标记是否已触发死亡流程（防止重复）
    /// </summary>
    private bool _dead = false;

    /// <summary>
    /// 是否处于无敌帧状态
    /// </summary>
    public bool IsInvincible => Time.time < _invincibleUntil;

    /// <summary>
    /// 玩家无敌帧闪烁效果（每帧调用）
    /// </summary>
    private float _regenAccumulator;

    private void Update()
    {
        // HP 回复
        if (_hpRegenPerSecond > 0 && _currentHp > 0 && _currentHp < _maxHp)
        {
            _regenAccumulator += _hpRegenPerSecond * Time.deltaTime;
            if (_regenAccumulator >= 1f)
            {
                int heal = Mathf.FloorToInt(_regenAccumulator);
                _regenAccumulator -= heal;
                Heal(heal);
            }
        }

        // 仅玩家无敌帧期间执行闪烁
        if (!gameObject.CompareTag("Player")) return;
        if (!IsInvincible)
        {
            // 无敌帧结束，确保 SpriteRenderer 完全不透明
            if (_playerSr != null && _playerSr.color.a < 1f)
            {
                var c = _playerSr.color;
                c.a = 1f;
                _playerSr.color = c;
            }
            return;
        }

        // 闪烁：每 0.05 秒切换透明/不透明
        _flashTimer -= Time.deltaTime;
        if (_flashTimer <= 0f)
        {
            _flashTimer = FLASH_INTERVAL;
            if (_playerSr == null)
                _playerSr = GetComponent<SpriteRenderer>();
            if (_playerSr != null)
            {
                var c = _playerSr.color;
                c.a = c.a > 0.5f ? 0.2f : 1f;
                _playerSr.color = c;
            }
        }
    }

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
    /// 受到伤害（默认白色数字）
    /// 伤害公式：actualDamage = max(0.01, damage - armor)
    /// </summary>
    /// <param name="damage">原始伤害值</param>
    public void TakeDamage(float damage)
    {
        TakeDamage(damage, Color.white);
    }

    /// <summary>
    /// 受到伤害（指定颜色的伤害数字）
    /// 伤害公式：actualDamage = max(0.01, damage - armor)
    /// </summary>
    /// <param name="damage">原始伤害值</param>
    /// <param name="popupColor">伤害数字颜色</param>
    public void TakeDamage(float damage, Color popupColor)
    {
        if (_currentHp <= 0) return;

        // 闪避系统（P1-27）— 仅对玩家生效
        if (gameObject.CompareTag("Player"))
        {
            // 无敌帧检查：玩家在无敌帧期间免疫所有伤害
            if (IsInvincible)
            {
                DebugHelper.Log($"[Damageable] {gameObject.name} is invincible — damage blocked!");
                return;
            }

            float dodgeChance = SaveManager.Instance?.GetPermanentBonus("dodge_chance") ?? 0f;
            if (dodgeChance > 0f && UnityEngine.Random.value < dodgeChance)
            {
                DebugHelper.Log($"[Damageable] {gameObject.name} dodged attack!");
                return;
            }
        }

        // 受伤加深效果（光明标记 + 黑暗标记）
        if (_cachedLightMark == null) _cachedLightMark = GetComponent<LightMarkEffect>();
        if (_cachedDarkMark == null) _cachedDarkMark = GetComponent<DarkMarkEffect>();

        float damageMultiplier = 1f;
        if (_cachedLightMark != null && _cachedLightMark.IsActive)
            damageMultiplier *= _cachedLightMark.GetDamageMultiplier();
        if (_cachedDarkMark != null && _cachedDarkMark.IsActive)
            damageMultiplier *= 1f + _cachedDarkMark.StackCount * DotEffectConfig.GetDefault().DarkMarkDamageBonus;

        damage *= damageMultiplier;

        // 混合护甲公式：固定减伤（上限50%） + 百分比减伤（递减收益）
        // 固定减伤：最多减掉伤害的50%
        float flatReduction = Mathf.Min(_armor, damage * FLAT_CAP);
        // 百分比减伤：护甲/(护甲+100)，递减收益（护甲100=50%，护甲200=67%）
        float percentReduction = _armor / (_armor + PERCENT_BASE);
        float actualDamage = Mathf.Max(0.01f, (damage - flatReduction) * (1f - percentReduction));
        _currentHp = Mathf.Max(0, _currentHp - Mathf.CeilToInt(actualDamage));

        DebugHelper.Log($"[Damageable] {gameObject.name} took {actualDamage:F2} damage (raw:{damage:F2} - armor:{_armor}), HP: {_currentHp}/{_maxHp}");

        // 显示伤害数字（敌人受击时）
        if (!gameObject.CompareTag("Player"))
        {
            DamagePopup.Create(transform.position, actualDamage, popupColor, false);

            if (_cachedHitFlash == null) _cachedHitFlash = GetComponent<HitFlashEffect>();
            if (_cachedHitFlash != null) _cachedHitFlash.TriggerFlash();
        }

        OnDamaged?.Invoke(_currentHp, _maxHp);
        EventManager.TriggerDamage(gameObject, actualDamage, transform.position);

        // 玩家受伤后：触发无敌帧 + 受伤闪红
        if (gameObject.CompareTag("Player"))
        {
            // 触发无敌帧
            _invincibleUntil = Time.time + _invincibleDuration;
            _flashTimer = 0f; // 立即开始闪烁

            // 受伤闪红效果（全屏红色闪一下）
            DamageFlashEffect.Show(0.1f, new Color(1f, 0f, 0f, 0.3f));

            // 受伤音效由 SFXManager 通过 EventManager.OnPlayerDamaged 事件自动播放
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

        // 凋零状态禁止回血（由 StatusEffectManager 设置）
        var sem = GetComponent<StatusEffectManager>();
        if (sem != null && sem.IsWithered())
        {
            DebugHelper.Log($"[Damageable] {gameObject.name} is Withered — heal blocked!");
            return;
        }

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
    /// 追加护甲值（由进化系统调用）
    /// </summary>
    public void AddArmor(int bonus)
    {
        _armor += bonus;
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