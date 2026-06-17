using UnityEngine;

/// <summary>
/// 冰刃散弹 — 速度2倍，命中叠1层霜冻。
/// 由 MagePassive.Firing.cs 每5发霜冻子弹后生成5发。
/// </summary>
public class IceBladeBullet : ProjectileBase
{
    protected override void OnHitEnemy(GameObject enemy)
    {
        var frost = enemy.GetComponent<FrostEffect>();
        if (frost == null) frost = enemy.AddComponent<FrostEffect>();
        frost.ApplyFreeze(0f, 0f, 0f, false, 0f, 0f);
    }

    public void SetupIceBlade(float speed, float lifetime)
    {
        SetupBullet(speed, lifetime, 0, 1f, false, 0f, 0f);
    }

    protected override void OnBulletDespawn()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.ICE_BLADE_BULLET);
    }

    private static GameObject BuildTemplate()
    {
        var go = DotBulletTemplate.BuildBox("IceBladeBullet",
            DotSpriteCache.Get(), DotPalette.FrostBlue, 12, 0.5f,
            DotBulletTemplate.BoxColliderParams.Default);
        go.AddComponent<IceBladeBullet>();
        go.transform.localScale = Vector3.one * 0.3f;
        return go;
    }

    public static IceBladeBullet Create(Vector2 pos, Vector2 dir, float speed, float lifetime)
    {
        var go = PoolHelper.SpawnOrFallback(PoolHelper.ICE_BLADE_BULLET, BuildTemplate,
            BuildTemplate, pos);
        var b = go.GetComponent<IceBladeBullet>();
        if (b == null) b = go.AddComponent<IceBladeBullet>();
        b.SetupIceBlade(speed, lifetime);
        b.SetDirection(dir);
        return b;
    }
}
