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
    private readonly List<(GameObject enemy, float dist)> _chainCandidates = new List<(GameObject, float)>(16);
    private bool _consumed = false;
    private Rigidbody2D _cachedRb;
    private PenetrateHandler _cachedPenetrate;

    public void Setup(float speed, int impactDmg, float dmgMult)
    {
        _speed = speed; _impactDamage = impactDmg; _damageMultiplier = dmgMult;
    }
    public void SetDirection(Vector2 dir) { _direction = dir.normalized; RotateToDirection(); }
    private void RotateToDirection() { float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg; transform.rotation = Quaternion.Euler(0, 0, angle); }

    private void Awake() { _cachedRb = GetComponent<Rigidbody2D>(); _cachedPenetrate = GetComponent<PenetrateHandler>(); }
    private void OnEnable()
    {
        _spawnTime = Time.time;
        _consumed = false;
        _hitEnemies.Clear();
        if (_cachedRb == null) _cachedRb = GetComponent<Rigidbody2D>();
        if (_cachedRb != null) _cachedRb.linearVelocity = Vector2.zero;
        _cachedPenetrate = GetComponent<PenetrateHandler>();
    }
    private void Update() { if (Time.time - _spawnTime > _lifetime) DespawnSelf(); }
    private void FixedUpdate() { if (_cachedRb != null) _cachedRb.linearVelocity = _direction * _speed; }

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
        if (_cachedPenetrate == null) _cachedPenetrate = GetComponent<PenetrateHandler>();
        if (_cachedPenetrate != null && _cachedPenetrate.TryPenetrate(other)) { _consumed = false; return; }
        var ricochet = GetComponent<RicochetHandler>();
        if (ricochet != null && ricochet.TryRicochet(transform.position, other)) { _consumed = false; return; }
        DespawnSelf();
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
        var candidates = _chainCandidates;
        candidates.Clear();

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
        CombatManager.CreateExplosionEffect(from, 0.15f, new Color(0.5f, 0.8f, 1f, 0.9f), 0.3f);
        var lineObj = VFXPool.Get("ChainLine");
        lineObj.transform.position = from;
        var lr = lineObj.GetComponent<LineRenderer>();
        if (lr == null) lr = lineObj.AddComponent<LineRenderer>();
        lr.material = MaterialCache.GetDefault();
        lr.startColor = new Color(0.5f, 0.8f, 1f, 0.9f);
        lr.endColor = new Color(0.3f, 0.6f, 1f, 0f);
        lr.startWidth = 0.15f;
        lr.endWidth = 0.05f;
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.sortingOrder = 20;
        VFXPool.Return(lineObj, 0.3f);
    }

    private void DespawnSelf()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.DOT_LIGHTNING_BULLET);
    }

    private static GameObject BuildTemplate()
    {
        var go = new GameObject("LightningBullet");
        go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.Get();
        sr.color = new Color(0.3f, 0.8f, 1f);
        sr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * 0.5f;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(0.4f, 0.2f);
        DotBulletVisualEffects.AttachLightningTrail(go);
        go.AddComponent<LightningBullet>();
        return go;
    }

    public static LightningBullet Create(Vector2 pos, Vector2 dir, float speed, int impactDmg, float dmgMult)
    {
        var go = PoolHelper.SpawnOrFallback(PoolHelper.DOT_LIGHTNING_BULLET, BuildTemplate,
            () => {
                var g = new GameObject("LightningBullet");
                g.tag = "Untagged";
                PhysicsLayerSetup.SetAsBullet(g);
                var sr = g.AddComponent<SpriteRenderer>();
                sr.sprite = DotSpriteCache.Get();
                sr.color = new Color(0.3f, 0.8f, 1f);
                sr.sortingOrder = 15;
                g.transform.localScale = Vector3.one * 0.5f;
                g.AddComponent<Rigidbody2D>().gravityScale = 0f;
                var col = g.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(0.4f, 0.2f);
                DotBulletVisualEffects.AttachLightningTrail(g);
                g.AddComponent<LightningBullet>();
                return g;
            }, pos);

        var b = go.GetComponent<LightningBullet>();
        if (b == null) b = go.AddComponent<LightningBullet>();
        b.Setup(speed, impactDmg, dmgMult);
        b.SetDirection(dir);
        b._cachedPenetrate = go.GetComponent<PenetrateHandler>();
        return b;
    }
}