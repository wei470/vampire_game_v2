using UnityEngine;
using UnityEditor;

/// <summary>
/// DotBulletConfig 自定义 Inspector — 在 Inspector 面板中以中文标签显示所有字段。
/// 英文字段名保持不变，仅改变 Inspector 的显示。
/// </summary>
[CustomEditor(typeof(DotBulletConfig))]
public class DotBulletConfigInspector : Editor
{
    private bool _showBurn = true;
    private bool _showPoison = true;
    private bool _showFrost = true;
    private bool _showLightning = true;
    private bool _showDark = true;
    private bool _showLight = true;
    private bool _showWind = true;
    private bool _showReaction = true;
    private bool _showHoming = true;

    public override void OnInspectorGUI()
    {
        var config = (DotBulletConfig)target;
        serializedObject.Update();

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("🎯 DOT 子弹数据驱动配置", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("所有参数在 Inspector 中以中文显示。\n" +
            "字段名保持英文以便代码引用。\n" +
            "修改后需保存（Ctrl+S）或点击 Apply。", MessageType.Info);
        EditorGUILayout.Space(4);

        // ── 燃烧子弹 ──
        _showBurn = EditorGUILayout.Foldout(_showBurn, "🔥 燃烧子弹（BurnBullet）", true, EditorStyles.foldoutHeader);
        if (_showBurn)
        {
            EditorGUI.indentLevel++;
            FloatField(config, "子弹飞行速度", "BurnSpeed", "单位：场景单位/秒");
            FloatField(config, "子弹存活时间", "BurnLifetime", "单位：秒，超时回收");
            FloatField(config, "基础每秒伤害(DPS)", "BurnBaseDps", "tick伤害 = DPS × tick间隔");
            FloatField(config, "效果持续时间", "BurnDuration", "命中刷新，超时消失");
            FloatField(config, "最小tick间隔", "BurnMinTickInterval", "层数再高也不会低于此值");
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(2);

        // ── 毒液子弹 ──
        _showPoison = EditorGUILayout.Foldout(_showPoison, "☠️ 毒液子弹（PoisonBullet）", true, EditorStyles.foldoutHeader);
        if (_showPoison)
        {
            EditorGUI.indentLevel++;
            FloatField(config, "子弹飞行速度", "PoisonSpeed", "无限射程");
            FloatField(config, "命中爆炸半径", "PoisonExplosionRadius", "溅射范围");
            FloatField(config, "毒液池半径", "PoisonPuddleRadius", "地面毒液区域大小");
            FloatField(config, "毒液池持续时间", "PoisonPuddleDuration", "单位：秒");
            FloatField(config, "毒液池tick间隔", "PoisonPuddleTickInterval", "每次伤害间隔");
            FloatField(config, "tick间隔衰减系数", "PoisonPuddleTickDecay", "每次×此系数，越到后面越密集");
            FloatField(config, "tick间隔最小值", "PoisonPuddleMinTickInterval", "间隔不会低于此值");
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(2);

        // ── 霜冻子弹 ──
        _showFrost = EditorGUILayout.Foldout(_showFrost, "❄️ 霜冻子弹（FrostBullet）", true, EditorStyles.foldoutHeader);
        if (_showFrost)
        {
            EditorGUI.indentLevel++;
            FloatField(config, "子弹飞行速度", "FrostSpeed", "速度较快");
            FloatField(config, "每层减速百分比", "FrostSlowPerStack", "0.05 = 每层+5%");
            FloatField(config, "基础减速百分比", "FrostBaseSlowPct", "首次命中即减速30%");
            FloatField(config, "效果持续时间", "FrostFreezeDuration", "命中刷新");
            FloatField(config, "减速上限", "FrostMaxSlow", "0.9 = 最高减速90%");
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(2);

        // ── 雷电子弹 ──
        _showLightning = EditorGUILayout.Foldout(_showLightning, "⚡ 雷电子弹（LightningBullet）", true, EditorStyles.foldoutHeader);
        if (_showLightning)
        {
            EditorGUI.indentLevel++;
            FloatField(config, "子弹飞行速度", "LightningSpeed", "");
            IntField(config, "连锁弹射次数", "LightningChainCount", "命中后弹射到附近敌人");
            FloatField(config, "连锁弹射半径", "LightningChainRadius", "搜索下一个目标的范围");
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("── 静电标记参数 ──", EditorStyles.miniLabel);
            FloatField(config, "放电基础间隔", "StaticBaseInterval", "单位：秒");
            FloatField(config, "每层减少放电间隔", "StaticStackReduction", "秒/层");
            FloatField(config, "放电间隔最小值", "StaticMinInterval", "不会低于此值");
            FloatField(config, "命中硬直时间", "StaticStunOnHit", "被子弹击中时");
            FloatField(config, "放电硬直时间", "StaticStunOnDischarge", "定时放电时");
            FloatField(config, "首次叠加硬直", "StaticStunOnFirstStack", "首次施加时较长");
            IntField(config, "最大叠加层数", "StaticMaxStacks", "");
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(2);

        // ── 暗影子弹 ──
        _showDark = EditorGUILayout.Foldout(_showDark, "🌑 暗影子弹（DarkBullet）", true, EditorStyles.foldoutHeader);
        if (_showDark)
        {
            EditorGUI.indentLevel++;
            FloatField(config, "子弹飞行速度", "DarkSpeed", "速度较慢");
            FloatField(config, "传播基础半径", "DarkBaseRadius", "敌人死亡时传播范围");
            FloatField(config, "传播基础效率", "DarkBaseEfficiency", "0.5 = 保留50%伤害");
            FloatField(config, "每级+传播半径", "DarkRadiusPerLevel", "升级时扩大");
            FloatField(config, "每级+传播效率", "DarkEfficiencyPerLevel", "升级时提升");
            FloatField(config, "标记受伤加深", "DarkMarkDamageBonus", "每层+此比例受伤");
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(2);

        // ── 光明子弹 ──
        _showLight = EditorGUILayout.Foldout(_showLight, "☀️ 光明子弹（LightBulletController）", true, EditorStyles.foldoutHeader);
        if (_showLight)
        {
            EditorGUI.indentLevel++;
            FloatField(config, "基础蓄力时间", "LightChargeDuration", "单位：秒");
            FloatField(config, "每级减少蓄力", "LightChargeReductionPerLevel", "秒/级");
            FloatField(config, "蓄力时间下限", "LightMinChargeDuration", "不会低于此值");
            IntField(config, "激光单次伤害", "LightLaserDamage", "");
            FloatField(config, "扫射角度", "LightSweepAngle", "单位：度");
            FloatField(config, "扫射持续时间", "LightSweepDuration", "单位：秒");
            FloatField(config, "激光长度", "LightLaserLength", "单位：场景单位");
            FloatField(config, "激光宽度", "LightLaserWidth", "影响命中判定");
            FloatField(config, "标记持续时间", "LightMarkDuration", "超时消失");
            IntField(config, "标记最大层数", "LightMarkMaxStacks", "0 = 无上限");
            FloatField(config, "每层受伤加成", "LightMarkDamagePerStack", "0.005 = 每层+0.5%");
            FloatField(config, "贴图缩放比例", "LightTextureScale", "视觉大小");
            FloatField(config, "发射后延迟", "LightPostFireDelay", "0 = 立即重新蓄力");
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(2);

        // ── 风蚀子弹 ──
        _showWind = EditorGUILayout.Foldout(_showWind, "💨 风蚀子弹（WindBullet）", true, EditorStyles.foldoutHeader);
        if (_showWind)
        {
            EditorGUI.indentLevel++;
            FloatField(config, "子弹飞行速度", "WindSpeed", "速度最快");
            IntField(config, "命中叠层间隔", "WindHitsPerStack", "每N次命中叠一层");
            FloatField(config, "基础击退距离", "WindKnockbackDistance", "单位：场景单位");

            // 散射角度数组用默认 Inspector 绘制
            var anglesProp = serializedObject.FindProperty("WindSpreadAngles");
            if (anglesProp != null)
                EditorGUILayout.PropertyField(anglesProp, new GUIContent("散射角度数组(度)"), true);

            FloatField(config, "风化击退距离", "WindErosionKnockbackDistance", "风化标记固定击退");
            IntField(config, "最大叠加层数", "WindMaxStacks", "超过不再增加");
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(2);

        // ── 元素反应 ──
        _showReaction = EditorGUILayout.Foldout(_showReaction, "🔥⚡ 元素反应参数", true, EditorStyles.foldoutHeader);
        if (_showReaction)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("── 燃烧 × 风化 → 燃烧扩散 ──", EditorStyles.miniLabel);
            FloatField(config, "扩散半径", "BurnSpreadRadius", "消耗一层风化传播燃烧");

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("── 霜冻 × 静电 → 霜电冰场 ──", EditorStyles.miniLabel);
            FloatField(config, "冰场半径", "FrostLightningFieldRadius", "");
            FloatField(config, "冰场持续时间", "FrostLightningFieldDuration", "单位：秒");
            FloatField(config, "冰场tick间隔", "FrostLightningTickInterval", "减速施加间隔");
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(2);

        // ── 追踪子弹 ──
        _showHoming = EditorGUILayout.Foldout(_showHoming, "🎯 追踪子弹（通用参数）", true, EditorStyles.foldoutHeader);
        if (_showHoming)
        {
            EditorGUI.indentLevel++;
            FloatField(config, "飞行速度", "HomingSpeed", "自动追踪敌人");
            FloatField(config, "存活时间", "HomingLifetime", "超时回收");
            FloatField(config, "搜索目标范围", "HomingTargetRadius", "超出此范围直线飞行");
            EditorGUI.indentLevel--;
        }

        // 应用修改
        if (GUI.changed)
        {
            EditorUtility.SetDirty(config);
        }
    }

    // ── 辅助绘制方法 ──

    private void FloatField(DotBulletConfig config, string chineseLabel, string fieldName, string tooltip)
    {
        var field = typeof(DotBulletConfig).GetField(fieldName);
        if (field == null) return;

        float currentValue = (float)field.GetValue(config);
        GUIContent content = new GUIContent(chineseLabel, tooltip + "\n字段名: " + fieldName);

        EditorGUI.BeginChangeCheck();
        float newValue = EditorGUILayout.FloatField(content, currentValue);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(config, $"修改 {chineseLabel}");
            field.SetValue(config, newValue);
        }
    }

    private void IntField(DotBulletConfig config, string chineseLabel, string fieldName, string tooltip)
    {
        var field = typeof(DotBulletConfig).GetField(fieldName);
        if (field == null) return;

        int currentValue = (int)field.GetValue(config);
        GUIContent content = new GUIContent(chineseLabel, tooltip + "\n字段名: " + fieldName);

        EditorGUI.BeginChangeCheck();
        int newValue = EditorGUILayout.IntField(content, currentValue);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(config, $"修改 {chineseLabel}");
            field.SetValue(config, newValue);
        }
    }
}