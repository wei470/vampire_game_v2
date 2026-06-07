using UnityEngine;

/// <summary>
/// 黑暗子弹 — 命中敌人后施加黑暗标记（非DOT伤害，纯标记效果）
/// 持有黑暗标记的敌人死亡时，自身所有DOT层数按50%效果传播给周围敌人
/// </summary>
public class DarkBullet : MonoBehaviour
{
    private float _speed = 6f;       // 缓慢子弹（约40%普通子弹速度）
    private float _lifetime = 5f;
    private float _markSpreadRadius = 3f;   // 死亡传播半径
    private float _markSpreadEfficiency = 0.5f; // 传播效率50%
    private Vector2 _direction;
    private float _spawnTime;
    private Rigidbody2D _rb;

    private void Awake() { _rb = GetComponent<Rigidbody2D>(); }
    private void Start() { _spawnTime = Time.time; }
    private void Update() { if (Time.time - _spawnTime > _lifetime) Destroy(gameObject); }
    private void FixedUpdate() { _rb.linearVelocity = _direction * _speed; }

    public void SetDirection(Vector2 dir)
    {
        _direction = dir.normalized;
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void Setup(float speed, float spreadRadius, float spreadEfficiency)
    {
        _speed = speed;
        _markSpreadRadius = spreadRadius;
        _markSpreadEfficiency = spreadEfficiency;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        var dmg = other.GetComponent<Damageable>();
        if (dmg == null || dmg.CurrentHp <= 0) return;

        // 施加黑暗标记（不造成直接伤害）
        DotBulletHelper.EnsureStatusEffectManager(other.gameObject);
        var darkMark = other.GetComponent<DarkMarkEffect>();
        if (darkMark == null)
            darkMark = other.gameObject.AddComponent<DarkMarkEffect>();
        darkMark.Init(_markSpreadRadius, _markSpreadEfficiency);

        // 命中视觉效果
        CombatManager.CreateExplosionEffect(other.transform.position, 0.4f,
            new Color(0.4f, 0.1f, 0.6f, 0.6f), 0.3f);

        Destroy(gameObject);
    }

    /// <summary>
    /// 创建黑暗子弹
    /// </summary>
    public static DarkBullet Create(Vector2 pos, Vector2 dir, float speed,
        float spreadRadius, float spreadEfficiency)
    {
        var go = new GameObject("DarkBullet");
        go.transform.position = pos;
        go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.Get();
        sr.color = new Color(0.4f, 0.1f, 0.6f); // 暗紫色
        sr.sortingOrder = 15;

        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.6f, 0.3f);

        // 暗紫色拖尾
        DotBulletVisualEffects.AttachTrail(go, new Color(0.4f, 0.1f, 0.6f, 0.7f), 0.8f, 0.05f);

        var bullet = go.AddComponent<DarkBullet>();
        bullet.Setup(speed, spreadRadius, spreadEfficiency);
        bullet.SetDirection(dir);
        return bullet;
    }
}

/// <summary>
/// 黑暗标记效果 — 挂载到敌人身上
/// 命中的敌人略微变黑（视觉反馈）
/// 敌人死亡时，将所有DOT层数按50%效果传播给周围敌人
/// 黑暗标记本身不会被传播
/// </summary>
public class DarkMarkEffect : MonoBehaviour
{
    private float _spreadRadius = 3f;
    private float _spreadEfficiency = 0.5f;
    private Damageable _damageable;
    private bool _spreadDone = false;
    private bool _visualApplied = false;
    private Color _originalColor;
    private BaseEntity _entity;

    /// <summary>
    /// 传播范围加成（可被升级增强）
    /// </summary>
    [System.NonSerialized] public float RadiusBonus = 0f;
    /// <summary>
    /// 传播效率加成（可被升级增强）
    /// </summary>
    [System.NonSerialized] public float EfficiencyBonus = 0f;

    public void Init(float radius, float efficiency)
    {
        _spreadRadius = radius;
        _spreadEfficiency = efficiency;
        _damageable = GetComponent<Damageable>();
        _spreadDone = false;

        // 视觉：敌人略微变黑
        ApplyDarkVisual();
        // 订阅死亡事件（在敌人实际死亡时立即传播，此时DOT效果还在）
        SubscribeDeath();
    }

    private void SubscribeDeath()
    {
        _entity = GetComponent<BaseEntity>();
        if (_entity != null) _entity.OnDeath += OnDeathHandler;
    }

    private void UnsubscribeDeath()
    {
        if (_entity != null) { _entity.OnDeath -= OnDeathHandler; _entity = null; }
    }

    private void OnDeathHandler(Vector3 deathPos)
    {
        if (!_spreadDone)
        {
            _spreadDone = true;
            SpreadDotOnDeath();
        }
    }

    private void ApplyDarkVisual()
    {
        if (_visualApplied) return;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            _originalColor = sr.color;
            // 降低亮度30%，增加紫色色调
            float darken = 0.7f;
            sr.color = new Color(
                _originalColor.r * darken + 0.1f,
                _originalColor.g * darken,
                _originalColor.b * darken + 0.15f,
                _originalColor.a);
            _visualApplied = true;
        }
    }

    private void OnEnable()
    {
        // 对象池重用时重置状态
        _spreadDone = false;
        _visualApplied = false;
        _damageable = GetComponent<Damageable>();
        // 重新订阅死亡事件（如果之前Init过）
        if (_entity != null) SubscribeDeath();
    }

    private void OnDisable()
    {
        UnsubscribeDeath();
        RestoreVisual();
    }

    private void OnDestroy()
    {
        UnsubscribeDeath();
        RestoreVisual();
    }

    /// <summary>
    /// 死亡时传播DOT到周围敌人（通过OnDeath事件触发，此时效果还在）
    /// </summary>
    private void SpreadDotOnDeath()
    {
        var sem = GetComponent<StatusEffectManager>();

        float radius = _spreadRadius * (1f + RadiusBonus);
        float efficiency = Mathf.Clamp01(_spreadEfficiency + EfficiencyBonus);
        Vector3 pos = transform.position;

        // 收集自身的DOT效果（可能为空，但仍需传播独立DOT组件）
        var effects = sem?.ActiveEffects;
        bool hasAnyDot = (effects != null && effects.Count > 0);
        bool hasBleed = TryGetComponent<BleedEffect>(out _);
        bool hasBurn = TryGetComponent<BurnStackEffect>(out _);
        bool hasPoison = TryGetComponent<PoisonStackEffect>(out _);
        bool hasFrost = TryGetComponent<FrostEffect>(out _);

        if (!hasAnyDot && !hasBleed && !hasBurn && !hasPoison && !hasFrost)
        {
            DebugHelper.Log("[DarkMark] No active DOT effects to spread");
            return;
        }

        DebugHelper.Log($"[DarkMark] Enemy dying with {(effects?.Count ?? 0)} DOT types + indie components, spreading at {efficiency:P0} eff");

        // 查找周围敌人
        Collider2D[] nearby = Physics2D.OverlapCircleAll(pos, radius);
        int spreadCount = 0;
        Collider2D nearestEnemy = null;
        float nearestDist = float.MaxValue;

        foreach (var col in nearby)
        {
            if (!col.CompareTag("Enemy")) continue;
            if (col.gameObject == gameObject) continue;

            var targetDmg = col.GetComponent<Damageable>();
            if (targetDmg == null || targetDmg.CurrentHp <= 0) continue;

            float dist = Vector2.Distance(pos, col.transform.position);
            if (dist < nearestDist) { nearestDist = dist; nearestEnemy = col; }

            DotBulletHelper.EnsureStatusEffectManager(col.gameObject);
            var targetSem = col.GetComponent<StatusEffectManager>();
            if (targetSem == null) continue;

            // 传播 StatusEffectManager 中的每种DOT
            if (effects != null)
            {
                foreach (var effect in effects)
                {
                    // 跳过黑暗标记本身（防止无限传播）
                    if (effect.type == StatusEffectType.Dark) continue;

                    float spreadDps = effect.damagePerSecond * efficiency;
                    float spreadDuration = effect.remainingDuration * efficiency;

                    if (spreadDps <= 0f && effect.type != StatusEffectType.Frostbite) continue;

                    targetSem.ApplyEffect(effect.type, spreadDps, spreadDuration,
                        effect.canCrit, effect.critChance, effect.critMultiplier);
                }
            }

            // 传播独立DOT组件（与CurseSpreadSystem一致）
            if (hasBleed)
            {
                var srcBleed = GetComponent<BleedEffect>();
                if (!col.TryGetComponent<BleedEffect>(out var tgtBleed))
                    tgtBleed = col.gameObject.AddComponent<BleedEffect>();
                tgtBleed.Refresh(srcBleed._dps * efficiency, srcBleed._duration * efficiency,
                    srcBleed._canCrit, srcBleed._critChance, srcBleed._critMult);
            }
            if (hasBurn)
            {
                var srcBurn = GetComponent<BurnStackEffect>();
                if (!col.TryGetComponent<BurnStackEffect>(out var tgtBurn))
                    tgtBurn = col.gameObject.AddComponent<BurnStackEffect>();
                int stacks = Mathf.Max(1, Mathf.RoundToInt(srcBurn.StackCount * efficiency));
                for (int s = 0; s < stacks; s++)
                    tgtBurn.AddStack(srcBurn._baseDps * efficiency, srcBurn._duration * efficiency,
                        srcBurn._canCrit, srcBurn._critChance, srcBurn._critMult);
            }
            if (hasPoison)
            {
                var srcPoison = GetComponent<PoisonStackEffect>();
                if (!col.TryGetComponent<PoisonStackEffect>(out var tgtPoison))
                    tgtPoison = col.gameObject.AddComponent<PoisonStackEffect>();
                int pStacks = Mathf.Max(1, Mathf.RoundToInt(srcPoison.StackCount * efficiency));
                for (int s = 0; s < pStacks; s++)
                    tgtPoison.AddStack(2f, 0f, srcPoison._canCrit, srcPoison._critChance, srcPoison._critMult);
            }
            if (hasFrost)
            {
                var srcFrost = GetComponent<FrostEffect>();
                if (!col.TryGetComponent<FrostEffect>(out var tgtFrost))
                    tgtFrost = col.gameObject.AddComponent<FrostEffect>();
                tgtFrost.ApplyFreeze(0.3f, srcFrost._slowPercent * efficiency,
                    srcFrost._frostDps * efficiency, srcFrost._canCrit, srcFrost._critChance, srcFrost._critMult);
            }

            // 传播时附加紫色视觉效果
            CombatManager.CreateExplosionEffect(col.transform.position, 0.4f,
                new Color(0.4f, 0.1f, 0.6f, 0.5f), 0.3f);

            spreadCount++;
        }

        // 锁链视觉：从死亡敌人指向最近敌人
        if (nearestEnemy != null)
            CreateDarkChain(pos, nearestEnemy.transform.position);

        if (spreadCount > 0)
        {
            // 死亡时暗紫色冲击波扩散
            CombatManager.CreateExplosionEffect(pos, radius, new Color(0.4f, 0.1f, 0.6f, 0.3f), 0.5f);
            DebugHelper.Log($"[DarkMark] Spread DOT to {spreadCount} enemies, radius={radius:F1}, eff={efficiency:P0}");
        }
    }

    /// <summary>
    /// 创建暗紫色锁链视觉效果（从死亡敌人指向最近敌人）
    /// </summary>
    private static void CreateDarkChain(Vector3 from, Vector3 to)
    {
        var lineObj = new GameObject("DarkChain");
        lineObj.transform.position = from;
        var lr = lineObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(0.5f, 0.1f, 0.8f, 0.9f);
        lr.endColor = new Color(0.3f, 0.05f, 0.5f, 0f);
        lr.startWidth = 0.15f;
        lr.endWidth = 0.05f;
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.sortingOrder = 25;
        Object.Destroy(lineObj, 0.6f);
    }

    private void RestoreVisual()
    {
        if (_visualApplied)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = _originalColor;
            _visualApplied = false;
        }
    }
}
