using UnityEngine;

/// <summary>
/// 基础子弹 - 直线飞行的玩家投射物。
/// 对应 Python: entities/projectiles.py 中的 Bullet
/// 
/// 特点：直线飞行，可穿透，可击退
/// 使用方式：继承自 Projectile 基类，由 WeaponController 实例化
/// </summary>
public class Bullet : Projectile
{
    [Header("子弹特效")]
    [SerializeField] private Color _trailColor = new Color(0.3f, 0.8f, 1f, 0.5f);
    [SerializeField] private float _trailLength = 0.3f;

    private TrailRenderer _trail;

    protected override void Awake()
    {
        base.Awake();

        // 添加拖尾效果
        _trail = gameObject.AddComponent<TrailRenderer>();
        _trail.time = _trailLength;
        _trail.startWidth = 0.15f;
        _trail.endWidth = 0f;
        _trail.material = new Material(Shader.Find("Sprites/Default"));
        _trail.startColor = _trailColor;
        _trail.endColor = new Color(_trailColor.r, _trailColor.g, _trailColor.b, 0f);
        _trail.sortingOrder = 14;
    }

    /// <summary>
    /// 创建标准子弹 GameObject（无预制体时的备用方案）
    /// </summary>
    public static Bullet CreateDefault(Vector2 position, Vector2 direction, int damage, float speed, float lifetime, int pierce)
    {
        var go = new GameObject("Bullet");
        go.transform.position = position;
        go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go); // #17 Bullet Layer

        // Sprite
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateBulletSprite();
        sr.color = new Color(0.3f, 0.8f, 1f); // 浅蓝色
        sr.sortingOrder = 15;

        // Physics
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.5f, 0.3f);

        // Bullet component
        var bullet = go.AddComponent<Bullet>();
        bullet.Setup(damage, speed, lifetime, pierce);
        bullet.SetDirection(direction);

        return bullet;
    }

    private static Sprite _cachedSprite;
    private static Sprite CreateBulletSprite()
    {
        if (_cachedSprite != null) return _cachedSprite;

        var tex = new Texture2D(16, 8);
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 8; y++)
            {
                float cx = (x - 7.5f) / 7.5f;
                float cy = (y - 3.5f) / 3.5f;
                float dist = cx * cx + cy * cy;
                tex.SetPixel(x, y, dist <= 1f ? Color.white : new Color(0, 0, 0, 0));
            }
        tex.Apply();
        _cachedSprite = Sprite.Create(tex, new Rect(0, 0, 16, 8), new Vector2(0.5f, 0.5f), 10f);
        return _cachedSprite;
    }
}