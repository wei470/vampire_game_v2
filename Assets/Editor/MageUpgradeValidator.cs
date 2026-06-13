using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// MageUpgradeConfig 数据验证工具。
/// 在 Editor 窗口中一键检查所有 UpgradeEntry 和 DotGunEntry 的合法性。
///
/// 打开方式：菜单 Mage → Validate Upgrade Config
/// </summary>
public class MageUpgradeValidator : EditorWindow
{
    private MageUpgradeConfig _config;
    private Vector2 _scrollPos;
    private List<ValidationResult> _results = new List<ValidationResult>();
    private bool _hasValidated = false;

    private enum Severity { Error, Warning, Info }

    private struct ValidationResult
    {
        public Severity severity;
        public string target;   // "DotGun" 或 "Upgrade"
        public string entryId;
        public string message;

        public string Icon => severity switch
        {
            Severity.Error => "❌",
            Severity.Warning => "⚠️",
            Severity.Info => "ℹ️",
            _ => "?"
        };
    }

    [MenuItem("Mage/Validate Upgrade Config")]
    public static void ShowWindow()
    {
        var window = GetWindow<MageUpgradeValidator>("Mage 升级配置验证");
        window.minSize = new Vector2(500, 400);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("🔮 Mage Upgrade Config 验证器", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 配置引用
        EditorGUI.BeginChangeCheck();
        _config = (MageUpgradeConfig)EditorGUILayout.ObjectField(
            "MageUpgradeConfig", _config, typeof(MageUpgradeConfig), false);
        if (EditorGUI.EndChangeCheck())
        {
            _hasValidated = false;
            _results.Clear();
        }

        if (_config == null)
        {
            EditorGUILayout.HelpBox("请拖入 MageUpgradeConfig ScriptableObject。", MessageType.Info);

            // 尝试自动查找
            if (GUILayout.Button("自动查找 (Assets 下搜索)"))
            {
                var guids = AssetDatabase.FindAssets("t:MageUpgradeConfig");
                if (guids.Length > 0)
                {
                    _config = AssetDatabase.LoadAssetAtPath<MageUpgradeConfig>(
                        AssetDatabase.GUIDToAssetPath(guids[0]));
                    if (_config != null)
                    {
                        Debug.Log($"[Validator] 自动找到: {AssetDatabase.GetAssetPath(_config)}");
                    }
                }
                else
                {
                    Debug.LogWarning("[Validator] 未找到 MageUpgradeConfig asset。");
                }
            }
            return;
        }

        EditorGUILayout.Space(5);

        // 验证按钮
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("▶ 运行验证", GUILayout.Height(30)))
        {
            RunValidation();
        }
        GUI.backgroundColor = Color.white;

        if (_hasValidated)
        {
            int errors = _results.Count(r => r.severity == Severity.Error);
            int warnings = _results.Count(r => r.severity == Severity.Warning);
            int infos = _results.Count(r => r.severity == Severity.Info);

            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleRight,
                richText = true
            };
            EditorGUILayout.LabelField(
                $"<color=red>{errors} 错误</color>  <color=yellow>{warnings} 警告</color>  <color=gray>{infos} 信息</color>",
                style);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        if (!_hasValidated)
        {
            EditorGUILayout.HelpBox("点击「运行验证」检查配置合法性。", MessageType.Info);
            DrawConfigSummary();
            return;
        }

        // 结果列表
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        if (_results.Count == 0)
        {
            EditorGUILayout.HelpBox("✅ 全部通过！配置数据合法。", MessageType.Info);
        }
        else
        {
            // 按严重度分组
            DrawGroup("错误 (Error)", _results.Where(r => r.severity == Severity.Error).ToList());
            DrawGroup("警告 (Warning)", _results.Where(r => r.severity == Severity.Warning).ToList());
            DrawGroup("信息 (Info)", _results.Where(r => r.severity == Severity.Info).ToList());
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawConfigSummary()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("── 配置摘要 ──", EditorStyles.boldLabel);

        int dotGunCount = _config.dotGunEntries?.Length ?? 0;
        int upgradeCount = _config.upgradeEntries?.Length ?? 0;

        EditorGUILayout.LabelField($"  DOT 子弹枪: {dotGunCount} 种");
        EditorGUILayout.LabelField($"  升级选项: {upgradeCount} 项");
        EditorGUILayout.LabelField($"  总计: {dotGunCount + upgradeCount} 项");

        // 快速检查：是否有已弃用的 category
        var deprecatedCats = new HashSet<CharacterUpgradeOption.UpgradeCategory>
        {

        };

        if (_config.upgradeEntries != null)
        {
            var deprecatedUsed = _config.upgradeEntries
                .Where(e => deprecatedCats.Contains(e.category))
                .ToList();
            if (deprecatedUsed.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    $"⚠️ 发现 {deprecatedUsed.Count} 个使用已弃用 category 的条目: " +
                    string.Join(", ", deprecatedUsed.Select(e => e.upgradeId)),
                    MessageType.Warning);
            }
        }
    }

    private void DrawGroup(string title, List<ValidationResult> items)
    {
        if (items.Count == 0) return;

        EditorGUILayout.LabelField($"── {title} ({items.Count}) ──", EditorStyles.boldLabel);
        foreach (var r in items)
        {
            var msgColor = r.severity switch
            {
                Severity.Error => "<color=red>",
                Severity.Warning => "<color=yellow>",
                _ => "<color=gray>"
            };
            var style = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true };
            EditorGUILayout.LabelField(
                $"{r.Icon} [{r.target}] {r.entryId}: {msgColor}{r.message}</color>",
                style);
        }
        EditorGUILayout.Space(3);
    }

    // ═══ 验证逻辑 ═══

    private void RunValidation()
    {
        _results.Clear();
        _hasValidated = true;

        if (_config.dotGunEntries != null)
            ValidateDotGunEntries(_config.dotGunEntries);

        if (_config.upgradeEntries != null)
            ValidateUpgradeEntries(_config.upgradeEntries);

        // 交叉验证
        if (_config.dotGunEntries != null && _config.upgradeEntries != null)
            CrossValidate(_config.dotGunEntries, _config.upgradeEntries);

        // 汇总
        int errors = _results.Count(r => r.severity == Severity.Error);
        if (errors > 0)
            Debug.LogError($"[Validator] 发现 {errors} 个错误！请修复后重新验证。");
        else
            Debug.Log($"[Validator] 验证完成，无错误。{_results.Count} 条警告/信息。");
    }

    private void ValidateDotGunEntries(DotGunEntry[] entries)
    {
        var seenIds = new HashSet<string>();

        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            string tag = e.upgradeId ?? $"[index {i}]";

            // upgradeId 检查
            if (string.IsNullOrWhiteSpace(e.upgradeId))
                AddError("DotGun", tag, "upgradeId 为空");

            if (!string.IsNullOrEmpty(e.upgradeId) && !seenIds.Add(e.upgradeId))
                AddError("DotGun", tag, $"upgradeId '{e.upgradeId}' 重复");

            // displayName 检查
            if (string.IsNullOrWhiteSpace(e.displayName))
                AddWarning("DotGun", tag, "displayName 为空");

            // description 检查
            if (string.IsNullOrWhiteSpace(e.description))
                AddWarning("DotGun", tag, "description 为空");

            // cooldown 检查
            if (e.cooldown < 0)
                AddError("DotGun", tag, $"cooldown 为负数 ({e.cooldown})");
            else if (e.cooldown == 0)
                AddWarning("DotGun", tag, "cooldown 为 0（瞬间射击）");
            else if (e.cooldown > 10f)
                AddWarning("DotGun", tag, $"cooldown 过大 ({e.cooldown}s)，可能不合理");

            // impactDmg 检查
            if (e.impactDmg < 0)
                AddError("DotGun", tag, $"impactDmg 为负数 ({e.impactDmg})");

            // dotDps 检查
            if (e.dotDps < 0)
                AddError("DotGun", tag, $"dotDps 为负数 ({e.dotDps})");

            // dotDuration 检查
            if (e.dotDuration < 0)
                AddError("DotGun", tag, $"dotDuration 为负数 ({e.dotDuration})");

            // 逻辑：有 DPS 但无持续时间
            if (e.dotDps > 0 && e.dotDuration <= 0)
                AddWarning("DotGun", tag, "dotDps > 0 但 dotDuration = 0，DOT 伤害不会生效");

            // 逻辑：无 DPS 且无持续时间且无命中伤害
            if (e.dotDps <= 0 && e.dotDuration <= 0 && e.impactDmg <= 0)
                AddInfo("DotGun", tag, "无任何伤害输出（特殊子弹如 Dark/Light/Wind 可正常）");

            // color 检查
            if (e.color == default)
                AddWarning("DotGun", tag, "颜色未设置 (黑色)");
        }
    }

    private void ValidateUpgradeEntries(UpgradeEntry[] entries)
    {
        var seenIds = new HashSet<string>();
        var deprecatedCats = new HashSet<CharacterUpgradeOption.UpgradeCategory>
        {

        };

        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            string tag = e.upgradeId ?? $"[index {i}]";

            // upgradeId 检查
            if (string.IsNullOrWhiteSpace(e.upgradeId))
                AddError("Upgrade", tag, "upgradeId 为空");

            if (!string.IsNullOrEmpty(e.upgradeId) && !seenIds.Add(e.upgradeId))
                AddError("Upgrade", tag, $"upgradeId '{e.upgradeId}' 重复");

            // upgradeName 检查
            if (string.IsNullOrWhiteSpace(e.upgradeName))
                AddWarning("Upgrade", tag, "upgradeName 为空");

            // description 检查
            if (string.IsNullOrWhiteSpace(e.description))
                AddWarning("Upgrade", tag, "description 为空");

            // 已弃用 category
            if (deprecatedCats.Contains(e.category))
                AddWarning("Upgrade", tag, $"使用已弃用的 category: {e.category}");

            // value1 检查
            if (IsPercentageCategory(e.category) && e.value1 < 0)
                AddError("Upgrade", tag, $"百分比类型 {e.category} 的 value1 为负数 ({e.value1})");

            if (IsPercentageCategory(e.category) && e.value1 > 1f)
                AddWarning("Upgrade", tag, $"百分比类型 {e.category} 的 value1 > 100% ({e.value1})，请确认是否应为小数");

            // maxStacks 检查
            if (e.maxStacks < 0)
                AddError("Upgrade", tag, $"maxStacks 为负数 ({e.maxStacks})");

            if (e.maxStacks == 1)
                AddWarning("Upgrade", tag, "maxStacks = 1，意味着只能获取一次，是否应为 0（无限）？");

            // 特定 category 的数值范围检查
            ValidateCategorySpecificValues(e, tag);
        }
    }

    private void ValidateCategorySpecificValues(UpgradeEntry e, string tag)
    {
        switch (e.category)
        {
            case CharacterUpgradeOption.UpgradeCategory.ArmorReduction:
                if (e.value1 > 0.5f)
                    AddWarning("Upgrade", tag, $"护甲削减 {e.value1:P0}，超过 50% 可能过于强力");
                break;

            case CharacterUpgradeOption.UpgradeCategory.DotCritBurst:
                if (e.value1 > 0.5f)
                    AddWarning("Upgrade", tag, $"DOT 暴击率 {e.value1:P0}，超过 50% 可能过于强力");
                break;

            case CharacterUpgradeOption.UpgradeCategory.DetonateMultiplier:
                if (e.value1 > 1f)
                    AddWarning("Upgrade", tag, $"引爆伤害加成 {e.value1:P0}，超过 100% 可能过于强力");
                break;

            case CharacterUpgradeOption.UpgradeCategory.Ricochet:
                if (e.value1 < 1f && e.value1 > 0)
                    AddInfo("Upgrade", tag, $"穿透值 {e.value1} 小于 1，可能无效（应为整数）");
                break;

            case CharacterUpgradeOption.UpgradeCategory.BulletCount:
                if (e.value1 < 1f && e.value1 > 0)
                    AddInfo("Upgrade", tag, $"子弹数 {e.value1} 小于 1，可能无效（应为整数）");
                break;

            case CharacterUpgradeOption.UpgradeCategory.FrostExplosion:
                if (e.value1 > 0.1f)
                    AddWarning("Upgrade", tag, $"霜爆最大生命比 {e.value1:P0}，超过 10% 可能过于强力");
                break;
        }
    }

    private void CrossValidate(DotGunEntry[] dotGuns, UpgradeEntry[] upgrades)
    {
        // 检查 DotGun 和 Upgrade 是否有 ID 冲突
        var dotGunIds = new HashSet<string>(dotGuns.Select(g => g.upgradeId));
        foreach (var u in upgrades)
        {
            if (!string.IsNullOrEmpty(u.upgradeId) && dotGunIds.Contains(u.upgradeId))
                AddError("Cross", u.upgradeId, $"upgradeId 与 DotGunEntry 重复，可能导致查询冲突");
        }

        // 检查总数量是否合理
        int total = dotGuns.Length + upgrades.Length;
        if (total > 40)
            AddInfo("Cross", "总计", $"共 {total} 个选项，升级 UI 可能需要翻页");

        // 检查已弃用 category 的总数
        var deprecatedCats = new HashSet<CharacterUpgradeOption.UpgradeCategory>
        {

        };
        int deprecatedCount = upgrades.Count(u => deprecatedCats.Contains(u.category));
        if (deprecatedCount > 0)
            AddInfo("Cross", "弃用检查", $"发现 {deprecatedCount} 个使用已弃用 category 的条目，建议清理");
    }

    // ═══ 辅助方法 ═══

    private static bool IsPercentageCategory(CharacterUpgradeOption.UpgradeCategory cat)
    {
        // 大部分 Mage 专属 category 的 value1 是百分比（0~1）
        switch (cat)
        {
            case CharacterUpgradeOption.UpgradeCategory.ArmorReduction:
            case CharacterUpgradeOption.UpgradeCategory.DotFrequency:
            case CharacterUpgradeOption.UpgradeCategory.DotCritBurst:
            case CharacterUpgradeOption.UpgradeCategory.DetonateMultiplier:
            case CharacterUpgradeOption.UpgradeCategory.DetonateAbility:
            case CharacterUpgradeOption.UpgradeCategory.AttackSpeed:
            case CharacterUpgradeOption.UpgradeCategory.LightJudgment:
            case CharacterUpgradeOption.UpgradeCategory.FrostExplosion:
                return true;
            default:
                return false;
        }
    }

    private void AddError(string target, string entryId, string message)
        => _results.Add(new ValidationResult { severity = Severity.Error, target = target, entryId = entryId, message = message });

    private void AddWarning(string target, string entryId, string message)
        => _results.Add(new ValidationResult { severity = Severity.Warning, target = target, entryId = entryId, message = message });

    private void AddInfo(string target, string entryId, string message)
        => _results.Add(new ValidationResult { severity = Severity.Info, target = target, entryId = entryId, message = message });
}