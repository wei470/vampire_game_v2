using UnityEngine;
using UnityEditor;

/// <summary>
/// 一键创建 DotBulletConfig ScriptableObject Asset。
/// 菜单：Mage → Create DotBulletConfig Asset
/// </summary>
public static class CreateDotBulletConfigAsset
{
    [MenuItem("Mage/Create DotBulletConfig Asset")]
    public static void Create()
    {
        // 确保目录存在
        string dir = "Assets/Resources/Configs";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Resources", "Configs");

        string path = dir + "/DotBulletConfig.asset";

        // 检查是否已存在
        var existing = AssetDatabase.LoadAssetAtPath<DotBulletConfig>(path);
        if (existing != null)
        {
            EditorUtility.DisplayDialog(
                "DotBulletConfig 已存在",
                $"Asset 已存在于:\n{path}\n\n点击 Inspector 中的参数可直接修改。",
                "确定");
            Selection.activeObject = existing;
            EditorGUIUtility.PingObject(existing);
            return;
        }

        // 创建新 asset（使用 DotBulletConfig 中的默认值）
        var config = ScriptableObject.CreateInstance<DotBulletConfig>();
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();

        // 选中新创建的 asset
        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);

        Debug.Log($"[DotBulletConfig] ✅ Asset 已创建: {path}");
        EditorUtility.DisplayDialog(
            "创建成功",
            $"DotBulletConfig.asset 已创建到:\n{path}\n\n默认参数已填充，可在 Inspector 中修改。",
            "确定");
    }
}