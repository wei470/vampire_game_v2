using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 环境区域组件，实现 4 种类型的环境效果区域。
/// 对应 Python: game/environment_zones.py
/// 
/// 4 种类型：
/// - Slow：减速区域（玩家移动速度降低 50%）
/// - Damage：伤害区域（每秒造成 5 点伤害）
/// - Heal：治疗区域（每秒恢复 3 点 HP）
/// - Lava：岩浆区域（每秒造成 15 点高伤害）
/// 
/// 使用方式：挂载到有 Collider2D(isTrigger) 的 GameObject 上
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EnvironmentZone : MonoBehaviour
{
    [Header("区域类型")]
    [SerializeField] private MapThemeData.EnvironmentZoneType _zoneType = MapThemeData.EnvironmentZoneType.Slow;

    [Header("区域属性")]
    [Tooltip("效果强度（减速比例 / 每秒伤害 / 每秒治疗量）")]
    [SerializeField] private float _effectStrength = 5f;

    [Tooltip("效果间隔（秒），伤害/治疗类区域的 tick 间隔")]
    [SerializeField] private float _tickInterval = 1f;

    [Tooltip("区域持续时间（秒），0=永久")]
    [SerializeField] private float _duration = 0f;

    [Header("视觉配置")]
    [Tooltip("区域颜色")]
    [SerializeField] private Color _zoneColor = Color.white;

    [Tooltip("区域不透明度")]
    [Range(0f, 1f)]
    [SerializeField] private float _zoneAlpha = 0.3f;

    [Tooltip("是否显示脉冲动画")]
    [SerializeField] private bool _pulseAnimation = true;

    [Tooltip("脉冲速度")]
    [SerializeField] private float _pulseSpeed = 2f;

    [Tooltip("脉冲最小缩放")]
    [SerializeField] private float _pulseMinScale = 0.95f;

    // 运行时状态
    private float _spawnTime;
    private float _lastTickTime;
    private SpriteRenderer _spriteRenderer;
    private Collider2D _collider;

    // 当前在区域内的实体
    private HashSet<Collider2D> _entitiesInZone = new HashSet<Collider2D>();

    /// <summary>
    /// 区域类型
    /// </summary>
    public MapThemeData.EnvironmentZoneType ZoneType => _zoneType;

    /// <summary>
    /// 效果强度
    /// </summary>
    public float EffectStrength => _effectStrength;

    /// <summary>
    /// 区域持续时间
    /// </summary>
    public float Duration => _duration;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();

        // 确保 Collider2D 是触发器
        if (_collider != null)
        {
            _collider.isTrigger = true;
        }
    }

    private void Start()
    {
        _spawnTime = Time.time;
        _lastTickTime = Time.time;

        // 初始化视觉效果
        InitializeVisuals();
    }

    private void Update()
    {
        // 检查持续时间
        if (_duration > 0 && Time.time - _spawnTime > _duration)
        {
            Destroy(gameObject);
            return;
        }

        // 脉冲动画
        if (_pulseAnimation && _spriteRenderer != null)
        {
            float pulse = Mathf.Lerp(_pulseMinScale, 1f, (Mathf.Sin(Time.time * _pulseSpeed) + 1f) * 0.5f);
            transform.localScale = Vector3.one * pulse;
        }

        // 处理持续效果（伤害/治疗类区域）
        if (Time.time - _lastTickTime >= _tickInterval)
        {
            _lastTickTime = Time.time;
            ProcessTickEffects();
        }
    }

    /// <summary>
    /// 初始化视觉效果
    /// </summary>
    private void InitializeVisuals()
    {
        // 根据区域类型设置默认颜色
        switch (_zoneType)
        {
            case MapThemeData.EnvironmentZoneType.Slow:
                _zoneColor = new Color(0.5f, 0.8f, 1f, _zoneAlpha); // 淡蓝色
                _effectStrength = _effectStrength <= 0 ? 0.5f : _effectStrength;
                break;
            case MapThemeData.EnvironmentZoneType.Damage:
                _zoneColor = new Color(1f, 0.3f, 0.3f, _zoneAlpha); // 红色
                _effectStrength = _effectStrength <= 0 ? 5f : _effectStrength;
                break;
            case MapThemeData.EnvironmentZoneType.Heal:
                _zoneColor = new Color(0.3f, 1f, 0.3f, _zoneAlpha); // 绿色
                _effectStrength = _effectStrength <= 0 ? 3f : _effectStrength;
                break;
            case MapThemeData.EnvironmentZoneType.Lava:
                _zoneColor = new Color(1f, 0.5f, 0f, _zoneAlpha); // 橙红色
                _effectStrength = _effectStrength <= 0 ? 15f : _effectStrength;
                break;
        }

        // 如果没有 SpriteRenderer，添加一个
        if (_spriteRenderer == null)
        {
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // 创建默认区域 Sprite
        _spriteRenderer.sprite = CreateCircleSprite();
        _spriteRenderer.color = _zoneColor;
        _spriteRenderer.sortingOrder = -50;

        // 设置默认缩放
        if (transform.localScale == Vector3.one)
        {
            transform.localScale = Vector3.one * 4f; // 默认半径 2
        }
    }

    /// <summary>
    /// 处理持续效果（每 tick 对区域内实体生效）
    /// </summary>
    private void ProcessTickEffects()
    {
        // 清理已销毁的实体
        _entitiesInZone.RemoveWhere(e => e == null);

        foreach (var col in _entitiesInZone)
        {
            if (col == null) continue;

            switch (_zoneType)
            {
                case MapThemeData.EnvironmentZoneType.Damage:
                    ApplyDamage(col);
                    break;
                case MapThemeData.EnvironmentZoneType.Heal:
                    ApplyHeal(col);
                    break;
                case MapThemeData.EnvironmentZoneType.Lava:
                    ApplyLavaDamage(col);
                    break;
                // Slow 效果在 Enter/Exit 时处理，不需要 tick
            }
        }
    }

    /// <summary>
    /// 进入区域
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsAffectedEntity(other)) return;

        _entitiesInZone.Add(other);

        // 立即应用进入效果
        switch (_zoneType)
        {
            case MapThemeData.EnvironmentZoneType.Slow:
                ApplySlow(other, true);
                break;
        }

        DebugHelper.Log($"[EnvironmentZone] {other.name} entered {_zoneType} zone");
    }

    /// <summary>
    /// 离开区域
    /// </summary>
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!_entitiesInZone.Contains(other)) return;

        _entitiesInZone.Remove(other);

        // 移除离开效果
        switch (_zoneType)
        {
            case MapThemeData.EnvironmentZoneType.Slow:
                ApplySlow(other, false);
                break;
        }

        DebugHelper.Log($"[EnvironmentZone] {other.name} exited {_zoneType} zone");
    }

    /// <summary>
    /// 检查实体是否受影响
    /// </summary>
    private bool IsAffectedEntity(Collider2D other)
    {
        // 只影响玩家
        return other.CompareTag("Player");
    }

    /// <summary>
    /// 应用/移除减速效果
    /// </summary>
    private void ApplySlow(Collider2D entity, bool apply)
    {
        var player = entity.GetComponent<PlayerController>();
        if (player == null) return;

        if (apply)
        {
            // 减速 _effectStrength 比例（默认 50%）
            float slowMultiplier = 1f - _effectStrength;
            player.MoveSpeed *= slowMultiplier;
            DebugHelper.Log($"[EnvironmentZone] Applied slow to {entity.name}, speed reduced by {_effectStrength * 100}%");
        }
        else
        {
            // 恢复速度（通过重新计算）
            float slowMultiplier = 1f - _effectStrength;
            player.MoveSpeed /= slowMultiplier;
            DebugHelper.Log($"[EnvironmentZone] Removed slow from {entity.name}");
        }
    }

    /// <summary>
    /// 应用伤害效果
    /// </summary>
    private void ApplyDamage(Collider2D entity)
    {
        var damageable = entity.GetComponent<Damageable>();
        if (damageable == null || damageable.CurrentHp <= 0) return;

        int damage = Mathf.RoundToInt(_effectStrength * _tickInterval);
        damageable.TakeDamage(Mathf.Max(1, damage));
    }

    /// <summary>
    /// 应用治疗效果
    /// </summary>
    private void ApplyHeal(Collider2D entity)
    {
        var damageable = entity.GetComponent<Damageable>();
        if (damageable == null || damageable.CurrentHp <= 0) return;

        int heal = Mathf.RoundToInt(_effectStrength * _tickInterval);
        damageable.Heal(heal);
    }

    /// <summary>
    /// 应用岩浆伤害（高伤害）
    /// </summary>
    private void ApplyLavaDamage(Collider2D entity)
    {
        var damageable = entity.GetComponent<Damageable>();
        if (damageable == null || damageable.CurrentHp <= 0) return;

        int damage = Mathf.RoundToInt(_effectStrength * _tickInterval);
        damageable.TakeDamage(Mathf.Max(1, damage));
    }

    /// <summary>
    /// 设置区域类型（用于代码创建时）
    /// </summary>
    public void SetZoneType(MapThemeData.EnvironmentZoneType type)
    {
        _zoneType = type;
        InitializeVisuals();
    }

    /// <summary>
    /// 设置效果强度
    /// </summary>
    public void SetEffectStrength(float strength)
    {
        _effectStrength = strength;
    }

    /// <summary>
    /// 设置区域半径
    /// </summary>
    public void SetRadius(float radius)
    {
        transform.localScale = Vector3.one * radius * 2f;
    }

    /// <summary>
    /// 创建默认的环境区域（代码创建备用方案）
    /// </summary>
    /// <param name="position">位置</param>
    /// <param name="type">区域类型</param>
    /// <param name="radius">半径</param>
    /// <param name="duration">持续时间，0=永久</param>
    /// <returns>创建的 EnvironmentZone 实例</returns>
    public static EnvironmentZone CreateDefault(Vector3 position, MapThemeData.EnvironmentZoneType type, float radius = 2f, float duration = 0f)
    {
        var go = new GameObject($"EnvZone_{type}");
        go.transform.position = position;

        // 添加触发器碰撞体
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f; // 单位圆，通过缩放控制实际大小

        // 添加区域组件
        var zone = go.AddComponent<EnvironmentZone>();
        zone._zoneType = type;
        zone._duration = duration;
        zone.SetRadius(radius);

        return zone;
    }

    /// <summary>
    /// 创建白色圆形 Sprite
    /// </summary>
    private static Sprite _cachedCircleSprite;
    private static Sprite CreateCircleSprite()
    {
        if (_cachedCircleSprite != null) return _cachedCircleSprite;

        int size = 32;
        var tex = new Texture2D(size, size);
        float center = size * 0.5f;
        float radius = center - 1;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    // 渐变边缘
                    float alpha = 1f - Mathf.Clamp01((dist - radius + 2f) / 2f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        _cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return _cachedCircleSprite;
    }

    private void OnDestroy()
    {
        // 清理：移除所有实体的减速效果
        if (_zoneType == MapThemeData.EnvironmentZoneType.Slow)
        {
            foreach (var col in _entitiesInZone)
            {
                if (col != null)
                {
                    ApplySlow(col, false);
                }
            }
        }
        _entitiesInZone.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        // 在 Scene 视图中显示区域范围
        switch (_zoneType)
        {
            case MapThemeData.EnvironmentZoneType.Slow:
                Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f);
                break;
            case MapThemeData.EnvironmentZoneType.Damage:
                Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.3f);
                break;
            case MapThemeData.EnvironmentZoneType.Heal:
                Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.3f);
                break;
            case MapThemeData.EnvironmentZoneType.Lava:
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                break;
        }

        float radius = transform.localScale.x * 0.5f;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}