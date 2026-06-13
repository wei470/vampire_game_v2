using UnityEngine;

/// <summary>
/// DOT 子弹通用工具类 — 确保 StatusEffectManager 存在
/// 从 DotProjectile.cs 拆分而来
/// </summary>
public static class DotBulletHelper
{
    /// <summary>
    /// 确保敌人有 DotColorBlender（颜色混合系统）
    /// </summary>
    public static DotColorBlender EnsureColorBlender(GameObject enemy)
    {
        var blender = enemy.GetComponent<DotColorBlender>();
        if (blender == null)
            blender = enemy.AddComponent<DotColorBlender>();
        return blender;
    }

    /// <summary>
    /// 确保敌人有 StatusEffectManager（诅咒传播需要死亡事件注册）
    /// </summary>
    public static void EnsureStatusEffectManager(GameObject enemy)
    {
        var sem = enemy.GetComponent<StatusEffectManager>();
        if (sem == null)
            sem = enemy.AddComponent<StatusEffectManager>();

        var magePassive = GameReferences.Player?.GetComponent<MagePassive>();
        if (magePassive != null)
        {
            sem.DotDurationMultiplier = magePassive.GetDotDurationMultiplier();
            sem.DotFrequencyBonus = magePassive.DotFrequencyBonus;
            sem.CorrosionArmorReduction = magePassive.CorrosionArmorReduction;
            sem.WindErosionKnockback = magePassive.KnockbackBonus;
            sem.EternalAgonyDamageMult = magePassive.EternalAgonyActive ? 0.85f : 1f;
            sem.DotCritBurstChance = magePassive.DotCritBurstChance;
        }
    }
}

/// <summary>
/// DOT 子弹 Sprite 缓存 — 运行时生成椭圆/圆形 Sprite
/// </summary>
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

/// <summary>
/// #8 DOT 子弹视觉特效组件
/// </summary>
public static class DotBulletVisualEffects
{
    public static void AttachTrail(GameObject go, Color trailColor, float trailTime, float startWidth)
    {
        var trail = go.AddComponent<TrailRenderer>();
        trail.time = trailTime;
        trail.startWidth = startWidth;
        trail.endWidth = 0f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
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
        var glowSr = glow.AddComponent<SpriteRenderer>();
        glowSr.sprite = DotSpriteCache.CircleSprite();
        glowSr.color = new Color(1f, 0.6f, 0f, 0.3f);
        glowSr.sortingOrder = 14;
        var pulse = glow.AddComponent<FlamePulseEffect>();
        pulse.Init(glowSr);
    }

    public static void AttachFrostTrail(GameObject go)
    {
        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.3f;
        trail.startWidth = 0.2f;
        trail.endWidth = 0.05f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
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
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = new Color(0.7f, 0.85f, 1f, 0.6f);
        trail.endColor = new Color(0.7f, 0.85f, 1f, 0f);
        trail.numCapVertices = 2;
        trail.minVertexDistance = 0.03f;
        trail.sortingOrder = 14;

        // 风粒子光晕
        var glow = new GameObject("WindGlow");
        glow.transform.SetParent(go.transform);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localScale = Vector3.one * 1.3f;
        var glowSr = glow.AddComponent<SpriteRenderer>();
        glowSr.sprite = DotSpriteCache.CircleSprite();
        glowSr.color = new Color(0.7f, 0.85f, 1f, 0.2f);
        glowSr.sortingOrder = 14;
    }

    public static void AttachLightningTrail(GameObject go)
    {
        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.2f;
        trail.startWidth = 0.15f;
        trail.endWidth = 0.02f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = new Color(0.4f, 0.8f, 1f, 0.8f);
        trail.endColor = new Color(0.2f, 0.5f, 1f, 0f);
        trail.numCapVertices = 2;
        trail.minVertexDistance = 0.03f;
        trail.sortingOrder = 14;

        var glow = new GameObject("StaticGlow");
        glow.transform.SetParent(go.transform);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localScale = Vector3.one * 1.5f;
        var glowSr = glow.AddComponent<SpriteRenderer>();
        glowSr.sprite = DotSpriteCache.CircleSprite();
        glowSr.color = new Color(0.3f, 0.7f, 1f, 0.25f);
        glowSr.sortingOrder = 14;
        var pulse = glow.AddComponent<FlamePulseEffect>();
        pulse.Init(glowSr);
    }
}
