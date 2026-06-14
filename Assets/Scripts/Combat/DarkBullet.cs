using UnityEngine;

/// <summary>
/// 黑暗子弹 — 命中敌人后施加黑暗标记（非DOT伤害，纯标记效果）
/// 持有黑暗标记的敌人死亡时，自身所有DOT层数按50%效果传播给周围敌人
/// </summary>
public class DarkBullet : MonoBehaviour
{
    private float _speed = 6f;
    private float _lifetime = 5f;
    private float _markSpreadRadius = 3f;
    private float _markSpreadEfficiency = 0.5f;
    private Vector2 _direction;
    private float _spawnTime;
    private Rigidbody2D _rb;
    private PenetrateHandler _cachedPenetrate;

    private void Awake() { _rb = GetComponent<Rigidbody2D>(); }
    private void OnEnable()
    {
        _spawnTime = Time.time;
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        if (_rb != null) _rb.linearVelocity = Vector2.zero;
        _cachedPenetrate = GetComponent<PenetrateHandler>();

        // 池回收时重置颜色
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.4f, 0.1f, 0.6f);
        var trail = GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.material = MaterialCache.GetDefault();
            trail.startColor = new Color(0.4f, 0.1f, 0.6f, 0.7f);
            trail.endColor = new Color(0.4f, 0.1f, 0.6f, 0f);
            trail.Clear();
        }
    }
    private void Update() { if (Time.time - _spawnTime > _lifetime) DespawnSelf(); }
    private void FixedUpdate() { if (_rb != null) _rb.linearVelocity = _direction * _speed; }

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
        {
            darkMark = other.gameObject.AddComponent<DarkMarkEffect>();
            darkMark.Init(_markSpreadRadius, _markSpreadEfficiency);
        }
        else
        {
            darkMark.AddStack();
        }

        // 命中视觉效果
        CombatManager.CreateExplosionEffect(other.transform.position, 0.4f,
            new Color(0.4f, 0.1f, 0.6f, 0.6f), 0.3f);

        // 贯穿检查：懒加载回退（OnEnable 时 PenetrateHandler 可能还未添加）
        if (_cachedPenetrate == null) _cachedPenetrate = GetComponent<PenetrateHandler>();
        if (_cachedPenetrate != null && _cachedPenetrate.TryPenetrate(other)) return;
        DespawnSelf();
    }

    private void DespawnSelf()
    {
        PoolHelper.DespawnOrDestroy(gameObject, PoolHelper.DOT_DARK_BULLET);
    }

    private static GameObject BuildTemplate()
    {
        var go = new GameObject("DarkBullet");
        go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.Get();
        sr.color = new Color(0.4f, 0.1f, 0.6f);
        sr.sortingOrder = 15;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.6f, 0.3f);
        DotBulletVisualEffects.AttachTrail(go, new Color(0.4f, 0.1f, 0.6f, 0.7f), 0.8f, 0.05f);
        go.AddComponent<DarkBullet>();
        return go;
    }

    public static DarkBullet Create(Vector2 pos, Vector2 dir, float speed,
        float spreadRadius, float spreadEfficiency)
    {
        var go = PoolHelper.SpawnOrFallback(PoolHelper.DOT_DARK_BULLET, BuildTemplate,
            () => {
                var g = new GameObject("DarkBullet");
                g.tag = "Untagged";
                PhysicsLayerSetup.SetAsBullet(g);
                var sr = g.AddComponent<SpriteRenderer>();
                sr.sprite = DotSpriteCache.Get();
                sr.color = new Color(0.4f, 0.1f, 0.6f);
                sr.sortingOrder = 15;
                g.AddComponent<Rigidbody2D>().gravityScale = 0f;
                var col = g.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(0.6f, 0.3f);
                DotBulletVisualEffects.AttachTrail(g, new Color(0.4f, 0.1f, 0.6f, 0.7f), 0.8f, 0.05f);
                g.AddComponent<DarkBullet>();
                return g;
            }, pos, 10);

        var b = go.GetComponent<DarkBullet>();
        if (b == null) b = go.AddComponent<DarkBullet>();
        b.Setup(speed, spreadRadius, spreadEfficiency);
        b.SetDirection(dir);
        return b;
    }
}
