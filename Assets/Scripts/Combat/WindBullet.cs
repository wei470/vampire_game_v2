using UnityEngine;

/// <summary>
/// 风子弹 — 高攻速（0.2s/发）高子弹速度的DOT子弹。
/// 
/// 机制：
/// - 命中敌人造成即时伤害
/// - 每命中同一敌人5次，施加1层风化（WindErosionEffect）
/// - 风化层数越高，敌人被击退距离越远
/// - 风化效果：每层减速5%（最高90%），定时击退
/// </summary>
public class WindBullet : MonoBehaviour
{
    private float _speed = 24f;       // 高子弹速度
    private float _lifetime = 2f;
    private int _impactDamage = 2;    // 低单发伤害（高攻速补偿）
    private float _damageMultiplier = 1f;
    private bool _canCrit; private float _critChance, _critMult;
    private Vector2 _direction;
    private float _spawnTime;
    private Rigidbody2D _rb;
    private PenetrateHandler _cachedPenetrate;

    public void Setup(float speed, int impactDmg, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        _speed = speed; _impactDamage = impactDmg;
        _damageMultiplier = dmgMult;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }

    public void SetDirection(Vector2 dir)
    {
        _direction = dir.normalized;
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

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
        if (dmg == null || dmg.CurrentHp <= 0) return;

        // 命中不造成直接伤害（与霜冻/雷电/黑暗一致，只施加效果）
        // 施加风化效果（追踪命中次数 + 叠层）
        DotBulletHelper.EnsureStatusEffectManager(other.gameObject);
        var windEffect = other.GetComponent<WindErosionEffect>();
        if (windEffect == null)
            windEffect = other.gameObject.AddComponent<WindErosionEffect>();
        windEffect.RegisterHit();

        // 穿透检查
        if (_cachedPenetrate != null && _cachedPenetrate.TryPenetrate(other)) return;
        var ricochet = GetComponent<RicochetHandler>();
        if (ricochet != null && ricochet.TryRicochet(transform.position, other)) return;
        DespawnSelf();
    }

    private void DespawnSelf()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.DOT_WIND_BULLET);
    }

    private static GameObject BuildTemplate()
    {
        var go = new GameObject("WindBullet");
        go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.Get();
        sr.color = new Color(0.7f, 0.85f, 1f); // 淡风蓝色
        sr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * 0.35f;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(0.4f, 0.2f);
        DotBulletVisualEffects.AttachWindTrail(go);
        go.AddComponent<WindBullet>();
        return go;
    }

    public static WindBullet Create(Vector2 pos, Vector2 dir, float speed, int impactDmg,
        float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var pool = ObjectPool.Instance;
        GameObject go = null;
        if (pool != null && pool.HasPool(PoolHelper.DOT_WIND_BULLET))
        {
            go = pool.Spawn(PoolHelper.DOT_WIND_BULLET, pos, Quaternion.identity);
        }
        else
        {
            PoolHelper.RegisterVirtualPrefab(PoolHelper.DOT_WIND_BULLET, BuildTemplate, 20);
            go = pool != null ? pool.Spawn(PoolHelper.DOT_WIND_BULLET, pos, Quaternion.identity) : null;
        }

        if (go == null)
        {
            go = new GameObject("WindBullet");
            go.tag = "Untagged";
            PhysicsLayerSetup.SetAsBullet(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = DotSpriteCache.Get();
            sr.color = new Color(0.7f, 0.85f, 1f);
            sr.sortingOrder = 15;
            go.transform.localScale = Vector3.one * 0.35f;
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(0.4f, 0.2f);
            DotBulletVisualEffects.AttachWindTrail(go);
            go.AddComponent<WindBullet>();
        }

        go.transform.position = pos;
        go.SetActive(true);
        var b = go.GetComponent<WindBullet>();
        b.Setup(speed, impactDmg, dmgMult, canCrit, critChance, critMult);
        b.SetDirection(dir);
        b._cachedPenetrate = go.GetComponent<PenetrateHandler>();
        return b;
    }
}

/// <summary>
/// 风化效果 — 挂载到敌人身上。
///
/// 命中追踪：
/// - 每被风子弹命中5次，叠加1层风化
/// - 每层风化：击退距离+5%（基础2f，每层额外+5%距离）
/// - 每施加1层风化造成5点伤害（可叠加）
/// - 敌人头上显示风化层数（xN文字）
///
/// 视觉：敌人颜色逐渐变为淡蓝白色，层数越高越明显
/// </summary>
public class WindErosionEffect : MonoBehaviour
{
    private int _hitCount = 0;
    private int _windStacks = 0;
    private Damageable _damageable;
    private SpriteRenderer _sr;
    private Color _originalColor;
    private DotColorBlender _blender;

    // ── 层数文字显示 ──
    private GameObject _stackTextObj;
    private TextMesh _cachedTextMesh;
    private int _lastDisplayStacks = -1;

    private const int HITS_PER_STACK = 1;    // 每发子弹叠1层风化
    private const int DAMAGE_PER_STACK = 5;       // 每层风化造成5点伤害
    private const float KNOCKBACK_DISTANCE = 3f;   // 固定击退距离（约30%屏幕）
    private static readonly Color WIND_COLOR = new Color(0.7f, 0.85f, 1f);
    private static readonly Color WIND_POPUP_COLOR = new Color(0.7f, 0.85f, 1f);

    public int WindStacks => _windStacks;
    public int HitCount => _hitCount;

    /// <summary>
    /// 注册一次风子弹命中。每发子弹叠加1层风化。
    /// </summary>
    public void RegisterHit()
    {
        _hitCount++;
        AddWindStack();
    }

    /// <summary>
    /// 消耗一层风化（用于元素反应：燃烧扩散）。返回是否成功消耗。
    /// </summary>
    public bool ConsumeStack()
    {
        if (_windStacks <= 0) return false;
        _windStacks--;
        UpdateStackText();
        DebugHelper.Log($"[WindErosion] Stack consumed! Remaining={_windStacks}");
        // 风化层数归零时清理效果
        if (_windStacks <= 0)
        {
            Cleanup();
        }
        return true;
    }

    private void AddWindStack()
    {
        _windStacks++;

        // 每施加一层风化造成5点伤害
        if (_damageable == null) _damageable = GetComponent<Damageable>();
        if (_damageable != null && _damageable.CurrentHp > 0)
        {
            _damageable.TakeDamage(DAMAGE_PER_STACK, WIND_POPUP_COLOR);
        }

        // 立即施加击退
        ApplyKnockback();

        // 更新层数文字
        UpdateStackText();

        // 视觉反馈：风化爆炸特效
        CombatManager.CreateExplosionEffect(transform.position, 0.6f + _windStacks * 0.15f,
            new Color(0.7f, 0.85f, 1f, 0.6f), 0.3f);

        DebugHelper.Log($"[WindErosion] Stack added! Total={_windStacks}, Hits={_hitCount}, Knockback={GetKnockbackForce():F1}");
    }

    /// <summary>
    /// 获取固定击退距离
    /// </summary>
    public float GetKnockbackForce()
    {
        return KNOCKBACK_DISTANCE;
    }

    private void Start()
    {
        _damageable = GetComponent<Damageable>();
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _originalColor = _sr.color;
        _blender = GetComponent<DotColorBlender>();
    }

    private void OnEnable()
    {
        _hitCount = 0;
        _windStacks = 0;
        _lastDisplayStacks = -1;
        // 清理可能残留的文字（对象池复用时）
        if (_stackTextObj != null) { Destroy(_stackTextObj); _stackTextObj = null; }
    }

    private void Update()
    {
        if (_windStacks <= 0) return;
        if (_damageable == null || _damageable.CurrentHp <= 0) { Cleanup(); return; }

        // 通过 DotColorBlender 更新颜色
        if (_blender == null) _blender = GetComponent<DotColorBlender>();
        if (_blender != null)
        {
            float intensity = Mathf.Clamp01(_windStacks / 10f);
            _blender.RegisterDot("wind", WIND_COLOR, intensity, 8f);
        }
    }

    /// <summary>
    /// 更新层数文字（与 LightMarkEffect 相同的 xN 格式）
    /// </summary>
    private void UpdateStackText()
    {
        if (_stackTextObj == null)
        {
            _stackTextObj = new GameObject("WindErosionText");
            _stackTextObj.transform.localScale = Vector3.one * 0.3f;

            _cachedTextMesh = _stackTextObj.AddComponent<TextMesh>();
            _cachedTextMesh.characterSize = 0.2f;
            _cachedTextMesh.anchor = TextAnchor.MiddleCenter;
            _cachedTextMesh.alignment = TextAlignment.Center;
            _cachedTextMesh.fontSize = 40;
            _cachedTextMesh.color = new Color(0.7f, 0.85f, 1f);
            _cachedTextMesh.fontStyle = FontStyle.Bold;
            _lastDisplayStacks = -1;
        }

        if (_windStacks != _lastDisplayStacks)
        {
            _lastDisplayStacks = _windStacks;
            if (_cachedTextMesh == null) _cachedTextMesh = _stackTextObj.GetComponent<TextMesh>();
            if (_cachedTextMesh != null)
            {
                _cachedTextMesh.text = $"x{_windStacks}";
                // 颜色越叠越亮
                float t = Mathf.Clamp01(_windStacks / 10f);
                _cachedTextMesh.color = Color.Lerp(new Color(0.7f, 0.85f, 1f), new Color(0.9f, 1f, 1f), t);
            }
        }
    }

    /// <summary>
    /// 施加击退 — 从玩家方向推开敌人，距离随风化层数增加
    /// </summary>
    private void ApplyKnockback()
    {
        if (_damageable == null || _damageable.CurrentHp <= 0) return;

        var player = GameReferences.Player;
        if (player == null) return;

        // 直接修改位置实现击退（EnemyBase.FixedUpdate会覆盖velocity，所以用transform）
        Vector2 knockDir = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;
        float dist = GetKnockbackForce();
        transform.position += (Vector3)(knockDir * dist);

        // 击退特效
        CombatManager.CreateExplosionEffect(transform.position, 0.3f + _windStacks * 0.1f,
            new Color(0.7f, 0.85f, 1f, 0.4f), 0.2f);
    }

    private void LateUpdate()
    {
        // 持续更新文字位置跟随敌人
        if (_stackTextObj != null)
        {
            _stackTextObj.transform.position = transform.position + new Vector3(0, 0.5f, 0);
            _stackTextObj.transform.rotation = Quaternion.identity;
        }
    }

    private void Cleanup()
    {
        UnregisterColor();
        if (_stackTextObj != null) { Destroy(_stackTextObj); _stackTextObj = null; }
        Destroy(this);
    }

    private void UnregisterColor()
    {
        if (_blender != null) _blender.UnregisterDot("wind");
    }

    private void OnDisable()
    {
        UnregisterColor();
        if (_stackTextObj != null) { Destroy(_stackTextObj); _stackTextObj = null; }
        _windStacks = 0;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = _originalColor;
    }

    private void OnDestroy()
    {
        UnregisterColor();
        if (_stackTextObj != null) Destroy(_stackTextObj);
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = _originalColor;
    }
}
