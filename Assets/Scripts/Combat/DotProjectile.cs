using UnityEngine;

/// <summary>
/// DOT 子弹系统 — Mage 专属，支持 4 种不同的子弹类型。
///
/// 1. BleedBullet: 被动效果，命中后附加流血，敌人移动时受伤
/// 2. PoisonPotion: 投掷药瓶，爆炸生成毒液池
/// 3. BurnBullet: 慢速橙色子弹，叠加燃烧效果
/// 4. FrostBullet: 快速子弹，冰冻+永久减速+霜伤
/// </summary>

// ═══════════════════════════════════════════════════════════════
// 通用工具方法
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// DOT 子弹通用工具类
/// </summary>
public static class DotBulletHelper
{
    /// <summary>
    /// 确保敌人有 StatusEffectManager（诅咒传播需要死亡事件注册）
    /// </summary>
    public static void EnsureStatusEffectManager(GameObject enemy)
    {
        if (enemy.GetComponent<StatusEffectManager>() == null)
            enemy.AddComponent<StatusEffectManager>();
    }
}

// ═══════════════════════════════════════════════════════════════
// 通用 DOT 子弹基类
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// 流血子弹 — 命中后附加流血被动效果（敌人移动时受伤）
/// </summary>
public class BleedBullet : MonoBehaviour
{
    private float _speed = 14f;
    private float _lifetime = 3f;
    private int _impactDamage = 3;
    private float _bleedDps = 2f;
    private float _bleedDuration = 4f;
    private float _damageMultiplier = 1f;
    private bool _canCrit;
    private float _critChance, _critMultiplier;
    private Vector2 _direction;
    private float _spawnTime;

    public void Setup(float speed, int impactDmg, float bleedDps, float bleedDuration,
        float dmgMult, bool canCrit, float critChance, float critMult)
    {
        _speed = speed; _impactDamage = impactDmg; _bleedDps = bleedDps;
        _bleedDuration = bleedDuration; _damageMultiplier = dmgMult;
        _canCrit = canCrit; _critChance = critChance; _critMultiplier = critMult;
    }

    public void SetDirection(Vector2 dir) { _direction = dir.normalized; }

    private void Start() { _spawnTime = Time.time; }
    private void Update() { if (Time.time - _spawnTime > _lifetime) Destroy(gameObject); }
    private void FixedUpdate() { GetComponent<Rigidbody2D>().linearVelocity = _direction * _speed; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        var dmg = other.GetComponent<Damageable>();
        if (dmg != null && dmg.CurrentHp > 0)
        {
            dmg.TakeDamage(Mathf.RoundToInt(_impactDamage * _damageMultiplier));
            // 确保敌人有 StatusEffectManager（诅咒传播需要死亡事件注册）
            DotBulletHelper.EnsureStatusEffectManager(other.gameObject);
            // 附加流血被动效果
            var bleed = other.GetComponent<BleedEffect>();
            if (bleed == null) bleed = other.gameObject.AddComponent<BleedEffect>();
            bleed.Refresh(_bleedDps * _damageMultiplier, _bleedDuration, _canCrit, _critChance, _critMultiplier);
        }
        Destroy(gameObject);
    }

    public static BleedBullet Create(Vector2 pos, Vector2 dir, float speed, int impactDmg,
        float bleedDps, float bleedDuration, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var go = new GameObject("BleedBullet");
        go.transform.position = pos;
        go.tag = "Untagged";
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.Get(); sr.color = new Color(0.9f, 0.1f, 0.1f); sr.sortingOrder = 15;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(0.5f, 0.25f);
        var b = go.AddComponent<BleedBullet>();
        b.Setup(speed, impactDmg, bleedDps, bleedDuration, dmgMult, canCrit, critChance, critMult);
        b.SetDirection(dir);
        return b;
    }
}

/// <summary>
/// 流血被动效果 — 挂载到敌人身上，敌人移动时受伤
/// </summary>
public class BleedEffect : MonoBehaviour
{
    public float _dps;
    public float _duration;
    private float _startTime;
    public bool _canCrit; public float _critChance, _critMult;
    private Vector3 _lastPosition;
    private float _damageAccumulator;
    private const float MOVE_THRESHOLD = 0.1f; // 移动阈值
    private Damageable _damageable;

    public void Refresh(float dps, float duration, bool canCrit, float critChance, float critMult)
    {
        _dps = Mathf.Max(_dps, dps);
        _duration = duration;
        _startTime = Time.time;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }

    private void Start()
    {
        _startTime = Time.time;
        _lastPosition = transform.position;
        _damageable = GetComponent<Damageable>();
    }

    private void Update()
    {
        if (Time.time - _startTime > _duration) { Destroy(this); return; }

        // 检测移动
        float moved = Vector3.Distance(transform.position, _lastPosition);
        _lastPosition = transform.position;

        if (moved > MOVE_THRESHOLD && _damageable != null && _damageable.CurrentHp > 0)
        {
            float dmg = _dps * Time.deltaTime * 3f; // 移动时伤害放大
            if (_canCrit && Random.value < _critChance) dmg *= _critMult;
            _damageAccumulator += dmg;

            if (_damageAccumulator >= 1f)
            {
                int intDmg = Mathf.FloorToInt(_damageAccumulator);
                _damageable.TakeDamage(intDmg, new Color(0.9f, 0.15f, 0.15f)); // 流血红色
                _damageAccumulator -= intDmg;
            }
        }
    }

    private void OnDestroy()
    {
        // 恢复颜色
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;
    }
}

/// <summary>
/// 毒子弹 — 直线飞行（无限距离），命中第一个敌人后范围爆炸，叠加中毒层数
/// Mage 默认攻击子弹
/// </summary>
public class PoisonBullet : MonoBehaviour
{
    private float _speed = 14f;
    private float _poisonDps = 2f;
    private float _poisonDuration = 5f;
    private float _explosionRadius = 0.5f;
    private float _damageMultiplier = 1f;
    private bool _canCrit; private float _critChance, _critMult;
    private Vector2 _direction;
    private bool _exploded;
    private float _spawnTime;
    private float _lifetime = 5f; // 最长存活时间，未命中自动销毁

    public void Setup(float speed, float poisonDps, float poisonDuration, float explosionRadius,
        float dmgMult, bool canCrit, float critChance, float critMult)
    {
        _speed = speed; _poisonDps = poisonDps; _poisonDuration = poisonDuration;
        _explosionRadius = explosionRadius; _damageMultiplier = dmgMult;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }

    public void SetDirection(Vector2 dir) { _direction = dir.normalized; }

    private void Start() { _spawnTime = Time.time; }

    private void Update()
    {
        // 超时未命中，自动销毁（防止 Hierarchy 残留）
        if (!_exploded && Time.time - _spawnTime > _lifetime)
            Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        if (!_exploded)
            GetComponent<Rigidbody2D>().linearVelocity = _direction * _speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_exploded) return;
        if (!other.CompareTag("Enemy")) return;
        // 确保敌人有 StatusEffectManager（诅咒传播需要死亡事件注册）
        DotBulletHelper.EnsureStatusEffectManager(other.gameObject);
        // 命中毒液范围生成毒液池
        LeavePuddle(transform.position);
    }

    private void LeavePuddle(Vector2 center)
    {
        _exploded = true;
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

        // 命中敌人本身叠加中毒
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, _explosionRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            var dmg = hit.GetComponent<Damageable>();
            if (dmg == null || dmg.CurrentHp <= 0) continue;
            var poison = hit.GetComponent<PoisonStackEffect>();
            if (poison == null) poison = hit.gameObject.AddComponent<PoisonStackEffect>();
            poison.AddStack(_poisonDps * _damageMultiplier, _poisonDuration,
                _canCrit, _critChance, _critMult);
        }

        // 留下一滩毒液，持续2秒
        PoisonPuddle.Create(center, 0.5f, 2f,
            _poisonDps * _damageMultiplier, _canCrit, _critChance, _critMult);

        // 立即销毁子弹本体（旧代码只隐藏不销毁，导致Hierarchy残留）
        Destroy(gameObject);
    }

    public static PoisonBullet Create(Vector2 pos, Vector2 dir, float speed,
        float poisonDps, float poisonDuration, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var go = new GameObject("PoisonBullet");
        go.transform.position = pos; go.tag = "Untagged";
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(0.1f, 0.9f, 0.2f); sr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * 0.25f;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.25f;
        var b = go.AddComponent<PoisonBullet>();
        b.Setup(speed, poisonDps, poisonDuration, 0.5f, dmgMult, canCrit, critChance, critMult);
        b.SetDirection(dir);
        return b;
    }
}

// ═══════════════════════════════════════════════════════════════
// 中毒药瓶
// ═══════════════════════════════════════════════════════════════

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
        if (other.CompareTag("Enemy") || other.CompareTag("Untagged"))
            Explode();
    }

    private void Explode()
    {
        _exploded = true;
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        // 生成毒液池
        PoisonPuddle.Create(transform.position, _puddleRadius, _puddleDuration,
            _baseDps * _damageMultiplier, _canCrit, _critChance, _critMult);
        // 爆炸视觉
        CombatManager.CreateExplosionEffect(transform.position, _puddleRadius, new Color(0.1f, 0.9f, 0.2f, 0.5f), 0.3f);
        Destroy(gameObject);
    }

    public static PoisonPotion Create(Vector2 pos, Vector2 target, float speed,
        float puddleDuration, float puddleRadius, float baseDps, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var go = new GameObject("PoisonPotion");
        go.transform.position = pos; go.tag = "Untagged";
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.Get(); sr.color = new Color(0.1f, 0.8f, 0.1f); sr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * 0.8f;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.3f;
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

    public void Setup(float radius, float duration, float baseDps, bool canCrit, float critChance, float critMult)
    {
        _radius = radius; _duration = duration; _baseDps = baseDps;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }

    private void Start()
    {
        _spawnTime = Time.time;
        _lastTick = Time.time - 0.5f; // 立即触发第一次tick
        // 视觉特效缩小一半
        transform.localScale = Vector3.one * _radius;

        // 添加触发器碰撞体，确保敌人能被检测到
        var col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = _radius;
    }

    private void Update()
    {
        if (Time.time - _spawnTime > _duration) { Destroy(gameObject); return; }
        if (Time.time - _lastTick < 0.5f) return; // 每 0.5 秒叠加一层
        _lastTick = Time.time;

        ApplyPoisonToNearby();
    }

    /// <summary>
    /// 触发器持续检测 — 敌人站在毒液池内持续叠加中毒
    /// </summary>
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        if (Time.time - _lastTick < 0.5f) return;

        var dmg = other.GetComponent<Damageable>();
        if (dmg == null || dmg.CurrentHp <= 0) return;

        var poison = other.GetComponent<PoisonStackEffect>();
        if (poison == null) poison = other.gameObject.AddComponent<PoisonStackEffect>();
        poison.AddStack(_baseDps, _duration - (Time.time - _spawnTime), _canCrit, _critChance, _critMult);
    }

    private void ApplyPoisonToNearby()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _radius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            var dmg = hit.GetComponent<Damageable>();
            if (dmg == null || dmg.CurrentHp <= 0) continue;

            // 叠加中毒效果
            var poison = hit.GetComponent<PoisonStackEffect>();
            if (poison == null) poison = hit.gameObject.AddComponent<PoisonStackEffect>();
            poison.AddStack(_baseDps, _duration - (Time.time - _spawnTime), _canCrit, _critChance, _critMult);
        }
    }

    public static PoisonPuddle Create(Vector2 pos, float radius, float duration, float baseDps,
        bool canCrit, float critChance, float critMult)
    {
        var go = new GameObject("PoisonPuddle");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(0.1f, 0.7f, 0.1f, 0.4f);
        sr.sortingOrder = 1;
        var p = go.AddComponent<PoisonPuddle>();
        p.Setup(radius, duration, baseDps, canCrit, critChance, critMult);
        return p;
    }
}

/// <summary>
/// 中毒叠加效果 — 每tick掉2滴血，tick间隔随层数加速
/// x初始=1秒，每层 x = x * 0.9，最低0.2秒
/// 中毒 debuff 持续到敌人死亡（不再有时间限制）
/// </summary>
public class PoisonStackEffect : MonoBehaviour
{
    private int _stacks;
    public bool _canCrit; public float _critChance, _critMult;
    private float _tickAccumulator;
    private Damageable _damageable;
    private SpriteRenderer _sr;
    private Color _originalColor;

    private const float BASE_TICK_INTERVAL = 1f;   // 初始间隔1秒
    private const float TICK_DECAY = 0.9f;          // 每层 乘以0.9
    private const float MIN_TICK_INTERVAL = 0.2f;   // 最低0.2秒
    private const int DAMAGE_PER_TICK = 2;           // 每tick掉2滴血

    /// <summary>
    /// 当前中毒层数（供 EnemyHealthBar 显示）
    /// </summary>
    public int StackCount => _stacks;

    public void AddStack(float dps, float remainingTime, bool canCrit, float critChance, float critMult)
    {
        _stacks++;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }

    private void Start()
    {
        _damageable = GetComponent<Damageable>();
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _originalColor = _sr.color;
    }

    private void Update()
    {
        // 中毒持续到敌人死亡（只在层数为0时清理）
        if (_stacks <= 0) { Cleanup(); return; }

        // 敌人死亡时清除
        if (_damageable != null && _damageable.CurrentHp <= 0) { Cleanup(); return; }

        // 绿色闪烁
        if (_sr != null)
        {
            float pulse = Mathf.Sin(Time.time * 8f) * 0.3f;
            _sr.color = Color.Lerp(_originalColor, new Color(0.1f, 0.8f, 0.1f), 0.5f + pulse * 0.2f);
        }

        // 计算当前tick间隔：1 * 0.9^(stacks-1)，最低0.2
        float tickInterval = Mathf.Max(MIN_TICK_INTERVAL,
            BASE_TICK_INTERVAL * Mathf.Pow(TICK_DECAY, _stacks - 1));

        _tickAccumulator += Time.deltaTime;
        if (_tickAccumulator < tickInterval) return;
        _tickAccumulator -= tickInterval;

        if (_damageable != null && _damageable.CurrentHp > 0)
        {
            // 基础2 + 每层+1伤害
            int dmg = DAMAGE_PER_TICK + (_stacks - 1);
            if (_canCrit && Random.value < _critChance)
                dmg = Mathf.RoundToInt(dmg * _critMult);
            _damageable.TakeDamage(dmg, new Color(0.1f, 0.8f, 0.1f)); // 毒伤绿色
        }
    }

    private void Cleanup()
    {
        if (_sr != null) _sr.color = _originalColor;
        _stacks = 0;
        Destroy(this);
    }

    private void OnDestroy()
    {
        if (_sr != null) _sr.color = _originalColor;
    }
}

// ═══════════════════════════════════════════════════════════════
// 燃烧子弹
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// 燃烧子弹 — 慢速橙色子弹，命中叠加燃烧层数
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

    public void Setup(float speed, int impactDmg, float burnDps, float burnDuration,
        float dmgMult, bool canCrit, float critChance, float critMult)
    {
        _speed = speed; _impactDamage = impactDmg; _burnDps = burnDps;
        _burnDuration = burnDuration; _damageMultiplier = dmgMult;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }

    public void SetDirection(Vector2 dir) { _direction = dir.normalized; }

    private void Start() { _spawnTime = Time.time; }
    private void Update() { if (Time.time - _spawnTime > _lifetime) Destroy(gameObject); }
    private void FixedUpdate() { GetComponent<Rigidbody2D>().linearVelocity = _direction * _speed; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        var dmg = other.GetComponent<Damageable>();
        if (dmg != null && dmg.CurrentHp > 0)
        {
            dmg.TakeDamage(Mathf.RoundToInt(_impactDamage * _damageMultiplier));
            // 确保敌人有 StatusEffectManager（诅咒传播需要死亡事件注册）
            DotBulletHelper.EnsureStatusEffectManager(other.gameObject);
            // 叠加燃烧
            var burn = other.GetComponent<BurnStackEffect>();
            if (burn == null) burn = other.gameObject.AddComponent<BurnStackEffect>();
            burn.AddStack(_burnDps * _damageMultiplier, _burnDuration, _canCrit, _critChance, _critMult);
        }
        Destroy(gameObject);
    }

    public static BurnBullet Create(Vector2 pos, Vector2 dir, float speed, int impactDmg,
        float burnDps, float burnDuration, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        var go = new GameObject("BurnBullet");
        go.transform.position = pos; go.tag = "Untagged";
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite(); sr.color = new Color(1f, 0.4f, 0f); sr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * 0.2f; // 很小的球
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.25f;
        var b = go.AddComponent<BurnBullet>();
        b.Setup(speed, impactDmg, burnDps, burnDuration, dmgMult, canCrit, critChance, critMult);
        b.SetDirection(dir);
        return b;
    }
}

/// <summary>
/// 燃烧叠加效果 — 层数越高，tick 间隔越短（最低 0.2 秒）
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
    private SpriteRenderer _sr;
    private Color _originalColor;
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
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _originalColor = _sr.color;
        _lastTick = Time.time;
    }

    private void Update()
    {
        if (Time.time > _endTime || _stacks <= 0) { _stacks = 0; if (_sr != null) _sr.color = _originalColor; Destroy(this); return; }

        // 视觉：橙色闪烁
        if (_sr != null)
        {
            float pulse = Mathf.Sin(Time.time * 10f) * 0.3f;
            _sr.color = Color.Lerp(_originalColor, new Color(1f, 0.5f + pulse, 0f), 0.6f);
        }

        // 层数越高 tick 间隔越短（最低 0.2 秒）
        float tickInterval = Mathf.Max(0.2f, 1.0f / _stacks);
        _tickAccumulator += Time.deltaTime;

        if (_tickAccumulator >= tickInterval)
        {
            _tickAccumulator -= tickInterval;
            if (_damageable != null && _damageable.CurrentHp > 0)
            {
                float dmg = _baseDps * tickInterval;
                if (_canCrit && Random.value < _critChance) dmg *= _critMult;
                _damageable.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(dmg)), new Color(1f, 0.5f, 0f)); // 燃烧橙色
            }
        }
    }

    private void OnDestroy()
    {
        if (_sr != null) _sr.color = _originalColor;
    }
}

// ═══════════════════════════════════════════════════════════════
// 霜冻子弹
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// 霜冻子弹 — 快速子弹，冰冻敌人 + 永久减速 + 霜伤
/// </summary>
public class FrostBullet : MonoBehaviour
{
    private float _speed = 20f;
    private float _lifetime = 2f;
    private int _impactDamage = 6;
    private float _frostDps = 2f;
    private float _freezeDuration = 1f;
    private float _slowPercent = 0.3f;
    private float _damageMultiplier = 1f;
    private bool _canCrit; private float _critChance, _critMult;
    private Vector2 _direction;
    private float _spawnTime;

    public void Setup(float speed, int impactDmg, float frostDps, float freezeDuration,
        float slowPercent, float dmgMult, bool canCrit, float critChance, float critMult)
    {
        _speed = speed; _impactDamage = impactDmg; _frostDps = frostDps;
        _freezeDuration = freezeDuration; _slowPercent = slowPercent;
        _damageMultiplier = dmgMult;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
    }

    public void SetDirection(Vector2 dir) { _direction = dir.normalized; }

    private void Start() { _spawnTime = Time.time; }
    private void Update() { if (Time.time - _spawnTime > _lifetime) Destroy(gameObject); }
    private void FixedUpdate() { GetComponent<Rigidbody2D>().linearVelocity = _direction * _speed; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        var dmg = other.GetComponent<Damageable>();
        if (dmg != null && dmg.CurrentHp > 0)
        {
            dmg.TakeDamage(Mathf.RoundToInt(_impactDamage * _damageMultiplier));
            // 确保敌人有 StatusEffectManager（诅咒传播需要死亡事件注册）
            DotBulletHelper.EnsureStatusEffectManager(other.gameObject);
            // 冰冻效果
            var frost = other.GetComponent<FrostEffect>();
            if (frost == null) frost = other.gameObject.AddComponent<FrostEffect>();
            frost.ApplyFreeze(_freezeDuration, _slowPercent, _frostDps * _damageMultiplier, _canCrit, _critChance, _critMult);
        }
        Destroy(gameObject);
    }

    public static FrostBullet Create(Vector2 pos, Vector2 dir, float speed, int impactDmg,
        float frostDps, float freezeDuration, float slowPercent, float dmgMult,
        bool canCrit, float critChance, float critMult)
    {
        var go = new GameObject("FrostBullet");
        go.transform.position = pos; go.tag = "Untagged";
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.Get(); sr.color = new Color(0.3f, 0.6f, 1f); sr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * 0.5f;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(0.4f, 0.2f);
        var b = go.AddComponent<FrostBullet>();
        b.Setup(speed, impactDmg, frostDps, freezeDuration, slowPercent, dmgMult, canCrit, critChance, critMult);
        b.SetDirection(dir);
        return b;
    }
}

/// <summary>
/// 霜冻效果 — 冰冻 + 永久减速 + 每 2 秒霜伤
/// </summary>
public class FrostEffect : MonoBehaviour
{
    private float _freezeEndTime;
    public float _slowPercent;
    public float _frostDps;
    public bool _canCrit; public float _critChance, _critMult;
    private bool _frozen;
    private EnemyBase _enemyBase;
    private Damageable _damageable;
    private float _originalSpeed;
    private float _lastFrostTick;
    private SpriteRenderer _sr;
    private Color _originalColor;
    private bool _speedCaptured; // 防止重复捕获原始速度

    public void ApplyFreeze(float freezeDuration, float slowPercent, float frostDps,
        bool canCrit, float critChance, float critMult)
    {
        _freezeEndTime = Time.time + freezeDuration;
        _slowPercent = slowPercent;
        _frostDps = frostDps;
        _canCrit = canCrit; _critChance = critChance; _critMult = critMult;
        _frozen = true;

        // 缓存敌人引用并冻住
        if (_enemyBase == null) _enemyBase = GetComponent<EnemyBase>();
        if (_enemyBase != null)
        {
            if (!_speedCaptured)
            {
                _originalSpeed = _enemyBase.MoveSpeed;
                _speedCaptured = true;
            }
            _enemyBase.MoveSpeed = 0f;
        }
    }

    /// <summary>
    /// 对象池回收时重置状态（关键修复：防止跨局残留）
    /// </summary>
    private void OnEnable()
    {
        // 重置为初始状态，确保对象池恢复的敌人不被永久减速
        RestoreSpeed();
        _frozen = false;
        _freezeEndTime = 0f;
        _speedCaptured = false;
        _slowPercent = 0f;
    }

    private void Start()
    {
        if (_enemyBase == null) _enemyBase = GetComponent<EnemyBase>();
        _damageable = GetComponent<Damageable>();
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _originalColor = _sr.color;
        _lastFrostTick = Time.time;
    }

    private void Update()
    {
        if (_damageable == null || _damageable.CurrentHp <= 0) { Cleanup(); return; }

        // 冰冻阶段
        if (_frozen && Time.time < _freezeEndTime)
        {
            if (_sr != null) _sr.color = new Color(0.3f, 0.5f, 1f); // 冰蓝色
            if (_enemyBase != null) _enemyBase.MoveSpeed = 0f;
        }
        else if (_frozen)
        {
            // 解冻：永久减速
            _frozen = false;
            if (_enemyBase != null)
            {
                _enemyBase.MoveSpeed = _originalSpeed * (1f - _slowPercent);
            }
            if (_sr != null) _sr.color = Color.Lerp(_originalColor, new Color(0.5f, 0.7f, 1f), 0.3f);
        }

        // 每 2 秒霜伤
        if (Time.time - _lastFrostTick >= 2f)
        {
            _lastFrostTick = Time.time;
            float dmg = _frostDps * 2f;
            if (_canCrit && Random.value < _critChance) dmg *= _critMult;
            if (_damageable != null && _damageable.CurrentHp > 0)
                _damageable.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(dmg)), new Color(0.4f, 0.7f, 1f)); // 霜冻冰蓝色
        }
    }

    private void Cleanup()
    {
        RestoreSpeed();
        if (_sr != null) _sr.color = _originalColor;
        Destroy(this);
    }

    /// <summary>
    /// 恢复敌人原始速度（防止永久减速残留）
    /// </summary>
    private void RestoreSpeed()
    {
        if (_enemyBase != null && _speedCaptured)
        {
            _enemyBase.MoveSpeed = _originalSpeed;
            _speedCaptured = false;
        }
    }

    private void OnDisable()
    {
        RestoreSpeed();
    }

    private void OnDestroy()
    {
        RestoreSpeed();
        if (_sr != null) _sr.color = _originalColor;
    }
}

// ═══════════════════════════════════════════════════════════════
// 共享 Sprite 缓存
// ═══════════════════════════════════════════════════════════════

public static class DotSpriteCache
{
    private static Sprite _cachedSprite;
    private static Sprite _cachedCircle;

    public static Sprite Get()
    {
        if (_cachedSprite != null) return _cachedSprite;
        int w = 16, h = 8;
        var tex = new Texture2D(w, h);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                float cx = (x - 7.5f) / 7.5f;
                float cy = (y - 3.5f) / 3.5f;
                tex.SetPixel(x, y, cx * cx + cy * cy <= 1f ? Color.white : new Color(0, 0, 0, 0));
            }
        tex.Apply();
        _cachedSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 10f);
        return _cachedSprite;
    }

    public static Sprite CircleSprite()
    {
        if (_cachedCircle != null) return _cachedCircle;
        int size = 64;
        var tex = new Texture2D(size, size);
        float center = size / 2f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                tex.SetPixel(x, y, dist <= 1f ? new Color(1, 1, 1, 1f - dist) : new Color(0, 0, 0, 0));
            }
        tex.Apply();
        _cachedCircle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 10f);
        return _cachedCircle;
    }
}