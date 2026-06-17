using UnityEngine;

public class WindBullet : DotBulletBase
{
    public bool IsTornado { get; set; } = false;

    protected override StatusEffectType EffectType => StatusEffectType.WindErosion;
    protected override Color DefaultBulletColor => new Color(0.7f, 0.85f, 1f);
    protected override Color DefaultTrailStartColor => new Color(0.7f, 0.85f, 1f, 0.6f);

    protected override void OnHitEnemy(GameObject enemy)
    {
        // 元素反应：球状闪电（风 × 雷电）
        var staticEffect = enemy.GetComponent<StaticStackEffect>();
        if (staticEffect != null && staticEffect.StackCount > 0)
        {
            // 消耗 1 层雷电
            staticEffect.ConsumeStack();

            // 消耗全部风层数
            var windEffect = enemy.GetComponent<WindErosionEffect>();
            if (windEffect != null)
            {
                while (windEffect.WindStacks > 0)
                    windEffect.ConsumeStack();
            }

            // 生成球状闪电，随机方向
            float angle = Random.Range(0f, 360f);
            float rad = angle * Mathf.Deg2Rad;
            Vector2 randomDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            BallLightning.Create(transform.position, randomDir);

            return;
        }

        // 正常风化叠层
        var wind = enemy.GetComponent<WindErosionEffect>();
        if (wind == null)
            wind = enemy.AddComponent<WindErosionEffect>();
        if (IsTornado) wind.KnockbackMultiplier *= 1.5f;
        wind.RegisterHit();
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
