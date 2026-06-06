using UnityEngine;

/// <summary>
/// Boss 技能库 — 从 BossEnemy 提取。
/// 职责：弹幕/召唤/冲锋/毒区/震波/幽灵闪现/分裂弹幕等技能实现。
/// 所有方法为静态，由 BossEnemy 的 Update 阶段调用。
/// </summary>
public static class BossAbilities
{
    // ── Sprite 缓存 ──
    private static Sprite _cachedCircleSprite;

    /// <summary>
    /// 创建/获取圆形 Sprite（8×8 纹理）
    /// </summary>
    public static Sprite GetCircleSprite()
    {
        if (_cachedCircleSprite != null) return _cachedCircleSprite;
        var tex = new Texture2D(8, 8);
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
            {
                float dx = x - 3.5f, dy = y - 3.5f;
                bool inside = (dx * dx + dy * dy) <= 16f;
                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        tex.Apply();
        _cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
        return _cachedCircleSprite;
    }

    // ── 弹幕攻击 ──

    /// <summary>
    /// 以 Boss 为中心发射环形弹幕
    /// </summary>
    public static void FireBarrage(Vector3 bossPos, int count, float bulletSpeed, int bulletDamage, Transform playerTransform)
    {
        if (playerTransform == null) return;
        DebugHelper.Log($"[BossAbilities] Firing barrage ({count} bullets)");

        float angleStep = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            var bulletGo = new GameObject($"BossBullet_{i}");
            bulletGo.transform.position = bossPos;
            bulletGo.tag = "Untagged";

            var sr = bulletGo.AddComponent<SpriteRenderer>();
            sr.sprite = GetCircleSprite();
            sr.color = new Color(1f, 0.2f, 0.8f);
            bulletGo.transform.localScale = Vector3.one * 0.3f;

            var rb = bulletGo.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearVelocity = dir * bulletSpeed;

            var col = bulletGo.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.15f;

            bulletGo.AddComponent<EnemyBullet>();

            Object.Destroy(bulletGo, 8f);
        }
    }

    // ── 召唤小怪 ──

    /// <summary>
    /// 在 Boss 周围召唤小怪
    /// </summary>
    public static void SummonMinions(Vector3 bossPos, int count, int bossLayer)
    {
        DebugHelper.Log($"[BossAbilities] Summoning {count} minions!");
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 2f;
            Vector2 spawnPos = (Vector2)bossPos + offset;

            var minion = new GameObject($"BossMinion_{i}");
            minion.transform.position = spawnPos;
            minion.tag = "Enemy";

            var sr = minion.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Square;
            sr.color = new Color(0.8f, 0f, 0.8f);

            var rb = minion.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = minion.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.6f, 0.6f);

            minion.AddComponent<BaseEntity>();
            var dmg = minion.AddComponent<Damageable>();
            dmg.SetMaxHp(30);
            dmg.Heal(30);
            minion.AddComponent<KillRewarder>();

            var enemyBase = minion.AddComponent<EnemyBase>();
            enemyBase.Setup(4f, 8, 1, 5);
            var player = GameReferences.Player;
            if (player != null) enemyBase.SetTarget(player.transform);
        }
    }

    // ── 冲锋 ──

    /// <summary>
    /// 开始冲锋（返回冲锋方向和结束时间）
    /// </summary>
    public static (Vector2 dir, float endTime) StartCharge(Vector3 bossPos, Transform playerTransform)
    {
        if (playerTransform == null) return (Vector2.zero, 0f);
        DebugHelper.Log("[BossAbilities] Charging!");
        Vector2 dir = (playerTransform.position - bossPos).normalized;
        return (dir, Time.time + 1f);
    }

    /// <summary>
    /// 更新冲锋移动
    /// </summary>
    public static void UpdateCharge(Rigidbody2D rb, Vector2 chargeDirection, float chargeSpeed)
    {
        if (rb != null) rb.linearVelocity = chargeDirection * chargeSpeed;
    }

    // ── 碰撞伤害 ──

    /// <summary>
    /// 冲锋碰撞伤害（由 OnCollisionStay2D 调用）
    /// </summary>
    public static void HandleCollision(Collision2D collision, int contactDamage, int chargeDamage, bool isCharging)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var dmg = collision.gameObject.GetComponent<Damageable>();
            if (dmg != null)
            {
                int damage = isCharging ? chargeDamage : contactDamage;
                dmg.TakeDamage(damage);
            }
        }
    }

    // ── 毒区 ──

    /// <summary>
    /// 在 Boss 周围生成毒区
    /// </summary>
    public static void SpawnPoisonZone(Vector3 bossPos, int poisonDamage, float poisonDuration, float poisonZoneRadius)
    {
        DebugHelper.Log("[BossAbilities] Spawning poison zone!");

        Vector2 pos = (Vector2)bossPos + Random.insideUnitCircle * 3f;
        var zone = new GameObject("BossPoisonZone");
        zone.transform.position = pos;
        zone.transform.localScale = Vector3.one * poisonZoneRadius;

        var sr = zone.AddComponent<SpriteRenderer>();
        sr.sprite = GetCircleSprite();
        sr.color = new Color(0.2f, 0.8f, 0f, 0.3f);
        sr.sortingOrder = -1;

        var col = zone.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        var poissonComponent = zone.AddComponent<BossPoisonZone>();
        poissonComponent.Setup(poisonDamage, poisonDuration, 0.5f);

        Object.Destroy(zone, poisonDuration);
    }

    // ── AoE 震波 ──

    /// <summary>
    /// 以 Boss 为中心释放 AoE 震波
    /// </summary>
    public static void FireShockwave(Vector3 bossPos, int shockwaveDamage, float shockwaveRadius)
    {
        DebugHelper.Log("[BossAbilities] AoE Shockwave!");

        var shockwave = new GameObject("BossShockwave");
        shockwave.transform.position = bossPos;
        shockwave.transform.localScale = Vector3.one * 0.5f;

        var sr = shockwave.AddComponent<SpriteRenderer>();
        sr.sprite = GetCircleSprite();
        sr.color = new Color(1f, 0.5f, 0f, 0.6f);
        sr.sortingOrder = 1;

        var col = shockwave.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        var shockwaveComp = shockwave.AddComponent<BossShockwave>();
        shockwaveComp.Setup(shockwaveDamage, shockwaveRadius, 0.5f);

        Object.Destroy(shockwave, 0.8f);
    }

    // ── 分裂弹幕（狂战型） ──

    /// <summary>
    /// 向玩家方向发射分裂弹幕（飞行 1.5 秒后分裂为 3 发）
    /// </summary>
    public static void FireSplitBarrage(Vector3 bossPos, Transform playerTransform, float bulletSpeed, int bulletDamage)
    {
        if (playerTransform == null) return;
        DebugHelper.Log("[BossAbilities] Berserker split barrage!");

        Vector2 baseDir = (playerTransform.position - bossPos).normalized;
        for (int i = -1; i <= 1; i++)
        {
            float angle = i * 20f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(
                baseDir.x * Mathf.Cos(angle) - baseDir.y * Mathf.Sin(angle),
                baseDir.x * Mathf.Sin(angle) + baseDir.y * Mathf.Cos(angle)
            ).normalized;

            var bulletGo = new GameObject("SplitBullet");
            bulletGo.transform.position = bossPos;
            bulletGo.tag = "Untagged";

            var sr = bulletGo.AddComponent<SpriteRenderer>();
            sr.sprite = GetCircleSprite();
            sr.color = new Color(1f, 0.5f, 0f);
            bulletGo.transform.localScale = Vector3.one * 0.4f;

            var rb = bulletGo.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearVelocity = dir * bulletSpeed * 1.2f;

            var col = bulletGo.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.2f;

            bulletGo.AddComponent<EnemyBullet>();

            var splitInfo = bulletGo.AddComponent<BossSplitBullet>();
            splitInfo.Init(bulletSpeed * 0.8f, bulletDamage);

            Object.Destroy(bulletGo, 8f);
        }
    }

    // ── 幽灵型：隐身闪现 ──

    /// <summary>
    /// 幽灵型 Boss 开始隐身闪现
    /// </summary>
    public static void StartBlink(BossEnemy boss, Transform playerTransform, float invisibleDuration)
    {
        // 视觉效果：完全透明
        var sr = boss.GetComponent<SpriteRenderer>();
        if (sr != null) { var c = sr.color; c.a = 0.1f; sr.color = c; }

        // 禁用碰撞
        var col = boss.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        DebugHelper.Log("[BossAbilities] Phantom: Invisible blink!");

        // 闪现到玩家附近
        if (playerTransform != null)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * 3f;
            boss.transform.position = playerTransform.position + (Vector3)offset;
        }
    }

    /// <summary>
    /// 幽灵型 Boss 隐身期间缓慢接近玩家
    /// </summary>
    public static void UpdateInvisibleMovement(BossEnemy boss, Transform playerTransform, float bossSpeed)
    {
        if (playerTransform != null)
        {
            Vector2 dir = (playerTransform.position - boss.transform.position).normalized;
            var rb = boss.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = dir * bossSpeed * 0.5f;
        }
    }

    /// <summary>
    /// 幽灵型 Boss 结束隐身，恢复视觉和碰撞，释放环形弹幕
    /// </summary>
    public static void EndBlink(BossEnemy boss)
    {
        var sr = boss.GetComponent<SpriteRenderer>();
        if (sr != null) { var c = sr.color; c.a = 0.6f; sr.color = c; }

        var col = boss.GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        DebugHelper.Log("[BossAbilities] Phantom: Reappeared!");
    }

    // ── 阶段 3 专属技能（Juggernaut） ──

    /// <summary>
    /// Juggernaut 阶段 3：召唤 2 个 TankEnemy 作为护盾
    /// </summary>
    public static void ExecuteJuggernautPhase3(Vector3 bossPos, int bossLayer)
    {
        DebugHelper.Log("[BossAbilities] Juggernaut Phase 3: Summoning 2 TankEnemy shields!");
        for (int i = 0; i < 2; i++)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * 2f;
            var minion = new GameObject($"JuggernautShield_{i}");
            minion.transform.position = (Vector2)bossPos + offset;
            minion.tag = "Enemy";
            minion.layer = bossLayer;
            minion.transform.localScale = Vector3.one * 0.8f;
            var sr = minion.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Hexagon;
            sr.color = new Color(0.5f, 0.5f, 0.5f);
            sr.sortingOrder = 7;
            var rb = minion.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; rb.freezeRotation = true;
            minion.AddComponent<BoxCollider2D>().isTrigger = true;
            minion.AddComponent<BaseEntity>();
            var dmg = minion.AddComponent<Damageable>();
            dmg.SetMaxHp(80);
            minion.AddComponent<KillRewarder>().SetRewards(10, 5);
            minion.AddComponent<TankEnemy>();
        }
    }

    /// <summary>
    /// Sorcerer 阶段 3：释放反魔法区域
    /// </summary>
    public static void ExecuteSorcererPhase3(Vector3 bossPos)
    {
        DebugHelper.Log("[BossAbilities] Sorcerer Phase 3: Anti-magic zone deployed!");
        var zone = new GameObject("AntiMagicZone");
        zone.transform.position = bossPos;
        zone.transform.localScale = Vector3.one * 6f;
        var zoneSr = zone.AddComponent<SpriteRenderer>();
        zoneSr.sprite = GetCircleSprite();
        zoneSr.color = new Color(0.6f, 0f, 0.8f, 0.15f);
        zoneSr.sortingOrder = -1;
        var zoneCol = zone.AddComponent<CircleCollider2D>();
        zoneCol.isTrigger = true;
        zoneCol.radius = 0.5f;
        var antiMagic = zone.AddComponent<BossPoisonZone>();
        antiMagic.Setup(0, 10f, 0.5f);
        Object.Destroy(zone, 10f);
    }

    // ── 阶段 5 专属技能 ──

    /// <summary>
    /// Juggernaut 阶段 5：狂暴模式（速度×2，冲锋无冷却）
    /// </summary>
    public static void ExecuteJuggernautPhase5(BossEnemy boss, float baseSpeed)
    {
        DebugHelper.Log("[BossAbilities] Juggernaut Phase 5: BERSERK! Speed x2, charge no cooldown!");
    }

    /// <summary>
    /// Sorcerer 阶段 5：同时释放环形弹幕 + 召唤 + 震波
    /// </summary>
    public static void ExecuteSorcererPhase5(Vector3 bossPos, int barrageCount, float bulletSpeed, int bulletDamage,
        Transform playerTransform, int summonCount, int shockwaveDamage, float shockwaveRadius, int bossLayer)
    {
        FireBarrage(bossPos, barrageCount, bulletSpeed, bulletDamage, playerTransform);
        SummonMinions(bossPos, summonCount, bossLayer);
        FireShockwave(bossPos, shockwaveDamage, shockwaveRadius);
        DebugHelper.Log("[BossAbilities] Sorcerer Phase 5: BARRAGE + SUMMON + SHOCKWAVE!");
    }

    /// <summary>
    /// Phantom 阶段 5：分裂为 2 个幽灵分身（血量各 50%）
    /// </summary>
    public static void ExecutePhantomPhase5(BossEnemy boss, Damageable bossDamageable, float bossSpeed, int barrageCount)
    {
        DebugHelper.Log("[BossAbilities] Phantom Phase 5: SPLIT into 2 phantoms!");
        if (bossDamageable != null)
        {
            int splitHp = bossDamageable.MaxHp / 2;
            var clone = BossFactory.CreateBoss(boss.transform.position + Vector3.right * 2f, splitHp);
            clone._bossHP = splitHp;
            clone._bossSpeed = bossSpeed * 1.2f;
            clone._bulletBarrageCount = barrageCount;
            var sr = clone.GetComponent<SpriteRenderer>();
            if (sr != null) { var c = sr.color; c.a = 0.5f; sr.color = c; }
            bossDamageable.SetMaxHp(splitHp);
            bossDamageable.Heal(splitHp);
        }
    }
}