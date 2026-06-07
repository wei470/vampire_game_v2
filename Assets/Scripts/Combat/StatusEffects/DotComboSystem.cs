using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DOT 组合效果系统 — 从 StatusEffectManager 拆分而来
/// 检测并应用 DOT 之间的协同效果
/// 现有3种 + 新增7种基础组合 + 黑暗/光明相关组合待后续扩展
/// </summary>
public class DotComboSystem
{
    // ── 现有组合常量 ──
    private const float COMBO_DETONATE_POISON_THRESHOLD = 5f;
    private const float COMBO_DETONATE_AOE_RADIUS = 2.5f;
    public const float COMBO_SHATTER_BLEED_MULT = 1.0f;
    private const float COMBO_SEPSIS_BLEED_DPS_PER_POISON = 0.3f;
    private const float DETONATE_COMBO_COOLDOWN = 3f;

    // ── 新增组合常量 ──
    private const float COMBO_BOILING_BLOOD_BLEED_MULT = 2f;      // 沸血：流血伤害×2
    private const float COMBO_BOILING_BLOOD_BURN_RATE = 1.5f;     // 沸血：燃烧蔓延+50%
    private const float COMBO_LIGHTNING_BLEED_CHANCE = 0.3f;      // 血电：30%概率连锁
    private const float COMBO_LIGHTNING_BLEED_RADIUS = 3f;        // 血电：连锁范围
    private const float COMBO_LIGHTNING_BLEED_DMG = 5f;           // 血电：连锁伤害
    private const float COMBO_FROST_POISON_THRESHOLD = 5f;        // 冻毒：霜冻层数阈值
    private const float COMBO_FROST_POISON_MULT = 2f;             // 冻毒：中毒伤害×2
    private const float COMBO_CONDUCTIVE_POISON_RADIUS = 2f;      // 导电毒液：范围
    private const float COMBO_CONDUCTIVE_POISON_DMG = 3f;         // 导电毒液：伤害
    private const float COMBO_EVAPORATE_RADIUS = 2f;              // 蒸汽：范围
    private const float COMBO_EVAPORATE_SLOW = 0.5f;              // 蒸汽：减速50%
    private const float COMBO_PLASMA_THRESHOLD = 10f;             // 等离子：层数阈值
    private const float COMBO_PLASMA_AOE_DMG = 20f;               // 等离子：爆发伤害
    private const float COMBO_PLASMA_AOE_RADIUS = 3f;             // 等离子：范围
    private const float COMBO_SUPERCONDUCT_LIGHTNING_MULT = 3f;   // 超导：雷电伤害×3

    public bool ShatterActive { get; private set; }
    public bool BoilingBloodActive { get; private set; }
    public bool FrostPoisonActive { get; private set; }
    public float SuperconductMult { get; private set; } = 1f;

    private float _lastDetonateComboTime;
    private float _lastPlasmaTime;
    private float _lastConductiveTime;
    private const float PLASMA_COOLDOWN = 5f;
    private const float CONDUCTIVE_COOLDOWN = 2f;

    // ── 进化系统：全局组合伤害倍率 ──
    private static float _evolutionComboDamageMultiplier = 0f;

    /// <summary>
    /// 设置进化系统提供的全局DOT组合伤害加成（如+30%填0.3）
    /// </summary>
    public static void SetEvolutionComboMultiplier(float bonus)
    {
        _evolutionComboDamageMultiplier += bonus;
    }

    /// <summary>
    /// 获取进化系统提供的组合伤害倍率（1.0 + 加成）
    /// </summary>
    public static float GetEvolutionComboMult() => 1f + _evolutionComboDamageMultiplier;

    /// <summary>
    /// 重置进化组合倍率（游戏重启时调用）
    /// </summary>
    public static void ResetEvolutionComboMultiplier()
    {
        _evolutionComboDamageMultiplier = 0f;
    }

    /// <summary>
    /// 检测并应用 DOT 组合效果（每DOT tick调用）
    /// </summary>
    public void CheckComboEffects(List<StatusEffect> activeEffects, GameObject go, SpriteRenderer sr, Color originalColor)
    {
        // 重置状态
        ShatterActive = false;
        BoilingBloodActive = false;
        FrostPoisonActive = false;
        SuperconductMult = 1f;

        // ── 收集所有DOT类型 ──
        bool hasBleed = false, hasPoison = false, hasBurn = false, hasFrost = false;
        bool hasLightning = false;
        int poisonStacks = 0, burnStacks = 0, frostStacks = 0, lightningStacks = 0;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            var e = activeEffects[i];
            switch (e.type)
            {
                case StatusEffectType.Bleed: hasBleed = true; break;
                case StatusEffectType.Poison: hasPoison = true; poisonStacks = e.stackCount; break;
                case StatusEffectType.Burn: hasBurn = true; burnStacks = e.stackCount; break;
                case StatusEffectType.Frostbite: hasFrost = true; frostStacks = e.stackCount; break;
                case StatusEffectType.Static: hasLightning = true; lightningStacks = e.stackCount; break;
            }
        }

        // 检查独立DOT组件
        if (!hasBleed && go.GetComponent<BleedEffect>() != null) hasBleed = true;
        if (!hasPoison)
        {
            var ps = go.GetComponent<PoisonStackEffect>();
            if (ps != null) { hasPoison = true; poisonStacks = ps.StackCount; }
        }
        if (!hasBurn)
        {
            var bs = go.GetComponent<BurnStackEffect>();
            if (bs != null) { hasBurn = true; burnStacks = bs.StackCount; }
        }
        if (!hasFrost)
        {
            var fs = go.GetComponent<FrostEffect>();
            if (fs != null) { hasFrost = true; frostStacks = fs.FrostStacks; }
        }
        if (!hasLightning)
        {
            var ss = go.GetComponent<StaticStackEffect>();
            if (ss != null) { hasLightning = true; lightningStacks = ss.StackCount; }
        }

        // ═══ 现有组合 ═══

        // 碎冰：霜冻+流血 → 流血伤害×2
        if (hasFrost && hasBleed)
        {
            ShatterActive = true;
            if (sr != null)
                sr.color = Color.Lerp(sr.color, new Color(0.4f, 0.7f, 1f), 0.3f);
        }

        // 爆燃：燃烧+中毒层数>5 → 范围伤害
        if (hasBurn && hasPoison && poisonStacks > (int)COMBO_DETONATE_POISON_THRESHOLD)
        {
            TriggerDetonateCombo(activeEffects, go);
        }

        // 脓毒：中毒+流血 → 流血DPS随中毒层数增加
        if (hasPoison && hasBleed)
        {
            var bleed = go.GetComponent<BleedEffect>();
            if (bleed != null)
                bleed._comboSepsisBonus = poisonStacks * COMBO_SEPSIS_BLEED_DPS_PER_POISON;
        }

        // ═══ 新增基础组合 ═══

        // 沸血：流血+燃烧 → 流血伤害×2（通过ShatterActive的另一种方式标记）
        if (hasBleed && hasBurn)
        {
            BoilingBloodActive = true;
            if (sr != null)
                sr.color = Color.Lerp(sr.color, new Color(1f, 0.3f, 0.1f), 0.25f);
        }

        // 血电：流血+雷电 → 流血tick时30%概率连锁闪电
        if (hasBleed && hasLightning)
        {
            TriggerBloodLightning(go, burnStacks > 0);
        }

        // 冻毒：中毒+霜冻且霜冻>5层 → 中毒伤害×2
        if (hasPoison && hasFrost && frostStacks > (int)COMBO_FROST_POISON_THRESHOLD)
        {
            FrostPoisonActive = true;
            if (sr != null)
                sr.color = Color.Lerp(sr.color, new Color(0.3f, 0.9f, 0.5f), 0.2f);
        }

        // 导电毒液：中毒+雷电 → 毒液区域触电
        if (hasPoison && hasLightning)
        {
            TriggerConductivePoison(go);
        }

        // 蒸发：燃烧+霜冻 → 蒸汽云致盲减速
        if (hasBurn && hasFrost)
        {
            TriggerEvaporate(go);
        }

        // 等离子：燃烧+雷电且燃烧>10层 → 范围爆发
        if (hasBurn && hasLightning && burnStacks > (int)COMBO_PLASMA_THRESHOLD)
        {
            TriggerPlasma(go);
        }

        // 超导：霜冻+雷电 → 雷电伤害×3
        if (hasFrost && hasLightning)
        {
            SuperconductMult = COMBO_SUPERCONDUCT_LIGHTNING_MULT;
            if (sr != null)
                sr.color = Color.Lerp(sr.color, new Color(0.5f, 0.8f, 1f), 0.3f);
        }
    }

    /// <summary>
    /// 获取流血伤害倍率（沸血组合+脓毒组合综合计算）
    /// </summary>
    public float GetBleedDamageMult()
    {
        float mult = 1f;
        if (ShatterActive) mult += COMBO_SHATTER_BLEED_MULT;       // 碎冰+1.0
        if (BoilingBloodActive) mult += COMBO_BOILING_BLOOD_BLEED_MULT - 1f; // 沸血×2 → +1.0
        return mult;
    }

    /// <summary>
    /// 获取中毒伤害倍率（冻毒组合）
    /// </summary>
    public float GetPoisonDamageMult()
    {
        return FrostPoisonActive ? COMBO_FROST_POISON_MULT : 1f;
    }

    // ═══ 组合触发方法 ═══

    /// <summary>
    /// 爆燃组合：对周围造成范围伤害
    /// </summary>
    private void TriggerDetonateCombo(List<StatusEffect> activeEffects, GameObject go)
    {
        if (Time.time - _lastDetonateComboTime < DETONATE_COMBO_COOLDOWN) return;
        _lastDetonateComboTime = Time.time;

        int poisonStacks = 1;
        for (int i = 0; i < activeEffects.Count; i++)
        {
            if (activeEffects[i].type == StatusEffectType.Poison)
            { poisonStacks = activeEffects[i].stackCount; break; }
        }

        float aoeDmg = poisonStacks * 3f;
        DealAreaDamage(go, aoeDmg, COMBO_DETONATE_AOE_RADIUS);

        CombatManager.CreateExplosionEffect(go.transform.position, COMBO_DETONATE_AOE_RADIUS,
            new Color(1f, 0.6f, 0f), 0.4f);
        DebugHelper.Log($"[DOT Combo] DETONATE! Poison stacks={poisonStacks}, AOE dmg={aoeDmg:F0}");
    }

    /// <summary>
    /// 血电组合：流血tick时30%概率对周围敌人连锁闪电
    /// </summary>
    private void TriggerBloodLightning(GameObject go, bool forceChain)
    {
        if (!forceChain && Random.value > COMBO_LIGHTNING_BLEED_CHANCE) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(go.transform.position, COMBO_LIGHTNING_BLEED_RADIUS);
        int chainCount = 0;
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy") || hit.gameObject == go) continue;
            var dmg = hit.GetComponent<Damageable>();
            if (dmg == null || dmg.CurrentHp <= 0) continue;
            dmg.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(COMBO_LIGHTNING_BLEED_DMG)));
            chainCount++;
            if (chainCount >= 3) break;
        }
        if (chainCount > 0)
        {
            CombatManager.CreateExplosionEffect(go.transform.position, COMBO_LIGHTNING_BLEED_RADIUS,
                new Color(0.8f, 0.2f, 1f), 0.3f);
            DebugHelper.Log($"[DOT Combo] BLOOD LIGHTNING! Chained {chainCount} enemies");
        }
    }

    /// <summary>
    /// 导电毒液组合：中毒+雷电 → 范围持续电击
    /// </summary>
    private void TriggerConductivePoison(GameObject go)
    {
        if (Time.time - _lastConductiveTime < CONDUCTIVE_COOLDOWN) return;
        _lastConductiveTime = Time.time;

        DealAreaDamage(go, COMBO_CONDUCTIVE_POISON_DMG, COMBO_CONDUCTIVE_POISON_RADIUS);
        CombatManager.CreateExplosionEffect(go.transform.position, COMBO_CONDUCTIVE_POISON_RADIUS,
            new Color(0.3f, 0.8f, 0.2f), 0.25f);
    }

    /// <summary>
    /// 蒸发组合：燃烧+霜冻 → 蒸汽减速周围敌人
    /// </summary>
    private void TriggerEvaporate(GameObject go)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(go.transform.position, COMBO_EVAPORATE_RADIUS);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy") || hit.gameObject == go) continue;
            var rb = hit.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity *= (1f - COMBO_EVAPORATE_SLOW);
        }
    }

    /// <summary>
    /// 等离子组合：燃烧+雷电且燃烧>10层 → 范围爆发
    /// </summary>
    private void TriggerPlasma(GameObject go)
    {
        if (Time.time - _lastPlasmaTime < PLASMA_COOLDOWN) return;
        _lastPlasmaTime = Time.time;

        DealAreaDamage(go, COMBO_PLASMA_AOE_DMG, COMBO_PLASMA_AOE_RADIUS);
        CombatManager.CreateExplosionEffect(go.transform.position, COMBO_PLASMA_AOE_RADIUS,
            new Color(0.9f, 0.9f, 1f), 0.5f);
        DebugHelper.Log($"[DOT Combo] PLASMA BURST! AOE dmg={COMBO_PLASMA_AOE_DMG}");
    }

    // ═══ 工具方法 ═══

    /// <summary>
    /// 对周围敌人造成范围伤害
    /// </summary>
    private void DealAreaDamage(GameObject source, float damage, float radius)
    {
        var spawnMgr = GameReferences.SpawnManager;
        IReadOnlyList<GameObject> enemies = spawnMgr?.ActiveEnemies;
        if (enemies == null) return;

        float radiusSqr = radius * radius;
        for (int j = 0; j < enemies.Count; j++)
        {
            var enemy = enemies[j];
            if (enemy == null || enemy == source || !enemy.activeInHierarchy) continue;
            Vector2 delta = (Vector2)(enemy.transform.position - source.transform.position);
            if (delta.sqrMagnitude > radiusSqr) continue;
            var hitDmg = enemy.GetComponent<Damageable>();
            if (hitDmg != null && hitDmg.CurrentHp > 0)
                hitDmg.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(damage)));
        }
    }
}