using UnityEngine;

/// <summary>
/// 受击闪白效果 — 挂在敌人上，受伤时 SpriteRenderer 短暂变白再恢复。
/// </summary>
public class HitFlashEffect : MonoBehaviour
{
    private SpriteRenderer _sr;
    private Color _originalColor;
    private float _flashEndTime;
    private bool _flashing;

    private const float FLASH_DURATION = 0.1f;
    private static readonly Color FLASH_COLOR = new Color(1f, 1f, 1f, 1f);

    public void TriggerFlash()
    {
        if (_sr == null)
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) return;
        }
        _originalColor = _sr.color;
        _sr.color = FLASH_COLOR;
        _flashEndTime = Time.time + FLASH_DURATION;
        _flashing = true;
    }

    private void Update()
    {
        if (!_flashing) return;
        if (Time.time >= _flashEndTime)
        {
            if (_sr != null) _sr.color = _originalColor;
            _flashing = false;
        }
    }

    private void OnDisable()
    {
        _flashing = false;
    }
}
