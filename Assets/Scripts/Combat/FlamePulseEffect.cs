using UnityEngine;

/// <summary>
/// 火焰脉冲效果 — 燃烧子弹的光晕闪烁
/// </summary>
public class FlamePulseEffect : MonoBehaviour
{
    private SpriteRenderer _sr;
    private float _baseAlpha = 0.3f;
    private float _pulseSpeed = 12f;

    public void Init(SpriteRenderer sr) { _sr = sr; }

    private void Update()
    {
        if (_sr == null) return;
        float pulse = Mathf.Sin(Time.time * _pulseSpeed) * 0.15f;
        var c = _sr.color;
        c.a = _baseAlpha + pulse;
        _sr.color = c;
        transform.localScale = Vector3.one * (1.8f + Mathf.Sin(Time.time * _pulseSpeed * 0.7f) * 0.3f);
    }
}
