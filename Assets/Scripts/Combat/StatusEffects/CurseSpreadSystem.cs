using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 诅咒传播系统 — 从 StatusEffectManager 拆分而来
/// 负责敌人死亡时传播DOT给附近敌人
/// </summary>
public static class CurseSpreadSystem
{
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
        var magePassive = GameReferences.Player?.GetComponent<MagePassive>();
        if (magePassive == null) return;
        int spreadTargets = magePassive.CurseSpreadTargets;
        if (spreadTargets <= 1) return;

        float range = source.ContaminateRange > 0 ? source.ContaminateRange : 5f;
        var go = source.gameObject;

        // 检查是否有任何DOT效果
        bool hasAnyDot = source.ActiveEffects.Count > 0;
        bool hasBleed = go.GetComponent<BleedEffect>() != null;
        bool hasBurn = go.GetComponent<BurnStackEffect>() != null;
        bool hasPoison = go.GetComponent<PoisonStackEffect>() != null;
        bool hasFrost = go.GetComponent<FrostEffect>() != null;
        if (!hasAnyDot && !hasBleed && !hasBurn && !hasPoison && !hasFrost) return;

        // 找到范围内的敌人
        Collider2D[] hits = Physics2D.OverlapCircleAll(go.transform.position, range);
        var validTargets = new List<Collider2D>();
        foreach (var hit in hits)
        {
            if (hit.gameObject == go) continue;
            if (!hit.CompareTag("Enemy")) continue;
            validTargets.Add(hit);
        }

        // 缓存源DOT组件
        var srcBleed = go.GetComponent<BleedEffect>();
        var srcBurn = go.GetComponent<BurnStackEffect>();
        var srcPoison = go.GetComponent<PoisonStackEffect>();
        var srcFrost = go.GetComponent<FrostEffect>();

        int spreadCount = validTargets.Count;
        for (int i = 0; i < spreadCount; i++)
        {
            var hit = validTargets[i];
            var otherManager = hit.GetComponent<StatusEffectManager>();
            if (otherManager == null)
                otherManager = hit.gameObject.AddComponent<StatusEffectManager>();

            // 传播 StatusEffectManager 中的 DOT（继承10%层数/伤害）
            foreach (var effect in source.ActiveEffects)
            {
                otherManager.ApplyEffect(effect.type, effect.damagePerSecond * 0.1f,
                    effect.remainingDuration * 0.1f, effect.canCrit, effect.critChance, effect.critMultiplier);
            }

            // 传播独立DOT组件
            if (srcBleed != null)
            {
                var otherBleed = hit.GetComponent<BleedEffect>();
                if (otherBleed == null) otherBleed = hit.gameObject.AddComponent<BleedEffect>();
                otherBleed.Refresh(srcBleed._dps * 0.1f, srcBleed._duration * 0.1f,
                    srcBleed._canCrit, srcBleed._critChance, srcBleed._critMult);
            }
            if (srcBurn != null)
            {
                var otherBurn = hit.GetComponent<BurnStackEffect>();
                if (otherBurn == null) otherBurn = hit.gameObject.AddComponent<BurnStackEffect>();
                int stacks = Mathf.Max(1, Mathf.RoundToInt(srcBurn.StackCount * 0.1f));
                for (int s = 0; s < stacks; s++)
                    otherBurn.AddStack(srcBurn._baseDps * 0.1f, srcBurn._duration * 0.1f,
                        srcBurn._canCrit, srcBurn._critChance, srcBurn._critMult);
            }
            if (srcPoison != null)
            {
                var otherPoison = hit.GetComponent<PoisonStackEffect>();
                if (otherPoison == null) otherPoison = hit.gameObject.AddComponent<PoisonStackEffect>();
                int pStacks = Mathf.Max(1, Mathf.RoundToInt(srcPoison.StackCount * 0.1f));
                for (int s = 0; s < pStacks; s++)
                    otherPoison.AddStack(2f, 0f, srcPoison._canCrit, srcPoison._critChance, srcPoison._critMult);
            }
            if (srcFrost != null)
            {
                var otherFrost = hit.GetComponent<FrostEffect>();
                if (otherFrost == null) otherFrost = hit.gameObject.AddComponent<FrostEffect>();
                otherFrost.ApplyFreeze(0.3f, srcFrost._slowPercent * 0.1f,
                    srcFrost._frostDps * 0.1f, srcFrost._canCrit, srcFrost._critChance, srcFrost._critMult);
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