using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 角色专属被动进化系统。
/// 监听升级事件，当玩家达到特定等级时自动解锁进化里程碑并应用效果。
///
/// 架构：
/// - 通过 GameReferences 获取 PlayerLevelSystem 和 CharacterData
/// - 订阅 PlayerLevelSystem.OnLevelUpLocal 事件
/// - 每次升级检查是否触发进化里程碑
/// - 根据 EvolutionEffectType 应用对应效果
///
/// 使用方式：挂载到玩家 GameObject 上，或由 GameSceneBootstrap 初始化
/// </summary>
public class EvolutionSystem : MonoBehaviour
{
    [Header("调试信息")]
    [SerializeField] private List<string> _unlockedEvolutions = new List<string>();

    private CharacterData _characterData;
    private PlayerLevelSystem _levelSystem;
    private bool _initialized;

    /// <summary>
    /// 已解锁的进化ID集合
    /// </summary>
    public HashSet<string> UnlockedEvolutionIds { get; private set; } = new HashSet<string>();

    /// <summary>
    /// 进化触发事件（用于UI通知），参数：(EvolutionMilestone)
    /// </summary>
    public event System.Action<EvolutionMilestone> OnEvolutionUnlocked;

    private void OnEnable()
    {
        // 如果已经初始化过，重新订阅事件
        if (_initialized && _levelSystem != null)
        {
            _levelSystem.OnLevelUpLocal += HandleLevelUp;
        }
    }

    private void OnDisable()
    {
        if (_levelSystem != null)
        {
            _levelSystem.OnLevelUpLocal -= HandleLevelUp;
        }
    }

    /// <summary>
    /// 初始化进化系统（由 GameStarter 或 GameSceneBootstrap 调用）
    /// </summary>
    public void Init(CharacterData characterData, PlayerLevelSystem levelSystem)
    {
        if (characterData == null || levelSystem == null)
        {
            DebugHelper.LogError("[EvolutionSystem] Init failed: characterData or levelSystem is null!");
            return;
        }

        _characterData = characterData;
        _levelSystem = levelSystem;

        // 订阅升级事件
        _levelSystem.OnLevelUpLocal += HandleLevelUp;

        _initialized = true;
        DebugHelper.Log($"[EvolutionSystem] Initialized for character: {_characterData.characterName}, evolution tree: {(_characterData.evolutionTree != null ? _characterData.evolutionTree.Length : 0)} milestones");
    }

    /// <summary>
    /// 升级回调 — 检查是否解锁进化里程碑
    /// </summary>
    private void HandleLevelUp(int newLevel)
    {
        if (!_initialized || _characterData == null) return;

        var milestone = _characterData.GetEvolutionForLevel(newLevel);
        if (milestone == null) return;

        // 防止重复解锁
        if (UnlockedEvolutionIds.Contains(milestone.milestoneId))
        {
            DebugHelper.LogWarning($"[EvolutionSystem] Evolution '{milestone.milestoneId}' already unlocked, skipping.");
            return;
        }

        UnlockEvolution(milestone);
    }

    /// <summary>
    /// 解锁进化并应用效果
    /// </summary>
    private void UnlockEvolution(EvolutionMilestone milestone)
    {
        UnlockedEvolutionIds.Add(milestone.milestoneId);
        _unlockedEvolutions.Add($"[Lv{milestone.requiredLevel}] {milestone.displayName}");

        DebugHelper.Log($"[EvolutionSystem] ★ EVOLUTION UNLOCKED: {milestone.displayName} (Lv{milestone.requiredLevel}) — {milestone.description}");

        // 应用进化效果
        ApplyEvolutionEffect(milestone);

        // 显示进化通知（浮字+音效）
        ShowEvolutionNotification(milestone);

        // 触发进化事件
        OnEvolutionUnlocked?.Invoke(milestone);
    }

    /// <summary>
    /// 根据进化类型应用效果
    /// </summary>
    private void ApplyEvolutionEffect(EvolutionMilestone milestone)
    {
        var character = FindCharacterPassive();
        if (character == null) return;

        // 回退：通用进化效果
        switch (milestone.effectType)
        {
            case EvolutionEffectType.DotDurationBonus:
                ApplyDotDurationBonus(milestone.value);
                break;

            case EvolutionEffectType.DotComboDamageBonus:
                ApplyDotComboDamageBonus(milestone.value);
                break;

            case EvolutionEffectType.DetonateTriggerAllCombos:
                ApplyDetonateTriggerAllCombos();
                break;

            case EvolutionEffectType.DotDamageBonus:
                ApplyDotDamageBonus(milestone.value);
                break;

            case EvolutionEffectType.MoveSpeedBonus:
                ApplyMoveSpeedBonus(milestone.value);
                break;

            case EvolutionEffectType.ArmorBonus:
                ApplyArmorBonus((int)milestone.value);
                break;

            case EvolutionEffectType.CritChanceBonus:
                ApplyCritChanceBonus(milestone.value);
                break;

            case EvolutionEffectType.HpRegenBonus:
                ApplyHpRegenBonus(milestone.value);
                break;

            case EvolutionEffectType.AttackSpeedBonus:
                ApplyAttackSpeedBonus(milestone.value);
                break;

            default:
                DebugHelper.LogWarning($"[EvolutionSystem] Unknown effect type: {milestone.effectType}");
                break;
        }
    }

    // ═══ 具体效果应用方法 ═══

    /// <summary>
    /// DOT持续时间加成（如+20%）
    /// </summary>
    private void ApplyDotDurationBonus(float bonus)
    {
        var character = FindCharacterPassive();
        var dotChar = character as IDotCharacterPassive;
        if (dotChar != null)
        {
            dotChar.AddDotDurationBonus(bonus);
            DebugHelper.Log($"[EvolutionSystem] DOT Duration +{bonus * 100:F0}% (total mult: {dotChar.GetDotDurationMultiplier()})");
        }
    }

    /// <summary>
    /// DOT组合伤害加成
    /// </summary>
    private void ApplyDotComboDamageBonus(float bonus)
    {
        // 通过 DotComboSystem 设置全局组合伤害倍率
        DotComboSystem.SetEvolutionComboMultiplier(bonus);
        DebugHelper.Log($"[EvolutionSystem] DOT Combo Damage +{bonus * 100:F0}%");
    }

    /// <summary>
    /// 引爆时触发所有DOT组合
    /// </summary>
    private void ApplyDetonateTriggerAllCombos()
    {
        var detonate = GameReferences.DetonateSystem;
        if (detonate != null)
        {
            detonate.TriggerAllCombosOnDetonate = true;
            DebugHelper.Log("[EvolutionSystem] ✦ Detonate now triggers ALL DOT Combos!");
        }
    }

    /// <summary>
    /// DOT伤害加成
    /// </summary>
    private void ApplyDotDamageBonus(float bonus)
    {
        var character = FindCharacterPassive();
        if (character != null)
        {
            character.DotDamageMultiplier += bonus;
            DebugHelper.Log($"[EvolutionSystem] DOT Damage +{bonus * 100:F0}%");
        }
    }

    /// <summary>
    /// 移速加成
    /// </summary>
    private void ApplyMoveSpeedBonus(float bonus)
    {
        var player = GameReferences.Player;
        if (player != null)
        {
            var controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.MoveSpeed *= (1f + bonus);
                DebugHelper.Log($"[EvolutionSystem] Move Speed +{bonus * 100:F0}% (total: {controller.MoveSpeed})");
            }
        }
    }

    /// <summary>
    /// 护甲加成
    /// </summary>
    private void ApplyArmorBonus(int bonus)
    {
        var player = GameReferences.Player;
        if (player != null)
        {
            var damageable = player.GetComponent<Damageable>();
            if (damageable != null)
            {
                damageable.AddArmor(bonus);
                DebugHelper.Log($"[EvolutionSystem] Armor +{bonus}");
            }
        }
    }

    /// <summary>
    /// 暴击率加成
    /// </summary>
    private void ApplyCritChanceBonus(float bonus)
    {
        var character = FindCharacterPassive();
        if (character != null)
        {
            character.CritChanceBonus += bonus;
            DebugHelper.Log($"[EvolutionSystem] Crit Chance +{bonus * 100:F1}%");
        }
    }

    /// <summary>
    /// 回血加成
    /// </summary>
    private void ApplyHpRegenBonus(float bonus)
    {
        var player = GameReferences.Player;
        if (player != null)
        {
            var controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.AddHpRegen(bonus);
                DebugHelper.Log($"[EvolutionSystem] HP Regen +{bonus}/s");
            }
        }
    }

    /// <summary>
    /// 攻速加成
    /// </summary>
    private void ApplyAttackSpeedBonus(float bonus)
    {
        var character = FindCharacterPassive();
        if (character != null)
        {
            character.AttackSpeedBonus += bonus;
            DebugHelper.Log($"[EvolutionSystem] Attack Speed +{bonus * 100:F0}%");
        }
    }

    // ═══ 辅助方法 ═══

    /// <summary>
    /// 显示进化通知浮字
    /// </summary>
    private void ShowEvolutionNotification(EvolutionMilestone milestone)
    {
        var player = GameReferences.Player;
        if (player != null)
        {
            // 浮字通知
            DamagePopup.Create(
                player.transform.position + Vector3.up * 3.5f,
                0,
                milestone.glowColor,
                false,
                $"★ EVOLUTION: {milestone.displayName}!"
            );
        }

        // 音效
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayLevelUp();
    }

    /// <summary>
    /// 查找 ICharacterPassive 组件
    /// </summary>
    private ICharacterPassive FindCharacterPassive()
    {
        var player = GameReferences.Player;
        if (player == null) return null;
        return player.GetComponent<ICharacterPassive>();
    }

    /// <summary>
    /// 检查指定进化是否已解锁
    /// </summary>
    public bool IsEvolutionUnlocked(string milestoneId)
    {
        return UnlockedEvolutionIds.Contains(milestoneId);
    }

    /// <summary>
    /// 检查指定效果类型是否已解锁
    /// </summary>
    public bool HasEvolutionEffect(EvolutionEffectType effectType)
    {
        if (_characterData == null || _characterData.evolutionTree == null) return false;
        for (int i = 0; i < _characterData.evolutionTree.Length; i++)
        {
            var m = _characterData.evolutionTree[i];
            if (m != null && m.effectType == effectType && UnlockedEvolutionIds.Contains(m.milestoneId))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 获取所有已解锁的进化
    /// </summary>
    public List<EvolutionMilestone> GetUnlockedEvolutions()
    {
        var result = new List<EvolutionMilestone>();
        if (_characterData == null || _characterData.evolutionTree == null) return result;
        for (int i = 0; i < _characterData.evolutionTree.Length; i++)
        {
            var m = _characterData.evolutionTree[i];
            if (m != null && UnlockedEvolutionIds.Contains(m.milestoneId))
                result.Add(m);
        }
        return result;
    }

    /// <summary>
    /// 重置进化系统（游戏重启时调用）
    /// </summary>
    public void ResetEvolution()
    {
        UnlockedEvolutionIds.Clear();
        _unlockedEvolutions.Clear();
        _initialized = false;
        DebugHelper.Log("[EvolutionSystem] Evolution system reset.");
    }
}