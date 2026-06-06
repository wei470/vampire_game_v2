#pragma warning disable CS0618
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 装饰物Sprite生成工具，提供程序化生成装饰物纹理的静态方法。
/// 从 DecorationSpawner 中提取，减少主类代码量。
/// </summary>
public static class DecorationSpriteHelper
{
    /// <summary>
    /// 根据装饰物类型创建程序化Sprite
    /// </summary>
    public static Sprite CreateDecorationSprite(MapThemeData.DecorationType type)
    {
        int size = 16;
        var tex = new Texture2D(size, size);

        // 根据类型绘制不同形状
        switch (type)
        {
            case MapThemeData.DecorationType.Tree:
                DrawTriangle(tex, size, Color.green);
                break;
            case MapThemeData.DecorationType.Rock:
                DrawBlob(tex, size, Color.gray);
                break;
            case MapThemeData.DecorationType.Bush:
                DrawCircle(tex, size, new Color(0.2f, 0.7f, 0.2f));
                break;
            case MapThemeData.DecorationType.Mushroom:
                DrawMushroom(tex, size);
                break;
            case MapThemeData.DecorationType.Torch:
                DrawTorch(tex, size);
                break;
            case MapThemeData.DecorationType.Pillar:
                DrawRectangle(tex, size, new Color(0.5f, 0.4f, 0.3f));
                break;
            case MapThemeData.DecorationType.Cactus:
                DrawCactus(tex, size);
                break;
            case MapThemeData.DecorationType.Dune:
                DrawDune(tex, size);
                break;
            case MapThemeData.DecorationType.LavaRock:
                DrawCircle(tex, size, new Color(0.6f, 0.1f, 0.1f));
                break;
            case MapThemeData.DecorationType.Obsidian:
                DrawBlob(tex, size, new Color(0.1f, 0.1f, 0.15f));
                break;
            case MapThemeData.DecorationType.IceCrystal:
                DrawDiamond(tex, size, new Color(0.7f, 0.9f, 1f));
                break;
            case MapThemeData.DecorationType.SnowPile:
                DrawCircle(tex, size, new Color(0.9f, 0.95f, 1f));
                break;
            default:
                DrawCircle(tex, size, Color.white);
                break;
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size * 2);
    }

    /// <summary>
    /// 获取装饰物颜色
    /// </summary>
    public static Color GetDecorationColor(MapThemeData.DecorationType type)
    {
        switch (type)
        {
            case MapThemeData.DecorationType.Tree:
                return new Color(0.2f, 0.6f, 0.2f);
            case MapThemeData.DecorationType.Rock:
                return new Color(0.5f, 0.5f, 0.5f);
            case MapThemeData.DecorationType.Bush:
                return new Color(0.3f, 0.7f, 0.3f);
            case MapThemeData.DecorationType.Mushroom:
                return new Color(0.8f, 0.3f, 0.3f);
            case MapThemeData.DecorationType.Torch:
                return new Color(1f, 0.7f, 0.2f);
            case MapThemeData.DecorationType.Pillar:
                return new Color(0.5f, 0.4f, 0.3f);
            case MapThemeData.DecorationType.Cactus:
                return new Color(0.3f, 0.6f, 0.2f);
            case MapThemeData.DecorationType.Dune:
                return new Color(0.9f, 0.8f, 0.5f);
            case MapThemeData.DecorationType.LavaRock:
                return new Color(0.6f, 0.2f, 0.1f);
            case MapThemeData.DecorationType.Obsidian:
                return new Color(0.15f, 0.1f, 0.2f);
            case MapThemeData.DecorationType.IceCrystal:
                return new Color(0.7f, 0.9f, 1f);
            case MapThemeData.DecorationType.SnowPile:
                return new Color(0.9f, 0.95f, 1f);
            default:
                return Color.white;
        }
    }

    // ============================================================
    // 程序化 Sprite 绘制辅助方法
    // ============================================================

    private static void DrawCircle(Texture2D tex, int size, Color color)
    {
        float center = size * 0.5f;
        float radius = center - 2;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                tex.SetPixel(x, y, dist <= radius ? color : Color.clear);
            }
    }

    private static void DrawTriangle(Texture2D tex, int size, Color color)
    {
        float center = size * 0.5f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                // 等边三角形
                float normalizedY = (float)y / size;
                float halfWidth = normalizedY * center;
                bool inside = Mathf.Abs(x - center) <= halfWidth && y >= 2 && y <= size - 2;
                tex.SetPixel(x, y, inside ? color : Color.clear);
            }
    }

    private static void DrawBlob(Texture2D tex, int size, Color color)
    {
        float center = size * 0.5f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                // 不规则边缘
                float angle = Mathf.Atan2(y - center, x - center);
                float radius = center - 2 + Mathf.Sin(angle * 3) * 1.5f;
                tex.SetPixel(x, y, dist <= radius ? color : Color.clear);
            }
    }

    private static void DrawMushroom(Texture2D tex, int size)
    {
        float center = size * 0.5f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                // 菌盖（上半部分）
                if (y > center)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center + 2));
                    if (dist < center - 2)
                    {
                        tex.SetPixel(x, y, new Color(0.8f, 0.2f, 0.2f));
                        continue;
                    }
                }
                // 菌柄（中间窄矩形）
                if (Mathf.Abs(x - center) < 2 && y >= 2 && y <= center + 1)
                {
                    tex.SetPixel(x, y, new Color(0.9f, 0.85f, 0.7f));
                    continue;
                }
                tex.SetPixel(x, y, Color.clear);
            }
    }

    private static void DrawTorch(Texture2D tex, int size)
    {
        float center = size * 0.5f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                // 火把柄（窄矩形）
                if (Mathf.Abs(x - center) < 1.5f && y >= 1 && y < center + 2)
                {
                    tex.SetPixel(x, y, new Color(0.4f, 0.3f, 0.2f));
                    continue;
                }
                // 火焰（上半部分小圆）
                if (y > center + 1)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center + 4));
                    if (dist < 3f)
                    {
                        tex.SetPixel(x, y, new Color(1f, 0.7f, 0.1f));
                        continue;
                    }
                }
                tex.SetPixel(x, y, Color.clear);
            }
    }

    private static void DrawRectangle(Texture2D tex, int size, Color color)
    {
        int margin = 3;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                bool inside = x >= margin && x < size - margin && y >= 1 && y < size - 1;
                tex.SetPixel(x, y, inside ? color : Color.clear);
            }
    }

    private static void DrawCactus(Texture2D tex, int size)
    {
        float center = size * 0.5f;
        Color cactusColor = new Color(0.3f, 0.6f, 0.2f);
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                // 主干
                if (Mathf.Abs(x - center) < 2 && y >= 1 && y < size - 1)
                {
                    tex.SetPixel(x, y, cactusColor);
                    continue;
                }
                // 左臂
                if (x >= 2 && x < center - 1 && Mathf.Abs(y - (center + 2)) < 2)
                {
                    tex.SetPixel(x, y, cactusColor);
                    continue;
                }
                // 右臂
                if (x > center + 1 && x <= size - 3 && Mathf.Abs(y - (center - 1)) < 2)
                {
                    tex.SetPixel(x, y, cactusColor);
                    continue;
                }
                tex.SetPixel(x, y, Color.clear);
            }
    }

    private static void DrawDune(Texture2D tex, int size)
    {
        float center = size * 0.5f;
        Color duneColor = new Color(0.9f, 0.8f, 0.5f);
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                // 半椭圆
                float dx = (x - center) / center;
                float dy = (y - 2) / (center - 2);
                bool inside = dx * dx + dy * dy <= 1f && y < center;
                tex.SetPixel(x, y, inside ? duneColor : Color.clear);
            }
    }

    private static void DrawDiamond(Texture2D tex, int size, Color color)
    {
        float center = size * 0.5f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dx = Mathf.Abs(x - center) / center;
                float dy = Mathf.Abs(y - center) / center;
                bool inside = dx + dy <= 0.8f;
                tex.SetPixel(x, y, inside ? color : Color.clear);
            }
    }
}