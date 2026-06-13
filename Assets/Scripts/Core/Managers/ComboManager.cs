using UnityEngine;

/// <summary>
/// 连击系统管理器 — 连续击杀敌人增加 Combo 计数。
/// 
/// 功能：
/// - 连续击杀增加 Combo 计数
/// - Combo 越高，经验/金币加成越大
/// - 超时后 Combo 重置
/// - 通过 EventManager.TriggerComboChanged 广播变化
/// 
/// 使用方式：Singleton，场景中自动创建
/// </summary>
public class ComboManager : Singleton<ComboManager>
{
    [Header("连击设置")]
    [SerializeField] private float _comboTimeout = 3f;        // 连击超时时间（秒）
    [SerializeField] private float _xpBonusPerCombo = 0.05f;  // 每层 Combo +5% 经验
    [SerializeField] private float _coinBonusPerCombo = 0.03f; // 每层 Combo +3% 金币

    [Header("运行时状态")]
    [SerializeField] private int _currentCombo = 0;
    [SerializeField] private float _lastKillTime = 0f;

    /// <summary>
    /// 当前连击数
    /// </summary>
    public int CurrentCombo => _currentCombo;

    /// <summary>
    /// 经验加成倍率（1.0 = 无加成）
    /// </summary>
    public float XpMultiplier => 1f + _currentCombo * _xpBonusPerCombo;

    /// <summary>
    /// 金币加成倍率（1.0 = 无加成）
    /// </summary>
    public float CoinMultiplier => 1f + _currentCombo * _coinBonusPerCombo;

    protected override void Awake()
    {
        base.Awake();
    }

    private void OnEnable()
    {
        EventManager.OnEnemyKilled += OnEnemyKilled;
    }

    private void OnDisable()
    {
        EventManager.OnEnemyKilled -= OnEnemyKilled;
    }

    private void Update()
    {
        // 超时重置连击
        if (_currentCombo > 0 && Time.time - _lastKillTime > _comboTimeout)
        {
            ResetCombo();
        }
    }

    private void OnEnemyKilled(Vector3 deathPosition, int xpReward, int coinReward)
    {
        _currentCombo++;
        _lastKillTime = Time.time;
        EventManager.TriggerComboChanged(_currentCombo);

        DebugHelper.Log($"[ComboManager] Combo: {_currentCombo} (XP ×{XpMultiplier:F2}, Coin ×{CoinMultiplier:F2})");
    }

    private void ResetCombo()
    {
        _currentCombo = 0;
        EventManager.TriggerComboChanged(0);
    }

    /// <summary>
    /// 手动重置（游戏结束时调用）
    /// </summary>
    public void ResetAll()
    {
        _currentCombo = 0;
        _lastKillTime = 0f;
    }
}