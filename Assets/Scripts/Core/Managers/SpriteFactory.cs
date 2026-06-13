using UnityEngine;

/// <summary>
/// Sprite 工具类，缓存常用运行时生成的 Sprite，避免重复创建。
/// </summary>
public static class SpriteFactory
{
    private static Sprite _square;
    private static Sprite _circle;
    private static Sprite _triangle;
    private static Sprite _diamond;
    private static Sprite _pentagon;
    private static Sprite _hexagon;
    private static Sprite _star;
    private static Sprite _cross;

    /// <summary>
    /// 4x4 白色方块 Sprite（默认 Pivot 0.5, 0.5, PPU 4）
    /// </summary>
    public static Sprite Square => _square ??= CreateSquare();

    /// <summary>
    /// 16x16 白色圆形 Sprite
    /// </summary>
    public static Sprite Circle => _circle ??= CreateCircle();

    /// <summary>
    /// 16x16 白色正三角 Sprite（尖朝上）
    /// </summary>
    public static Sprite Triangle => _triangle ??= CreateTriangle();

    /// <summary>
    /// 16x16 白色菱形 Sprite
    /// </summary>
    public static Sprite Diamond => _diamond ??= CreateDiamond();

    /// <summary>
    /// 16x16 白色五边形 Sprite
    /// </summary>
    public static Sprite Pentagon => _pentagon ??= CreatePentagon();

    /// <summary>
    /// 16x16 白色六边形 Sprite
    /// </summary>
    public static Sprite Hexagon => _hexagon ??= CreateHexagon();

    /// <summary>
    /// 16x16 白色星形 Sprite
    /// </summary>
    public static Sprite Star => _star ??= CreateStar();

    /// <summary>
    /// 16x16 白色十字形 Sprite
    /// </summary>
    public static Sprite Cross => _cross ??= CreateCross();

    /// <summary>
    /// 自定义尺寸白色方块 Sprite
    /// </summary>
    public static Sprite SquareWithSize(int size, float ppu)
    {
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
    }

    private static Sprite CreateSquare()
    {
        var tex = new Texture2D(4, 4);
        var pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    /// <summary>
    /// 清除所有缓存（场景卸载时调用，释放纹理内存）
    /// </summary>
    public static void ClearCache()
    {
        DestroySprite(ref _square);
        DestroySprite(ref _circle);
        DestroySprite(ref _triangle);
        DestroySprite(ref _diamond);
        DestroySprite(ref _pentagon);
        DestroySprite(ref _hexagon);
        DestroySprite(ref _star);
        DestroySprite(ref _cross);
    }

    private static void DestroySprite(ref Sprite sprite)
    {
        if (sprite != null)
        {
            if (sprite.texture != null) Object.Destroy(sprite.texture);
            Object.Destroy(sprite);
            sprite = null;
        }
    }

    private static Sprite CreateCircle()
    {
        int size = 16;
        var tex = new Texture2D(size, size);
        float center = size / 2f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center - 0.5f, center - 0.5f)) / (center);
                tex.SetPixel(x, y, dist <= 1f ? Color.white : new Color(0, 0, 0, 0));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // ── 以下为敌人形状区分的各种几何图形 ──

    private static Sprite CreateTriangle()
    {
        int size = 16;
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                // 正三角：从底边中心向上收窄
                float fx = (float)x / size;           // 0..1
                float fy = (float)y / size;
                float halfW = (1f - fy) * 0.5f;       // 底部宽，顶部窄
                pixels[y * size + x] = (fx >= 0.5f - halfW && fx <= 0.5f + halfW)
                    ? Color.white : new Color(0, 0, 0, 0);
            }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateDiamond()
    {
        int size = 16;
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];
        float half = size / 2f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dx = Mathf.Abs(x - half + 0.5f) / half;
                float dy = Mathf.Abs(y - half + 0.5f) / half;
                pixels[y * size + x] = (dx + dy <= 1f) ? Color.white : new Color(0, 0, 0, 0);
            }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreatePentagon()
    {
        return CreateRegularPolygon(16, 5, 0f);
    }

    private static Sprite CreateHexagon()
    {
        return CreateRegularPolygon(16, 6, 0f);
    }

    private static Sprite CreateRegularPolygon(int size, int sides, float rotationOffset)
    {
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];
        float center = size / 2f - 0.5f;
        float radius = size / 2f - 1f;

        // 预计算多边形顶点
        Vector2[] vertices = new Vector2[sides];
        for (int i = 0; i < sides; i++)
        {
            float angle = rotationOffset + i * Mathf.PI * 2f / sides - Mathf.PI / 2f;
            vertices[i] = new Vector2(center + Mathf.Cos(angle) * radius, center + Mathf.Sin(angle) * radius);
        }

        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                pixels[y * size + x] = IsPointInPolygon(new Vector2(x, y), vertices)
                    ? Color.white : new Color(0, 0, 0, 0);
            }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        int j = polygon.Length - 1;
        for (int i = 0; i < polygon.Length; i++)
        {
            if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
            {
                inside = !inside;
            }
            j = i;
        }
        return inside;
    }

    private static Sprite CreateStar()
    {
        int size = 16;
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];
        float center = size / 2f - 0.5f;
        float outerR = size / 2f - 1f;
        float innerR = outerR * 0.45f;
        int points = 5;

        Vector2[] vertices = new Vector2[points * 2];
        for (int i = 0; i < points * 2; i++)
        {
            float r = (i % 2 == 0) ? outerR : innerR;
            float angle = i * Mathf.PI / points - Mathf.PI / 2f;
            vertices[i] = new Vector2(center + Mathf.Cos(angle) * r, center + Mathf.Sin(angle) * r);
        }

        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                pixels[y * size + x] = IsPointInPolygon(new Vector2(x, y), vertices)
                    ? Color.white : new Color(0, 0, 0, 0);
            }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateCross()
    {
        int size = 16;
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];
        float armWidth = size * 0.25f;
        float center = size / 2f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                bool horizontal = y >= center - armWidth && y <= center + armWidth;
                bool vertical = x >= center - armWidth && x <= center + armWidth;
                pixels[y * size + x] = (horizontal || vertical) ? Color.white : new Color(0, 0, 0, 0);
            }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
