using UnityEngine;

/// <summary>
/// Boss 狂战型分裂弹幕组件 — 子弹飞行 1.5 秒后分裂成 3 发小子弹。
/// </summary>
public class BossSplitBullet : MonoBehaviour
{
    private float _splitSpeed;
    private int _splitDamage;
    private float _splitTime;
    private bool _hasSplit = false;

    public void Init(float speed, int damage)
    {
        _splitSpeed = speed;
        _splitDamage = damage;
        _splitTime = Time.time + 1.5f;
    }

    private void Update()
    {
        if (!_hasSplit && Time.time >= _splitTime)
        {
            Split();
            _hasSplit = true;
        }
    }

    private void Split()
    {
        Vector2 pos = transform.position;
        var rb = GetComponent<Rigidbody2D>();
        Vector2 baseDir = rb != null ? rb.linearVelocity.normalized : Vector2.right;

        for (int i = -1; i <= 1; i++)
        {
            float angle = i * 40f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(
                baseDir.x * Mathf.Cos(angle) - baseDir.y * Mathf.Sin(angle),
                baseDir.x * Mathf.Sin(angle) + baseDir.y * Mathf.Cos(angle)
            ).normalized;

            var bulletGo = new GameObject("SplitChild");
            bulletGo.transform.position = pos;
            bulletGo.tag = "Untagged";

            var sr = bulletGo.AddComponent<SpriteRenderer>();
            var tex = new Texture2D(4, 4);
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                {
                    float dx = x - 1.5f, dy = y - 1.5f;
                    tex.SetPixel(x, y, (dx * dx + dy * dy) <= 2.5f ? Color.white : Color.clear);
                }
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            sr.color = new Color(1f, 0.6f, 0.1f);
            bulletGo.transform.localScale = Vector3.one * 0.25f;

            var childRb = bulletGo.AddComponent<Rigidbody2D>();
            childRb.gravityScale = 0f;
            childRb.linearVelocity = dir * _splitSpeed;

            var col = bulletGo.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.12f;

            bulletGo.AddComponent<EnemyBullet>();

            Object.Destroy(bulletGo, 5f);
        }

        DebugHelper.Log("[BossSplitBullet] Split into 3 child bullets!");
    }
}