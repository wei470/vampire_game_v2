using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 一键打包工具 — 菜单 Tools → Build Windows EXE
/// 自动收集场景、打包为 Windows x64 可执行文件。
/// </summary>
public class BuildTool
{
    private const string BUILD_PATH = "Builds/VampireGame.exe";

    [MenuItem("Tools/Build Windows EXE")]
    public static void BuildWindows()
    {
        // 1. 收集所有场景
        string[] scenes = GetBuildScenes();
        if (scenes.Length == 0)
        {
            EditorUtility.DisplayDialog("打包失败", "没有找到任何场景！请在 Build Settings 中添加场景。", "确定");
            return;
        }

        // 2. 确保输出目录存在
        string outputDir = Path.GetDirectoryName(BUILD_PATH);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        // 3. 执行打包
        DebugHelper.Log("[BuildTool] 开始打包 Windows EXE...");
        DebugHelper.Log($"[BuildTool] 场景列表: {string.Join(", ", scenes)}");
        DebugHelper.Log($"[BuildTool] 输出路径: {Path.GetFullPath(BUILD_PATH)}");

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = BUILD_PATH,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            long sizeMB = (long)(report.summary.totalSize / (1024 * 1024));
            float timeSec = (float)report.summary.totalTime.TotalSeconds;
            string msg = $"打包成功！\n\n" +
                         $"输出: {Path.GetFullPath(BUILD_PATH)}\n" +
                         $"大小: {sizeMB} MB\n" +
                         $"耗时: {timeSec:F1} 秒";
            DebugHelper.Log($"[BuildTool] {msg}");
            EditorUtility.DisplayDialog("打包成功 ✅", msg, "打开文件夹");

            // 打开输出目录
            EditorUtility.RevealInFinder(Path.GetFullPath(BUILD_PATH));
        }
        else
        {
            string msg = $"打包失败: {report.summary.result}\n" +
                         $"错误数: {report.summary.totalErrors}\n" +
                         "请查看 Console 窗口的错误信息。";
            DebugHelper.LogError($"[BuildTool] {msg}");
            EditorUtility.DisplayDialog("打包失败 ❌", msg, "确定");
        }
    }

    [MenuItem("Tools/Build Windows EXE (Development)")]
    public static void BuildWindowsDevelopment()
    {
        string[] scenes = GetBuildScenes();
        if (scenes.Length == 0)
        {
            EditorUtility.DisplayDialog("打包失败", "没有找到任何场景！", "确定");
            return;
        }

        string devPath = "Builds/VampireGame_Dev.exe";
        string outputDir = Path.GetDirectoryName(devPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        DebugHelper.Log("[BuildTool] 开始打包 Development Build...");

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = devPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler
        };

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            DebugHelper.Log($"[BuildTool] Development 打包成功: {Path.GetFullPath(devPath)}");
            EditorUtility.RevealInFinder(Path.GetFullPath(devPath));
        }
        else
        {
            DebugHelper.LogError($"[BuildTool] Development 打包失败: {report.summary.result}");
            EditorUtility.DisplayDialog("打包失败 ❌", $"打包失败: {report.summary.result}", "确定");
        }
    }

    /// <summary>
    /// 获取 Build Settings 中所有已启用的场景
    /// </summary>
    private static string[] GetBuildScenes()
    {
        var scenes = new System.Collections.Generic.List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled && !string.IsNullOrEmpty(scene.path))
                scenes.Add(scene.path);
        }
        return scenes.ToArray();
    }
}