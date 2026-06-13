using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// DotEffectConfig 可视化配置编辑器。
/// 以表格形式展示所有子弹参数，支持 DPS 曲线预览。
///
/// 打开方式：菜单 Mage → Dot Bullet Config Editor
/// </summary>
public class DotEffectConfigEditor : EditorWindow
{
    private DotEffectConfig _config;
    private Vector2 _scrollPos;
    private bool _showDpsPanel = true;
    private bool _showDamagePanel = true;
    private bool _showSpeedPanel = true;
    private bool _showElementReaction = true;
    private bool _showSpecial = true;

    // DPS 预览参数
    private float _previewLevel = 1f;
    private float _previewDuration = 10f; // 预览时长（秒）
    private AnimationCurve _dpsCurve;

    // 颜色定义
    private static readonly Color BurnColor = new Color(1f, 0.4f, 0f);
    private static readonly Color PoisonColor = new Color(0.1f, 0.9f, 0.2f);
    private static readonly Color FrostColor = new Color(0.3f, 0.6f, 1f);
    private static readonly Color LightningColor = new Color(0.3f, 0.8f, 1f);
    private static readonly Color DarkColor = new Color(0.4f, 0.1f, 0.6f);
    private static readonly Color LightColor = new Color(1f, 1f, 0.9f);
    private static readonly Color WindColor = new Color(0.7f, 0.85f, 1f);

    [MenuItem("Mage/Dot Bullet Config Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<DotEffectConfigEditor>("DOT 子弹配置");
        window.minSize = new Vector2(600, 500);
    }

    private void OnEnable()
    {
        _dpsCurve = new AnimationCurve();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("🎯 DOT Bullet Config 可视化编辑器", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 配置引用
        EditorGUI.BeginChangeCheck();
        _config = (DotEffectConfig)EditorGUILayout.ObjectField(
            "DotEffectConfig", _config, typeof(DotEffectConfig), false);
        if (EditorGUI.EndChangeCheck())
            RebuildDpsCurve();

        if (_config == null)
        {
            EditorGUILayout.HelpBox("请拖入 DotEffectConfig ScriptableObject，或点击下方按钮自动查找。", MessageType.Info);
            if (GUILayout.Button("自动查找"))
            {
                var guids = AssetDatabase.FindAssets("t:DotEffectConfig");
                if (guids.Length > 0)
                {
                    _config = AssetDatabase.LoadAssetAtPath<DotEffectConfig>(
                        AssetDatabase.GUIDToAssetPath(guids[0]));
                    RebuildDpsCurve();
                }
            }
            return;
        }

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        // ═══ DPS 曲线预览 ═══
        _showDpsPanel = EditorGUILayout.Foldout(_showDpsPanel, "📈 DPS 曲线预览", true, EditorStyles.foldoutHeader);
        if (_showDpsPanel)
        {
            DrawDpsPanel();
        }

        EditorGUILayout.Space(10);

        // ═══ 伤害参数表格 ═══
        _showDamagePanel = EditorGUILayout.Foldout(_showDamagePanel, "⚔️ 伤害参数", true, EditorStyles.foldoutHeader);
        if (_showDamagePanel)
        {
            DrawDamageTable();
        }

        EditorGUILayout.Space(10);

        // ═══ 速度/射程参数 ═══
        _showSpeedPanel = EditorGUILayout.Foldout(_showSpeedPanel, "🚀 速度/射程参数", true, EditorStyles.foldoutHeader);
        if (_showSpeedPanel)
        {
            DrawSpeedTable();
        }

        EditorGUILayout.Space(10);

        // ═══ 元素反应参数 ═══
        _showElementReaction = EditorGUILayout.Foldout(_showElementReaction, "🔥 元素反应参数", true, EditorStyles.foldoutHeader);
        if (_showElementReaction)
        {
            DrawElementReactionTable();
        }

        EditorGUILayout.Space(10);

        // ═══ 特殊子弹参数 ═══
        _showSpecial = EditorGUILayout.Foldout(_showSpecial, "✨ 特殊子弹参数 (Dark/Light/Wind)", true, EditorStyles.foldoutHeader);
        if (_showSpecial)
        {
            DrawSpecialTable();
        }

        EditorGUILayout.EndScrollView();
    }

    // ═══ DPS 预览面板 ═══

    private void DrawDpsPanel()
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        _previewLevel = EditorGUILayout.Slider("预览等级", _previewLevel, 1, 20);
        _previewDuration = EditorGUILayout.Slider("预览时长(秒)", _previewDuration, 5, 60);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("刷新 DPS 曲线"))
            RebuildDpsCurve();

        if (_dpsCurve != null && _dpsCurve.length > 0)
        {
            var rect = GUILayoutUtility.GetRect(0, 200, GUILayout.ExpandWidth(true));
            EditorGUI.CurveField(rect, "DPS 预览 (等级 1~20)", _dpsCurve, Color.yellow,
                new Rect(1, 0, 19, GetMaxDps()));
        }

        // DPS 说明表
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("各子弹理论 DPS（等级 1）:", EditorStyles.boldLabel);
        DrawDpsSummary();

        EditorGUILayout.EndVertical();
    }

    private float GetMaxDps()
    {
        float max = 10f;
        if (_config != null)
        {
            max = Mathf.Max(max,
                _config.BurnBaseDps * 10f,
                _config.BurnBaseDps * 5f + _config.BurnSpeed);
        }
        return max * 1.2f;
    }

    private void RebuildDpsCurve()
    {
        if (_config == null) return;

        _dpsCurve = new AnimationCurve();

        // 燃烧 DPS 随等级变化（假设 DPS 线性增长）
        for (int lvl = 1; lvl <= 20; lvl++)
        {
            float burnDps = _config.BurnBaseDps * lvl; // 简化模型
            _dpsCurve.AddKey(lvl, burnDps);
        }
    }

    private void DrawDpsSummary()
    {
        EditorGUILayout.BeginVertical("box");

        DrawDpsRow("燃烧 (Burn)", BurnColor,
            $"基础DPS={_config.BurnBaseDps}, 持续={_config.BurnDuration}s, " +
            $"理论总伤={_config.BurnBaseDps * _config.BurnDuration:F1}");

        DrawDpsRow("毒液 (Poison)", PoisonColor,
            $"基础DPS=3.0 (来自 MageUpgradeConfig), 速度={_config.PoisonSpeed}");

        DrawDpsRow("霜冻 (Frostbite)", FrostColor,
            $"命中={_config.FrostSpeed}速度, 减速={_config.FrostBaseSlowPct:P0}/层" +
            $"{_config.FrostSlowPerStack:P0}");

        DrawDpsRow("雷电 (Lightning)", LightningColor,
            $"速度={_config.LightningSpeed}, 链弹={_config.LightningChainCount}次");

        DrawDpsRow("暗影 (Dark)", DarkColor,
            $"速度={_config.DarkSpeed}, 传播半径={_config.DarkBaseRadius}" +
            $"(每级+{_config.DarkRadiusPerLevel})");

        DrawDpsRow("光明 (Light)", LightColor,
            $"蓄力={_config.LightChargeDuration}s(最少{_config.LightMinChargeDuration}s), " +
            $"伤害={_config.LightLaserDamage}, 扫角={_config.LightSweepAngle}°");

        DrawDpsRow("风蚀 (Wind)", WindColor,
            $"速度={_config.WindSpeed}, 击退={_config.WindKnockbackDistance}, " +
            $"散射={_config.WindSpreadAngles.Length}发");

        EditorGUILayout.EndVertical();
    }

    private void DrawDpsRow(string label, Color color, string info)
    {
        var oldColor = GUI.backgroundColor;
        GUI.backgroundColor = color * 0.3f + Color.white * 0.7f;
        EditorGUILayout.BeginHorizontal("box");
        GUI.backgroundColor = oldColor;

        // 颜色标记
        var colorRect = GUILayoutUtility.GetRect(4, 18, GUILayout.Width(4));
        EditorGUI.DrawRect(colorRect, color);

        EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(120));
        EditorGUILayout.LabelField(info, EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndHorizontal();
    }

    // ═══ 伤害参数表格 ═══

    private void DrawDamageTable()
    {
        EditorGUILayout.BeginVertical("box");

        // 表头
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("子弹类型", EditorStyles.toolbarButton, GUILayout.Width(100));
        GUILayout.Label("命中伤害", EditorStyles.toolbarButton, GUILayout.Width(80));
        GUILayout.Label("DOT DPS", EditorStyles.toolbarButton, GUILayout.Width(80));
        GUILayout.Label("DOT 持续", EditorStyles.toolbarButton, GUILayout.Width(80));
        GUILayout.Label("理论总伤", EditorStyles.toolbarButton, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        // 燃烧
        DrawDamageRow("燃烧", BurnColor,
            "-", _config.BurnBaseDps.ToString("F1"), _config.BurnDuration.ToString("F1"),
            (_config.BurnBaseDps * _config.BurnDuration).ToString("F1"));

        // 毒液 (DPS 来自 MageUpgradeConfig)
        DrawDamageRow("毒液", PoisonColor, "-", "3.0*", "5.0*", "15.0*");

        // 霜冻
        DrawDamageRow("霜冻", FrostColor, "-", "-", _config.FrostFreezeDuration.ToString("F1"),
            $"控制(减速{_config.FrostBaseSlowPct:P0})");

        // 雷电
        DrawDamageRow("雷电", LightningColor, "-", "-", "-",
            $"链弹×{_config.LightningChainCount}");

        // 暗影
        DrawDamageRow("暗影", DarkColor, "-", "-", "-",
            "传播DOT");

        // 光明
        DrawDamageRow("光明", LightColor, _config.LightLaserDamage.ToString(), "-", "-",
            $"扫射{_config.LightSweepAngle}°");

        // 风蚀
        DrawDamageRow("风蚀", WindColor, "-", "-", "-",
            $"击退{_config.WindKnockbackDistance}");

        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField("* 数值来自 MageUpgradeConfig DotGunEntry", EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }

    private void DrawDamageRow(string type, Color color, string impact, string dps, string duration, string total)
    {
        EditorGUILayout.BeginHorizontal();

        var colorRect = GUILayoutUtility.GetRect(4, 18, GUILayout.Width(4));
        EditorGUI.DrawRect(colorRect, color);

        EditorGUILayout.LabelField(type, GUILayout.Width(100));
        EditorGUILayout.LabelField(impact, GUILayout.Width(80));
        EditorGUILayout.LabelField(dps, GUILayout.Width(80));
        EditorGUILayout.LabelField(duration, GUILayout.Width(80));
        EditorGUILayout.LabelField(total, GUILayout.Width(80));

        EditorGUILayout.EndHorizontal();
    }

    // ═══ 速度参数表格 ═══

    private void DrawSpeedTable()
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("子弹类型", EditorStyles.toolbarButton, GUILayout.Width(100));
        GUILayout.Label("速度", EditorStyles.toolbarButton, GUILayout.Width(80));
        GUILayout.Label("生存时间", EditorStyles.toolbarButton, GUILayout.Width(80));
        GUILayout.Label("理论射程", EditorStyles.toolbarButton, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        DrawSpeedRow("燃烧", BurnColor, _config.BurnSpeed, _config.BurnLifetime);
        DrawSpeedRow("毒液", PoisonColor, _config.PoisonSpeed, 5f);
        DrawSpeedRow("霜冻", FrostColor, _config.FrostSpeed, 4f);
        DrawSpeedRow("雷电", LightningColor, _config.LightningSpeed, 4f);
        DrawSpeedRow("暗影", DarkColor, _config.DarkSpeed, 5f);
        DrawSpeedRow("风蚀", WindColor, _config.WindSpeed, 4f);
        DrawSpeedRow("追踪", Color.white, _config.HomingSpeed, _config.HomingLifetime);

        EditorGUILayout.EndVertical();
    }

    private void DrawSpeedRow(string type, Color color, float speed, float lifetime)
    {
        EditorGUILayout.BeginHorizontal();

        var colorRect = GUILayoutUtility.GetRect(4, 18, GUILayout.Width(4));
        EditorGUI.DrawRect(colorRect, color);

        EditorGUILayout.LabelField(type, GUILayout.Width(100));
        EditorGUILayout.LabelField($"{speed:F1}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"{lifetime:F1}s", GUILayout.Width(80));
        EditorGUILayout.LabelField($"{speed * lifetime:F1}", GUILayout.Width(80));

        EditorGUILayout.EndHorizontal();
    }

    // ═══ 元素反应参数 ═══

    private void DrawElementReactionTable()
    {
        EditorGUILayout.BeginVertical("box");

        DrawParamRow("燃烧扩散半径", _config.BurnSpreadRadius, "F1", "BurnSpreadRadius");
        DrawParamRow("霜电冰场半径", _config.FrostLightningFieldRadius, "F1", "FrostLightningFieldRadius");
        DrawParamRow("霜电冰场持续", _config.FrostLightningFieldDuration, "F1", "FrostLightningFieldDuration");
        DrawParamRow("霜电冰场间隔", _config.FrostLightningTickInterval, "F2", "FrostLightningTickInterval");

        // DPS 计算
        float frostLightningDps = _config.FrostLightningFieldRadius > 0 ?
            5f / _config.FrostLightningTickInterval : 0; // 假设每次 5 伤害
        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField($"  霜电冰场理论 DPS: ~{frostLightningDps:F1}/s (假设每次 5 伤害)", EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }

    // ═══ 特殊子弹参数 ═══

    private void DrawSpecialTable()
    {
        EditorGUILayout.BeginVertical("box");

        // Dark
        EditorGUILayout.LabelField("🌑 暗影子弹", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        DrawParamRow("基础传播半径", _config.DarkBaseRadius, "F1", "DarkBaseRadius");
        DrawParamRow("基础传播效率", _config.DarkBaseEfficiency, "F2", "DarkBaseEfficiency");
        DrawParamRow("每级+半径", _config.DarkRadiusPerLevel, "F2", "DarkRadiusPerLevel");
        DrawParamRow("每级+效率", _config.DarkEfficiencyPerLevel, "F2", "DarkEfficiencyPerLevel");
        float darkMaxRadius = _config.DarkBaseRadius + _config.DarkRadiusPerLevel * 10;
        float darkMaxEff = _config.DarkBaseEfficiency + _config.DarkEfficiencyPerLevel * 10;
        EditorGUILayout.LabelField($"  10级时: 半径={darkMaxRadius:F1}, 效率={darkMaxEff:P0}", EditorStyles.miniLabel);
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(5);

        // Light
        EditorGUILayout.LabelField("☀️ 光明子弹", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        DrawParamRow("蓄力时间", _config.LightChargeDuration, "F1", "LightChargeDuration");
        DrawParamRow("每级减少蓄力", _config.LightChargeReductionPerLevel, "F2", "LightChargeReductionPerLevel");
        DrawParamRow("最小蓄力", _config.LightMinChargeDuration, "F1", "LightMinChargeDuration");
        DrawParamRow("激光伤害", _config.LightLaserDamage, "F0", "LightLaserDamage");
        DrawParamRow("扫角(度)", _config.LightSweepAngle, "F0", "LightSweepAngle");
        DrawParamRow("扫角持续", _config.LightSweepDuration, "F2", "LightSweepDuration");
        DrawParamRow("激光长度", _config.LightLaserLength, "F0", "LightLaserLength");
        DrawParamRow("激光宽度", _config.LightLaserWidth, "F1", "LightLaserWidth");
        DrawParamRow("标记持续", _config.LightMarkDuration, "F0", "LightMarkDuration");
        DrawParamRow("标记最大层", _config.LightMarkMaxStacks, "F0", "LightMarkMaxStacks");
        DrawParamRow("贴图比例", _config.LightTextureScale, "F1", "LightTextureScale");
        float lightMinCharge = Mathf.Max(_config.LightMinChargeDuration,
            _config.LightChargeDuration - _config.LightChargeReductionPerLevel * 10);
        EditorGUILayout.LabelField($"  10级时最少蓄力: {lightMinCharge:F1}s", EditorStyles.miniLabel);
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(5);

        // Wind
        EditorGUILayout.LabelField("💨 风蚀子弹", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        DrawParamRow("每次命中叠层", _config.WindHitsPerStack, "F0", "WindHitsPerStack");
        DrawParamRow("击退距离", _config.WindKnockbackDistance, "F2", "WindKnockbackDistance");
        string angles = _config.WindSpreadAngles != null ?
            string.Join(", ", _config.WindSpreadAngles) : "null";
        EditorGUILayout.LabelField($"  散射角度: [{angles}]", EditorStyles.miniLabel);
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(5);

        // Homing
        EditorGUILayout.LabelField("🎯 追踪子弹", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        DrawParamRow("追踪速度", _config.HomingSpeed, "F1", "HomingSpeed");
        DrawParamRow("生存时间", _config.HomingLifetime, "F1", "HomingLifetime");
        DrawParamRow("目标获取范围", _config.HomingTargetRadius, "F1", "HomingTargetRadius");
        EditorGUI.indentLevel--;

        EditorGUILayout.EndVertical();
    }

    // ═══ 辅助方法 ═══

    private void DrawParamRow(string label, float value, string format, string fieldName)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(160));

        EditorGUI.BeginChangeCheck();
        float newValue = EditorGUILayout.FloatField(float.Parse(value.ToString(format)));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_config, $"Change {fieldName}");
            var field = typeof(DotEffectConfig).GetField(fieldName);
            if (field != null)
            {
                field.SetValue(_config, newValue);
                EditorUtility.SetDirty(_config);
            }
        }

        EditorGUILayout.EndHorizontal();
    }
}