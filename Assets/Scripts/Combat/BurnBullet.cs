using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 燃烧子弹 — 慢速橙色子弹，命中叠加燃烧层数
/// 从 DotProjectile.cs 拆分而来
/// </summary>
public class BurnBullet : MonoBehaviour
{
    private float _speed = 12f;
    private float _lifetime = 4f;
    private int _impactDamage = 2;
    private float _burnDps = 2f;
    private float _burnDuration = 3f;
    private float _damageMultiplier = 1f;
    private bool _canCrit; private float _critChance, _critMult;
    private Vector2 _direction;
    private float _spawnTime;
    private Rigidbody2D _rb;
    private PenetrateHandler _cachedPenetrate;

    public void Setup(float speed, int impactDmg, float burnDps, float burnDuration,
        float dmgMult, bool canCrit, float critChance, float critMult)
    {
        _speed = speed; _impactDamage = impactDmg; _burnDps = burnDps;
        _burnDuration = burnDuration; _damageMultiplier = dmgMult;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }
    public void SetDirection(Vector2 dir) { _direction = dir.normalized; RotateToDirection(); }
    private void RotateToDirection() { float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg; transform.rotation = Quaternion.Euler(0, 0, angle); }

    private void Awake() { _rb = GetComponent<Rigidbody2D>(); _cachedPenetrate = GetComponent<PenetrateHandler>(); }
    private void OnEnable()
    {
        _spawnTime = Time.time;
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        if (_rb != null) _rb.linearVelocity = Vector2.zero;
    }
    private void Update() { if (Time.time - _spawnTime > _lifetime) DespawnSelf(); }
    private void FixedUpdate() { if (_rb != null) _rb.linearVelocity = _direction * _speed; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        var dmg = other.GetComponent<Damageable>();
        if (dmg != null && dmg.CurrentHp > 0)
        {
            DotBulletHelper.EnsureStatusEffectManager(other.gameObject);
            var burn = other.GetComponent<BurnStackEffect>();
            if (burn == null) burn = other.gameObject.AddComponent<BurnStackEffect>();
            burn.AddStack(_burnDps * _damageMultiplier, _burnDuration, _canCrit, _critChance, _critMult);

            // ── 元素反应：燃烧扩散（燃烧 × 风化）──
            // 当燃烧子弹打中带有风化层数的敌人时，消耗一层风化，
            // 敌人的燃烧效果会扩散至一个圆，圆内所有敌人都施加一层燃烧
            var windEffect = other.GetComponent<WindErosionEffect>();
            if (windEffect != null && windEffect.WindStacks > 0)
            {
                TriggerBurnSpread(other.transform.position, burn);
            }
        }
        if (_cachedPenetrate != null && _cachedPenetrate.TryPenetrate(other)) return;
        var ricochet = GetComponent<RicochetHandler>();
        if (ricochet != null && ricochet.TryRicochet(transform.position, other)) return;
        DespawnSelf();
    }

    /// <summary>
    /// 元素反应：燃烧扩散 — 消耗一层风化，将燃烧扩散到周围所有敌人
    /// </summary>
    private void TriggerBurnSpread(Vector2 center, BurnStackEffect sourceBurn)
    {
        // 查找中心敌人并消耗风化层数
        var spawnMgr = GameReferences.SpawnManager;
        if (spawnMgr == null) return;

        // 消耗一层风化
        var centerEnemy = sourceBurn.GetComponent<WindErosionEffect>();
        if (centerEnemy == null || !centerEnemy.ConsumeStack()) return;

        // 燃烧扩散范围（缩小为原来的30%：5 * 0.3 = 1.5）
        const float SPREAD_RADIUS = 1.5f;
        float radiusSqr = SPREAD_RADIUS * SPREAD_RADIUS;

        // 视觉特效：燃烧扩散爆发
        CombatManager.CreateExplosionEffect(center, SPREAD_RADIUS,
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

            // 范围检测
            Vector2 delta = (Vector2)e.transform.position - center;
            if (delta.sqrMagnitude > radiusSqr) continue;

            // 检查是否存活
            var enemyDmg = e.GetComponent<Damageable>();
            if (enemyDmg == null || enemyDmg.CurrentHp <= 0) continue;

            // 施加燃烧
            DotBulletHelper.EnsureStatusEffectManager(e);
            var targetBurn = e.GetComponent<BurnStackEffect>();
            if (targetBurn == null) targetBurn = e.AddComponent<BurnStackEffect>();
            targetBurn.AddStack(_burnDps * _damageMultiplier, _burnDuration, _canCrit, _critChance, _critMult);

            // 扩散视觉特效（小）
            CombatManager.CreateExplosionEffect(e.transform.position, 0.4f,
                new Color(1f, 0.5f, 0f, 0.4f), 0.25f);
        }

        DebugHelper.Log($"[BurnSpread] 燃烧扩散触发！范围={SPREAD_RADIUS}，消耗1层风化");
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
        textMesh.color = new Color(1f, 0.5f, 0f); // 橙色，与燃烧同色

        // 向上飘动 + 自动销毁
        var ticker = textObj.AddComponent<BurnSpreadTextTicker>();
        ticker.Lifetime = 1.0f;
    }

    private void DespawnSelf()
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
        b.Setup(speed, impactDmg, burnDps, burnDuration, dmgMult, canCrit, critChance, critMult);
        b.SetDirection(dir);
        b._cachedPenetrate = go.GetComponent<PenetrateHandler>();
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

    public void AddStack(float baseDps, float duration, bool canCrit, float critChance, float critMult)
    {
        _stacks++;
        _baseDps = Mathf.Max(_baseDps, baseDps);
        _duration = duration;
        _endTime = Time.time + duration;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }

    private void Start()
    {
        _damageable = GetComponent<Damageable>();
        _blender = GetComponent<DotColorBlender>();
        _lastTick = Time.time;
    }

    private void Update()
    {
        if (_damageable == null || _damageable.CurrentHp <= 0 || _stacks <= 0) { _stacks = 0; UnregisterColor(); Destroy(this); return; }

        // 通过 DotColorBlender 更新燃烧颜色贡献
        if (_blender == null) _blender = GetComponent<DotColorBlender>();
        if (_blender != null)
        {
            float intensity = Mathf.Clamp01(_stacks / 10f);
            _blender.RegisterDot("burn", DotColorBlender.BURN_ORANGE, intensity, 10f);
        }

        float tickInterval = Mathf.Max(0.2f, 1.0f / _stacks);
        _tickAccumulator += Time.deltaTime;

        if (_tickAccumulator >= tickInterval)
        {
            _tickAccumulator -= tickInterval;
            if (_damageable != null && _damageable.CurrentHp > 0)
            {
                float dmg = _baseDps * tickInterval;
                if (_canCrit && Random.value < _critChance) dmg *= _critMult;
                _damageable.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(dmg)), new Color(1f, 0.5f, 0f));
            }
        }
    }

    private void OnDestroy() { UnregisterColor(); }
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
        // 安全兜底：无论如何都在 Lifetime+1 秒后销毁
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

        // 向上飘动
        transform.position += Vector3.up * Time.deltaTime * 1.5f;

        // 淡出
        if (_textMesh != null)
        {
            Color c = _textMesh.color;
            c.a = Mathf.Clamp01(1f - (elapsed / Lifetime));
            _textMesh.color = c;
        }
    }

    private void OnDisable()
    {
        // 场景重置时确保销毁
        if (gameObject != null) Destroy(gameObject);
    }
}
