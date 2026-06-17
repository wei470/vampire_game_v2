using UnityEngine;

/// <summary>
/// DOT 元素颜色常量 — 集中管理所有子弹/效果的颜色字面量。
/// 替代散落在各文件中的 new Color(r,g,b) 硬编码。
/// </summary>
public static class DotPalette
{
    // ── 子弹本体颜色 ──
    public static readonly Color BurnOrange = new Color(1f, 0.4f, 0f, 0.6f);
    public static readonly Color PoisonGreen = new Color(0.1f, 0.9f, 0.2f);
    public static readonly Color FrostBlue = new Color(0.3f, 0.6f, 1f);
    public static readonly Color LightningCyan = new Color(0.3f, 0.8f, 1f);
    public static readonly Color DarkPurple = new Color(0.4f, 0.1f, 0.6f);
    public static readonly Color WindBlue = new Color(0.7f, 0.85f, 1f);

    // ── 拖尾颜色 ──
    public static readonly Color BurnTrail = new Color(1f, 0.4f, 0f, 0.4f);
    public static readonly Color PoisonTrail = new Color(0.1f, 0.9f, 0.2f, 0.6f);
    public static readonly Color FrostTrailStart = new Color(0.5f, 0.8f, 1f, 0.7f);
    public static readonly Color FrostTrailEnd = new Color(0.3f, 0.6f, 1f, 0f);
    public static readonly Color LightningTrailStart = new Color(0.4f, 0.8f, 1f, 0.8f);
    public static readonly Color LightningTrailEnd = new Color(0.2f, 0.5f, 1f, 0f);
    public static readonly Color DarkTrail = new Color(0.4f, 0.1f, 0.6f, 0.7f);

    // ── 元素反应颜色 ──
    public static readonly Color MeltOrange = new Color(1f, 0.3f, 0f);
    public static readonly Color MeltExplosion = new Color(1f, 0.3f, 0f, 0.6f);
    public static readonly Color PoisonBurstPurple = new Color(0.4f, 0.1f, 0.6f, 0.6f);
    public static readonly Color BurnSpreadOrange = new Color(1f, 0.5f, 0f, 0.5f);
    public static readonly Color BurnSpreadExplosion = new Color(1f, 0.5f, 0f, 0.4f);

    // ── 紫电颜色 ──
    public static readonly Color PurpleSprite = new Color(0.12f, 0.02f, 0.18f);
    public static readonly Color PurpleFx = new Color(0.5f, 0.1f, 0.8f, 0.7f);
    public static readonly Color PurpleTrailStart = new Color(0.5f, 0.05f, 0.7f, 0.9f);
    public static readonly Color PurpleTrailEnd = new Color(0.15f, 0f, 0.25f, 0f);

    // ── 伤害数字颜色 ──
    public static readonly Color DamagePopupBlue = new Color(0.3f, 0.5f, 1f);
    public static readonly Color DamagePopupOrange = new Color(1f, 0.5f, 0f);
    public static readonly Color DamagePopupGreen = new Color(0.1f, 0.8f, 0.1f);
    public static readonly Color DamagePopupFrost = new Color(0.3f, 0.6f, 1f);

    // ── VFX 颜色 ──
    public static readonly Color ExplosionBlue = new Color(0.4f, 0.8f, 1f);
    public static readonly Color ChainLineStart = new Color(0.5f, 0.8f, 1f, 0.9f);
    public static readonly Color ChainLineEnd = new Color(0.3f, 0.6f, 1f, 0f);
}
