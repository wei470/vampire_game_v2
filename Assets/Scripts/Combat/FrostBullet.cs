using UnityEngine;

/// <summary>
/// 霜冻子弹 — 快速子弹，冰冻敌人 + 永久减速 + 霜伤
/// 从 DotProjectile.cs 拆分而来，已迁移到 DotBulletBase 基类
/// </summary>
public class FrostBullet : DotBulletBase
{
    private float _frostDps = 2f;
    private float _freezeDuration = 1f;
    private float _slowPercent = 0.3f;

    protected override StatusEffectType EffectType => StatusEffectType.Frostbite;

    /// <summary>
    /// 霜冻子弹专属参数设置
    /// </summary>
    public void SetupFrost(float speed, int impactDmg, float frostDps, float freezeDuration,
        float slowPercent, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        SetupBullet(speed, 2f, impactDmg, dmgMult, canCrit, critChance, critMult);
        _frostDps = frostDps;
        _freezeDuration = freezeDuration;
        _slowPercent = slowPercent;
    }

    protected override void OnHitEnemy(GameObject enemy)
    {
        var frost = enemy.GetComponent<FrostEffect>();
        if (frost == null) frost = enemy.AddComponent<FrostEffect>();
        frost.ApplyFreeze(_freezeDuration, _slowPercent, 0f, false, 0f, 0f);

        // ── 元素反应：霜电（霜冻 × 雷电）──
        var staticEffect = enemy.GetComponent<StaticStackEffect>();
        if (staticEffect != null && staticEffect.StackCount > 0)
        {
            TryTriggerFrostLightning(enemy.transform.position, staticEffect);
        }
    }

    /// <summary>
    /// 元素反应：霜电 — 消耗一层静电，在敌人周围生成冰场
    /// </summary>
    private static void TryTriggerFrostLightning(Vector2 pos, StaticStackEffect staticEff)
    {
        if (!staticEff.ConsumeStack()) return;
        FrostLightningField.Create(pos, 1f, 2f);
        DebugHelper.Log($"[FrostLightning] 霜电反应触发！pos={pos}");
    }

    protected override void OnBulletDespawn()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.DOT_FROST_BULLET);
    }

    private static GameObject BuildTemplate()
    {
        var go = new GameObject("FrostBullet");
        go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.Get(); sr.color = new Color(0.3f, 0.6f, 1f); sr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * 0.5f;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(0.4f, 0.2f);
        DotBulletVisualEffects.AttachFrostTrail(go);
        go.AddComponent<FrostBullet>();
        return go;
    }

    public static FrostBullet Create(Vector2 pos, Vector2 dir, float speed, int impactDmg,
        float frostDps, float freezeDuration, float slowPercent, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var pool = ObjectPool.Instance;
        GameObject go = null;
        if (pool != null && pool.HasPool(PoolHelper.DOT_FROST_BULLET))
        {
            go = pool.Spawn(PoolHelper.DOT_FROST_BULLET, pos, Quaternion.identity);
        }
        else
        {
            PoolHelper.RegisterVirtualPrefab(PoolHelper.DOT_FROST_BULLET, BuildTemplate, 15);
            go = pool != null ? pool.Spawn(PoolHelper.DOT_FROST_BULLET, pos, Quaternion.identity) : null;
        }

        if (go == null)
        {
            go = new GameObject("FrostBullet");
            go.tag = "Untagged";
            PhysicsLayerSetup.SetAsBullet(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = DotSpriteCache.Get(); sr.color = new Color(0.3f, 0.6f, 1f); sr.sortingOrder = 15;
            go.transform.localScale = Vector3.one * 0.5f;
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(0.4f, 0.2f);
            DotBulletVisualEffects.AttachFrostTrail(go);
            go.AddComponent<FrostBullet>();
        }

        go.transform.position = pos;
        go.SetActive(true);
        var b = go.GetComponent<FrostBullet>();
        b.SetupFrost(speed, impactDmg, frostDps, freezeDuration, slowPercent, dmgMult, canCrit, critChance, critMult);
        b.SetDirection(dir);
        return b;
    }
}

/// <summary>
/// 霜冻效果 — 永久减速（30%基础 + 每层额外5%，最高90%减速），持续直到敌人死亡
/// 不再冻住敌人，只降低移动速度并叠层
/// </summary>
public class FrostEffect : MonoBehaviour
{
    public float _slowPercent;
    public float _frostDps;
    public bool _canCrit; public float _critChance, _critMult;
    private EnemyBase _enemyBase;
    private Damageable _damageable;
    private SpriteRenderer _sr;
    private Color _originalColor;
    private DotColorBlender _blender;
    private int _frostStacks = 0;

    private float _baseSlow = 0.30f;
    private float _perStackSlow = 0.05f;
    private float _maxSlow = 0.90f;

    /// <summary>
    /// 施加霜冻减速 — 通过 EnemyBase.FrostSlowMultiplier 集中管理
    /// </summary>
    public void ApplyFreeze(float freezeDuration, float slowPercent, float frostDps,
        bool canCrit, float critChance, float critMult)
    {
        _frostDps = Mathf.Max(_frostDps, frostDps);
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
        _frostStacks++;
        _slowPercent = Mathf.Min(_maxSlow, _baseSlow + (_frostStacks - 1) * _perStackSlow);

        // 确保已捕获原始颜色（ApplyFreeze可能在Start之前被调用）
        EnsureColorCaptured();

        if (_enemyBase == null) _enemyBase = GetComponent<EnemyBase>();
        if (_enemyBase != null)
        {
            // 通过集中管理接口设置减速乘数
            _enemyBase.FrostSlowMultiplier = 1f - _slowPercent;
        }
    }

    public int FrostStacks => _frostStacks;

    /// <summary>
    /// 消耗一层霜冻（用于元素反应：霜电）。返回是否成功消耗。
    /// </summary>
    public bool ConsumeStack()
    {
        if (_frostStacks <= 0) return false;
        _frostStacks--;
        _slowPercent = Mathf.Min(_maxSlow, _baseSlow + (_frostStacks - 1) * _perStackSlow);
        if (_frostStacks <= 0) _slowPercent = 0f;
        DebugHelper.Log($"[FrostEffect] Stack consumed! Remaining={_frostStacks}");
        return true;
    }

    private void OnEnable()
    {
        _slowPercent = 0f;
        _frostStacks = 0;
        _frostDps = 0f;
        _lastSyncedStacks = -1;
        // 缓存组件引用（支持对象池复用）
        if (_enemyBase == null) _enemyBase = GetComponent<EnemyBase>();
        if (_enemyBase != null) _enemyBase.FrostSlowMultiplier = 1f;
        _damageable = GetComponent<Damageable>();
        _blender = GetComponent<DotColorBlender>();
        EnsureColorCaptured();
        DotEffectRegistry.Register(this); // #24 注册到统一注册表
        RefreshFromConfig();
        DotBulletConfig.OnConfigChanged += RefreshFromConfig;
    }

    /// <summary>确保已捕获原始颜色（ApplyFreeze可能在Start之前被调用）</summary>
    private void EnsureColorCaptured()
    {
        if (_sr == null)
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _originalColor = _sr.color;
        }
    }

    private int _lastSyncedStacks = -1; // 只在层数变化时同步减速

    private void Update()
    {
        // 敌人死亡时清理
        if (_damageable == null || _damageable.CurrentHp <= 0) { Cleanup(); return; }

        // 只在层数变化时同步减速乘数到 EnemyBase（避免每帧赋值）
        if (_frostStacks > 0 && _enemyBase != null && _frostStacks != _lastSyncedStacks)
        {
            _lastSyncedStacks = _frostStacks;
            _enemyBase.FrostSlowMultiplier = 1f - _slowPercent;
        }
    }

    /// <summary>
    /// 在 LateUpdate 中设置颜色，确保在 DotColorBlender 之后执行
    /// 1层≈3%蓝，5层≈15%蓝，20层≈60%蓝，34层以上完全蓝色
    /// </summary>
    private void LateUpdate()
    {
        if (_frostStacks <= 0 || _sr == null) return;
        if (_damageable == null || _damageable.CurrentHp <= 0) return;

        // 注销 DotColorBlender 的 frost 注册（我们自己管理颜色）
        if (_blender != null) _blender.UnregisterDot("frost");

        float intensity = Mathf.Clamp01(_frostStacks / 34f);
        _sr.color = Color.Lerp(_originalColor, DotColorBlender.FROST_BLUE, intensity);
    }

    private void Cleanup()
    {
        // 恢复霜冻减速乘数
        if (_enemyBase != null) _enemyBase.FrostSlowMultiplier = 1f;
        // 恢复原始颜色
        if (_sr != null) _sr.color = _originalColor;
        UnregisterColor();
        Destroy(this);
    }
    private void UnregisterColor()
    {
        if (_blender != null) _blender.UnregisterDot("frost");
    }

    private void OnDisable()
    {
        DotBulletConfig.OnConfigChanged -= RefreshFromConfig;
        // 恢复霜冻减速乘数
        if (_enemyBase != null && _frostStacks > 0) _enemyBase.FrostSlowMultiplier = 1f;
        // 恢复原始颜色
        if (_sr != null && _frostStacks > 0) _sr.color = _originalColor;
        UnregisterColor();
    }
    private void OnDestroy() { DotEffectRegistry.Unregister(this); UnregisterColor(); }

    private void RefreshFromConfig()
    {
        var cfg = DotBulletConfig.GetDefault();
        _maxSlow = cfg.FrostMaxSlow;
        _baseSlow = cfg.FrostBaseSlowPct;
        _perStackSlow = cfg.FrostSlowPerStack;
    }
}