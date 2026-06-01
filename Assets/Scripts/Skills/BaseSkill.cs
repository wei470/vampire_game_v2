using UnityEngine;

/// <summary>
/// 技能抽象基类。所有具体技能继承此类。
/// 对应 Python: skills/base_skill.py
/// 
/// 功能：
/// - Activate() - 激活技能
/// - Cooldown 管理
/// - Upgrade() - 升级技能
/// </summary>
public abstract class BaseSkill : MonoBehaviour
{
    [Header("技能数据")]
    [SerializeField] protected SkillData _skillData;

    // 运行时状态
    protected int _currentLevel = 1;
    protected float _lastUseTime = -999f;
    protected bool _isActive = false;
    protected float _activeTimer = 0f;

    // 组件引用
    protected PlayerController _playerController;
    protected Transform _playerTransform;

    // 公开属性
    public SkillData Data => _skillData;
    public int CurrentLevel => _currentLevel;
    public bool IsActive => _isActive;
    public float CooldownRemaining => Mathf.Max(0f, _lastUseTime + GetCooldown() - Time.time);
    public float CooldownPercent => GetCooldown() > 0 ? CooldownRemaining / GetCooldown() : 0f;
    public bool IsOnCooldown => CooldownRemaining > 0f;

    protected virtual void Awake()
    {
        _playerController = GameReferences.Player;
        if (_playerController != null)
        {
            _playerTransform = _playerController.transform;
        }
    }

    protected virtual void Update()
    {
        // 处理持续性技能的计时
        if (_isActive)
        {
            _activeTimer -= Time.deltaTime;
            if (_activeTimer <= 0f)
            {
                Deactivate();
            }
        }
    }

    /// <summary>
    /// 尝试激活技能。返回是否成功。
    /// </summary>
    public bool TryActivate()
    {
        if (_skillData == null) return false;
        if (_skillData.skillType == SkillData.SkillType.Passive) return false;
        if (IsOnCooldown) return false;
        if (_isActive) return false;

        _lastUseTime = Time.time;
        Activate();

        if (_skillData.duration > 0f)
        {
            _isActive = true;
            _activeTimer = _skillData.duration;
        }

        return true;
    }

    /// <summary>
    /// 技能激活逻辑（由子类实现）
    /// </summary>
    protected abstract void Activate();

    /// <summary>
    /// 技能结束/取消（由子类可选覆写）
    /// </summary>
    protected virtual void Deactivate()
    {
        _isActive = false;
        _activeTimer = 0f;
    }

    /// <summary>
    /// 升级技能
    /// </summary>
    public bool Upgrade()
    {
        if (_skillData == null) return false;
        if (_currentLevel >= _skillData.maxLevel) return false;

        _currentLevel++;
        OnUpgrade();
        return true;
    }

    /// <summary>
    /// 升级时的回调（由子类可选覆写）
    /// </summary>
    protected virtual void OnUpgrade()
    {
        DebugHelper.Log($"[Skill] {_skillData.skillName} upgraded to level {_currentLevel}");
    }

    /// <summary>
    /// 获取当前等级的冷却时间
    /// </summary>
    public float GetCooldown()
    {
        return _skillData != null ? _skillData.GetCooldownAtLevel(_currentLevel) : 10f;
    }

    /// <summary>
    /// 获取当前等级的伤害
    /// </summary>
    public int GetDamage()
    {
        return _skillData != null ? _skillData.GetDamageAtLevel(_currentLevel) : 0;
    }

    /// <summary>
    /// 获取当前等级的效果强度
    /// </summary>
    public float GetEffectStrength()
    {
        return _skillData != null ? _skillData.GetEffectAtLevel(_currentLevel) : 1f;
    }

    /// <summary>
    /// 设置技能数据（用于运行时动态分配）
    /// </summary>
    public void SetSkillData(SkillData data)
    {
        _skillData = data;
        _currentLevel = 1;
        _lastUseTime = -999f;
        _isActive = false;
    }

    /// <summary>
    /// 重置技能冷却
    /// </summary>
    public void ResetCooldown()
    {
        _lastUseTime = -999f;
    }
}