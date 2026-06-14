using UnityEngine;
using UnityEditor;

public static class CreatePlayerConfigAsset
{
    [MenuItem("Mage/Create PlayerConfig Asset")]
    public static void Create()
    {
        string dir = "Assets/Resources/Configs";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Resources", "Configs");

        string path = dir + "/PlayerConfig.asset";

        var existing = AssetDatabase.LoadAssetAtPath<PlayerConfig>(path);
        if (existing != null)
        {
            Selection.activeObject = existing;
            EditorGUIUtility.PingObject(existing);
            return;
        }

        var config = ScriptableObject.CreateInstance<PlayerConfig>();
        config.defaultMoveSpeed = 10f;
        config.characters = new PlayerConfig.CharacterStats[]
        {
            new PlayerConfig.CharacterStats { characterId = "mage", moveSpeed = 10f }
        };
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();

        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);
        Debug.Log($"[PlayerConfig] Asset created: {path}");
    }
}
