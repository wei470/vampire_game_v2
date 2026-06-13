using UnityEngine;

/// <summary>
/// 残影淡出组件
/// </summary>
public class GhostFadeOut : MonoBehaviour
{
    private float _lifetime;
    private float _spawnTime;
    private SpriteRenderer _sr;

    public void Init(float lifetime) { _lifetime = lifetime; _spawnTime = Time.time; _sr = GetComponent<SpriteRenderer>(); }

    private void Update()
    {
        float elapsed = Time.time - _spawnTime;
        if (elapsed >= _lifetime) { Destroy(gameObject); return; }
        float t = 1f - (elapsed / _lifetime);
        if (_sr != null) { var c = _sr.color; c.a = 0.5f * t; _sr.color = c; }
        transform.localScale = Vector3.one * (0.3f * t);
    }
}
