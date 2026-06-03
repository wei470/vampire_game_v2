using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 统一状态效果（DOT/Debuff）系统。
/// 替代分散的 PoisonEffect / FireZone / FrostOrb 各自的 DOT 实现。
///
/// 使用方式：StatusEffectManager 挂载到敌人身上，管理所有活跃效果。
/// </summary>

/// <summary>
/// 状态效果类型枚举（14 种 Mage 专属升级对应）
/// </summary>
public enum StatusEffectType
{
    Bleed,          // 流血 — 物理 DOT
    Poison,         // 中毒 — 自然 DOT
    Burn,           // 燃烧 — 火焰 DOT
    Frostbite,      // 霜冻 — 冰霜 DOT + 减速
    Corrosion,      // 腐蚀 — 降低护甲
    Curse,          // 诅咒 — 受到伤害增加
    Agony,          // 痛苦 — DOT 随目标已损生命增强
    Wither,         // 凋零 — 禁止回血
    Immolate,       // 献祭 — 高伤害 DOT
    Radiate,        // 辐射 — DOT 对周围造成伤害
    Contaminate,    // 污染 — 死亡时传播 DOT
    Erosion,        // 侵蚀 — 降低最大生命
    WindErosion,    // 风蚀 — DOT + 击退
    Rend            // 撕裂 — 增强所有 DOT 伤害
}

/// <summary>
/// 单个状态效果实例
/// </summary>
[System.Serializable]
public class StatusEffect
{
    public StatusEffectType type;
    public float damagePerSecond;
    public float remainingDuration;
    public float totalDuration;
    public int stackCount;
    public bool canCrit;              // 是否可暴击（Mage 被动）
    public float critChance;
    public float critMultiplier;

    /// <summary>
    /// 刷新效果（叠加或刷新持续时间）
    /// </summary>
    public void Refresh(float dps, float duration, bool stackDps = false)
    {
        if (stackDps)
        {
            stackCount++;
            damagePerSecond += dps;
        }
        else
        {
            damagePerSecond = Mathf.Max(damagePerSecond, dps);
        }
        remainingDuration = Mathf.Max(remainingDuration, duration);
        totalDuration = remainingDuration;
    }
}

/// <summary>
/// 状态效果管理器 — 挂载到敌人身上，管理所有活跃 DOT/Debuff
/// </summary>
public class StatusEffectManager : MonoBehaviour
{
    private List<StatusEffect> _activeEffects = new List<StatusEffect>();
    private Damageable _damageable;
    private SpriteRenderer _sr;
    private Color _originalColor;
    private float _lastTickTime;
    private const float BASE_TICK_INTERVAL = 0.5f;
    private float _tickInterval = 0.5f; // 动态 tick 间隔，受痛苦升级影响
    private int _erosionDotHitCount; // 侵蚀：DOT 生效计数器
    private DotParticleVFX _dotVFX; // #18 DOT 粒子视觉效果
    private EnemyDotResistance _dotResistance; // #11 DOT 抗性系统

    /// <summary>
    /// DOT 频率加成（痛苦升级：每层 -10% 间隔）
    /// </summary>
    public float DotFrequencyBonus
    {
        get => _dotFrequencyBonus;
        set
        {
            _dotFrequencyBonus = Mathf.Clamp01(value);
            _tickInterval = Mathf.Max(0.15f, BASE_TICK_INTERVAL * (1f - _dotFrequencyBonus));
        }
    }
    private float _dotFrequencyBonus = 0f;

    // 全局属性（由 MagePassive 等设置）
    public float DotDurationMultiplier { get; set; } = 1f;
    public float DotDamageMultiplier { get; set; } = 1f;
    public float RendDamageBonus { get; set; } = 0f;     // 撕裂加成
    public float CorrosionArmorReduction { get; set; } = 0f; // 腐蚀护甲减少
    public float CurseDamageAmplify { get; set; } = 0f;  // 诅咒伤害增幅
    public float AgonyMissingHpScale { get; set; } = 0f; // 痛苦已损生命系数
    public bool WitherActive { get; set; } = false;       // 凋零禁回血
    public float ErosionMaxHpReduce { get; set; } = 0f;  // 侵蚀降最大生命
    public float WindErosionKnockback { get; set; } = 0f; // 风蚀击退
    public float ErosionDamagePercent { get; set; } = 0f; // 侵蚀：额外冲击伤害比例（默认0，升级后+0.5）
    public int ErosionTriggerCount { get; set; } = 5;      // 侵蚀：每N次DOT生效触发冲击（默认5，最低2）
    public float ContaminateRange { get; set; } = 0f;     // 污染传播范围
    public float RadiateRange { get; set; } = 0f;         // 辐射范围
    public float RadiateDamagePercent { get; set; } = 0f; // 辐射伤害比例

    /// <summary>
    /// 获取当前所有活跃效果
    /// </summary>
    public List<StatusEffect> ActiveEffects => _activeEffects;

    /// <summary>
    /// 是否有任何 DOT 效果
    /// </summary>
    public bool HasAnyDot => _activeEffects.Count > 0;

    private void Awake()
    {
        _damageable = GetComponent<Damageable>();
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _originalColor = _sr.color;
        _dotResistance = GetComponent<EnemyDotResistance>(); // #11 缓存抗性引用
    }

    private void OnEnable()
    {
        _activeEffects.Clear();
        _lastTickTime = Time.time;
        _erosionDotHitCount = 0;
        if (_sr != null) _originalColor = _sr.color;

        // 诅咒：注册死亡事件，在敌人死亡时自动传播DOT
        var entity = GetComponent<BaseEntity>();
        if (entity != null)
            entity.OnDeath += OnDeathHandler;
    }

    private void OnDisable()
    {
        // 注销死亡事件
        var entity = GetComponent<BaseEntity>();
        if (entity != null)
            entity.OnDeath -= OnDeathHandler;
    }

    /// <summary>
    /// 敌人死亡时触发诅咒DOT传播
    /// </summary>
    private void OnDeathHandler(Vector3 deathPos)
    {
        OnEnemyDeath_SpreadContaminate();
    }

    /// <summary>
    /// 施加一个状态效果
    /// </summary>
    public void ApplyEffect(StatusEffectType type, float dps, float duration,
        bool canCrit = false, float critChance = 0f, float critMult = 2f)
    {
        // 应用持续时间倍率
        duration *= DotDurationMultiplier;

        // #30 查找已有同类效果 — 使用 for 循环替代 Find(lambda)，避免闭包 GC
        StatusEffect existing = null;
        for (int i = 0; i < _activeEffects.Count; i++)
        {
            if (_activeEffects[i].type == type) { existing = _activeEffects[i]; break; }
        }
        if (existing != null)
        {
            existing.Refresh(dps * DotDamageMultiplier, duration, stackDps: (type == StatusEffectType.Bleed || type == StatusEffectType.Immolate));
        }
        else
        {
            // #16 防止极端情况：超过 10 个效果时移除最旧的
            if (_activeEffects.Count >= 10)
            {
                _activeEffects.RemoveAt(0);
            }

            _activeEffects.Add(new StatusEffect
            {
                type = type,
                damagePerSecond = dps * DotDamageMultiplier,
                remainingDuration = duration,
                totalDuration = duration,
                stackCount = 1,
                canCrit = canCrit,
                critChance = critChance,
                critMultiplier = critMult
            });
        }

        // 应用腐蚀护甲减少
        if (type == StatusEffectType.Corrosion && _damageable != null)
        {
            _damageable.SetArmor(Mathf.Max(0, _damageable.Armor - Mathf.RoundToInt(CorrosionArmorReduction)));
        }
    }

    /// <summary>
    /// 施加通用效果（不带暴击参数）
    /// </summary>
    public void ApplyEffect(StatusEffectType type, float dps, float duration)
    {
        ApplyEffect(type, dps, duration, false, 0f, 2f);
    }

    private void Update()
    {
        if (_activeEffects.Count == 0) return;

        // #31 屏幕外敌人降低 DOT tick 频率（2倍间隔），跳过视觉更新
        bool isOffScreen = OffScreenCuller.IsOffScreen((Vector2)transform.position);
        float effectiveInterval = isOffScreen ? _tickInterval * 2f : _tickInterval;
        if (Time.time - _lastTickTime < effectiveInterval) return;
        _lastTickTime = Time.time;

        // #31 屏幕外敌人跳过视觉更新（颜色脉冲+粒子）
        if (!isOffScreen)
        {
            // 颜色闪烁（取最优先的效果颜色）
            UpdateVisual();
        }

        float totalTickDamage = 0f;
        bool hasRadiate = false;
        float radiateDmg = 0f;

        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = _activeEffects[i];
            effect.remainingDuration -= _tickInterval;

            if (effect.remainingDuration <= 0)
            {
                _activeEffects.RemoveAt(i);
                continue;
            }

            // 计算基础 DOT 伤害
        float tickDmg = effect.damagePerSecond * _tickInterval;

            // #11 应用敌人 DOT 抗性
            if (_dotResistance != null)
                tickDmg *= _dotResistance.GetDamageMultiplier(effect.type);

            // 撕裂加成
            if (RendDamageBonus > 0)
                tickDmg *= (1f + RendDamageBonus);

            // 痛苦加成（已损生命越多伤害越高）
            if (AgonyMissingHpScale > 0 && _damageable != null)
            {
                float missingPercent = 1f - (float)_damageable.CurrentHp / _damageable.MaxHp;
                tickDmg *= (1f + missingPercent * AgonyMissingHpScale);
            }

            // 暴击判定（Mage 被动）
            if (effect.canCrit && Random.value < effect.critChance)
            {
                tickDmg *= effect.critMultiplier;
            }

            // 凋零：禁止回血（在 Damageable 中检查）
            if (effect.type == StatusEffectType.Wither)
                WitherActive = true;

            // 辐射检测
            if (effect.type == StatusEffectType.Radiate && RadiateRange > 0)
            {
                hasRadiate = true;
                radiateDmg = tickDmg * RadiateDamagePercent;
            }

            totalTickDamage += tickDmg;
        }

        // ═══ #19 DOT 组合效果检测 ═══
        CheckComboEffects();

        // ═══ #18 DOT 粒子视觉效果更新 ═══
        UpdateDotParticles();

        // 对敌人造成 DOT 伤害
        if (totalTickDamage > 0 && _damageable != null && _damageable.CurrentHp > 0)
        {
            // 碎冰：霜冻+流血时，冰冻期间流血伤害×2
            if (_comboShatterActive)
                totalTickDamage *= 1f + COMBO_SHATTER_BLEED_MULT;

            // 凋零禁回血检查在 Damageable.Heal() 中处理
            _damageable.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(totalTickDamage)));

            // 播放 DOT tick 音效（带冷却，防止音效轰炸）
            if (SFXManager.Instance != null)
                SFXManager.Instance.PlayDotTick();

            // 记录 DOT 伤害到 DamageMeter
            if (DamageMeter.Instance != null && _activeEffects.Count > 0)
                DamageMeter.Instance.RecordDotDamage(_activeEffects[0].type, Mathf.RoundToInt(totalTickDamage));

            // 侵蚀：每 N 次 DOT 生效触发额外冲击
            if (ErosionDamagePercent > 0)
            {
                _erosionDotHitCount++;
                if (_erosionDotHitCount >= ErosionTriggerCount)
                {
                    _erosionDotHitCount = 0;
                    int erosionDmg = Mathf.Max(1, Mathf.RoundToInt(totalTickDamage * ErosionDamagePercent));
                    _damageable.TakeDamage(erosionDmg, new Color(0.7f, 0.8f, 0.2f));

                    // #24 侵蚀视觉反馈：绿色弹字 + 爆炸特效
                    DamagePopup.Create(transform.position, erosionDmg,
                        new Color(0.7f, 0.9f, 0.1f), false);
                    CombatManager.CreateExplosionEffect(transform.position, 1.5f,
                        new Color(0.6f, 0.8f, 0.2f), 0.3f);

                    // 侵蚀冲击时短暂变色（黄绿色闪一下）
                    if (_sr != null)
                    {
                        _sr.color = Color.Lerp(_sr.color, new Color(0.8f, 1f, 0.2f), 0.7f);
                    }

                    DebugHelper.Log($"[DOT] EROSION! {erosionDmg} bonus damage");
                }
            }
        }

        // 风蚀击退 + 漩涡生成
        if (WindErosionKnockback > 0)
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                var player = GameReferences.Player;
                if (player != null)
                {
                    Vector2 pushDir = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;
                    rb.linearVelocity += pushDir * WindErosionKnockback;
                }
            }
            // #14 风蚀：每秒在脚下生成微型漩涡
            TrySpawnVortex();
        }

        // 辐射：对周围敌人造成伤害（#28 优化：使用 SpawnManager 活跃敌人列表替代 Physics2D 查询）
        if (hasRadiate && RadiateRange > 0)
        {
            float radiateRangeSqr = RadiateRange * RadiateRange;
            var spawnMgr = GameReferences.SpawnManager;
            IReadOnlyList<GameObject> enemies = spawnMgr != null ? spawnMgr.ActiveEnemies : null;
            if (enemies != null)
            {
                for (int j = 0; j < enemies.Count; j++)
                {
                    var enemy = enemies[j];
                    if (enemy == null || enemy == gameObject || !enemy.activeInHierarchy) continue;
                    Vector2 delta = (Vector2)(enemy.transform.position - transform.position);
                    if (delta.sqrMagnitude > radiateRangeSqr) continue;
                    var hitDmg = enemy.GetComponent<Damageable>();
                    if (hitDmg != null && hitDmg.CurrentHp > 0)
                    {
                        hitDmg.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(radiateDmg)));
                    }
                }
            }
        }

        // 侵蚀：降低最大生命
        if (ErosionMaxHpReduce > 0 && _damageable != null)
        {
            int reduce = Mathf.RoundToInt(_damageable.MaxHp * ErosionMaxHpReduce * _tickInterval);
            if (reduce > 0)
            {
                int newMax = Mathf.Max(1, _damageable.MaxHp - reduce);
                _damageable.SetMaxHp(newMax);
            }
        }
    }

    /// <summary>
    /// #22 引爆结果数据，包含各类型层数信息，用于 MagePassive 实现特殊效果
    /// </summary>
    public struct DetonateResult
    {
        public int totalDamage;
        public int poisonStacks;
        public int burnStacks;
        public int bleedStacks;
        public int frostStacks;
        public bool hadBurn;
        public bool hadFrost;
        public bool hadPoison;
    }

    /// <summary>
    /// 引爆所有 DOT — Mage 专属能力
    /// #22 增强：高层数中毒额外倍率，燃烧>10层触发余烬，霜冻触发碎裂
    /// 返回引爆总伤害 + DetonateResult（供 MagePassive 使用）
    /// </summary>
    public int Detonate(float multiplier, float critChance, float critMult, out DetonateResult result)
    {
        result = new DetonateResult();
        if (_activeEffects.Count == 0) return 0;

        // 先收集各类型信息
        int poisonStacks = 0, burnStacks = 0, bleedStacks = 0, frostStacks = 0;
        float totalDamage = 0f;
        foreach (var effect in _activeEffects)
        {
            // 记录各类型层数
            switch (effect.type)
            {
                case StatusEffectType.Poison: poisonStacks += effect.stackCount; result.hadPoison = true; break;
                case StatusEffectType.Burn: burnStacks += effect.stackCount; result.hadBurn = true; break;
                case StatusEffectType.Bleed: bleedStacks += effect.stackCount; break;
                case StatusEffectType.Frostbite: frostStacks += effect.stackCount; result.hadFrost = true; break;
            }

            // 剩余时间 × 每秒伤害 × 引爆倍率
            float baseDmg = effect.damagePerSecond * effect.remainingDuration * multiplier;

            // #22 高层数中毒引爆额外倍率：每层 +5% 引爆伤害
            if (effect.type == StatusEffectType.Poison && effect.stackCount > 1)
            {
                float stackBonus = 1f + (effect.stackCount - 1) * 0.05f;
                baseDmg *= stackBonus;
            }

            // #22 高层数流血引爆额外倍率：每层 +3% 引爆伤害
            if (effect.type == StatusEffectType.Bleed && effect.stackCount > 1)
            {
                float stackBonus = 1f + (effect.stackCount - 1) * 0.03f;
                baseDmg *= stackBonus;
            }

            // 撕裂加成
            if (RendDamageBonus > 0)
                baseDmg *= (1f + RendDamageBonus);

            // 痛苦加成
            if (AgonyMissingHpScale > 0 && _damageable != null)
            {
                float missingPercent = 1f - (float)_damageable.CurrentHp / _damageable.MaxHp;
                baseDmg *= (1f + missingPercent * AgonyMissingHpScale);
            }

            // 暴击
            if (Random.value < critChance)
            {
                baseDmg *= critMult;
            }

            totalDamage += baseDmg;
        }

        // 填充结果
        result.poisonStacks = poisonStacks;
        result.burnStacks = burnStacks;
        result.bleedStacks = bleedStacks;
        result.frostStacks = frostStacks;

        // 清除所有效果
        _activeEffects.Clear();

        // 造成引爆伤害
        int finalDmg = Mathf.Max(1, Mathf.RoundToInt(totalDamage));
        if (_damageable != null && _damageable.CurrentHp > 0)
        {
            _damageable.TakeDamage(finalDmg);
        }

        result.totalDamage = finalDmg;
        return finalDmg;
    }

    /// <summary>
    /// 污染传播 — 敌人死亡时调用（诅咒升级触发）
    /// 传播范围 5f 内的敌人，目标数量由 MagePassive.CurseSpreadTargets 决定
    /// </summary>
    public void OnEnemyDeath_SpreadContaminate()
    {
        // 获取 MagePassive — 必须拥有诅咒升级才触发传播
        var magePassive = GameReferences.Player?.GetComponent<MagePassive>();
        if (magePassive == null) return;
        int spreadTargets = magePassive.CurseSpreadTargets;

        // 没有诅咒升级（默认1个目标=未升级），不传播
        if (spreadTargets <= 1) return;

        // 传播范围
        float range = ContaminateRange > 0 ? ContaminateRange : 5f;

        // 检查是否有任何DOT效果可以传播（StatusEffectManager + 独立DOT组件）
        bool hasAnyDot = _activeEffects.Count > 0;
        bool hasBleed = GetComponent<BleedEffect>() != null;
        bool hasBurn = GetComponent<BurnStackEffect>() != null;
        bool hasPoison = GetComponent<PoisonStackEffect>() != null;
        bool hasFrost = GetComponent<FrostEffect>() != null;
        if (!hasAnyDot && !hasBleed && !hasBurn && !hasPoison && !hasFrost) return;

        // 找到最近的 N 个敌人
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
        var validTargets = new System.Collections.Generic.List<Collider2D>();
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (!hit.CompareTag("Enemy")) continue;
            validTargets.Add(hit);
        }

        // 缓存源敌人的独立 DOT 组件（移到循环外部，避免每个目标重复 GetComponent）
        var srcBleed = GetComponent<BleedEffect>();
        var srcBurn = GetComponent<BurnStackEffect>();
        var srcPoison = GetComponent<PoisonStackEffect>();
        var srcFrost = GetComponent<FrostEffect>();

        // 传递给范围内所有敌人
        int spreadCount = validTargets.Count;
        for (int i = 0; i < spreadCount; i++)
        {
            var hit = validTargets[i];
            var otherManager = hit.GetComponent<StatusEffectManager>();
            if (otherManager == null)
                otherManager = hit.gameObject.AddComponent<StatusEffectManager>();

            // 传播 StatusEffectManager 中的 DOT（继承 10% 层数/伤害）
            foreach (var effect in _activeEffects)
            {
                otherManager.ApplyEffect(effect.type, effect.damagePerSecond * 0.1f, effect.remainingDuration * 0.1f,
                    effect.canCrit, effect.critChance, effect.critMultiplier);
            }

            // 传播独立 DOT 组件（继承 10% 的层数/伤害）
            if (srcBleed != null)
            {
                var otherBleed = hit.GetComponent<BleedEffect>();
                if (otherBleed == null) otherBleed = hit.gameObject.AddComponent<BleedEffect>();
                otherBleed.Refresh(srcBleed._dps * 0.1f, srcBleed._duration * 0.1f, srcBleed._canCrit, srcBleed._critChance, srcBleed._critMult);
            }

            if (srcBurn != null)
            {
                var otherBurn = hit.GetComponent<BurnStackEffect>();
                if (otherBurn == null) otherBurn = hit.gameObject.AddComponent<BurnStackEffect>();
                int burnStacks = Mathf.Max(1, Mathf.RoundToInt(srcBurn.StackCount * 0.1f));
                for (int s = 0; s < burnStacks; s++)
                    otherBurn.AddStack(srcBurn._baseDps * 0.1f, srcBurn._duration * 0.1f, srcBurn._canCrit, srcBurn._critChance, srcBurn._critMult);
            }

            if (srcPoison != null)
            {
                var otherPoison = hit.GetComponent<PoisonStackEffect>();
                if (otherPoison == null) otherPoison = hit.gameObject.AddComponent<PoisonStackEffect>();
                int poisonStacks = Mathf.Max(1, Mathf.RoundToInt(srcPoison.StackCount * 0.1f));
                for (int s = 0; s < poisonStacks; s++)
                    otherPoison.AddStack(2f, 0f, srcPoison._canCrit, srcPoison._critChance, srcPoison._critMult);
            }

            if (srcFrost != null)
            {
                var otherFrost = hit.GetComponent<FrostEffect>();
                if (otherFrost == null) otherFrost = hit.gameObject.AddComponent<FrostEffect>();
                otherFrost.ApplyFreeze(0.3f, srcFrost._slowPercent * 0.1f, srcFrost._frostDps * 0.1f, srcFrost._canCrit, srcFrost._critChance, srcFrost._critMult);
            }
        }

        // 传播视觉特效：灰色锁链连线
        if (spreadCount > 0)
        {
            for (int i = 0; i < spreadCount; i++)
            {
                var target = validTargets[i];
                if (target == null) continue;
                CreateSpreadLine(transform.position, target.transform.position);
            }
            DebugHelper.Log($"[StatusEffectManager] Curse spread to {spreadCount} enemies");
        }
    }

    /// <summary>
    /// 获取诅咒伤害增幅
    /// </summary>
    public float GetCurseAmplify()
    {
        return CurseDamageAmplify;
    }

    /// <summary>
    /// 是否处于凋零状态（禁止回血）
    /// </summary>
    public bool IsWithered()
    {
        return WitherActive;
    }

    /// <summary>
    /// #14 风蚀漩涡：当 WindErosionKnockback > 0 时，DOT 敌人脚下生成微型漩涡
    /// </summary>
    private float _lastVortexTime;
    private const float VORTEX_INTERVAL = 1.0f; // 每秒生成一个漩涡

    private void TrySpawnVortex()
    {
        if (WindErosionKnockback <= 0) return;
        if (Time.time - _lastVortexTime < VORTEX_INTERVAL) return;
        _lastVortexTime = Time.time;

        WindErosionVortex.Create(transform.position, 1.5f, 2f, 3f, 0.2f);
    }

    /// <summary>
    /// 缓存的连线材质（避免每次 Shader.Find + new Material 导致内存泄漏）
    /// </summary>
    private static Material _lineMaterial;

    private static Material GetLineMaterial()
    {
        if (_lineMaterial == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
                _lineMaterial = new Material(shader);
        }
        return _lineMaterial;
    }

    private void CreateSpreadLine(Vector3 from, Vector3 to)
    {
        var lineObj = new GameObject("CurseLine");
        lineObj.transform.position = from;
        var lr = lineObj.AddComponent<LineRenderer>();
        lr.material = GetLineMaterial();
        lr.startColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        lr.endColor = new Color(0.5f, 0.5f, 0.5f, 0f);
        lr.startWidth = 0.12f;
        lr.endWidth = 0.04f;
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.sortingOrder = 20;
        Destroy(lineObj, 0.5f); // 0.5秒后自动销毁
    }

    private void UpdateVisual()
    {
        if (_sr == null) return;

        // 根据效果类型变色
        Color effectColor = _originalColor;
        if (_activeEffects.Count > 0)
        {
            var primary = _activeEffects[0];
            switch (primary.type)
            {
                case StatusEffectType.Bleed:
                case StatusEffectType.Rend:
                    effectColor = Color.Lerp(_originalColor, new Color(0.8f, 0.1f, 0.1f), 0.6f);
                    break;
                case StatusEffectType.Poison:
                    effectColor = Color.Lerp(_originalColor, new Color(0.1f, 0.9f, 0.1f), 0.6f);
                    break;
                case StatusEffectType.Burn:
                case StatusEffectType.Immolate:
                    effectColor = Color.Lerp(_originalColor, new Color(1f, 0.4f, 0f), 0.6f);
                    break;
                case StatusEffectType.Frostbite:
                    effectColor = Color.Lerp(_originalColor, new Color(0.3f, 0.6f, 1f), 0.6f);
                    break;
                case StatusEffectType.Corrosion:
                case StatusEffectType.Erosion:
                    effectColor = Color.Lerp(_originalColor, new Color(0.5f, 0.8f, 0.2f), 0.6f);
                    break;
                case StatusEffectType.Curse:
                case StatusEffectType.Wither:
                    effectColor = Color.Lerp(_originalColor, new Color(0.4f, 0f, 0.6f), 0.6f);
                    break;
                case StatusEffectType.Agony:
                    effectColor = Color.Lerp(_originalColor, new Color(0.6f, 0f, 0.3f), 0.6f);
                    break;
                case StatusEffectType.Radiate:
                    effectColor = Color.Lerp(_originalColor, new Color(0f, 1f, 0.5f), 0.6f);
                    break;
                case StatusEffectType.Contaminate:
                    effectColor = Color.Lerp(_originalColor, new Color(0.3f, 0.5f, 0.3f), 0.6f);
                    break;
                case StatusEffectType.WindErosion:
                    effectColor = Color.Lerp(_originalColor, new Color(0.7f, 0.85f, 1f), 0.6f);
                    break;
            }
            float pulse = Mathf.Sin(Time.time * 6f) * 0.15f;
            effectColor = Color.Lerp(effectColor, _originalColor, 0.3f + pulse);
        }
        _sr.color = effectColor;
    }

    // ════════════════════════════════════════════════════════════════
    // #19 DOT 组合效果系统
    // ════════════════════════════════════════════════════════════════

    // 组合效果常量
    private const float COMBO_DETONATE_POISON_THRESHOLD = 5f; // 爆燃：中毒层数>5时引爆
    private const float COMBO_DETONATE_AOE_RADIUS = 2.5f;     // 爆燃：范围伤害半径
    private const float COMBO_SHATTER_BLEED_MULT = 1.0f;       // 碎冰：流血伤害×2
    private const float COMBO_SEPSIS_BLEED_DPS_PER_POISON = 0.3f; // 脓毒：每层中毒+30%流血DPS

    private bool _comboShatterActive;   // 碎冰：霜冻+流血
    #pragma warning disable CS0414 // 赋值后未使用，保留作为组合效果状态标记
    private bool _comboDetonateActive;  // 爆燃：燃烧+中毒
    private bool _comboSepsisActive;    // 脓毒：中毒+流血
    #pragma warning restore CS0414

    /// <summary>
    /// 检测并应用 DOT 组合效果
    /// </summary>
    private void CheckComboEffects()
    {
        _comboShatterActive = false;
        _comboDetonateActive = false;
        _comboSepsisActive = false;

        // 检查当前活跃的 DOT 类型
        bool hasBleed = false, hasPoison = false, hasBurn = false, hasFrost = false;
        int poisonStacks = 0;

        for (int i = 0; i < _activeEffects.Count; i++)
        {
            var e = _activeEffects[i];
            switch (e.type)
            {
                case StatusEffectType.Bleed: hasBleed = true; break;
                case StatusEffectType.Poison: hasPoison = true; poisonStacks = e.stackCount; break;
                case StatusEffectType.Burn: hasBurn = true; break;
                case StatusEffectType.Frostbite: hasFrost = true; break;
            }
        }

        // 也检查独立 DOT 组件
        if (!hasBleed && GetComponent<BleedEffect>() != null) hasBleed = true;
        if (!hasPoison)
        {
            var ps = GetComponent<PoisonStackEffect>();
            if (ps != null) { hasPoison = true; poisonStacks = ps.StackCount; }
        }
        if (!hasBurn && GetComponent<BurnStackEffect>() != null) hasBurn = true;
        if (!hasFrost && GetComponent<FrostEffect>() != null) hasFrost = true;

        // 🔵+🔴 = 碎冰：霜冻+流血时，冰冻期间流血伤害×2
        if (hasFrost && hasBleed)
        {
            _comboShatterActive = true;
            // 视觉：冰蓝色边框闪烁
            if (_sr != null)
                _sr.color = Color.Lerp(_sr.color, new Color(0.4f, 0.7f, 1f), 0.3f);
        }

        // 🔥+🟢 = 爆燃：燃烧+中毒时，中毒层数>5时引爆，造成范围伤害
        if (hasBurn && hasPoison && poisonStacks > (int)COMBO_DETONATE_POISON_THRESHOLD)
        {
            _comboDetonateActive = true;
            TriggerDetonateCombo();
        }

        // 🟢+🔴 = 脓毒：中毒+流血时，流血 DPS 随中毒层数增加
        if (hasPoison && hasBleed)
        {
            _comboSepsisActive = true;
            // 在流血组件中应用脓毒加成
            var bleed = GetComponent<BleedEffect>();
            if (bleed != null)
            {
                bleed._comboSepsisBonus = poisonStacks * COMBO_SEPSIS_BLEED_DPS_PER_POISON;
            }
        }
    }

    /// <summary>
    /// 爆燃组合：燃烧+中毒层数>5时，对周围造成范围伤害
    /// </summary>
    private float _lastDetonateComboTime;
    private const float DETONATE_COMBO_COOLDOWN = 3f; // 每3秒触发一次

    private void TriggerDetonateCombo()
    {
        if (Time.time - _lastDetonateComboTime < DETONATE_COMBO_COOLDOWN) return;
        _lastDetonateComboTime = Time.time;

        // 获取中毒层数决定伤害
        int poisonStacks = 1;
        for (int i = 0; i < _activeEffects.Count; i++)
        {
            if (_activeEffects[i].type == StatusEffectType.Poison)
            {
                poisonStacks = _activeEffects[i].stackCount;
                break;
            }
        }

        float aoeDmg = poisonStacks * 3f; // 每层中毒 3 点范围伤害

        // 对周围敌人造成范围伤害（使用 SpawnManager 活跃敌人列表）
        var spawnMgr = GameReferences.SpawnManager;
        IReadOnlyList<GameObject> enemies = spawnMgr != null ? spawnMgr.ActiveEnemies : null;
        if (enemies != null)
        {
            float radiusSqr = COMBO_DETONATE_AOE_RADIUS * COMBO_DETONATE_AOE_RADIUS;
            for (int j = 0; j < enemies.Count; j++)
            {
                var enemy = enemies[j];
                if (enemy == null || enemy == gameObject || !enemy.activeInHierarchy) continue;
                Vector2 delta = (Vector2)(enemy.transform.position - transform.position);
                if (delta.sqrMagnitude > radiusSqr) continue;
                var hitDmg = enemy.GetComponent<Damageable>();
                if (hitDmg != null && hitDmg.CurrentHp > 0)
                {
                    hitDmg.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(aoeDmg)));
                }
            }
        }

        // 爆燃视觉效果：橙绿色爆炸
        CombatManager.CreateExplosionEffect(transform.position, COMBO_DETONATE_AOE_RADIUS,
            new Color(1f, 0.6f, 0f), 0.4f);

        DebugHelper.Log($"[DOT Combo] DETONATE! Poison stacks={poisonStacks}, AOE dmg={aoeDmg:F0}");
    }

    /// <summary>
    /// #18 DOT 粒子视觉效果更新
    /// </summary>
    private void UpdateDotParticles()
    {
        // 检查是否有任何 DOT 活跃
        bool hasBleed = false, hasPoison = false, hasBurn = false, hasFrost = false;

        // 检查 StatusEffectManager 中的效果
        for (int i = 0; i < _activeEffects.Count; i++)
        {
            switch (_activeEffects[i].type)
            {
                case StatusEffectType.Bleed: hasBleed = true; break;
                case StatusEffectType.Poison: hasPoison = true; break;
                case StatusEffectType.Burn: case StatusEffectType.Immolate: hasBurn = true; break;
                case StatusEffectType.Frostbite: hasFrost = true; break;
            }
        }

        // 检查独立 DOT 组件
        if (!hasBleed && GetComponent<BleedEffect>() != null) hasBleed = true;
        if (!hasPoison && GetComponent<PoisonStackEffect>() != null) hasPoison = true;
        if (!hasBurn && GetComponent<BurnStackEffect>() != null) hasBurn = true;
        if (!hasFrost && GetComponent<FrostEffect>() != null) hasFrost = true;

        // 如果没有任何 DOT，移除 VFX 组件
        if (!hasBleed && !hasPoison && !hasBurn && !hasFrost)
        {
            if (_dotVFX != null)
            {
                Destroy(_dotVFX);
                _dotVFX = null;
            }
            return;
        }

        // 延迟获取或创建 VFX 组件
        if (_dotVFX == null)
            _dotVFX = GetComponent<DotParticleVFX>();
        if (_dotVFX == null)
            _dotVFX = gameObject.AddComponent<DotParticleVFX>();

        _dotVFX.UpdateEffects(hasBleed, hasPoison, hasBurn, hasFrost);
    }

    private void OnDestroy()
    {
        if (_sr != null)
            _sr.color = _originalColor;
        // 清理 DOT 粒子特效
        if (_dotVFX != null)
        {
            Destroy(_dotVFX);
            _dotVFX = null;
        }
    }
}