using System.Diagnostics;

/// <summary>
/// 调试日志工具类，在非编辑器构建中自动移除日志调用，
/// 避免 Build 中产生不必要的字符串拼接开销。
/// </summary>
public static class DebugHelper
{
    /// <summary>
    /// 输出普通日志（仅在编辑器中生效）
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message)
    {
        UnityEngine.Debug.Log(message);
    }

    /// <summary>
    /// 输出警告日志（仅在编辑器或 Development Build 中生效）
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message)
    {
        UnityEngine.Debug.LogWarning(message);
    }

    /// <summary>
    /// 输出错误日志（始终生效，不受条件编译限制）
    /// </summary>
    public static void LogError(string message)
    {
        UnityEngine.Debug.LogError(message);
    }
}