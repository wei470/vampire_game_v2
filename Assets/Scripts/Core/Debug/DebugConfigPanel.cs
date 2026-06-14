#pragma warning disable CS0414
using UnityEngine;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

/// <summary>
/// 调试配置面板 — 运行时热加载配置 + 实时参数修改。
/// 
/// 功能：
/// - F1 键：打开/关闭 Debug 面板
/// - F5 键：从 JSON 热加载配置
/// - 面板中可实时修改游戏参数（敌人血量倍率、DOT 伤害倍率、波次间隔等）
/// - 仅在 Development Build 或 Editor 中可用
/// 
/// JSON 路径：Application.persistentDataPath/Configs/GameConfig.json
/// </summary>
public class DebugConfigPanel : MonoBehaviour
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD

    private static readonly List<Collider2D> _overlapBuffer = new List<Collider2D>(16);

    /// <summary>
    /// Debug 面板是否打开
    /// </summary>
    public static bool IsOpen { get; private set; }

    /// <summary>
    /// 全局伤害倍率（Debug 面板调节，影响所有伤害）
    /// </summary>
    public static float DebugDamageMultiplier { get; private set; } = 1f;

    /// <summary>
    /// 全局敌人血量倍率（Debug 面板调节）
    /// </summary>
    public static float DebugEnemyHpMultiplier { get; private set; } = 1f;

    /// <summary>
    /// 全局 DOT 伤害倍率（Debug 面板调节）
    /// </summary>
    public static float DebugDotDamageMultiplier { get; private set; } = 1f;

    /// <summary>
    /// 全局敌人速度倍率（Debug 面板调节）
    /// </summary>
    public static float DebugEnemySpeedMultiplier { get; private set; } = 1f;

    private static Rect _windowRect = new Rect(10, 10, 420, 600);
    private static Vector2 _scrollPos;
    private static string _statusMessage = "";
    private static float _statusTimer;

    // 配置 JSON 路径
    private static string ConfigDirectory => Path.Combine(Application.persistentDataPath, "Configs");
    private static string GameConfigPath => Path.Combine(ConfigDirectory, "GameConfig.json");
    private static string WaveConfigPath => Path.Combine(ConfigDirectory, "EnemyWaveConfig.json");

    // 字段编辑缓存（用于文本输入）
    private static string _hpInput = "";
    private static string _speedInput = "";
    private static string _armorInput = "";
    private static string _attackInput = "";
    private static string _baseExpInput = "";
    private static string _expPerLevelInput = "";
    private static string _expRadiusInput = "";
    private static string _coinDropInput = "";
    private static string _dmgScaleInput = "";
    private static string _hpScaleInput = "";
    private static string _speedScaleInput = "";
    private static string _bossIntervalInput = "";
    private static string _bossBaseHpInput = "";
    private static string _bossHpPerWaveInput = "";
    private static string _spawnRadiusInput = "";
    private static string _waveBaseCountInput = "";
    private static string _wavePerWaveInput = "";
    private static string _restBetweenWavesInput = "";
    private static bool _fieldsInitialized = false;

    private void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        // F1 键切换 Debug 面板
        if (kb.f1Key.wasPressedThisFrame)
        {
            IsOpen = !IsOpen;
            if (IsOpen) InitFieldValues();
        }

        // F5 键热加载配置
        if (kb.f5Key.wasPressedThisFrame)
        {
            HotReloadConfig();
        }

        // 状态消息倒计时
        if (_statusTimer > 0)
        {
            _statusTimer -= Time.unscaledDeltaTime;
            if (_statusTimer <= 0) _statusMessage = "";
        }
    }

    /// <summary>
    /// 初始化文本字段值（从当前配置读取）
    /// </summary>
    private static void InitFieldValues()
    {
        var gc = ConfigLoader.Game;
        var wc = ConfigLoader.Wave;

        _hpInput = gc.basePlayerHP.ToString();
        _speedInput = gc.basePlayerSpeed.ToString("F1");
        _armorInput = gc.basePlayerArmor.ToString();
        _attackInput = gc.basePlayerAttack.ToString();
        _baseExpInput = gc.baseLevelUpExp.ToString();
        _expPerLevelInput = gc.expPerLevel.ToString();
        _expRadiusInput = gc.expPickupRadius.ToString("F1");
        _coinDropInput = gc.baseCoinDrop.ToString();
        _dmgScaleInput = gc.damageScalingPerWave.ToString("F2");
        _hpScaleInput = gc.hpScalingPerWave.ToString("F2");
        _speedScaleInput = gc.speedScalingPerWave.ToString("F2");
        _bossIntervalInput = gc.bossWaveInterval.ToString();
        _bossBaseHpInput = gc.bossBaseHP.ToString();
        _bossHpPerWaveInput = gc.bossHPPerWave.ToString();
        _spawnRadiusInput = gc.spawnRadius.ToString("F1");
        _waveBaseCountInput = wc.baseEnemyCount.ToString();
        _wavePerWaveInput = wc.enemiesPerWave.ToString();
        _restBetweenWavesInput = gc.restBetweenWaves.ToString("F1");
        _fieldsInitialized = true;
    }

    /// <summary>
    /// 从 JSON 文件热加载配置
    /// </summary>
    private static void HotReloadConfig()
    {
        bool anyLoaded = false;

        // 加载 GameConfig JSON
        if (File.Exists(GameConfigPath))
        {
            try
            {
                string json = File.ReadAllText(GameConfigPath);
                var gc = ConfigLoader.Game;
                JsonUtility.FromJsonOverwrite(json, gc);
                anyLoaded = true;
                DebugHelper.Log($"[DebugConfigPanel] GameConfig reloaded from: {GameConfigPath}");
            }
            catch (System.Exception e)
            {
                ShowStatus($"GameConfig 加载失败: {e.Message}", 5f);
                DebugHelper.LogError($"[DebugConfigPanel] GameConfig reload failed: {e.Message}");
                return;
            }
        }

        // 加载 EnemyWaveConfig JSON
        if (File.Exists(WaveConfigPath))
        {
            try
            {
                string json = File.ReadAllText(WaveConfigPath);
                var wc = ConfigLoader.Wave;
                JsonUtility.FromJsonOverwrite(json, wc);
                anyLoaded = true;
                DebugHelper.Log($"[DebugConfigPanel] EnemyWaveConfig reloaded from: {WaveConfigPath}");
            }
            catch (System.Exception e)
            {
                ShowStatus($"EnemyWaveConfig 加载失败: {e.Message}", 5f);
                DebugHelper.LogError($"[DebugConfigPanel] EnemyWaveConfig reload failed: {e.Message}");
                return;
            }
        }

        if (anyLoaded)
        {
            ShowStatus("✅ 配置已从 JSON 热加载！", 3f);
            InitFieldValues(); // 刷新面板显示
        }
        else
        {
            ShowStatus("⚠️ 未找到 JSON 配置文件，请先导出", 3f);
        }
    }

    /// <summary>
    /// 将当前配置导出为 JSON 文件
    /// </summary>
    private static void ExportConfigToJson()
    {
        try
        {
            if (!Directory.Exists(ConfigDirectory))
                Directory.CreateDirectory(ConfigDirectory);

            // 导出 GameConfig
            var gc = ConfigLoader.Game;
            string gcJson = JsonUtility.ToJson(gc, true);
            File.WriteAllText(GameConfigPath, gcJson);

            // 导出 EnemyWaveConfig
            var wc = ConfigLoader.Wave;
            string wcJson = JsonUtility.ToJson(wc, true);
            File.WriteAllText(WaveConfigPath, wcJson);

            ShowStatus($"✅ 配置已导出到: {ConfigDirectory}", 3f);
            DebugHelper.Log($"[DebugConfigPanel] Config exported to: {ConfigDirectory}");
        }
        catch (System.Exception e)
        {
            ShowStatus($"导出失败: {e.Message}", 5f);
            DebugHelper.LogError($"[DebugConfigPanel] Export failed: {e.Message}");
        }
    }

    /// <summary>
    /// 显示状态消息
    /// </summary>
    private static void ShowStatus(string message, float duration)
    {
        _statusMessage = message;
        _statusTimer = duration;
    }

    private void OnGUI()
    {
        if (!IsOpen) return;

        _windowRect = GUI.Window(9999, _windowRect, DrawWindow, "🔧 Debug Config Panel (F1 关闭)");
    }

    private static void DrawWindow(int id)
    {
        _scrollPos = GUILayout.BeginScrollView(_scrollPos);

        var gc = ConfigLoader.Game;
        var wc = ConfigLoader.Wave;

        // === 状态消息 ===
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            GUI.color = _statusMessage.StartsWith("✅") ? Color.green : Color.yellow;
            GUILayout.Label(_statusMessage);
            GUI.color = Color.white;
        }

        // === 快捷操作 ===
        GUILayout.Space(5);
        DrawSectionHeader("快捷操作");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("📂 导出配置到 JSON", GUILayout.Height(28)))
            ExportConfigToJson();
        if (GUILayout.Button("🔄 F5 热加载", GUILayout.Height(28)))
            HotReloadConfig();
        if (GUILayout.Button("📋 Dump Config", GUILayout.Height(28)))
            ConfigLoader.DumpConfig();
        GUILayout.EndHorizontal();

        // === 全局倍率调节 ===
        GUILayout.Space(8);
        DrawSectionHeader("全局倍率（实时生效）");

        DebugDamageMultiplier = DrawSlider("全局伤害倍率", DebugDamageMultiplier, 0.1f, 5f);
        DebugEnemyHpMultiplier = DrawSlider("敌人血量倍率", DebugEnemyHpMultiplier, 0.1f, 5f);
        DebugDotDamageMultiplier = DrawSlider("DOT 伤害倍率", DebugDotDamageMultiplier, 0.1f, 5f);
        DebugEnemySpeedMultiplier = DrawSlider("敌人速度倍率", DebugEnemySpeedMultiplier, 0.1f, 5f);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("重置倍率"))
        {
            DebugDamageMultiplier = 1f;
            DebugEnemyHpMultiplier = 1f;
            DebugDotDamageMultiplier = 1f;
            DebugEnemySpeedMultiplier = 1f;
        }
        if (GUILayout.Button("2x 难度"))
        {
            DebugEnemyHpMultiplier = 2f;
            DebugEnemySpeedMultiplier = 1.5f;
        }
        if (GUILayout.Button("0.5x 简单"))
        {
            DebugEnemyHpMultiplier = 0.5f;
            DebugDamageMultiplier = 0.5f;
        }
        GUILayout.EndHorizontal();

        // === 玩家基础属性 ===
        GUILayout.Space(8);
        DrawSectionHeader("玩家基础属性");
        if (_fieldsInitialized)
        {
            gc.basePlayerHP = DrawIntField("基础 HP", ref _hpInput, gc.basePlayerHP);
            gc.basePlayerSpeed = DrawFloatField("移动速度", ref _speedInput, gc.basePlayerSpeed);
            gc.basePlayerArmor = DrawIntField("基础护甲", ref _armorInput, gc.basePlayerArmor);
            gc.basePlayerAttack = DrawIntField("基础攻击", ref _attackInput, gc.basePlayerAttack);
        }

        // === 经验系统 ===
        GUILayout.Space(8);
        DrawSectionHeader("经验系统");
        if (_fieldsInitialized)
        {
            gc.baseLevelUpExp = DrawIntField("升级基础经验", ref _baseExpInput, gc.baseLevelUpExp);
            gc.expPerLevel = DrawIntField("每级经验增长", ref _expPerLevelInput, gc.expPerLevel);
            gc.expPickupRadius = DrawFloatField("经验拾取半径", ref _expRadiusInput, gc.expPickupRadius);
        }

        // === 金币系统 ===
        GUILayout.Space(8);
        DrawSectionHeader("金币系统");
        if (_fieldsInitialized)
        {
            gc.baseCoinDrop = DrawIntField("基础金币掉落", ref _coinDropInput, gc.baseCoinDrop);
        }

        // === 难度倍率 ===
        GUILayout.Space(8);
        DrawSectionHeader("难度曲线");
        if (_fieldsInitialized)
        {
            gc.damageScalingPerWave = DrawFloatField("伤害增长/波", ref _dmgScaleInput, gc.damageScalingPerWave);
            gc.hpScalingPerWave = DrawFloatField("血量增长/波", ref _hpScaleInput, gc.hpScalingPerWave);
            gc.speedScalingPerWave = DrawFloatField("速度增长/波", ref _speedScaleInput, gc.speedScalingPerWave);
        }

        // === Boss 配置 ===
        GUILayout.Space(8);
        DrawSectionHeader("Boss 配置");
        if (_fieldsInitialized)
        {
            gc.bossWaveInterval = DrawIntField("Boss 出现间隔（波）", ref _bossIntervalInput, gc.bossWaveInterval);
            gc.bossBaseHP = DrawIntField("Boss 基础 HP", ref _bossBaseHpInput, gc.bossBaseHP);
            gc.bossHPPerWave = DrawIntField("Boss HP 增长/波", ref _bossHpPerWaveInput, gc.bossHPPerWave);
        }

        // === 生成系统 ===
        GUILayout.Space(8);
        DrawSectionHeader("生成系统");
        if (_fieldsInitialized)
        {
            gc.spawnRadius = DrawFloatField("生成半径", ref _spawnRadiusInput, gc.spawnRadius);
            gc.restBetweenWaves = DrawFloatField("波次间隔（秒）", ref _restBetweenWavesInput, gc.restBetweenWaves);
        }

        // === 波次配置 ===
        GUILayout.Space(8);
        DrawSectionHeader("波次配置 (EnemyWaveConfig)");
        if (_fieldsInitialized)
        {
            wc.baseEnemyCount = DrawIntField("基础敌人数量", ref _waveBaseCountInput, wc.baseEnemyCount);
            wc.enemiesPerWave = DrawIntField("每波增加敌人", ref _wavePerWaveInput, wc.enemiesPerWave);
        }

        // === DOT 注入器 ===
        GUILayout.Space(8);
        DrawSectionHeader("DOT 注入器");
        GUILayout.Label("点击给最近敌人施加DOT:");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("毒x5", GUILayout.Width(50))) InjectDOT(StatusEffectType.Poison, 5);
        if (GUILayout.Button("火x5", GUILayout.Width(50))) InjectDOT(StatusEffectType.Burn, 5);
        if (GUILayout.Button("冰x5", GUILayout.Width(50))) InjectDOT(StatusEffectType.Frostbite, 5);
        if (GUILayout.Button("雷x5", GUILayout.Width(50))) InjectDOT(StatusEffectType.Static, 5);
        if (GUILayout.Button("暗x3", GUILayout.Width(50))) InjectDOT(StatusEffectType.Dark, 3);
        if (GUILayout.Button("光x3", GUILayout.Width(50))) InjectDOT(StatusEffectType.Light, 3);
        GUILayout.EndHorizontal();

        // === 配置文件信息 ===
        GUILayout.Space(8);
        DrawSectionHeader("配置文件路径");
        GUILayout.Label($"GameConfig: {GameConfigPath}");
        GUILayout.Label($"WaveConfig: {WaveConfigPath}");

        bool gcExists = File.Exists(GameConfigPath);
        bool wcExists = File.Exists(WaveConfigPath);
        GUI.color = (gcExists && wcExists) ? Color.green : Color.yellow;
        GUILayout.Label($"GameConfig JSON: {(gcExists ? "✅ 存在" : "❌ 不存在")}");
        GUILayout.Label($"WaveConfig JSON: {(wcExists ? "✅ 存在" : "❌ 不存在")}");
        GUI.color = Color.white;

        GUILayout.Space(5);
        GUILayout.EndScrollView();
        GUI.DragWindow();
    }

    /// <summary>
    /// 注入DOT到最近敌人
    /// </summary>
    private static void InjectDOT(StatusEffectType type, int stacks)
    {
        var player = GameReferences.Player;
        if (player == null) return;

        int count = PhysicsHelper.OverlapCircle(player.transform.position, 30f, _overlapBuffer);
        float nearest = float.MaxValue;
        GameObject nearestEnemy = null;
        for (int i = 0; i < count; i++)
        { var col = _overlapBuffer[i];
            if (!col.CompareTag("Enemy")) continue;
            float dist = Vector2.Distance(player.transform.position, col.transform.position);
            if (dist < nearest) { nearest = dist; nearestEnemy = col.gameObject; }
        }

        if (nearestEnemy == null) { DebugHelper.Log("[Debug] No enemy found"); return; }

        DotBulletHelper.EnsureStatusEffectManager(nearestEnemy);
        var sem = nearestEnemy.GetComponent<StatusEffectManager>();
        if (sem == null) return;

        for (int i = 0; i < stacks; i++)
            sem.ApplyEffect(type, 5f, 10f, false, 0f, 0f);

        DebugHelper.Log($"[Debug] Injected {stacks}x {type} on {nearestEnemy.name}");
    }

    /// <summary>
    /// 绘制分段标题
    /// </summary>
    private static void DrawSectionHeader(string title)
    {
        GUI.color = new Color(0.4f, 0.8f, 1f);
        GUILayout.Label($"── {title} ──");
        GUI.color = Color.white;
    }

    /// <summary>
    /// 绘制滑动条（返回新值）
    /// </summary>
    private static float DrawSlider(string label, float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {value:F2}", GUILayout.Width(180));
        float newValue = GUILayout.HorizontalSlider(value, min, max);
        if (GUILayout.Button("R", GUILayout.Width(24)))
            newValue = 1f;
        GUILayout.EndHorizontal();
        return newValue;
    }

    /// <summary>
    /// 绘制 Int 输入字段
    /// </summary>
    private static int DrawIntField(string label, ref string input, int currentValue)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label + ":", GUILayout.Width(160));
        input = GUILayout.TextField(input, GUILayout.Width(100));
        if (int.TryParse(input, out int newValue) && newValue != currentValue)
        {
            return newValue;
        }
        GUILayout.EndHorizontal();
        return currentValue;
    }

    /// <summary>
    /// 绘制 Float 输入字段
    /// </summary>
    private static float DrawFloatField(string label, ref string input, float currentValue)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label + ":", GUILayout.Width(160));
        input = GUILayout.TextField(input, GUILayout.Width(100));
        if (float.TryParse(input, out float newValue) && Mathf.Abs(newValue - currentValue) > 0.001f)
        {
            return newValue;
        }
        GUILayout.EndHorizontal();
        return currentValue;
    }

    #else
    // Release Build 中所有方法为空实现
    public static bool IsOpen => false;
    public static float DebugDamageMultiplier => 1f;
    public static float DebugEnemyHpMultiplier => 1f;
    public static float DebugDotDamageMultiplier => 1f;
    public static float DebugEnemySpeedMultiplier => 1f;
    #endif
}