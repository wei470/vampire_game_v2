using UnityEngine;

/// <summary>
/// 激光渐隐效果
/// </summary>
public class LaserFadeOut : MonoBehaviour
{
    private float _duration;
    private float _startTime;
    private SpriteRenderer _sr;

    public void Init(float duration)
    {
        _duration = duration;
        _startTime = Time.time;
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (_sr == null) return;
        float elapsed = Time.time - _startTime;
        float alpha = 1f - (elapsed / _duration);
        if (alpha <= 0f) { Destroy(gameObject); return; }
        _sr.color = new Color(1f, 1f, 1f, alpha * 0.7f);
    }
}
