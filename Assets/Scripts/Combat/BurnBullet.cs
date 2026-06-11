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
        float spreadRadius = DotBulletConfig.GetDefault().BurnSpreadRadius;
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

        var ticker = textObj.AddComponent<BurnSpreadTextTicker>();
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
        var pool = ObjectPool.Instance;
        GameObject go = null;
        if (pool != null && pool.HasPool(PoolHelper.DOT_BURN_BULLET))
        {
            go = pool.Spawn(PoolHelper.DOT_BURN_BULLET, pos, Quaternion.identity);
        }
        else
        {
            PoolHelper.RegisterVirtualPrefab(PoolHelper.DOT_BURN_BULLET, BuildTemplate, 15);
            go = pool != null ? pool.Spawn(PoolHelper.DOT_BURN_BULLET, pos, Quaternion.identity) : null;
        }

        if (go == null)
        {
            go = new GameObject("BurnBullet");
            go.tag = "Untagged";
            PhysicsLayerSetup.SetAsBullet(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(1f, 0.4f, 0f); sr.sortingOrder = 15;
            go.transform.localScale = Vector3.one * 0.2f;
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.25f;
            DotBulletVisualEffects.AttachFlameEffect(go);
            go.AddComponent<BurnBullet>();
        }

        go.transform.position = pos;
        go.SetActive(true);
        var b = go.GetComponent<BurnBullet>();
        b.SetupBurn(speed, impactDmg, burnDps, burnDuration, dmgMult, canCrit, critChance, critMult);
        b.SetDirection(dir);
        return b;
    }
}

/// <summary>
/// 燃烧叠加效果 — 层数越高，tick间隔越短（最低0.2秒）
/// </summary>
public class BurnStackEffect : MonoBehaviour
{
    private int _stacks;
    public float _baseDps;
    public float _duration;
    public int StackCount => _stacks;
    public float _endTime;
    public bool _canCrit; public float _critChance, _critMult;
    private float _lastTick;
    private Damageable _damageable;
    private DotColorBlender _blender;
    private float _tickAccumulator;
    private int _lastRegisteredStacks = -1;

    public void AddStack(float baseDps, float duration, bool canCrit, float critChance, float critMult)
    {
        _stacks++;
        _baseDps = Mathf.Max(_baseDps, baseDps);
        _duration = duration;
        _endTime = Time.time + duration;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }

    private void OnEnable()
    {
        _damageable = GetComponent<Damageable>();
        _blender = GetComponent<DotColorBlender>();
        _lastTick = Time.time;
        _lastRegisteredStacks = -1;
        DotEffectRegistry.Register(this); // #24 注册到统一注册表
        DotBulletConfig.OnConfigChanged += RefreshFromConfig;
    }

    private void OnDisable()
    {
        DotBulletConfig.OnConfigChanged -= RefreshFromConfig;
    }

    private void RefreshFromConfig()
    {
        var cfg = DotBulletConfig.GetDefault();
        _duration = cfg.BurnDuration;
    }

    private void Update()
    {
        if (_damageable == null || _damageable.CurrentHp <= 0 || _stacks <= 0) { _stacks = 0; DotEffectRegistry.Unregister(this); UnregisterColor(); Destroy(this); return; }

        if (_blender != null && _stacks != _lastRegisteredStacks)
        {
            _lastRegisteredStacks = _stacks;
            float intensity = Mathf.Clamp01(_stacks / 10f);
            _blender.RegisterDot("burn", DotColorBlender.BURN_ORANGE, intensity, 10f);
        }

        float tickInterval = Mathf.Max(0.2f, 1.0f / _stacks);
        _tickAccumulator += Time.deltaTime;

        if (_tickAccumulator >= tickInterval)
        {
            _tickAccumulator -= tickInterval;
            float dmg = _baseDps * tickInterval;
            if (_canCrit && Random.value < _critChance) dmg *= _critMult;
            _damageable.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(dmg)), new Color(1f, 0.5f, 0f));
        }
    }

    private void OnDestroy() { DotEffectRegistry.Unregister(this); UnregisterColor(); } // #24 注销
    private void UnregisterColor() { if (_blender != null) _blender.UnregisterDot("burn"); }
}

/// <summary>
/// 燃烧扩散文字 — 向上飘动并淡出，1秒后自动销毁
/// </summary>
public class BurnSpreadTextTicker : MonoBehaviour
{
    public float Lifetime = 1.0f;
    private float _spawnTime;
    private TextMesh _textMesh;

    private void Awake()
    {
        _spawnTime = Time.time;
        _textMesh = GetComponent<TextMesh>();
        Destroy(gameObject, Lifetime + 1f);
    }

    private void OnEnable()
    {
        _spawnTime = Time.time;
    }

    private void Update()
    {
        float elapsed = Time.time - _spawnTime;
        if (elapsed >= Lifetime)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += Vector3.up * Time.deltaTime * 1.5f;

        if (_textMesh != null)
        {
            Color c = _textMesh.color;
            c.a = Mathf.Clamp01(1f - (elapsed / Lifetime));
            _textMesh.color = c;
        }
    }

    private void OnDisable()
    {
        // 不在 OnDisable 中 Destroy(gameObject) —— FullReset 会先 disable 所有 MB
        // 再由 CleanupLingeringCombatObjects 统一销毁，避免级联销毁导致异常
    }
}