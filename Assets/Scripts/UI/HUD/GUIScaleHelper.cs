using UnityEngine;

/// <summary>
/// IMGUI 统一缩放辅助类。
/// 所有 IMGUI OnGUI 方法应在绘制前调用 BeginScale / 绘制后调用 EndScale，
/// 使所有硬编码像素值以 1920×1080 为参考分辨率等比缩放。
/// </summary>
public static class GUIScaleHelper
{
    public const float REF_W = 1920f;
    public const float REF_H = 1080f;

    private static Matrix4x4 _originalMatrix;
    private static bool _scaling = false;

    /// <summary>
    /// 开始缩放 — 将所有 GUI 坐标按当前屏幕尺寸等比映射到参考分辨率。
    /// </summary>
    public static void BeginScale()
    {
        if (_scaling) return;

        float scaleX = Screen.width / REF_W;
        float scaleY = Screen.height / REF_H;
        float scale = Mathf.Min(scaleX, scaleY); // 保持宽高比

        _originalMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
        _scaling = true;
    }

    /// <summary>
    /// 结束缩放 — 恢复原始 GUI 矩阵。
    /// </summary>
    public static void EndScale()
    {
        if (!_scaling) return;
        GUI.matrix = _originalMatrix;
        _scaling = false;
    }

    /// <summary>
    /// 获取当前缩放比例
    /// </summary>
    public static float ScaleFactor => Mathf.Min(Screen.width / REF_W, Screen.height / REF_H);

    /// <summary>
    /// 获取缩放后的虚拟屏幕宽度（始终 = REF_W）
    /// </summary>
    public static float VirtualWidth => REF_W;

    /// <summary>
    /// 获取缩放后的虚拟屏幕高度（始终 = REF_H）
    /// </summary>
    public static float VirtualHeight => REF_H;
}