using UnityEngine;

/// <summary>
/// 血条 Sprite 工具类 — 静态缓存白色方块 Sprite 和多边形形状，
/// 供 EnemyHealthBar 和 DotStatusBar 共享使用。
/// </summary>
public static class HealthBarSpriteHelper
{
    private static Sprite _whiteSprite;
    private static Sprite _leftPivotWhiteSprite;

    // #6 几何形状 Sprite 缓存（DOT 图标用）
    private static Sprite _triangleSprite;  // 流血/风化
    private static Sprite _diamondSprite;   // 中毒/黑暗
    private static Sprite _pentagonSprite;  // 燃烧
    private static Sprite _hexagonSprite;   // 霜冻
    private static Sprite _octagonSprite;   // 静电
    private static Sprite _starSprite;      // 光明
    private static Sprite _circleSprite;    // 通用

    /// <summary>
    /// 获取居中 pivot 的白色方块 Sprite（单例缓存）
    /// </summary>
    public static Sprite GetWhiteSprite()
    {
        if (_whiteSprite == null)
        {
            int size = 4;
            var tex = new Texture2D(size, size);
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 10f);
        }
        return _whiteSprite;
    }

    /// <summary>
    /// 左对齐 Sprite（pivot 在左边缘），用于填充条 scale 缩放时不偏移位置。
    /// </summary>
    public static Sprite GetLeftPivotWhiteSprite()
    {
        if (_leftPivotWhiteSprite == null)
        {
            int size = 4;
            var tex = new Texture2D(size, size);
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            _leftPivotWhiteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0f, 0.5f), 10f);
        }
        return _leftPivotWhiteSprite;
    }

    // ═══ #6 DOT 图标几何形状 ═══

    public static Sprite GetTriangleSprite()
    {
        if (_triangleSprite == null)
            _triangleSprite = LoadPrebakedSprite("Triangle") ?? CreatePolygonSprite(3, 0.45f, "Triangle");
        return _triangleSprite;
    }

    public static Sprite GetDiamondSprite()
    {
        if (_diamondSprite == null)
            _diamondSprite = LoadPrebakedSprite("Diamond") ?? CreatePolygonSprite(4, 0.45f, "Diamond");
        return _diamondSprite;
    }

    public static Sprite GetPentagonSprite()
    {
        if (_pentagonSprite == null)
            _pentagonSprite = LoadPrebakedSprite("Pentagon") ?? CreatePolygonSprite(5, 0.45f, "Pentagon");
        return _pentagonSprite;
    }

    public static Sprite GetHexagonSprite()
    {
        if (_hexagonSprite == null)
            _hexagonSprite = LoadPrebakedSprite("Hexagon") ?? CreatePolygonSprite(6, 0.45f, "Hexagon");
        return _hexagonSprite;
    }

    public static Sprite GetOctagonSprite()
    {
        if (_octagonSprite == null)
            _octagonSprite = LoadPrebakedSprite("Octagon") ?? CreatePolygonSprite(8, 0.45f, "Octagon");
        return _octagonSprite;
    }

    public static Sprite GetStarSprite()
    {
        if (_starSprite == null)
            _starSprite = LoadPrebakedSprite("Star") ?? CreateStarSprite(0.45f, 0.2f, 5);
        return _starSprite;
    }

    public static Sprite GetCircleSprite()
    {
        if (_circleSprite == null)
            _circleSprite = LoadPrebakedSprite("Circle") ?? CreatePolygonSprite(32, 0.45f, "Circle");
        return _circleSprite;
    }

    /// <summary>
    /// 尝试从 Resources/DOTIcons/ 加载预烘焙 Sprite。
    /// 若未预烘焙则返回 null，由调用方回退到程序化生成。
    /// </summary>
    private static Sprite LoadPrebakedSprite(string name)
    {
        return Resources.Load<Sprite>($"DOTIcons/{name}");
    }

    /// <summary>
    /// 生成正多边形 Sprite（三角形/菱形/五边形/六边形）
    /// </summary>
    private static Sprite CreatePolygonSprite(int sides, float radius, string name)
    {
        int pxSize = 64;
        var tex = new Texture2D(pxSize, pxSize, TextureFormat.RGBA32, false);
        for (int x = 0; x < pxSize; x++)
            for (int y = 0; y < pxSize; y++)
                tex.SetPixel(x, y, Color.clear);

        float cx = pxSize / 2f;
        float cy = pxSize / 2f;
        float r = pxSize * radius;

        for (int x = 0; x < pxSize; x++)
        {
            for (int y = 0; y < pxSize; y++)
            {
                float dx = x - cx;
                float dy = y - cy;
                if (IsInsidePolygon(dx, dy, r, sides))
                    tex.SetPixel(x, y, Color.white);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, pxSize, pxSize), new Vector2(0.5f, 0.5f), pxSize);
    }

    private static Sprite CreateStarSprite(float outerRadius, float innerRadius, int points)
    {
        int pxSize = 64;
        var tex = new Texture2D(pxSize, pxSize, TextureFormat.RGBA32, false);
        for (int x = 0; x < pxSize; x++)
            for (int y = 0; y < pxSize; y++)
                tex.SetPixel(x, y, Color.clear);

        float cx = pxSize / 2f;
        float cy = pxSize / 2f;
        float outerR = pxSize * outerRadius;
        float innerR = pxSize * innerRadius;

        for (int x = 0; x < pxSize; x++)
        {
            for (int y = 0; y < pxSize; y++)
            {
                float dx = x - cx;
                float dy = y - cy;
                if (IsInsideStar(dx, dy, outerR, innerR, points))
                    tex.SetPixel(x, y, Color.white);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, pxSize, pxSize), new Vector2(0.5f, 0.5f), pxSize);
    }

    private static bool IsInsideStar(float px, float py, float outerR, float innerR, int points)
    {
        float dist = Mathf.Sqrt(px * px + py * py);
        float maxR = Mathf.Max(outerR, innerR);
        if (dist > maxR) return false;
        if (dist < 0.01f) return true;

        float angle = Mathf.Atan2(py, px);
        if (angle < 0) angle += 2f * Mathf.PI;
        float sectorAngle = Mathf.PI / points;
        float relAngle = angle % sectorAngle;
        float halfSector = sectorAngle * 0.5f;

        float t = relAngle / halfSector;
        float edgeR;
        if (t < 1f)
            edgeR = Mathf.Lerp(outerR, innerR, t);
        else
            edgeR = Mathf.Lerp(innerR, outerR, t - 1f);

        return dist <= edgeR;
    }

    /// <summary>
    /// 判断点是否在正多边形内（用于多边形光栅化）
    /// </summary>
    private static bool IsInsidePolygon(float px, float py, float r, int sides)
    {
        float dist = Mathf.Sqrt(px * px + py * py);
        if (dist > r) return false;
        if (dist < 0.01f) return true;
        float angle = Mathf.Atan2(py, px);
        if (angle < 0) angle += 2f * Mathf.PI;
        float sectorAngle = 2f * Mathf.PI / sides;
        float halfSector = sectorAngle / 2f;
        float relAngle = angle % sectorAngle;
        float edgeDist = r * Mathf.Cos(halfSector) / Mathf.Cos(relAngle - halfSector);
        return dist <= edgeDist;
    }

    /// <summary>
    /// 获取 StatusEffectType 对应的显示颜色
    /// </summary>
    public static Color GetEffectColor(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Bleed:
            case StatusEffectType.Rend:
                return new Color(0.9f, 0.1f, 0.1f);
            case StatusEffectType.Poison:
                return new Color(0.1f, 0.9f, 0.2f);
            case StatusEffectType.Burn:
            case StatusEffectType.Immolate:
                return new Color(1f, 0.5f, 0f);
            case StatusEffectType.Frostbite:
                return new Color(0.3f, 0.6f, 1f);
            case StatusEffectType.Corrosion:
            case StatusEffectType.Erosion:
                return new Color(0.5f, 0.8f, 0.2f);
            case StatusEffectType.Curse:
            case StatusEffectType.Wither:
                return new Color(0.4f, 0f, 0.6f);
            case StatusEffectType.Agony:
                return new Color(0.6f, 0f, 0.3f);
            case StatusEffectType.Radiate:
                return new Color(0f, 1f, 0.5f);
            case StatusEffectType.Contaminate:
                return new Color(0.3f, 0.5f, 0.3f);
            case StatusEffectType.WindErosion:
                return new Color(0.7f, 0.85f, 1f);
            default:
                return Color.white;
        }
    }
}