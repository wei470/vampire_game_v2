#pragma warning disable CS0414
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 音效管理器 — 管理所有游戏音效的播放。
/// 使用对象池管理 AudioSource 组件，避免运行时频繁创建/销毁。
/// 通过 EventManager 事件自动触发关键音效。
/// 音效配置统一由 SoundTrack ScriptableObject 管理。
/// 
/// 音效分类：
/// - 战斗音效：子弹命中、DOT生效、引爆、暴击
/// - 敌人音效：死亡、Boss出场
/// - 玩家音效：受伤、治疗、升级、拾取
/// - UI音效：选择确认、暂停、商店
/// - 环境音效：波次开始、波次完成
/// - 技能音效：8种主动技能
/// - 元素DOT音效：7种DOT元素
/// - 特殊音效：连击、商店、成就、警告
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
    [Tooltip("音效配置包（ScriptableObject），统一管理所有音效")]
    [SerializeField] private SoundTrack _soundTrack;

    [Tooltip("对象池大小")]
    [SerializeField] private int _poolSize = 16;

    [Tooltip("空间音频池大小（3D 音效）")]
    [SerializeField] private int _spatialPoolSize = 16;

    // ════════════════════════════════════════════════════════════════
    // 音效池
    // ════════════════════════════════════════════════════════════════

    private SFXPoolHelper _poolHelper;

    // ════════════════════════════════════════════════════════════════
    // 冷却状态追踪（每个音效条目独立冷却）
    // ════════════════════════════════════════════════════════════════

    private Dictionary<SoundTrack.SoundEntry, float> _cooldownTracker = new Dictionary<SoundTrack.SoundEntry, float>();

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

        // 如果没有配置 SoundTrack，运行时创建并生成程序化音效
        if (_soundTrack == null)
        {
            _soundTrack = ScriptableObject.CreateInstance<SoundTrack>();
            ProceduralSFX.GenerateAll(_soundTrack);
            DebugHelper.Log("[SFXManager] No SoundTrack assigned, generated procedural sound effects.");
        }

        float master = _soundTrack.masterVolume;
        float sfx = _soundTrack.sfxVolume;
        _poolHelper = new SFXPoolHelper(_poolSize, _spatialPoolSize, transform, master, sfx);
        RegisterEvents();
    }

    private void OnDestroy()
    {
        UnregisterEvents();
        if (_instance == this) _instance = null;
    }

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
        PlayEntry(_soundTrack?.enemyDeath);
    }

    private void OnDamage(GameObject target, float damage, Vector3 sourcePos)
    {
        // 判断是否是暴击（伤害超过阈值）
        bool isCrit = damage > 30;
        if (isCrit)
            PlayEntry(_soundTrack?.crit);
        else
            PlayEntry(_soundTrack?.hit);
    }

    private void OnPlayerDamaged(int currentHP, int maxHP)
    {
        PlayEntry(_soundTrack?.playerHurt);
    }

    private void OnPlayerHealed(int healAmount, int currentHP)
    {
        PlayEntry(_soundTrack?.playerHeal);
    }

    private void OnLevelUp(int newLevel)
    {
        PlayEntry(_soundTrack?.levelUp);
    }

    private void OnItemPicked(string itemType, int amount)
    {
        PlayEntry(_soundTrack?.pickup);
    }

    private void OnCoinChanged(int totalCoins)
    {
        PlayEntry(_soundTrack?.coin);
    }

    private void OnWaveStart(int waveNumber)
    {
        PlayEntry(_soundTrack?.waveStart);
        // Boss 波次播放 Boss 出场音效
        if (waveNumber % 5 == 0)
        {
            PlayEntry(_soundTrack?.bossSpawn);
        }
    }

    private void OnWaveComplete(int waveNumber)
    {
        PlayEntry(_soundTrack?.waveComplete);
    }

    private void OnSelectionComplete(CharacterData cd, WeaponData wd, SkillData sd)
    {
        PlayEntry(_soundTrack?.confirm);
    }

    // ════════════════════════════════════════════════════════════════
    // 公共 API — 按名称播放音效
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 播放引爆音效（由 MagePassive.Detonate 调用）
    /// </summary>
    public void PlayDetonate()
    {
        PlayEntry(_soundTrack?.detonate);
    }

    /// <summary>
    /// 播放 DOT tick 音效（由 StatusEffectManager 调用）
    /// </summary>
    public void PlayDotTick()
    {
        PlayEntry(_soundTrack?.dotTick);
    }

    /// <summary>
    /// 播放指定元素 DOT 音效
    /// </summary>
    public void PlayDotElement(DotElementType type)
    {
        if (_soundTrack == null) return;
        // 测试：直接播放 enemyDeath 音效（已确认能听到的音效）
        var deathEntry = _soundTrack.enemyDeath;
        if (deathEntry != null && deathEntry.IsValid)
        {
            _poolHelper.Play(deathEntry.clip, deathEntry.volume);
        }
    }

    /// <summary>
    /// 确保 SoundTrack 中的 DOT 音效 clip 已初始化
    /// </summary>
    private void EnsureDotClips()
    {
        if (_soundTrack == null) return;
        if (_soundTrack.dotBleed.clip == null) _soundTrack.dotBleed.clip = ProceduralSFX.GenerateDotBleed(0.2f);
        if (_soundTrack.dotPoison.clip == null) _soundTrack.dotPoison.clip = ProceduralSFX.GenerateDotPoison(0.2f);
        if (_soundTrack.dotBurn.clip == null) _soundTrack.dotBurn.clip = ProceduralSFX.GenerateDotBurn(0.2f);
        if (_soundTrack.dotFrost.clip == null) _soundTrack.dotFrost.clip = ProceduralSFX.GenerateDotFrost(0.2f);
        if (_soundTrack.dotLightning.clip == null) _soundTrack.dotLightning.clip = ProceduralSFX.GenerateDotLightning(0.2f);
        if (_soundTrack.dotDark.clip == null) _soundTrack.dotDark.clip = ProceduralSFX.GenerateDotDark(0.2f);
        if (_soundTrack.dotLight.clip == null) _soundTrack.dotLight.clip = ProceduralSFX.GenerateDotLight(0.2f);
    }

    /// <summary>
    /// 播放技能音效
    /// </summary>
    public void PlaySkill(SkillSoundType type)
    {
        switch (type)
        {
            case SkillSoundType.Cast:      PlayEntry(_soundTrack?.skillCast); break;
            case SkillSoundType.Teleport:  PlayEntry(_soundTrack?.skillTeleport); break;
            case SkillSoundType.FrostNova: PlayEntry(_soundTrack?.skillFrostNova); break;
            case SkillSoundType.Lightning: PlayEntry(_soundTrack?.skillLightning); break;
            case SkillSoundType.Gravity:   PlayEntry(_soundTrack?.skillGravity); break;
            case SkillSoundType.DeathAura: PlayEntry(_soundTrack?.skillDeathAura); break;
            case SkillSoundType.Berserk:   PlayEntry(_soundTrack?.skillBerserk); break;
            case SkillSoundType.TheWorld:  PlayEntry(_soundTrack?.skillTheWorld); break;
            case SkillSoundType.WindWave:  PlayEntry(_soundTrack?.skillWindWave); break;
        }
    }

    /// <summary>
    /// 播放升级音效（由 MagePassive / AchievementUI 等调用）
    /// </summary>
    public void PlayLevelUp()
    {
        PlayEntry(_soundTrack?.levelUp);
    }

    /// <summary>
    /// 播放升级选择音效（由 LevelUpUI 调用）
    /// </summary>
    public void PlaySelect()
    {
        PlayEntry(_soundTrack?.select);
    }

    /// <summary>
    /// 播放暂停音效
    /// </summary>
    public void PlayPause()
    {
        PlayEntry(_soundTrack?.pause);
    }

    /// <summary>
    /// 播放连击音效
    /// </summary>
    public void PlayCombo()
    {
        PlayEntry(_soundTrack?.combo);
    }

    /// <summary>
    /// 播放商店购买音效
    /// </summary>
    public void PlayShopBuy()
    {
        PlayEntry(_soundTrack?.shopBuy);
    }

    /// <summary>
    /// 播放成就解锁音效
    /// </summary>
    public void PlayAchievement()
    {
        PlayEntry(_soundTrack?.achievement);
    }

    /// <summary>
    /// 播放警告音效
    /// </summary>
    public void PlayWarning()
    {
        PlayEntry(_soundTrack?.warning);
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

    /// <summary>
    /// 在指定位置播放 3D 空间音效（线性距离衰减，最大距离 20 单位）
    /// </summary>
    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume)
    {
        _poolHelper.PlayAtPosition(clip, position, volume);
    }

    // ════════════════════════════════════════════════════════════════
    // 音量控制
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 设置主音量
    /// </summary>
    public void SetMasterVolume(float vol)
    {
        if (_soundTrack != null) _soundTrack.masterVolume = Mathf.Clamp01(vol);
        _poolHelper?.SetVolume(GetMasterVolume(), GetSFXVolume());
    }

    /// <summary>
    /// 设置音效音量
    /// </summary>
    public void SetSFXVolume(float vol)
    {
        if (_soundTrack != null) _soundTrack.sfxVolume = Mathf.Clamp01(vol);
        _poolHelper?.SetVolume(GetMasterVolume(), GetSFXVolume());
    }

    /// <summary>
    /// 获取主音量
    /// </summary>
    public float GetMasterVolume() => _soundTrack != null ? _soundTrack.masterVolume : 0.7f;

    /// <summary>
    /// 获取音效音量
    /// </summary>
    public float GetSFXVolume() => _soundTrack != null ? _soundTrack.sfxVolume : 1f;

    /// <summary>
    /// 获取当前 SoundTrack 配置
    /// </summary>
    public SoundTrack GetSoundTrack() => _soundTrack;

    /// <summary>
    /// 运行时切换 SoundTrack 配置
    /// </summary>
    public void SetSoundTrack(SoundTrack track)
    {
        _soundTrack = track;
        if (track != null)
        {
            _poolHelper?.SetVolume(track.masterVolume, track.sfxVolume);
        }
    }

    // ════════════════════════════════════════════════════════════════
    // 内部工具
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 通过 SoundEntry 播放音效（自动处理冷却和音量）
    /// </summary>
    private void PlayEntry(SoundTrack.SoundEntry entry)
    {
        if (entry == null || !entry.IsValid) return;

        // 检查冷却
        if (entry.cooldown > 0f)
        {
            if (!_cooldownTracker.TryGetValue(entry, out float lastTime))
            {
                lastTime = 0f;
            }
            if (Time.unscaledTime - lastTime < entry.cooldown) return;
            _cooldownTracker[entry] = Time.unscaledTime;
        }

        _poolHelper.Play(entry.clip, entry.volume);
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
        _cooldownTracker.Clear();
    }
}

// ════════════════════════════════════════════════════════════════
// 枚举定义
// ════════════════════════════════════════════════════════════════

/// <summary>
/// DOT 元素类型（用于 PlayDotElement 调用）
/// </summary>
public enum DotElementType
{
    Bleed,
    Poison,
    Burn,
    Frost,
    Lightning,
    Dark,
    Light
}

/// <summary>
/// 技能音效类型（用于 PlaySkill 调用）
/// </summary>
public enum SkillSoundType
{
    Cast,
    Teleport,
    FrostNova,
    Lightning,
    Gravity,
    DeathAura,
    Berserk,
    TheWorld,
    WindWave
}