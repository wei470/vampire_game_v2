using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DOT 视觉效果管理器 — 管理敌人受 DOT 时的颜色变化和粒子效果。
/// 从 StatusEffectSystem 中提取，减少主文件行数。
///
/// 职责：
/// - UpdateVisual: 根据活跃 DOT 类型改变敌人颜色（脉冲闪烁）
/// - UpdateDotParticles: 管理 DOT 粒子特效组件
/// </summary>
public static class DotVisualEffectManager
{
    /// <summary>
    /// 根据当前活跃的 DOT 效果更新 SpriteRenderer 颜色
    /// </summary>
    public static void UpdateVisual(SpriteRenderer sr, List<StatusEffect> activeEffects, Color originalColor)
    {
        if (sr == null || activeEffects.Count == 0) return;
        Color ec = originalColor;
        var p = activeEffects[0];
        switch (p.type)
        {
            case StatusEffectType.Bleed: case StatusEffectType.Rend: ec = Color.Lerp(originalColor, new Color(0.8f, 0.1f, 0.1f), 0.6f); break;
            case StatusEffectType.Poison: ec = Color.Lerp(originalColor, new Color(0.1f, 0.9f, 0.1f), 0.6f); break;
            case StatusEffectType.Burn: case StatusEffectType.Immolate: ec = Color.Lerp(originalColor, new Color(1f, 0.4f, 0f), 0.6f); break;
            case StatusEffectType.Frostbite: ec = Color.Lerp(originalColor, new Color(0.3f, 0.6f, 1f), 0.6f); break;
            case StatusEffectType.Curse: case StatusEffectType.Wither: ec = Color.Lerp(originalColor, new Color(0.4f, 0f, 0.6f), 0.6f); break;
            case StatusEffectType.Agony: ec = Color.Lerp(originalColor, new Color(0.6f, 0f, 0.3f), 0.6f); break;
            case StatusEffectType.Radiate: ec = Color.Lerp(originalColor, new Color(0f, 1f, 0.5f), 0.6f); break;
            case StatusEffectType.Contaminate: ec = Color.Lerp(originalColor, new Color(0.3f, 0.5f, 0.3f), 0.6f); break;
            case StatusEffectType.WindErosion: ec = Color.Lerp(originalColor, new Color(0.7f, 0.85f, 1f), 0.6f); break;
        }
        float pulse = Mathf.Sin(Time.time * 6f) * 0.15f;
        sr.color = Color.Lerp(ec, originalColor, 0.3f + pulse);
    }

    /// <summary>
    /// 更新 DOT 粒子视觉效果（创建/销毁 DotParticleVFX 组件）
    /// </summary>
    public static void UpdateDotParticles(GameObject target, List<StatusEffect> activeEffects, ref DotParticleVFX dotVFX)
    {
        bool hasBleed = false, hasPoison = false, hasBurn = false, hasFrost = false;
        for (int i = 0; i < activeEffects.Count; i++)
        {
            switch (activeEffects[i].type)
            {
                case StatusEffectType.Bleed: hasBleed = true; break;
                case StatusEffectType.Poison: hasPoison = true; break;
                case StatusEffectType.Burn: case StatusEffectType.Immolate: hasBurn = true; break;
                case StatusEffectType.Frostbite: hasFrost = true; break;
            }
        }
        // 独立组件检测
        if (!hasBleed && target.GetComponent<BleedEffect>() != null) hasBleed = true;
        if (!hasPoison && target.GetComponent<PoisonStackEffect>() != null) hasPoison = true;
        if (!hasBurn && target.GetComponent<BurnStackEffect>() != null) hasBurn = true;
        if (!hasFrost && target.GetComponent<FrostEffect>() != null) hasFrost = true;

        if (!hasBleed && !hasPoison && !hasBurn && !hasFrost)
        {
            if (dotVFX != null) { Object.Destroy(dotVFX); dotVFX = null; }
            return;
        }
        if (dotVFX == null) dotVFX = target.GetComponent<DotParticleVFX>();
        if (dotVFX == null) dotVFX = target.AddComponent<DotParticleVFX>();
        dotVFX.UpdateEffects(hasBleed, hasPoison, hasBurn, hasFrost);
    }
}