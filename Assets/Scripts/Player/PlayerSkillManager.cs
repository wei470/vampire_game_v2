using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 玩家技能管理器 - 管理玩家的所有技能，按 E 使用主动技能。
/// 对应 Python: player/skill_manager.py
/// 
/// 功能：
/// - 管理主动/被动技能槽位
/// - 按 E 使用当前选中的主动技能
/// - Q 键切换当前技能
/// - 提供技能冷却信息给 HUD
/// </summary>
public class PlayerSkillManager : MonoBehaviour
{
    [Header("技能槽位")]
    [SerializeField] private int _maxActiveSlots = 1;
    [SerializeField] private int _maxPassiveSlots = 3;

    // 已装备的技能
    private List<BaseSkill> _activeSkills = new List<BaseSkill>();
    private List<BaseSkill> _passiveSkills = new List<BaseSkill>();
    private int _currentActiveIndex = 0;

    // 公开属性
    public List<BaseSkill> ActiveSkills => _activeSkills;
    public List<BaseSkill> PassiveSkills => _passiveSkills;
    public BaseSkill CurrentActiveSkill => _activeSkills.Count > 0 ? _activeSkills[_currentActiveIndex] : null;
    public int CurrentActiveIndex => _currentActiveIndex;

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // 暂停时不处理输入
        if (GameManager.Instance != null &&
            (GameManager.Instance.CurrentState == GameManager.GameState.Paused ||
             GameManager.Instance.CurrentState == GameManager.GameState.GameOver))
        {
            return;
        }

        // E 键使用当前主动技能
        if (kb.eKey.wasPressedThisFrame)
        {
            UseCurrentSkill();
        }

        // Q 键循环切换主动技能
        if (kb.qKey.wasPressedThisFrame && _activeSkills.Count > 1)
        {
            CycleActiveSkill();
        }
    }

    /// <summary>
    /// 使用当前选中的主动技能
    /// </summary>
    public bool UseCurrentSkill()
    {
        if (_activeSkills.Count == 0) return false;

        var skill = _activeSkills[_currentActiveIndex];
        if (skill == null) return false;

        bool success = skill.TryActivate();
        if (success)
        {
            DebugHelper.Log($"[SkillManager] Used skill: {skill.Data.skillName}");
        }
        else if (skill.IsOnCooldown)
        {
            DebugHelper.Log($"[SkillManager] {skill.Data.skillName} on cooldown: {skill.CooldownRemaining:F1}s");
        }
        return success;
    }

    /// <summary>
    /// 使用指定索引的主动技能
    /// </summary>
    public bool UseSkill(int index)
    {
        if (index < 0 || index >= _activeSkills.Count) return false;
        return _activeSkills[index].TryActivate();
    }

    /// <summary>
    /// 循环切换主动技能
    /// </summary>
    public void CycleActiveSkill()
    {
        if (_activeSkills.Count <= 1) return;
        _currentActiveIndex = (_currentActiveIndex + 1) % _activeSkills.Count;
        DebugHelper.Log($"[SkillManager] Switched to skill: {_activeSkills[_currentActiveIndex].Data.skillName}");
    }

    /// <summary>
    /// 添加技能到玩家
    /// </summary>
    public bool AddSkill(BaseSkill skill)
    {
        if (skill == null || skill.Data == null) return false;

        if (skill.Data.skillType == SkillData.SkillType.Active)
        {
            if (_activeSkills.Count >= _maxActiveSlots)
            {
                DebugHelper.LogWarning($"[SkillManager] Active skill slots full (max {_maxActiveSlots})");
                return false;
            }
            _activeSkills.Add(skill);
            DebugHelper.Log($"[SkillManager] Added active skill: {skill.Data.skillName}");
        }
        else
        {
            if (_passiveSkills.Count >= _maxPassiveSlots)
            {
                DebugHelper.LogWarning($"[SkillManager] Passive skill slots full (max {_maxPassiveSlots})");
                return false;
            }
            _passiveSkills.Add(skill);
            DebugHelper.Log($"[SkillManager] Added passive skill: {skill.Data.skillName}");
        }

        return true;
    }

    /// <summary>
    /// 移除技能
    /// </summary>
    public bool RemoveSkill(BaseSkill skill)
    {
        if (skill == null) return false;

        bool removed = false;
        if (skill.Data.skillType == SkillData.SkillType.Active)
        {
            removed = _activeSkills.Remove(skill);
            if (_currentActiveIndex >= _activeSkills.Count)
            {
                _currentActiveIndex = Mathf.Max(0, _activeSkills.Count - 1);
            }
        }
        else
        {
            removed = _passiveSkills.Remove(skill);
        }

        return removed;
    }

    /// <summary>
    /// 清除所有技能
    /// </summary>
    public void ClearAllSkills()
    {
        // 销毁所有技能组件
        foreach (var skill in _activeSkills)
        {
            if (skill != null) Destroy(skill);
        }
        foreach (var skill in _passiveSkills)
        {
            if (skill != null) Destroy(skill);
        }

        _activeSkills.Clear();
        _passiveSkills.Clear();
        _currentActiveIndex = 0;
    }

    /// <summary>
    /// 通过 SkillData 添加技能（运行时动态创建组件）
    /// </summary>
    public BaseSkill AddSkillByData(SkillData data)
    {
        if (data == null) return null;

        // 根据技能ID/名称创建对应的技能组件
        BaseSkill skill = CreateSkillComponent(data);
        if (skill == null)
        {
            DebugHelper.LogError($"[SkillManager] Failed to create skill component for: {data.skillName}");
            return null;
        }

        if (AddSkill(skill))
        {
            return skill;
        }
        else
        {
            Destroy(skill);
            return null;
        }
    }

    /// <summary>
    /// 根据 SkillData 创建对应的技能组件
    /// </summary>
    private BaseSkill CreateSkillComponent(SkillData data)
    {
        BaseSkill skill = null;

        // 根据技能名称匹配组件类型
        switch (data.skillName)
        {
            case "Wind Wave":
                skill = gameObject.AddComponent<WindWaveSkill>();
                break;
            case "Berserk":
                skill = gameObject.AddComponent<BerserkSkill>();
                break;
            case "The World":
                skill = gameObject.AddComponent<TheWorldSkill>();
                break;
            case "Teleport":
                skill = gameObject.AddComponent<TeleportSkill>();
                break;
            case "Death Aura":
                skill = gameObject.AddComponent<DeathAuraSkill>();
                break;
            case "Lightning Storm":
                skill = gameObject.AddComponent<LightningStormSkill>();
                break;
            case "Gravity Well":
                skill = gameObject.AddComponent<GravityWellSkill>();
                break;
            case "Frost Nova":
                skill = gameObject.AddComponent<FrostNovaSkill>();
                break;
            default:
                DebugHelper.LogWarning($"[SkillManager] Unknown skill: {data.skillName}");
                return null;
        }

        // 关键：将 SkillData 注入到技能组件，否则 skill.Data 为 null
        if (skill != null)
        {
            skill.SetSkillData(data);
        }

        return skill;
    }

    /// <summary>
    /// 获取所有技能的冷却信息（用于 HUD 显示）
    /// </summary>
    public (string name, float cooldownPercent, float cooldownRemaining)[] GetSkillCooldownInfo()
    {
        var info = new (string, float, float)[_activeSkills.Count];
        for (int i = 0; i < _activeSkills.Count; i++)
        {
            var s = _activeSkills[i];
            info[i] = (s.Data.skillName, s.CooldownPercent, s.CooldownRemaining);
        }
        return info;
    }
}