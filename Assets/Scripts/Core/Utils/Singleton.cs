using UnityEngine;

/// <summary>
/// 泛型单例基类，所有 Manager 类继承此类。
/// 保证全局唯一实例，跨场景存活。
/// </summary>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;

    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                return null; // 应用退出时静默返回，不打印警告
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    // 查找场景中是否已存在
                    _instance = FindAnyObjectByType<T>();

                    if (_instance == null)
                    {
                        // 创建新 GameObject
                        GameObject singletonObj = new GameObject($"[Singleton] {typeof(T).Name}");
                        _instance = singletonObj.AddComponent<T>();
                        DontDestroyOnLoad(singletonObj);
                    }
                }
                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            DebugHelper.LogWarning($"[Singleton] Duplicate {typeof(T).Name} detected, destroying {gameObject.name}");
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}