using System.Collections.Generic;

/// <summary>
/// 可重置接口 — 静态持有者实现此接口并自注册，
/// GameStateResetter.FullReset 遍历调用 ResetState，杜绝漏清。
/// </summary>
public interface IResettable
{
    void ResetState();
}

/// <summary>
/// 可重置注册表 — 集中管理所有 IResettable 实例。
/// 静态持有者在静态构造函数或初始化时调用 Register 注册。
/// </summary>
public static class ResettableRegistry
{
    private static readonly List<IResettable> _registered = new List<IResettable>();

    public static void Register(IResettable resettable)
    {
        if (resettable != null && !_registered.Contains(resettable))
            _registered.Add(resettable);
    }

    public static void Unregister(IResettable resettable)
    {
        _registered.Remove(resettable);
    }

    public static void ResetAll()
    {
        for (int i = _registered.Count - 1; i >= 0; i--)
        {
            try
            {
                _registered[i].ResetState();
            }
            catch (System.Exception e)
            {
                DebugHelper.LogError($"[ResettableRegistry] Reset failed: {e}");
            }
        }
    }

    public static void Clear()
    {
        _registered.Clear();
    }

    public static int Count => _registered.Count;
}
