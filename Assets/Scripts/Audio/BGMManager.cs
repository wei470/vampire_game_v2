using UnityEngine;

/// <summary>
/// 背景音乐管理器 — 加载 Assets/Audio/ 下的 mp3 文件并循环播放。
/// 自动创建 AudioSource，随机打乱播放列表。
/// 入口：挂载到场景 GameObject 上，或由 GameSceneBootstrap 自动创建。
/// </summary>
public class BGMManager : MonoBehaviour
{
    private AudioSource _audioSource;
    private AudioClip[] _clips;
    private int _currentIndex = 0;
    private bool _isPlaying = false;

    [Header("配置")]
    [SerializeField] private float _volume = 0f; // 静音（开发阶段）
    [SerializeField] private bool _shuffle = true;

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

        LoadAudioClips();
    }

    private void LoadAudioClips()
    {
        // 从 Resources 或直接从路径加载
        // Unity 不支持直接从 Assets/Audio 运行时加载，需要用 Resources
        // 但这里我们用另一种方式：通过 AssetDatabase 在编辑器加载
        // 运行时用 Resources.LoadAll 或手动指定

        var loaded = Resources.LoadAll<AudioClip>("Audio");
        if (loaded != null && loaded.Length > 0)
        {
            _clips = loaded;
            DebugHelper.Log($"[BGMManager] Loaded {_clips.Length} clips from Resources/Audio");
        }
        else
        {
            // 备用：尝试直接加载（需要文件在 Resources 目录）
            DebugHelper.LogWarning("[BGMManager] No audio clips found in Resources/Audio. " +
                "Please move audio files to Assets/Resources/Audio/ or use GameSceneBootstrap to configure.");
            _clips = new AudioClip[0];
        }

        if (_shuffle && _clips.Length > 1)
        {
            // Fisher-Yates shuffle
            for (int i = _clips.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = _clips[i];
                _clips[i] = _clips[j];
                _clips[j] = temp;
            }
        }
    }

    /// <summary>
    /// 手动设置音频剪辑（编辑器中拖拽或代码设置）
    /// </summary>
    public void SetClips(AudioClip[] clips)
    {
        _clips = clips;
        if (_shuffle && _clips.Length > 1)
        {
            for (int i = _clips.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = _clips[i];
                _clips[i] = _clips[j];
                _clips[j] = temp;
            }
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