using UnityEngine;

/// <summary>
/// 屏幕抖动效果 — 玩家受击时触发。
/// 
/// 功能：
/// - 受击时屏幕随机偏移抖动
/// - 支持自定义强度和持续时间
/// - 平滑衰减
/// 
/// 使用方式：挂载到主摄像机上
/// </summary>
public class ScreenShake : MonoBehaviour
{
    [Header("抖动设置")]
    [SerializeField] private float _defaultIntensity = 0.3f;
    [SerializeField] private float _defaultDuration = 0.15f;

    private float _shakeIntensity = 0f;
    private float _shakeDuration = 0f;
    private float _shakeTimer = 0f;
    private Vector3 _originalPosition;

    private void OnEnable()
    {
        _originalPosition = transform.localPosition;
        EventManager.OnPlayerDamaged += OnPlayerDamaged;
    }

    private void OnDisable()
    {
        EventManager.OnPlayerDamaged -= OnPlayerDamaged;
        transform.localPosition = _originalPosition;
    }

    private void OnPlayerDamaged(int currentHp, int maxHp)
    {
        Shake(_defaultIntensity, _defaultDuration);
    }

    private void Update()
    {
        if (_shakeTimer > 0f)
        {
            _shakeTimer -= Time.unscaledDeltaTime; // 使用 unscaledTime 以支持暂停时抖动
            float t = _shakeTimer / _shakeDuration;
            float currentIntensity = _shakeIntensity * t;

            transform.localPosition = _originalPosition + (Vector3)Random.insideUnitCircle * currentIntensity;
        }
        else
        {
            transform.localPosition = _originalPosition;
        }
    }

    /// <summary>
    /// 触发屏幕抖动
    /// </summary>
    public void Shake(float intensity = 0f, float duration = 0f)
    {
        _shakeIntensity = intensity > 0f ? intensity : _defaultIntensity;
        _shakeDuration = duration > 0f ? duration : _defaultDuration;
        _shakeTimer = _shakeDuration;
    }
}