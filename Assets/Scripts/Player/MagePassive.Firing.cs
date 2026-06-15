using UnityEngine;
using System.Collections.Generic;

public partial class MagePassive
{
    private const int MAX_ACTIVE_BULLETS = 200;
    private const int MAX_BARRAGE = 2;
    private const int MAX_BULLETS_PER_FRAME = 15;
    private const float MIN_COOLDOWN = 0.33f;

    // 帧预算耗尽时的轮转起始枪索引，保证各枪轮流获得开火机会（避免末尾的枪饿死）
    private int _fireStartIndex;

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

        int gunCount = _dotGuns.Count;
        if (gunCount == 0) return;

        int bulletsThisFrame = 0;

        for (int k = 0; k < gunCount; k++)
        {
            // 轮转起始索引：帧预算耗尽时让不同的枪在不同帧优先开火
            int i = (_fireStartIndex + k) % gunCount;
            var gun = _dotGuns[i];
            float effectiveCooldown = Mathf.Max(MIN_COOLDOWN, gun.cooldown * attackSpeedMult);

            gun.accumulator += dt;

            // 积压上限：最多缓冲约 1 发。用取模保留各枪的子周期相位，
            // 避免硬钳位把所有枪压到同一值导致同步开火、交错失效（Bug 1/2）。
            if (gun.accumulator > effectiveCooldown * 1.5f)
                gun.accumulator = effectiveCooldown + Mathf.Repeat(gun.accumulator, effectiveCooldown);

            if (hasTarget && gun.accumulator >= effectiveCooldown)
            {
                int barrage = Mathf.Min(1 + _bulletCountBonus, MAX_BARRAGE);

                // 帧预算以「整把枪」为粒度：本帧已经开过火、且再加一把会超预算 →
                // 推迟整把枪到下一帧（保留 accumulator，下帧补发）。
                // 绝不在弹幕内部截断——保证弹幕数始终恒定（Bug 4 不再误伤弹幕数量）。
                if (bulletsThisFrame > 0 && bulletsThisFrame + barrage > MAX_BULLETS_PER_FRAME)
                    continue;

                gun.accumulator -= effectiveCooldown;
                bulletsThisFrame += SpawnDotBullet(gun, fireDir, dmgMult);

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

        _fireStartIndex = (_fireStartIndex + 1) % gunCount;
    }

    /// <summary>
    /// 创建一发完整弹幕。弹幕数恒为 Min(1 + bulletCountBonus, MAX_BARRAGE)，绝不被帧预算截断。
    /// 仅当场上子弹逼近硬上限 MAX_ACTIVE_BULLETS（极端安全阀）时才可能少于该值。
    /// 返回实际创建的子弹数。
    /// </summary>
    private int SpawnDotBullet(DotGunState gun, Vector2 direction, float dmgMultiplier)
    {
        // 安全阀：场上子弹已达硬上限则整把跳过——在弹幕**之前**判断一次，
        // 绝不在弹幕内部逐发截断，否则会出现「弹幕+2 却只射 1/2 发」的不稳定（核心 bug）。
        // 允许至多 MAX_BARRAGE-1 的轻微溢出（≤204），无害。
        if (DotBulletBase.ActiveDotBullets.Count >= MAX_ACTIVE_BULLETS)
            return 0;

        float durMult = GetDotDurationMultiplier();
        float critChance = GetDotCritChance();
        // 元素专属弹幕加成：雷暴（雷电+1）、飓风（风+2）
        int elementExtra = 0;
        if (gun.effectType == StatusEffectType.Static && StormMulti) elementExtra += 1;
        if (gun.effectType == StatusEffectType.WindErosion && WindHurricane) elementExtra += 2;
        int bulletCount = Mathf.Min(1 + _bulletCountBonus + elementExtra, MAX_BARRAGE);
        float spreadAngle = 15f;
        int created = 0;

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
            created++;
        }

        return created;
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
