using UnityEngine;

/// <summary>
/// 敌人特效辅助类 — 提供统一的圆形光环/护盾/范围显示。
/// 所有特效 GameObject 在敌人死亡时自动跟随销毁（挂载为子物体）。
/// </summary>
public static class EnemyEffectHelper
{
    /// <summary>
    /// 创建或更新一个圆形光环子物体（显示在敌人周围）。
    /// 如果 GameObject 已存在则更新颜色和半径，否则创建新的。
    /// </summary>
    /// <param name="parent">敌人 Transform</param>
    /// <param name="existingGo">现有光环球体的引用（可为 null）</param>
    /// <param name="name">子物体名称</param>
    /// <param name="color">光环颜色</param>
    /// <param name="radius">光环半径（世界单位）</param>
    /// <param name="alpha">透明度</param>
    /// <param name="sortingOrder">渲染层级</param>
    /// <returns>光环球体引用（下次更新时传入）</returns>
    public static GameObject UpdateCircleAura(Transform parent, GameObject existingGo,
        string name, Color color, float radius, float alpha = 0.25f, int sortingOrder = 5)
    {
        if (existingGo == null)
        {
            existingGo = new GameObject(name);
            existingGo.transform.SetParent(parent);
            existingGo.transform.localPosition = Vector3.zero;
            existingGo.transform.localScale = Vector3.one * radius * 2f;

            var sr = existingGo.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Circle;
            sr.sortingOrder = sortingOrder;
        }

        var renderer = existingGo.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = new Color(color.r, color.g, color.b, alpha);
        }

        existingGo.transform.localScale = Vector3.one * radius * 2f;
        return existingGo;
    }

    /// <summary>
    /// 创建一次性圆形脉冲特效（短暂显示后自动销毁）
    /// </summary>
    public static void CreatePulseEffect(Vector3 position, Color color, float radius, float duration = 0.5f)
    {
        var go = new GameObject("EnemyPulseEffect");
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.1f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Circle;
        sr.color = new Color(color.r, color.g, color.b, 0.6f);
        sr.sortingOrder = 20;

        // 动画协程需要挂载到 GameObject 上
        var pulse = go.AddComponent<PulseEffect>();
        pulse.Setup(color, radius, duration);
    }

    /// <summary>
    /// 简单脉冲动画组件
    /// </summary>
    private class PulseEffect : MonoBehaviour
    {
        private Color _color;
        private float _targetRadius;
        private float _duration;
        private float _elapsed;

        public void Setup(Color color, float targetRadius, float duration)
        {
            _color = color;
            _targetRadius = targetRadius;
            _duration = duration;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / _duration;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            float scale = Mathf.Lerp(0.1f, _targetRadius * 2f, t);
            transform.localScale = Vector3.one * scale;

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                float alpha = Mathf.Lerp(0.6f, 0f, t);
                sr.color = new Color(_color.r, _color.g, _color.b, alpha);
            }
        }
    }
}