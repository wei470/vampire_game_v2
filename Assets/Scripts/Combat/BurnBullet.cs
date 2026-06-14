using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 燃烧子弹 — 慢速橙色子弹，命中叠加燃烧层数
/// 从 DotProjectile.cs 拆分而来。已迁移到 DotBulletBase 基类。
/// </summary>
public class BurnBullet : DotBulletBase
{
    private float _burnDps = 2f;
    private float _burnDuration = 3f;

    protected override StatusEffectType EffectType => StatusEffectType.Burn;
    protected override Color DefaultBulletColor => new Color(1f, 0.4f, 0f);

    /// <summary>
    /// 燃烧子弹专属参数设置
    /// </summary>
    public void SetupBurn(float speed, int impactDmg, float burnDps, float burnDuration,
        float dmgMult, bool canCrit, float critChance, float critMult)
    {
        SetupBullet(speed, 4f, impactDmg, dmgMult, canCrit, critChance, critMult);
        _burnDps = burnDps;
        _burnDuration = burnDuration;
    }

    protected override void OnHitEnemy(GameObject enemy)
    {
        var burn = enemy.GetComponent<BurnStackEffect>();
        if (burn == null) burn = enemy.AddComponent<BurnStackEffect>();
        burn.AddStack(_burnDps * _damageMultiplier, _burnDuration, _canCrit, _critChance, _critMult);

        // ── 元素反应：燃烧扩散（燃烧 × 风化）──
        var windEffect = enemy.GetComponent<WindErosionEffect>();
        if (windEffect != null && windEffect.WindStacks > 0)
        {
            TriggerBurnSpread(enemy.transform.position, burn);
        }

        // ── 元素反应：融化（霜冻 × 燃烧）──
        var frostEffect = enemy.GetComponent<FrostEffect>();
        if (frostEffect != null && frostEffect.FrostStacks > 0)
        {
            TriggerMelt(enemy, frostEffect);
        }
    }

    /// <summary>
    /// 元素反应：燃烧扩散 — 消耗一层风化，将燃烧扩散到周围所有敌人
    /// </summary>
    private void TriggerBurnSpread(Vector2 center, BurnStackEffect sourceBurn)
    {
        var spawnMgr = GameReferences.SpawnManager;
        if (spawnMgr == null) return;

        // 消耗一层风化
        var centerEnemy = sourceBurn.GetComponent<WindErosionEffect>();
        if (centerEnemy == null || !centerEnemy.ConsumeStack()) return;

        // 从配置读取燃烧扩散范围
        float spreadRadius = DotEffectConfig.GetDefault().BurnSpreadRadius;
        float radiusSqr = spreadRadius * spreadRadius;

        // 视觉特效：燃烧扩散爆发
        CombatManager.CreateExplosionEffect(center, 1f,
            new Color(1f, 0.5f, 0f, 0.5f), 0.4f);

        // 显示"扩散！"文字
        ShowSpreadText(center);

        // 遍历所有敌人，对范围内的施加燃烧
        IReadOnlyList<GameObject> enemies = spawnMgr.ActiveEnemies;
        if (enemies == null) return;

        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null || !e.activeInHierarchy) continue;
            if (e.transform.position == (Vector3)center) continue; // 跳过源敌人

            Vector2 delta = (Vector2)e.transform.position - center;
            if (delta.sqrMagnitude > radiusSqr) continue;

            var enemyDmg = e.GetComponent<Damageable>();
            if (enemyDmg == null || enemyDmg.CurrentHp <= 0) continue;

            DotBulletHelper.EnsureStatusEffectManager(e);
            var targetBurn = e.GetComponent<BurnStackEffect>();
            if (targetBurn == null) targetBurn = e.AddComponent<BurnStackEffect>();
            targetBurn.AddStack(_burnDps * _damageMultiplier, _burnDuration, _canCrit, _critChance, _critMult);

            // 扩散视觉特效（小，缩小2倍）
            CombatManager.CreateExplosionEffect(e.transform.position, 0.2f,
                new Color(1f, 0.5f, 0f, 0.4f), 0.25f);
        }

        DebugHelper.Log($"[BurnSpread] 燃烧扩散触发！范围={spreadRadius}，消耗1层风化");
    }

    /// <summary>
    /// 元素反应：融化 — 消耗一层霜冻，使敌人在持续时间内所有 DOT 伤害翻倍
    /// </summary>
    private static void TriggerMelt(GameObject enemy, FrostEffect frostEff)
    {
        if (!frostEff.ConsumeStack()) return;

        var config = DotEffectConfig.GetDefault();
        var melt = enemy.GetComponent<MeltEffect>();
        if (melt == null) melt = enemy.AddComponent<MeltEffect>();
        melt.Activate(config.MeltDuration, config.MeltDamageMultiplier);

        CombatManager.CreateExplosionEffect(enemy.transform.position, 0.6f,
            new Color(1f, 0.3f, 0f, 0.5f), 0.3f);
        ShowMeltText(enemy.transform.position);
        DebugHelper.Log($"[Melt] 融化反应触发！DOT伤害翻倍 {config.MeltDuration}秒");
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

    /// <summary>
    /// 在扩散圆心显示"扩散！"文字，1秒后自动销毁
    /// </summary>
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

    protected override void OnBulletDespawn()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.DOT_BURN_BULLET);
    }

    private static GameObject BuildTemplate()
    {
        var go = new GameObject("BurnBullet");
        go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(1f, 0.4f, 0f); sr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * 0.2f;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.25f;
        DotBulletVisualEffects.AttachFlameEffect(go);
        go.AddComponent<BurnBullet>();
        return go;
    }

    public static BurnBullet Create(Vector2 pos, Vector2 dir, float speed, int impactDmg,
        float burnDps, float burnDuration, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var go = PoolHelper.SpawnOrFallback(PoolHelper.DOT_BURN_BULLET, BuildTemplate,
            () => {
                var g = new GameObject("BurnBullet");
                g.tag = "Untagged";
                PhysicsLayerSetup.SetAsBullet(g);
                var sr = g.AddComponent<SpriteRenderer>();
                sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(1f, 0.4f, 0f); sr.sortingOrder = 15;
                g.transform.localScale = Vector3.one * 0.2f;
                g.AddComponent<Rigidbody2D>().gravityScale = 0f;
                var col = g.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.25f;
                DotBulletVisualEffects.AttachFlameEffect(g);
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

