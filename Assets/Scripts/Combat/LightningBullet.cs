using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 雷电子弹 — 命中敌人后连锁附近最多3个敌人
/// 从 DotProjectile.cs 拆分而来
/// </summary>
public class LightningBullet : MonoBehaviour
{
    private float _speed = 16f;
    private float _lifetime = 2f;
    private int _impactDamage = 5;
    private float _damageMultiplier = 1f;
    private Vector2 _direction;
    private float _spawnTime;
    private int _maxChainCount = 3;
    private float _chainRadius = 8f;
    private HashSet<GameObject> _hitEnemies = new HashSet<GameObject>();
    private bool _consumed = false;

    public void Setup(float speed, int impactDmg, float dmgMult)
    {
        _speed = speed; _impactDamage = impactDmg; _damageMultiplier = dmgMult;
    }
    public void SetDirection(Vector2 dir) { _direction = dir.normalized; RotateToDirection(); }
    private void RotateToDirection() { float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg; transform.rotation = Quaternion.Euler(0, 0, angle); }
    private void Start() { _spawnTime = Time.time; }
    private void Update() { if (Time.time - _spawnTime > _lifetime) Destroy(gameObject); }
    private Rigidbody2D _cachedRb;
    private void Awake() { _cachedRb = GetComponent<Rigidbody2D>(); }
    private void FixedUpdate() { _cachedRb.linearVelocity = _direction * _speed; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_consumed) return;
        if (!other.CompareTag("Enemy")) return;
        var dmg = other.GetComponent<Damageable>();
        if (dmg != null && dmg.CurrentHp > 0)
        {
            // 雷电不造成直接伤害，只叠静电层数
            DotBulletHelper.EnsureStatusEffectManager(other.gameObject);
            ApplyStaticToEnemy(other.gameObject);
            _hitEnemies.Add(other.gameObject);
            ChainLightning(other.gameObject);
        }
        _consumed = true;
        var penetrate = GetComponent<PenetrateHandler>();
        if (penetrate != null && penetrate.TryPenetrate(other)) { _consumed = false; return; }
        var ricochet = GetComponent<RicochetHandler>();
        if (ricochet != null && ricochet.TryRicochet(transform.position, other)) { _consumed = false; return; }
        Destroy(gameObject);
    }

    private void ApplyStaticToEnemy(GameObject enemy)
    {
        var staticEffect = enemy.GetComponent<StaticStackEffect>();
        if (staticEffect == null)
            staticEffect = enemy.AddComponent<StaticStackEffect>();
        staticEffect.AddStack();
    }

    private void ChainLightning(GameObject origin)
    {
        var spawnMgr = GameReferences.SpawnManager;
        IReadOnlyList<GameObject> enemies = spawnMgr != null ? spawnMgr.ActiveEnemies : null;
        if (enemies == null || enemies.Count == 0) return;

        float chainRadiusSqr = _chainRadius * _chainRadius;
        Vector2 originPos = origin.transform.position;
        var candidates = new List<(GameObject enemy, float dist)>();

        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null || e == origin || !e.activeInHierarchy) continue;
            if (_hitEnemies.Contains(e)) continue;
            var d = e.GetComponent<Damageable>();
            if (d == null || d.CurrentHp <= 0) continue;
            float distSqr = ((Vector2)e.transform.position - originPos).sqrMagnitude;
            if (distSqr <= chainRadiusSqr)
                candidates.Add((e, distSqr));
        }

        candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

        int chained = 0;
        Vector2 lastPos = originPos;
        for (int i = 0; i < candidates.Count && chained < _maxChainCount; i++)
        {
            var target = candidates[i].enemy;
            if (_hitEnemies.Contains(target)) continue;
            _hitEnemies.Add(target);

            CreateChainLine(lastPos, target.transform.position);

            // 连锁只叠静电，不造成伤害
            DotBulletHelper.EnsureStatusEffectManager(target);
            ApplyStaticToEnemy(target);
            CombatManager.CreateExplosionEffect(target.transform.position, 0.8f, new Color(0.4f, 0.8f, 1f), 0.2f);

            lastPos = target.transform.position;
            chained++;
        }

        if (chained > 0)
            DebugHelper.Log($"[LightningBullet] Chained to {chained} enemies");
    }

    private void CreateChainLine(Vector2 from, Vector2 to)
    {
        var lineObj = new GameObject("ChainLine");
        lineObj.transform.position = from;
        var lr = lineObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(0.5f, 0.8f, 1f, 0.9f);
        lr.endColor = new Color(0.3f, 0.6f, 1f, 0f);
        lr.startWidth = 0.15f;
        lr.endWidth = 0.05f;
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.sortingOrder = 20;
        Destroy(lineObj, 0.3f);
    }

    public static LightningBullet Create(Vector2 pos, Vector2 dir, float speed, int impactDmg, float dmgMult)
    {
        var go = new GameObject("LightningBullet");
        go.transform.position = pos; go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.Get();
        sr.color = new Color(0.3f, 0.8f, 1f);
        sr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * 0.5f;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(0.4f, 0.2f);
        DotBulletVisualEffects.AttachLightningTrail(go);
        var b = go.AddComponent<LightningBullet>();
        b.Setup(speed, impactDmg, dmgMult);
        b.SetDirection(dir);
        return b;
    }
}

/// <summary>
/// 静电效果组件 — 挂在敌人身上，管理静电层数
///
/// 命中逻辑：
/// - 无层数时：立刻触发1秒静电 + 施加1层
/// - 有层数时：触发0.1秒静电 + 施加1层 + 重新计算冷却
///
/// 定时逻辑：
/// - 基础每5秒触发一次静电放电
/// - 每层减少0.2秒，最低2秒（15层叠满）
/// - 伤害：固定为0（纯控制效果）
/// </summary>
public class StaticStackEffect : MonoBehaviour
{
    private int _stackCount = 0;
    private const float BASE_INTERVAL = 5.0f;
    private const float STACK_REDUCTION = 0.2f;
    private const float MIN_INTERVAL = 2.0f;
    private const float STUN_DURATION_HIT = 0.1f;
    private const float STUN_DURATION_DISCHARGE = 0.5f;
    private const float STUN_DURATION_FIRST = 1.0f;
    private const int MAX_STACKS = 15;
    private float _lastTickTime;
    private float _stunEndTime;
    private EnemyBase _enemyBase;
    private float _originalSpeed;
    private bool _speedCaptured;
    private Damageable _damageable;
    private SpriteRenderer _sr;
    private Color _originalColor;

    public int StackCount => _stackCount;

    /// <summary>
    /// 添加一层静电并触发效果
    /// - 无层数时：立刻触发1秒静电 + 施加1层
    /// - 有层数时：触发0.1秒静电 + 施加1层 + 重新计算冷却
    /// </summary>
    public void AddStack()
    {
        bool isFirstStack = (_stackCount == 0);
        _stackCount = Mathf.Min(_stackCount + 1, MAX_STACKS);

        if (isFirstStack)
        {
            // 首次命中：立刻触发1秒静电
            ApplyStun(STUN_DURATION_FIRST);
        }
        else
        {
            // 已有层数：触发0.1秒静电（短暂打断）
            ApplyStun(STUN_DURATION_HIT);
        }

        // 重新计算冷却（重置定时器）
        _lastTickTime = Time.time;
        DebugHelper.Log($"[StaticStackEffect] Stack added! Total={_stackCount}, Interval={GetInterval():F1}s, First={isFirstStack}");
    }

    /// <summary>
    /// 获取当前静电触发间隔（基础5秒，每层-0.2秒，最低2秒）
    /// </summary>
    public float GetInterval()
    {
        return Mathf.Max(MIN_INTERVAL, BASE_INTERVAL - _stackCount * STACK_REDUCTION);
    }

    private void ApplyStun(float duration)
    {
        _stunEndTime = Time.time + duration;
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

    private void OnEnable()
    {
        _stackCount = 0;
        _lastTickTime = Time.time;
        _stunEndTime = 0f;
        _speedCaptured = false;
        if (_sr != null) _sr.color = _originalColor;
    }

    private void Start()
    {
        if (_enemyBase == null) _enemyBase = GetComponent<EnemyBase>();
        _damageable = GetComponent<Damageable>();
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _originalColor = _sr.color;
        _lastTickTime = Time.time;
    }

    private void Update()
    {
        if (_stackCount <= 0) return;
        if (_damageable == null || _damageable.CurrentHp <= 0) { Cleanup(); return; }

        // 硬直期间暂停移动 + 闪烁效果
        if (Time.time < _stunEndTime)
        {
            if (_enemyBase != null) _enemyBase.MoveSpeed = 0f;
            if (_sr != null)
            {
                float flash = Mathf.Sin(Time.time * 20f) > 0 ? 0.7f : 0.3f;
                _sr.color = Color.Lerp(_originalColor, new Color(0.3f, 0.8f, 1f), flash);
            }
        }
        else if (_speedCaptured && _enemyBase != null)
        {
            // 硬直结束，恢复移动速度
            _enemyBase.MoveSpeed = _originalSpeed;
            _speedCaptured = false;
            if (_sr != null) _sr.color = _originalColor;
        }

        // 定时触发静电放电
        float interval = GetInterval();
        if (Time.time - _lastTickTime >= interval)
        {
            _lastTickTime = Time.time;
            TriggerStaticDischarge();
        }
    }

    /// <summary>
    /// 定时触发放电：0.5秒静电（无伤害，纯控制）
    /// </summary>
    private void TriggerStaticDischarge()
    {
        if (_damageable == null || _damageable.CurrentHp <= 0) return;

        // 暂停行动0.5秒
        ApplyStun(STUN_DURATION_DISCHARGE);

        // 视觉特效
        CombatManager.CreateExplosionEffect(transform.position, 1f, new Color(0.4f, 0.8f, 1f), 0.3f);

        DebugHelper.Log($"[StaticStackEffect] Static discharge! stacks={_stackCount}, next in {GetInterval():F1}s");
    }

    private void Cleanup()
    {
        if (_enemyBase != null && _speedCaptured)
        {
            _enemyBase.MoveSpeed = _originalSpeed;
            _speedCaptured = false;
        }
        if (_sr != null) _sr.color = _originalColor;
        Destroy(this);
    }

    private void OnDisable()
    {
        if (_enemyBase != null && _speedCaptured)
        {
            _enemyBase.MoveSpeed = _originalSpeed;
            _speedCaptured = false;
        }
    }

    private void OnDestroy()
    {
        if (_enemyBase != null && _speedCaptured)
            _enemyBase.MoveSpeed = _originalSpeed;
        if (_sr != null) _sr.color = _originalColor;
    }
}