using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DOT 组合效果系统 — 从 StatusEffectManager 拆分而来
/// 检测并应用 DOT 之间的协同效果（碎冰/爆燃/脓毒）
/// </summary>
public class DotComboSystem
{
    // 组合效果常量
    private const float COMBO_DETONATE_POISON_THRESHOLD = 5f;
    private const float COMBO_DETONATE_AOE_RADIUS = 2.5f;
    public const float COMBO_SHATTER_BLEED_MULT = 1.0f;
    private const float COMBO_SEPSIS_BLEED_DPS_PER_POISON = 0.3f;

    public bool ShatterActive { get; private set; }

    private float _lastDetonateComboTime;
    private const float DETONATE_COMBO_COOLDOWN = 3f;

    /// <summary>
    /// 检测并应用 DOT 组合效果
    /// </summary>
    public void CheckComboEffects(List<StatusEffect> activeEffects, GameObject go, SpriteRenderer sr, Color originalColor)
    {
        ShatterActive = false;

        bool hasBleed = false, hasPoison = false, hasBurn = false, hasFrost = false;
        int poisonStacks = 0;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            var e = activeEffects[i];
            switch (e.type)
            {
                case StatusEffectType.Bleed: hasBleed = true; break;
                case StatusEffectType.Poison: hasPoison = true; poisonStacks = e.stackCount; break;
                case StatusEffectType.Burn: hasBurn = true; break;
                case StatusEffectType.Frostbite: hasFrost = true; break;
            }
        }

        // 检查独立DOT组件
        if (!hasBleed && go.GetComponent<BleedEffect>() != null) hasBleed = true;
        if (!hasPoison)
        {
            var ps = go.GetComponent<PoisonStackEffect>();
            if (ps != null) { hasPoison = true; poisonStacks = ps.StackCount; }
        }
        if (!hasBurn && go.GetComponent<BurnStackEffect>() != null) hasBurn = true;
        if (!hasFrost && go.GetComponent<FrostEffect>() != null) hasFrost = true;

        // 碎冰：霜冻+流血 → 冰冻期间流血伤害×2
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
    }

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
            {
                poisonStacks = activeEffects[i].stackCount;
                break;
            }
        }

        float aoeDmg = poisonStacks * 3f;
        var spawnMgr = GameReferences.SpawnManager;
        IReadOnlyList<GameObject> enemies = spawnMgr != null ? spawnMgr.ActiveEnemies : null;
        if (enemies != null)
        {
            float radiusSqr = COMBO_DETONATE_AOE_RADIUS * COMBO_DETONATE_AOE_RADIUS;
            for (int j = 0; j < enemies.Count; j++)
            {
                var enemy = enemies[j];
                if (enemy == null || enemy == go || !enemy.activeInHierarchy) continue;
                Vector2 delta = (Vector2)(enemy.transform.position - go.transform.position);
                if (delta.sqrMagnitude > radiusSqr) continue;
                var hitDmg = enemy.GetComponent<Damageable>();
                if (hitDmg != null && hitDmg.CurrentHp > 0)
                    hitDmg.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(aoeDmg)));
            }
        }

        CombatManager.CreateExplosionEffect(go.transform.position, COMBO_DETONATE_AOE_RADIUS,
            new Color(1f, 0.6f, 0f), 0.4f);
        DebugHelper.Log($"[DOT Combo] DETONATE! Poison stacks={poisonStacks}, AOE dmg={aoeDmg:F0}");
    }
}