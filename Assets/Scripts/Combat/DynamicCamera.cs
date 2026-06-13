using UnityEngine;

/// <summary>
/// 动态相机系统 — Boss聚焦、引爆特写、死亡回放。
/// 挂在主摄像机上。
/// </summary>
public class DynamicCamera : MonoBehaviour
{
    public enum CameraMode { Default, BossFocus, DetonateZoom, DeathReplay }

    private CameraMode _mode = CameraMode.Default;
    private float _modeEndTime;
    private float _originalSize;
    private Transform _focusTarget;
    private Camera _cam;

    private const float DEFAULT_SIZE = 10f;
    private const float BOSS_FOCUS_SIZE = 6f;
    private const float DETONATE_ZOOM_SIZE = 8f;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        _originalSize = _cam != null ? _cam.orthographicSize : DEFAULT_SIZE;
    }

    private void LateUpdate()
    {
        if (_mode == CameraMode.Default) return;

        if (Time.time >= _modeEndTime)
        {
            _mode = CameraMode.Default;
            if (_cam != null) _cam.orthographicSize = _originalSize;
            return;
        }

        switch (_mode)
        {
            case CameraMode.BossFocus:
                if (_focusTarget != null && _cam != null)
                {
                    _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, BOSS_FOCUS_SIZE, Time.deltaTime * 3f);
                }
                break;
            case CameraMode.DetonateZoom:
                if (_cam != null)
                {
                    _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, DETONATE_ZOOM_SIZE, Time.deltaTime * 10f);
                }
                break;
        }
    }

    public void FocusBoss(Transform boss, float duration = 2f)
    {
        _mode = CameraMode.BossFocus;
        _focusTarget = boss;
        _modeEndTime = Time.time + duration;
    }

    public void DetonateZoom(float duration = 0.3f)
    {
        _mode = CameraMode.DetonateZoom;
        _modeEndTime = Time.time + duration;
    }
}
