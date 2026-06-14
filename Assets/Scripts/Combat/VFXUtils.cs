using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// VFX 工具集 — 合并 MaterialCache / VFXPool / GlowReturnHelper / DotSpriteCache / DotBulletVisualEffects。
/// 所有 VFX 相关的缓存、池化、Sprite 生成、特效挂载都在此文件。
/// </summary>

#region Material 缓存

public static class MaterialCache
{
    private static Material _defaultMaterial;
    public static Material GetDefault()
    {
        if (_defaultMaterial == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null) _defaultMaterial = new Material(shader);
        }
        return _defaultMaterial;
    }
}

#endregion

#region VFX 对象池

public static class VFXPool
{
    private static readonly Dictionary<string, Stack<GameObject>> _pools = new();
    private static readonly List<DelayedReturn> _pendingReturns = new(32);

    private struct DelayedReturn
    {
        public GameObject obj;
        public float returnTime;
        public string poolKey;
    }

    public static GameObject Get(string poolKey)
    {
        if (_pools.TryGetValue(poolKey, out var pool))
        {
            while (pool.Count > 0)
            {
                var obj = pool.Pop();
                if (obj != null) { obj.SetActive(true); return obj; }
            }
        }
        return new GameObject(poolKey);
    }

    public static void Return(GameObject obj, float delay)
    {
        if (obj == null) return;
        if (delay <= 0f) { ReturnImmediate(obj); return; }
        _pendingReturns.Add(new DelayedReturn { obj = obj, returnTime = Time.time + delay, poolKey = obj.name });
    }

    public static void ReturnImmediate(GameObject obj)
    {
        if (obj == null) return;
        obj.SetActive(false);
        if (!_pools.TryGetValue(obj.name, out var pool))
        {
            pool = new Stack<GameObject>(8);
            _pools[obj.name] = pool;
        }
        pool.Push(obj);
    }

    public static void UpdatePendingReturns()
    {
        float now = Time.time;
        for (int i = _pendingReturns.Count - 1; i >= 0; i--)
        {
            if (now >= _pendingReturns[i].returnTime)
            {
                ReturnImmediate(_pendingReturns[i].obj);
                _pendingReturns.RemoveAt(i);
            }
        }
    }

    public static void ClearAll()
    {
        foreach (var pool in _pools.Values) pool.Clear();
        _pools.Clear();
        _pendingReturns.Clear();
    }
}

#endregion

#region Glow 池

public class GlowReturnHelper : MonoBehaviour
{
    private static readonly Stack<GameObject> _glowPool = new(16);

    private void OnDisable() { ReturnToPool(gameObject); }

    public static void ReturnToPool(GameObject glow)
    {
        if (glow == null) return;
        glow.SetActive(false);
        glow.transform.SetParent(null);
        _glowPool.Push(glow);
    }

    public static GameObject GetOrCreate()
    {
        while (_glowPool.Count > 0)
        {
            var g = _glowPool.Pop();
            if (g != null) return g;
        }
        var glow = new GameObject("Glow");
        var sr = glow.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 14;
        return glow;
    }
}

#endregion

#region Sprite 缓存

public static class DotSpriteCache
{
    private static Sprite _cachedSprite;
    private static Sprite _cachedCircle;

    public static Sprite Get()
    {
        if (_cachedSprite != null) return _cachedSprite;
        int w = 16, h = 8;
        var tex = new Texture2D(w, h);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                float cx = (x - 7.5f) / 7.5f;
                float cy = (y - 3.5f) / 3.5f;
                tex.SetPixel(x, y, cx * cx + cy * cy <= 1f ? Color.white : new Color(0, 0, 0, 0));
            }
        tex.Apply();
        _cachedSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 10f);
        return _cachedSprite;
    }

    public static Sprite CircleSprite()
    {
        if (_cachedCircle != null) return _cachedCircle;
        int size = 64;
        var tex = new Texture2D(size, size);
        float center = size / 2f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                tex.SetPixel(x, y, dist <= 1f ? new Color(1, 1, 1, 1f - dist) : new Color(0, 0, 0, 0));
            }
        tex.Apply();
        _cachedCircle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 10f);
        return _cachedCircle;
    }
}

#endregion

#region 子弹视觉特效

public static class DotBulletVisualEffects
{
    private static Material Mat => MaterialCache.GetDefault();

    public static void AttachTrail(GameObject go, Color trailColor, float trailTime, float startWidth)
    {
        var trail = go.AddComponent<TrailRenderer>();
        trail.time = trailTime;
        trail.startWidth = startWidth;
        trail.endWidth = 0f;
        trail.material = Mat;
        trail.startColor = trailColor;
        trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
        trail.numCapVertices = 2;
        trail.minVertexDistance = 0.05f;
        trail.sortingOrder = 14;
    }

    public static void AttachFlameEffect(GameObject go)
    {
        var glow = new GameObject("FlameGlow");
        glow.transform.SetParent(go.transform);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localScale = Vector3.one * 1.8f;
        var sr = glow.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite();
        sr.color = new Color(1f, 0.6f, 0f, 0.3f);
        sr.sortingOrder = 14;
        glow.AddComponent<FlamePulseEffect>().Init(sr);
    }

    public static void AttachFrostTrail(GameObject go)
    {
        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.3f;
        trail.startWidth = 0.2f;
        trail.endWidth = 0.05f;
        trail.material = Mat;
        trail.startColor = new Color(0.5f, 0.8f, 1f, 0.7f);
        trail.endColor = new Color(0.5f, 0.8f, 1f, 0f);
        trail.numCapVertices = 3;
        trail.minVertexDistance = 0.03f;
        trail.sortingOrder = 14;
        go.AddComponent<FrostGhostSpawner>();
    }

    public static void AttachSpinEffect(GameObject go, float spinSpeed)
    {
        go.AddComponent<SpinEffect>().Init(spinSpeed);
    }

    public static void AttachWindTrail(GameObject go)
    {
        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.15f;
        trail.startWidth = 0.1f;
        trail.endWidth = 0.02f;
        trail.material = Mat;
        trail.startColor = new Color(0.7f, 0.85f, 1f, 0.6f);
        trail.endColor = new Color(0.7f, 0.85f, 1f, 0f);
        trail.numCapVertices = 2;
        trail.minVertexDistance = 0.03f;
        trail.sortingOrder = 14;

        var glow = new GameObject("WindGlow");
        glow.transform.SetParent(go.transform);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localScale = Vector3.one * 1.3f;
        var sr = glow.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite();
        sr.color = new Color(0.7f, 0.85f, 1f, 0.2f);
        sr.sortingOrder = 14;
    }

    public static void AttachLightningTrail(GameObject go)
    {
        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.2f;
        trail.startWidth = 0.15f;
        trail.endWidth = 0.02f;
        trail.material = Mat;
        trail.startColor = new Color(0.4f, 0.8f, 1f, 0.8f);
        trail.endColor = new Color(0.2f, 0.5f, 1f, 0f);
        trail.numCapVertices = 2;
        trail.minVertexDistance = 0.03f;
        trail.sortingOrder = 14;

        var glow = new GameObject("StaticGlow");
        glow.transform.SetParent(go.transform);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localScale = Vector3.one * 1.5f;
        var sr = glow.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite();
        sr.color = new Color(0.3f, 0.7f, 1f, 0.25f);
        sr.sortingOrder = 14;
        glow.AddComponent<FlamePulseEffect>().Init(sr);
    }
}

#endregion

#region VFX MonoBehaviour 组件

public class SpinEffect : MonoBehaviour
{
    private float _spinSpeed = 360f;
    public void Init(float speed) { _spinSpeed = speed; }
    private void Update() { transform.Rotate(0, 0, _spinSpeed * Time.deltaTime); }
}

public class GhostFadeOut : MonoBehaviour
{
    private float _lifetime;
    private float _spawnTime;
    private SpriteRenderer _sr;

    public void Init(float lifetime) { _lifetime = lifetime; _spawnTime = Time.time; _sr = GetComponent<SpriteRenderer>(); }

    private void Update()
    {
        float elapsed = Time.time - _spawnTime;
        if (elapsed >= _lifetime) { Destroy(gameObject); return; }
        float t = 1f - (elapsed / _lifetime);
        if (_sr != null) { var c = _sr.color; c.a = 0.5f * t; _sr.color = c; }
        transform.localScale = Vector3.one * (0.3f * t);
    }
}

public class FlamePulseEffect : MonoBehaviour
{
    private SpriteRenderer _sr;
    private float _baseAlpha = 0.3f;
    private float _pulseSpeed = 12f;

    public void Init(SpriteRenderer sr) { _sr = sr; }

    private void Update()
    {
        if (_sr == null) return;
        float pulse = Mathf.Sin(Time.time * _pulseSpeed) * 0.15f;
        var c = _sr.color;
        c.a = _baseAlpha + pulse;
        _sr.color = c;
        transform.localScale = Vector3.one * (1.8f + Mathf.Sin(Time.time * _pulseSpeed * 0.7f) * 0.3f);
    }
}

public class LaserFadeOut : MonoBehaviour
{
    private float _duration;
    private float _startTime;
    private SpriteRenderer _sr;

    public void Init(float duration)
    {
        _duration = duration;
        _startTime = Time.unscaledTime;
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (_sr == null) return;
        float elapsed = Time.unscaledTime - _startTime;
        float alpha = 1f - (elapsed / _duration);
        if (alpha <= 0f) { Destroy(gameObject); return; }
        _sr.color = new Color(1f, 1f, 1f, alpha * 0.7f);
    }
}

/// <summary>
/// 定时自毁 — 使用 unscaledTime，不受 timeScale 影响。
/// 挂载到需要定时销毁的 VFX 对象上。
/// </summary>
public class TimedSelfDestruct : MonoBehaviour
{
    private float _destroyTime;

    public void Setup(float delay)
    {
        _destroyTime = Time.unscaledTime + delay;
    }

    private void Update()
    {
        if (Time.unscaledTime >= _destroyTime)
            Destroy(gameObject);
    }
}

#endregion
