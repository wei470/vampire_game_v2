using UnityEngine;
using System.Collections.Generic;

public partial class MagePassive
{
    private const int MAX_BULLETS_PER_FRAME = 15;
    private int _bulletsThisFrame;

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

        _bulletsThisFrame = 0;

        for (int i = 0; i < _dotGuns.Count; i++)
        {
            if (_bulletsThisFrame >= MAX_BULLETS_PER_FRAME) break;

            var gun = _dotGuns[i];
            float effectiveCooldown = Mathf.Max(0.1f, gun.cooldown * attackSpeedMult);

            gun.accumulator += dt;

            while (hasTarget && gun.accumulator >= effectiveCooldown && _bulletsThisFrame < MAX_BULLETS_PER_FRAME)
            {
                gun.accumulator -= effectiveCooldown;
                SpawnDotBullet(gun, fireDir, dmgMult);
                _bulletsThisFrame += Mathf.Min(1 + _bulletCountBonus, 3);

                if (gun.effectType == StatusEffectType.Light)
                {
                    var config = DotEffectConfig.GetDefault();
                    float chargeDur = Mathf.Max(config.LightMinChargeDuration,
                        config.LightChargeDuration - (gun.upgradeLevel - 1) * config.LightChargeReductionPerLevel);
                    float actualCycle = chargeDur + config.LightSweepDuration + config.LightPostFireDelay;
                    gun.accumulator = -actualCycle + effectiveCooldown;
                    break;
                }
            }

            // 防止长时间不射击后累加器积压过大（最多保留 3 轮）
            if (gun.accumulator > effectiveCooldown * 3f)
                gun.accumulator = effectiveCooldown * 3f;
        }
    }

    private void SpawnDotBullet(DotGunState gun, Vector2 direction, float dmgMultiplier)
    {
        float durMult = GetDotDurationMultiplier();
        float critChance = GetDotCritChance();
        int bulletCount = Mathf.Min(1 + _bulletCountBonus, 3);
        float spreadAngle = 15f;

        for (int b = 0; b < bulletCount; b++)
        {
            Vector2 fireDir = direction;
            if (bulletCount > 1)
            {
                float angle = (b - (bulletCount - 1) / 2f) * spreadAngle;
                float rad = angle * Mathf.Deg2Rad;
                fireDir = new Vector2(direction.x * Mathf.Cos(rad) - direction.y * Mathf.Sin(rad), direction.x * Mathf.Sin(rad) + direction.y * Mathf.Cos(rad)).normalized;
            }
            GameObject bullet = DotBulletFactory.Create(gun.effectType, transform.position, fireDir, gun, durMult, dmgMultiplier, true, critChance, _dotCritMultiplier);
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
