using System.Collections.Generic;

/// <summary>
/// Build 路线推荐器 — 管理升级选项的 Build 路线分类和推荐计算。
/// 从 LevelUpOptionGenerator 中提取，减少主文件行数。
///
/// 职责：
/// - ClassifyUpgradeRoute: 根据 upgradeId 分类 Build 路线
/// - CalculateRecommendations: 计算路线完成度并标记推荐
/// - GetBuildRouteTag: 获取路线标签文字
/// </summary>
public static class BuildPathRecommender
{
    /// <summary>
    /// 根据 upgradeId 分类 Build 路线
    /// </summary>
    public static LevelUpOptionGenerator.BuildRoute ClassifyUpgradeRoute(string upgradeId)
    {
        switch (upgradeId)
        {
            case "bleed": case "poison": case "burn": case "frostbite":
            case "static": case "dark": case "light": case "wind":
            case "corrosion": case "curse": case "agony": case "wither": case "erosion":
                return LevelUpOptionGenerator.BuildRoute.DotType;
            case "radiate": case "contaminate":
                return LevelUpOptionGenerator.BuildRoute.Detonate;
            case "haste": case "barrage": case "ricochet":
                return LevelUpOptionGenerator.BuildRoute.Bullet;
        }
        return LevelUpOptionGenerator.BuildRoute.None;
    }

    /// <summary>
    /// 计算 Build 路线完成度并标记推荐选项
    /// </summary>
    public static void CalculateRecommendations(IDotCharacterPassive dotPassive, Dictionary<string, int> upgradeStacks,
        List<LevelUpOptionGenerator.UpgradeSlot> slots)
    {
        if (dotPassive == null) return;

        int dotGunCount = dotPassive.DotGuns.Count;
        int dotEnhanceTypes = 0;
        if (upgradeStacks.ContainsKey("corrosion")) dotEnhanceTypes++;
        if (upgradeStacks.ContainsKey("curse")) dotEnhanceTypes++;
        if (upgradeStacks.ContainsKey("agony")) dotEnhanceTypes++;
        if (upgradeStacks.ContainsKey("wither")) dotEnhanceTypes++;
        if (upgradeStacks.ContainsKey("erosion")) dotEnhanceTypes++;
        float dotProgress = (dotGunCount / 7f) * 0.5f + (dotEnhanceTypes / 5f) * 0.5f;

        int detCount = 0;
        if (upgradeStacks.ContainsKey("radiate")) detCount++;
        if (upgradeStacks.ContainsKey("contaminate")) detCount++;
        float detProgress = detCount / 2f;

        int bulletCount = 0;
        if (upgradeStacks.ContainsKey("haste")) bulletCount++;
        if (upgradeStacks.ContainsKey("barrage")) bulletCount++;
        if (upgradeStacks.ContainsKey("ricochet")) bulletCount++;
        float bulletProgress = bulletCount / 3f;

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot.buildRoute == LevelUpOptionGenerator.BuildRoute.None) continue;
            bool recommend = false;
            switch (slot.buildRoute)
            {
                case LevelUpOptionGenerator.BuildRoute.DotType: recommend = dotProgress > 0.5f; break;
                case LevelUpOptionGenerator.BuildRoute.Detonate: recommend = detProgress > 0.5f; break;
                case LevelUpOptionGenerator.BuildRoute.Bullet: recommend = bulletProgress > 0.5f; break;
            }
            if (recommend) { slot.isRecommended = true; slots[i] = slot; }
        }

        DebugHelper.Log($"[BuildPathRecommender] DOT:{dotProgress:P0} Det:{detProgress:P0} Bullet:{bulletProgress:P0}");
    }

    /// <summary>
    /// 获取路线标签文字
    /// </summary>
    public static string GetBuildRouteTag(LevelUpOptionGenerator.BuildRoute route, bool isRecommended)
    {
        string tag = "";
        switch (route)
        {
            case LevelUpOptionGenerator.BuildRoute.DotType: tag = "[DOT]"; break;
            case LevelUpOptionGenerator.BuildRoute.Detonate: tag = "[DETO]"; break;
            case LevelUpOptionGenerator.BuildRoute.Bullet: tag = "[BULLET]"; break;
        }
        if (isRecommended && route != LevelUpOptionGenerator.BuildRoute.None) return $"💡{tag} 推荐";
        return tag;
    }
}