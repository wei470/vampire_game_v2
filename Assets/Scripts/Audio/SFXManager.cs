#pragma warning disable CS0414
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 音效管理器 — 管理所有游戏音效的播放。
/// 使用对象池管理 AudioSource 组件，避免运行时频繁创建/销毁。
/// 通过 EventManager 事件自动触发关键音效。
/// 
/// 音效分类：
/// - 战斗音效：子弹命中、DOT生效、引爆、暴击
/// - 敌人音效：死亡、Boss出场
/// - 玩家音效：受伤、治疗、升级、拾取
/// - UI音效：选择确认、暂停、商店
/// - 环境音效：波次开始、波次完成
/// </summary>
public class SFXManager : MonoBehaviour
{
    // ════════════════════════════════════════════════════════════════
    // 单例
    // ════════════════════════════════════════════════════════════════

    private static SFXManager _instance;
    public static SFXManager Instance => _instance;

    // ════════════════════════════════════════════════════════════════
    // 配置
    // ════════════════════════════════════════════════════════════════

    [Header("音效配置")]
    [SerializeField] [Range(0f, 1f)] private float _masterVolume = 0.7f;
    [SerializeField] [Range(0f, 1f)] private float _sfxVolume = 1f;
    [SerializeField] private int _poolSize = 16;

    [Header("战斗音效")]
    [SerializeField] private AudioClip _hitSound;
    [SerializeField] private AudioClip _critSound;
    [SerializeField] private AudioClip _detonateSound;
    [SerializeField] private AudioClip _dotTickSound;

    [Header("敌人音效")]
    [SerializeField] private AudioClip _enemyDeathSound;
    [SerializeField] private AudioClip _bossSpawnSound;

    [Header("玩家音效")]
    [SerializeField] private AudioClip _playerHurtSound;
    [SerializeField] private AudioClip _playerHealSound;
    [SerializeField] private AudioClip _levelUpSound;
    [SerializeField] private AudioClip _pickupSound;
    [SerializeField] private AudioClip _coinSound;

    [Header("UI音效")]
    [SerializeField] private AudioClip _selectSound;
    [SerializeField] private AudioClip _confirmSound;
    [SerializeField] private AudioClip _pauseSound;

    [Header("环境音效")]
    [SerializeField] private AudioClip _waveStartSound;
    [SerializeField] private AudioClip _waveCompleteSound;

    // ════════════════════════════════════════════════════════════════
    // 音效池
    // ════════════════════════════════════════════════════════════════

    private SFXPoolHelper _poolHelper;

    // ════════════════════════════════════════════════════════════════
    // 冷却控制（防止音效轰炸）
    // ════════════════════════════════════════════════════════════════

    private float _lastHitTime;
    private float _lastDotTickTime;
    private float _lastEnemyDeathTime;
    private const float HIT_COOLDOWN = 0.05f;      // 命中音效最短间隔50ms
    private const float DOT_TICK_COOLDOWN = 0.1f;   // DOT音效最短间隔100ms
    private const float ENEMY_DEATH_COOLDOWN = 0.08f; // 敌人死亡音效最短间隔80ms

    // ════════════════════════════════════════════════════════════════
    // 生命周期
    // ════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        _poolHelper = new SFXPoolHelper(_poolSize, transform, _masterVolume, _sfxVolume);
        RegisterEvents();
    }

    private void OnDestroy()
    {
        UnregisterEvents();
        if (_instance == this) _instance = null;
    }

    // ════════════════════════════════════════════════════════════════
    // 初始化
    // ════════════════════════════════════════════════════════════════


    // ════════════════════════════════════════════════════════════════
    // 事件注册
    // ════════════════════════════════════════════════════════════════

    private void RegisterEvents()
    {
        EventManager.OnEnemyKilled += OnEnemyKilled;
        EventManager.OnDamage += OnDamage;
        EventManager.OnPlayerDamaged += OnPlayerDamaged;
        EventManager.OnPlayerHealed += OnPlayerHealed;
        EventManager.OnLevelUp += OnLevelUp;
        EventManager.OnItemPicked += OnItemPicked;
        EventManager.OnCoinChanged += OnCoinChanged;
        EventManager.OnWaveStart += OnWaveStart;
        EventManager.OnWaveComplete += OnWaveComplete;
        EventManager.OnSelectionComplete += OnSelectionComplete;
    }

    private void UnregisterEvents()
    {
        EventManager.OnEnemyKilled -= OnEnemyKilled;
        EventManager.OnDamage -= OnDamage;
        EventManager.OnPlayerDamaged -= OnPlayerDamaged;
        EventManager.OnPlayerHealed -= OnPlayerHealed;
        EventManager.OnLevelUp -= OnLevelUp;
        EventManager.OnItemPicked -= OnItemPicked;
        EventManager.OnCoinChanged -= OnCoinChanged;
        EventManager.OnWaveStart -= OnWaveStart;
        EventManager.OnWaveComplete -= OnWaveComplete;
        EventManager.OnSelectionComplete -= OnSelectionComplete;
    }

    // ════════════════════════════════════════════════════════════════
    // 事件回调
    // ════════════════════════════════════════════════════════════════

    private void OnEnemyKilled(Vector3 position, int xp, int coin)
    {
        PlayWithCooldown(ref _lastEnemyDeathTime, ENEMY_DEATH_COOLDOWN, _enemyDeathSound, 0.7f);
    }

    private void OnDamage(GameObject target, int damage, Vector3 sourcePos)
    {
        // 判断是否是暴击（伤害超过阈值）
        bool isCrit = damage > 30; // 简单判断
        if (isCrit)
            PlayWithCooldown(ref _lastHitTime, HIT_COOLDOWN, _critSound ?? _hitSound, 1f);
        else
            PlayWithCooldown(ref _lastHitTime, HIT_COOLDOWN, _hitSound, 0.5f);
    }

    private void OnPlayerDamaged(int currentHP, int maxHP)
    {
        Play(_playerHurtSound, 0.8f);
    }

    private void OnPlayerHealed(int healAmount, int currentHP)
    {
        Play(_playerHealSound, 0.6f);
    }

    private void OnLevelUp(int newLevel)
    {
        Play(_levelUpSound, 1f);
    }

    private void OnItemPicked(string itemType, int amount)
    {
        Play(_pickupSound, 0.5f);
    }

    private void OnCoinChanged(int totalCoins)
    {
        Play(_coinSound, 0.3f);
    }

    private void OnWaveStart(int waveNumber)
    {
        Play(_waveStartSound, 0.8f);
        // Boss 波次播放 Boss 出场音效
        if (waveNumber % 5 == 0)
        {
            Play(_bossSpawnSound, 1f);
        }
    }

    private void OnWaveComplete(int waveNumber)
    {
        Play(_waveCompleteSound, 0.7f);
    }

    private void OnSelectionComplete(CharacterData cd, WeaponData wd, SkillData sd)
    {
        Play(_confirmSound, 0.8f);
    }

    // ════════════════════════════════════════════════════════════════
    // 公共 API — 手动播放音效
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 播放引爆音效（由 MagePassive.Detonate 调用）
    /// </summary>
    public void PlayDetonate()
    {
        Play(_detonateSound, 1f);
    }

    /// <summary>
    /// 播放 DOT tick 音效（由 StatusEffectManager 调用）
    /// </summary>
    public void PlayDotTick()
    {
        PlayWithCooldown(ref _lastDotTickTime, DOT_TICK_COOLDOWN, _dotTickSound, 0.2f);
    }

    /// <summary>
    /// 播放升级音效（由 MagePassive / AchievementUI 等调用）
    /// </summary>
    public void PlayLevelUp()
    {
        Play(_levelUpSound, 1f);
    }

    /// <summary>
    /// 播放升级选择音效（由 LevelUpUI 调用）
    /// </summary>
    public void PlaySelect()
    {
        Play(_selectSound, 0.6f);
    }

    /// <summary>
    /// 播放暂停音效
    /// </summary>
    public void PlayPause()
    {
        Play(_pauseSound, 0.5f);
    }

    /// <summary>
    /// 播放指定音效
    /// </summary>
    public void Play(AudioClip clip, float volumeScale = 1f)
    {
        _poolHelper.Play(clip, volumeScale);
    }

    /// <summary>
    /// 播放指定音效（指定音高）
    /// </summary>
    public void Play(AudioClip clip, float volumeScale, float pitch)
    {
        _poolHelper.Play(clip, volumeScale, pitch);
    }

    /// <summary>
    /// 在指定位置播放 3D 音效
    /// </summary>
    public void PlayAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        _poolHelper.PlayAtPosition(clip, position, volumeScale);
    }

    // ════════════════════════════════════════════════════════════════
    // 音量控制
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 设置主音量
    /// </summary>
    public void SetMasterVolume(float vol)
    {
        _masterVolume = Mathf.Clamp01(vol);
        _poolHelper?.SetVolume(_masterVolume, _sfxVolume);
    }

    /// <summary>
    /// 设置音效音量
    /// </summary>
    public void SetSFXVolume(float vol)
    {
        _sfxVolume = Mathf.Clamp01(vol);
        _poolHelper?.SetVolume(_masterVolume, _sfxVolume);
    }

    /// <summary>
    /// 获取主音量
    /// </summary>
    public float GetMasterVolume() => _masterVolume;

    /// <summary>
    /// 获取音效音量
    /// </summary>
    public float GetSFXVolume() => _sfxVolume;

    // ════════════════════════════════════════════════════════════════
    // 内部工具
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 带冷却的音效播放（防止音效轰炸）
    /// </summary>
    private void PlayWithCooldown(ref float lastTime, float cooldown, AudioClip clip, float volumeScale)
    {
        _poolHelper.PlayWithCooldown(ref lastTime, cooldown, clip, volumeScale);
    }

    /// <summary>
    /// 停止所有正在播放的音效
    /// </summary>
    public void StopAll()
    {
        _poolHelper.StopAll();
    }

    /// <summary>
    /// 重置状态（返回菜单时调用）
    /// </summary>
    public void ResetState()
    {
        StopAll();
        _lastHitTime = 0;
        _lastDotTickTime = 0;
        _lastEnemyDeathTime = 0;
    }
}