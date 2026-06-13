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
        _cachedPenetrate = GetComponent<PenetrateHandler>();
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

        // 穿透检查（懒加载回退）
        if (_cachedPenetrate == null) _cachedPenetrate = GetComponent<PenetrateHandler>();
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
        var go = PoolHelper.SpawnOrFallback(PoolHelper.DOT_WIND_BULLET, BuildTemplate,
            () => {
                var g = new GameObject("WindBullet");
                g.tag = "Untagged";
                PhysicsLayerSetup.SetAsBullet(g);
                var sr = g.AddComponent<SpriteRenderer>();
                sr.sprite = DotSpriteCache.Get();
                sr.color = new Color(0.7f, 0.85f, 1f);
                sr.sortingOrder = 15;
                g.transform.localScale = Vector3.one * 0.35f;
                g.AddComponent<Rigidbody2D>().gravityScale = 0f;
                var col = g.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(0.4f, 0.2f);
                DotBulletVisualEffects.AttachWindTrail(g);
                g.AddComponent<WindBullet>();
                return g;
            }, pos, 20);

        var b = go.GetComponent<WindBullet>();
        if (b == null) b = go.AddComponent<WindBullet>();
        b.Setup(speed, impactDmg, dmgMult, canCrit, critChance, critMult);
        b.SetDirection(dir);
        b._cachedPenetrate = go.GetComponent<PenetrateHandler>();
        return b;
    }
}
