using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>
/// 游戏主场景创建器 — 一键创建包含所有系统的游戏场景。
/// 
/// 菜单：Tools → Create Game Scene
/// 
/// 创建内容：
///   - 玩家（蓝色方块 + PlayerController + Damageable + WeaponController + PlayerLevelSystem 
///          + PlayerSkillManager + EquipmentManager）
///   - SpawnManager（波次生成系统）
///   - HUD（Canvas: HP Bar + XP Bar + Level + Wave + Coins）
///   - LevelUpUI（升级选择面板）
///   - GameOverUI（游戏结束面板）
///   - EventSystem（New Input System 兼容）
///   - Camera（正交，跟随玩家）
///   - GameSceneBootstrap（装备掉落 + 调试 GUI）
///   - GridBackground（地图网格）
/// </summary>
public class GameSceneCreator
{
    [MenuItem("Tools/Create Game Scene")]
    public static void CreateGameScene()
    {
        // Play Mode 下禁止创建场景
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("提示",
                "请先退出 Play Mode（点击 Play 按钮停止），\n然后再执行 Tools → Create Game Scene。", "确定");
            return;
        }

        // ── 1. 创建新场景 ──────────────────────────────────────
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ── 2. 添加 Enemy Tag ──────────────────────────────────
        AddTagIfMissing("Enemy");
        AddTagIfMissing("Player");

        // ── 3. 删除默认 EventSystem ────────────────────────────
        var defaultES = Object.FindAnyObjectByType<EventSystem>();
        if (defaultES != null) Object.DestroyImmediate(defaultES.gameObject);

        // ── 4. 设置相机 ────────────────────────────────────────
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 12;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
            cam.transform.position = new Vector3(0, 0, -10);
        }

        // ── 5. 创建玩家 ────────────────────────────────────────
        GameObject player = CreatePlayer();

        // ── 6. 创建 SpawnManager ───────────────────────────────
        GameObject spawnObj = new GameObject("SpawnManager");
        spawnObj.AddComponent<SpawnManager>();

        // ── 7. 创建 HUD Canvas ─────────────────────────────────
        CreateHUD(player);

        // ── 8. 创建 LevelUp UI ─────────────────────────────────
        CreateLevelUpUI();

        // ── 9. 创建 GameOver UI ────────────────────────────────
        GameObject goMgr = new GameObject("GameOverManager");
        goMgr.AddComponent<GameOverUI>();

        // ── 10. 创建 SelectionApplier ──────────────────────────
        GameObject selApplier = new GameObject("SelectionApplier");
        selApplier.AddComponent<SelectionApplier>();

        // ── 11. 创建 GameSceneBootstrap ────────────────────────
        GameObject bootstrap = new GameObject("GameSceneBootstrap");
        bootstrap.AddComponent<GameSceneBootstrap>();

        // ── 12. 创建 GridBackground（挂在摄像机上，因为它需要 Camera 组件）──
        if (cam != null)
        {
            cam.gameObject.AddComponent<GridBackground>();
        }

        // ── 13. 创建 EventSystem（New Input System）─────────────
        CreateEventSystem();

        // ── 14. 保存场景 ───────────────────────────────────────
        string scenePath = "Assets/Scenes/GameScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        // ── 15. 更新 Build Settings ────────────────────────────
        UpdateBuildSettings(scenePath);

        Debug.Log("╔══════════════════════════════════════════════════════╗");
        Debug.Log("║  Game Scene 创建完成！                               ║");
        Debug.Log("║  场景已保存到: Assets/Scenes/GameScene.unity         ║");
        Debug.Log("║                                                      ║");
        Debug.Log("║  包含系统:                                           ║");
        Debug.Log("║  ✓ 玩家 (WASD移动 + 自动射击)                        ║");
        Debug.Log("║  ✓ 波次生成 (14种敌人)                               ║");
        Debug.Log("║  ✓ 武器系统 (8种武器)                                ║");
        Debug.Log("║  ✓ 升级系统 (经验 + 选择UI)                          ║");
        Debug.Log("║  ✓ 装备系统 (掉落 + 拾取 + 3槽位)                    ║");
        Debug.Log("║  ✓ HUD (HP/XP/等级/波次/金币)                        ║");
        Debug.Log("║  ✓ Game Over + Restart                               ║");
        Debug.Log("║                                                      ║");
        Debug.Log("║  操作: WASD=移动 鼠标=瞄准 R=重开 ESC=暂停           ║");
        Debug.Log("╚══════════════════════════════════════════════════════╝");

        EditorUtility.DisplayDialog("Game Scene 创建完成",
            "游戏主场景已创建成功！\n\n" +
            "包含：\n" +
            "• 玩家移动 + 自动射击\n" +
            "• 波次敌人生成（14种）\n" +
            "• 升级选择 UI\n" +
            "• 装备掉落/拾取系统\n" +
            "• HUD + Game Over\n\n" +
            "点击 Play 即可开始游戏！",
            "确定");
    }

    // ═══════════════════════════════════════════════════════════
    // 创建玩家
    // ═══════════════════════════════════════════════════════════

    private static GameObject CreatePlayer()
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Default");
        player.transform.position = Vector3.zero;

        // 外观
        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = new Color(0.2f, 0.4f, 1f);
        sr.sortingOrder = 10;

        // 物理
        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        CircleCollider2D col = player.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;

        // 核心组件
        player.AddComponent<BaseEntity>();
        player.AddComponent<Damageable>();  // 默认 100 HP

        // 玩家控制器
        player.AddComponent<PlayerController>();

        // 经验升级系统
        player.AddComponent<PlayerLevelSystem>();

        // 武器控制器
        WeaponController wc = player.AddComponent<WeaponController>();

        // 技能管理器
        player.AddComponent<PlayerSkillManager>();

        return player;
    }

    // ═══════════════════════════════════════════════════════════
    // 创建 HUD
    // ═══════════════════════════════════════════════════════════

    private static void CreateHUD(GameObject player)
    {
        // ── Canvas ──
        GameObject canvasObj = new GameObject("HUDCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // HUD 信息由 GameSceneBootstrap 的调试 GUI 显示，无需 Canvas 文字

        // ── HUDManager ──
        GameObject hudObj = new GameObject("HUDManager");
        hudObj.AddComponent<HUDManager>();
    }

    // ═══════════════════════════════════════════════════════════
    // 创建 LevelUp UI
    // ═══════════════════════════════════════════════════════════

    private static void CreateLevelUpUI()
    {
        // 查找 HUD Canvas
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // ── Panel（半透明背景）──
        GameObject panel = new GameObject("LevelUpPanel");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.6f);

        // ── 标题 ──
        GameObject titleObj = CreateUIElement("LevelUpTitle", panel,
            new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), new Vector2(400, 60));
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "Level Up!";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 48;
        titleText.color = Color.yellow;
        titleText.alignment = TextAnchor.MiddleCenter;

        // ── 3 个选项按钮 ──
        for (int i = 0; i < 3; i++)
        {
            float xPos = 0.3f + i * 0.2f;
            CreateLevelUpOption(panel, i, xPos);
        }

        // 关键：默认隐藏面板，否则会遮挡整个屏幕
        panel.SetActive(false);

        // ── LevelUpUI 组件 ──
        GameObject lvlUpObj = new GameObject("LevelUpManager");
        LevelUpUI lvlUp = lvlUpObj.AddComponent<LevelUpUI>();

        // 通过反射设置字段
        var panelField = typeof(LevelUpUI).GetField("_panel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var titleField = typeof(LevelUpUI).GetField("_titleText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (panelField != null) panelField.SetValue(lvlUp, panel);
        if (titleField != null) titleField.SetValue(lvlUp, titleText);

        // 设置按钮引用
        var btns = panel.GetComponentsInChildren<Button>(true);
        var texts = new Text[3];
        var buttons = new Button[3];

        int btnIdx = 0;
        foreach (var btn in btns)
        {
            if (btnIdx >= 3) break;
            buttons[btnIdx] = btn;
            texts[btnIdx] = btn.GetComponentInChildren<Text>();
            btnIdx++;
        }

        SetField(lvlUp, "_option1Button", buttons.Length > 0 ? buttons[0] : null);
        SetField(lvlUp, "_option2Button", buttons.Length > 1 ? buttons[1] : null);
        SetField(lvlUp, "_option3Button", buttons.Length > 2 ? buttons[2] : null);
        SetField(lvlUp, "_option1Text", texts.Length > 0 ? texts[0] : null);
        SetField(lvlUp, "_option2Text", texts.Length > 1 ? texts[1] : null);
        SetField(lvlUp, "_option3Text", texts.Length > 2 ? texts[2] : null);
    }

    private static void CreateLevelUpOption(GameObject parent, int index, float xPos)
    {
        // 按钮背景
        GameObject btnObj = new GameObject($"Option{index + 1}Button");
        btnObj.transform.SetParent(parent.transform, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.3f, 0.5f, 0.9f);
        Button btn = btnObj.AddComponent<Button>();

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(xPos, 0.3f);
        btnRect.anchorMax = new Vector2(xPos, 0.3f);
        btnRect.sizeDelta = new Vector2(200, 200);

        // 按钮文字
        GameObject textObj = new GameObject("OptionText");
        textObj.transform.SetParent(btnObj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.text = $"Option {index + 1}";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 20;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);
    }

    // ═══════════════════════════════════════════════════════════
    // 创建 EventSystem
    // ═══════════════════════════════════════════════════════════

    private static void CreateEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;

        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<EventSystem>();
        esObj.AddComponent<InputSystemUIInputModule>();

        Debug.Log("[GameSceneCreator] Created EventSystem with InputSystemUIInputModule");
    }

    // ═══════════════════════════════════════════════════════════
    // 更新 Build Settings
    // ═══════════════════════════════════════════════════════════

    private static void UpdateBuildSettings(string gameScenePath)
    {
        // 检查是否已有菜单场景
        string menuScenePath = null;
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.path.Contains("menu") || s.path.Contains("Menu"))
            {
                menuScenePath = s.path;
                break;
            }
        }

        var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();

        // 菜单场景放第一个（如果存在）
        if (!string.IsNullOrEmpty(menuScenePath))
        {
            buildScenes.Add(new EditorBuildSettingsScene(menuScenePath, true));
        }

        // 游戏场景
        buildScenes.Add(new EditorBuildSettingsScene(gameScenePath, true));

        // 保留其他已有场景
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.path != gameScenePath && s.path != menuScenePath)
            {
                buildScenes.Add(s);
            }
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
    }

    // ═══════════════════════════════════════════════════════════
    // 辅助方法
    // ═══════════════════════════════════════════════════════════

    private static void AddTagIfMissing(string tagName)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");

        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tagName)
                return;
        }

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
        tagManager.ApplyModifiedProperties();
        Debug.Log($"[GameSceneCreator] Added Tag: {tagName}");
    }

    private static Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(4, 4);
        Color[] colors = new Color[16];
        for (int i = 0; i < 16; i++) colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
    }

    /// <summary>
    /// 创建 UI 元素并设置 RectTransform 锚点
    /// </summary>
    private static GameObject CreateUIElement(string name, GameObject parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2? offsetMin = null, Vector2? offsetMax = null)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin ?? Vector2.zero;
        rect.offsetMax = offsetMax ?? Vector2.zero;
        return obj;
    }

    /// <summary>
    /// 创建 UI 元素（固定大小模式）
    /// </summary>
    private static GameObject CreateUIElement(string name, GameObject parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = Vector2.zero;
        return obj;
    }

    /// <summary>
    /// 通过反射设置私有字段
    /// </summary>
    private static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
        }
        else
        {
            Debug.LogWarning($"[GameSceneCreator] Field not found: {fieldName}");
        }
    }
}