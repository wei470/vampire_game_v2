using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 毒液池 — 敌人站在上面会叠加中毒层数
/// </summary>
public class PoisonPuddle : MonoBehaviour
{
    private static readonly List<Collider2D> _overlapBuffer = new List<Collider2D>(16);
    private float _radius;
    private float _duration;
    private float _baseDps;
    private bool _canCrit; private float _critChance, _critMult;
    private float _spawnTime;
    private float _lastTick;
    private CircleCollider2D _cachedCol;
    private SpriteRenderer _cachedSr;

    public void Setup(float radius, float duration, float baseDps, bool canCrit, float critChance, float critMult)
    {
        _radius = radius; _duration = duration; _baseDps = baseDps;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
        ApplyRadius();
    }

    private void ApplyRadius()
    {
        transform.localScale = Vector3.one * _radius;
        if (_cachedCol == null) _cachedCol = GetComponent<CircleCollider2D>();
        if (_cachedCol != null) { _cachedCol.isTrigger = true; _cachedCol.radius = _radius; }
    }

    private void Awake()
    {
        _cachedCol = GetComponent<CircleCollider2D>();
        _cachedSr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        DotBulletBase.ActiveDotBullets.Add(this);
        _spawnTime = Time.time; _lastTick = Time.time - 0.5f;
        ApplyRadius();
        if (_cachedSr != null) _cachedSr.color = new Color(0.1f, 0.7f, 0.1f, 0.4f);
        DotEffectConfig.OnConfigChanged += RefreshFromConfig;
    }

    private void OnDisable()
    {
        DotBulletBase.ActiveDotBullets.Remove(this);
        DotEffectConfig.OnConfigChanged -= RefreshFromConfig;
    }

    private void RefreshFromConfig()
    {
        _duration = DotEffectConfig.GetDefault().PoisonPuddleDuration;
    }

    private void Update()
    {
        if (Time.time - _spawnTime > _duration) { DespawnSelf(); return; }
        if (Time.time - _lastTick < 0.5f) return;
        _lastTick = Time.time;
        ApplyPoisonToNearby();
    }

    private void ApplyPoisonToNearby()
    {
        int count = PhysicsHelper.OverlapCircle(transform.position, _radius, _overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            var hit = _overlapBuffer[i];
            if (!hit.CompareTag("Enemy")) continue;
            var dmg = hit.GetComponent<Damageable>();
            if (dmg == null || dmg.CurrentHp <= 0) continue;
            var poison = hit.GetComponent<PoisonStackEffect>();
            if (poison == null) poison = hit.gameObject.AddComponent<PoisonStackEffect>();
            poison.AddStack(_baseDps, _duration - (Time.time - _spawnTime), _canCrit, _critChance, _critMult);
        }
    }

    private void DespawnSelf()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.DOT_POISON_PUDDLE);
    }

    private static GameObject BuildPuddleTemplate()
    {
        var go = new GameObject("PoisonPuddle");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(0.1f, 0.7f, 0.1f, 0.4f); sr.sortingOrder = 1;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        go.AddComponent<PoisonPuddle>();
        return go;
    }

    public static PoisonPuddle Create(Vector2 pos, float radius, float duration, float baseDps,
        bool canCrit, float critChance, float critMult)
    {
        var pool = ObjectPool.Instance;
        GameObject go = null;
        if (pool != null && pool.HasPool(PoolHelper.DOT_POISON_PUDDLE))
        {
            go = pool.Spawn(PoolHelper.DOT_POISON_PUDDLE, pos, Quaternion.identity);
        }
        else
        {
            PoolHelper.RegisterVirtualPrefab(PoolHelper.DOT_POISON_PUDDLE, BuildPuddleTemplate, 8);
            go = pool != null ? pool.Spawn(PoolHelper.DOT_POISON_PUDDLE, pos, Quaternion.identity) : null;
        }

        if (go == null)
        {
            go = new GameObject("PoisonPuddle");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(0.1f, 0.7f, 0.1f, 0.4f); sr.sortingOrder = 1;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            go.AddComponent<PoisonPuddle>();
        }

        go.transform.position = pos;
        go.SetActive(true);
        var p = go.GetComponent<PoisonPuddle>();
        if (p == null) p = go.AddComponent<PoisonPuddle>();
        p.Setup(radius, duration, baseDps, canCrit, critChance, critMult);
        return p;
    }
}
