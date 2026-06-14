using UnityEngine;

/// <summary>
/// 选择流程数据加载工具，负责从Resources/AssetDatabase加载角色/武器/技能数据。
/// 从 SelectionFlowManager 中提取，减少主类代码量。
/// </summary>
public static class SelectionDataLoader
{
    /// <summary>
    /// 加载角色数据数组
    /// </summary>
    public static CharacterData[] LoadCharacters()
    {
        // 优先从 Resources/Characters 加载（打包后可用）
        var charAssets = Resources.LoadAll<CharacterData>("Characters");
        if (charAssets.Length > 0)
            return charAssets;

        // 回退：从 Resources 根目录加载
        charAssets = Resources.LoadAll<CharacterData>("");
        if (charAssets.Length > 0)
            return charAssets;

#if UNITY_EDITOR
        // 编辑器回退：AssetDatabase
        return new CharacterData[]
        {
            UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/ScriptableObjects/Characters/Char_warrior.asset"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/ScriptableObjects/Characters/Char_mage.asset"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/ScriptableObjects/Characters/Char_ranger.asset"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/ScriptableObjects/Characters/Char_vampire.asset"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/ScriptableObjects/Characters/Char_assassin.asset"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/ScriptableObjects/Characters/Char_paladin.asset"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/ScriptableObjects/Characters/Char_necromancer.asset"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/ScriptableObjects/Characters/Char_berserker.asset"),
        };
#else
        return new CharacterData[0];
#endif
    }

    /// <summary>
    /// 获取武器数据数组（优先使用Inspector分配，否则从WeaponController获取）
    /// </summary>
    public static WeaponData[] GetWeaponDataArray(WeaponData[] inspectorWeapons)
    {
        if (inspectorWeapons != null && inspectorWeapons.Length > 0 && inspectorWeapons[0] != null)
            return inspectorWeapons;

        var wc = GameReferences.WeaponCtrl;
        if (wc != null)
            return wc.GetAllWeaponData();

        return new WeaponData[0];
    }

    /// <summary>
    /// 从 Resources 或 AssetDatabase 加载资源
    /// </summary>
    private static T LoadAsset<T>(string path) where T : UnityEngine.Object
    {
        // 尝试 Resources.Load
        string resourcesPath = ExtractResourcesPath(path);
        if (!string.IsNullOrEmpty(resourcesPath))
        {
            var res = Resources.Load<T>(resourcesPath);
            if (res != null) return res;
        }

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
#else
        return null;
#endif
    }

    private static string ExtractResourcesPath(string path)
    {
        int idx = path.IndexOf("Resources/");
        if (idx >= 0)
        {
            string sub = path.Substring(idx + "Resources/".Length);
            return System.IO.Path.ChangeExtension(sub, null);
        }
        return null;
    }
}