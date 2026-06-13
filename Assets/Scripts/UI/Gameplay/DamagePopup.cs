using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 伤害数字弹出显示 — 受伤时在实体头顶显示浮动数字
/// 使用 TextMesh 实现，无需 Canvas/EventSystem
/// 支持不同颜色：普通伤害白色，DOT伤害按类型着色，暴击放大震动
/// 
/// #13 优化：使用内部对象池避免频繁 Instantiate/Destroy，减少 GC
/// </summary>
public class DamagePopup : MonoBehaviour
{
    private float _lifetime = 0.8f;
    private float _moveSpeed = 2f;
    private float _timer;
    private TextMesh _textMesh;
    private TextMesh _prefixTextMesh;
    private Color _color;
    private MeshRenderer _meshRenderer;

    // ── 暴击震动动画 ──
    private bool _isCrit;
    private float _critShakeIntensity = 0.15f;
    private Vector3 _basePosition;

    // ── DOT 类型颜色常量 ──
    public static readonly Color ColorBleed  = new Color(0.9f, 0.15f, 0.15f);  // 流血红
    public static readonly Color ColorPoison = new Color(0.2f, 0.9f, 0.2f);    // 中毒绿
    public static readonly Color ColorBurn   = new Color(1f, 0.5f, 0.1f);      // 燃烧橙
    public static readonly Color ColorFrost  = new Color(0.4f, 0.7f, 1f);      // 霜冻蓝
    public static readonly Color ColorLightning = new Color(0.7f, 0.3f, 1f);   // 雷电紫
    public static readonly Color ColorDark   = new Color(0.5f, 0.1f, 0.7f);    // 黑暗暗紫
    public static readonly Color ColorLight  = new Color(1f, 1f, 0.8f);        // 光明白
    public static readonly Color ColorCrit   = new Color(1f, 0.85f, 0f);       // 暴击金色

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
    /// 确保前缀子 TextMesh 存在
    /// </summary>
    private void EnsurePrefixTextMesh()
    {
        if (_prefixTextMesh != null) return;

        var prefixGo = new GameObject("DotPrefix");
        prefixGo.transform.SetParent(transform, false);
        prefixGo.transform.localPosition = new Vector3(-0.35f, 0f, 0f);

        _prefixTextMesh = prefixGo.AddComponent<TextMesh>();
        _prefixTextMesh.font = _cachedFont;
    }

    private void HidePrefix()
    {
        if (_prefixTextMesh != null)
            _prefixTextMesh.gameObject.SetActive(false);
    }

    /// <summary>
    /// 回收弹字到池中
    /// </summary>
    private void ReturnToPool()
    {
        HidePrefix();
        gameObject.SetActive(false);
        transform.SetParent(_poolParent);
    }

    /// <summary>
    /// 重置弹字状态
    /// </summary>
    private void Reset(Vector3 position, Color color, float lifeTime, float moveSpd, bool isCrit = false)
    {
        transform.position = position + new Vector3(Random.Range(-0.3f, 0.3f), 0.5f, 0);
        _basePosition = transform.position;
        transform.SetParent(null); // 脱离池父级，放到世界空间
        _color = color;
        _lifetime = lifeTime;
        _moveSpeed = moveSpd;
        _timer = 0f;
        _isCrit = isCrit;

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
    public static void Create(Vector3 position, float damage, bool isCrit = false, bool isHeal = false)
    {
        Color color = isHeal ? new Color(0.3f, 1f, 0.3f) : Color.white;
        Create(position, damage, color, isCrit);
    }

    /// <summary>
    /// 创建指定颜色的伤害数字（自定义文本）
    /// </summary>
    public static void Create(Vector3 position, float damage, Color color, bool isCrit, string customText)
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
    public static void Create(Vector3 position, float damage, Color color, bool isCrit = false)
    {
        InitPool();
        var popup = GetFromPool();
        popup.Reset(position, color, isCrit ? 1.2f : 0.8f, isCrit ? 3f : 2f, isCrit);

        if (popup._textMesh != null)
        {
            popup._textMesh.text = isCrit ? damage.ToString("F2") + "!" : damage.ToString("F2");
            popup._textMesh.fontSize = isCrit ? 100 : 60;
            popup._textMesh.fontStyle = isCrit ? FontStyle.Bold : FontStyle.Normal;
            popup._textMesh.characterSize = isCrit ? 0.18f : 0.12f;
        }
        if (popup._meshRenderer != null)
            popup._meshRenderer.sortingOrder = isCrit ? 105 : 100;
    }

    private static string GetDotPrefix(string dotType) => dotType switch
    {
        "poison" => "毒",
        "burn" => "火",
        "frostbite" or "frost" => "冰",
        "static" or "lightning" => "雷",
        "wind" => "风",
        "dark" => "暗",
        "light" => "光",
        "bleed" => "血",
        _ => ""
    };

    /// <summary>
    /// 创建 DOT 伤害数字（按 DOT 类型着色，带类型前缀标识，字号比普通伤害大20%便于区分）
    /// </summary>
    public static void CreateDOT(Vector3 position, float damage, string dotType)
    {
        Color color = dotType switch
        {
            "bleed" => ColorBleed,
            "poison" => ColorPoison,
            "burn" => ColorBurn,
            "frost" => ColorFrost,
            "lightning" => ColorLightning,
            "dark" => ColorDark,
            "light" => ColorLight,
            _ => Color.white
        };
        InitPool();
        var popup = GetFromPool();
        popup.Reset(position, color, 0.8f, 2f, false);
        if (popup._textMesh != null)
        {
            popup._textMesh.text = damage.ToString("F2");
            popup._textMesh.fontSize = 72;
            popup._textMesh.fontStyle = FontStyle.Normal;
            popup._textMesh.characterSize = 0.14f;

            string prefix = GetDotPrefix(dotType);
            if (!string.IsNullOrEmpty(prefix))
            {
                popup.EnsurePrefixTextMesh();
                if (popup._prefixTextMesh != null)
                {
                    popup._prefixTextMesh.text = prefix;
                    popup._prefixTextMesh.fontSize = 40;
                    popup._prefixTextMesh.fontStyle = FontStyle.Normal;
                    popup._prefixTextMesh.characterSize = 0.08f;
                    popup._prefixTextMesh.color = color;
                    popup._prefixTextMesh.alignment = TextAlignment.Center;
                    popup._prefixTextMesh.anchor = TextAnchor.MiddleRight;

                    var prefixRenderer = popup._prefixTextMesh.GetComponent<MeshRenderer>();
                    if (prefixRenderer != null)
                        prefixRenderer.sortingOrder = 100;

                    popup._prefixTextMesh.gameObject.SetActive(true);
                }
            }
            else
            {
                popup.HidePrefix();
            }
        }
        if (popup._meshRenderer != null)
            popup._meshRenderer.sortingOrder = 100;
    }

    /// <summary>
    /// 创建 DOT 组合名称浮字（如 "碎冰!"、"爆燃!"）
    /// </summary>
    public static void CreateComboName(Vector3 position, string comboName, Color color)
    {
        Create(position, 0, color, false, comboName);
    }

    /// <summary>
    /// #17 创建屏幕中央的引爆总伤害数字（巨大字体，带"DETONATE!"前缀）
    /// </summary>
    public static void CreateDetonateTotal(Vector3 worldPos, float totalDamage, int enemyCount)
    {
        InitPool();
        var popup = GetFromPool();
        popup.Reset(worldPos + new Vector3(0f, 1.5f, 0), new Color(1f, 0.2f, 0.6f), 1.5f, 1f);

        if (popup._textMesh != null)
        {
            popup._textMesh.text = $"DETONATE!\n-{totalDamage:F2}";
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

        // 暴击震动效果
        if (_isCrit && _timer < 0.3f)
        {
            float shake = _critShakeIntensity * (1f - _timer / 0.3f);
            transform.position = _basePosition + Vector3.up * _moveSpeed * _timer
                + new Vector3(Random.Range(-shake, shake), Random.Range(-shake, shake), 0);
        }

        // 淡出
        if (_textMesh != null)
        {
            Color c = _textMesh.color;
            c.a = Mathf.Lerp(1f, 0f, _timer / _lifetime);
            _textMesh.color = c;
        }
        if (_prefixTextMesh != null && _prefixTextMesh.gameObject.activeSelf)
        {
            Color pc = _prefixTextMesh.color;
            pc.a = Mathf.Lerp(1f, 0f, _timer / _lifetime);
            _prefixTextMesh.color = pc;
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

    /// <summary>
    /// 完全清理对象池（FullReset 时调用，销毁 DontDestroyOnLoad 池父级及所有池化对象）
    /// 防止 DamagePopup 在 DontDestroyOnLoad 中永久残留
    /// </summary>
    public static void FullCleanup()
    {
        // 销毁所有池化对象
        if (_pool != null)
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null)
                    Object.Destroy(_pool[i].gameObject);
            }
            _pool.Clear();
        }

        // 销毁 DontDestroyOnLoad 池父级
        if (_poolParent != null)
        {
            Object.Destroy(_poolParent.gameObject);
            _poolParent = null;
        }

        _poolInitialized = false;
        _cachedFont = null;
    }
}