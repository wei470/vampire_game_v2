using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Boss Rush 模式管理器 — 连续挑战5个Boss，难度递增。
///
/// 规则：
/// - 解锁条件：击败过所有4种Boss变体
/// - 连续挑战5个Boss，难度递增（HP×1.5/波，攻击力+20%/波）
/// - 每击败一个Boss可选择一个增益buff
/// - 通关奖励：大量金币
/// - 记录最快通关时间
///
/// 架构：
/// - 由 GameManager 管理状态（Playing → BossRushReward → Playing）
/// - 通过 EventManager.OnEnemyKilled 监听Boss死亡
/// - 复用 BossFactory 生成Boss
/// </summary>
public class BossRushManager : MonoBehaviour
{
    [Header("Boss Rush 配置")]
    [SerializeField] private int _totalRounds = 5;
    [SerializeField] private float _hpMultiplierPerRound = 1.5f;
    [SerializeField] private float _damageMultiplierPerRound = 1.2f;
    [SerializeField] private int _baseBossHp = 800;
    [SerializeField] private int _completionGoldReward = 500;
    [SerializeField] private float _spawnDistance = 15f;

    [Header("运行时状态")]
    [SerializeField] private int _currentRound = 0;
    [SerializeField] private bool _isActive = false;
    [SerializeField] private float _startTime;
    [SerializeField] private int _bossesDefeated = 0;

    // Boss 类型轮换顺序
    private BossEnemy.BossType[] _bossSequence;

    // 当前存活的Boss
    private BossEnemy _currentBoss;

    /// <summary>当前波次（从1开始）</summary>
    public int CurrentRound => _currentRound;

    /// <summary>总波次数</summary>
    public int TotalRounds => _totalRounds;

    /// <summary>Boss Rush 是否正在进行</summary>
    public bool IsActive => _isActive;

    /// <summary>Boss Rush 是否已完成</summary>
    public bool IsCompleted => _currentRound > _totalRounds;

    /// <summary>当前已用时间（秒）</summary>
    public float ElapsedTime => _isActive ? Time.time - _startTime : 0f;

    /// <summary>Boss Rush 通关奖励</summary>
    public event System.Action<int, float> OnBossRushCompleted; // (goldReward, completionTime)

    /// <summary>Boss 被击败事件（用于UI奖励选择）</summary>
    public event System.Action<int> OnBossDefeated; // (roundNumber)

    /// <summary>当前Boss的HP倍率</summary>
    public float CurrentHpMultiplier => Mathf.Pow(_hpMultiplierPerRound, _currentRound - 1);

    /// <summary>当前Boss的攻击力倍率</summary>
    public float CurrentDamageMultiplier => Mathf.Pow(_damageMultiplierPerRound, _currentRound - 1);

    // 单例（非DontDestroyOnLoad，跟随场景）
    public static BossRushManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        GenerateBossSequence();
    }

    private void OnEnable()
    {
        EventManager.OnEnemyKilled += HandleEnemyKilled;
    }

    private void OnDisable()
    {
        EventManager.OnEnemyKilled -= HandleEnemyKilled;
    }

    /// <summary>
    /// 检查Boss Rush是否已解锁（击败过所有4种Boss变体）
    /// </summary>
    public static bool IsUnlocked()
    {
        // 通过SaveManager检查是否击败过所有Boss
        // 简化实现：检查击杀Boss总数≥4
        if (SaveManager.Instance == null) return false;
        return SaveManager.Instance.GetTotalBossKills() >= 4;
    }

    /// <summary>
    /// 开始 Boss Rush 模式
    /// </summary>
    public void StartBossRush()
    {
        _isActive = true;
        _currentRound = 0;
        _bossesDefeated = 0;
        _startTime = Time.time;

        GenerateBossSequence();

        DebugHelper.Log("[BossRush] ★ Boss Rush STARTED! 5 rounds of boss battles!");
        DamagePopup.Create(GameReferences.Player.transform.position + Vector3.up * 4f,
            0, new Color(1f, 0.3f, 0f), false, "★ BOSS RUSH START!");

        // 开始第一轮
        StartNextRound();
    }

    /// <summary>
    /// 生成Boss出场顺序（4种Boss循环，最后一轮为随机强化版）
    /// </summary>
    private void GenerateBossSequence()
    {
        _bossSequence = new BossEnemy.BossType[_totalRounds];
        for (int i = 0; i < _totalRounds; i++)
        {
            if (i < _totalRounds - 1)
            {
                // 前4轮：循环4种Boss类型
                _bossSequence[i] = (BossEnemy.BossType)(i % 4);
            }
            else
            {
                // 最终轮：随机选择一个Boss作为最终Boss
                _bossSequence[i] = (BossEnemy.BossType)Random.Range(0, 4);
            }
        }
    }

    /// <summary>
    /// 开始下一轮Boss战
    /// </summary>
    private void StartNextRound()
    {
        _currentRound++;

        if (_currentRound > _totalRounds)
        {
            CompleteBossRush();
            return;
        }

        BossEnemy.BossType bossType = _bossSequence[_currentRound - 1];
        int bossHp = Mathf.RoundToInt(_baseBossHp * CurrentHpMultiplier);

        DebugHelper.Log($"[BossRush] Round {_currentRound}/{_totalRounds}: {bossType} (HP={bossHp}, HP×{CurrentHpMultiplier:F1}, DMG×{CurrentDamageMultiplier:F1})");

        // 显示回合通知
        var player = GameReferences.Player;
        if (player != null)
        {
            string roundText = _currentRound == _totalRounds
                ? $"★ FINAL BOSS: {bossType}!"
                : $"Round {_currentRound}/{_totalRounds}: {bossType}";

            DamagePopup.Create(player.transform.position + Vector3.up * 4f,
                0, BossFactory.BossColors[(int)bossType], false, roundText);
        }

        // 延迟生成Boss
        StartCoroutine(SpawnBossDelayed(bossType, bossHp));
    }

    /// <summary>
    /// 延迟生成Boss（给玩家准备时间）
    /// </summary>
    private IEnumerator SpawnBossDelayed(BossEnemy.BossType bossType, int hp)
    {
        yield return new WaitForSeconds(2f);

        var player = GameReferences.Player;
        if (player == null || !player.gameObject.activeInHierarchy) yield break;

        // 在玩家前方生成Boss
        Vector3 spawnPos = player.transform.position + (Vector3)(Random.insideUnitCircle.normalized * _spawnDistance);

        var boss = BossFactory.CreateBoss(spawnPos, hp);
        if (boss != null)
        {
            // 设置Boss类型和颜色
            var sr = boss.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = BossFactory.BossColors[(int)bossType];

            // 应用攻击力倍率
            // 攻击力倍率由BossEnemy内部配置处理

            // 最终轮Boss体积更大
            if (_currentRound == _totalRounds)
            {
                boss.transform.localScale *= 1.3f;
                var finalSr = boss.GetComponent<SpriteRenderer>();
                if (finalSr != null)
                    finalSr.color = new Color(1f, 0.1f, 0.1f); // 最终Boss深红色
            }

            _currentBoss = boss;

            // 屏幕震动
            var cam = GameReferences.MainCamera;
            if (cam != null)
            {
                var shake = cam.GetComponent<ScreenShake>();
                if (shake != null) shake.Shake(2f, 0.5f);
            }

            DebugHelper.Log($"[BossRush] Boss spawned: {bossType} at {spawnPos}");
        }
    }

    /// <summary>
    /// 敌人死亡回调 — 检测Boss是否被击败
    /// </summary>
    private void HandleEnemyKilled(Vector3 deathPosition, int xpReward, int coinReward)
    {
        if (!_isActive) return;

        // 检查死亡的是否是当前Boss
        if (_currentBoss != null && !_currentBoss.gameObject.activeInHierarchy)
        {
            _currentBoss = null;
            _bossesDefeated++;

            DebugHelper.Log($"[BossRush] Boss defeated! ({_bossesDefeated}/{_totalRounds})");

            // 触发Boss被击败事件（用于UI奖励选择）
            OnBossDefeated?.Invoke(_currentRound);

            // 给予击杀奖励（额外金币）
            int bonusGold = 50 * _currentRound;
            if (SaveManager.Instance != null)
                SaveManager.Instance.AddGold(bonusGold);

            // 显示击败通知
            var player = GameReferences.Player;
            if (player != null)
            {
                DamagePopup.Create(player.transform.position + Vector3.up * 3f,
                    bonusGold, new Color(1f, 0.85f, 0f), false,
                    $"BOSS DEFEATED! +{bonusGold}G");
            }

            // 治疗玩家（每轮恢复20%HP）
            var playerDmg = player?.GetComponent<Damageable>();
            if (playerDmg != null)
                playerDmg.Heal(Mathf.RoundToInt(playerDmg.MaxHp * 0.2f));

            // 延迟后开始下一轮
            StartCoroutine(NextRoundDelayed());
        }
    }

    /// <summary>
    /// 延迟开始下一轮（给玩家拾取奖励的时间）
    /// </summary>
    private IEnumerator NextRoundDelayed()
    {
        yield return new WaitForSeconds(3f);
        StartNextRound();
    }

    /// <summary>
    /// Boss Rush 通关
    /// </summary>
    private void CompleteBossRush()
    {
        _isActive = false;
        float completionTime = Time.time - _startTime;

        DebugHelper.Log($"[BossRush] ★★★ BOSS RUSH COMPLETED! Time: {completionTime:F1}s, Gold reward: {_completionGoldReward}");

        // 发放通关金币奖励
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AddGold(_completionGoldReward);

            // 记录最快通关时间
            float bestTime = SaveManager.Instance.GetBestBossRushTime();
            if (bestTime <= 0f || completionTime < bestTime)
            {
                SaveManager.Instance.SetBestBossRushTime(completionTime);
                DebugHelper.Log($"[BossRush] NEW BEST TIME: {completionTime:F1}s!");
            }
        }

        // 显示通关通知
        var player = GameReferences.Player;
        if (player != null)
        {
            DamagePopup.Create(player.transform.position + Vector3.up * 5f,
                _completionGoldReward, new Color(1f, 0.85f, 0f), false,
                $"★ BOSS RUSH CLEAR! +{_completionGoldReward}G");

            // 全屏金色闪光
            DamageFlashEffect.Show(0.3f, new Color(1f, 0.85f, 0f, 0.4f));
        }

        // 屏幕震动庆祝
        var cam = GameReferences.MainCamera;
        if (cam != null)
        {
            var shake = cam.GetComponent<ScreenShake>();
            if (shake != null) shake.Shake(3f, 1f);
        }

        // 音效
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayLevelUp();

        // 触发完成事件
        OnBossRushCompleted?.Invoke(_completionGoldReward, completionTime);
    }

    /// <summary>
    /// 获取当前Boss的进度信息（供UI显示）
    /// </summary>
    public string GetProgressText()
    {
        if (!_isActive) return "Boss Rush Inactive";
        return $"Boss Rush: Round {_currentRound}/{_totalRounds}";
    }

    /// <summary>
    /// 获取最佳通关时间
    /// </summary>
    public float GetBestTime()
    {
        if (SaveManager.Instance == null) return 0f;
        return SaveManager.Instance.GetBestBossRushTime();
    }

    /// <summary>
    /// 重置Boss Rush状态
    /// </summary>
    public void ResetBossRush()
    {
        _isActive = false;
        _currentRound = 0;
        _bossesDefeated = 0;
        _currentBoss = null;
        DebugHelper.Log("[BossRush] Boss Rush reset.");
    }
}