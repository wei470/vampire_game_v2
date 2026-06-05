/// <summary>
/// 磁铁范围倍率系统 — 从 LevelUpUI 拆分而来
/// 负责管理全局磁铁拾取范围倍率
/// </summary>
public static class MagnetMultiplierSystem
{
    /// <summary>
    /// 全局磁铁范围倍率（由升级系统修改，XPGem/Coin 读取）
    /// </summary>
    public static float MagnetRangeMultiplier { get; private set; } = 1f;

    /// <summary>
    /// 应用磁铁范围升级（+30%）
    /// </summary>
    public static void ApplyMagnetRangeUp()
    {
        MagnetRangeMultiplier *= 1.3f;
        DebugHelper.Log("[MagnetMultiplier] Magnet range +30%");
    }

    /// <summary>
    /// 重置磁铁倍率（返回菜单/游戏结束时调用）
    /// </summary>
    public static void Reset()
    {
        MagnetRangeMultiplier = 1f;
    }
}