using UnityEngine;

/// <summary>
/// #18 DOT 粒子视觉效果组件 — 挂载到敌人身上，根据活跃的 DOT 类型显示粒子特效。
/// 流血：红色粒子从身上飘出
/// 中毒：绿色气泡冒泡效果
/// 燃烧：火焰粒子环绕
/// 霜冻：冰晶粒子闪烁
/// 
/// 使用 Unity ParticleSystem 运行时创建，无需 Prefab。
/// </summary>
public class DotParticleVFX : MonoBehaviour
{
    // 粒子系统引用（每种 DOT 类型一个）
    private ParticleSystem _bleedPS;
    private ParticleSystem _poisonPS;
    private ParticleSystem _burnPS;
    private ParticleSystem _frostPS;

    // 缓存的 Sprite（所有敌人共享）
    private static Sprite _particleSprite;

    /// <summary>
    /// 获取或创建 1x1 白色粒子 Sprite
    /// </summary>
    private static Sprite GetParticleSprite()
    {
        if (_particleSprite != null) return _particleSprite;

        int size = 8;
        var tex = new Texture2D(size, size);
        float center = size / 2f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                float alpha = dist <= 1f ? 1f - dist * dist : 0f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        tex.Apply();
        _particleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _particleSprite;
    }

    /// <summary>
    /// 更新活跃的 DOT 粒子效果
    /// </summary>
    public void UpdateEffects(bool hasBleed, bool hasPoison, bool hasBurn, bool hasFrost)
    {
        // 流血：红色粒子从身上飘出
        UpdateBleedVFX(hasBleed);

        // 中毒：绿色气泡冒泡
        UpdatePoisonVFX(hasPoison);

        // 燃烧：火焰粒子环绕
        UpdateBurnVFX(hasBurn);

        // 霜冻：冰晶粒子
        UpdateFrostVFX(hasFrost);
    }

    private void UpdateBleedVFX(bool active)
    {
        if (active)
        {
            if (_bleedPS == null)
            {
                _bleedPS = CreateParticleSystem("BleedVFX",
                    new Color(0.9f, 0.1f, 0.1f, 0.8f), // 红色
                    0.08f,  // startSize
                    3f,     // emission rate
                    0.8f,   // lifetime
                    new Vector2(0.5f, 0.5f), // 圆形发射范围
                    Vector2.up * 0.5f,       // 向上飘
                    0.3f);  // 速度
            }
            if (!_bleedPS.isPlaying) _bleedPS.Play();
        }
        else if (_bleedPS != null && _bleedPS.isPlaying)
        {
            _bleedPS.Stop();
        }
    }

    private void UpdatePoisonVFX(bool active)
    {
        if (active)
        {
            if (_poisonPS == null)
            {
                _poisonPS = CreateParticleSystem("PoisonVFX",
                    new Color(0.1f, 0.9f, 0.2f, 0.7f), // 绿色
                    0.1f,   // startSize
                    4f,     // emission rate
                    1.2f,   // lifetime
                    new Vector2(0.8f, 0.3f), // 底部区域
                    Vector2.up * 1f,         // 向上冒泡
                    0.8f);  // 速度（气泡快速上升）
            }
            if (!_poisonPS.isPlaying) _poisonPS.Play();
        }
        else if (_poisonPS != null && _poisonPS.isPlaying)
        {
            _poisonPS.Stop();
        }
    }

    private void UpdateBurnVFX(bool active)
    {
        if (active)
        {
            if (_burnPS == null)
            {
                _burnPS = CreateParticleSystem("BurnVFX",
                    new Color(1f, 0.5f, 0f, 0.9f), // 橙色
                    0.12f,  // startSize
                    8f,     // emission rate（火焰密集）
                    0.5f,   // lifetime（快速消散）
                    new Vector2(0.4f, 0.4f), // 环绕
                    Vector2.up * 1.5f,       // 向上飘
                    1.2f);  // 速度（火焰快速）

                // 火焰需要额外设置：发射形状为半球形
                var shape = _burnPS.shape;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 0.3f;
            }
            if (!_burnPS.isPlaying) _burnPS.Play();
        }
        else if (_burnPS != null && _burnPS.isPlaying)
        {
            _burnPS.Stop();
        }
    }

    private void UpdateFrostVFX(bool active)
    {
        if (active)
        {
            if (_frostPS == null)
            {
                _frostPS = CreateParticleSystem("FrostVFX",
                    new Color(0.6f, 0.8f, 1f, 0.6f), // 冰蓝色
                    0.06f,  // startSize（小冰晶）
                    5f,     // emission rate
                    1.5f,   // lifetime
                    new Vector2(0.6f, 0.6f), // 周身
                    Vector2.up * 0.2f,       // 缓慢漂浮
                    0.15f); // 速度（冰晶缓慢）

                // 冰晶需要更随机的运动
                var noise = _frostPS.noise;
                noise.enabled = true;
                noise.strength = 0.3f;
                noise.frequency = 2f;
            }
            if (!_frostPS.isPlaying) _frostPS.Play();
        }
        else if (_frostPS != null && _frostPS.isPlaying)
        {
            _frostPS.Stop();
        }
    }

    /// <summary>
    /// 创建运行时粒子系统（轻量级，无需 Prefab）
    /// </summary>
    private ParticleSystem CreateParticleSystem(string name, Color color,
        float startSize, float emissionRate, float lifetime,
        Vector2 spread, Vector2 velocity, float speed)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = lifetime;
        main.startSize = startSize;
        main.startSpeed = speed;
        main.startColor = color;
        main.maxParticles = 20; // 限制粒子数，避免性能问题
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = emissionRate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale = new Vector3(spread.x, spread.y, 1f);

        var velocityModule = ps.velocityOverLifetime;
        velocityModule.enabled = true;
        velocityModule.x = velocity.x;
        velocityModule.y = velocity.y;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetParticleMaterial();
        renderer.sortingOrder = 15;

        ps.Play();
        return ps;
    }

    private static Material _particleMat;
    private static Material GetParticleMaterial()
    {
        if (_particleMat == null)
        {
            var shader = Shader.Find("Sprites/Default");
            _particleMat = new Material(shader);
        }
        return _particleMat;
    }

    private void OnDisable()
    {
        // 停止所有粒子
        if (_bleedPS != null) _bleedPS.Stop();
        if (_poisonPS != null) _poisonPS.Stop();
        if (_burnPS != null) _burnPS.Stop();
        if (_frostPS != null) _frostPS.Stop();
    }

    private void OnDestroy()
    {
        // 清理粒子系统 GameObject
        if (_bleedPS != null) Destroy(_bleedPS.gameObject);
        if (_poisonPS != null) Destroy(_poisonPS.gameObject);
        if (_burnPS != null) Destroy(_burnPS.gameObject);
        if (_frostPS != null) Destroy(_frostPS.gameObject);
    }
}