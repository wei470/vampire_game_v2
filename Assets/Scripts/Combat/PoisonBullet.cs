using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 毒子弹 — 直线飞行，命中第一个敌人后范围爆炸，叠加中毒层数
/// Mage 默认攻击子弹。从 DotProjectile.cs 拆分而来。已迁移到 DotBulletBase 基类。
/// </summary>
public class PoisonBullet : DotBulletBase
{
    private static readonly List<Collider2D> _overlapBuffer = new List<Collider2D>(16);
    private float _poisonDps = 2f;
    private float _poisonDuration = 5f;
    private float _explosionRadius = 0.5f;
    private bool _exploded;

    protected override StatusEffectType EffectType => StatusEffectType.Poison;

    public void SetupPoison(float speed, float poisonDps, float poisonDuration, float explosionRadius,
        float dmgMult, bool canCrit, float critChance, float critMult)
    {
        SetupBullet(speed, 5f, 0, dmgMult, canCrit, critChance, critMult);
        _poisonDps = poisonDps;
        _poisonDuration = poisonDuration;
        _explosionRadius = explosionRadius;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _exploded = false;
    }

    protected override void Update()
    {
        if (!_exploded && Time.time - _spawnTime > _lifetime) DespawnSelf();
        else if (_exploded && Time.time - _spawnTime > _lifetime + 1f) DespawnSelf();
    }

    protected override void FixedUpdate()
    {
        if (!_exploded && _rb != null) _rb.linearVelocity = _direction * _speed;
    }

    /// <summary>
    /// 重写命中逻辑：毒子弹的穿透/爆炸行为与基类不同
    /// </summary>
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (_exploded) return;
        if (!other.CompareTag("Enemy")) return;
        DotBulletHelper.EnsureStatusEffectManager(other.gameObject);

        // 穿透检查：先对当前敌人施加中毒DOT，然后检查是否可以继续穿透
        if (_cachedPenetrate == null) _cachedPenetrate = GetComponent<PenetrateHandler>();
        if (_cachedPenetrate != null && _cachedPenetrate.TryPenetrate(other))
        {
            var dmg = other.GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp > 0)
            {
                var poison = other.GetComponent<PoisonStackEffect>();
                if (poison == null) poison = other.gameObject.AddComponent<PoisonStackEffect>();
                poison.AddStack(_poisonDps * _damageMultiplier, _poisonDuration, _canCrit, _critChance, _critMult);
            }
            return;
        }

        // ── 元素反应：毒爆（中毒 × 黑暗）──
        bool hasDarkMark = other.GetComponent<DarkMarkEffect>() != null;
        LeavePuddle(transform.position, hasDarkMark);
    }

    protected override void OnHitEnemy(GameObject enemy) { }

    private void LeavePuddle(Vector2 center, bool darkMarkBonus = false)
    {
        _exploded = true;
        if (_rb != null) _rb.linearVelocity = Vector2.zero;

        // 从 Config 实时读取毒液池参数
        var cfg = DotEffectConfig.GetDefault();
        float explosionRadius = darkMarkBonus ? _explosionRadius * 2f : cfg.PoisonExplosionRadius * 2f;
        float puddleRadius = darkMarkBonus ? cfg.PoisonPuddleRadius : cfg.PoisonPuddleRadius * 0.5f;
        float puddleDuration = cfg.PoisonPuddleDuration;

        int count = PhysicsHelper.OverlapCircle(center, explosionRadius, _overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            var hit = _overlapBuffer[i];
            if (!hit.CompareTag("Enemy")) continue;
            var dmg = hit.GetComponent<Damageable>();
            if (dmg == null || dmg.CurrentHp <= 0) continue;
            var poison = hit.GetComponent<PoisonStackEffect>();
            if (poison == null) poison = hit.gameObject.AddComponent<PoisonStackEffect>();
            poison.AddStack(_poisonDps * _damageMultiplier, _poisonDuration, _canCrit, _critChance, _critMult);
        }
        PoisonPuddle.Create(center, puddleRadius, puddleDuration, _poisonDps * _damageMultiplier, _canCrit, _critChance, _critMult);

        if (darkMarkBonus)
        {
            CombatManager.CreateExplosionEffect(center, 1f,
                new Color(0.4f, 0.1f, 0.6f, 0.6f), 0.5f);
            ShowPoisonBurstText(center);
            DebugHelper.Log($"[PoisonBurst] 毒爆触发！爆炸范围={explosionRadius:F1}，毒圈范围={puddleRadius:F1}");
        }

        DespawnSelf();
    }

    private static void ShowPoisonBurstText(Vector2 pos)
    {
        var textObj = new GameObject("PoisonBurstText");
        textObj.transform.position = pos + new Vector2(0, 0.6f);
        textObj.transform.localScale = Vector3.one * 0.3f;

        var textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = "毒爆！";
        textMesh.characterSize = 0.2f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 50;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.color = new Color(0.4f, 0.1f, 0.6f);

        var ticker = textObj.AddComponent<PoisonBurstTextTicker>();
        ticker.Lifetime = 1.0f;
    }

    protected override void OnBulletDespawn()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.DOT_POISON_BULLET);
    }

    private static GameObject BuildTemplate()
    {
        var go = new GameObject("PoisonBullet");
        go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(0.1f, 0.9f, 0.2f); sr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * 0.25f;
        var rb = go.AddComponent<Rigidbody2D>(); rb.gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.25f;
        DotBulletVisualEffects.AttachTrail(go, new Color(0.1f, 0.9f, 0.2f, 0.6f), 0.4f, 0.03f);
        go.AddComponent<PoisonBullet>();
        return go;
    }

    public static PoisonBullet Create(Vector2 pos, Vector2 dir, float speed,
        float poisonDps, float poisonDuration, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var go = PoolHelper.SpawnOrFallback(PoolHelper.DOT_POISON_BULLET, BuildTemplate,
            () => {
                var g = new GameObject("PoisonBullet");
                g.tag = "Untagged";
                PhysicsLayerSetup.SetAsBullet(g);
                var sr = g.AddComponent<SpriteRenderer>();
                sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(0.1f, 0.9f, 0.2f); sr.sortingOrder = 15;
                g.transform.localScale = Vector3.one * 0.25f;
                g.AddComponent<Rigidbody2D>().gravityScale = 0f;
                var col = g.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.25f;
                DotBulletVisualEffects.AttachTrail(g, new Color(0.1f, 0.9f, 0.2f, 0.6f), 0.4f, 0.03f);
                g.AddComponent<PoisonBullet>();
                return g;
            }, pos);

        var b = go.GetComponent<PoisonBullet>();
        if (b == null) b = go.AddComponent<PoisonBullet>();
        b.SetupPoison(speed, poisonDps, poisonDuration, 0.5f, dmgMult, canCrit, critChance, critMult);
        b.SetDirection(dir);
        return b;
    }
}