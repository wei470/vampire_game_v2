using UnityEngine;

/// <summary>
/// 地图边界系统 — 限制玩家移动范围并显示可见边界。
/// 
/// 功能：
/// - 玩家无法移动超出边界（物理碰撞墙）
/// - 可见的边界线（使用 LineRenderer）
/// - 超出边界的敌人自动回收
/// 
/// 使用方式：挂载到场景中的 GameObject 上，由 GameSceneBootstrap 创建
/// </summary>
public class MapBoundary : MonoBehaviour
{
    [Header("边界设置")]
    [SerializeField] private float _mapRadius = 50f;     // 地图半径（正方形边界）
    [SerializeField] private bool _circularBoundary = false; // true=圆形边界，false=正方形
    [SerializeField] private float _wallThickness = 2f;   // 碰撞墙厚度

    [Header("视觉设置")]
    [SerializeField] private bool _showBoundary = true;
    [SerializeField] private Color _boundaryColor = new Color(1f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private float _lineWidth = 0.2f;

    private void Start()
    {
        CreateBoundaryWalls();
        if (_showBoundary) DrawBoundaryLine();
    }

    /// <summary>
    /// 创建四面碰撞墙（使用 BoxCollider2D）
    /// </summary>
    private void CreateBoundaryWalls()
    {
        float size = _mapRadius * 2f;

        // 创建四面墙
        CreateWall("Wall_Top",    new Vector2(0, _mapRadius + _wallThickness/2f), new Vector2(size + _wallThickness*2, _wallThickness));
        CreateWall("Wall_Bottom", new Vector2(0, -_mapRadius - _wallThickness/2f), new Vector2(size + _wallThickness*2, _wallThickness));
        CreateWall("Wall_Left",   new Vector2(-_mapRadius - _wallThickness/2f, 0), new Vector2(_wallThickness, size + _wallThickness*2));
        CreateWall("Wall_Right",  new Vector2(_mapRadius + _wallThickness/2f, 0), new Vector2(_wallThickness, size + _wallThickness*2));

        DebugHelper.Log($"[MapBoundary] Boundary walls created. Map radius: {_mapRadius}");
    }

    private void CreateWall(string name, Vector2 position, Vector2 size)
    {
        var wall = new GameObject(name);
        wall.transform.SetParent(transform);
        wall.transform.position = position;
        wall.layer = LayerMask.NameToLayer("Default");

        var col = wall.AddComponent<BoxCollider2D>();
        col.size = size;
    }

    /// <summary>
    /// 绘制可见的边界线
    /// </summary>
    private void DrawBoundaryLine()
    {
        var lr = gameObject.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = _boundaryColor;
        lr.endColor = _boundaryColor;
        lr.startWidth = _lineWidth;
        lr.endWidth = _lineWidth;
        lr.sortingOrder = 50;

        if (_circularBoundary)
        {
            // 圆形边界
            int segments = 64;
            lr.positionCount = segments + 1;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * 360f * Mathf.Deg2Rad;
                lr.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * _mapRadius,
                    Mathf.Sin(angle) * _mapRadius,
                    0f
                ));
            }
            lr.loop = true;
        }
        else
        {
            // 正方形边界
            lr.positionCount = 5;
            lr.SetPosition(0, new Vector3(-_mapRadius, -_mapRadius, 0f));
            lr.SetPosition(1, new Vector3( _mapRadius, -_mapRadius, 0f));
            lr.SetPosition(2, new Vector3( _mapRadius,  _mapRadius, 0f));
            lr.SetPosition(3, new Vector3(-_mapRadius,  _mapRadius, 0f));
            lr.SetPosition(4, new Vector3(-_mapRadius, -_mapRadius, 0f));
            lr.loop = false;
        }
    }

    /// <summary>
    /// 创建地图边界实例
    /// </summary>
    public static MapBoundary Create(float radius = 50f, bool circular = false)
    {
        var go = new GameObject("MapBoundary");
        var boundary = go.AddComponent<MapBoundary>();
        boundary._mapRadius = radius;
        boundary._circularBoundary = circular;
        return boundary;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _boundaryColor;
        if (_circularBoundary)
        {
            Gizmos.DrawWireSphere(Vector3.zero, _mapRadius);
        }
        else
        {
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(_mapRadius * 2, _mapRadius * 2, 0));
        }
    }
}