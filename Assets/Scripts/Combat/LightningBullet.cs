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
    public int ExtraChainTargets { get; set; } = 0;
    private int TotalChainCount => _maxChainCount + ExtraChainTargets;
    private float _chainRadius = 8f;
    public float ExtraChainRadius { get; set; } = 0f;
    private float TotalChainRadius => _chainRadius + ExtraChainRadius;
    private HashSet<GameObject> _hitEnemies = new HashSet<GameObject>();
    private readonly List<(GameObject enemy, float dist)> _chainCandidates = new List<(GameObject, float)>(16);
    private bool _consumed = false;
    private Rigidbody2D _cachedRb;
    private PenetrateHandler _cachedPenetrate;

    // ===== 元素反应「紫电」（雷电 × 黑暗）=====
    private bool _isPurple = false;
    private const float PURPLE_SPEED_MULT = 3f;      // 变形后速度倍率
    private const float PURPLE_LIFETIME = 1.5f;       // 变形后存活时间（飞出地图用）
    private const float EXECUTE_THRESHOLD = 0.2f;     // 处决血量阈值（<20%）
    private static readonly Color PURPLE_SPRITE = new Color(0.12f, 0.02f, 0.18f);       // 近黑暗紫子弹本体
    private static readonly Color PURPLE_FX = new Color(0.5f, 0.1f, 0.8f, 0.7f);        // 紫电特效色

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
        // 紫电状态必须在池回收时重置，否则下一发会残留黑色/3x 速度/超长生命
        _isPurple = false;
        _lifetime = 2f;
        transform.localScale = Vector3.one * 0.5f;
        if (_cachedRb == null) _cachedRb = GetComponent<Rigidbody2D>();
        if (_cachedRb != null) _cachedRb.linearVelocity = Vector2.zero;
        _cachedPenetrate = GetComponent<PenetrateHandler>();

        // 重置拖尾颜色，防止池回收后残留旧颜色
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.3f, 0.8f, 1f);
        var trail = GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.material = MaterialCache.GetDefault();
            trail.startColor = new Color(0.4f, 0.8f, 1f, 0.8f);
            trail.endColor = new Color(0.2f, 0.5f, 1f, 0f);
            trail.Clear();
        }
    }
    private void Update() { if (Time.time - _spawnTime > _lifetime) DespawnSelf(); }
    private void FixedUpdate() { if (_cachedRb != null) _cachedRb.linearVelocity = _direction * _speed; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_consumed) return;
        if (!other.CompareTag("Enemy")) return;
        var dmg = other.GetComponent<Damageable>();
        bool aliveEnemy = dmg != null && dmg.CurrentHp > 0;

        if (aliveEnemy)
        {
            // 紫电贯穿模式：穿透所有单位，不消耗、不 despawn
            if (_isPurple) { HandlePurpleHit(other.gameObject, dmg); return; }

            // 命中带黑暗标记的敌人 → 触发元素反应「紫电」，子弹变形
            var darkMark = other.GetComponent<DarkMarkEffect>();
            if (darkMark != null && darkMark.IsActive)
            {
                EnterPurpleMode();
                HandlePurpleHit(other.gameObject, dmg);
                return;
            }

            // 普通雷电：不造成直接伤害，只叠静电层数 + 连锁
            DotBulletHelper.EnsureStatusEffectManager(other.gameObject);
            ApplyStaticToEnemy(other.gameObject);

            var mage = GameReferences.DotCharacterPassive as MagePassive;
            if (mage != null && mage.IsLightningExplosionPending)
            {
                mage.ConsumeLightningExplosion();
                TriggerStaticExplosion(other.transform.position);
            }

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

    // ============ 元素反应「紫电」（雷电 × 黑暗）============

    /// <summary>
    /// 切换为紫电贯穿模式（一次性）：变黑紫、3x 速度、绕过穿透/反弹无限贯穿。
    /// </summary>
    private void EnterPurpleMode()
    {
        if (_isPurple) return;
        _isPurple = true;
        _consumed = false;                 // 紫电永不被消耗
        _speed *= PURPLE_SPEED_MULT;
        _lifetime = PURPLE_LIFETIME;
        _spawnTime = Time.time;            // 从变形点重新计生命周期，确保飞出地图

        ApplyPurpleVisual();
        CombatManager.CreateExplosionEffect(transform.position, 1f, PURPLE_FX, 0.35f);
        DebugHelper.Log("[LightningBullet] 紫电触发：转入贯穿模式");
    }

    /// <summary>
    /// 紫电模式命中单个敌人：叠 1 层雷电(静电)，残血处决。
    /// 注意：紫电贯穿不叠加黑暗层数。
    /// </summary>
    private void HandlePurpleHit(GameObject enemy, Damageable dmg)
    {
        if (!_hitEnemies.Add(enemy)) return;   // 同一敌人只处理一次

        DotBulletHelper.EnsureStatusEffectManager(enemy);

        // 叠 1 层雷电（AddStack 内含触发静电 + 眩屏）
        ApplyStaticToEnemy(enemy);

        // 处决：血量 < 20% 直接斩杀（绕过护甲，走完整死亡流程 → 触发暗影传播）
        if (dmg.HpPercent < EXECUTE_THRESHOLD)
            ExecuteEnemy(enemy);
        else
            CombatManager.CreateExplosionEffect(enemy.transform.position, 0.5f, PURPLE_FX, 0.2f);
    }

    /// <summary>
    /// 处决：直接调 BaseEntity.Die()，不走 TakeDamage（避免护甲/减伤吃掉斩杀）。
    /// Die() 幂等且广播 OnDeath → 击杀奖励 + 暗影传播 + 回收一并触发。
    /// </summary>
    private void ExecuteEnemy(GameObject enemy)
    {
        var be = enemy.GetComponent<BaseEntity>();
        if (be == null || !be.Alive) return;
        CombatManager.CreateExplosionEffect(enemy.transform.position, 1.2f, PURPLE_FX, 0.4f);
        be.Die();
    }

    private void ApplyPurpleVisual()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = PURPLE_SPRITE;       // 蓝 → 近黑暗紫
        var trail = GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.startColor = new Color(0.5f, 0.05f, 0.7f, 0.9f);
            trail.endColor = new Color(0.15f, 0f, 0.25f, 0f);
            trail.Clear();
        }
        transform.localScale = Vector3.one * 0.7f;       // 略放大，强调贯穿弹
    }

    private void ApplyStaticToEnemy(GameObject enemy)
    {
        var staticEffect = enemy.GetComponent<StaticStackEffect>();
        if (staticEffect == null)
            staticEffect = enemy.AddComponent<StaticStackEffect>();
        staticEffect.AddStack();
    }

    private void TriggerStaticExplosion(Vector2 center)
    {
        var cam = Camera.main;
        if (cam == null) return;
        float halfScreen = cam.orthographicSize * cam.aspect;
        CombatManager.CreateExplosionEffect(center, halfScreen, new Color(0.5f, 0.3f, 1f, 0.4f), 0.5f);
        var spawnMgr = GameReferences.SpawnManager;
        if (spawnMgr == null) return;
        var enemies = spawnMgr.ActiveEnemies;
        if (enemies == null) return;
        float radiusSqr = halfScreen * halfScreen;
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null || !e.activeInHierarchy) continue;
            Vector2 delta = (Vector2)e.transform.position - center;
            if (delta.sqrMagnitude > radiusSqr) continue;
            DotBulletHelper.EnsureStatusEffectManager(e);
            var se = e.GetComponent<StaticStackEffect>();
            if (se == null) se = e.AddComponent<StaticStackEffect>();
            for (int s = 0; s < 3; s++) se.AddStack();
        }
    }

    private void ChainLightning(GameObject origin)
    {
        var spawnMgr = GameReferences.SpawnManager;
        IReadOnlyList<GameObject> enemies = spawnMgr != null ? spawnMgr.ActiveEnemies : null;
        if (enemies == null || enemies.Count == 0) return;

        float chainRadiusSqr = TotalChainRadius * TotalChainRadius;
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
        for (int i = 0; i < candidates.Count && chained < TotalChainCount; i++)
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
        var lineObj = new GameObject("ChainLine");
        lineObj.transform.position = from;
        var lr = lineObj.AddComponent<LineRenderer>();
        lr.material = MaterialCache.GetDefault();
        lr.startColor = new Color(0.5f, 0.8f, 1f, 0.9f);
        lr.endColor = new Color(0.3f, 0.6f, 1f, 0f);
        lr.startWidth = 0.15f;
        lr.endWidth = 0.05f;
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.sortingOrder = 20;
        lineObj.AddComponent<TimedSelfDestruct>().Setup(0.3f);
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