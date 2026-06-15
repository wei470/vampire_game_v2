using UnityEngine;
using System.Collections.Generic;

public static class DotBulletFactory
{
    public delegate GameObject BulletSpawner(
        Vector2 pos, Vector2 dir, DotGunState gun,
        float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult);

    private static readonly Dictionary<StatusEffectType, BulletSpawner> _spawners
        = new Dictionary<StatusEffectType, BulletSpawner>();

    private static DotEffectConfig _config;
    private static DotEffectConfig Config
    {
        get { if (_config == null) _config = DotEffectConfig.GetDefault(); return _config; }
    }

    static DotBulletFactory()
    {
        Register(StatusEffectType.Poison, SpawnPoison);
        Register(StatusEffectType.Burn, SpawnBurn);
        Register(StatusEffectType.Frostbite, SpawnFrost);
        Register(StatusEffectType.Static, SpawnStatic);
        Register(StatusEffectType.Dark, SpawnDark);
        Register(StatusEffectType.Light, SpawnLight);
        Register(StatusEffectType.WindErosion, SpawnWind);
        DotEffectConfig.OnConfigChanged += OnConfigChanged;
    }

    public static void Register(StatusEffectType type, BulletSpawner spawner)
    {
        _spawners[type] = spawner;
    }

    public static GameObject Create(StatusEffectType type, Vector2 pos, Vector2 dir,
        DotGunState gun, float durMult, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        if (_spawners.TryGetValue(type, out var spawner))
            return spawner(pos, dir, gun, durMult, dmgMult, canCrit, critChance, critMult);

        DebugHelper.LogWarning($"[DotBulletFactory] 未注册的子弹类型: {type}");
        return null;
    }

    private static void OnConfigChanged()
    {
        _config = null;
    }

    private static MagePassive GetMage()
    {
        var mage = GameReferences.DotCharacterPassive as MagePassive;
        return mage;
    }

    private static GameObject SpawnPoison(Vector2 pos, Vector2 dir, DotGunState gun,
        float durMult, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var mage = GetMage();
        float dpsBonus = mage != null ? mage.PoisonDpsBonus : 0f;
        float durBonus = mage != null ? mage.PoisonDurationBonus : 0f;
        float critBonus = mage != null ? mage.PoisonCritBonus : 0f;
        float dps = gun.dotDps + dpsBonus;
        if (mage != null)
        {
            if (mage.PoisonLethal) dps *= 2f;        // 猛毒：毒素 DPS ×2
            if (mage.PoisonSepsis) dps *= 1.3f;      // 脓毒：全 DOT 伤害 +30%
        }
        float dur = gun.dotDuration * durMult + durBonus;

        var go = PoisonBullet.Create(pos, dir, Config.PoisonSpeed,
            dps, dur, dmgMult,
            canCrit || critBonus > 0, critChance + critBonus, critMult)?.gameObject;
        AttachRicochetIfAvailable(go);
        return go;
    }

    private static GameObject SpawnBurn(Vector2 pos, Vector2 dir, DotGunState gun,
        float durMult, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var mage = GetMage();
        float dps = gun.dotDps + (mage != null ? mage.BurnDurationBonus : 0f);
        if (mage != null && mage.PoisonSepsis) dps *= 1.3f;   // 脓毒：全 DOT 伤害 +30%
        float dur = gun.dotDuration * durMult + (mage != null ? mage.BurnDurationBonus : 0f);
        float critBonus = mage != null ? mage.BurnCritBonus : 0f;
        float speed = Config.BurnSpeed;

        var go = BurnBullet.Create(pos, dir, speed, 0,
            dps, dur, dmgMult,
            canCrit || critBonus > 0, critChance + critBonus, critMult)?.gameObject;

        // 火场范围加成
        if (go != null && mage != null && mage.BurnRadiusBonus > 0)
        {
            go.transform.localScale *= (1f + mage.BurnRadiusBonus);
            var col = go.GetComponent<CircleCollider2D>();
            if (col != null) col.radius *= (1f + mage.BurnRadiusBonus);
        }

        AttachRicochetIfAvailable(go);
        return go;
    }

    private static GameObject SpawnFrost(Vector2 pos, Vector2 dir, DotGunState gun,
        float durMult, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var mage = GetMage();
        float slowPct = Config.FrostBaseSlowPct + (mage != null ? mage.FrostSlowBonus : 0f);
        float critBonus = mage != null ? mage.FrostCritBonus : 0f;
        float rangeBonus = mage != null ? mage.FrostRangeBonus : 0f;

        // 绝对零度（frost_absolute）：默认霜冻只减速不造成伤害，拿到强化后每发 DPS 5（每层叠加）
        float frostDps = (mage != null && mage.FrostAbsolute) ? 5f : 0f;
        if (frostDps > 0f && mage.PoisonSepsis) frostDps *= 1.3f;   // 脓毒：全 DOT 伤害 +30%

        var go = FrostBullet.Create(pos, dir, Config.FrostSpeed, gun.impactDamage,
            frostDps, Config.FrostFreezeDuration, slowPct, dmgMult,
            canCrit || critBonus > 0, critChance + critBonus, critMult)?.gameObject;

        // 霜冻范围加成
        if (go != null && rangeBonus > 0)
        {
            go.transform.localScale *= (1f + rangeBonus);
            var col = go.GetComponent<Collider2D>();
            if (col is CircleCollider2D c) c.radius *= (1f + rangeBonus);
        }

        // 暴风雪（frost_blizzard）：霜冻范围 ×2
        if (go != null && mage != null && mage.FrostBlizzard)
        {
            go.transform.localScale *= 2f;
            var col = go.GetComponent<Collider2D>();
            if (col is CircleCollider2D c) c.radius *= 2f;
        }

        AttachRicochetIfAvailable(go);
        return go;
    }

    private static GameObject SpawnStatic(Vector2 pos, Vector2 dir, DotGunState gun,
        float durMult, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var mage = GetMage();
        int damage = gun.impactDamage;

        var go = LightningBullet.Create(pos, dir, Config.LightningSpeed, damage,
            dmgMult)?.gameObject;

        if (go != null && mage != null)
        {
            var lb = go.GetComponent<LightningBullet>();
            if (lb != null)
            {
                lb.ExtraChainTargets = mage.StaticChainBonus;
                lb.ExtraChainRadius = mage.StaticRangeBonus * 8f;
            }
        }

        AttachRicochetIfAvailable(go);
        return go;
    }
        }

        AttachRicochetIfAvailable(go);
        return go;
    }

    private static GameObject SpawnDark(Vector2 pos, Vector2 dir, DotGunState gun,
        float durMult, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        float radius = Config.DarkBaseRadius + (gun.upgradeLevel - 1) * Config.DarkRadiusPerLevel;
        float efficiency = Config.DarkBaseEfficiency + (gun.upgradeLevel - 1) * Config.DarkEfficiencyPerLevel;
        var go = DarkBullet.Create(pos, dir, Config.DarkSpeed, radius, efficiency)?.gameObject;
        AttachRicochetIfAvailable(go);
        return go;
    }

    private static GameObject SpawnLight(Vector2 pos, Vector2 dir, DotGunState gun,
        float durMult, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        float chargeDuration = Mathf.Max(Config.LightMinChargeDuration,
            Config.LightChargeDuration - (gun.upgradeLevel - 1) * Config.LightChargeReductionPerLevel);
        int laserDamage = Config.LightLaserDamage;
        float sweepAngle = Config.LightSweepAngle;
        float sweepDuration = Config.LightSweepDuration;
        float laserLength = Config.LightLaserLength;
        float laserWidth = Config.LightLaserWidth;
        float markDuration = Config.LightMarkDuration;
        int markMaxStacks = Config.LightMarkMaxStacks;

        var controller = LightBulletController.Create(pos);
        controller.Setup(chargeDuration, laserDamage, sweepAngle, sweepDuration,
            laserLength, laserWidth, markDuration, markMaxStacks, Config.LightTextureScale);
        controller.BeginCharge();
        return controller.gameObject;
    }

    private static GameObject SpawnWind(Vector2 pos, Vector2 dir, DotGunState gun,
        float durMult, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var mage = GetMage();
        float critBonus = mage != null ? mage.WindCritBonus : 0f;
        float damageBonus = mage != null ? mage.WindDamageBonus : 0f;
        float speed = Config.WindSpeed * (1f + (mage != null ? mage.WindSpeedBonus : 0f));   // 风速强化

        float randomAngle = Random.Range(-25f, 25f);
        float rad = randomAngle * Mathf.Deg2Rad;
        Vector2 spreadDir = new Vector2(
            dir.x * Mathf.Cos(rad) - dir.y * Mathf.Sin(rad),
            dir.x * Mathf.Sin(rad) + dir.y * Mathf.Cos(rad)
        ).normalized;
        var go = WindBullet.Create(pos, spreadDir, speed, gun.impactDamage + Mathf.RoundToInt(damageBonus),
            dmgMult, canCrit || critBonus > 0, critChance + critBonus, critMult)?.gameObject;

        // 风弹范围加成
        if (go != null && mage != null && mage.WindLord)
        {
            go.transform.localScale *= 2f;
            var col = go.GetComponent<Collider2D>();
            if (col is CircleCollider2D c) c.radius *= 2f;
        }

        AttachRicochetIfAvailable(go);
        return go;
    }

    public static T CreateBullet<T>(string poolKey, System.Func<GameObject> factory,
        Vector2 pos, Vector2 dir, System.Action<T> setup, int warmupCount = 15) where T : MonoBehaviour
    {
        var go = PoolHelper.SpawnOrFallback(poolKey, factory, () => {
            var g = new GameObject(typeof(T).Name);
            g.tag = "Untagged";
            PhysicsLayerSetup.SetAsBullet(g);
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = DotSpriteCache.CircleSprite();
            sr.sortingOrder = 15;
            g.transform.localScale = Vector3.one * 0.2f;
            g.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var col = g.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.25f;
            g.AddComponent<T>();
            return g;
        }, pos, warmupCount);

        var bullet = go.GetComponent<T>();
        if (bullet == null) bullet = go.AddComponent<T>();
        setup(bullet);
        return bullet;
    }

    private static void AttachRicochetIfAvailable(GameObject bullet)
    {
        if (bullet == null) return;
        var passive = GameReferences.CharacterPassive;
        if (passive == null) return;

        int pierce = passive.GetPenetrateCount();
        if (pierce > 0)
        {
            var ph = bullet.GetComponent<PenetrateHandler>();
            if (ph == null) ph = bullet.AddComponent<PenetrateHandler>();
            ph.Setup(pierce);
        }
    }
}
