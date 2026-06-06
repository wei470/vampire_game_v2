using UnityEngine;

/// <summary>
/// 武器投射物工厂，封装各种武器类型的投射物生成逻辑。
/// 从 WeaponController 中提取，减少主类代码量。
/// </summary>
public static class WeaponProjectileFactory
{
    #region 投射物生成方法

    /// <summary>
    /// 生成标准子弹
    /// </summary>
    public static void SpawnBullet(Vector2 position, Vector2 dir, int damage, WeaponData data)
    {
        Bullet.CreateDefault(
            position, dir,
            damage, data.projectileSpeed,
            data.projectileLifetime, data.pierce
        );
    }

    /// <summary>
    /// 生成闪电链
    /// </summary>
    public static void SpawnLightning(Vector2 position, Vector2 dir, int damage, WeaponData data)
    {
        LightningBolt.CreateDefault(
            position, dir,
            damage, data.chainCount,
            data.chainRadius, data.projectileLifetime
        );
    }

    /// <summary>
    /// 生成冲击波
    /// </summary>
    public static void SpawnShockwave(Vector2 position, int damage, WeaponData data)
    {
        ShockwaveProjectile.CreateDefault(
            position,
            damage, 8f, data.zoneRadius > 0 ? data.zoneRadius : 5f,
            data.projectileLifetime
        );
    }

    /// <summary>
    /// 生成追踪导弹
    /// </summary>
    public static void SpawnHomingMissile(Vector2 position, Vector2 dir, int damage, WeaponData data)
    {
        HomingProjectile.CreateDefault(
            position, dir,
            damage, data.projectileSpeed,
            data.homingTurnSpeed, data.projectileLifetime
        );
    }

    /// <summary>
    /// 生成地雷
    /// </summary>
    public static void SpawnMineTrap(Vector2 position, Vector2 fireDir, int damage, WeaponData data)
    {
        Vector2 pos = position + fireDir * 1.5f;
        MineTrap.CreateDefault(
            pos,
            damage, 2f, data.zoneRadius > 0 ? data.zoneRadius : 3f,
            data.projectileLifetime > 0 ? data.projectileLifetime : 10f
        );
    }

    /// <summary>
    /// 生成火焰区域
    /// </summary>
    public static void SpawnFireZone(Vector2 position, Vector2 dir, int damage, WeaponData data)
    {
        Vector2 targetPos = position + dir * 8f;
        var zone = FireZone.CreateDefault(
            position,
            damage,
            data.zoneLifetime > 0 ? data.zoneLifetime : 3f,
            data.zoneRadius > 0 ? data.zoneRadius : 1.5f,
            data.tickInterval > 0 ? data.tickInterval : 0.5f
        );
        zone.SetTargetPosition(targetPos);
    }

    /// <summary>
    /// 生成霜冻宝珠
    /// </summary>
    public static void SpawnFrostOrb(Vector2 position, Vector2 dir, int damage, WeaponData data)
    {
        Vector2 targetPos = position + dir * 6f;
        FrostOrb.CreateDefault(
            position, targetPos,
            damage, Mathf.RoundToInt(data.dotDamage > 0 ? data.dotDamage : 3),
            data.slowAmount > 0 ? data.slowAmount : 0.5f,
            data.slowDuration > 0 ? data.slowDuration : 1.5f,
            data.zoneLifetime > 0 ? data.zoneLifetime : 4f,
            data.frostRadius > 0 ? data.frostRadius : 2.5f
        );
    }

    /// <summary>
    /// 生成毒镖
    /// </summary>
    public static void SpawnVenomDart(Vector2 position, Vector2 dir, int damage, WeaponData data)
    {
        VenomDart.CreateDefault(
            position, dir,
            damage,
            data.dotDamage > 0 ? data.dotDamage : 3f,
            data.dotDuration > 0 ? data.dotDuration : 3f,
            data.projectileSpeed,
            data.projectileLifetime,
            data.pierce
        );
    }

    #endregion

    #region 默认投射物（无 WeaponData 时使用）

    /// <summary>
    /// 创建默认投射物 GameObject
    /// </summary>
    public static GameObject CreateDefaultProjectileGO(Vector2 position)
    {
        var go = new GameObject("Projectile");
        go.transform.position = position;
        go.tag = "Untagged";

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetCachedProjectileSprite();
        sr.color = new Color(0.3f, 0.8f, 1f);
        sr.sortingOrder = 15;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.5f, 0.3f);

        go.AddComponent<Projectile>();

        return go;
    }

    /// <summary>
    /// 创建椭圆形投射物 Sprite（16x8）
    /// </summary>
    private static Sprite _cachedProjectileSprite;
    public static Sprite GetCachedProjectileSprite()
    {
        if (_cachedProjectileSprite != null) return _cachedProjectileSprite;

        var tex = new Texture2D(16, 8);
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 8; y++)
            {
                float cx = (x - 7.5f) / 7.5f;
                float cy = (y - 3.5f) / 3.5f;
                float dist = cx * cx + cy * cy;
                tex.SetPixel(x, y, dist <= 1f ? Color.white : new Color(0, 0, 0, 0));
            }
        tex.Apply();
        _cachedProjectileSprite = Sprite.Create(tex, new Rect(0, 0, 16, 8), new Vector2(0.5f, 0.5f), 10f);
        return _cachedProjectileSprite;
    }

    #endregion

    /// <summary>
    /// 旋转向量
    /// </summary>
    public static Vector2 RotateVector2(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}