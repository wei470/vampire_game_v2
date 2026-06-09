using UnityEngine;

/// <summary>
/// 敌人血条组件 — 在敌人头顶显示 HP 条。
/// 使用两个 SpriteRenderer（背景 + 填充）作为子对象，
/// 不依赖 Canvas，纯 2D Sprite 实现。
///
/// DOT 效果指示器委托给 DotStatusIndicator 组件处理。
/// Sprite 工具方法委托给 HealthBarSpriteHelper 静态类。
///
/// 使用方式：由 EnemyBase 在 OnEnable 时自动创建。
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    private SpriteRenderer _backgroundRenderer;
    private SpriteRenderer _fillRenderer;
    private Transform _barTransform;
    private Damageable _damageable;

    private float _barWidth = 2.2f;
    private float _barHeight = 0.22f;
    private float _offsetY = 1.0f;

    // DOT 指示器（委托组件）
    private DotStatusIndicator _dotIndicator;

    // 性能优化：距离远的敌人降低更新频率
    private Transform _cameraTransform;
    private float _lastHpPercent = -1f;
    private int _skipFrameCounter = 0;
    private const int FAR_SKIP_FRAMES = 4; // 距离远时每5帧更新一次
    private const float FAR_DISTANCE_SQ = 225f; // 15单位的平方（屏幕外）

    /// <summary>
    /// 初始化血条（由 EnemyBase 调用）
    /// </summary>
    public void Setup(Damageable damageable, float barWidth = 2.2f, float barHeight = 0.22f, float offsetY = 1.0f)
    {
        _damageable = damageable;
        _barWidth = barWidth;
        _barHeight = barHeight;
        _offsetY = offsetY;

        CreateBarVisuals();
        CreateDotIndicators();
        UpdateFill();
    }

    private void CreateBarVisuals()
    {
        // 父容器（跟随敌人位置）
        _barTransform = new GameObject("HealthBar").transform;
        _barTransform.SetParent(transform);
        _barTransform.localPosition = new Vector3(0f, _offsetY, 0f);
        _barTransform.localRotation = Quaternion.identity;

        // 背景（深色）— 居中，固定不动
        var bgObj = new GameObject("HP_BG");
        bgObj.transform.SetParent(_barTransform);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = new Vector3(_barWidth, _barHeight, 1f);
        _backgroundRenderer = bgObj.AddComponent<SpriteRenderer>();
        _backgroundRenderer.sprite = HealthBarSpriteHelper.GetWhiteSprite();
        _backgroundRenderer.color = UIColorTheme.DarkBackground;
        _backgroundRenderer.sortingOrder = 10;

        // 填充条 — 与背景同位置同 pivot，覆盖在背景上方缩放
        var fillObj = new GameObject("HP_Fill");
        fillObj.transform.SetParent(_barTransform);
        fillObj.transform.localPosition = Vector3.zero;
        fillObj.transform.localScale = new Vector3(_barWidth, _barHeight, 1f);
        _fillRenderer = fillObj.AddComponent<SpriteRenderer>();
        _fillRenderer.sprite = HealthBarSpriteHelper.GetWhiteSprite();
        _fillRenderer.color = UIColorTheme.AccentCyan;
        _fillRenderer.sortingOrder = 11;
    }

    /// <summary>
    /// 创建 DOT 效果指示器（委托给 DotStatusIndicator）
    /// </summary>
    private void CreateDotIndicators()
    {
        _dotIndicator = new DotStatusIndicator();
        _dotIndicator.Create(_barTransform, _barHeight);
    }

    private void LateUpdate()
    {
        if (_damageable == null || _barTransform == null) return;

        // 性能优化：满血时完全跳过更新
        float currentHp = _damageable.HpPercent;

        if (currentHp >= 1f)
        {
            // 确保满血时血条隐藏，但仍需更新DOT指示器（雷电/霜冻不造成伤害但需要显示叠层）
            if (_lastHpPercent >= 1f)
            {
                // 满血时跳过血条更新，但DOT指示器仍需刷新
                if (_dotIndicator != null)
                    _dotIndicator.Update(gameObject);
                _barTransform.rotation = Quaternion.identity;
                return;
            }
        }

        // 距离优化：远距离敌人降低更新频率
        if (_cameraTransform == null) _cameraTransform = Camera.main?.transform;
        if (_cameraTransform != null)
        {
            float distSq = (_cameraTransform.position - transform.position).sqrMagnitude;
            if (distSq > FAR_DISTANCE_SQ)
            {
                _skipFrameCounter++;
                if (_skipFrameCounter < FAR_SKIP_FRAMES) 
                {
                    _barTransform.rotation = Quaternion.identity; // 保持水平
                    return;
                }
                _skipFrameCounter = 0;
            }
        }

        // 只在HP变化时更新血条填充和颜色
        if (Mathf.Abs(currentHp - _lastHpPercent) > 0.001f)
        {
            UpdateFill();
            _lastHpPercent = currentHp;
        }

        // 委托 DOT 更新（内部已有频率控制）
        if (_dotIndicator != null)
            _dotIndicator.Update(gameObject);

        // 保持血条水平（不随敌人旋转）
        _barTransform.rotation = Quaternion.identity;
    }

    private void UpdateFill()
    {
        if (_damageable == null || _fillRenderer == null) return;

        float percent = _damageable.HpPercent;
        percent = Mathf.Clamp01(percent);

        // 中心 pivot：填充条始终与背景条对齐居中，scale.x 控制宽度
        var fillScale = _fillRenderer.transform.localScale;
        fillScale.x = _barWidth * percent;
        _fillRenderer.transform.localScale = fillScale;

        // 颜色渐变：荧光青 → 洋红 → 亮粉
        if (percent > 0.6f)
        {
            _fillRenderer.color = Color.Lerp(UIColorTheme.AccentMagenta, UIColorTheme.AccentCyan, (percent - 0.6f) / 0.4f);
        }
        else if (percent > 0.3f)
        {
            _fillRenderer.color = Color.Lerp(UIColorTheme.AccentPink, UIColorTheme.AccentMagenta, (percent - 0.3f) / 0.3f);
        }
        else
        {
            _fillRenderer.color = UIColorTheme.AccentPink;
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
        if (_dotIndicator != null)
            _dotIndicator.Destroy();

        if (_barTransform != null)
        {
            Destroy(_barTransform.gameObject);
        }
    }
}