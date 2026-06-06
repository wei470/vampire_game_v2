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

    public void Setup(float speed, int impactDmg, float dmgMult)
    {
        _speed = speed; _impactDamage = impactDmg; _damageMultiplier = dmgMult;
    }
    public void SetDirection(Vector2 dir) { _direction = dir.normalized; }
    private void Start() { _spawnTime = Time.time; }
    private void Update() { if (Time.time - _spawnTime > _lifetime) Destroy(gameObject); }
    private Rigidbody2D _cachedRb;
    private void Awake() { _cachedRb = GetComponent<Rigidbody2D>(); }
    private void FixedUpdate() { _cachedRb.linearVelocity = _direction * _speed; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        var dmg = other.GetComponent<Damageable>();
        if (dmg != null && dmg.CurrentHp > 0)
        {
            dmg.TakeDamage(Mathf.RoundToInt(_impactDamage * _damageMultiplier));
            DotBulletHelper.EnsureStatusEffectManager(other.gameObject);
            ApplyStaticToEnemy(other.gameObject);
            _hitEnemies.Add(other.gameObject);
            ChainLightning(other.gameObject);
        }
        var penetrate = GetComponent<PenetrateHandler>();
        if (penetrate != null && penetrate.TryPenetrate(other)) return;
        var ricochet = GetComponent<RicochetHandler>();
        if (ricochet != null && ricochet.TryRicochet(transform.position, other)) return;
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

            var dmg = target.GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp > 0)
            {
                float chainDmgMult = _damageMultiplier * Mathf.Pow(0.5f, chained + 1);
                dmg.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(_impactDamage * chainDmgMult)));
            }

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
/// 静电效果组件 — 挂在敌人身上，管理连锁层数和定时静电伤害
/// 每层降低0.1秒触发间隔，初始5秒，最低2秒
/// </summary>
public class StaticStackEffect : MonoBehaviour
{
    private int _stackCount = 0;
    private float _baseInterval = 5.0f;
    private float _stackReduction = 0.1f;
    private float _minInterval = 2.0f;
    private float _lastTickTime;
    private float _stunEndTime;
    private EnemyBase _enemyBase;
    private float _originalSpeed;
    private bool _speedCaptured;
    private Damageable _damageable;
    private SpriteRenderer _sr;
    private Color _originalColor;

    public int StackCount => _stackCount;

    public void AddStack()
    {
        _stackCount++;
        _lastTickTime = Time.time;
        ApplyStun(0.5f);
        DebugHelper.Log($"[StaticStackEffect] Stack added! Total={_stackCount}, Interval={GetInterval():F1}s");
    }

    public float GetInterval()
    {
        return Mathf.Max(_minInterval, _baseInterval - _stackCount * _stackReduction);
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
            _enemyBase.MoveSpeed = _originalSpeed;
            _speedCaptured = false;
            if (_sr != null) _sr.color = _originalColor;
        }

        float interval = GetInterval();
        if (Time.time - _lastTickTime >= interval)
        {
            _lastTickTime = Time.time;
            TriggerStaticDamage();
        }
    }

    private void TriggerStaticDamage()
    {
        if (_damageable == null || _damageable.CurrentHp <= 0) return;
        int damage = Mathf.Max(1, _stackCount * 2);
        _damageable.TakeDamage(damage, new Color(0.3f, 0.8f, 1f));
        CombatManager.CreateExplosionEffect(transform.position, 1f, new Color(0.4f, 0.8f, 1f), 0.3f);
        DamagePopup.Create(transform.position, damage, new Color(0.4f, 0.8f, 1f), false);
        DebugHelper.Log($"[StaticStackEffect] Static discharge! {damage} damage, stacks={_stackCount}");
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