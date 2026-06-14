using UnityEngine;

/// <summary>
/// 简单子弹 — 蓝色子弹，命中直接扣血，无 DOT 效果。
/// 非 DOT 角色的默认子弹类型。
/// </summary>
public class SimpleBullet : ProjectileBase
{
    public static readonly System.Collections.Generic.List<MonoBehaviour> ActiveBullets = new();

    protected override void OnEnable()
    {
        base.OnEnable();
        ActiveBullets.Add(this);
    }

    protected override void OnHitEnemy(GameObject enemy)
    {
        var dmg = enemy.GetComponent<Damageable>();
        if (dmg == null || dmg.CurrentHp <= 0) return;

        float damage = _impactDamage * _damageMultiplier;
        if (_canCrit && Random.value < _critChance)
            damage *= _critMult;

        dmg.TakeDamage(damage);
        DamagePopup.Create(enemy.transform.position, damage, new Color(0.3f, 0.5f, 1f), false);
    }

    protected override void OnBulletDespawn()
    {
        ActiveBullets.Remove(this);
    }

    /// <summary>
    /// 工厂方法 — 创建蓝色子弹
    /// </summary>
    public static SimpleBullet Create(Vector2 pos, Vector2 dir, float speed,
        int impactDamage, float dmgMult, bool canCrit = false, float critChance = 0f, float critMult = 2f)
    {
        var go = new GameObject("SimpleBullet");
        go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite();
        sr.color = new Color(0.3f, 0.5f, 1f);
        sr.sortingOrder = 15;
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.2f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.25f;

        var bullet = go.AddComponent<SimpleBullet>();
        bullet.SetupBullet(speed, 4f, impactDamage, dmgMult, canCrit, critChance, critMult);
        bullet.SetDirection(dir);

        return bullet;
    }
}
