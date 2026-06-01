using UnityEngine;

/// <summary>
/// Sprite 工具类，缓存常用运行时生成的 Sprite，避免重复创建。
/// </summary>
public static class SpriteFactory
{
    private static Sprite _square;
    private static Sprite _circle;

    /// <summary>
    /// 4x4 白色方块 Sprite（默认 Pivot 0.5, 0.5, PPU 4）
    /// </summary>
    public static Sprite Square => _square ??= CreateSquare();

    /// <summary>
    /// 8x8 白色圆形 Sprite
    /// </summary>
    public static Sprite Circle => _circle ??= CreateCircle();

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
        if (_square != null)
        {
            Object.Destroy(_square.texture);
            Object.Destroy(_square);
            _square = null;
        }
        if (_circle != null)
        {
            Object.Destroy(_circle.texture);
            Object.Destroy(_circle);
            _circle = null;
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
}