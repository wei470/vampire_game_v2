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
        var charAssets = Resources.LoadAll<CharacterData>("");
        if (charAssets.Length > 0)
            return charAssets;

        // 尝试从 AssetDatabase 加载（编辑器模式）
        return new CharacterData[]
        {
            LoadAsset<CharacterData>("Assets/ScriptableObjects/Characters/Char_warrior.asset"),
            LoadAsset<CharacterData>("Assets/ScriptableObjects/Characters/Char_mage.asset"),
            LoadAsset<CharacterData>("Assets/ScriptableObjects/Characters/Char_ranger.asset"),
            LoadAsset<CharacterData>("Assets/ScriptableObjects/Characters/Char_vampire.asset"),
            LoadAsset<CharacterData>("Assets/ScriptableObjects/Characters/Char_assassin.asset"),
            LoadAsset<CharacterData>("Assets/ScriptableObjects/Characters/Char_paladin.asset"),
            LoadAsset<CharacterData>("Assets/ScriptableObjects/Characters/Char_necromancer.asset"),
            LoadAsset<CharacterData>("Assets/ScriptableObjects/Characters/Char_berserker.asset"),
        };
    }

    /// <summary>
    /// 加载技能数据数组
    /// </summary>
    public static SkillData[] LoadSkills()
    {
        return new SkillData[]
        {
            LoadAsset<SkillData>("Assets/ScriptableObjects/Skills/Skill_WindWave.asset"),
            LoadAsset<SkillData>("Assets/ScriptableObjects/Skills/Skill_Berserk.asset"),
            LoadAsset<SkillData>("Assets/ScriptableObjects/Skills/Skill_TheWorld.asset"),
            LoadAsset<SkillData>("Assets/ScriptableObjects/Skills/Skill_Teleport.asset"),
            LoadAsset<SkillData>("Assets/ScriptableObjects/Skills/Skill_DeathAura.asset"),
            LoadAsset<SkillData>("Assets/ScriptableObjects/Skills/Skill_LightningStorm.asset"),
            LoadAsset<SkillData>("Assets/ScriptableObjects/Skills/Skill_GravityWell.asset"),
            LoadAsset<SkillData>("Assets/ScriptableObjects/Skills/Skill_FrostNova.asset"),
        };
    }

    /// <summary>
    /// 获取武器数据数组（优先使用Inspector分配，否则从WeaponController获取）
    /// </summary>
    public static WeaponData[] GetWeaponDataArray(WeaponData[] inspectorWeapons)
    {
        if (inspectorWeapons != null && inspectorWeapons.Length > 0 && inspectorWeapons[0] != null)
            return inspectorWeapons;

        var wc = GameReferences.Player?.GetComponent<WeaponController>();
        if (wc != null)
            return wc.GetAllWeaponData();

        return new WeaponData[0];
    }

    /// <summary>
    /// 从AssetDatabase加载资源（编辑器专用）
    /// </summary>
    private static T LoadAsset<T>(string path) where T : UnityEngine.Object
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
#else
        return null;
#endif
    }
}