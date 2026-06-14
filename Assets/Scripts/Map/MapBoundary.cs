using UnityEngine;

/// <summary>
/// 地图边界系统 — 在屏幕边缘创建跟随摄像机的空气墙。
/// 
/// 功能：
/// - 四面碰撞墙紧贴屏幕边缘（带 margin），跟随摄像机移动
/// - LateUpdate 强制钳制所有实体在屏幕内
/// - 玩家和敌人都无法走出或被击退出屏幕
/// </summary>
public class MapBoundary : MonoBehaviour
{
    [Header("边界设置")]
    [SerializeField] private float _mapRadius = 50f;
    [SerializeField] private bool _circularBoundary = false;
    [SerializeField] private float _margin = -1f;
    [SerializeField] private float _wallThickness = 5f;

    [Header("视觉设置")]
    [SerializeField] private bool _showBoundary = true;
    [SerializeField] private Color _boundaryColor = new Color(1f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private float _lineWidth = 0.2f;

    private Camera _cam;
    private GameObject _wallTop, _wallBottom, _wallLeft, _wallRight;
    private float _screenHalfW, _screenHalfH;

    private void Start()
    {
        _cam = Camera.main;
        CreateScreenWalls();
        if (_showBoundary) DrawBoundaryLine();
    }

    private void CreateScreenWalls()
    {
        _wallTop = CreateWall("ScreenWall_Top");
        _wallBottom = CreateWall("ScreenWall_Bottom");
        _wallLeft = CreateWall("ScreenWall_Left");
        _wallRight = CreateWall("ScreenWall_Right");
        UpdateWallPositions();
    }

    private GameObject CreateWall(string name)
    {
        var wall = new GameObject(name);
        wall.transform.SetParent(transform);
        wall.layer = PhysicsLayerSetup.LAYER_ENVIRONMENT;

        var col = wall.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);

        var rb = wall.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        return wall;
    }

    private void FixedUpdate()
    {
        UpdateWallPositions();
        ClampAllEntities();
    }

    private void LateUpdate()
    {
        UpdateWallPositions();
        ClampAllEntities();
    }

    private void UpdateWallPositions()
    {
        if (_cam == null) return;

        float h = _cam.orthographicSize + _margin;
        float w = h * _cam.aspect + _margin;
        float t = _wallThickness;

        Vector3 camPos = _cam.transform.position;

        _wallTop.transform.position = new Vector3(camPos.x, camPos.y + h + t / 2f, 0f);
        _wallTop.GetComponent<BoxCollider2D>().size = new Vector2(w * 2 + t * 2, t);

        _wallBottom.transform.position = new Vector3(camPos.x, camPos.y - h - t / 2f, 0f);
        _wallBottom.GetComponent<BoxCollider2D>().size = new Vector2(w * 2 + t * 2, t);

        _wallLeft.transform.position = new Vector3(camPos.x - w - t / 2f, camPos.y, 0f);
        _wallLeft.GetComponent<BoxCollider2D>().size = new Vector2(t, h * 2 + t * 2);

        _wallRight.transform.position = new Vector3(camPos.x + w + t / 2f, camPos.y, 0f);
        _wallRight.GetComponent<BoxCollider2D>().size = new Vector2(t, h * 2 + t * 2);

        _screenHalfW = w;
        _screenHalfH = h;
    }

    private void ClampAllEntities()
    {
        if (_cam == null) return;
        Vector3 cp = _cam.transform.position;
        float minX = cp.x - _screenHalfW;
        float maxX = cp.x + _screenHalfW;
        float minY = cp.y - _screenHalfH;
        float maxY = cp.y + _screenHalfH;

        var enemies = EnemyBase.AllAlive;
        for (int i = 0; i < enemies.Count; i++)
        {
            var eb = enemies[i];
            if (eb == null) continue;
            var pos = eb.transform.position;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            if (eb.transform.position != pos) eb.transform.position = pos;
        }

        var player = GameReferences.Player;
        if (player != null)
        {
            var pos = player.transform.position;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            if (player.transform.position != pos) player.transform.position = pos;
        }
    }

    private void DrawBoundaryLine()
    {
        var lr = gameObject.AddComponent<LineRenderer>();
        lr.material = MaterialCache.GetDefault();
        lr.startColor = _boundaryColor;
        lr.endColor = _boundaryColor;
        lr.startWidth = _lineWidth;
        lr.endWidth = _lineWidth;
        lr.sortingOrder = 50;
        lr.useWorldSpace = true;

        lr.positionCount = 5;
        lr.loop = false;
        UpdateBoundaryLine(lr);
    }

    private LineRenderer _lr;
    private void Update()
    {
        if (_lr == null) _lr = GetComponent<LineRenderer>();
        if (_lr != null && _showBoundary) UpdateBoundaryLine(_lr);
    }

    private void UpdateBoundaryLine(LineRenderer lr)
    {
        if (_cam == null) return;
        float h = _cam.orthographicSize + _margin;
        float w = h * _cam.aspect + _margin;
        Vector3 cp = _cam.transform.position;
        lr.SetPosition(0, new Vector3(cp.x - w, cp.y - h, 0f));
        lr.SetPosition(1, new Vector3(cp.x + w, cp.y - h, 0f));
        lr.SetPosition(2, new Vector3(cp.x + w, cp.y + h, 0f));
        lr.SetPosition(3, new Vector3(cp.x - w, cp.y + h, 0f));
        lr.SetPosition(4, new Vector3(cp.x - w, cp.y - h, 0f));
    }

    public static MapBoundary Create(float radius = 50f, bool circular = false)
    {
        var go = new GameObject("MapBoundary");
        var boundary = go.AddComponent<MapBoundary>();
        boundary._mapRadius = radius;
        boundary._circularBoundary = circular;
        return boundary;
    }
}
