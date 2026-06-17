#pragma warning disable CS0414
using UnityEngine;

/// <summary>
/// 特殊掉落物组件 — 敌人死亡时有概率掉落的特殊物品。
/// 
/// 类型：
/// - HealthPotion: 回复一定百分比HP
/// - MagnetBurst: 临时吸引范围内所有掉落物
/// - DamageBoost: 临时攻击力提升
/// - ShieldOrb: 临时护盾（吸收伤害）
/// - XPMultiplier: 临时经验加成
/// 
/// 使用方式：由 KillRewarder 在击杀时按概率生成
/// </summary>
public class SpecialDrop : MonoBehaviour
{
    public enum DropType
    {
        HealthPotion,
        MagnetBurst,
        DamageBoost,
        ShieldOrb,
        XPMultiplier,
        // 临时道具系统新增
        BerserkPotion,
        GhostWalk,
        TimeSlowField,
        ElementStorm,
        InvincibleShield,
        GoldRain,
        ThornsShield,
        RevivalFlame
    }

    [Header("掉落配置")]
    [SerializeField] private DropType _dropType = DropType.HealthPotion;
    [SerializeField] private float _value = 0.25f;           // 治疗量/加成比例
    [SerializeField] private float _duration = 0f;            // 持续时间（0=立即生效）
    [SerializeField] private float _lifetime = 10f;           // 地面存活时间
    [SerializeField] private float _pickupRadius = 1.5f;      // 拾取半径

    private float _spawnTime;
    private bool _collected = false;

    /// <summary>
    /// 掉落类型
    /// </summary>
    public DropType Type => _dropType;

    /// <summary>
    /// 拾取半径（供碰撞检测使用）
    /// </summary>
    public float PickupRadius => _pickupRadius;

    private void OnEnable()
    {
        _spawnTime = Time.time;
        _collected = false;

        // 注册到屏幕外裁剪器
        if (OffScreenCuller.Instance != null)
        {
            OffScreenCuller.TrackLoot(gameObject, "SpecialDrop");
        }
    }

    private void OnDisable()
    {
        OffScreenCuller.Untrack(gameObject);
    }

    private void Update()
    {
        // 超时消失
        if (Time.time - _spawnTime > _lifetime)
        {
            PoolHelper.DespawnOrDestroy(gameObject, "SpecialDrop");
            return;
        }

        // 检测玩家拾取（简化版，不依赖碰撞器）
        if (!_collected && GameReferences.Player != null)
        {
            float dist = Vector2.Distance(transform.position, GameReferences.Player.transform.position);
            if (dist <= _pickupRadius)
            {
                Collect();
            }
        }
    }

    /// <summary>
    /// 设置掉落物属性
    /// </summary>
    public void Setup(DropType type, float value, float duration)
    {
        _dropType = type;
        _value = value;
        _duration = duration;
    }

    /// <summary>
    /// 拾取掉落物
    /// </summary>
    private void Collect()
    {
        if (_collected) return;
        _collected = true;

        var player = GameReferences.Player;
        if (player == null) { PoolHelper.DespawnOrDestroy(gameObject, "SpecialDrop"); return; }

        var dmg = player.Damageable;
        switch (_dropType)
        {
            case DropType.HealthPotion:
                if (dmg != null)
                {
                    int healAmount = Mathf.RoundToInt(dmg.MaxHp * _value);
                    dmg.Heal(healAmount);
                    DebugHelper.Log($"[SpecialDrop] Health Potion: healed {healAmount} HP");
                }
                break;

            case DropType.MagnetBurst:
                // 吸引附近掉落物到玩家位置
                AttractNearbyLoot();
                DebugHelper.Log("[SpecialDrop] Magnet Burst: attracting loot!");
                break;

            case DropType.DamageBoost:
                // 通过 CombatManager 临时加成
                DebugHelper.Log($"[SpecialDrop] Damage Boost: +{_value:P0} for {_duration}s");
                break;

            case DropType.ShieldOrb:
                // Armor system removed — shield orb grants temporary invincibility instead
                var buffSys = player.GetComponent<TemporaryBuffSystem>();
                if (buffSys == null)
                    buffSys = player.gameObject.AddComponent<TemporaryBuffSystem>();
                buffSys.AddBuff(TemporaryBuffSystem.BuffType.InvincibleShield, _duration > 0 ? _duration : 3f);
                DebugHelper.Log($"[SpecialDrop] Shield Orb: invincibility for {(_duration > 0 ? _duration : 3f)}s");
                break;

            case DropType.XPMultiplier:
                DebugHelper.Log($"[SpecialDrop] XP Multiplier: x{_value} for {_duration}s");
                break;

            // 临时道具系统 — 委托给 TemporaryBuffSystem
            case DropType.BerserkPotion:
            case DropType.GhostWalk:
            case DropType.TimeSlowField:
            case DropType.ElementStorm:
            case DropType.InvincibleShield:
            case DropType.GoldRain:
            case DropType.ThornsShield:
            case DropType.RevivalFlame:
                var buffSystem = player.GetComponent<TemporaryBuffSystem>();
                if (buffSystem == null)
                    buffSystem = player.gameObject.AddComponent<TemporaryBuffSystem>();
                var buffType = MapDropToBuff(_dropType);
                buffSystem.AddBuff(buffType, _duration, _value);
                break;
        }

        PoolHelper.DespawnOrDestroy(gameObject, "SpecialDrop");
    }

    /// <summary>
    /// 吸引附近掉落物
    /// </summary>
    private void AttractNearbyLoot()
    {
        var player = GameReferences.Player;
        if (player == null) return;

        // 找到附近的 XPGem 和 Coin，让它们加速飞向玩家
        var gems = FindObjectsByType<XPGem>();
        var coins = FindObjectsByType<Coin>();
        float attractRange = 15f;
        Vector3 playerPos = player.transform.position;

        foreach (var gem in gems)
        {
            if (gem == null) continue;
            float dist = Vector2.Distance(gem.transform.position, playerPos);
            if (dist <= attractRange)
            {
                var rb = gem.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 dir = (playerPos - gem.transform.position).normalized;
                    rb.linearVelocity = dir * 20f;
                }
            }
        }

        foreach (var coin in coins)
        {
            if (coin == null) continue;
            float dist = Vector2.Distance(coin.transform.position, playerPos);
            if (dist <= attractRange)
            {
                var rb = coin.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 dir = (playerPos - coin.transform.position).normalized;
                    rb.linearVelocity = dir * 20f;
                }
            }
        }
    }

    /// <summary>
    /// 将DropType映射到TemporaryBuffSystem.BuffType
    /// </summary>
    private static TemporaryBuffSystem.BuffType MapDropToBuff(DropType dropType)
    {
        switch (dropType)
        {
            case DropType.BerserkPotion:    return TemporaryBuffSystem.BuffType.BerserkPotion;
            case DropType.GhostWalk:        return TemporaryBuffSystem.BuffType.GhostWalk;
            case DropType.TimeSlowField:    return TemporaryBuffSystem.BuffType.TimeSlowField;
            case DropType.ElementStorm:     return TemporaryBuffSystem.BuffType.ElementStorm;
            case DropType.InvincibleShield: return TemporaryBuffSystem.BuffType.InvincibleShield;
            case DropType.GoldRain:         return TemporaryBuffSystem.BuffType.GoldRain;
            case DropType.ThornsShield:     return TemporaryBuffSystem.BuffType.ThornsShield;
            case DropType.RevivalFlame:     return TemporaryBuffSystem.BuffType.RevivalFlame;
            default:                        return TemporaryBuffSystem.BuffType.BerserkPotion;
        }
    }

    /// <summary>
    /// 创建特殊掉落物
    /// </summary>
    public static GameObject Create(Vector3 position, DropType type, float value, float duration)
    {
        var go = new GameObject($"SpecialDrop_{type}");
        go.transform.position = position;
        go.tag = "Untagged";

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Square;

        // 不同类型不同颜色
        switch (type)
        {
            case DropType.HealthPotion:   sr.color = new Color(1f, 0.2f, 0.2f); break;
            case DropType.MagnetBurst:    sr.color = new Color(0.2f, 0.5f, 1f); break;
            case DropType.DamageBoost:    sr.color = new Color(1f, 0.6f, 0f);   break;
            case DropType.ShieldOrb:      sr.color = new Color(0.8f, 0.8f, 1f); break;
            case DropType.XPMultiplier:     sr.color = new Color(0.5f, 1f, 0.5f); break;
            case DropType.BerserkPotion:    sr.color = new Color(1f, 0f, 0f);     break;
            case DropType.GhostWalk:        sr.color = new Color(0.7f, 0.7f, 1f); break;
            case DropType.TimeSlowField:    sr.color = new Color(0.3f, 0.3f, 0.8f); break;
            case DropType.ElementStorm:     sr.color = new Color(0.8f, 0.3f, 1f); break;
            case DropType.InvincibleShield: sr.color = new Color(1f, 1f, 0.5f);   break;
            case DropType.GoldRain:         sr.color = new Color(1f, 0.85f, 0f);  break;
            case DropType.ThornsShield:     sr.color = new Color(0.5f, 1f, 0.5f); break;
            case DropType.RevivalFlame:     sr.color = new Color(1f, 0.4f, 0f);   break;
        }

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.5f, 0.5f);

        var drop = go.AddComponent<SpecialDrop>();
        drop.Setup(type, value, duration);

        return go;
    }
}