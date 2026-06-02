using UnityEngine;

/// <summary>
/// 伤害数字弹出显示 — 受伤时在实体头顶显示浮动数字
/// 使用 TextMesh 实现，无需 Canvas/EventSystem
/// 支持不同颜色：普通伤害白色，DOT伤害按类型着色
/// </summary>
public class DamagePopup : MonoBehaviour
{
    private float _lifetime = 0.8f;
    private float _moveSpeed = 2f;
    private float _timer;
    private TextMesh _textMesh;
    private Color _color;

    /// <summary>
    /// 创建伤害数字（默认白色）
    /// </summary>
    public static void Create(Vector3 position, int damage, bool isCrit = false, bool isHeal = false)
    {
        Color color = isHeal ? new Color(0.3f, 1f, 0.3f) : Color.white;
        Create(position, damage, color, isCrit);
    }

    /// <summary>
    /// 创建指定颜色的伤害数字
    /// </summary>
    public static void Create(Vector3 position, int damage, Color color, bool isCrit = false)
    {
        var go = new GameObject("DamagePopup");
        go.transform.position = position + new Vector3(Random.Range(-0.3f, 0.3f), 0.5f, 0);

        var popup = go.AddComponent<DamagePopup>();
        popup._color = color;
        popup._lifetime = 0.8f;
        popup._moveSpeed = 2f;

        // 使用 TextMesh（无需 Canvas）
        popup._textMesh = go.AddComponent<TextMesh>();
        popup._textMesh.text = isCrit ? damage + "!" : damage.ToString();
        popup._textMesh.color = color;
        popup._textMesh.fontSize = isCrit ? 80 : 60;
        popup._textMesh.fontStyle = isCrit ? FontStyle.Bold : FontStyle.Normal;
        popup._textMesh.alignment = TextAlignment.Center;
        popup._textMesh.anchor = TextAnchor.MiddleCenter;
        popup._textMesh.characterSize = 0.12f;

        // 使用默认字体
        popup._textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 添加 MeshRenderer 设置
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingOrder = 100;
        }

        popup._timer = 0f;
    }

    /// <summary>
    /// #17 创建屏幕中央的引爆总伤害数字（巨大字体，带"DETONATE!"前缀）
    /// </summary>
    public static void CreateDetonateTotal(Vector3 worldPos, int totalDamage, int enemyCount)
    {
        // 世界坐标弹字 — 在玩家位置附近
        var go = new GameObject("DetonateTotalPopup");
        go.transform.position = worldPos + new Vector3(0f, 1.5f, 0);

        var popup = go.AddComponent<DamagePopup>();
        popup._color = new Color(1f, 0.2f, 0.6f); // 品红色
        popup._lifetime = 1.5f; // 更长持续时间
        popup._moveSpeed = 1f;  // 缓慢上升

        popup._textMesh = go.AddComponent<TextMesh>();
        popup._textMesh.text = $"DETONATE!\n-{totalDamage}";
        popup._textMesh.color = new Color(1f, 0.2f, 0.6f);
        popup._textMesh.fontSize = 120; // 巨大字体
        popup._textMesh.fontStyle = FontStyle.Bold;
        popup._textMesh.alignment = TextAlignment.Center;
        popup._textMesh.anchor = TextAnchor.MiddleCenter;
        popup._textMesh.characterSize = 0.2f;

        popup._textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 110;

        popup._timer = 0f;
    }

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

        // 销毁
        if (_timer >= _lifetime)
        {
            Destroy(gameObject);
        }
    }
}