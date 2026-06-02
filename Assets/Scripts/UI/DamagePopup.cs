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