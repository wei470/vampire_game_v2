using UnityEngine;

/// <summary>
/// 浮动文字 — 向上飘动并淡出，自动销毁。
/// 合并自 ReactionTextTicker / PoisonBurstTextTicker / BurnSpreadTextTicker。
/// </summary>
public class FloatingText : MonoBehaviour
{
    public float Lifetime = 1.0f;
    private float _spawnTime;
    private TextMesh _textMesh;

    private void Awake()
    {
        _spawnTime = Time.time;
        _textMesh = GetComponent<TextMesh>();
        Destroy(gameObject, Lifetime + 1f);
    }

    private void OnEnable() { _spawnTime = Time.time; }

    private void Update()
    {
        float elapsed = Time.time - _spawnTime;
        if (elapsed >= Lifetime) { Destroy(gameObject); return; }

        transform.position += Vector3.up * Time.deltaTime * 1.5f;

        if (_textMesh != null)
        {
            Color c = _textMesh.color;
            c.a = Mathf.Clamp01(1f - (elapsed / Lifetime));
            _textMesh.color = c;
        }
    }

    public static FloatingText Create(Vector3 position, string text, Color color, float lifetime = 1.0f)
    {
        var go = new GameObject("FloatingText");
        go.transform.position = position;
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.color = color;
        tm.characterSize = 0.3f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        var ticker = go.AddComponent<FloatingText>();
        ticker.Lifetime = lifetime;
        return ticker;
    }
}
