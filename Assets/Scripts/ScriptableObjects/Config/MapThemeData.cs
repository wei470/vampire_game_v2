using UnityEngine;

/// <summary>
/// 地图主题数据 ScriptableObject，定义地图主题的所有配置。
/// 对应 Python: game/map_themes.py 中的主题配置
/// 
/// 5 种主题：Forest, Dungeon, Desert, Volcano, Ice
/// 每 5 波切换，波次 3+ 生效
/// 
/// 使用方式：在 Assets/ScriptableObjects/Config/ 下创建资源
/// </summary>
[CreateAssetMenu(fileName = "NewMapTheme", menuName = "VampireGame/Map Theme Data")]
public class MapThemeData : ScriptableObject
{
    [Header("基础信息")]
    public string themeName = "New Theme";
    public string description = "A map theme";
    public int themeId;
    public Sprite icon;

    [Header("主题类型")]
    public MapThemeType themeType = MapThemeType.Forest;

    [Header("视觉配置")]
    [Tooltip("摄像机背景色")]
    public Color backgroundColor = Color.black;

    [Tooltip("网格线颜色")]
    public Color gridColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Tooltip("网格线透明度")]
    [Range(0f, 1f)]
    public float gridAlpha = 0.3f;

    [Tooltip("网格单元格大小")]
    public float gridCellSize = 1f;

    [Tooltip("网格线粗细")]
    [Range(0.01f, 0.2f)]
    public float gridLineWidth = 0.05f;

    [Header("环境区域配置")]
    [Tooltip("该主题中可能出现的环境区域类型")]
    public EnvironmentZoneType[] availableZones;

    [Tooltip("环境区域生成概率 (0-1)")]
    [Range(0f, 1f)]
    public float zoneSpawnChance = 0.3f;

    [Tooltip("每波最多生成的区域数")]
    public int maxZonesPerWave = 3;

    [Header("装饰物配置")]
    [Tooltip("该主题的装饰物类型")]
    public DecorationType[] decorations;

    [Tooltip("装饰物密度（每 10x10 单位面积的数量）")]
    [Range(0f, 20f)]
    public float decorationDensity = 5f;

    [Tooltip("装饰物生成范围（以玩家为中心的半径）")]
    public float decorationRadius = 25f;

    [Tooltip("装饰物最小不透明度")]
    [Range(0f, 1f)]
    public float decorationMinAlpha = 0.6f;

    [Tooltip("装饰物最大不透明度")]
    [Range(0f, 1f)]
    public float decorationMaxAlpha = 1f;

    [Header("氛围配置")]
    [Tooltip("环境色调叠加")]
    public Color ambientTint = Color.white;

    [Tooltip("雾气密度（0=无雾）")]
    [Range(0f, 1f)]
    public float fogDensity = 0f;

    [Tooltip("雾气颜色")]
    public Color fogColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    /// <summary>
    /// 地图主题类型枚举
    /// </summary>
    public enum MapThemeType
    {
        Forest,     // 森林 - 绿色背景 + 树木/岩石
        Dungeon,    // 地城 - 暗色背景 + 火把/石柱
        Desert,     // 沙漠 - 黄色背景 + 仙人掌/岩石
        Volcano,    // 火山 - 红色背景 + 岩浆/黑石
        Ice         // 冰原 - 蓝白背景 + 冰晶/雪堆
    }

    /// <summary>
    /// 环境区域类型枚举
    /// </summary>
    public enum EnvironmentZoneType
    {
        Slow,       // 减速区域
        Damage,     // 伤害区域
        Heal,       // 治疗区域
        Lava        // 岩浆区域（持续高伤害）
    }

    /// <summary>
    /// 装饰物类型枚举
    /// </summary>
    public enum DecorationType
    {
        // 森林
        Tree,           // 树木
        Rock,           // 岩石
        Bush,           // 灌木
        Mushroom,       // 蘑菇
        // 地城
        Torch,          // 火把
        Pillar,         // 石柱
        // 沙漠
        Cactus,         // 仙人掌
        Dune,           // 沙丘
        // 火山
        LavaRock,       // 熔岩石
        Obsidian,       // 黑曜石
        // 冰原
        IceCrystal,     // 冰晶
        SnowPile        // 雪堆
    }
}