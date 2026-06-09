using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System;
using System.IO;
using System.Linq;

/// <summary>
/// 一键打包工具 — 将游戏打包为 Windows EXE
/// 使用方式：
///   1. Unity 编辑器菜单：Build → 🎮 打包 Windows EXE
///   2. 命令行：Unity.exe -batchmode -projectPath "项目路径" -executeMethod Execute.BuildWindows -quit
/// </summary>
public static class Execute
{
    // ── 配置常量 ──────────────────────────────────────────
    private const string ProductName   = "VampireSurvivors";
    private const string OutputFolder  = "Build";          // 相对于项目根目录
    private const string ExecutableName = "VampireSurvivors.exe";

    // ── 菜单入口 ──────────────────────────────────────────
    [MenuItem("Build/🎮 打包 Windows EXE %#b")]   // Ctrl+Shift+B
    public static void BuildWindows()
    {
        BuildWindowsInternal(autoRun: true);
    }

    [MenuItem("Build/📦 打包（不自动运行）")]
    public static void BuildWindowsNoRun()
    {
        BuildWindowsInternal(autoRun: false);
    }

    [MenuItem("Build/🧹 清理构建输出")]
    public static void CleanBuild()
    {
        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputFolder));
        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive: true);
            Debug.Log($"<color=green>[Build]</color> 已清理构建输出: {fullPath}");
        }
        else
        {
            Debug.Log("[Build] 构建输出目录不存在，无需清理。");
        }
        AssetDatabase.Refresh();
    }

    // ── 核心构建逻辑 ──────────────────────────────────────
    private static void BuildWindowsInternal(bool autoRun)
    {
        // 1. 收集所有已启用的场景
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[Build] ❌ 没有启用的场景！请在 File → Build Settings 中添加场景。");
            return;
        }

        Debug.Log($"[Build] 收集到 {scenes.Length} 个场景:");
        foreach (var s in scenes)
            Debug.Log($"  → {s}");

        // 2. 准备输出路径
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputDir = Path.Combine(projectRoot, OutputFolder);
        string exePath = Path.Combine(outputDir, ExecutableName);

        // 确保输出目录存在
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        // 3. 配置构建选项
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = autoRun
                ? BuildOptions.AutoRunPlayer
                : BuildOptions.None
        };

        Debug.Log($"[Build] 🚀 开始打包...");
        Debug.Log($"[Build]    目标平台: Windows 64-bit");
        Debug.Log($"[Build]    输出路径: {exePath}");
        Debug.Log($"[Build]    场景数量: {scenes.Length}");

        // 4. 记录开始时间
        DateTime startTime = DateTime.Now;

        // 5. 执行构建
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        // 6. 输出结果
        TimeSpan elapsed = DateTime.Now - startTime;

        switch (summary.result)
        {
            case BuildResult.Succeeded:
                ulong sizeMB = summary.totalSize / (1024 * 1024);
                Debug.Log($"<color=green>[Build] ✅ 打包成功！</color>");
                Debug.Log($"[Build]    耗时: {elapsed.TotalSeconds:F1} 秒");
                Debug.Log($"[Build]    大小: {sizeMB} MB");
                Debug.Log($"[Build]    警告: {summary.totalWarnings} 个");
                Debug.Log($"[Build]    输出: {exePath}");

                // 打开输出文件夹
                if (autoRun)
                    EditorUtility.RevealInFinder(exePath);
                break;

            case BuildResult.Failed:
                Debug.LogError($"<color=red>[Build] ❌ 打包失败！</color>");
                Debug.LogError($"[Build]    耗时: {elapsed.TotalSeconds:F1} 秒");
                Debug.LogError($"[Build]    错误: {summary.totalErrors} 个");

                // 输出所有错误
                foreach (BuildStep step in report.steps)
                {
                    foreach (BuildStepMessage msg in step.messages)
                    {
                        if (msg.type == LogType.Error || msg.type == LogType.Exception)
                            Debug.LogError($"[Build]    {msg.content}");
                    }
                }
                break;

            case BuildResult.Cancelled:
                Debug.LogWarning("[Build] ⚠️ 打包已取消。");
                break;

            default:
                Debug.LogWarning($"[Build] 打包结果: {summary.result}");
                break;
        }
    }

    // ── 命令行调用入口（CI/CD 或批处理脚本使用）──────────
    /// <summary>
    /// 命令行一键打包，用法:
    ///   Unity.exe -batchmode -projectPath "D:\vampire_game_v2" -executeMethod Execute.CommandLineBuild -quit
    /// </summary>
    public static void CommandLineBuild()
    {
        // 命令行模式下不自动运行
        BuildWindowsInternal(autoRun: false);

        // 检查结果，设置退出码
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string exePath = Path.Combine(projectRoot, OutputFolder, ExecutableName);

        if (File.Exists(exePath))
        {
            Debug.Log("[Build] 命令行打包完成，退出码: 0");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError("[Build] 命令行打包失败，退出码: 1");
            EditorApplication.Exit(1);
        }
    }

    // ── 多平台打包（可选）──────────────────────────────────
    [MenuItem("Build/🖥️ 打包 Windows (32-bit)")]
    public static void BuildWindows32()
    {
        BuildMultiPlatform(BuildTarget.StandaloneWindows);
    }

    [MenuItem("Build/🐧 打包 Linux")]
    public static void BuildLinux()
    {
        BuildMultiPlatform(BuildTarget.StandaloneLinux64);
    }

    private static void BuildMultiPlatform(BuildTarget target)
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[Build] ❌ 没有启用的场景！");
            return;
        }

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string platformFolder = target switch
        {
            BuildTarget.StandaloneWindows    => "Build/Win32",
            BuildTarget.StandaloneWindows64  => "Build/Win64",
            BuildTarget.StandaloneLinux64    => "Build/Linux",
            _                                => $"Build/{target}"
        };

        string outputDir = Path.Combine(projectRoot, platformFolder);
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        string ext = target == BuildTarget.StandaloneLinux64 ? "" : ".exe";
        string exePath = Path.Combine(outputDir, ProductName + ext);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = exePath,
            target = target,
            options = BuildOptions.None
        };

        Debug.Log($"[Build] 🚀 开始打包 {target}...");
        Debug.Log($"[Build]    输出: {exePath}");

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            ulong sizeMB = summary.totalSize / (1024 * 1024);
            Debug.Log($"<color=green>[Build] ✅ {target} 打包成功！大小: {sizeMB} MB</color>");
        }
        else
        {
            Debug.LogError($"<color=red>[Build] ❌ {target} 打包失败！错误: {summary.totalErrors}</color>");
        }
    }
}