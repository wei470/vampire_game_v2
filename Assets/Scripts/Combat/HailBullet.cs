using UnityEngine;

/// <summary>
/// 冰雹 — 从天而降，命中敌人施加霜冻层数。
/// 由 SnowyDaySystem 每秒生成一颗。
/// </summary>
public class HailBullet : ProjectileBase
{
    private int _frostStacks = 2;

    protected override void OnHitEnemy(GameObject enemy)
    {
        var frost = enemy.GetComponent<FrostEffect>();
        if (frost == null) frost = enemy.AddComponent<FrostEffect>();
        for (int i = 0; i < _frostStacks; i++)
            frost.ApplyFreeze(0f, 0f, 0f, false, 0f, 0f);
    }

    public void SetupHail(float speed, float lifetime, int stacks)
    {
        SetupBullet(speed, lifetime, 0, 1f, false, 0f, 0f);
        _frostStacks = stacks;
    }

    protected override void OnBulletDespawn()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.HAIL_BULLET);
    }

    private static GameObject BuildTemplate()
    {
        var go = DotBulletTemplate.BuildCircle("HailBullet",
            DotSpriteCache.CircleSprite(), new Color(0.7f, 0.85f, 1f), 10, 0.4f,
            new DotBulletTemplate.CircleColliderParams { radius = 1.2f });
        go.AddComponent<HailBullet>();
        go.transform.localScale = Vector3.one * 0.8f;
        return go;
    }

    public static HailBullet Create(Vector2 pos, Vector2 dir, float speed, float lifetime, int stacks)
    {
        var go = PoolHelper.SpawnOrFallback(PoolHelper.HAIL_BULLET, BuildTemplate,
            BuildTemplate, pos);
        var h = go.GetComponent<HailBullet>();
        if (h == null) h = go.AddComponent<HailBullet>();
        h.SetupHail(speed, lifetime, stacks);
        h.SetDirection(dir);
        return h;
    }
}
