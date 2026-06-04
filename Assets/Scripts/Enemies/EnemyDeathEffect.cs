using UnityEngine;
using System.Collections;

/// <summary>
/// 敌人死亡特效工具类 — 提供轻量级死亡视觉反馈。
/// 
/// 功能：
/// 1. 快速缩小 + 透明度渐变（0.2秒）
/// 2. 根据敌人颜色生成爆炸粒子效果
/// 3. Boss 死亡：慢动作 + 金色粒子爆发 + 屏幕震动
/// 
/// 使用方式：在 EnemyBase.OnEnemyDeathHandler 中调用 PlayDeathEffect()
/// </summary>
public static class EnemyDeathEffect
{
    // ── 动画参数 ──
    private const float SHRINK_DURATION = 0.2f;
    private const float BOSS_SLOWMO_DURATION = 0.5f;
    private const float BOSS_SLOWMO_SCALE = 0.3f;

    // ── 静态纹理缓存 ──
    private static Texture2D _particleTex;
    private static Texture2D _glowTex;

    /// <summary>
    /// 播放敌人死亡特效（非阻塞，创建临时 GameObject 执行动画）
    /// </summary>
    /// <param name="enemy">死亡的敌人 GameObject</param>
    /// <param name="enemyColor">敌人颜色</param>
    /// <param name="isBoss">是否为 Boss</param>
    public static void PlayDeathEffect(GameObject enemy, Color enemyColor, bool isBoss = false)
    {
        if (enemy == null) return;

        Vector3 pos = enemy.transform.position;
        Vector3 scale = enemy.transform.localScale;

        // 创建爆炸粒子效果（轻量级，使用临时 GameObject）
        SpawnExplosionParticles(pos, enemyColor, isBoss);

        // 创建缩小+淡出动画代理
        SpawnShrinkProxy(pos, scale, enemyColor, isBoss);

        // Boss 特效：慢动作 + 屏幕震动
        if (isBoss)
        {
            PlayBossSlowMotion();
            PlayScreenShake();
        }
    }

    /// <summary>
    /// 播放敌人死亡特效（从 SpriteRenderer 获取颜色）
    /// </summary>
    public static void PlayDeathEffect(GameObject enemy, bool isBoss = false)
    {
        Color color = Color.white;
        var sr = enemy.GetComponent<SpriteRenderer>();
        if (sr != null) color = sr.color;
        PlayDeathEffect(enemy, color, isBoss);
    }

    /// <summary>
    /// 生成爆炸粒子（5-8个小圆形，快速向外飞散并淡出）
    /// </summary>
    private static void SpawnExplosionParticles(Vector3 pos, Color baseColor, bool isBoss)
    {
        int particleCount = isBoss ? 15 : Random.Range(5, 9);
        float baseForce = isBoss ? 5f : 3f;

        for (int i = 0; i < particleCount; i++)
        {
            var particleGo = new GameObject("DeathParticle");
            particleGo.transform.position = pos;
            particleGo.transform.localScale = Vector3.one * (isBoss ? 0.2f : 0.12f);

            var sr = particleGo.AddComponent<SpriteRenderer>();
            if (_particleTex == null)
                _particleTex = CreateCircleTexture(16);
            sr.sprite = Sprite.Create(_particleTex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
            sr.sortingOrder = 100; // 确保在最前面

            // 粒子颜色：基于敌人颜色 + 随机偏移
            Color particleColor = baseColor;
            particleColor.r = Mathf.Clamp01(baseColor.r + Random.Range(-0.2f, 0.2f));
            particleColor.g = Mathf.Clamp01(baseColor.g + Random.Range(-0.2f, 0.2f));
            particleColor.b = Mathf.Clamp01(baseColor.b + Random.Range(-0.2f, 0.2f));
            if (isBoss)
            {
                // Boss 死亡：金色粒子
                particleColor = Color.Lerp(baseColor, new Color(1f, 0.85f, 0.2f), 0.6f);
            }
            sr.color = particleColor;

            // 添加粒子运动组件
            var particle = particleGo.AddComponent<DeathParticle>();
            Vector2 dir = Random.insideUnitCircle.normalized;
            particle.Initialize(dir * baseForce * Random.Range(0.5f, 1.5f), isBoss ? 0.6f : 0.3f);
        }
    }

    /// <summary>
    /// 创建缩小+淡出动画代理（复制敌人外观，执行 0.2 秒动画）
    /// </summary>
    private static void SpawnShrinkProxy(Vector3 pos, Vector3 scale, Color color, bool isBoss)
    {
        var proxyGo = new GameObject("DeathShrinkProxy");
        proxyGo.transform.position = pos;
        proxyGo.transform.localScale = scale;

        var sr = proxyGo.AddComponent<SpriteRenderer>();
        var enemySr = GetNearestSpriteRenderer(pos);
        if (enemySr != null)
        {
            sr.sprite = enemySr.sprite;
            sr.color = enemySr.color;
            sr.sortingOrder = 99;
        }
        else
        {
            sr.color = color;
        }

        var proxy = proxyGo.AddComponent<DeathShrinkProxy>();
        proxy.Initialize(isBoss);
    }

    /// <summary>
    /// 查找死亡位置附近最近的 SpriteRenderer（用于复制 Sprite）
    /// </summary>
    private static SpriteRenderer GetNearestSpriteRenderer(Vector3 pos)
    {
        // 使用 OverlapCircle 查找附近的敌人（半径 2 格内）
        var cols = Physics2D.OverlapCircleAll(pos, 2f);
        foreach (var col in cols)
        {
            var sr = col.GetComponent<SpriteRenderer>();
            if (sr != null && sr.gameObject.activeInHierarchy)
                return sr;
        }
        return null;
    }

    /// <summary>
    /// Boss 慢动作效果（0.5 秒 × 0.3 时间缩放）
    /// </summary>
    private static void PlayBossSlowMotion()
    {
        var go = new GameObject("BossSlowMotion");
        var coroutine = go.AddComponent<BossSlowMotionCoroutine>();
        coroutine.Initialize(BOSS_SLOWMO_DURATION, BOSS_SLOWMO_SCALE);
    }

    /// <summary>
    /// Boss 屏幕震动效果
    /// </summary>
    private static void PlayScreenShake()
    {
        var cam = GameReferences.MainCamera;
        if (cam != null)
        {
            var shake = cam.GetComponent<ScreenShake>();
            if (shake == null)
                shake = cam.gameObject.AddComponent<ScreenShake>();
            shake.Shake(0.3f, 0.15f);
        }
    }

    /// <summary>
    /// 创建圆形纹理（16x16 像素）
    /// </summary>
    private static Texture2D CreateCircleTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float radius = center - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = dist <= radius ? 1f : 0f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }
}

/// <summary>
/// 死亡粒子组件 — 快速向外飞散并淡出销毁
/// </summary>
public class DeathParticle : MonoBehaviour
{
    private Vector2 _velocity;
    private float _lifetime;
    private float _startTime;
    private SpriteRenderer _sr;
    private Vector3 _startScale;

    public void Initialize(Vector2 velocity, float lifetime)
    {
        _velocity = velocity;
        _lifetime = lifetime;
        _startTime = Time.time;
        _sr = GetComponent<SpriteRenderer>();
        _startScale = transform.localScale;
    }

    private void Update()
    {
        float elapsed = Time.time - _startTime;
        float t = elapsed / _lifetime;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // 移动（带减速）
        transform.position += (Vector3)(_velocity * Time.deltaTime * (1f - t * 0.5f));

        // 缩小
        float scale = Mathf.Lerp(1f, 0f, t * t); // 平方衰减
        transform.localScale = _startScale * scale;

        // 淡出
        if (_sr != null)
        {
            Color c = _sr.color;
            c.a = 1f - t;
            _sr.color = c;
        }
    }
}

/// <summary>
/// 缩小代理组件 — 复制敌人外观，执行缩小+淡出动画后自毁
/// </summary>
public class DeathShrinkProxy : MonoBehaviour
{
    private float _duration;
    private float _startTime;
    private SpriteRenderer _sr;
    private Vector3 _startScale;

    public void Initialize(bool isBoss)
    {
        _duration = isBoss ? 0.4f : 0.2f;
        _startTime = Time.time;
        _sr = GetComponent<SpriteRenderer>();
        _startScale = transform.localScale;
    }

    private void Update()
    {
        float elapsed = Time.time - _startTime;
        float t = elapsed / _duration;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // 缩小（缓出曲线）
        float scale = Mathf.Lerp(1f, 0f, t * t);
        transform.localScale = _startScale * scale;

        // 淡出
        if (_sr != null)
        {
            Color c = _sr.color;
            c.a = 1f - t;
            _sr.color = c;
        }
    }
}

/// <summary>
/// Boss 慢动作协程组件 — 临时降低时间缩放
/// </summary>
public class BossSlowMotionCoroutine : MonoBehaviour
{
    private float _duration;
    private float _slowScale;
    private float _originalScale;

    public void Initialize(float duration, float slowScale)
    {
        _duration = duration;
        _slowScale = slowScale;
        _originalScale = Time.timeScale;
        StartCoroutine(SlowMoCoroutine());
    }

    private System.Collections.IEnumerator SlowMoCoroutine()
    {
        Time.timeScale = _slowScale;
        yield return new WaitForSecondsRealtime(_duration);
        Time.timeScale = _originalScale;
        Destroy(gameObject);
    }
}

