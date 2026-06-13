using UnityEngine;

/// <summary>
/// 旋转效果 — 中毒药瓶飞行时旋转
/// </summary>
public class SpinEffect : MonoBehaviour
{
    private float _spinSpeed = 360f;
    public void Init(float speed) { _spinSpeed = speed; }
    private void Update() { transform.Rotate(0, 0, _spinSpeed * Time.deltaTime); }
}
