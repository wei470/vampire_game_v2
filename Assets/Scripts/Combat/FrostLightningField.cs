using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 霜电冰场 — 元素反应"霜电"产生的冰场区域。
///
/// 效果：
/// - 半径=1，持续2秒
/// - 范围内敌人速度变为原来的70%（与霜冻减速乘法叠加）
/// - 显示"霜电！"文字
/// </summary>
public class FrostLightningField : MonoBehaviour
{
    private float _radius = 1f;
    private float _duration = 2f;
    private float _spawnTime;
    private float _lastFrostTick;
    private float _frostTickInterval = 1.25f; // 每1.25秒施加一层霜冻（从配置读取）
    private const float SLOW_PERCENT = 0.30f; // 霜冻基础减速30%

    /// <summary>
    /// 创建霜电冰场
    /// </summary>
    public static FrostLightningField Create(Vector2 center, float radius, float duration)
    {
        // 从配置读取参数（如果未指定则使用配置默认值）
        var config = DotBulletConfig.GetDefault();
        if (radius <= 0f) radius = config.FrostLightningFieldRadius;
        if (duration <= 0f) duration = config.FrostLightningFieldDuration;

        var go = new GameObject("FrostLightningField");
        go.transform.position = center;

        // 可视化：半透明蓝白圆
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite();
        sr.color = new Color(0.4f, 0.7f, 1f, 0.25f);
        sr.sortingOrder = 2;
        go.transform.localScale = Vector3.one * (radius * 2f);

        var field = go.AddComponent<FrostLightningField>();
        field._radius = radius;
        field._duration = duration;
        field._spawnTime = Time.time;
        field._frostTickInterval = config.FrostLightningTickInterval;
        field._lastFrostTick = Time.time - field._frostTickInterval; // 立即触发第一次

        // 显示"霜电！"文字
        ShowFrostLightningText(center, radius);

        DebugHelper.Log($"[FrostLightning] 冰场创建！pos={center}, r={radius}, dur={duration}");

        return field;
    }

    private void Update()
    {
        float elapsed = Time.time - _spawnTime;
        if (elapsed >= _duration)
        {
            CleanupAndDestroy();
            return;
        }

        // 每0.5秒对范围内敌人施加一层霜冻
        if (Time.time - _lastFrostTick >= _frostTickInterval)
        {
            _lastFrostTick = Time.time;
            ApplyFrostToTracked();
        }

        // 更新视觉淡出
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float fadeStart = _duration * 0.6f;
            if (elapsed > fadeStart)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(0.25f, 0f, (elapsed - fadeStart) / (_duration - fadeStart));
                sr.color = c;
            }
        }
    }

    /// <summary>
    /// 用距离检测对范围内敌人施加霜冻（避免物理层问题）
    /// </summary>
    private void ApplyFrostToTracked()
    {
        var spawnMgr = GameReferences.SpawnManager;
        IReadOnlyList<GameObject> enemies = spawnMgr?.ActiveEnemies;
        if (enemies == null || enemies.Count == 0) return;

        // 检测半径扩大4.5倍以匹配视觉范围
        float checkRadius = _radius * 5f;
        float radiusSqr = checkRadius * checkRadius;
        int applied = 0;

        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null || !e.activeInHierarchy) continue;

            Vector2 delta = (Vector2)e.transform.position - (Vector2)transform.position;
            if (delta.sqrMagnitude > radiusSqr) continue;

            var dmg = e.GetComponent<Damageable>();
            if (dmg == null || dmg.CurrentHp <= 0) continue;

            // 施加一层霜冻（减速30%基础，每层+5%）
            DotBulletHelper.EnsureStatusEffectManager(e);
            var frost = e.GetComponent<FrostEffect>();
            if (frost == null) frost = e.AddComponent<FrostEffect>();
            frost.ApplyFreeze(0f, SLOW_PERCENT, 0f, false, 0f, 0f);
            applied++;
        }
        if (applied > 0)
            DebugHelper.Log($"[FrostLightning] 施加霜冻给 {applied} 个敌人");
    }

    private void CleanupAndDestroy()
    {
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        DotBulletConfig.OnConfigChanged -= RefreshFromConfig;
    }

    private void Awake()
    {
        DotBulletConfig.OnConfigChanged += RefreshFromConfig;
    }

    private void RefreshFromConfig()
    {
        var cfg = DotBulletConfig.GetDefault();
        _duration = cfg.FrostLightningFieldDuration;
        _radius = cfg.FrostLightningFieldRadius;
        _frostTickInterval = cfg.FrostLightningTickInterval;
    }

    /// <summary>
    /// 显示"霜电！"文字
    /// </summary>
    private static void ShowFrostLightningText(Vector2 pos, float radius)
    {
        var textObj = new GameObject("FrostLightningText");
        textObj.transform.position = pos + new Vector2(0, radius + 0.3f);
        textObj.transform.localScale = Vector3.one * 0.3f;

        var textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = "霜电！";
        textMesh.characterSize = 0.2f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 50;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.color = new Color(0.4f, 0.7f, 1f); // 蓝白色

        var ticker = textObj.AddComponent<ReactionTextTicker>();
        ticker.Lifetime = 1.5f;
    }
}

/// <summary>
/// 通用反应文字飘动组件 — 向上飘动并淡出
/// </summary>
public class ReactionTextTicker : MonoBehaviour
{
    public float Lifetime = 1.0f;
    private float _spawnTime;
    private TextMesh _textMesh;

    private void Awake()
    {
        _spawnTime = Time.time;
        _textMesh = GetComponent<TextMesh>();
        Destroy(gameObject, Lifetime + 1f);
    }

    private void OnEnable()
    {
        _spawnTime = Time.time;
    }

    private void Update()
    {
        float elapsed = Time.time - _spawnTime;
        if (elapsed >= Lifetime)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += Vector3.up * Time.deltaTime * 1.5f;

        if (_textMesh != null)
        {
            Color c = _textMesh.color;
            c.a = Mathf.Clamp01(1f - (elapsed / Lifetime));
            _textMesh.color = c;
        }
    }

    private void OnDisable()
    {
        if (gameObject != null) Destroy(gameObject);
    }
}