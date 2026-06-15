using UnityEngine;
using System.Collections.Generic;

public partial class MagePassive
{
    private const int MAX_ACTIVE_BULLETS = 200;
    private const int MAX_BARRAGE = 5;

    private void Update()
    {
        _detonateSystem.UpdateChargeInput();

        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        float dt = Time.deltaTime;
        Vector2 fireDir = GetFireDirection();
        bool hasTarget = fireDir.sqrMagnitude >= 0.01f;
        float dmgMult = _weaponController != null ? _weaponController.DamageMultiplier : 1f;
        float attackSpeedMult = GetAttackSpeedMultiplier();

        for (int i = 0; i < _dotGuns.Count; i++)
        {
            var gun = _dotGuns[i];
            float effectiveCooldown = Mathf.Max(0.1f, gun.cooldown * attackSpeedMult);

            gun.accumulator += dt;

            if (gun.accumulator > effectiveCooldown * 1.5f)
                gun.accumulator = effectiveCooldown * 1.5f;

            if (hasTarget && gun.accumulator >= effectiveCooldown)
            {
                gun.accumulator -= effectiveCooldown;
                SpawnDotBullet(gun, fireDir, dmgMult);

                if (gun.effectType == StatusEffectType.Light)
                {
                    var config = DotEffectConfig.GetDefault();
                    float chargeDur = Mathf.Max(config.LightMinChargeDuration,
                        config.LightChargeDuration - (gun.upgradeLevel - 1) * config.LightChargeReductionPerLevel);
                    float actualCycle = chargeDur + config.LightSweepDuration + config.LightPostFireDelay;
                    gun.accumulator = -actualCycle + effectiveCooldown;
                }
            }
        }
    }

    private void SpawnDotBullet(DotGunState gun, Vector2 direction, float dmgMultiplier)
    {
        float durMult = GetDotDurationMultiplier();
        float critChance = GetDotCritChance();
        int bulletCount = Mathf.Min(1 + _bulletCountBonus, MAX_BARRAGE);
        float spreadAngle = 15f;

        for (int b = 0; b < bulletCount; b++)
        {
            // 场上子弹过多时停止创建
            if (DotBulletBase.ActiveDotBullets.Count >= MAX_ACTIVE_BULLETS)
                break;

            Vector2 fireDir = direction;
            if (bulletCount > 1)
            {
                float angle = (b - (bulletCount - 1) / 2f) * spreadAngle;
                float rad = angle * Mathf.Deg2Rad;
                fireDir = new Vector2(direction.x * Mathf.Cos(rad) - direction.y * Mathf.Sin(rad), direction.x * Mathf.Sin(rad) + direction.y * Mathf.Cos(rad)).normalized;
            }
            GameObject bullet = DotBulletFactory.Create(gun.effectType, transform.position, fireDir, gun, durMult, dmgMultiplier, true, critChance, _dotCritMultiplier);

            // 创建失败重试一次
            if (bullet == null)
                bullet = DotBulletFactory.Create(gun.effectType, transform.position, fireDir, gun, durMult, dmgMultiplier, true, critChance, _dotCritMultiplier);

            if (bullet == null)
            {
                DebugHelper.LogWarning($"[MagePassive] Bullet create failed for {gun.effectType}");
                continue;
            }

            ApplyBulletSizeBonus(bullet);
            ApplyUpgradeVisual(bullet, gun);
        }
    }

    private void ApplyUpgradeVisual(GameObject bullet, DotGunState gun)
    {
        if (bullet == null || gun.upgradeLevel <= 1) return;
        float scaleBonus = 1f + (gun.upgradeLevel - 1) * 0.1f;
        bullet.transform.localScale *= scaleBonus;
        var sr = bullet.GetComponent<SpriteRenderer>();
        if (sr != null) { float brightness = Mathf.Min(0.3f, (gun.upgradeLevel - 1) * 0.1f); sr.color = Color.Lerp(sr.color, Color.white, brightness); }
        if (gun.upgradeLevel >= 3)
        {
            var glow = GetOrCreateGlow();
            glow.transform.SetParent(bullet.transform);
            glow.transform.localPosition = Vector3.zero;
            glow.transform.localScale = Vector3.one * 1.8f;
            var glowSr = glow.GetComponent<SpriteRenderer>();
            if (glowSr != null)
            {
                glowSr.sprite = sr?.sprite;
                glowSr.color = new Color(gun.color.r, gun.color.g, gun.color.b, 0.25f);
                glowSr.sortingOrder = 14;
            }
            glow.SetActive(true);
        }
        var col = bullet.GetComponent<Collider2D>();
        if (col != null && col is BoxCollider2D box) box.size *= scaleBonus;
        else if (col != null && col is CircleCollider2D circle) circle.radius *= scaleBonus;
    }

    private static GameObject GetOrCreateGlow()
    {
        return GlowReturnHelper.GetOrCreate();
    }

    private void ApplyBulletSizeBonus(GameObject bullet)
    {
        if (bullet == null || _bulletSizeBonus <= 0f) return;
        bullet.transform.localScale *= (1f + _bulletSizeBonus);
        var col = bullet.GetComponent<Collider2D>();
        if (col != null && col is BoxCollider2D box) box.size *= (1f + _bulletSizeBonus);
        else if (col != null && col is CircleCollider2D circle) circle.radius *= (1f + _bulletSizeBonus);
    }

    public static void ReturnGlowToPool(GameObject glow)
    {
        GlowReturnHelper.ReturnToPool(glow);
    }
}
