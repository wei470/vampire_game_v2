using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 地图主题管理器，管理5种地图主题的切换和视觉更新。
/// 对应 Python: game/map_themes.py 中的主题管理逻辑
/// 
/// 主题切换规则：
/// - 波次 1-2：使用默认主题（Forest）
/// - 波次 3 开始：每 5 波切换一次主题
///   - 波 3-7: Forest
///   - 波 8-12: Dungeon
///   - 波 13-17: Desert
///   - 波 18-22: Volcano
///   - 波 23-27: Ice
///   - 波 28+: 循环
/// 
/// 使用方式：挂载到场景中的 GameObject，或自动通过 Singleton 创建
/// </summary>
public class MapThemeManager : Singleton<MapThemeManager>
{
    [Header("主题配置")]
    [Tooltip("5种地图主题数据，按顺序：Forest, Dungeon, Desert, Volcano, Ice")]
    [SerializeField] private List<MapThemeData> _themes = new List<MapThemeData>();

    [Header("主题切换规则")]
    [Tooltip("从第几波开始应用主题")]
    [SerializeField] private int _themeStartWave = 3;

    [Tooltip("每多少波切换一次主题")]
    [SerializeField] private int _wavesPerTheme = 5;

    [Header("随机种子")]
    [Tooltip("地图随机种子。0 = 每局随机，>0 = 固定种子（可复现）")]
    [SerializeField] private int _mapSeed = 0;

    [Tooltip("是否随机打乱主题顺序")]
    [SerializeField] private bool _shuffleThemeOrder = true;

    [Header("运行时状态")]
    [SerializeField] private int _currentThemeIndex = -1;
    [SerializeField] private string _currentThemeName = "None";

    // #12 随机化后的主题顺序映射（原始索引 → 随机化索引）
    private int[] _themeOrderMap = null;
    private System.Random _rng;

    /// <summary>
    /// 当前主题数据（只读）
    /// </summary>
    public MapThemeData CurrentTheme => _currentThemeIndex >= 0 && _currentThemeIndex < _themes.Count
        ? _themes[_currentThemeIndex]
        : null;

    /// <summary>
    /// 当前主题索引
    /// </summary>
    public int CurrentThemeIndex => _currentThemeIndex;

    /// <summary>
    /// 当前主题名称
    /// </summary>
    public string CurrentThemeName => _currentThemeName;

    /// <summary>
    /// 总主题数
    /// </summary>
    public int ThemeCount => _themes.Count;

    /// <summary>
    /// 主题变化事件，参数：(新主题数据, 主题索引)
    /// </summary>
    public static event System.Action<MapThemeData, int> OnMapThemeChanged;

    protected override void Awake()
    {
        base.Awake();
        InitializeRandomSeed();
    }

    /// <summary>
    /// #12 初始化随机种子和主题顺序
    /// </summary>
    private void InitializeRandomSeed()
    {
        int seed = _mapSeed > 0 ? _mapSeed : (int)(Time.realtimeSinceStartup * 1000) ^ System.Environment.TickCount;
        _rng = new System.Random(seed);
        DebugHelper.Log($"[MapThemeManager] Initialized with seed: {seed}");

        if (_shuffleThemeOrder && _themes.Count > 1)
        {
            ShuffleThemeOrder();
        }
    }

    /// <summary>
    /// #12 Fisher-Yates 洗牌打乱主题顺序
    /// </summary>
    private void ShuffleThemeOrder()
    {
        int count = _themes.Count;
        _themeOrderMap = new int[count];
        for (int i = 0; i < count; i++)
            _themeOrderMap[i] = i;

        // Fisher-Yates 洗牌
        for (int i = count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            int temp = _themeOrderMap[i];
            _themeOrderMap[i] = _themeOrderMap[j];
            _themeOrderMap[j] = temp;
        }

        // 记录随机化后的顺序
        var orderNames = new System.Text.StringBuilder();
        for (int i = 0; i < count; i++)
        {
            if (i > 0) orderNames.Append(" → ");
            int idx = _themeOrderMap[i];
            orderNames.Append(idx < _themes.Count ? _themes[idx].themeName : "?");
        }
        DebugHelper.Log($"[MapThemeManager] Theme order shuffled: {orderNames}");
    }

    private void OnEnable()
    {
        EventManager.OnWaveStart += OnWaveStart;
    }

    private void OnDisable()
    {
        EventManager.OnWaveStart -= OnWaveStart;
    }

    /// <summary>
    /// 波次开始时检查是否需要切换主题
    /// </summary>
    private void OnWaveStart(int waveNumber)
    {
        int newThemeIndex = CalculateThemeIndex(waveNumber);

        if (newThemeIndex != _currentThemeIndex)
        {
            SwitchTheme(newThemeIndex);
        }
    }

    /// <summary>
    /// 根据波次号计算应该使用的主题索引
    /// </summary>
    /// <param name="waveNumber">当前波次</param>
    /// <returns>主题索引，-1表示使用默认</returns>
    public int CalculateThemeIndex(int waveNumber)
    {
        if (_themes.Count == 0) return -1;

        // 波次 1-2 使用默认主题（第一个主题）
        if (waveNumber < _themeStartWave)
            return MapThemeOrderIndex(0);

        // 从 _themeStartWave 开始，每 _wavesPerTheme 波切换
        int adjustedWave = waveNumber - _themeStartWave;
        int logicalIndex = (adjustedWave / _wavesPerTheme) % _themes.Count;

        // #12 通过随机化映射表转换索引
        return MapThemeOrderIndex(logicalIndex);
    }

    /// <summary>
    /// #12 将逻辑主题索引映射到随机化后的实际索引
    /// </summary>
    private int MapThemeOrderIndex(int logicalIndex)
    {
        if (_themeOrderMap != null && logicalIndex >= 0 && logicalIndex < _themeOrderMap.Length)
            return _themeOrderMap[logicalIndex];
        return logicalIndex;
    }

    /// <summary>
    /// #12 设置随机种子（运行时调用）
    /// </summary>
    public void SetSeed(int seed)
    {
        _mapSeed = seed;
        InitializeRandomSeed();
    }

    /// <summary>
    /// #12 获取当前使用的随机种子
    /// </summary>
    public int CurrentSeed => _mapSeed;

    /// <summary>
    /// 切换到指定主题
    /// </summary>
    /// <param name="themeIndex">目标主题索引</param>
    public void SwitchTheme(int themeIndex)
    {
        if (themeIndex < 0 || themeIndex >= _themes.Count)
        {
            DebugHelper.LogWarning($"[MapThemeManager] Invalid theme index: {themeIndex}, themes count: {_themes.Count}");
            return;
        }

        _currentThemeIndex = themeIndex;
        MapThemeData theme = _themes[themeIndex];
        _currentThemeName = theme.themeName;

        DebugHelper.Log($"[MapThemeManager] Theme switched to: {theme.themeName} ({theme.themeType}) at index {themeIndex}");

        // 应用主题视觉效果
        ApplyThemeVisuals(theme);

        // 触发事件
        OnMapThemeChanged?.Invoke(theme, themeIndex);
        EventManager.TriggerThemeChanged(theme.themeName);
    }

    /// <summary>
    /// 应用主题的视觉效果（背景色、网格等）
    /// </summary>
    /// <param name="theme">目标主题数据</param>
    private void ApplyThemeVisuals(MapThemeData theme)
    {
        // 设置摄像机背景色
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.backgroundColor = theme.backgroundColor;
        }

        DebugHelper.Log($"[MapThemeManager] Applied visuals for {theme.themeName}: bg={theme.backgroundColor}, grid={theme.gridColor}");
    }

    /// <summary>
    /// 获取主题数据（通过索引）
    /// </summary>
    /// <param name="index">主题索引</param>
    /// <returns>主题数据，索引无效返回 null</returns>
    public MapThemeData GetTheme(int index)
    {
        if (index < 0 || index >= _themes.Count) return null;
        return _themes[index];
    }

    /// <summary>
    /// 获取主题数据（通过类型）
    /// </summary>
    /// <param name="themeType">主题类型</param>
    /// <returns>主题数据，未找到返回 null</returns>
    public MapThemeData GetThemeByType(MapThemeData.MapThemeType themeType)
    {
        foreach (var theme in _themes)
        {
            if (theme.themeType == themeType)
                return theme;
        }
        return null;
    }

    /// <summary>
    /// 手动添加主题数据（用于代码创建场景时）
    /// </summary>
    /// <param name="theme">要添加的主题数据</param>
    public void AddTheme(MapThemeData theme)
    {
        if (theme != null && !_themes.Contains(theme))
        {
            _themes.Add(theme);
        }
    }

    /// <summary>
    /// 清除所有主题
    /// </summary>
    public void ClearThemes()
    {
        _themes.Clear();
        _currentThemeIndex = -1;
        _currentThemeName = "None";
    }

    /// <summary>
    /// 获取指定波次对应的主题名称（不切换，仅查询）
    /// </summary>
    /// <param name="waveNumber">波次号</param>
    /// <returns>主题名称</returns>
    public string GetThemeNameForWave(int waveNumber)
    {
        int index = CalculateThemeIndex(waveNumber);
        if (index >= 0 && index < _themes.Count)
            return _themes[index].themeName;
        return "Default";
    }

    /// <summary>
    /// 获取主题在指定波次的下一个切换波次
    /// </summary>
    /// <param name="currentWave">当前波次</param>
    /// <returns>下一个切换波次号</returns>
    public int GetNextThemeSwitchWave(int currentWave)
    {
        if (currentWave < _themeStartWave)
            return _themeStartWave;

        int adjustedWave = currentWave - _themeStartWave;
        int cycles = adjustedWave / _wavesPerTheme;
        return _themeStartWave + (cycles + 1) * _wavesPerTheme;
    }

    /// <summary>
    /// 重置主题管理器
    /// </summary>
    public void ResetThemes()
    {
        _currentThemeIndex = -1;
        _currentThemeName = "None";

        // 重置摄像机背景色为黑色
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.backgroundColor = Color.black;
        }

        DebugHelper.Log("[MapThemeManager] Reset to default.");
    }
}