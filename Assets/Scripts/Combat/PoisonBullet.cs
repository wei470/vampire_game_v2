using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 毒子弹 — 直线飞行，命中第一个敌人后范围爆炸，叠加中毒层数
/// Mage 默认攻击子弹。从 DotProjectile.cs 拆分而来。已迁移到 DotBulletBase 基类。
/// </summary>
public class PoisonBullet : DotBulletBase
{
    private float _poisonDps = 2f;
    private float _poisonDuration = 5f;
    private float _explosionRadius = 0.5f;
    private bool _exploded;

    protected override StatusEffectType EffectType => StatusEffectType.Poison;

    public void SetupPoison(float speed, float poisonDps, float poisonDuration, float explosionRadius,
        float dmgMult, bool canCrit, float critChance, float critMult)
    {
        SetupBullet(speed, 5f, 0, dmgMult, canCrit, critChance, critMult);
        _poisonDps = poisonDps;
        _poisonDuration = poisonDuration;
        _explosionRadius = explosionRadius;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _exploded = false;
    }

    protected override void Update()
    {
        if (!_exploded && Time.time - _spawnTime > _lifetime) DespawnSelf();
        else if (_exploded && Time.time - _spawnTime > _lifetime + 1f) DespawnSelf();
    }

    protected override void FixedUpdate()
    {
        if (!_exploded && _rb != null) _rb.linearVelocity = _direction * _speed;
    }

    /// <summary>
    /// 重写命中逻辑：毒子弹的穿透/爆炸行为与基类不同
    /// </summary>
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (_exploded) return;
        if (!other.CompareTag("Enemy")) return;
        DotBulletHelper.EnsureStatusEffectManager(other.gameObject);

        // 穿透检查：先对当前敌人施加中毒DOT，然后检查是否可以继续穿透
        if (_cachedPenetrate != null && _cachedPenetrate.TryPenetrate(other))
        {
            var dmg = other.GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp > 0)
            {
                var poison = other.GetComponent<PoisonStackEffect>();
                if (poison == null) poison = other.gameObject.AddComponent<PoisonStackEffect>();
                poison.AddStack(_poisonDps * _damageMultiplier, _poisonDuration, _canCrit, _critChance, _critMult);
            }
            return;
        }

        // ── 元素反应：毒爆（中毒 × 黑暗）──
        bool hasDarkMark = other.GetComponent<DarkMarkEffect>() != null;
        LeavePuddle(transform.position, hasDarkMark);
    }

    protected override void OnHitEnemy(GameObject enemy) { }

    private void LeavePuddle(Vector2 center, bool darkMarkBonus = false)
    {
        _exploded = true;
        if (_rb != null) _rb.linearVelocity = Vector2.zero;

        // 从 Config 实时读取毒液池参数
        var cfg = DotBulletConfig.GetDefault();
        float explosionRadius = darkMarkBonus ? _explosionRadius * 2f : cfg.PoisonExplosionRadius * 2f;
        float puddleRadius = darkMarkBonus ? cfg.PoisonPuddleRadius : cfg.PoisonPuddleRadius * 0.5f;
        float puddleDuration = cfg.PoisonPuddleDuration;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, explosionRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            var dmg = hit.GetComponent<Damageable>();
            if (dmg == null || dmg.CurrentHp <= 0) continue;
            var poison = hit.GetComponent<PoisonStackEffect>();
            if (poison == null) poison = hit.gameObject.AddComponent<PoisonStackEffect>();
            poison.AddStack(_poisonDps * _damageMultiplier, _poisonDuration, _canCrit, _critChance, _critMult);
        }
        PoisonPuddle.Create(center, puddleRadius, puddleDuration, _poisonDps * _damageMultiplier, _canCrit, _critChance, _critMult);

        if (darkMarkBonus)
        {
            CombatManager.CreateExplosionEffect(center, 1f,
                new Color(0.4f, 0.1f, 0.6f, 0.6f), 0.5f);
            ShowPoisonBurstText(center);
            DebugHelper.Log($"[PoisonBurst] 毒爆触发！爆炸范围={explosionRadius:F1}，毒圈范围={puddleRadius:F1}");
        }

        DespawnSelf();
    }

    private static void ShowPoisonBurstText(Vector2 pos)
    {
        var textObj = new GameObject("PoisonBurstText");
        textObj.transform.position = pos + new Vector2(0, 0.6f);
        textObj.transform.localScale = Vector3.one * 0.3f;

        var textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = "毒爆！";
        textMesh.characterSize = 0.2f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 50;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.color = new Color(0.4f, 0.1f, 0.6f);

        var ticker = textObj.AddComponent<PoisonBurstTextTicker>();
        ticker.Lifetime = 1.0f;
    }

    protected override void OnBulletDespawn()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.DOT_POISON_BULLET);
    }

    private static GameObject BuildTemplate()
    {
        var go = new GameObject("PoisonBullet");
        go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(0.1f, 0.9f, 0.2f); sr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * 0.25f;
        var rb = go.AddComponent<Rigidbody2D>(); rb.gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.25f;
        DotBulletVisualEffects.AttachTrail(go, new Color(0.1f, 0.9f, 0.2f, 0.6f), 0.4f, 0.03f);
        go.AddComponent<PoisonBullet>();
        return go;
    }

    public static PoisonBullet Create(Vector2 pos, Vector2 dir, float speed,
        float poisonDps, float poisonDuration, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var pool = ObjectPool.Instance;
        GameObject go = null;
        if (pool != null && pool.HasPool(PoolHelper.DOT_POISON_BULLET))
        {
            go = pool.Spawn(PoolHelper.DOT_POISON_BULLET, pos, Quaternion.identity);
        }
        else
        {
            PoolHelper.RegisterVirtualPrefab(PoolHelper.DOT_POISON_BULLET, BuildTemplate, 15);
            go = pool != null ? pool.Spawn(PoolHelper.DOT_POISON_BULLET, pos, Quaternion.identity) : null;
        }

        if (go == null)
        {
            go = new GameObject("PoisonBullet");
            go.tag = "Untagged";
            PhysicsLayerSetup.SetAsBullet(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(0.1f, 0.9f, 0.2f); sr.sortingOrder = 15;
            go.transform.localScale = Vector3.one * 0.25f;
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.25f;
            DotBulletVisualEffects.AttachTrail(go, new Color(0.1f, 0.9f, 0.2f, 0.6f), 0.4f, 0.03f);
            go.AddComponent<PoisonBullet>();
        }

        go.transform.position = pos;
        go.SetActive(true);
        var b = go.GetComponent<PoisonBullet>();
        b.SetupPoison(speed, poisonDps, poisonDuration, 0.5f, dmgMult, canCrit, critChance, critMult);
        b.SetDirection(dir);
        return b;
    }
}

/// <summary>
/// 中毒药瓶 — 投掷后爆炸生成毒液池
/// </summary>
public class PoisonPotion : MonoBehaviour
{
    private float _speed = 10f;
    private float _lifetime = 3f;
    private float _puddleDuration = 5f;
    private float _puddleRadius = 1.5f;
    private float _baseDps = 3f;
    private float _damageMultiplier = 1f;
    private bool _canCrit; private float _critChance, _critMult;
    private Vector2 _targetPos;
    private float _spawnTime;
    private bool _exploded;

    public void Setup(float speed, float puddleDuration, float puddleRadius, float baseDps,
        float dmgMult, bool canCrit, float critChance, float critMult)
    {
        _speed = speed; _puddleDuration = puddleDuration; _puddleRadius = puddleRadius;
        _baseDps = baseDps; _damageMultiplier = dmgMult;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }
    public void SetTarget(Vector2 target) { _targetPos = target; }
    private void Start() { _spawnTime = Time.time; }
    private void Update() { if (Time.time - _spawnTime > _lifetime) Destroy(gameObject); }

    private void FixedUpdate()
    {
        if (_exploded) return;
        Vector2 dir = _targetPos - (Vector2)transform.position;
        if (dir.magnitude < 0.3f) { Explode(); return; }
        GetComponent<Rigidbody2D>().linearVelocity = dir.normalized * _speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_exploded) return;
        if (other.CompareTag("Enemy") || other.CompareTag("Untagged")) Explode();
    }

    private void Explode()
    {
        _exploded = true;
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        PoisonPuddle.Create(transform.position, _puddleRadius, _puddleDuration, _baseDps * _damageMultiplier, _canCrit, _critChance, _critMult);
        CombatManager.CreateExplosionEffect(transform.position, _puddleRadius, new Color(0.1f, 0.9f, 0.2f, 0.5f), 0.3f);
        Destroy(gameObject);
    }

    public static PoisonPotion Create(Vector2 pos, Vector2 target, float speed,
        float puddleDuration, float puddleRadius, float baseDps, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var go = new GameObject("PoisonPotion");
        go.transform.position = pos; go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.Get(); sr.color = new Color(0.1f, 0.8f, 0.1f); sr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * 0.8f;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.3f;
        DotBulletVisualEffects.AttachSpinEffect(go, 360f);
        var p = go.AddComponent<PoisonPotion>();
        p.Setup(speed, puddleDuration, puddleRadius, baseDps, dmgMult, canCrit, critChance, critMult);
        p.SetTarget(target);
        return p;
    }
}

/// <summary>
/// 毒液池 — 敌人站在上面会叠加中毒层数
/// </summary>
public class PoisonPuddle : MonoBehaviour
{
    private float _radius;
    private float _duration;
    private float _baseDps;
    private bool _canCrit; private float _critChance, _critMult;
    private float _spawnTime;
    private float _lastTick;
    private CircleCollider2D _cachedCol;
    private SpriteRenderer _cachedSr;

    public void Setup(float radius, float duration, float baseDps, bool canCrit, float critChance, float critMult)
    {
        _radius = radius; _duration = duration; _baseDps = baseDps;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
        ApplyRadius();
    }

    private void ApplyRadius()
    {
        transform.localScale = Vector3.one * _radius;
        if (_cachedCol == null) _cachedCol = GetComponent<CircleCollider2D>();
        if (_cachedCol != null) { _cachedCol.isTrigger = true; _cachedCol.radius = _radius; }
    }

    private void Awake()
    {
        _cachedCol = GetComponent<CircleCollider2D>();
        _cachedSr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        _spawnTime = Time.time; _lastTick = Time.time - 0.5f;
        ApplyRadius();
        if (_cachedSr != null) _cachedSr.color = new Color(0.1f, 0.7f, 0.1f, 0.4f);
        DotBulletConfig.OnConfigChanged += RefreshFromConfig;
    }

    private void OnDisable()
    {
        DotBulletConfig.OnConfigChanged -= RefreshFromConfig;
    }

    private void RefreshFromConfig()
    {
        _duration = DotBulletConfig.GetDefault().PoisonPuddleDuration;
    }

    private void Update()
    {
        if (Time.time - _spawnTime > _duration) { DespawnSelf(); return; }
        if (Time.time - _lastTick < 0.5f) return;
        _lastTick = Time.time;
        ApplyPoisonToNearby();
    }

    private void ApplyPoisonToNearby()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _radius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            var dmg = hit.GetComponent<Damageable>();
            if (dmg == null || dmg.CurrentHp <= 0) continue;
            var poison = hit.GetComponent<PoisonStackEffect>();
            if (poison == null) poison = hit.gameObject.AddComponent<PoisonStackEffect>();
            poison.AddStack(_baseDps, _duration - (Time.time - _spawnTime), _canCrit, _critChance, _critMult);
        }
    }

    private void DespawnSelf()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.DOT_POISON_PUDDLE);
    }

    private static GameObject BuildPuddleTemplate()
    {
        var go = new GameObject("PoisonPuddle");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(0.1f, 0.7f, 0.1f, 0.4f); sr.sortingOrder = 1;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        go.AddComponent<PoisonPuddle>();
        return go;
    }

    public static PoisonPuddle Create(Vector2 pos, float radius, float duration, float baseDps,
        bool canCrit, float critChance, float critMult)
    {
        var pool = ObjectPool.Instance;
        GameObject go = null;
        if (pool != null && pool.HasPool(PoolHelper.DOT_POISON_PUDDLE))
        {
            go = pool.Spawn(PoolHelper.DOT_POISON_PUDDLE, pos, Quaternion.identity);
        }
        else
        {
            PoolHelper.RegisterVirtualPrefab(PoolHelper.DOT_POISON_PUDDLE, BuildPuddleTemplate, 8);
            go = pool != null ? pool.Spawn(PoolHelper.DOT_POISON_PUDDLE, pos, Quaternion.identity) : null;
        }

        if (go == null)
        {
            go = new GameObject("PoisonPuddle");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(0.1f, 0.7f, 0.1f, 0.4f); sr.sortingOrder = 1;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            go.AddComponent<PoisonPuddle>();
        }

        go.transform.position = pos;
        go.SetActive(true);
        var p = go.GetComponent<PoisonPuddle>();
        p.Setup(radius, duration, baseDps, canCrit, critChance, critMult);
        return p;
    }
}

/// <summary>
/// 中毒叠加效果 — 每tick掉2滴血，固定1秒间隔，每层+1伤害
/// </summary>
public class PoisonStackEffect : MonoBehaviour
{
    private int _stacks;
    public bool _canCrit; public float _critChance, _critMult;
    private float _tickAccumulator;
    private Damageable _damageable;
    private DotColorBlender _blender;
    private const float BASE_TICK_INTERVAL = 1f;
    private const float TICK_DECAY = 0.9f;
    private const float MIN_TICK_INTERVAL = 0.2f;
    private const int DAMAGE_PER_TICK = 2;
    private const int MAX_STACKS = 20;
    private int _lastRegisteredStacks = -1;

    public int StackCount => _stacks;

    public void AddStack(float dps, float remainingTime, bool canCrit, float critChance, float critMult)
    {
        if (_stacks >= MAX_STACKS) return;
        _stacks++;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }

    private void OnEnable()
    {
        _damageable = GetComponent<Damageable>();
        _blender = GetComponent<DotColorBlender>();
        _lastRegisteredStacks = -1;
        _tickAccumulator = 0f;
        DotEffectRegistry.Register(this); // #24 注册到统一注册表
    }

    private void Update()
    {
        if (_stacks <= 0) { Cleanup(); return; }
        if (_damageable != null && _damageable.CurrentHp <= 0) { Cleanup(); return; }

        if (_blender != null && _stacks != _lastRegisteredStacks)
        {
            _lastRegisteredStacks = _stacks;
            float intensity = Mathf.Clamp01(_stacks / 10f);
            _blender.RegisterDot("poison", DotColorBlender.POISON_GREEN, intensity, 6f);
        }

        float tickInterval = BASE_TICK_INTERVAL;
        _tickAccumulator += Time.deltaTime;
        if (_tickAccumulator < tickInterval) return;
        _tickAccumulator -= tickInterval;

        int dmg = DAMAGE_PER_TICK + (_stacks - 1);
        if (_canCrit && Random.value < _critChance) dmg = Mathf.RoundToInt(dmg * _critMult);
        _damageable.TakeDamage(dmg, new Color(0.1f, 0.8f, 0.1f));
    }

    private void Cleanup() { UnregisterColor(); _stacks = 0; Destroy(this); }
    private void OnDestroy() { DotEffectRegistry.Unregister(this); UnregisterColor(); }
    private void UnregisterColor() { if (_blender != null) _blender.UnregisterDot("poison"); }
}

/// <summary>
/// 毒爆文字 — 向上飘动并淡出，1秒后自动销毁
/// </summary>
public class PoisonBurstTextTicker : MonoBehaviour
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

    private void OnEnable() { _spawnTime = Time.time; }

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