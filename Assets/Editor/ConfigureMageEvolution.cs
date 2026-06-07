#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 角色进化树配置工具 — EditorWindow 版本。
/// 可视化编辑所有角色的进化里程碑数据。
/// 
/// 使用方式：Unity 菜单栏 → VampireGame → Evolution Tree Editor
/// </summary>
public class EvolutionTreeEditorWindow : EditorWindow
{
    private Vector2 _scrollPos;
    private CharacterData[] _allCharacters;
    private int _selectedCharIndex;
    private bool[] _foldouts;

    [MenuItem("VampireGame/Evolution Tree Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<EvolutionTreeEditorWindow>("进化树编辑器");
        window.minSize = new Vector2(500, 400);
        window.LoadCharacters();
    }

    private void OnFocus()
    {
        LoadCharacters();
    }

    private void LoadCharacters()
    {
        // 搜索所有 CharacterData 资源
        string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { "Assets/ScriptableObjects/Characters" });
        var list = new List<CharacterData>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (data != null) list.Add(data);
        }
        _allCharacters = list.ToArray();
        _foldouts = new bool[_allCharacters.Length];
    }

    private void OnGUI()
    {
        if (_allCharacters == null || _allCharacters.Length == 0)
        {
            EditorGUILayout.HelpBox("未找到任何 CharacterData 资源。请确保 Assets/ScriptableObjects/Characters/ 下有 .asset 文件。", MessageType.Warning);
            if (GUILayout.Button("重新扫描")) LoadCharacters();
            return;
        }

        // 顶部工具栏
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            if (GUILayout.Button("重新扫描", EditorStyles.toolbarButton, GUILayout.Width(80)))
                LoadCharacters();

            GUILayout.FlexibleSpace();

            // 快速配置按钮
            if (GUILayout.Button("一键配置 Mage 进化树", EditorStyles.toolbarButton, GUILayout.Width(180)))
                QuickConfigureMage();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 角色列表
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        for (int i = 0; i < _allCharacters.Length; i++)
        {
            DrawCharacterEvolutionTree(i);
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 绘制单个角色的进化树编辑面板
    /// </summary>
    private void DrawCharacterEvolutionTree(int index)
    {
        var charData = _allCharacters[index];
        if (charData == null) return;

        string charLabel = $"{charData.characterName} ({charData.characterId})";
        int milestoneCount = charData.evolutionTree != null ? charData.evolutionTree.Length : 0;

        // 折叠标题
        _foldouts[index] = EditorGUILayout.Foldout(_foldouts[index],
            $"[{index}] {charLabel} — {(milestoneCount > 0 ? $"{milestoneCount}个里程碑" : "无进化树")}",
            true, EditorStyles.foldoutHeader);

        if (!_foldouts[index]) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical("box");

        // 进化树数组
        if (charData.evolutionTree == null || charData.evolutionTree.Length == 0)
        {
            EditorGUILayout.HelpBox("该角色尚未配置进化树。点击下方按钮添加。", MessageType.Info);
            if (GUILayout.Button("初始化进化树（4个默认里程碑）"))
            {
                charData.evolutionTree = CreateDefaultEvolutionTree(charData.characterId);
                EditorUtility.SetDirty(charData);
                AssetDatabase.SaveAssets();
            }
        }
        else
        {
            // 遍历每个里程碑
            for (int m = 0; m < charData.evolutionTree.Length; m++)
            {
                DrawMilestone(charData, m);
            }

            EditorGUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("+ 添加里程碑", GUILayout.Width(120)))
                {
                    var newArray = new EvolutionMilestone[charData.evolutionTree.Length + 1];
                    for (int j = 0; j < charData.evolutionTree.Length; j++)
                        newArray[j] = charData.evolutionTree[j];
                    newArray[newArray.Length - 1] = new EvolutionMilestone
                    {
                        milestoneId = $"{charData.characterId}_evo_{newArray.Length}",
                        displayName = "新进化",
                        description = "效果描述",
                        requiredLevel = 5 * newArray.Length,
                        effectType = EvolutionEffectType.DotDurationBonus,
                        value = 0.2f,
                        glowColor = Color.white
                    };
                    charData.evolutionTree = newArray;
                    EditorUtility.SetDirty(charData);
                }

                if (GUILayout.Button("清空进化树", GUILayout.Width(100)))
                {
                    if (EditorUtility.DisplayDialog("确认清空",
                        $"确定要清空 {charData.characterName} 的所有进化里程碑吗？", "确认", "取消"))
                    {
                        charData.evolutionTree = null;
                        EditorUtility.SetDirty(charData);
                    }
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("保存", GUILayout.Width(60)))
                {
                    EditorUtility.SetDirty(charData);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[EvolutionTreeEditor] 已保存 {charData.characterName} 的进化树数据");
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// 绘制单个里程碑的编辑面板
    /// </summary>
    private void DrawMilestone(CharacterData charData, int index)
    {
        var m = charData.evolutionTree[index];
        if (m == null) return;

        EditorGUILayout.BeginVertical("textField");
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField($"Lv{m.requiredLevel}", EditorStyles.boldLabel, GUILayout.Width(40));
                m.displayName = EditorGUILayout.TextField(m.displayName);
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    // 删除里程碑
                    var newList = new List<EvolutionMilestone>(charData.evolutionTree);
                    newList.RemoveAt(index);
                    charData.evolutionTree = newList.ToArray();
                    EditorUtility.SetDirty(charData);
                    return;
                }
            }
            EditorGUILayout.EndHorizontal();

            m.milestoneId = EditorGUILayout.TextField("ID", m.milestoneId);
            m.description = EditorGUILayout.TextField("描述", m.description);

            EditorGUILayout.BeginHorizontal();
            {
                m.requiredLevel = EditorGUILayout.IntField("解锁等级", m.requiredLevel);
                m.effectType = (EvolutionEffectType)EditorGUILayout.EnumPopup("效果类型", m.effectType);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                m.value = EditorGUILayout.FloatField("数值(value)", m.value);
                m.value2 = EditorGUILayout.FloatField("数值2(value2)", m.value2);
            }
            EditorGUILayout.EndHorizontal();

            m.glowColor = EditorGUILayout.ColorField("光效颜色", m.glowColor);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    /// <summary>
    /// 一键配置 Mage 进化树
    /// </summary>
    private void QuickConfigureMage()
    {
        CharacterData mageData = null;
        for (int i = 0; i < _allCharacters.Length; i++)
        {
            if (_allCharacters[i] != null && _allCharacters[i].characterId == "mage")
            {
                mageData = _allCharacters[i];
                break;
            }
        }

        if (mageData == null)
        {
            EditorUtility.DisplayDialog("错误", "未找到 characterId='mage' 的 CharacterData", "确定");
            return;
        }

        mageData.evolutionTree = CreateDefaultEvolutionTree("mage");
        EditorUtility.SetDirty(mageData);
        AssetDatabase.SaveAssets();

        Debug.Log("[EvolutionTreeEditor] ✅ Mage 进化树已配置！4个里程碑：Lv5元素亲和 / Lv10元素精通 / Lv15元素融合 / Lv20元素主宰");
        EditorUtility.DisplayDialog("成功",
            "Mage 进化树已配置完成！\n\n" +
            "• Lv5  元素亲和：DOT持续时间+20%\n" +
            "• Lv10 元素精通：DOT组合伤害+30%\n" +
            "• Lv15 元素融合：解锁融合子弹\n" +
            "• Lv20 元素主宰：引爆触发所有DOT组合",
            "确定");
    }

    /// <summary>
    /// 创建默认进化树模板（适用于 Mage 类型角色）
    /// </summary>
    private static EvolutionMilestone[] CreateDefaultEvolutionTree(string characterId)
    {
        return new EvolutionMilestone[]
        {
            new EvolutionMilestone
            {
                milestoneId = $"{characterId}_evo_dot_duration",
                displayName = "元素亲和 (Element Affinity)",
                description = "DOT持续时间 +20%",
                requiredLevel = 5,
                effectType = EvolutionEffectType.DotDurationBonus,
                value = 0.2f,
                value2 = 0f,
                glowColor = new Color(0.3f, 0.8f, 0.3f)
            },
            new EvolutionMilestone
            {
                milestoneId = $"{characterId}_evo_combo_damage",
                displayName = "元素精通 (Element Mastery)",
                description = "DOT组合伤害 +30%",
                requiredLevel = 10,
                effectType = EvolutionEffectType.DotComboDamageBonus,
                value = 0.3f,
                value2 = 0f,
                glowColor = new Color(0.2f, 0.5f, 1f)
            },
            new EvolutionMilestone
            {
                milestoneId = $"{characterId}_evo_fusion_unlock",
                displayName = "元素融合 (Element Fusion)",
                description = "解锁元素融合子弹系统",
                requiredLevel = 15,
                effectType = EvolutionEffectType.FusionUnlock,
                value = 1f,
                value2 = 0f,
                glowColor = new Color(1f, 0.6f, 0.9f)
            },
            new EvolutionMilestone
            {
                milestoneId = $"{characterId}_evo_detonate_combos",
                displayName = "元素主宰 (Element Sovereign)",
                description = "引爆时自动触发所有DOT组合效果",
                requiredLevel = 20,
                effectType = EvolutionEffectType.DetonateTriggerAllCombos,
                value = 1f,
                value2 = 0f,
                glowColor = new Color(1f, 0.85f, 0f)
            }
        };
    }
}
#endif