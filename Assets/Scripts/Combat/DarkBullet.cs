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
        _visualApplied = false;

        // 视觉：敌人略微变黑
        ApplyDarkVisual();
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

    private void Start()
    {
        _damageable = GetComponent<Damageable>();
    }

    private void OnEnable()
    {
        // 对象池重用时重置状态
        _spreadDone = false;
        _visualApplied = false;
        _damageable = GetComponent<Damageable>();
    }


    /// <summary>
    /// 死亡时传播DOT到周围敌人
    /// </summary>
    private void SpreadDotOnDeath()
    {
        var sem = GetComponent<StatusEffectManager>();
        if (sem == null)
        {
            DebugHelper.Log("[DarkMark] No StatusEffectManager on death enemy");
            return;
        }

        float radius = _spreadRadius * (1f + RadiusBonus);
        float efficiency = Mathf.Clamp01(_spreadEfficiency + EfficiencyBonus);
        Vector3 pos = transform.position;

        // 收集自身的DOT效果
        var effects = sem.ActiveEffects;
        if (effects == null || effects.Count == 0)
        {
            DebugHelper.Log("[DarkMark] No active DOT effects to spread");
            return;
        }

        DebugHelper.Log($"[DarkMark] Enemy dying with {effects.Count} DOT types, spreading at {efficiency:P0} eff");

        // 查找周围敌人
        Collider2D[] nearby = Physics2D.OverlapCircleAll(pos, radius);
        int spreadCount = 0;

        foreach (var col in nearby)
        {
            if (!col.CompareTag("Enemy")) continue;
            if (col.gameObject == gameObject) continue; // 不传播给自己

            var targetDmg = col.GetComponent<Damageable>();
            if (targetDmg == null || targetDmg.CurrentHp <= 0) continue;

            DotBulletHelper.EnsureStatusEffectManager(col.gameObject);
            var targetSem = col.GetComponent<StatusEffectManager>();
            if (targetSem == null) continue;

            // 传播每种DOT
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

            // 传播时附加紫色视觉效果
            CombatManager.CreateExplosionEffect(col.transform.position, 0.4f,
                new Color(0.4f, 0.1f, 0.6f, 0.5f), 0.3f);

            spreadCount++;
        }

        if (spreadCount > 0)
        {
            // 死亡时暗紫色冲击波扩散
            CombatManager.CreateExplosionEffect(pos, radius, new Color(0.4f, 0.1f, 0.6f, 0.3f), 0.5f);
            DebugHelper.Log($"[DarkMark] Spread DOT to {spreadCount} enemies, radius={radius:F1}, eff={efficiency:P0}");
        }
    }

    private void OnDisable()
    {
        // 敌人死亡回到池时触发传播（比Update轮询更可靠）
        if (!_spreadDone)
        {
            var dmg = GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp <= 0)
            {
                SpreadDotOnDeath();
                _spreadDone = true;
            }
        }
        RestoreVisual();
    }

    private void OnDestroy()
    {
        RestoreVisual();
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