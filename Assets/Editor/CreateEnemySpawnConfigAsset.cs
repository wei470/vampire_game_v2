using UnityEngine;
using UnityEditor;

/// <summary>
/// 编辑器工具：一键创建 EnemySpawnConfig.asset 到 Resources/Configs/ 目录。
/// 菜单：Mage → Create Enemy Spawn Config Asset
/// </summary>
public class CreateEnemySpawnConfigAsset
{
    [MenuItem("Mage/Create Enemy Spawn Config Asset")]
    public static void CreateAsset()
    {
        string dir = "Assets/Resources/Configs";
        string path = dir + "/EnemySpawnConfig.asset";

        // 检查是否已存在
        if (AssetDatabase.LoadAssetAtPath<EnemySpawnConfig>(path) != null)
        {
            DebugHelper.Log($"[CreateEnemySpawnConfigAsset] 已存在: {path}");
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<EnemySpawnConfig>(path);
            return;
        }

        // 确保目录存在
        if (!AssetDatabase.IsValidFolder(dir))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CreateFolder("Assets/Resources", "Configs");
        }

        var config = ScriptableObject.CreateInstance<EnemySpawnConfig>();
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = config;

        DebugHelper.Log($"[CreateEnemySpawnConfigAsset] 创建成功: {path}");
    }
}