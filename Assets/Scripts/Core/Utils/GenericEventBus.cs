using System;
using System.Collections.Generic;

/// <summary>
/// 泛型事件总线 — 从 EventManager 拆分而来
/// 提供类型安全的发布/订阅机制，支持任意数据类型
///
/// 使用方式：
///   订阅: GenericEventBus<MyEvent>.Subscribe(handler);
///   触发: GenericEventBus<MyEvent>.Publish(data);
///   取消: GenericEventBus<MyEvent>.Unsubscribe(handler);
/// </summary>
public static class GenericEventBus<T>
{
    private static event Action<T> _handlers;

    public static void Subscribe(Action<T> handler) => _handlers += handler;
    public static void Unsubscribe(Action<T> handler) => _handlers -= handler;
    public static void Publish(T data) => _handlers?.Invoke(data);
    public static void Clear() => _handlers = null;
    public static bool HasSubscribers => _handlers != null;
}

/// <summary>
/// 泛型事件注册表 — 管理所有泛型类型的清理
/// </summary>
public static class GenericEventRegistry
{
    private static readonly List<Action> _clearActions = new List<Action>();
    private static readonly HashSet<Type> _registeredTypes = new HashSet<Type>();

    /// <summary>
    /// 确保类型已注册到清理列表
    /// </summary>
    public static void EnsureRegistered<T>()
    {
        if (_registeredTypes.Add(typeof(T)))
        {
            _clearActions.Add(() => GenericEventBus<T>.Clear());
        }
    }

    /// <summary>
    /// 清除所有已注册的泛型事件
    /// </summary>
    public static void ClearAll()
    {
        for (int i = 0; i < _clearActions.Count; i++)
            _clearActions[i]?.Invoke();
        _registeredTypes.Clear();
    }
}