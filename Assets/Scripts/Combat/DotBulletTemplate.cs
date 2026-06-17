using UnityEngine;

/// <summary>
/// DOT 子弹模板构建器 — 统一 GameObject 模板构建逻辑。
/// 替代各子弹中重复的 BuildTemplate() + fallback lambda。
/// </summary>
public static class DotBulletTemplate
{
    public struct CircleColliderParams
    {
        public float radius;
        public static CircleColliderParams Default => new CircleColliderParams { radius = 0.25f };
    }

    public struct BoxColliderParams
    {
        public Vector2 size;
        public static BoxColliderParams Default => new BoxColliderParams { size = new Vector2(0.4f, 0.2f) };
    }

    public static GameObject BuildCircle(
        string name, Sprite sprite, Color color, int sortingOrder,
        float scale, CircleColliderParams colParams,
        System.Action<GameObject> attachTrail = null)
    {
        var go = CreateBase(name, sprite, color, sortingOrder, scale);
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = colParams.radius;
        attachTrail?.Invoke(go);
        return go;
    }

    public static GameObject BuildBox(
        string name, Sprite sprite, Color color, int sortingOrder,
        float scale, BoxColliderParams colParams,
        System.Action<GameObject> attachTrail = null)
    {
        var go = CreateBase(name, sprite, color, sortingOrder, scale);
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = colParams.size;
        attachTrail?.Invoke(go);
        return go;
    }

    private static GameObject CreateBase(
        string name, Sprite sprite, Color color, int sortingOrder, float scale)
    {
        var go = new GameObject(name);
        go.tag = "Untagged";
        PhysicsLayerSetup.SetAsBullet(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        go.transform.localScale = Vector3.one * scale;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        return go;
    }
}
