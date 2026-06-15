using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 球状闪电 — 元素反应"球状闪电"（风 × 雷电）生成的雷电球。
///
/// 触发条件：风子弹命中有雷电层数的敌人
/// 消耗：1 层雷电 + 全部风层数
/// 效果：
///   - 随机方向移动，速度正常
///   - 存在 1 秒
///   - 接触敌人：立刻添加 1 层雷电 + 触发 0.33s 静电
///   - 同一敌人每 0.5s 最多触发一次
/// </summary>
public class BallLightning : MonoBehaviour
{
    private float _speed = 10f;
    private float _lifetime = 1f;
    private float _spawnTime;
    private Vector2 _direction;
    private Rigidbody2D _rb;
    private HashSet<GameObject> _hitCooldown = new HashSet<GameObject>();
    private Dictionary<GameObject, float> _lastHitTime = new Dictionary<GameObject, float>();
    private const float HIT_COOLDOWN = 0.5f;
    private const float STUN_DURATION = 0.33f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        _spawnTime = Time.time;
        _hitCooldown.Clear();
        _lastHitTime.Clear();
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        if (_rb != null) _rb.linearVelocity = _direction * _speed;
    }

    private void Update()
    {
        if (Time.time - _spawnTime > _lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void FixedUpdate()
    {
        if (_rb != null) _rb.linearVelocity = _direction * _speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        var dmg = other.GetComponent<Damageable>();
        if (dmg == null || dmg.CurrentHp <= 0) return;

        // 同一敌人 0.5s 冷却
        if (_lastHitTime.TryGetValue(other.gameObject, out float lastTime))
        {
            if (Time.time - lastTime < HIT_COOLDOWN) return;
        }
        _lastHitTime[other.gameObject] = Time.time;

        // 添加 1 层雷电
        var staticEffect = other.GetComponent<StaticStackEffect>();
        if (staticEffect == null)
            staticEffect = other.gameObject.AddComponent<StaticStackEffect>();

        // 确保 StatusEffectManager 存在
        DotBulletHelper.EnsureStatusEffectManager(other.gameObject);

        // 施加雷电层 + 眩晕
        staticEffect.RegisterHit(STUN_DURATION);

        // 视觉特效
        CombatManager.CreateExplosionEffect(other.transform.position, 0.2f,
            new Color(0.3f, 0.8f, 1f, 0.6f), 0.15f);
    }

    public void SetDirection(Vector2 dir)
    {
        _direction = dir.normalized;
    }

    /// <summary>
    /// 创建球状闪电
    /// </summary>
    public static BallLightning Create(Vector2 pos, Vector2 dir)
    {
        var go = new GameObject("BallLightning");
        go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSpriteCache.CircleSprite();
        sr.color = new Color(0.3f, 0.8f, 1f, 0.8f);
        sr.sortingOrder = 16;
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.5f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        // 拖尾
        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.3f;
        trail.startWidth = 0.3f;
        trail.endWidth = 0.05f;
        trail.material = MaterialCache.GetDefault();
        trail.startColor = new Color(0.3f, 0.8f, 1f, 0.7f);
        trail.endColor = new Color(0.3f, 0.8f, 1f, 0f);
        trail.numCapVertices = 2;
        trail.minVertexDistance = 0.03f;
        trail.sortingOrder = 15;

        // 光晕
        var glow = new GameObject("BallGlow");
        glow.transform.SetParent(go.transform);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localScale = Vector3.one * 2f;
        var glowSr = glow.AddComponent<SpriteRenderer>();
        glowSr.sprite = DotSpriteCache.CircleSprite();
        glowSr.color = new Color(0.3f, 0.8f, 1f, 0.2f);
        glowSr.sortingOrder = 14;

        var ball = go.AddComponent<BallLightning>();
        ball._speed = 10f;
        ball._lifetime = 1f;
        ball.SetDirection(dir);

        // 1 秒后自动销毁
        Destroy(go, 1.5f);

        return ball;
    }
}
