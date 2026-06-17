using UnityEngine;

/// <summary>
/// 霜冻子弹 — 快速子弹，冰冻敌人 + 永久减速 + 霜伤
/// 从 DotProjectile.cs 拆分而来，已迁移到 DotBulletBase 基类
/// </summary>
public class FrostBullet : DotBulletBase
{
    private float _frostDps = 2f;
    private float _freezeDuration = 1f;
    private float _slowPercent = 0.3f;

    protected override StatusEffectType EffectType => StatusEffectType.Frostbite;
    protected override Color DefaultBulletColor => new Color(0.3f, 0.6f, 1f);
    protected override Color DefaultTrailStartColor => new Color(0.5f, 0.8f, 1f, 0.7f);

    /// <summary>
    /// 霜冻子弹专属参数设置
    /// </summary>
    public void SetupFrost(float speed, int impactDmg, float frostDps, float freezeDuration,
        float slowPercent, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        SetupBullet(speed, 2f, impactDmg, dmgMult, canCrit, critChance, critMult);
        _frostDps = frostDps;
        _freezeDuration = freezeDuration;
        _slowPercent = slowPercent;
    }

    protected override void OnHitEnemy(GameObject enemy)
    {
        var frost = enemy.GetComponent<FrostEffect>();
        if (frost == null) frost = enemy.AddComponent<FrostEffect>();
        frost.ApplyFreeze(_freezeDuration, _slowPercent, 0f, false, 0f, 0f);

        // ── 元素反应：霜电（霜冻 × 雷电）──
        var staticEffect = enemy.GetComponent<StaticStackEffect>();
        if (staticEffect != null && staticEffect.StackCount > 0)
        {
            TryTriggerFrostLightning(enemy.transform.position, staticEffect);
        }
    }

    /// <summary>
    /// 元素反应：霜电 — 消耗一层静电，在敌人周围生成冰场
    /// </summary>
    private static void TryTriggerFrostLightning(Vector2 pos, StaticStackEffect staticEff)
    {
        if (!staticEff.ConsumeStack()) return;
        FrostLightningField.Create(pos, 1f, 1f);
        DebugHelper.Log($"[FrostLightning] 霜电反应触发！pos={pos}");
    }

    protected override void OnBulletDespawn()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.DOT_FROST_BULLET);
    }

    private static GameObject BuildTemplate()
    {
        var go = new GameObject("FrostBullet");
        go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.Get(); sr.color = new Color(0.3f, 0.6f, 1f); sr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * 0.5f;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(0.4f, 0.2f);
        DotBulletVisualEffects.AttachFrostTrail(go);
        go.AddComponent<FrostBullet>();
        return go;
    }

    public static FrostBullet Create(Vector2 pos, Vector2 dir, float speed, int impactDmg,
        float frostDps, float freezeDuration, float slowPercent, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var go = PoolHelper.SpawnOrFallback(PoolHelper.DOT_FROST_BULLET, BuildTemplate,
            () => {
                var g = new GameObject("FrostBullet");
                g.tag = "Untagged";
                PhysicsLayerSetup.SetAsBullet(g);
                var sr = g.AddComponent<SpriteRenderer>();
                sr.sprite = DotSpriteCache.Get(); sr.color = new Color(0.3f, 0.6f, 1f); sr.sortingOrder = 15;
                g.transform.localScale = Vector3.one * 0.5f;
                g.AddComponent<Rigidbody2D>().gravityScale = 0f;
                var col = g.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(0.4f, 0.2f);
                DotBulletVisualEffects.AttachFrostTrail(g);
                g.AddComponent<FrostBullet>();
                return g;
            }, pos);

        var b = go.GetComponent<FrostBullet>();
        if (b == null) b = go.AddComponent<FrostBullet>();
        b.SetupFrost(speed, impactDmg, frostDps, freezeDuration, slowPercent, dmgMult, canCrit, critChance, critMult);
        b.SetDirection(dir);
        return b;
    }
}

