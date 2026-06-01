using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>
/// 主菜单场景创建器 — 创建包含 "Start Game" 按钮的主菜单。
/// 
/// 菜单：Tools → Create Menu Scene
/// 
/// 修复内容：
///   - 使用 InputSystemUIInputModule（New Input System 兼容）
///   - Start 按钮跳转到 GameScene
///   - 添加副标题和版本信息
/// </summary>
public class MenuSceneCreator
{
    [MenuItem("Tools/Create Menu Scene")]
    public static void CreateMenuScene()
    {
        // Play Mode 下禁止创建场景
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("提示",
                "请先退出 Play Mode（点击 Play 按钮停止），\n然后再执行 Tools → Create Menu Scene。", "确定");
            return;
        }

        // ── 1. 创建新场景 ──────────────────────────────────────
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ── 2. 设置相机背景为黑色 ──────────────────────────────
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
        }

        // ── 3. 删除默认 EventSystem ────────────────────────────
        var defaultES = Object.FindAnyObjectByType<EventSystem>();
        if (defaultES != null) Object.DestroyImmediate(defaultES.gameObject);

        // ── 4. 创建 Canvas ─────────────────────────────────────
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // ── 5. 创建 EventSystem（New Input System）──────────────
        CreateEventSystem();

        // ── 6. 创建背景面板 ────────────────────────────────────
        GameObject backgroundObj = new GameObject("Background");
        backgroundObj.transform.SetParent(canvasObj.transform, false);
        backgroundObj.transform.SetAsFirstSibling();
        Image bgImage = backgroundObj.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.08f, 1f);
        RectTransform bgRect = backgroundObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // ── 7. 创建游戏标题 ────────────────────────────────────
        GameObject titleObj = new GameObject("GameTitle");
        titleObj.transform.SetParent(canvasObj.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "Vampire Survivors";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 72;
        titleText.color = new Color(0.9f, 0.2f, 0.2f);
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontStyle = FontStyle.Bold;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.65f);
        titleRect.anchorMax = new Vector2(0.5f, 0.65f);
        titleRect.sizeDelta = new Vector2(800, 100);
        titleRect.anchoredPosition = Vector2.zero;

        // ── 8. 创建副标题 ──────────────────────────────────────
        GameObject subtitleObj = new GameObject("Subtitle");
        subtitleObj.transform.SetParent(canvasObj.transform, false);
        Text subtitleText = subtitleObj.AddComponent<Text>();
        subtitleText.text = "Unity Remake - Inc 0~7";
        subtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subtitleText.fontSize = 24;
        subtitleText.color = new Color(0.6f, 0.6f, 0.7f);
        subtitleText.alignment = TextAnchor.MiddleCenter;
        RectTransform subRect = subtitleObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 0.55f);
        subRect.anchorMax = new Vector2(0.5f, 0.55f);
        subRect.sizeDelta = new Vector2(600, 40);
        subRect.anchoredPosition = Vector2.zero;

        // ── 9. 创建 Start 按钮 ─────────────────────────────────
        GameObject buttonObj = new GameObject("StartButton");
        buttonObj.transform.SetParent(canvasObj.transform, false);
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 0.3f, 1f);
        Button startButton = buttonObj.AddComponent<Button>();
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.38f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.38f);
        buttonRect.sizeDelta = new Vector2(280, 65);

        // 按钮文字
        GameObject buttonTextObj = new GameObject("ButtonText");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);
        Text buttonText = buttonTextObj.AddComponent<Text>();
        buttonText.text = "Start Game";
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 32;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.fontStyle = FontStyle.Bold;
        RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.sizeDelta = Vector2.zero;

        // ── 10. 创建 Shop 按钮 ─────────────────────────────────
        GameObject shopButtonObj = new GameObject("ShopButton");
        shopButtonObj.transform.SetParent(canvasObj.transform, false);
        Image shopButtonImage = shopButtonObj.AddComponent<Image>();
        shopButtonImage.color = new Color(0.6f, 0.45f, 0.1f, 1f);
        Button shopBtn = shopButtonObj.AddComponent<Button>();
        RectTransform shopBtnRect = shopButtonObj.GetComponent<RectTransform>();
        shopBtnRect.anchorMin = new Vector2(0.5f, 0.28f);
        shopBtnRect.anchorMax = new Vector2(0.5f, 0.28f);
        shopBtnRect.sizeDelta = new Vector2(280, 55);

        GameObject shopButtonTextObj = new GameObject("ButtonText");
        shopButtonTextObj.transform.SetParent(shopButtonObj.transform, false);
        Text shopBtnText = shopButtonTextObj.AddComponent<Text>();
        shopBtnText.text = "Upgrade Shop";
        shopBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        shopBtnText.fontSize = 28;
        shopBtnText.color = Color.white;
        shopBtnText.alignment = TextAnchor.MiddleCenter;
        shopBtnText.fontStyle = FontStyle.Bold;
        RectTransform shopBtnTextRect = shopButtonTextObj.GetComponent<RectTransform>();
        shopBtnTextRect.anchorMin = Vector2.zero;
        shopBtnTextRect.anchorMax = Vector2.one;
        shopBtnTextRect.sizeDelta = Vector2.zero;

        // ── 11. 创建 Test Mode 按钮 ─────────────────────────────
        GameObject testButtonObj = new GameObject("TestButton");
        testButtonObj.transform.SetParent(canvasObj.transform, false);
        Image testButtonImage = testButtonObj.AddComponent<Image>();
        testButtonImage.color = new Color(0.7f, 0.2f, 0.2f, 1f);
        Button testBtn = testButtonObj.AddComponent<Button>();
        RectTransform testBtnRect = testButtonObj.GetComponent<RectTransform>();
        testBtnRect.anchorMin = new Vector2(0.5f, 0.18f);
        testBtnRect.anchorMax = new Vector2(0.5f, 0.18f);
        testBtnRect.sizeDelta = new Vector2(280, 45);

        GameObject testButtonTextObj = new GameObject("ButtonText");
        testButtonTextObj.transform.SetParent(testButtonObj.transform, false);
        Text testBtnText = testButtonTextObj.AddComponent<Text>();
        testBtnText.text = "Test Mode (T)";
        testBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        testBtnText.fontSize = 24;
        testBtnText.color = Color.white;
        testBtnText.alignment = TextAnchor.MiddleCenter;
        testBtnText.fontStyle = FontStyle.Bold;
        RectTransform testBtnTextRect = testButtonTextObj.GetComponent<RectTransform>();
        testBtnTextRect.anchorMin = Vector2.zero;
        testBtnTextRect.anchorMax = Vector2.one;
        testBtnTextRect.sizeDelta = Vector2.zero;

        // ── 12. 创建菜单场景引导器（运行时脚本）─────────────────
        GameObject menuBootstrap = new GameObject("MenuBootstrap");
        menuBootstrap.AddComponent<MenuSceneBootstrap>();

        // ── 11. 创建操作说明 ────────────────────────────────────
        GameObject helpObj = new GameObject("HelpText");
        helpObj.transform.SetParent(canvasObj.transform, false);
        Text helpText = helpObj.AddComponent<Text>();
        helpText.text = "WASD=Move  Mouse=Aim  E=Skill  ESC=Pause  R=Restart";
        helpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        helpText.fontSize = 16;
        helpText.color = new Color(0.5f, 0.5f, 0.5f);
        helpText.alignment = TextAnchor.MiddleCenter;
        RectTransform helpRect = helpObj.GetComponent<RectTransform>();
        helpRect.anchorMin = new Vector2(0.5f, 0.1f);
        helpRect.anchorMax = new Vector2(0.5f, 0.1f);
        helpRect.sizeDelta = new Vector2(600, 30);
        helpRect.anchoredPosition = Vector2.zero;

        // ── 13. 保存场景 ───────────────────────────────────────
        string scenePath = "Assets/Scenes/MenuScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        // ── 14. 更新 Build Settings ────────────────────────────
        UpdateBuildSettings(scenePath);

        Debug.Log("╔══════════════════════════════════════════════════════╗");
        Debug.Log("║  Menu Scene 创建完成！                               ║");
        Debug.Log("║  场景已保存到: Assets/Scenes/MenuScene.unity         ║");
        Debug.Log("║                                                      ║");
        Debug.Log("║  包含:                                               ║");
        Debug.Log("║  ✓ 游戏标题 + 副标题                                 ║");
        Debug.Log("║  ✓ Start Game 按钮（跳转到 GameScene）               ║");
        Debug.Log("║  ✓ Upgrade Shop 按钮（打开商店）                     ║");
        Debug.Log("║  ✓ 操作说明                                         ║");
        Debug.Log("║  ✓ New Input System 兼容                             ║");
        Debug.Log("╚══════════════════════════════════════════════════════╝");

        EditorUtility.DisplayDialog("Menu Scene 创建完成",
            "主菜单场景已创建成功！\n\n" +
            "包含：\n" +
            "• Vampire Survivors 标题\n" +
            "• Start Game 按钮\n" +
            "• Upgrade Shop 按钮\n" +
            "• 操作说明\n\n" +
            "已配置 Build Settings (菜单场景 → 游戏场景)",
            "确定");
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

        Debug.Log("[MenuSceneCreator] Created EventSystem with InputSystemUIInputModule");
    }

    // ═══════════════════════════════════════════════════════════
    // 更新 Build Settings
    // ═══════════════════════════════════════════════════════════

    private static void UpdateBuildSettings(string menuScenePath)
    {
        var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();

        // 菜单场景放第一个
        buildScenes.Add(new EditorBuildSettingsScene(menuScenePath, true));

        // 保留其他已有场景（如 GameScene）
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.path != menuScenePath)
                buildScenes.Add(s);
        }

        // 如果没有 GameScene，尝试添加
        string gameScenePath = "Assets/Scenes/GameScene.unity";
        bool hasGameScene = false;
        foreach (var s in buildScenes)
        {
            if (s.path.Contains("GameScene"))
            {
                hasGameScene = true;
                break;
            }
        }
        if (!hasGameScene && System.IO.File.Exists(gameScenePath))
        {
            buildScenes.Add(new EditorBuildSettingsScene(gameScenePath, true));
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
    }
}

