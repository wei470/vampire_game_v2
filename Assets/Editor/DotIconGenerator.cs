using UnityEngine;
using UnityEditor;

/// <summary>
/// DOT 图标预烘焙工具 — 生成所有 DOT 效果的图标 Sprite 资源。
///
/// 菜单：Mage → Generate DOT Icons
///
/// 生成内容：
/// - 7 个独立 PNG 纹理（64x64，白色形状，透明背景）
/// - 自动设置为 Sprite 导入模式
///
/// 生成路径：Assets/Resources/DOTIcons/
/// 运行时由 HealthBarSpriteHelper.LoadPrebakedSprite() 自动加载。
/// </summary>
public static class DotIconGenerator
{
    private const string OUTPUT_DIR = "Assets/Resources/DOTIcons";
    private const int TEX_SIZE = 64;

    [MenuItem("Mage/Generate DOT Icons")]
    public static void GenerateAll()
    {
        EnsureDirectory(OUTPUT_DIR);

        GenerateAndSave("Triangle", () => CreatePolygonTexture(3, 0.45f));
        GenerateAndSave("Diamond", () => CreatePolygonTexture(4, 0.45f));
        GenerateAndSave("Pentagon", () => CreatePolygonTexture(5, 0.45f));
        GenerateAndSave("Hexagon", () => CreatePolygonTexture(6, 0.45f));
        GenerateAndSave("Octagon", () => CreatePolygonTexture(8, 0.45f));
        GenerateAndSave("Star", () => CreateStarTexture(0.45f, 0.2f, 5));
        GenerateAndSave("Circle", () => CreatePolygonTexture(32, 0.45f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DotIconGenerator] DOT 图标预烘焙完成！共 7 个 Sprite 已保存到 " + OUTPUT_DIR);
    }

    private static void GenerateAndSave(string name, System.Func<Texture2D> generator)
    {
        string assetPath = $"{OUTPUT_DIR}/{name}.png";
        string fullPath = System.IO.Path.Combine(ProjectRoot, assetPath);

        if (System.IO.File.Exists(fullPath))
        {
            Debug.Log($"[DotIconGenerator] {name} 已存在，跳过");
            return;
        }

        var tex = generator();
        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        System.IO.File.WriteAllBytes(fullPath, png);
        AssetDatabase.ImportAsset(assetPath);

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        Debug.Log($"[DotIconGenerator] 生成 {name} → {assetPath}");
    }

    // ═══ 纹理生成 ═══

    private static Texture2D CreatePolygonTexture(int sides, float radius)
    {
        var tex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBA32, false);
        ClearTexture(tex);

        float cx = TEX_SIZE / 2f;
        float cy = TEX_SIZE / 2f;
        float r = TEX_SIZE * radius;

        for (int x = 0; x < TEX_SIZE; x++)
        {
            for (int y = 0; y < TEX_SIZE; y++)
            {
                float dx = x - cx;
                float dy = y - cy;
                if (IsInsidePolygon(dx, dy, r, sides))
                    tex.SetPixel(x, y, Color.white);
            }
        }

        tex.Apply();
        return tex;
    }

    private static Texture2D CreateStarTexture(float outerRadius, float innerRadius, int points)
    {
        var tex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBA32, false);
        ClearTexture(tex);

        float cx = TEX_SIZE / 2f;
        float cy = TEX_SIZE / 2f;
        float outerR = TEX_SIZE * outerRadius;
        float innerR = TEX_SIZE * innerRadius;

        for (int x = 0; x < TEX_SIZE; x++)
        {
            for (int y = 0; y < TEX_SIZE; y++)
            {
                float dx = x - cx;
                float dy = y - cy;
                if (IsInsideStar(dx, dy, outerR, innerR, points))
                    tex.SetPixel(x, y, Color.white);
            }
        }

        tex.Apply();
        return tex;
    }

    private static void ClearTexture(Texture2D tex)
    {
        var clear = new Color[TEX_SIZE * TEX_SIZE];
        for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
        tex.SetPixels(clear);
    }

    // ═══ 碰撞检测（与 HealthBarSpriteHelper 一致）═══

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

    // ═══ 工具方法 ═══

    private static void EnsureDirectory(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string ProjectRoot
    {
        get
        {
            string dataPath = Application.dataPath;
            return dataPath.Substring(0, dataPath.Length - "Assets".Length);
        }
    }
}
