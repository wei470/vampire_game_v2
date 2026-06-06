#pragma warning disable CS0414
using UnityEngine;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// 伤害统计系统 — 记录并展示各伤害来源的贡献。
/// 暂停时在右上角显示 DPS 面板。
///
/// 统计维度：
/// - 各 DOT 类型总伤害 / DPS / 占比
/// - 引爆总伤害
/// - 子弹命中总伤害
/// - 暴击次数
/// - 总伤害 / 平均 DPS
///
/// 使用方式：由 GameSceneBootstrap 自动创建，挂在同一 GameObject 上。
/// GUI绘制委托给 DamageBreakdownUI。
/// </summary>
public class DamageMeter : MonoBehaviour
{
    // ════════════════════════════════════════════════════════════════
    // 伤害来源分类
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 伤害来源类型
    /// </summary>
    public enum DamageSource
    {
        Bleed,          // 流血 DOT
        Poison,         // 中毒 DOT
        Burn,           // 燃烧 DOT
        Frostbite,      // 霜冻 DOT
        Detonate,       // 引爆
        Bullet,         // 子弹命中（非 DOT）
        Skill,          // 技能伤害
        Other           // 其他
    }

    /// <summary>
    /// 单个来源的伤害统计
    /// </summary>
    public class SourceStats
    {
        public long totalDamage;
        public int hitCount;
        public int critCount;
        public float firstHitTime;
        public float lastHitTime;
    }

    // ════════════════════════════════════════════════════════════════
    // 数据存储
    // ════════════════════════════════════════════════════════════════

    private Dictionary<DamageSource, SourceStats> _stats = new Dictionary<DamageSource, SourceStats>();
    private float _combatStartTime;
    private bool _isTracking = false;

    // #41 额外统计追踪
    private long _maxSingleDetonateDamage = 0;   // 最高单次引爆伤害
    private float _maxDpsPeak = 0f;               // 最高 DPS 峰值
    private int _totalDotTicks = 0;               // 总 DOT 生效次数
    private int _totalKills = 0;                  // 总击杀数
    private int _bossKills = 0;                   // Boss 击杀数
    private float _lastDpsCalcTime = 0f;          // 上次 DPS 峰值计算时间
    private long _damageSinceLastDpsCalc = 0;    // 上次计算后的伤害增量

    // ════════════════════════════════════════════════════════════════
    // 单例
    // ════════════════════════════════════════════════════════════════

    private static DamageMeter _instance;
    public static DamageMeter Instance => _instance;

    // ════════════════════════════════════════════════════════════════
    // 公共属性
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 是否正在暂停显示面板
    /// </summary>
    public bool ShowPanel { get; set; } = false;

    /// <summary>
    /// 获取总伤害
    /// </summary>
    public long TotalDamage
    {
        get
        {
            long total = 0;
            foreach (var kvp in _stats)
                total += kvp.Value.totalDamage;
            return total;
        }
    }

    /// <summary>获取战斗时长</summary>
    public float CombatTime => _isTracking ? (Time.time - _combatStartTime) : 0f;

    // #41 额外统计公共访问器
    public long MaxSingleDetonateDamage => _maxSingleDetonateDamage;
    public float MaxDpsPeak => _maxDpsPeak;
    public int TotalDotTicks => _totalDotTicks;
    public int TotalKills => _totalKills;
    public int BossKills => _bossKills;
    public Dictionary<DamageSource, SourceStats> StatsSnapshot => _stats;

    // ════════════════════════════════════════════════════════════════
    // 生命周期
    // ════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void OnEnable()
    {
        EventManager.OnDamage += OnDamageReceived;
        EventManager.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        EventManager.OnDamage -= OnDamageReceived;
        EventManager.OnGameStateChanged -= OnGameStateChanged;
        if (_instance == this) _instance = null;
    }

    // ════════════════════════════════════════════════════════════════
    // 事件回调
    // ════════════════════════════════════════════════════════════════

    private void OnGameStateChanged(GameManager.GameState oldState, GameManager.GameState newState)
    {
        if (newState == GameManager.GameState.Playing && !_isTracking)
        {
            StartTracking();
        }
        // 暂停时自动显示面板
        ShowPanel = (newState == GameManager.GameState.Paused);
    }

    private void OnDamageReceived(GameObject target, int damage, Vector3 sourcePos)
    {
        if (!_isTracking || damage <= 0) return;
        if (target == null) return;

        // 只统计对敌人的伤害（Tag == "Enemy"）
        if (!target.CompareTag("Enemy")) return;

        // 推断伤害来源
        DamageSource source = InferDamageSource(target, damage);

        RecordDamage(source, damage, false);
    }

    // ════════════════════════════════════════════════════════════════
    // 公共 API — 手动记录伤害
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 开始追踪（游戏开始时调用）
    /// </summary>
    public void StartTracking()
    {
        _isTracking = true;
        _combatStartTime = Time.time;
        _stats.Clear();
    }

    /// <summary>
    /// 停止追踪
    /// </summary>
    public void StopTracking()
    {
        _isTracking = false;
    }

    /// <summary>
    /// 记录一次伤害
    /// </summary>
    public void RecordDamage(DamageSource source, int damage, bool isCrit = false)
    {
        if (!_isTracking) return;

        if (!_stats.TryGetValue(source, out var stats))
        {
            stats = new SourceStats { firstHitTime = Time.time };
            _stats[source] = stats;
        }

        stats.totalDamage += damage;
        stats.hitCount++;
        if (isCrit) stats.critCount++;
        stats.lastHitTime = Time.time;
    }

    /// <summary>
    /// 记录引爆伤害
    /// </summary>
    public void RecordDetonate(int damage, int enemiesHit)
    {
        RecordDamage(DamageSource.Detonate, damage);
        // #41 追踪最高单次引爆伤害
        if (damage > _maxSingleDetonateDamage)
            _maxSingleDetonateDamage = damage;
    }

    /// <summary>
    /// 记录 DOT tick 伤害
    /// </summary>
    public void RecordDotDamage(StatusEffectType dotType, int damage)
    {
        DamageSource source = dotType switch
        {
            StatusEffectType.Bleed => DamageSource.Bleed,
            StatusEffectType.Poison => DamageSource.Poison,
            StatusEffectType.Burn => DamageSource.Burn,
            StatusEffectType.Frostbite => DamageSource.Frostbite,
            _ => DamageSource.Other
        };
        RecordDamage(source, damage);
        _totalDotTicks++;
        _damageSinceLastDpsCalc += damage;
    }

    /// <summary>
    /// #41 记录击杀（由 EventManager 触发）
    /// </summary>
    public void RecordKill(bool isBoss)
    {
        _totalKills++;
        if (isBoss) _bossKills++;
    }

    /// <summary>
    /// 重置统计（新一局游戏时调用）
    /// </summary>
    public void ResetStats()
    {
        _stats.Clear();
        _isTracking = false;
        ShowPanel = false;
        _maxSingleDetonateDamage = 0;
        _maxDpsPeak = 0f;
        _totalDotTicks = 0;
        _totalKills = 0;
        _bossKills = 0;
        _lastDpsCalcTime = 0f;
        _damageSinceLastDpsCalc = 0;
    }

    /// <summary>
    /// 生成统计摘要文本（供分享按钮使用）
    /// </summary>
    public string GenerateStatsSummary()
    {
        float elapsed = CombatTime;
        long totalDmg = TotalDamage;
        float avgDps = elapsed > 0 ? totalDmg / elapsed : 0f;

        var sb = new StringBuilder();
        sb.AppendLine("═══ Vampire Survivors 战斗统计 ═══");
        sb.AppendLine($"⏱ 存活时间: {DamageBreakdownUI.FormatTime(elapsed)}");
        sb.AppendLine($"⚔ 总伤害: {DamageBreakdownUI.FormatDamage(totalDmg)}");
        sb.AppendLine($"📊 平均DPS: {DamageBreakdownUI.FormatDamage((long)avgDps)}/s");
        sb.AppendLine($"💀 击杀数: {_totalKills} | Boss: {_bossKills}");
        sb.AppendLine($"💥 最高引爆: {DamageBreakdownUI.FormatDamage(_maxSingleDetonateDamage)}");
        sb.AppendLine($"🔥 最高DPS: {DamageBreakdownUI.FormatDamage((long)_maxDpsPeak)}/s");
        sb.AppendLine($"☠ DOT总触发: {_totalDotTicks}次");
        sb.AppendLine();
        sb.AppendLine("─ 伤害分布 ─");

        long td = totalDmg;
        foreach (var kvp in _stats)
        {
            float pct = td > 0 ? (float)kvp.Value.totalDamage / td * 100f : 0f;
            sb.AppendLine($"  {DamageBreakdownUI.GetSourceName(kvp.Key)}: {DamageBreakdownUI.FormatDamage(kvp.Value.totalDamage)} ({pct:F1}%)");
        }
        return sb.ToString();
    }

    // ════════════════════════════════════════════════════════════════
    // 推断伤害来源（简化逻辑）
    // ════════════════════════════════════════════════════════════════

    private DamageSource InferDamageSource(GameObject target, int damage)
    {
        // 检查目标是否有 DOT 效果
        var sem = target.GetComponent<StatusEffectManager>();
        if (sem != null && sem.HasAnyDot)
        {
            // 根据主要 DOT 类型判断
            var effects = sem.ActiveEffects;
            if (effects.Count > 0)
            {
                var primary = effects[0];
                return primary.type switch
                {
                    StatusEffectType.Bleed => DamageSource.Bleed,
                    StatusEffectType.Poison => DamageSource.Poison,
                    StatusEffectType.Burn => DamageSource.Burn,
                    StatusEffectType.Frostbite => DamageSource.Frostbite,
                    _ => DamageSource.Other
                };
            }
        }

        // 默认为子弹伤害
        return DamageSource.Bullet;
    }

    // ════════════════════════════════════════════════════════════════
    // GUI 显示（暂停时在右上角）
    // ════════════════════════════════════════════════════════════════

    private void OnGUI()
    {
        if (!ShowPanel) return;
        DamageBreakdownUI.DrawPanel(_stats, _combatStartTime, _isTracking, TotalDamage);
    }
}