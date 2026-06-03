using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 伤害数字弹出显示 — 受伤时在实体头顶显示浮动数字
/// 使用 TextMesh 实现，无需 Canvas/EventSystem
/// 支持不同颜色：普通伤害白色，DOT伤害按类型着色
/// 
/// #13 优化：使用内部对象池避免频繁 Instantiate/Destroy，减少 GC
/// </summary>
public class DamagePopup : MonoBehaviour
{
    private float _lifetime = 0.8f;
    private float _moveSpeed = 2f;
    private float _timer;
    private TextMesh _textMesh;
    private Color _color;
    private MeshRenderer _meshRenderer;

    // ═══ #13 对象池 ═══
    private const int POOL_INITIAL_SIZE = 30;
    private const int POOL_MAX_SIZE = 50;
    private static List<DamagePopup> _pool = new List<DamagePopup>(POOL_INITIAL_SIZE);
    private static Transform _poolParent;
    private static Font _cachedFont;
    private static bool _poolInitialized = false;

    /// <summary>
    /// 初始化对象池（懒加载，首次使用时自动调用）
    /// </summary>
    private static void InitPool()
    {
        if (_poolInitialized) return;
        _poolInitialized = true;

        _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var parentGo = new GameObject("DamagePopupPool");
        _poolParent = parentGo.transform;
        Object.DontDestroyOnLoad(parentGo);

        // 预热对象池
        for (int i = 0; i < POOL_INITIAL_SIZE; i++)
        {
            CreatePoolInstance();
        }
    }

    /// <summary>
    /// 创建一个池化实例并放入池中
    /// </summary>
    private static DamagePopup CreatePoolInstance()
    {
        var go = new GameObject("DamagePopup");
        go.transform.SetParent(_poolParent);
        go.SetActive(false);

        var popup = go.AddComponent<DamagePopup>();
        popup._textMesh = go.AddComponent<TextMesh>();
        popup._textMesh.font = _cachedFont;
        popup._textMesh.alignment = TextAlignment.Center;
        popup._textMesh.anchor = TextAnchor.MiddleCenter;
        popup._textMesh.characterSize = 0.12f;

        popup._meshRenderer = go.GetComponent<MeshRenderer>();
        if (popup._meshRenderer != null)
            popup._meshRenderer.sortingOrder = 100;

        _pool.Add(popup);
        return popup;
    }

    /// <summary>
    /// 从池中获取一个 DamagePopup（无空闲时创建新实例，超过上限时复用最早的）
    /// </summary>
    private static DamagePopup GetFromPool()
    {
        // 搜索空闲的弹字
        for (int i = 0; i < _pool.Count; i++)
        {
            if (_pool[i] != null && !_pool[i].gameObject.activeInHierarchy)
            {
                return _pool[i];
            }
        }

        // 池满时复用最早创建的（索引0，通过移到末尾实现轮转）
        if (_pool.Count >= POOL_MAX_SIZE && _pool.Count > 0)
        {
            var oldest = _pool[0];
            _pool.RemoveAt(0);
            _pool.Add(oldest);
            oldest.gameObject.SetActive(false);
            return oldest;
        }

        // 池未满时创建新实例
        return CreatePoolInstance();
    }

    /// <summary>
    /// 回收弹字到池中
    /// </summary>
    private void ReturnToPool()
    {
        gameObject.SetActive(false);
        transform.SetParent(_poolParent);
    }

    /// <summary>
    /// 重置弹字状态
    /// </summary>
    private void Reset(Vector3 position, Color color, float lifeTime, float moveSpd)
    {
        transform.position = position + new Vector3(Random.Range(-0.3f, 0.3f), 0.5f, 0);
        transform.SetParent(null); // 脱离池父级，放到世界空间
        _color = color;
        _lifetime = lifeTime;
        _moveSpeed = moveSpd;
        _timer = 0f;

        if (_textMesh != null)
        {
            _textMesh.color = color;
            Color c = _textMesh.color;
            c.a = 1f;
            _textMesh.color = c;
        }

        if (_meshRenderer != null)
            _meshRenderer.sortingOrder = 100;

        gameObject.SetActive(true);
    }

    // ════════════════════════════════════════════════════════════════
    // 公共 API — 静态创建方法（对外接口不变）
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 创建伤害数字（默认白色）
    /// </summary>
    public static void Create(Vector3 position, int damage, bool isCrit = false, bool isHeal = false)
    {
        Color color = isHeal ? new Color(0.3f, 1f, 0.3f) : Color.white;
        Create(position, damage, color, isCrit);
    }

    /// <summary>
    /// 创建指定颜色的伤害数字（自定义文本）
    /// </summary>
    public static void Create(Vector3 position, int damage, Color color, bool isCrit, string customText)
    {
        InitPool();
        var popup = GetFromPool();
        popup.Reset(position, color, 0.8f, 2f);

        if (popup._textMesh != null)
        {
            popup._textMesh.text = customText;
            popup._textMesh.fontSize = 80;
            popup._textMesh.fontStyle = FontStyle.Bold;
            popup._textMesh.characterSize = 0.12f;
        }
        if (popup._meshRenderer != null)
            popup._meshRenderer.sortingOrder = 100;
    }

    /// <summary>
    /// 创建指定颜色的伤害数字
    /// </summary>
    public static void Create(Vector3 position, int damage, Color color, bool isCrit = false)
    {
        InitPool();
        var popup = GetFromPool();
        popup.Reset(position, color, 0.8f, 2f);

        if (popup._textMesh != null)
        {
            popup._textMesh.text = isCrit ? damage + "!" : damage.ToString();
            popup._textMesh.fontSize = isCrit ? 80 : 60;
            popup._textMesh.fontStyle = isCrit ? FontStyle.Bold : FontStyle.Normal;
            popup._textMesh.characterSize = 0.12f;
        }
        if (popup._meshRenderer != null)
            popup._meshRenderer.sortingOrder = 100;
    }

    /// <summary>
    /// #17 创建屏幕中央的引爆总伤害数字（巨大字体，带"DETONATE!"前缀）
    /// </summary>
    public static void CreateDetonateTotal(Vector3 worldPos, int totalDamage, int enemyCount)
    {
        InitPool();
        var popup = GetFromPool();
        popup.Reset(worldPos + new Vector3(0f, 1.5f, 0), new Color(1f, 0.2f, 0.6f), 1.5f, 1f);

        if (popup._textMesh != null)
        {
            popup._textMesh.text = $"DETONATE!\n-{totalDamage}";
            popup._textMesh.fontSize = 120;
            popup._textMesh.fontStyle = FontStyle.Bold;
            popup._textMesh.characterSize = 0.2f;
        }
        if (popup._meshRenderer != null)
            popup._meshRenderer.sortingOrder = 110;
    }

    // ════════════════════════════════════════════════════════════════
    // Update — 动画 + 回收
    // ════════════════════════════════════════════════════════════════

    private void Update()
    {
        _timer += Time.deltaTime;

        // 向上飘
        transform.position += Vector3.up * _moveSpeed * Time.deltaTime;

        // 淡出
        if (_textMesh != null)
        {
            Color c = _textMesh.color;
            c.a = Mathf.Lerp(1f, 0f, _timer / _lifetime);
            _textMesh.color = c;
        }

        // 回收到对象池（替代 Destroy）
        if (_timer >= _lifetime)
        {
            ReturnToPool();
        }
    }

    // ════════════════════════════════════════════════════════════════
    // 清理（场景重置时调用）
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 重置对象池（返回菜单/重启时调用）
    /// </summary>
    public static void ResetPool()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (_pool[i] != null && _pool[i].gameObject.activeInHierarchy)
            {
                _pool[i].ReturnToPool();
            }
        }
    }
}