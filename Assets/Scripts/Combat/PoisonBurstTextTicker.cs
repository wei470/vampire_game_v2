using UnityEngine;

/// <summary>
/// 毒爆文字 — 向上飘动并淡出，1秒后自动销毁
/// </summary>
public class PoisonBurstTextTicker : MonoBehaviour
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
        if (elapsed >= Lifetime)
        {
            Destroy(gameObject);
            return;
        }
        transform.position += Vector3.up * Time.deltaTime * 1.5f;
        if (_textMesh != null)
        {
            Color c = _textMesh.color;
            c.a = Mathf.Clamp01(1f - (elapsed / Lifetime));
            _textMesh.color = c;
        }
    }

    private void OnDisable()
    {
        // 不在 OnDisable 中 Destroy(gameObject) —— FullReset 会先 disable 所有 MB
        // 再由 CleanupLingeringCombatObjects 统一销毁，避免级联销毁导致异常
    }
}
