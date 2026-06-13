using UnityEngine;
using UnityEditor;

/// <summary>
/// 一键创建 MageUpgradeConfig ScriptableObject Asset。
/// 菜单：Mage → Create MageUpgradeConfig Asset
/// </summary>
public static class CreateMageUpgradeConfigAsset
{
    [MenuItem("Mage/Create MageUpgradeConfig Asset")]
    public static void Create()
    {
        string dir = "Assets/Resources/Configs";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Resources", "Configs");

        string path = dir + "/MageUpgradeConfig.asset";

        var existing = AssetDatabase.LoadAssetAtPath<MageUpgradeConfig>(path);
        if (existing != null)
        {
            EditorUtility.DisplayDialog(
                "MageUpgradeConfig 已存在",
                $"Asset 已存在于:\n{path}",
                "确定");
            Selection.activeObject = existing;
            EditorGUIUtility.PingObject(existing);
            return;
        }

        var config = ScriptableObject.CreateInstance<MageUpgradeConfig>();
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();

        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);

        Debug.Log($"[MageUpgradeConfig] Asset 已创建: {path}");
        EditorUtility.DisplayDialog(
            "创建成功",
            $"MageUpgradeConfig.asset 已创建到:\n{path}",
            "确定");
    }
}
