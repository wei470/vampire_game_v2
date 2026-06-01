using UnityEngine;

/// <summary>
/// 全局引用缓存，避免热路径中使用 FindAnyObjectByType 等昂贵查找。
/// 在场景初始化时注入引用，场景卸载时清除。
/// </summary>
public static class GameReferences
{
    /// <summary>
    /// 玩家控制器引用
    /// </summary>
    public static PlayerController Player { get; set; }

    /// <summary>
    /// 敌人生成管理器引用
    /// </summary>
    public static SpawnManager SpawnManager { get; set; }

    /// <summary>
    /// 主摄像机引用
    /// </summary>
    public static Camera MainCamera { get; set; }

    /// <summary>
    /// 清除所有引用（场景卸载时调用）
    /// </summary>
    /// <summary>
    /// 测试模式标志 — 跳过选择流程，使用默认配置直接开始游戏
    /// </summary>
    public static bool TestMode { get; set; } = false;

    public static void Clear()
    {
        Player = null;
        SpawnManager = null;
        MainCamera = null;
        TestMode = false;
    }
}