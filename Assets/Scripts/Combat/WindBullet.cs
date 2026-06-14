using UnityEngine;

public class WindBullet : DotBulletBase
{
    protected override StatusEffectType EffectType => StatusEffectType.WindErosion;

    protected override void OnHitEnemy(GameObject enemy)
    {
        var windEffect = enemy.GetComponent<WindErosionEffect>();
        if (windEffect == null)
            windEffect = enemy.AddComponent<WindErosionEffect>();
        windEffect.RegisterHit();
    }

    protected override void OnBulletDespawn()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.DOT_WIND_BULLET);
    }

    private static GameObject BuildTemplate()
    {
        var go = new GameObject("WindBullet");
        go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.Get();
        sr.color = new Color(0.7f, 0.85f, 1f);
        sr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * 0.35f;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(0.4f, 0.2f);
        DotBulletVisualEffects.AttachWindTrail(go);
        go.AddComponent<WindBullet>();
        return go;
    }

    public static WindBullet Create(Vector2 pos, Vector2 dir, float speed, int impactDmg,
        float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var go = PoolHelper.SpawnOrFallback(PoolHelper.DOT_WIND_BULLET, BuildTemplate,
            () => BuildTemplate(), pos, 20);

        var b = go.GetComponent<WindBullet>();
        if (b == null) b = go.AddComponent<WindBullet>();
        b.SetupBullet(speed, 1.5f, impactDmg, dmgMult, canCrit, critChance, critMult);
        b.SetDirection(dir);
        b._cachedPenetrate = go.GetComponent<PenetrateHandler>();
        return b;
    }
}
