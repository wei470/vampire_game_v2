using UnityEngine;

/// <summary>
/// 敌人血条组件 — 在敌人头顶显示 HP 条。
/// 使用 EnemyUIRoot 统一管理血条和叠层 UI 的缩放。
///
/// 层级结构：
///   Enemy → EnemyUIRoot (统一缩放) → HealthBar (血条)
///                                   → DotStatusBar (叠层显示)
///
/// 使用方式：由 EnemyBase 在 OnEnable 时自动创建。
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    private SpriteRenderer _backgroundRenderer;
    private SpriteRenderer _fillRenderer;
    private Transform _uiRoot;
    private Transform _barTransform;
    private Damageable _damageable;

    private float _barWidth = 3.0f;
    private float _barHeight = 0.35f;
    private float _offsetY = 1.0f;

    private DotStatusBar _dotStatusBar;

    // 统一缩放系数（可通过配置调整）
    private const float UI_SCALE = 1.0f;

    // 性能优化：距离远的敌人降低更新频率
    private Transform _cameraTransform;
    private float _lastHpPercent = -1f;
    private int _skipFrameCounter = 0;
    private const int FAR_SKIP_FRAMES = 4;
    private const float FAR_DISTANCE_SQ = 225f;

    public void Setup(Damageable damageable, float barWidth = 3.0f, float barHeight = 0.35f, float offsetY = 1.0f)
    {
        _damageable = damageable;
        _barWidth = barWidth;
        _barHeight = barHeight;
        _offsetY = offsetY;

        CreateUIRoot();
        CreateBarVisuals();
        CreateDotStatusBar();
        UpdateFill();
    }

    private void CreateUIRoot()
    {
        _uiRoot = new GameObject("EnemyUIRoot").transform;
        _uiRoot.SetParent(transform);
        _uiRoot.localPosition = new Vector3(0f, _offsetY, 0f);
        _uiRoot.localRotation = Quaternion.identity;
        _uiRoot.localScale = Vector3.one * UI_SCALE;
    }

    private void CreateBarVisuals()
    {
        _barTransform = new GameObject("HealthBar").transform;
        _barTransform.SetParent(_uiRoot);
        _barTransform.localPosition = Vector3.zero;
        _barTransform.localRotation = Quaternion.identity;

        var bgObj = new GameObject("HP_BG");
        bgObj.transform.SetParent(_barTransform);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = new Vector3(_barWidth, _barHeight, 1f);
        _backgroundRenderer = bgObj.AddComponent<SpriteRenderer>();
        _backgroundRenderer.sprite = HealthBarSpriteHelper.GetWhiteSprite();
        _backgroundRenderer.color = UIColorTheme.DarkBackground;
        _backgroundRenderer.sortingOrder = 10;

        var fillObj = new GameObject("HP_Fill");
        fillObj.transform.SetParent(_barTransform);
        fillObj.transform.localPosition = Vector3.zero;
        fillObj.transform.localScale = new Vector3(_barWidth, _barHeight, 1f);
        _fillRenderer = fillObj.AddComponent<SpriteRenderer>();
        _fillRenderer.sprite = HealthBarSpriteHelper.GetWhiteSprite();
        _fillRenderer.color = UIColorTheme.AccentCyan;
        _fillRenderer.sortingOrder = 11;
    }

    private void CreateDotStatusBar()
    {
        _dotStatusBar = new DotStatusBar();
        _dotStatusBar.Create(_uiRoot, _barHeight);
    }

    private void LateUpdate()
    {
        if (_damageable == null || _uiRoot == null) return;

        float currentHp = _damageable.HpPercent;

        if (currentHp >= 1f)
        {
            if (_lastHpPercent >= 1f)
            {
                if (_dotStatusBar != null)
                    _dotStatusBar.Update(gameObject);
                _uiRoot.rotation = Quaternion.identity;
                return;
            }
        }

        if (_cameraTransform == null) _cameraTransform = Camera.main?.transform;
        if (_cameraTransform != null)
        {
            float distSq = (_cameraTransform.position - transform.position).sqrMagnitude;
            if (distSq > FAR_DISTANCE_SQ)
            {
                _skipFrameCounter++;
                if (_skipFrameCounter < FAR_SKIP_FRAMES)
                {
                    _uiRoot.rotation = Quaternion.identity;
                    return;
                }
                _skipFrameCounter = 0;
            }
        }

        if (Mathf.Abs(currentHp - _lastHpPercent) > 0.001f)
        {
            UpdateFill();
            _lastHpPercent = currentHp;
        }

        if (_dotStatusBar != null)
            _dotStatusBar.Update(gameObject);

        _uiRoot.rotation = Quaternion.identity;
    }

    private void UpdateFill()
    {
        if (_damageable == null || _fillRenderer == null) return;

        float percent = _damageable.HpPercent;
        percent = Mathf.Clamp01(percent);

        var fillScale = _fillRenderer.transform.localScale;
        fillScale.x = _barWidth * percent;
        _fillRenderer.transform.localScale = fillScale;

        if (percent > 0.6f)
            _fillRenderer.color = Color.Lerp(UIColorTheme.AccentMagenta, UIColorTheme.AccentCyan, (percent - 0.6f) / 0.4f);
        else if (percent > 0.3f)
            _fillRenderer.color = Color.Lerp(UIColorTheme.AccentPink, UIColorTheme.AccentMagenta, (percent - 0.3f) / 0.3f);
        else
            _fillRenderer.color = UIColorTheme.AccentPink;

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
        if (_dotStatusBar != null)
            _dotStatusBar.Destroy();

        if (_uiRoot != null)
            Destroy(_uiRoot.gameObject);
    }
}