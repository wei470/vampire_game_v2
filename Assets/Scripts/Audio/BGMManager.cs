using UnityEngine;

/// <summary>
/// 背景音乐管理器 — 从 SoundTrack 配置加载 BGM 曲目并循环播放。
/// 自动创建 AudioSource，支持随机打乱播放列表。
/// 入口：挂载到场景 GameObject 上，或由 GameSceneBootstrap 自动创建。
/// </summary>
public class BGMManager : MonoBehaviour
{
    private AudioSource _audioSource;
    private AudioClip[] _clips;
    private int _currentIndex = 0;
    private bool _isPlaying = false;

    [Header("配置")]
    [Tooltip("音效配置包（ScriptableObject），统一管理 BGM 曲目")]
    [SerializeField] private SoundTrack _soundTrack;

    [Tooltip("BGM 音量（运行时值，优先级高于 SoundTrack.bgmVolume）")]
    [SerializeField] private float _volume = 0.5f;

    private static BGMManager _instance;

    public static BGMManager Instance => _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.loop = false;
        _audioSource.playOnAwake = false;
        _audioSource.volume = _volume;

        // 如果没有手动设置 SoundTrack，从 SFXManager 获取共享的
        if (_soundTrack == null && SFXManager.Instance != null)
        {
            _soundTrack = SFXManager.Instance.GetSoundTrack();
        }

        LoadFromSoundTrack();
    }

    /// <summary>
    /// 从 SoundTrack 加载 BGM 曲目
    /// </summary>
    private void LoadFromSoundTrack()
    {
        if (_soundTrack != null && _soundTrack.bgmClips != null && _soundTrack.bgmClips.Length > 0)
        {
            _clips = _soundTrack.bgmClips;
            _volume = _soundTrack.bgmVolume;
            if (_audioSource != null) _audioSource.volume = _volume;
            DebugHelper.Log($"[BGMManager] Loaded {_clips.Length} clips from SoundTrack");

            if (_soundTrack.bgmShuffle)
                ShuffleClips();
        }
        else
        {
            // 备用：从 Resources/Audio 加载
            LoadFromResources();
        }
    }

    /// <summary>
    /// 从 Resources/Audio 加载（备用方案）
    /// </summary>
    private void LoadFromResources()
    {
        var loaded = Resources.LoadAll<AudioClip>("Audio");
        if (loaded != null && loaded.Length > 0)
        {
            _clips = loaded;
            DebugHelper.Log($"[BGMManager] Loaded {_clips.Length} clips from Resources/Audio");
        }
        else
        {
            // 尝试逐个加载已知文件
            var clip1 = Resources.Load<AudioClip>("Audio/music1");
            var clip2 = Resources.Load<AudioClip>("Audio/music2");
            var list = new System.Collections.Generic.List<AudioClip>();
            if (clip1 != null) list.Add(clip1);
            if (clip2 != null) list.Add(clip2);
            _clips = list.ToArray();
            if (_clips.Length > 0)
                DebugHelper.Log($"[BGMManager] Loaded {_clips.Length} clips individually");
        }
    }

    /// <summary>
    /// 手动设置音频剪辑（编辑器中拖拽或代码设置）
    /// </summary>
    public void SetClips(AudioClip[] clips)
    {
        _clips = clips;
        ShuffleClips();
    }

    /// <summary>
    /// 设置 SoundTrack 配置并重新加载 BGM
    /// </summary>
    public void SetSoundTrack(SoundTrack track)
    {
        _soundTrack = track;
        LoadFromSoundTrack();
    }

    /// <summary>
    /// Fisher-Yates 洗牌
    /// </summary>
    private void ShuffleClips()
    {
        if (_clips == null || _clips.Length <= 1) return;

        for (int i = _clips.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = _clips[i];
            _clips[i] = _clips[j];
            _clips[j] = temp;
        }
    }

    /// <summary>
    /// 开始播放背景音乐
    /// </summary>
    public void Play()
    {
        if (_clips == null || _clips.Length == 0)
        {
            DebugHelper.LogWarning("[BGMManager] No clips to play");
            return;
        }
        _isPlaying = true;
        PlayNext();
    }

    /// <summary>
    /// 播放指定名称的音频剪辑
    /// </summary>
    public void PlayClip(string clipName)
    {
        var clip = Resources.Load<AudioClip>($"Audio/{clipName}");
        if (clip == null)
        {
            DebugHelper.Log($"[BGMManager] Clip not found: Audio/{clipName} (upload file to Assets/Resources/Audio/)");
            return;
        }
        _audioSource.clip = clip;
        _audioSource.loop = true;
        _audioSource.Play();
        _isPlaying = true;
        DebugHelper.Log($"[BGMManager] Playing: {clipName}");
    }

    /// <summary>
    /// 停止播放
    /// </summary>
    public void Stop()
    {
        _isPlaying = false;
        if (_audioSource != null) _audioSource.Stop();
    }

    /// <summary>
    /// 暂停
    /// </summary>
    public void Pause()
    {
        if (_audioSource != null) _audioSource.Pause();
    }

    /// <summary>
    /// 恢复
    /// </summary>
    public void Resume()
    {
        if (_audioSource != null) _audioSource.UnPause();
    }

    /// <summary>
    /// 设置音量
    /// </summary>
    public void SetVolume(float vol)
    {
        _volume = Mathf.Clamp01(vol);
        if (_audioSource != null) _audioSource.volume = _volume;
    }

    /// <summary>
    /// 获取当前音量
    /// </summary>
    public float GetVolume() => _volume;

    private void Update()
    {
        if (!_isPlaying || _clips == null || _clips.Length == 0) return;

        // 当前曲目播放完毕，播放下一首
        if (!_audioSource.isPlaying && _audioSource.clip != null)
        {
            _currentIndex = (_currentIndex + 1) % _clips.Length;
            PlayNext();
        }
    }

    private void PlayNext()
    {
        if (_clips == null || _clips.Length == 0) return;
        if (_currentIndex >= _clips.Length) _currentIndex = 0;

        _audioSource.clip = _clips[_currentIndex];
        _audioSource.Play();
        DebugHelper.Log($"[BGMManager] Playing: {_audioSource.clip.name}");
    }
}