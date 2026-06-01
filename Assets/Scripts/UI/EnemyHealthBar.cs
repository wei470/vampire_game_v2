using UnityEngine;

/// <summary>
/// 敌人血条组件 — 在敌人头顶显示 HP 条。
/// 使用两个 SpriteRenderer（背景 + 填充）作为子对象，
/// 不依赖 Canvas，纯 2D Sprite 实现。
///
/// 使用方式：由 EnemyBase 在 OnEnable 时自动创建。
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    private SpriteRenderer _backgroundRenderer;
    private SpriteRenderer _fillRenderer;
    private Transform _barTransform;
    private Damageable _damageable;

    private float _barWidth = 0.8f;
    private float _barHeight = 0.08f;
    private float _offsetY = 0.6f;

    private static Sprite _whiteSprite;

    /// <summary>
    /// 初始化血条（由 EnemyBase 调用）
    /// </summary>
    public void Setup(Damageable damageable, float barWidth = 0.8f, float barHeight = 0.08f, float offsetY = 0.6f)
    {
        _damageable = damageable;
        _barWidth = barWidth;
        _barHeight = barHeight;
        _offsetY = offsetY;

        CreateBarVisuals();
        UpdateFill();
    }

    private void CreateBarVisuals()
    {
        if (_whiteSprite == null)
        {
            _whiteSprite = SpriteFactory.Square;
        }

        // 父容器（跟随敌人位置）
        _barTransform = new GameObject("HealthBar").transform;
        _barTransform.SetParent(transform);
        _barTransform.localPosition = new Vector3(0f, _offsetY, 0f);
        _barTransform.localRotation = Quaternion.identity;

        // 背景（黑色边框）
        var bgObj = new GameObject("HP_BG");
        bgObj.transform.SetParent(_barTransform);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = new Vector3(_barWidth, _barHeight, 1f);
        _backgroundRenderer = bgObj.AddComponent<SpriteRenderer>();
        _backgroundRenderer.sprite = _whiteSprite;
        _backgroundRenderer.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        _backgroundRenderer.sortingOrder = 10; // 确保在敌人之上

        // 填充条（绿色 → 黄色 → 红色）
        var fillObj = new GameObject("HP_Fill");
        fillObj.transform.SetParent(_barTransform);
        fillObj.transform.localPosition = Vector3.zero;
        fillObj.transform.localScale = new Vector3(_barWidth, _barHeight, 1f);
        _fillRenderer = fillObj.AddComponent<SpriteRenderer>();
        _fillRenderer.sprite = _whiteSprite;
        _fillRenderer.color = Color.green;
        _fillRenderer.sortingOrder = 11;
    }

    private void LateUpdate()
    {
        if (_damageable == null || _barTransform == null) return;

        UpdateFill();

        // 保持血条水平（不随敌人旋转）
        _barTransform.rotation = Quaternion.identity;
    }

    private void UpdateFill()
    {
        if (_damageable == null || _fillRenderer == null) return;

        float percent = _damageable.HpPercent;
        percent = Mathf.Clamp01(percent);

        // 缩放填充条宽度
        var fillScale = _fillRenderer.transform.localScale;
        fillScale.x = _barWidth * percent;
        _fillRenderer.transform.localScale = fillScale;

        // 偏移填充条使其左对齐（而非居中）
        var fillPos = _fillRenderer.transform.localPosition;
        fillPos.x = -_barWidth * (1f - percent) * 0.5f;
        _fillRenderer.transform.localPosition = fillPos;

        // 颜色渐变：绿 → 黄 → 红
        if (percent > 0.6f)
        {
            _fillRenderer.color = Color.Lerp(Color.yellow, Color.green, (percent - 0.6f) / 0.4f);
        }
        else if (percent > 0.3f)
        {
            _fillRenderer.color = Color.Lerp(Color.red, Color.yellow, (percent - 0.3f) / 0.3f);
        }
        else
        {
            _fillRenderer.color = Color.red;
        }

        // 满血时隐藏血条
        if (percent >= 1f)
        {
            _backgroundRenderer.enabled = false;
            _fillRenderer.enabled = false;
        }
        else
        {
            _backgroundRenderer.enabled = true;
            _fillRenderer.enabled = true;
        }
    }

    private void OnDestroy()
    {
        if (_barTransform != null)
        {
            Destroy(_barTransform.gameObject);
        }
    }
}