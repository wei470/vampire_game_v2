using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// #35 对象池监控 Debug 面板
/// 按 F2 键切换显示/隐藏，仅在 Development Build 中可用
/// 
/// 功能：
/// - 显示每个池的统计：总容量、活跃数、空闲数、峰值使用数
/// - 颜色标记：绿色=正常、黄色=接近容量上限、红色=已溢出
/// - 自动检测"池泄漏"（活跃数持续增长不回收）
/// </summary>
public class DebugPoolMonitor : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Header("显示设置")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.F2;
    [SerializeField] private float _updateInterval = 0.5f; // 数据更新间隔（秒）
    
    private bool _isVisible = false;
    private float _lastUpdateTime;
    
    // 池统计数据缓存
    private Dictionary<string, PoolStats> _statsCache = new Dictionary<string, PoolStats>();
    
    // 泄漏检测历史记录
    private Dictionary<string, LeakDetection> _leakDetection = new Dictionary<string, LeakDetection>();
    
    // GUI 样式
    private GUIStyle _headerStyle;
    private GUIStyle _normalStyle;
    private GUIStyle _warningStyle;
    private GUIStyle _criticalStyle;
    private GUIStyle _leakStyle;
    private GUIStyle _panelStyle;
    private bool _stylesInitialized;
    
    // 滚动位置
    private Vector2 _scrollPosition = Vector2.zero;
    
    /// <summary>
    /// 池统计数据
    /// </summary>
    public struct PoolStats
    {
        public int TotalCreated;
        public int TotalSpawned;
        public int TotalDespawned;
        public int InactiveCount;
        public int ActiveCount;
        public int PeakActiveCount;
        public float LastActiveRatio;
    }
    
    /// <summary>
    /// 泄漏检测数据
    /// </summary>
    private class LeakDetection
    {
        public int LastActiveCount;
        public int ConsecutiveGrowthFrames;
        public bool IsLeaking;
    }

    private void Update()
    {
        // 检测 F2 键切换显示
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.f2Key.wasPressedThisFrame)
        {
            _isVisible = !_isVisible;
            if (_isVisible)
            {
                RefreshStats();
            }
        }
        
        // 定期刷新数据
        if (_isVisible && Time.unscaledTime - _lastUpdateTime >= _updateInterval)
        {
            RefreshStats();
            _lastUpdateTime = Time.unscaledTime;
        }
    }
    
    /// <summary>
    /// 刷新所有池的统计数据
    /// </summary>
    private void RefreshStats()
    {
        if (ObjectPool.Instance == null) return;
        
        _statsCache.Clear();
        string allStats = ObjectPool.Instance.GetAllStats();
        
        // 解析统计字符串获取各池数据
        // 格式: 'PoolKey': Created=X, Spawned=X, Despawned=X, Inactive=X
        string[] lines = allStats.Split('\n');
        foreach (string line in lines)
        {
            if (string.IsNullOrEmpty(line) || line.Contains("===")) continue;
            
            ParsePoolStatsLine(line.Trim());
        }
        
        // 更新泄漏检测
        UpdateLeakDetection();
    }
    
    /// <summary>
    /// 解析单行池统计信息
    /// </summary>
    private void ParsePoolStatsLine(string line)
    {
        // 格式: 'PoolKey': Created=X, Spawned=X, Despawned=X, Inactive=X
        int colonIndex = line.IndexOf(':');
        if (colonIndex < 0) return;
        
        string poolKey = line.Substring(1, colonIndex - 2); // 去掉引号
        string statsPart = line.Substring(colonIndex + 1);
        
        PoolStats stats = new PoolStats();
        
        // 解析各数值
        stats.TotalCreated = ExtractInt(statsPart, "Created=");
        stats.TotalSpawned = ExtractInt(statsPart, "Spawned=");
        stats.TotalDespawned = ExtractInt(statsPart, "Despawned=");
        stats.InactiveCount = ExtractInt(statsPart, "Inactive=");
        
        // 计算活跃数 = 已创建 - 空闲数
        stats.ActiveCount = stats.TotalCreated - stats.InactiveCount;
        
        // 计算活跃比例
        stats.LastActiveRatio = stats.TotalCreated > 0 
            ? (float)stats.ActiveCount / stats.TotalCreated 
            : 0f;
        
        // 更新峰值
        if (_statsCache.ContainsKey(poolKey))
        {
            stats.PeakActiveCount = Mathf.Max(stats.ActiveCount, _statsCache[poolKey].PeakActiveCount);
        }
        else
        {
            stats.PeakActiveCount = stats.ActiveCount;
        }
        
        _statsCache[poolKey] = stats;
    }
    
    /// <summary>
    /// 从字符串中提取整数值
    /// </summary>
    private int ExtractInt(string text, string key)
    {
        int startIndex = text.IndexOf(key);
        if (startIndex < 0) return 0;
        
        startIndex += key.Length;
        int endIndex = text.IndexOf(',', startIndex);
        if (endIndex < 0) endIndex = text.Length;
        
        string valueStr = text.Substring(startIndex, endIndex - startIndex).Trim();
        int result;
        if (int.TryParse(valueStr, out result))
        {
            return result;
        }
        return 0;
    }
    
    /// <summary>
    /// 更新泄漏检测
    /// </summary>
    private void UpdateLeakDetection()
    {
        foreach (var kvp in _statsCache)
        {
            string poolKey = kvp.Key;
            PoolStats stats = kvp.Value;
            
            if (!_leakDetection.ContainsKey(poolKey))
            {
                _leakDetection[poolKey] = new LeakDetection();
            }
            
            var detection = _leakDetection[poolKey];
            
            // 检查活跃数是否持续增长
            if (stats.ActiveCount > detection.LastActiveCount && stats.ActiveCount > 10)
            {
                detection.ConsecutiveGrowthFrames++;
                
                // 连续增长超过阈值视为泄漏
                if (detection.ConsecutiveGrowthFrames >= 10)
                {
                    detection.IsLeaking = true;
                }
            }
            else
            {
                detection.ConsecutiveGrowthFrames = Mathf.Max(0, detection.ConsecutiveGrowthFrames - 1);
                
                // 活跃数下降则取消泄漏标记
                if (stats.ActiveCount < detection.LastActiveCount)
                {
                    detection.IsLeaking = false;
                    detection.ConsecutiveGrowthFrames = 0;
                }
            }
            
            detection.LastActiveCount = stats.ActiveCount;
        }
    }
    
    /// <summary>
    /// 初始化 GUI 样式
    /// </summary>
    private void InitStyles()
    {
        if (_stylesInitialized) return;
        
        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        
        _normalStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = Color.white }
        };
        
        _warningStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = Color.yellow }
        };
        
        _criticalStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = Color.red }
        };
        
        _leakStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.5f, 0.5f) }
        };
        
        _panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTex(1, 1, new Color(0, 0, 0, 0.8f)) }
        };
        
        _stylesInitialized = true;
    }
    
    /// <summary>
    /// 创建纯色纹理
    /// </summary>
    private Texture2D MakeTex(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
    
    private void OnGUI()
    {
        if (!_isVisible) return;
        
        InitStyles();
        
        // 面板位置和大小
        float panelWidth = 450f;
        float panelHeight = Mathf.Min(Screen.height - 40f, 600f);
        float panelX = Screen.width - panelWidth - 10f;
        float panelY = 10f;
        
        GUILayout.BeginArea(new Rect(panelX, panelY, panelWidth, panelHeight), _panelStyle);
        
        // 标题
        GUILayout.BeginHorizontal();
        GUILayout.Label($"📊 对象池监控 [{_statsCache.Count} 个池]", _headerStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label($"按 {_toggleKey} 关闭", _normalStyle);
        GUILayout.EndHorizontal();
        
        // 总览信息
        DrawSummary();
        
        GUILayout.Space(5);
        
        // 滚动区域显示各池详情
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(panelHeight - 120f));
        
        // 按池名称排序显示
        List<string> sortedKeys = new List<string>(_statsCache.Keys);
        sortedKeys.Sort();
        
        foreach (string poolKey in sortedKeys)
        {
            DrawPoolEntry(poolKey, _statsCache[poolKey]);
        }
        
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
    
    /// <summary>
    /// 绘制总览信息
    /// </summary>
    private void DrawSummary()
    {
        int totalCreated = 0;
        int totalActive = 0;
        int totalInactive = 0;
        int leakingCount = 0;
        
        foreach (var kvp in _statsCache)
        {
            totalCreated += kvp.Value.TotalCreated;
            totalActive += kvp.Value.ActiveCount;
            totalInactive += kvp.Value.InactiveCount;
            
            if (_leakDetection.ContainsKey(kvp.Key) && _leakDetection[kvp.Key].IsLeaking)
            {
                leakingCount++;
            }
        }
        
        GUILayout.BeginHorizontal();
        GUILayout.Label($"总对象: {totalCreated}", _normalStyle);
        GUILayout.Label($"活跃: {totalActive}", totalActive > totalCreated * 0.8f ? _warningStyle : _normalStyle);
        GUILayout.Label($"空闲: {totalInactive}", _normalStyle);
        
        if (leakingCount > 0)
        {
            GUILayout.Label($"⚠ 泄漏: {leakingCount}", _leakStyle);
        }
        GUILayout.EndHorizontal();
    }
    
    /// <summary>
    /// 绘制单个池的统计信息
    /// </summary>
    private void DrawPoolEntry(string poolKey, PoolStats stats)
    {
        // 确定颜色状态
        GUIStyle keyStyle = _normalStyle;
        string statusIcon = "🟢";
        
        float usageRatio = stats.TotalCreated > 0 ? (float)stats.ActiveCount / stats.TotalCreated : 0f;
        
        if (usageRatio > 0.9f)
        {
            keyStyle = _criticalStyle;
            statusIcon = "🔴";
        }
        else if (usageRatio > 0.7f)
        {
            keyStyle = _warningStyle;
            statusIcon = "🟡";
        }
        
        // 检查泄漏
        bool isLeaking = _leakDetection.ContainsKey(poolKey) && _leakDetection[poolKey].IsLeaking;
        if (isLeaking)
        {
            statusIcon = "💀";
            keyStyle = _leakStyle;
        }
        
        // 绘制池信息
        GUILayout.BeginVertical(GUI.skin.box);
        
        // 池名称行
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{statusIcon} {poolKey}", keyStyle, GUILayout.Width(200));
        
        // 活跃数条形图
        DrawMiniBar(usageRatio, 80f);
        
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        
        // 详细数据行
        GUILayout.BeginHorizontal();
        GUILayout.Label($"  总容量: {stats.TotalCreated}", _normalStyle, GUILayout.Width(90));
        GUILayout.Label($"活跃: {stats.ActiveCount}", stats.ActiveCount > 50 ? _warningStyle : _normalStyle, GUILayout.Width(70));
        GUILayout.Label($"空闲: {stats.InactiveCount}", _normalStyle, GUILayout.Width(70));
        GUILayout.Label($"峰值: {stats.PeakActiveCount}", _normalStyle, GUILayout.Width(70));
        GUILayout.Label($"出池: {stats.TotalSpawned}", _normalStyle, GUILayout.Width(80));
        GUILayout.Label($"回池: {stats.TotalDespawned}", _normalStyle, GUILayout.Width(80));
        GUILayout.EndHorizontal();
        
        // 泄漏警告
        if (isLeaking)
        {
            GUILayout.Label($"  ⚠️ 检测到可能的内存泄漏！活跃数持续增长 ({stats.ActiveCount})", _leakStyle);
        }
        
        GUILayout.EndVertical();
    }
    
    /// <summary>
    /// 绘制迷你进度条
    /// </summary>
    private void DrawMiniBar(float ratio, float width)
    {
        Rect barRect = GUILayoutUtility.GetRect(width, 14, GUILayout.Width(width));
        
        // 背景
        EditorGUI_DrawRect(barRect, new Color(0.2f, 0.2f, 0.2f));
        
        // 填充
        Color fillColor = Color.green;
        if (ratio > 0.9f) fillColor = Color.red;
        else if (ratio > 0.7f) fillColor = Color.yellow;
        
        Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * Mathf.Clamp01(ratio), barRect.height);
        EditorGUI_DrawRect(fillRect, fillColor);
        
        // 边框
        GUI.Box(barRect, "");
        
        // 百分比文字
        GUIStyle percentStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        GUI.Label(barRect, $"{(ratio * 100):0}%", percentStyle);
    }
    
    /// <summary>
    /// 绘制矩形（兼容方法）
    /// </summary>
    private void EditorGUI_DrawRect(Rect rect, Color color)
    {
        // 在运行时使用 GUI.DrawTexture
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        GUI.DrawTexture(rect, tex);
        Destroy(tex);
    }
    
    /// <summary>
    /// 获取指定池的统计信息（供外部使用）
    /// </summary>
    public PoolStats GetPoolStats(string poolKey)
    {
        if (_statsCache.ContainsKey(poolKey))
        {
            return _statsCache[poolKey];
        }
        return default;
    }
    
    /// <summary>
    /// 检查指定池是否有泄漏嫌疑
    /// </summary>
    public bool IsPoolLeaking(string poolKey)
    {
        return _leakDetection.ContainsKey(poolKey) && _leakDetection[poolKey].IsLeaking;
    }
    
#else
    // Release Build 中为空实现
    private void Update() { }
    private void OnGUI() { }
#endif
}