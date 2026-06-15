using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 燃烧火场 — 缓慢移动的大型火场，处于火场内的敌人每 0.5 秒受到一次燃烧叠层。
/// 保留所有元素反应效果（燃烧扩散 × 风化、融化 × 霜冻）。
/// </summary>
public class BurnBullet : DotBulletBase
{
    private float _burnDps = 2f;
    private float _burnDuration = 3f;
    private float _tickInterval = 0.5f;
    private float _zoneRadius = 2.5f;
    private float _tickAccumulator;

    protected override StatusEffectType EffectType => StatusEffectType.Burn;
    protected override Color DefaultBulletColor => new Color(1f, 0.4f, 0f, 0.6f);
    protected override Color DefaultTrailStartColor => new Color(1f, 0.4f, 0f, 0.4f);

    private static readonly Collider2D[] _overlapBuffer = new Collider2D[32];

    public void SetupBurn(float speed, int impactDmg, float burnDps, float burnDuration,
        float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var config = DotEffectConfig.GetDefault();
        SetupBullet(speed, config.BurnLifetime, 0, dmgMult, canCrit, critChance, critMult);
        _burnDps = burnDps;
        _burnDuration = burnDuration;
        _tickInterval = config.BurnBaseTickInterval;
        _zoneRadius = config.BurnFireZoneRadius;

        // 设置火场大小
        transform.localScale = Vector3.one * config.BurnFireZoneScale;
        var col = GetComponent<Collider2D>();
        if (col is CircleCollider2D circle) circle.radius = _zoneRadius / config.BurnFireZoneScale;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _tickAccumulator = 0f;
    }

    protected override void Update()
    {
        // 基类处理超时销毁
        base.Update();

        // 渐变效果
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float elapsed = Time.time - _spawnTime;
            float alpha = Mathf.Lerp(0.6f, 0.1f, elapsed / _lifetime);
            Color c = DefaultBulletColor;
            c.a = alpha;
            sr.color = c;
        }

        // 每 tickInterval 对区域内敌人叠一次燃烧
        _tickAccumulator += Time.deltaTime;
        if (_tickAccumulator >= _tickInterval)
        {
            _tickAccumulator -= _tickInterval;
            ApplyZoneBurnStacks();
        }
    }

    // 火场不直接命中敌人（通过区域 tick 叠层）
    protected override void OnHitEnemy(GameObject enemy) { }

    /// <summary>
    /// 火场内敌人每 0.5 秒叠一次燃烧层，保留元素反应
    /// </summary>
    private void ApplyZoneBurnStacks()
    {
        int count = Physics2D.OverlapCircle(transform.position, _zoneRadius, new ContactFilter2D().NoFilter(), _overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            var hit = _overlapBuffer[i];
            if (!hit.CompareTag("Enemy")) continue;

            var enemyGo = hit.gameObject;
            var dmg = enemyGo.GetComponent<Damageable>();
            if (dmg == null || dmg.CurrentHp <= 0) continue;

            // 确保 StatusEffectManager 存在
            DotBulletHelper.EnsureStatusEffectManager(enemyGo);

            // 叠燃烧层
            var burn = enemyGo.GetComponent<BurnStackEffect>();
            if (burn == null) burn = enemyGo.AddComponent<BurnStackEffect>();
            burn.AddStack(_burnDps * _damageMultiplier, _burnDuration, _canCrit, _critChance, _critMult);

            // ── 元素反应：天照（燃烧 × 黑暗）──
            var darkMark = enemyGo.GetComponent<DarkMarkEffect>();
            if (darkMark != null && darkMark.StackCount > 0)
            {
                AmaterasuEffect.Apply(enemyGo);
            }

            // ── 元素反应：燃烧扩散（燃烧 × 风化）──
            var windEffect = enemyGo.GetComponent<WindErosionEffect>();
            if (windEffect != null && windEffect.WindStacks > 0)
            {
                TriggerBurnSpread(hit.transform.position, burn);
            }

            // ── 元素反应：融化（霜冻 × 燃烧）──
            var frostEffect = enemyGo.GetComponent<FrostEffect>();
            if (frostEffect != null && frostEffect.FrostStacks > 0)
            {
                TriggerMelt(enemyGo, frostEffect);
            }
        }
    }

    // ── 元素反应：燃烧扩散 ──
    private void TriggerBurnSpread(Vector2 center, BurnStackEffect sourceBurn)
    {
        var spawnMgr = GameReferences.SpawnManager;
        if (spawnMgr == null) return;

        var centerEnemy = sourceBurn.GetComponent<WindErosionEffect>();
        if (centerEnemy == null || !centerEnemy.ConsumeStack()) return;

        float spreadRadius = DotEffectConfig.GetDefault().BurnSpreadRadius;
        float radiusSqr = spreadRadius * spreadRadius;

        CombatManager.CreateExplosionEffect(center, 1f, new Color(1f, 0.5f, 0f, 0.5f), 0.4f);
        ShowSpreadText(center);

        IReadOnlyList<GameObject> enemies = spawnMgr.ActiveEnemies;
        if (enemies == null) return;

        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null || !e.activeInHierarchy) continue;
            if (e.transform.position == (Vector3)center) continue;

            Vector2 delta = (Vector2)e.transform.position - center;
            if (delta.sqrMagnitude > radiusSqr) continue;

            var enemyDmg = e.GetComponent<Damageable>();
            if (enemyDmg == null || enemyDmg.CurrentHp <= 0) continue;

            DotBulletHelper.EnsureStatusEffectManager(e);
            var targetBurn = e.GetComponent<BurnStackEffect>();
            if (targetBurn == null) targetBurn = e.AddComponent<BurnStackEffect>();
            targetBurn.AddStack(_burnDps * _damageMultiplier, _burnDuration, _canCrit, _critChance, _critMult);

            CombatManager.CreateExplosionEffect(e.transform.position, 0.2f, new Color(1f, 0.5f, 0f, 0.4f), 0.25f);
        }
    }

    // ── 元素反应：融化 ──
    private static void TriggerMelt(GameObject enemy, FrostEffect frostEff)
    {
        if (!frostEff.ConsumeStack()) return;

        var config = DotEffectConfig.GetDefault();
        var melt = enemy.GetComponent<MeltEffect>();
        if (melt == null) melt = enemy.AddComponent<MeltEffect>();
        melt.Activate(config.MeltDuration, config.MeltDamageMultiplier);

        CombatManager.CreateExplosionEffect(enemy.transform.position, 0.6f, new Color(1f, 0.3f, 0f, 0.5f), 0.3f);
        ShowMeltText(enemy.transform.position);
    }

    private static void ShowMeltText(Vector2 pos)
    {
        var textObj = new GameObject("MeltText");
        textObj.transform.position = pos + new Vector2(0, 0.6f);
        textObj.transform.localScale = Vector3.one * 0.3f;
        var textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = "融化！";
        textMesh.characterSize = 0.2f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 50;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.color = new Color(1f, 0.3f, 0f);
        var ticker = textObj.AddComponent<FloatingText>();
        ticker.Lifetime = 1.0f;
    }

    private static void ShowSpreadText(Vector2 pos)
    {
        var textObj = new GameObject("BurnSpreadText");
        textObj.transform.position = pos + new Vector2(0, 0.6f);
        textObj.transform.localScale = Vector3.one * 0.3f;
        var textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = "扩散！";
        textMesh.characterSize = 0.2f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 50;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.color = new Color(1f, 0.5f, 0f);
        var ticker = textObj.AddComponent<FloatingText>();
        ticker.Lifetime = 1.0f;
    }

    // 火场不触发碰撞逻辑（通过区域 tick 叠层，不通过碰撞）
    protected override void OnTriggerEnter2D(Collider2D other) { }

    protected override void OnBulletDespawn()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.DOT_BURN_BULLET);
    }

    private static GameObject BuildTemplate()
    {
        var go = new GameObject("BurnFireZone");
        go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite();
        sr.color = new Color(1f, 0.4f, 0f, 0.6f);
        sr.sortingOrder = 5;
        go.transform.localScale = Vector3.one * 2.0f;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 1.25f;
        go.AddComponent<BurnBullet>();
        return go;
    }

    public static BurnBullet Create(Vector2 pos, Vector2 dir, float speed, int impactDmg,
        float burnDps, float burnDuration, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var go = PoolHelper.SpawnOrFallback(PoolHelper.DOT_BURN_BULLET, BuildTemplate,
            () => {
                var g = new GameObject("BurnFireZone");
                g.tag = "Untagged";
                PhysicsLayerSetup.SetAsBullet(g);
                var sr = g.AddComponent<SpriteRenderer>();
                sr.sprite = DotSpriteCache.CircleSprite();
                sr.color = new Color(1f, 0.4f, 0f, 0.6f);
                sr.sortingOrder = 5;
                g.transform.localScale = Vector3.one * 2.0f;
                g.AddComponent<Rigidbody2D>().gravityScale = 0f;
                var col = g.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 1.25f;
                g.AddComponent<BurnBullet>();
                return g;
            }, pos);

        var b = go.GetComponent<BurnBullet>();
        if (b == null) b = go.AddComponent<BurnBullet>();
        b.SetupBurn(speed, impactDmg, burnDps, burnDuration, dmgMult, canCrit, critChance, critMult);
        b.SetDirection(dir);
        return b;
    }
}
