using UnityEngine;

/// <summary>
/// 击杀连击系统 — 连续击杀敌人时累积 Combo，Combo 越高奖励越大。
///
/// 规则：
///   - 击杀后 3 秒内再次击杀 → Combo + 1
///   - Combo 10+  → 经验 +10%，金币 +10%
///   - Combo 25+  → 经验 +25%，金币 +25%
///   - Combo 50+  → 经验 +50%，金币 +50%，屏幕震动
///   - Combo 100+ → 经验 +100%，金币 +100%，全屏特效
///   - 超过 3 秒无击杀 → Combo 归零
///
/// 使用方式：挂载到 GameSceneBootstrap 所在 GameObject 上
/// </summary>
public class ComboSystem : MonoBehaviour
{
    /// <summary>
    /// 静态实例引用（供 KillRewarder 等外部访问倍率）
    /// </summary>
    public static ComboSystem Instance { get; private set; }

    // ── 配置 ──
    [Header("连击配置")]
    [SerializeField] private float _comboTimeout = 3f;

    // ── 运行时状态 ──
    private int _currentCombo;
    private float _lastKillTime;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 当前连击数
    /// </summary>
    public int CurrentCombo => _currentCombo;

    /// <summary>
    /// 连击是否激活（Combo > 0 且未超时）
    /// </summary>
    public bool IsActive => _currentCombo > 0 && (Time.unscaledTime - _lastKillTime) < _comboTimeout;

    // ── 连击等级常量 ──
    private const int TIER_1_THRESHOLD = 10;
    private const int TIER_2_THRESHOLD = 25;
    private const int TIER_3_THRESHOLD = 50;
    private const int TIER_4_THRESHOLD = 100;

    // ── 屏幕震动引用（缓存）──
    private static ScreenShake _screenShake;

    private void OnEnable()
    {
        EventManager.OnEnemyKilled += OnEnemyKilled;
        EventManager.OnPlayerDeath += OnPlayerDeath;
        EventManager.OnGameStateChanged += OnGameStateChanged;
        _currentCombo = 0;
        _lastKillTime = 0f;
    }

    private void OnDisable()
    {
        EventManager.OnEnemyKilled -= OnEnemyKilled;
        EventManager.OnPlayerDeath -= OnPlayerDeath;
        EventManager.OnGameStateChanged -= OnGameStateChanged;
    }

    private void Update()
    {
        // 超时重置连击
        if (_currentCombo > 0 && (Time.unscaledTime - _lastKillTime) >= _comboTimeout)
        {
            ResetCombo();
        }
    }

    // ── 事件处理 ──

    private void OnEnemyKilled(Vector3 position, int xp, int coin)
    {
        int previousCombo = _currentCombo;
        _currentCombo++;
        _lastKillTime = Time.unscaledTime;

        // 触发连击变化事件
        EventManager.TriggerComboChanged(_currentCombo);

        // 检查是否跨过连击等级阈值
        if (_currentCombo >= TIER_3_THRESHOLD && previousCombo < TIER_3_THRESHOLD)
        {
            // Combo 50+ 屏幕震动
            TriggerScreenShake(0.15f);
        }
        else if (_currentCombo >= TIER_4_THRESHOLD && previousCombo < TIER_4_THRESHOLD)
        {
            // Combo 100+ 强震动
            TriggerScreenShake(0.3f);
        }
    }

    private void OnPlayerDeath()
    {
        ResetCombo();
    }

    private void OnGameStateChanged(GameManager.GameState oldState, GameManager.GameState newState)
    {
        if (newState == GameManager.GameState.GameOver || newState == GameManager.GameState.Menu)
        {
            ResetCombo();
        }
    }

    // ── 倍率查询 ──

    /// <summary>
    /// 获取经验倍率（基于当前连击数）
    /// </summary>
    public float GetXPMultiplier()
    {
        if (!IsActive) return 1f;

        if (_currentCombo >= TIER_4_THRESHOLD) return 2f;   // +100%
        if (_currentCombo >= TIER_3_THRESHOLD) return 1.5f;  // +50%
        if (_currentCombo >= TIER_2_THRESHOLD) return 1.25f; // +25%
        if (_currentCombo >= TIER_1_THRESHOLD) return 1.1f;  // +10%
        return 1f;
    }

    /// <summary>
    /// 获取金币倍率（基于当前连击数）
    /// </summary>
    public float GetCoinMultiplier()
    {
        if (!IsActive) return 1f;

        if (_currentCombo >= TIER_4_THRESHOLD) return 2f;
        if (_currentCombo >= TIER_3_THRESHOLD) return 1.5f;
        if (_currentCombo >= TIER_2_THRESHOLD) return 1.25f;
        if (_currentCombo >= TIER_1_THRESHOLD) return 1.1f;
        return 1f;
    }

    /// <summary>
    /// 获取当前连击等级（0-4）
    /// </summary>
    public int GetComboTier()
    {
        if (!IsActive) return 0;
        if (_currentCombo >= TIER_4_THRESHOLD) return 4;
        if (_currentCombo >= TIER_3_THRESHOLD) return 3;
        if (_currentCombo >= TIER_2_THRESHOLD) return 2;
        if (_currentCombo >= TIER_1_THRESHOLD) return 1;
        return 0;
    }

    /// <summary>
    /// 获取连击等级对应的颜色（白→黄→橙→红→紫）
    /// </summary>
    public static Color GetComboColor(int tier)
    {
        return tier switch
        {
            4 => new Color(0.8f, 0.2f, 1f),   // 紫色
            3 => new Color(1f, 0.2f, 0.2f),   // 红色
            2 => new Color(1f, 0.6f, 0f),     // 橙色
            1 => new Color(1f, 1f, 0.2f),     // 黄色
            _ => Color.white,                   // 白色
        };
    }

    // ── 内部方法 ──

    private void ResetCombo()
    {
        if (_currentCombo > 0)
        {
            _currentCombo = 0;
            EventManager.TriggerComboChanged(0);
        }
    }

    private void TriggerScreenShake(float intensity)
    {
        if (_screenShake == null)
            _screenShake = FindAnyObjectByType<ScreenShake>(); // ScreenShake 无 Instance，懒缓存到 _screenShake

        if (_screenShake != null)
            _screenShake.Shake(intensity, 0.2f);
    }
}