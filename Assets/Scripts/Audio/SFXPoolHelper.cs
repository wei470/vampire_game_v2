using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 音效池管理工具，封装 AudioSource 对象池的创建、获取和播放逻辑。
/// 从 SFXManager 中提取，减少主类代码量。
/// </summary>
public class SFXPoolHelper
{
    private List<AudioSource> _pool;
    private List<AudioSource> _spatialPool;
    private Transform _poolParent;
    private Transform _spatialPoolParent;
    private float _masterVolume;
    private float _sfxVolume;

    private const float SpatialMaxDistance = 20f;
    private const float SpatialMinDistance = 1f;
    private const int DefaultSpatialPoolSize = 16;

    public SFXPoolHelper(int poolSize, Transform parent, float masterVolume, float sfxVolume)
        : this(poolSize, DefaultSpatialPoolSize, parent, masterVolume, sfxVolume)
    {
    }

    public SFXPoolHelper(int poolSize, int spatialPoolSize, Transform parent, float masterVolume, float sfxVolume)
    {
        _masterVolume = masterVolume;
        _sfxVolume = sfxVolume;
        _pool = new List<AudioSource>(poolSize);
        _poolParent = new GameObject("SFX_Pool").transform;
        _poolParent.SetParent(parent);

        for (int i = 0; i < poolSize; i++)
        {
            _pool.Add(CreateAudioSource());
        }

        _spatialPool = new List<AudioSource>(spatialPoolSize);
        _spatialPoolParent = new GameObject("SFX_SpatialPool").transform;
        _spatialPoolParent.SetParent(parent);

        for (int i = 0; i < spatialPoolSize; i++)
        {
            _spatialPool.Add(CreateSpatialAudioSource());
        }
    }

    /// <summary>
    /// 更新音量设置
    /// </summary>
    public void SetVolume(float master, float sfx)
    {
        _masterVolume = master;
        _sfxVolume = sfx;
    }

    /// <summary>
    /// 创建一个 2D AudioSource 组件
    /// </summary>
    private AudioSource CreateAudioSource()
    {
        var go = new GameObject("SFX_AudioSource");
        go.transform.SetParent(_poolParent);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f; // 2D 音效
        return source;
    }

    /// <summary>
    /// 创建一个 3D 空间音频 AudioSource 组件
    /// </summary>
    private AudioSource CreateSpatialAudioSource()
    {
        var go = new GameObject("SFX_SpatialSource");
        go.transform.SetParent(_spatialPoolParent);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f; // 3D 音效
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = SpatialMinDistance;
        source.maxDistance = SpatialMaxDistance;
        source.dopplerLevel = 0f;
        return source;
    }

    /// <summary>
    /// 从池中获取一个空闲的 AudioSource
    /// </summary>
    public AudioSource GetSource()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (!_pool[i].isPlaying)
                return _pool[i];
        }

        // 池满了，扩展一个
        var newSource = CreateAudioSource();
        _pool.Add(newSource);
        DebugHelper.Log($"[SFXPoolHelper] Pool expanded to {_pool.Count}");
        return newSource;
    }

    /// <summary>
    /// 播放指定音效
    /// </summary>
    public void Play(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        var source = GetSource();
        source.clip = clip;
        source.volume = _masterVolume * _sfxVolume * volumeScale;
        source.pitch = 1f + Random.Range(-0.05f, 0.05f);
        source.Play();
    }

    /// <summary>
    /// 播放指定音效（指定音高）
    /// </summary>
    public void Play(AudioClip clip, float volumeScale, float pitch)
    {
        if (clip == null) return;

        var source = GetSource();
        source.clip = clip;
        source.volume = _masterVolume * _sfxVolume * volumeScale;
        source.pitch = pitch;
        source.Play();
    }

    /// <summary>
    /// 从空间音频池中获取一个空闲的 3D AudioSource
    /// </summary>
    private AudioSource GetSpatialSource()
    {
        for (int i = 0; i < _spatialPool.Count; i++)
        {
            if (!_spatialPool[i].isPlaying)
                return _spatialPool[i];
        }

        var newSource = CreateSpatialAudioSource();
        _spatialPool.Add(newSource);
        DebugHelper.Log($"[SFXPoolHelper] Spatial pool expanded to {_spatialPool.Count}");
        return newSource;
    }

    /// <summary>
    /// 在指定位置播放 3D 音效（使用独立空间音频池，带距离衰减）
    /// </summary>
    public void PlayAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip == null) return;

        var source = GetSpatialSource();
        source.transform.position = position;
        source.clip = clip;
        source.volume = _masterVolume * _sfxVolume * volumeScale;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = SpatialMinDistance;
        source.maxDistance = SpatialMaxDistance;
        source.pitch = 1f + Random.Range(-0.05f, 0.05f);
        source.Play();
    }

    /// <summary>
    /// 带冷却的音效播放（防止音效轰炸）
    /// </summary>
    public void PlayWithCooldown(ref float lastTime, float cooldown, AudioClip clip, float volumeScale)
    {
        if (clip == null) return;
        if (Time.unscaledTime - lastTime < cooldown) return;
        lastTime = Time.unscaledTime;
        Play(clip, volumeScale);
    }

    /// <summary>
    /// 停止所有正在播放的音效
    /// </summary>
    public void StopAll()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (_pool[i].isPlaying)
                _pool[i].Stop();
        }
        for (int i = 0; i < _spatialPool.Count; i++)
        {
            if (_spatialPool[i].isPlaying)
                _spatialPool[i].Stop();
        }
    }
}