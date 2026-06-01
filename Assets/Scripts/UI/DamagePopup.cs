using UnityEngine;

/// <summary>
/// 浮动伤害数字组件 — 敌人受击时在头顶显示伤害值。
/// 
/// 功能：
/// - 受击时显示伤害数值（暴击用红色大字）
/// - 向上漂浮 + 淡出动画
/// - 使用对象池回收（0.8 秒后自动回收）
/// - 支持不同颜色（普通白色、暴击红色、治疗绿色）
/// 
/// 使用方式：通过 DamagePopup.Create() 静态方法创建
/// </summary>
public class DamagePopup : MonoBehaviour
{
    private const string POOL_KEY = "DamagePopup";
    private const float LIFETIME = 0.8f;
    private const float FLOAT_SPEED = 2f;
    private const float FADE_SPEED = 2f;

    private TextMesh _textMesh;
    private float _spawnTime;
    private Color _startColor;
    private float _floatDirection = 1f;

    private void Awake()
    {
        _textMesh = GetComponent<TextMesh>();
        if (_textMesh == null)
        {
            _textMesh = gameObject.AddComponent<TextMesh>();
            _textMesh.characterSize = 0.15f;
            _textMesh.anchor = TextAnchor.MiddleCenter;
            _textMesh.alignment = TextAlignment.Center;
            _textMesh.fontSize = 48;
            _textMesh.fontStyle = FontStyle.Bold;
        }

        // 确保渲染在最上层
        var sr = GetComponent<MeshRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 100;
        }
    }

    private void OnEnable()
    {
        _spawnTime = Time.time;
        _floatDirection = Random.Range(-0.3f, 0.3f); // 轻微随机水平偏移
    }

    private void Update()
    {
        float elapsed = Time.time - _spawnTime;
        float t = elapsed / LIFETIME;

        if (t >= 1f)
        {
            DespawnSelf();
            return;
        }

        // 向上漂浮 + 轻微水平偏移
        transform.position += new Vector3(_floatDirection * Time.deltaTime, FLOAT_SPEED * Time.deltaTime, 0f);

        // 淡出（后半段开始淡出）
        if (t > 0.5f)
        {
            float alpha = 1f - (t - 0.5f) * 2f;
            if (_textMesh != null)
            {
                Color c = _startColor;
                c.a = alpha;
                _textMesh.color = c;
            }
        }

        // 缩放动画（出现时放大，然后缩回）
        float scale = 1f;
        if (t < 0.1f)
        {
            scale = Mathf.Lerp(1.5f, 1f, t / 0.1f);
        }
        transform.localScale = Vector3.one * scale;
    }

    /// <summary>
    /// 设置伤害数字文本和颜色
    /// </summary>
    public void Setup(int damage, bool isCrit, bool isHeal = false)
    {
        if (_textMesh == null) return;

        if (isHeal)
        {
            _textMesh.text = $"+{damage}";
            _startColor = Color.green;
            _textMesh.characterSize = 0.12f;
        }
        else if (isCrit)
        {
            _textMesh.text = $"-{damage}!";
            _startColor = Color.red;
            _textMesh.characterSize = 0.2f; // 暴击更大
            _textMesh.fontStyle = FontStyle.Bold;
        }
        else
        {
            _textMesh.text = $"-{damage}";
            _startColor = new Color(1f, 1f, 0.8f); // 淡黄色
            _textMesh.characterSize = 0.12f;
        }

        _textMesh.color = _startColor;
    }

    private void DespawnSelf()
    {
        PoolHelper.DespawnOrDestroy(gameObject, POOL_KEY);
    }

    /// <summary>
    /// 在指定位置创建伤害数字
    /// </summary>
    public static void Create(Vector3 position, int damage, bool isCrit = false, bool isHeal = false)
    {
        // 确保池已注册
        EnsurePoolRegistered();

        // 偏移到头顶
        Vector3 popupPos = position + Vector3.up * 0.5f + Vector3.right * Random.Range(-0.3f, 0.3f);

        var pool = ObjectPool.Instance;
        if (pool != null && pool.HasPool(POOL_KEY))
        {
            var obj = pool.Spawn(POOL_KEY, popupPos, Quaternion.identity);
            if (obj != null)
            {
                var popup = obj.GetComponent<DamagePopup>();
                if (popup == null) popup = obj.AddComponent<DamagePopup>();
                popup.Setup(damage, isCrit, isHeal);
                return;
            }
        }

        // 池中没有，创建新对象
        var go = new GameObject("DamagePopup");
        go.transform.position = popupPos;

        var textMesh = go.AddComponent<TextMesh>();
        textMesh.characterSize = 0.12f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 48;
        textMesh.fontStyle = FontStyle.Bold;

        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 100;
            // 使用默认字体材质
            renderer.material = new Material(Shader.Find("GUI/Text Shader"));
        }

        var popup2 = go.AddComponent<DamagePopup>();
        popup2.Setup(damage, isCrit, isHeal);

        // 注册到池供后续复用
        if (pool != null && !pool.HasPool(POOL_KEY))
        {
            pool.RegisterPrefab(POOL_KEY, go);
        }

        Object.Destroy(go, LIFETIME + 0.1f);
    }

    private static bool _poolRegistered = false;
    private static void EnsurePoolRegistered()
    {
        if (_poolRegistered) return;
        _poolRegistered = true;

        var pool = ObjectPool.Instance;
        if (pool == null || pool.HasPool(POOL_KEY)) return;

        // 创建模板对象
        var template = new GameObject("DamagePopup_Template");
        var textMesh = template.AddComponent<TextMesh>();
        textMesh.characterSize = 0.12f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 48;
        textMesh.fontStyle = FontStyle.Bold;

        template.AddComponent<DamagePopup>();
        template.SetActive(false);

        pool.WarmUp(POOL_KEY, template, 10);
        Object.Destroy(template);
    }
}