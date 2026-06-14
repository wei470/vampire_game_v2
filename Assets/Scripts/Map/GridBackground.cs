using UnityEngine;

/// <summary>
/// 网格背景渲染器，使用程序化方式绘制跟随摄像机的网格线。
/// 随主题切换动态更新网格颜色和样式。
/// 
/// 对应 Python: game/grid.py 中的网格绘制逻辑
/// 
/// 使用方式：挂载到摄像机上，或挂载到独立 GameObject
/// </summary>
[RequireComponent(typeof(Camera))]
public class GridBackground : MonoBehaviour
{
    [Header("网格配置")]
    [Tooltip("网格单元格大小")]
    [SerializeField] private float _cellSize = 1f;

    [Tooltip("网格线颜色")]
    [SerializeField] private Color _gridColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);

    [Tooltip("网格线粗细")]
    [SerializeField] private float _lineWidth = 0.05f;

    [Tooltip("网格线排序层")]
    [SerializeField] private int _sortingOrder = -100;

    [Header("跟随配置")]
    [Tooltip("是否跟随摄像机")]
    [SerializeField] private bool _followCamera = true;

    [Tooltip("更新间隔（秒），0=每帧更新")]
    [SerializeField] private float _updateInterval = 0f;

    // 网格线对象
    private GameObject _gridContainer;
    private Camera _camera;
    private float _lastUpdateTime;
    private Vector2 _lastCameraPos;
    private bool _needsUpdate = true;

    // 缓存的网格线对象池
    private System.Collections.Generic.List<GameObject> _lineObjects = new System.Collections.Generic.List<GameObject>();
    private int _activeLineCount = 0;

    // 用于创建线段的材质
    private static Material _lineMaterial;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        CreateGridContainer();
    }

    private void OnEnable()
    {
        // 订阅主题切换事件
        MapThemeManager.OnMapThemeChanged += OnThemeChanged;
        _needsUpdate = true;
    }

    private void OnDisable()
    {
        MapThemeManager.OnMapThemeChanged -= OnThemeChanged;
    }

    private void LateUpdate()
    {
        if (!_followCamera) return;

        // 检查更新间隔
        if (_updateInterval > 0 && Time.time - _lastUpdateTime < _updateInterval)
            return;

        // 检查摄像机是否移动
        Vector2 camPos = _camera.transform.position;
        if (!_needsUpdate && Vector2.Distance(camPos, _lastCameraPos) < 0.01f)
            return;

        _lastCameraPos = camPos;
        _lastUpdateTime = Time.time;
        _needsUpdate = false;

        UpdateGrid();
    }

    /// <summary>
    /// 创建网格容器
    /// </summary>
    private void CreateGridContainer()
    {
        if (_gridContainer != null) return;

        _gridContainer = new GameObject("GridBackground");
        _gridContainer.transform.SetParent(transform);
        _gridContainer.transform.localPosition = new Vector3(0, 0, 10); // 在摄像机前方
    }

    /// <summary>
    /// 更新网格线显示
    /// </summary>
    public void UpdateGrid()
    {
        if (_gridContainer == null) CreateGridContainer();

        // 计算可见区域
        float camHeight = _camera.orthographicSize * 2f;
        float camWidth = camHeight * _camera.aspect;
        Vector3 camPos = _camera.transform.position;

        // 扩展范围以避免边缘闪烁
        float expandX = camWidth * 0.5f + _cellSize * 2;
        float expandY = camHeight * 0.5f + _cellSize * 2;

        // 计算网格范围（对齐到单元格）
        float minX = Mathf.Floor((camPos.x - expandX) / _cellSize) * _cellSize;
        float maxX = Mathf.Ceil((camPos.x + expandX) / _cellSize) * _cellSize;
        float minY = Mathf.Floor((camPos.y - expandY) / _cellSize) * _cellSize;
        float maxY = Mathf.Ceil((camPos.y + expandY) / _cellSize) * _cellSize;

        _activeLineCount = 0;

        // 绘制垂直线
        for (float x = minX; x <= maxX; x += _cellSize)
        {
            Vector3 start = new Vector3(x, minY, 0);
            Vector3 end = new Vector3(x, maxY, 0);
            CreateOrUpdateLine(start, end);
        }

        // 绘制水平线
        for (float y = minY; y <= maxY; y += _cellSize)
        {
            Vector3 start = new Vector3(minX, y, 0);
            Vector3 end = new Vector3(maxX, y, 0);
            CreateOrUpdateLine(start, end);
        }

        // 隐藏未使用的线对象
        for (int i = _activeLineCount; i < _lineObjects.Count; i++)
        {
            _lineObjects[i].SetActive(false);
        }
    }

    /// <summary>
    /// 创建或更新一条网格线
    /// </summary>
    private void CreateOrUpdateLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj;

        if (_activeLineCount < _lineObjects.Count)
        {
            lineObj = _lineObjects[_activeLineCount];
            lineObj.SetActive(true);
        }
        else
        {
            lineObj = new GameObject($"GridLine_{_activeLineCount}");
            lineObj.transform.SetParent(_gridContainer.transform);
            lineObj.AddComponent<LineRenderer>();
            _lineObjects.Add(lineObj);
        }

        LineRenderer lr = lineObj.GetComponent<LineRenderer>();

        // 设置材质
        if (_lineMaterial == null)
        {
            _lineMaterial = MaterialCache.GetDefault();
        }
        lr.material = _lineMaterial;

        // 设置线条属性
        lr.startWidth = _lineWidth;
        lr.endWidth = _lineWidth;
        lr.startColor = _gridColor;
        lr.endColor = _gridColor;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.useWorldSpace = true;
        lr.sortingOrder = _sortingOrder;

        _activeLineCount++;
    }

    /// <summary>
    /// 主题切换时更新网格颜色
    /// </summary>
    private void OnThemeChanged(MapThemeData newTheme, int themeIndex)
    {
        if (newTheme == null) return;

        _gridColor = new Color(newTheme.gridColor.r, newTheme.gridColor.g, newTheme.gridColor.b, newTheme.gridAlpha);
        _cellSize = newTheme.gridCellSize;
        _lineWidth = newTheme.gridLineWidth;

        DebugHelper.Log($"[GridBackground] Updated for theme {newTheme.themeName}: color={_gridColor}, cellSize={_cellSize}");

        // 强制更新网格
        _needsUpdate = true;
        UpdateGrid();
    }

    /// <summary>
    /// 设置网格颜色
    /// </summary>
    public void SetGridColor(Color color)
    {
        _gridColor = color;
        _needsUpdate = true;
    }

    /// <summary>
    /// 设置单元格大小
    /// </summary>
    public void SetCellSize(float size)
    {
        _cellSize = Mathf.Max(0.1f, size);
        _needsUpdate = true;
    }

    /// <summary>
    /// 设置线宽
    /// </summary>
    public void SetLineWidth(float width)
    {
        _lineWidth = Mathf.Clamp(width, 0.01f, 0.2f);
        _needsUpdate = true;
    }

    /// <summary>
    /// 获取当前网格配置
    /// </summary>
    public float CellSize => _cellSize;
    public Color GridColor => _gridColor;
    public float LineWidth => _lineWidth;

    private void OnDestroy()
    {
        // 清理线对象
        if (_gridContainer != null)
        {
            Destroy(_gridContainer);
        }

        // 清理材质
        if (_lineMaterial != null)
        {
            Destroy(_lineMaterial);
            _lineMaterial = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _gridColor;
        Gizmos.DrawWireCube(transform.position, new Vector3(_cellSize * 20, _cellSize * 20, 0.1f));
    }
}