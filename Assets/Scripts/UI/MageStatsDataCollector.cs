using UnityEngine;

/// <summary>
/// Mage 统计数据收集器 — 计算 DPS 和提供 DOT 类型辅助信息。
/// 从 MageStatsHUD 中提取，减少主文件行数。
///
/// 职责：
/// - CalculateCurrentDPS: 计算当前所有 DOT 子弹的理论总 DPS
/// - DOT 类型颜色/名称/纹理映射
/// - 纹理创建工具
/// </summary>
public static class MageStatsDataCollector
{
    // DOT 颜色
    public static readonly Color BleedColor = new Color(0.9f, 0.2f, 0.2f);
    public static readonly Color PoisonColor = new Color(0.2f, 0.85f, 0.3f);
    public static readonly Color BurnColor = new Color(1f, 0.55f, 0.1f);
    public static readonly Color FrostColor = new Color(0.4f, 0.75f, 1f);

    // DPS 计算缓存
    private static float _cachedTotalDps;
    private static float _lastDpsCalcTime;
    private const float DPS_CALC_INTERVAL = 0.5f;

    /// <summary>
    /// 计算当前所有 DOT 子弹的理论总 DPS
    /// </summary>
    public static float CalculateCurrentDPS(MagePassive magePassive)
    {
        if (magePassive == null) return 0f;

        // 缓存 DPS 计算，避免每帧开销
        if (Time.time - _lastDpsCalcTime < DPS_CALC_INTERVAL)
            return _cachedTotalDps;

        _lastDpsCalcTime = Time.time;
        _cachedTotalDps = 0f;

        var guns = magePassive.DotGuns;
        if (guns == null) return 0f;

        float dmgMult = magePassive.GetDotDamageMultiplier();
        float critChance = magePassive.GetDotCritChance();
        float critMult = magePassive.GetDotCritMultiplier();

        for (int i = 0; i < guns.Count; i++)
        {
            var gun = guns[i];
            float critExpected = 1f + critChance * (critMult - 1f);
            float effectiveDps = gun.dotDps * dmgMult * critExpected;
            _cachedTotalDps += effectiveDps;
        }

        return _cachedTotalDps;
    }

    /// <summary>
    /// 重置缓存（场景重置时调用）
    /// </summary>
    public static void ResetCache()
    {
        _cachedTotalDps = 0f;
        _lastDpsCalcTime = 0f;
    }

    /// <summary>
    /// 获取 DOT 类型对应的颜色
    /// </summary>
    public static Color GetDotColor(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Bleed: return BleedColor;
            case StatusEffectType.Poison: return PoisonColor;
            case StatusEffectType.Burn: return BurnColor;
            case StatusEffectType.Frostbite: return FrostColor;
            default: return new Color(0.95f, 0.9f, 1f);
        }
    }

    /// <summary>
    /// 获取 DOT 类型名称（带 emoji）
    /// </summary>
    public static string GetDotTypeName(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Bleed: return "🔴 Bleed";
            case StatusEffectType.Poison: return "🟢 Poison";
            case StatusEffectType.Burn: return "🟠 Burn";
            case StatusEffectType.Frostbite: return "🔵 Frost";
            default: return type.ToString();
        }
    }

    /// <summary>
    /// 创建单像素纹理
    /// </summary>
    public static Texture2D MakeTex(Color color)
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return tex;
    }
}