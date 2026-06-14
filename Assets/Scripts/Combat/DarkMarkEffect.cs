using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 黑暗标记效果 — 挂载到敌人身上
/// 每次被暗影子弹命中叠加1层
/// 敌人死亡时，传播效率 = 30% + 层数×5%
/// 黑暗标记本身不会被传播
/// </summary>
public class DarkMarkEffect : MonoBehaviour, IStackEffect
{
    private float _spreadRadius = 3f;
    private float _baseSpreadEfficiency = 0.5f;
    private float _perStackEfficiency = 0.05f;
    private int _stackCount = 0;
    private Damageable _damageable;
    private bool _spreadDone = false;
    private bool _visualApplied = false;
    private Color _originalColor;
    private BaseEntity _entity;

    private static readonly List<Collider2D> _overlapBuffer = new List<Collider2D>(16);

    public int StackCount => _stackCount;
    public bool ConsumeStack() => false;
    public StatusEffectType EffectType => StatusEffectType.Dark;
    public bool IsActive => _stackCount > 0;

    /// <summary>
    /// 传播范围加成（可被升级增强）
    /// </summary>
    [System.NonSerialized] public float RadiusBonus = 0f;
    /// <summary>
    /// 传播效率加成（可被升级增强）
    /// </summary>
    [System.NonSerialized] public float EfficiencyBonus = 0f;

    public void Init(float radius, float baseEfficiency)
    {
        _spreadRadius = radius;
        _baseSpreadEfficiency = baseEfficiency;
        _damageable = GetComponent<Damageable>();
        _spreadDone = false;
        AddStack();
    }

    /// <summary>
    /// 叠加1层黑暗标记
    /// </summary>
    public void AddStack()
    {
        _stackCount++;
        ApplyDarkVisual();
        SubscribeDeath();
    }

    private void SubscribeDeath()
    {
        if (_entity != null) return; // 已订阅
        _entity = GetComponent<BaseEntity>();
        if (_entity != null) _entity.OnDeath += OnDeathHandler;
    }

    private void UnsubscribeDeath()
    {
        if (_entity != null) { _entity.OnDeath -= OnDeathHandler; _entity = null; }
    }

    private void OnDeathHandler(Vector3 deathPos)
    {
        if (!_spreadDone)
        {
            _spreadDone = true;
            SpreadDotOnDeath();
        }
    }

    private void ApplyDarkVisual()
    {
        if (_visualApplied) return;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            _originalColor = sr.color;
            // 降低亮度30%，增加紫色色调
            float darken = 0.7f;
            sr.color = new Color(
                _originalColor.r * darken + 0.1f,
                _originalColor.g * darken,
                _originalColor.b * darken + 0.15f,
                _originalColor.a);
            _visualApplied = true;
        }
    }

    private void OnEnable()
    {
        _spreadDone = false;
        _visualApplied = false;
        _stackCount = 0;
        _damageable = GetComponent<Damageable>();
        if (_entity != null) SubscribeDeath();
        DotEffectConfig.OnConfigChanged += RefreshFromConfig;
    }

    private void OnDisable()
    {
        DotEffectConfig.OnConfigChanged -= RefreshFromConfig;
        UnsubscribeDeath();
        RestoreVisual();
    }

    private void OnDestroy()
    {
        UnsubscribeDeath();
        RestoreVisual();
    }

    /// <summary>
    /// 死亡时传播DOT到周围敌人（通过OnDeath事件触发，此时效果还在）
    /// </summary>
    private void SpreadDotOnDeath()
    {
        var sem = GetComponent<StatusEffectManager>();

        float radius = _spreadRadius * (1f + RadiusBonus);
        float efficiency = Mathf.Clamp01(_baseSpreadEfficiency + _stackCount * _perStackEfficiency + EfficiencyBonus);
        Vector3 pos = transform.position;

        var effects = sem?.ActiveEffects;
        bool hasAnyDot = (effects != null && effects.Count > 0);

        // 传播独立DOT组件（Burn/Poison/Frost/Static）通过公共帮助类
        int componentSpread = DotSpreadHelper.SpreadEffects(gameObject, (Vector2)pos, radius, efficiency);

        if (!hasAnyDot && componentSpread == 0)
        {
            DebugHelper.Log("[DarkMark] No active DOT effects to spread");
            return;
        }

        DebugHelper.Log($"[DarkMark] Enemy dying with {(effects?.Count ?? 0)} DOT types + {componentSpread} component spreads, spreading at {efficiency:P0} eff ({_stackCount} stacks)");

        _overlapBuffer.Clear();
        int nearbyCount = PhysicsHelper.OverlapCircle(pos, radius, _overlapBuffer);
        int spreadCount = 0;
        Collider2D nearestEnemy = null;
        float nearestDist = float.MaxValue;

        for (int ni = 0; ni < nearbyCount; ni++)
        {
            var col = _overlapBuffer[ni];
            if (!col.CompareTag("Enemy")) continue;
            if (col.gameObject == gameObject) continue;

            var targetDmg = col.GetComponent<Damageable>();
            if (targetDmg == null || targetDmg.CurrentHp <= 0) continue;

            float dist = Vector2.Distance(pos, col.transform.position);
            if (dist < nearestDist) { nearestDist = dist; nearestEnemy = col; }

            DotBulletHelper.EnsureStatusEffectManager(col.gameObject);
            var targetSem = col.GetComponent<StatusEffectManager>();
            if (targetSem == null) continue;

            // 传播 StatusEffectManager 中的每种DOT
            if (effects != null)
            {
                foreach (var effect in effects)
                {
                    // 跳过黑暗标记本身（防止无限传播）
                    if (effect.type == StatusEffectType.Dark) continue;

                    float spreadDps = effect.damagePerSecond * efficiency;
                    float spreadDuration = effect.remainingDuration * efficiency;

                    if (spreadDps <= 0f && effect.type != StatusEffectType.Frostbite) continue;

                    targetSem.ApplyEffect(effect.type, spreadDps, spreadDuration,
                        effect.canCrit, effect.critChance, effect.critMultiplier);
                }
            }

            // 传播时附加紫色视觉效果
            CombatManager.CreateExplosionEffect(col.transform.position, 0.4f,
                new Color(0.4f, 0.1f, 0.6f, 0.5f), 0.3f);

            spreadCount++;
        }

        // 锁链视觉：从死亡敌人指向最近敌人
        if (nearestEnemy != null)
            CreateDarkChain(pos, nearestEnemy.transform.position);

        if (spreadCount > 0 || componentSpread > 0)
        {
            // 死亡时暗紫色冲击波扩散
            CombatManager.CreateExplosionEffect(pos, radius, new Color(0.4f, 0.1f, 0.6f, 0.3f), 0.5f);
            DebugHelper.Log($"[DarkMark] Spread DOT to {spreadCount + componentSpread} enemies, radius={radius:F1}, eff={efficiency:P0}");
        }
    }

    /// <summary>
    /// 创建暗紫色锁链视觉效果（从死亡敌人指向最近敌人）
    /// </summary>
    private static void CreateDarkChain(Vector3 from, Vector3 to)
    {
        var lineObj = VFXPool.Get("DarkChain");
        lineObj.transform.position = from;
        var lr = lineObj.GetComponent<LineRenderer>();
        if (lr == null) lr = lineObj.AddComponent<LineRenderer>();
        lr.material = MaterialCache.GetDefault();
        lr.startColor = new Color(0.5f, 0.1f, 0.8f, 0.9f);
        lr.endColor = new Color(0.3f, 0.05f, 0.5f, 0f);
        lr.startWidth = 0.15f;
        lr.endWidth = 0.05f;
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.sortingOrder = 25;
        Object.Destroy(lineObj, 0.6f);
    }

    private void RestoreVisual()
    {
        if (_visualApplied)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = _originalColor;
            _visualApplied = false;
        }
    }

    private void RefreshFromConfig()
    {
        var cfg = DotEffectConfig.GetDefault();
        _spreadRadius = cfg.DarkBaseRadius;
        _baseSpreadEfficiency = cfg.DarkBaseEfficiency;
        _perStackEfficiency = cfg.DarkPerStackEfficiency;
    }
}
