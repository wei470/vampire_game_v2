using UnityEngine;

/// <summary>
/// 霜冻残影生成器 — 使用 VFXPool 减少 GC 压力
/// </summary>
public class FrostGhostSpawner : MonoBehaviour
{
    private float _spawnInterval = 0.08f;
    private float _ghostLifetime = 0.25f;
    private float _lastSpawn;
    private static Sprite _ghostSprite;

    private void Update()
    {
        if (Time.time - _lastSpawn < _spawnInterval) return;
        _lastSpawn = Time.time;
        var ghost = VFXPool.Get("FrostGhost");
        ghost.transform.position = transform.position;
        ghost.transform.localScale = Vector3.one * 0.3f;
        var sr = ghost.GetComponent<SpriteRenderer>();
        if (sr == null) sr = ghost.AddComponent<SpriteRenderer>();
        if (_ghostSprite == null) _ghostSprite = DotSpriteCache.Get();
        sr.sprite = _ghostSprite;
        sr.color = new Color(0.5f, 0.8f, 1f, 0.5f);
        sr.sortingOrder = 13;
        var fade = ghost.GetComponent<GhostFadeOut>();
        if (fade == null) fade = ghost.AddComponent<GhostFadeOut>();
        fade.Init(_ghostLifetime);
    }
}
