using UnityEngine;

/// <summary>
/// MagePassive partial — 子弹发射逻辑（Update循环 + 射击方向 + 子弹生成）
/// </summary>
public partial class MagePassive
{
    private void Update()
    {
        _detonateSystem.UpdateChargeInput();

        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        Vector2 fireDir = GetFireDirection();
        if (fireDir.sqrMagnitude < 0.01f) return;

        float dmgMult = _weaponController != null ? _weaponController.DamageMultiplier : 1f;
        float attackSpeedMult = GetAttackSpeedMultiplier();

        for (int i = 0; i < _dotGuns.Count; i++)
        {
            var gun = _dotGuns[i];
            float effectiveCooldown = gun.cooldown * attackSpeedMult;
            if (Time.time - gun.lastFireTime >= effectiveCooldown)
            {
                gun.lastFireTime = Time.time;
                _dotGuns[i] = gun;
                SpawnDotBullet(gun, fireDir, dmgMult);

                // 光明子弹：用实际蓄力+扫射+延迟周期替代固定冷却，
                // 避免激光扫射结束后还要等待固定冷却才能重新蓄力
                if (gun.effectType == StatusEffectType.Light)
                {
                    var config = DotBulletConfig.GetDefault();
                    float chargeDur = Mathf.Max(config.LightMinChargeDuration,
                        config.LightChargeDuration - (gun.upgradeLevel - 1) * config.LightChargeReductionPerLevel);
                    float actualCycle = chargeDur + config.LightSweepDuration + config.LightPostFireDelay;
                    gun.lastFireTime = Time.time - effectiveCooldown + actualCycle;
                    _dotGuns[i] = gun;
                }
            }
        }
    }

    // ── 子弹发射 ──

    private Vector2 GetFireDirection()
    {
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            var cam = GameReferences.MainCamera;
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                Vector3 screenPos = mouse.position.ReadValue();
                screenPos.z = Mathf.Abs(cam.transform.position.z);
                Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);
                Vector2 dir = ((Vector2)worldPos - (Vector2)transform.position);
                if (dir.sqrMagnitude > 0.01f) return dir.normalized;
            }
        }

        var spawnMgr = GameReferences.SpawnManager;
        if (spawnMgr != null && spawnMgr.ActiveEnemies != null)
        {
            float minDist = float.MaxValue;
            Vector2 nearest = Vector2.zero;
            for (int i = 0; i < spawnMgr.ActiveEnemies.Count; i++)
            {
                var e = spawnMgr.ActiveEnemies[i];
                if (e == null) continue;
                var eb = e.GetComponent<EnemyBase>();
                if (eb == null || !eb.Alive) continue;
                float d = Vector2.Distance(transform.position, e.transform.position);
                if (d < minDist) { minDist = d; nearest = e.transform.position; }
            }
            if (minDist < float.MaxValue)
                return (nearest - (Vector2)transform.position).normalized;
        }

        return (Vector2)transform.right;
    }

    private void SpawnDotBullet(DotGunState gun, Vector2 direction, float dmgMultiplier)
    {
        float durMult = GetDotDurationMultiplier();
        float critChance = GetDotCritChance();
        float bulletSpeedMult = GetBulletSpeedMultiplier();
        int bulletCount = 1 + _bulletCountBonus;
        float spreadAngle = 15f;
        const int MAX_NORMAL = 5;
        int normalCount = Mathf.Min(bulletCount, MAX_NORMAL);
        int homingCount = bulletCount - normalCount;

        for (int b = 0; b < normalCount; b++)
        {
            Vector2 fireDir = direction;
            if (normalCount > 1)
            {
                float angle = (b - (normalCount - 1) / 2f) * spreadAngle;
                float rad = angle * Mathf.Deg2Rad;
                fireDir = new Vector2(direction.x * Mathf.Cos(rad) - direction.y * Mathf.Sin(rad), direction.x * Mathf.Sin(rad) + direction.y * Mathf.Cos(rad)).normalized;
            }
            GameObject bullet = DotBulletFactory.Create(gun.effectType, transform.position, fireDir, gun, bulletSpeedMult, durMult, dmgMultiplier, true, critChance, _dotCritMultiplier);
            ApplyBulletSizeBonus(bullet);
            ApplyUpgradeVisual(bullet, gun);
        }

        if (homingCount > 0)
        {
            float homingSpeed = 10f * bulletSpeedMult;
            int homingDmg = Mathf.Max(1, gun.impactDamage);
            float homingSpread = 30f;
            for (int h = 0; h < homingCount; h++)
            {
                Vector2 hDir = direction;
                if (homingCount > 1)
                {
                    float angle = (h - (homingCount - 1) / 2f) * homingSpread;
                    float rad = angle * Mathf.Deg2Rad;
                    hDir = new Vector2(direction.x * Mathf.Cos(rad) - direction.y * Mathf.Sin(rad), direction.x * Mathf.Sin(rad) + direction.y * Mathf.Cos(rad)).normalized;
                }
                var homing = HomingProjectile.CreateDefault(transform.position, hDir, homingDmg, homingSpeed, 5f, 6f);
                homing.SetDamageMultiplier(dmgMultiplier);
                homing.SetKnockback(_knockbackBonus > 0f ? 3f : 0f);
                var dotHoming = homing.gameObject.AddComponent<DotHomingBullet>();
                dotHoming.Init(gun, durMult, dmgMultiplier, true, critChance, _dotCritMultiplier);
                ApplyBulletSizeBonus(homing.gameObject);
            }
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
            var glow = new GameObject("Glow");
            glow.transform.SetParent(bullet.transform);
            glow.transform.localPosition = Vector3.zero;
            glow.transform.localScale = Vector3.one * 1.8f;
            var glowSr = glow.AddComponent<SpriteRenderer>();
            glowSr.sprite = sr?.sprite;
            glowSr.color = new Color(gun.color.r, gun.color.g, gun.color.b, 0.25f);
            glowSr.sortingOrder = 14;
        }
        var col = bullet.GetComponent<Collider2D>();
        if (col != null && col is BoxCollider2D box) box.size *= scaleBonus;
        else if (col != null && col is CircleCollider2D circle) circle.radius *= scaleBonus;
    }

    private void ApplyBulletSizeBonus(GameObject bullet)
    {
        if (bullet == null || _bulletSizeBonus <= 0f) return;
        bullet.transform.localScale *= (1f + _bulletSizeBonus);
        var col = bullet.GetComponent<Collider2D>();
        if (col != null && col is BoxCollider2D box) box.size *= (1f + _bulletSizeBonus);
        else if (col != null && col is CircleCollider2D circle) circle.radius *= (1f + _bulletSizeBonus);
    }

    /// <summary>
    /// Glow 对象池回收（供 GlowReturnHelper 调用）
    /// </summary>
    public static void ReturnGlowToPool(GameObject glow)
    {
        if (glow == null) return;
        glow.SetActive(false);
        glow.transform.SetParent(null);
    }
}
