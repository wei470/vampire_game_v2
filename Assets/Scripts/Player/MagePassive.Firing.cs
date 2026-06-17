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

    private float _burnRocketAccumulator;
    private int _frostBulletCount;

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

        // 燃烧枪 → 以 cooldown × 0.25 的频率发射 BurnRocketBullet
        DotGunState burnGun = null;
        for (int i = 0; i < gunCount; i++)
        {
            if (_dotGuns[i].effectType == StatusEffectType.Burn) { burnGun = _dotGuns[i]; break; }
        }
        if (burnGun != null && BurnStorm)
        {
            float burnCooldown = Mathf.Max(MIN_COOLDOWN, burnGun.cooldown * attackSpeedMult) * 0.25f;
            _burnRocketAccumulator += dt;
            if (_burnRocketAccumulator >= burnCooldown && hasTarget)
            {
                _burnRocketAccumulator -= burnCooldown;
                SpawnBurnRocket(fireDir, burnGun);
            }
        }

        // 雷暴多发延迟 (0.1s delay)
        if (_stormMultiDelay > 0f)
        {
            _stormMultiDelay -= dt;
            if (_stormMultiDelay <= 0f && _stormMultiGun != null && hasTarget)
            {
                for (int i = 0; i < _stormMultiDirs.Count; i++)
                    SpawnDotBullet(_stormMultiGun, _stormMultiDirs[i], _stormMultiDmgMult);
                _stormMultiGun = null;
                _stormMultiDirs.Clear();
            }
        }

        // 风暴延迟 (two stages, 0.2s each)
        if (_stormWindDelay > 0f)
        {
            _stormWindDelay -= dt;
            if (_stormWindDelay <= 0f && _stormWindGun != null && hasTarget)
            {
                for (int i = 0; i < _stormWindDirs.Count; i++)
                    SpawnDotBullet(_stormWindGun, _stormWindDirs[i], _stormWindDmgMult);
                _stormWindDelay = 0.2f;
                _stormWindGun2 = _stormWindGun;
                _stormWindDmgMult2 = _stormWindDmgMult;
                _stormWindDirs2 = new List<Vector2>(_stormWindDirs);
                _stormWindGun = null;
                _stormWindDirs.Clear();
            }
        }
        if (_stormWindDelay2 > 0f)
        {
            _stormWindDelay2 -= dt;
            if (_stormWindDelay2 <= 0f && _stormWindGun2 != null && hasTarget)
            {
                for (int i = 0; i < _stormWindDirs2.Count; i++)
                    SpawnDotBullet(_stormWindGun2, _stormWindDirs2[i], _stormWindDmgMult2);
                _stormWindGun2 = null;
                _stormWindDirs2.Clear();
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
        if (DotBulletBase.ActiveDotBullets.Count >= MAX_ACTIVE_BULLETS)
            return 0;

        float durMult = GetDotDurationMultiplier();
        float critChance = GetDotCritChance();
        int bulletCount = Mathf.Min(1 + _bulletCountBonus, MAX_BARRAGE);
        float spreadAngle = 15f;

        // 飓风：偏移角度从 15° 改为 value1°
        if (gun.effectType == StatusEffectType.WindErosion && WindHurricane)
        {
            var cfg = GetUpgradeConfig();
            if (cfg != null)
            {
                var entry = cfg.GetUpgradeEntry("wind_hurricane");
                if (entry.HasValue) spreadAngle = entry.Value.value1;
            }
        }

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

            // 雷暴多发延迟触发 (0.1s delay for wind-like double shot for lightning)
            if (gun.effectType == StatusEffectType.Static && StormMulti && _stormMultiDelay <= 0f)
            {
                _stormMultiGun = gun;
                _stormMultiDirs.Add(fireDir);
                _stormMultiDmgMult = dmgMultiplier;
                _stormMultiDelay = 0.1f;
            }

            // 闪电弹计数 → StormChain (every 5th → static explosion pending)
            if (gun.effectType == StatusEffectType.Static && StormChain)
            {
                _lightningBulletCount++;
                if (_lightningBulletCount >= 5)
                {
                    _lightningBulletCount = 0;
                    _lightningExplosionPending = true;
                }
            }

            // 风暴延迟触发 (33% chance → save directions for delayed burst)
            if (gun.effectType == StatusEffectType.WindErosion && Random.value < 0.33f && _stormWindDelay <= 0f)
            {
                _stormWindGun = gun;
                _stormWindDirs.Add(fireDir);
                _stormWindDmgMult = dmgMultiplier;
                _stormWindDelay = 0.2f;
            }

            // 冰刃计数 (every 5th frost → burst of 5 ice blade bullets)
            if (gun.effectType == StatusEffectType.Frostbite)
            {
                _frostBulletCount++;
                if (_frostBulletCount >= 5)
                {
                    _frostBulletCount = 0;
                    SpawnIceBladeBurst(fireDir);
                }
            }
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

    private void SpawnIceBladeBurst(Vector2 direction)
    {
        var config = DotEffectConfig.GetDefault();
        float speed = config.FrostSpeed * 2f;
        float lifetime = config.FrostFreezeDuration;
        int count = 5;
        float spreadAngle = 20f;
        for (int i = 0; i < count; i++)
        {
            float angle = (i - (count - 1) / 2f) * spreadAngle;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(
                direction.x * Mathf.Cos(rad) - direction.y * Mathf.Sin(rad),
                direction.x * Mathf.Sin(rad) + direction.y * Mathf.Cos(rad)).normalized;
            var bullet = IceBladeBullet.Create(transform.position, dir, speed, lifetime);
            if (bullet != null)
            {
                ApplyBulletSizeBonus(bullet.gameObject);
                if (_coldBullet)
                {
                    var bounce = bullet.gameObject.AddComponent<WallBounceHandler>();
                    bounce.Setup(2, 5f);
                }
            }
        }
    }

    private void SpawnBurnRocket(Vector2 direction, DotGunState burnGun)
    {
        var config = DotEffectConfig.GetDefault();
        float lifetime = config.BurnLifetime * 0.5f;
        float speed = 18f;
        float burnDps = burnGun.dotDps;
        float burnDuration = burnGun.dotDuration;
        var rocket = BurnRocketBullet.Create(transform.position, direction, speed, lifetime, burnDps, burnDuration);
        if (rocket != null) ApplyBulletSizeBonus(rocket.gameObject);
    }
}
