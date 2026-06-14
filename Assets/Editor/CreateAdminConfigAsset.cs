using UnityEngine;
using UnityEditor;

public static class CreateAdminConfigAsset
{
    [MenuItem("Mage/Create AdminConfig Asset")]
    public static void Create()
    {
        string dir = "Assets/Resources/Configs";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Resources", "Configs");

        string path = dir + "/AdminConfig.asset";

        var existing = AssetDatabase.LoadAssetAtPath<AdminConfig>(path);
        if (existing != null)
        {
            Selection.activeObject = existing;
            EditorGUIUtility.PingObject(existing);
            return;
        }

        var config = ScriptableObject.CreateInstance<AdminConfig>();
        config.admin = 0;
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();

        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);
        Debug.Log($"[AdminConfig] Asset created: {path} (admin=0)");
    }
}
