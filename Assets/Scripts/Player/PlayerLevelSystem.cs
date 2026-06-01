using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家经验与升级系统，管理经验值获取和升级逻辑。
/// 对应 Python: entities/player.py 中的经验/升级部分
/// 
/// 升级公式：levelUpExp = 20 + level * 15
/// 使用方式：挂载到玩家 GameObject 上
/// </summary>
public class PlayerLevelSystem : MonoBehaviour
{
    [Header("等级信息")]
    [SerializeField] private int _level = 1;
    [SerializeField] private int _currentExp = 0;
    [SerializeField] private int _totalExp = 0;

    /// <summary>
    /// 当前等级
    /// </summary>
    public int Level => _level;

    /// <summary>
    /// 当前经验值（当前等级内的）
    /// </summary>
    public int CurrentExp => _currentExp;

    /// <summary>
    /// 升级所需经验值
    /// </summary>
    public int ExpToLevelUp => GetExpForLevel(_level);

    /// <summary>
    /// 升级进度（0-1）
    /// </summary>
    public float ExpProgress => ExpToLevelUp > 0 ? (float)_currentExp / ExpToLevelUp : 0f;

    /// <summary>
    /// 累计总经验
    /// </summary>
    public int TotalExp => _totalExp;

    /// <summary>
    /// 升级次数统计
    /// </summary>
    public int LevelUpCount { get; private set; }

    /// <summary>
    /// 升级事件，参数：(新等级)
    /// </summary>
    public event System.Action<int> OnLevelUpLocal;

    private void OnEnable()
    {
        // XP now comes from XPGem pickups, not directly from kills
    }

    private void OnDisable()
    {
    }

    private void Update()
    {
        // Debug: L 键添加 100 经验
        var kb = Keyboard.current;
        if (kb != null && kb.lKey.wasPressedThisFrame)
        {
            AddExp(100);
        }
    }

    /// <summary>
    /// 敌人死亡时获得经验
    /// </summary>
    private void OnEnemyKilled(Vector3 deathPosition, int xpReward, int coinReward)
    {
        AddExp(xpReward);
    }

    /// <summary>
    /// 添加经验值
    /// </summary>
    /// <param name="amount">经验量</param>
    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        _currentExp += amount;
        _totalExp += amount;

        DebugHelper.Log($"[PlayerLevelSystem] +{amount} XP, current: {_currentExp}/{ExpToLevelUp} (Level {_level})");

        EventManager.TriggerXPGained(amount);

        // 检查是否可以升级（可能一次获得大量经验连升多级）
        while (_currentExp >= ExpToLevelUp)
        {
            _currentExp -= ExpToLevelUp;
            _level++;
            LevelUpCount++;

            DebugHelper.Log($"[PlayerLevelSystem] Level Up! Now level {_level}, remaining XP: {_currentExp}");

            // 触发本地升级事件
            OnLevelUpLocal?.Invoke(_level);

            // 触发全局升级事件
            EventManager.TriggerLevelUp(_level);
        }
    }

    /// <summary>
    /// 获取指定等级的升级所需经验
    /// 公式：20 + level * 15
    /// </summary>
    /// <param name="level">当前等级</param>
    /// <returns>升级所需经验值</returns>
    public static int GetExpForLevel(int level)
    {
        return 20 + level * 15;
    }

    /// <summary>
    /// 重置等级系统（用于重新开始游戏）
    /// </summary>
    public void ResetLevel()
    {
        _level = 1;
        _currentExp = 0;
        _totalExp = 0;
        LevelUpCount = 0;
        DebugHelper.Log("[PlayerLevelSystem] Level system reset to level 1");
    }
}