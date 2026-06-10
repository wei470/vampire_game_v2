using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 诅咒传播系统 — 从 StatusEffectManager 拆分而来
/// 负责敌人死亡时传播DOT给附近敌人
/// </summary>
public static class CurseSpreadSystem
{
    // ── Mage 专属强化静态字段 ──
    /// <summary>蔓延：DOT传播效率加成（默认0，每层+0.15）</summary>
    public static float PandemicEfficiencyBonus = 0f;
    /// <summary>暗影链接：黑暗标记传播范围加成</summary>
    public static float ShadowLinkRangeBonus = 0f;
    /// <summary>暗影链接：黑暗标记传播效率加成</summary>
    public static float ShadowLinkEffBonus = 0f;

    /// <summary>重置所有静态数据（场景切换时调用）</summary>
    public static void ResetAll()
    {
        PandemicEfficiencyBonus = 0f;
        ShadowLinkRangeBonus = 0f;
        ShadowLinkEffBonus = 0f;
    }

    private static Material _lineMaterial;

    private static Material GetLineMaterial()
    {
        if (_lineMaterial == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null) _lineMaterial = new Material(shader);
        }
        return _lineMaterial;
    }

    /// <summary>
    /// 敌人死亡时传播污染DOT
    /// </summary>
    public static void SpreadContaminate(StatusEffectManager source)
    {
        var player = GameReferences.Player;
        if (player == null || !player.TryGetComponent<MagePassive>(out var magePassive)) return;
        int spreadTargets = magePassive.CurseSpreadTargets;
        if (spreadTargets <= 1) return;

        float range = source.ContaminateRange > 0 ? source.ContaminateRange : 5f;
        var go = source.gameObject;

        // 缓存源DOT组件并检查是否有任何DOT效果
        bool hasAnyDot = source.ActiveEffects.Count > 0;
        go.TryGetComponent<BleedEffect>(out var srcBleed);
        go.TryGetComponent<BurnStackEffect>(out var srcBurn);
        go.TryGetComponent<PoisonStackEffect>(out var srcPoison);
        go.TryGetComponent<FrostEffect>(out var srcFrost);
        if (!hasAnyDot && srcBleed == null && srcBurn == null && srcPoison == null && srcFrost == null) return;

        // ── 使用空间分区查询范围内敌人（替代 Physics2D.OverlapCircleAll）──
        var spawnMgr = GameReferences.SpawnManager;
        var nearbyEnemies = (spawnMgr != null && spawnMgr.ActiveEnemies != null && spawnMgr.ActiveEnemies.Count > 0)
            ? SpatialGrid.QueryRadius((Vector2)go.transform.position, range)
            : null;

        var validTargets = new List<GameObject>(16);
        if (nearbyEnemies != null)
        {
            for (int i = 0; i < nearbyEnemies.Count; i++)
            {
                var e = nearbyEnemies[i];
                if (e == null || e == go || !e.activeInHierarchy) continue;
                if (!e.CompareTag("Enemy")) continue;
                validTargets.Add(e);
            }
        }

        int spreadCount = validTargets.Count;
        for (int i = 0; i < spreadCount; i++)
        {
            var hit = validTargets[i];
            if (!hit.TryGetComponent<StatusEffectManager>(out var otherManager))
                otherManager = hit.gameObject.AddComponent<StatusEffectManager>();

            // 传播效率：基础10% + 蔓延加成（上限100%）
            float spreadRatio = Mathf.Min(1f, 0.1f + PandemicEfficiencyBonus);

            // 传播 StatusEffectManager 中的 DOT
            foreach (var effect in source.ActiveEffects)
            {
                otherManager.ApplyEffect(effect.type, effect.damagePerSecond * spreadRatio,
                    effect.remainingDuration * spreadRatio, effect.canCrit, effect.critChance, effect.critMultiplier);
            }

            // 传播独立DOT组件（使用蔓延加成后的比率）
            if (srcBleed != null)
            {
                if (!hit.TryGetComponent<BleedEffect>(out var otherBleed))
                    otherBleed = hit.gameObject.AddComponent<BleedEffect>();
                otherBleed.Refresh(srcBleed._dps * spreadRatio, srcBleed._duration * spreadRatio,
                    srcBleed._canCrit, srcBleed._critChance, srcBleed._critMult);
            }
            if (srcBurn != null)
            {
                if (!hit.TryGetComponent<BurnStackEffect>(out var otherBurn))
                    otherBurn = hit.gameObject.AddComponent<BurnStackEffect>();
                int stacks = Mathf.Max(1, Mathf.RoundToInt(srcBurn.StackCount * spreadRatio));
                for (int s = 0; s < stacks; s++)
                    otherBurn.AddStack(srcBurn._baseDps * spreadRatio, srcBurn._duration * spreadRatio,
                        srcBurn._canCrit, srcBurn._critChance, srcBurn._critMult);
            }
            if (srcPoison != null)
            {
                if (!hit.TryGetComponent<PoisonStackEffect>(out var otherPoison))
                    otherPoison = hit.gameObject.AddComponent<PoisonStackEffect>();
                int pStacks = Mathf.Max(1, Mathf.RoundToInt(srcPoison.StackCount * spreadRatio));
                for (int s = 0; s < pStacks; s++)
                    otherPoison.AddStack(2f, 0f, srcPoison._canCrit, srcPoison._critChance, srcPoison._critMult);
            }
            if (srcFrost != null)
            {
                if (!hit.TryGetComponent<FrostEffect>(out var otherFrost))
                    otherFrost = hit.gameObject.AddComponent<FrostEffect>();
                otherFrost.ApplyFreeze(0.3f, srcFrost._slowPercent * spreadRatio,
                    srcFrost._frostDps * spreadRatio, srcFrost._canCrit, srcFrost._critChance, srcFrost._critMult);
            }
        }

        // 传播视觉特效
        if (spreadCount > 0)
        {
            for (int i = 0; i < spreadCount; i++)
            {
                var target = validTargets[i];
                if (target == null) continue;
                CreateSpreadLine(go.transform.position, target.transform.position);
            }
            DebugHelper.Log($"[CurseSpreadSystem] Spread to {spreadCount} enemies");
        }
    }

    private static void CreateSpreadLine(Vector3 from, Vector3 to)
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
        Object.Destroy(lineObj, 0.5f);
    }
}