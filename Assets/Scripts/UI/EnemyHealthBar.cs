using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 敌人血条组件 — 在敌人头顶显示 HP 条 + DOT 效果指示器。
/// 使用两个 SpriteRenderer（背景 + 填充）作为子对象，
/// 不依赖 Canvas，纯 2D Sprite 实现。
///
/// DOT 效果指示器在血条上方显示所有活跃的持续伤害类型：
/// - 🟢 中毒（显示层数条，持续到敌人死亡）
/// - 🔴 流血
/// - 🟠 燃烧
/// - 🔵 霜冻
/// - 其他所有 StatusEffectManager 中的 DOT 类型
///
/// 使用方式：由 EnemyBase 在 OnEnable 时自动创建。
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    private SpriteRenderer _backgroundRenderer;
    private SpriteRenderer _fillRenderer;
    private Transform _barTransform;
    private Damageable _damageable;

    private float _barWidth = 2.2f;
    private float _barHeight = 0.22f;
    private float _offsetY = 1.0f;

    private static Sprite _whiteSprite;

    // DOT 效果指示器容器（与血条同等大小）
    private Transform _dotContainer;
    private const float DOT_INDICATOR_HEIGHT = 0.22f;  // 与血条同高
    private const float DOT_INDICATOR_WIDTH = 2.2f;    // 与血条同宽
    private const float DOT_GAP = 0.04f;
    private const float DOT_OFFSET_ABOVE_BAR = 0.04f;

    // 独立 DOT 组件指示器
    private SpriteRenderer _poisonStackIndicator;
    private SpriteRenderer _burnStackIndicator;
    private SpriteRenderer _bleedComponentIndicator;
    private SpriteRenderer _frostComponentIndicator;
    // #21 中毒层数文字显示
    private TextMesh _poisonStackText;

    // #6 DOT 图标系统（小几何形状 + 层数文字）
    private Transform _dotIconContainer;
    private const float DOT_ICON_SIZE = 0.25f;
    private const float DOT_ICON_GAP = 0.35f;
    private const float DOT_ICON_TEXT_OFFSET = 0.18f;

    // 图标 SpriteRenderers（小几何形状）
    private SpriteRenderer _bleedIcon;
    private SpriteRenderer _poisonIcon;
    private SpriteRenderer _burnIcon;
    private SpriteRenderer _frostIcon;

    // 层数文字
    private TextMesh _bleedCountText;
    private TextMesh _burnCountText;
    private TextMesh _frostCountText;
    // _poisonStackText 已存在

    // #6 图标几何形状缓存
    private static Sprite _triangleSprite;  // 流血
    private static Sprite _diamondSprite;   // 中毒
    private static Sprite _pentagonSprite;  // 燃烧
    private static Sprite _hexagonSprite;   // 霜冻

    // StatusEffectManager 动态指示器缓存
    private Dictionary<StatusEffectType, SpriteRenderer> _semIndicators
        = new Dictionary<StatusEffectType, SpriteRenderer>();

    /// <summary>
    /// 初始化血条（由 EnemyBase 调用）
    /// </summary>
    public void Setup(Damageable damageable, float barWidth = 2.2f, float barHeight = 0.22f, float offsetY = 1.0f)
    {
        _damageable = damageable;
        _barWidth = barWidth;
        _barHeight = barHeight;
        _offsetY = offsetY;

        CreateBarVisuals();
        CreateDotIndicators();
        UpdateFill();
    }

    private static Sprite _leftPivotWhiteSprite;

    private static Sprite GetWhiteSprite()
    {
        if (_whiteSprite == null)
        {
            int size = 4;
            var tex = new Texture2D(size, size);
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 10f);
        }
        return _whiteSprite;
    }

    /// <summary>
    /// 左对齐 Sprite（pivot 在左边缘），用于填充条 scale 缩放时不偏移位置。
    /// </summary>
    private static Sprite GetLeftPivotWhiteSprite()
    {
        if (_leftPivotWhiteSprite == null)
        {
            int size = 4;
            var tex = new Texture2D(size, size);
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            _leftPivotWhiteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0f, 0.5f), 10f);
        }
        return _leftPivotWhiteSprite;
    }

    private void CreateBarVisuals()
    {
        // 父容器（跟随敌人位置）
        _barTransform = new GameObject("HealthBar").transform;
        _barTransform.SetParent(transform);
        _barTransform.localPosition = new Vector3(0f, _offsetY, 0f);
        _barTransform.localRotation = Quaternion.identity;

        // 背景（深色）— 居中，固定不动
        var bgObj = new GameObject("HP_BG");
        bgObj.transform.SetParent(_barTransform);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = new Vector3(_barWidth, _barHeight, 1f);
        _backgroundRenderer = bgObj.AddComponent<SpriteRenderer>();
        _backgroundRenderer.sprite = GetWhiteSprite();
        _backgroundRenderer.color = UIColorTheme.DarkBackground;
        _backgroundRenderer.sortingOrder = 10;

        // 填充条 — 与背景同位置同 pivot，覆盖在背景上方缩放
        var fillObj = new GameObject("HP_Fill");
        fillObj.transform.SetParent(_barTransform);
        fillObj.transform.localPosition = Vector3.zero;
        fillObj.transform.localScale = new Vector3(_barWidth, _barHeight, 1f);
        _fillRenderer = fillObj.AddComponent<SpriteRenderer>();
        _fillRenderer.sprite = GetWhiteSprite();
        _fillRenderer.color = UIColorTheme.AccentCyan;
        _fillRenderer.sortingOrder = 11;
    }

    /// <summary>
    /// 创建 DOT 效果指示器（在血条上方）
    /// </summary>
    private void CreateDotIndicators()
    {
        _dotContainer = new GameObject("DOT_Indicators").transform;
        _dotContainer.SetParent(_barTransform);
        _dotContainer.localPosition = new Vector3(0f, _barHeight + DOT_OFFSET_ABOVE_BAR, 0f);
        _dotContainer.localRotation = Quaternion.identity;

        // 独立 DOT 组件指示器
        _poisonStackIndicator = CreateSingleIndicator("PoisonStack", new Color(0.1f, 0.9f, 0.2f));
        _burnStackIndicator = CreateSingleIndicator("BurnStack", new Color(1f, 0.5f, 0f));
        _bleedComponentIndicator = CreateSingleIndicator("BleedComp", new Color(0.9f, 0.1f, 0.1f));
        _frostComponentIndicator = CreateSingleIndicator("FrostComp", new Color(0.3f, 0.6f, 1f));

        // #21 创建中毒层数文字
        var poisonTextObj = new GameObject("PoisonCount");
        poisonTextObj.transform.SetParent(_dotContainer);
        poisonTextObj.transform.localPosition = new Vector3(0f, DOT_INDICATOR_HEIGHT + 0.1f, 0f);
        _poisonStackText = poisonTextObj.AddComponent<TextMesh>();
        _poisonStackText.text = "";
        _poisonStackText.fontSize = 40;
        _poisonStackText.fontStyle = FontStyle.Bold;
        _poisonStackText.characterSize = 0.08f;
        _poisonStackText.alignment = TextAlignment.Center;
        _poisonStackText.anchor = TextAnchor.MiddleCenter;
        _poisonStackText.color = new Color(0.2f, 1f, 0.3f);
        _poisonStackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var mr = poisonTextObj.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 13;
        _poisonStackText.gameObject.SetActive(false);

        // #6 创建 DOT 图标系统
        CreateDotIcons();
    }

    private SpriteRenderer CreateSingleIndicator(string name, Color color)
    {
        var obj = new GameObject("DOT_" + name);
        obj.transform.SetParent(_dotContainer);
        obj.transform.localPosition = Vector3.zero;
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetWhiteSprite();
        sr.color = color;
        sr.sortingOrder = 12;
        sr.enabled = false; // 默认隐藏
        return sr;
    }

    /// <summary>
    /// 获取或创建 StatusEffectManager 中对应类型的指示器
    /// </summary>
    private SpriteRenderer GetOrCreateSemIndicator(StatusEffectType type, Color color)
    {
        if (_semIndicators.TryGetValue(type, out var existing))
            return existing;

        var sr = CreateSingleIndicator(type.ToString(), color);
        _semIndicators[type] = sr;
        return sr;
    }

    // ═══ #6 DOT 图标系统实现 ═══

    /// <summary>
    /// #6 创建 DOT 图标容器和图标（在 CreateDotIndicators 末尾调用）
    /// </summary>
    private void CreateDotIcons()
    {
        _dotIconContainer = new GameObject("DOT_Icons").transform;
        _dotIconContainer.SetParent(_barTransform);
        // 图标在指示器上方
        _dotIconContainer.localPosition = new Vector3(0f, _barHeight + DOT_OFFSET_ABOVE_BAR + DOT_INDICATOR_HEIGHT + 0.15f, 0f);
        _dotIconContainer.localRotation = Quaternion.identity;

        // 创建 4 种 DOT 图标（默认隐藏）
        _bleedIcon = CreateShapeIcon("BleedIcon", GetTriangleSprite(), new Color(0.9f, 0.1f, 0.1f));
        _poisonIcon = CreateShapeIcon("PoisonIcon", GetDiamondSprite(), new Color(0.1f, 0.9f, 0.2f));
        _burnIcon = CreateShapeIcon("BurnIcon", GetPentagonSprite(), new Color(1f, 0.5f, 0f));
        _frostIcon = CreateShapeIcon("FrostIcon", GetHexagonSprite(), new Color(0.3f, 0.6f, 1f));

        // 创建层数文字（图标旁）
        _bleedCountText = CreateIconCountText("BleedCount", new Color(1f, 0.3f, 0.3f));
        _burnCountText = CreateIconCountText("BurnCount", new Color(1f, 0.7f, 0.2f));
        _frostCountText = CreateIconCountText("FrostCount", new Color(0.5f, 0.8f, 1f));
        // _poisonStackText 已有，调整位置到图标旁
    }

    private SpriteRenderer CreateShapeIcon(string name, Sprite shape, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(_dotIconContainer);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localScale = Vector3.one * DOT_ICON_SIZE;
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = shape;
        sr.color = color;
        sr.sortingOrder = 14;
        sr.enabled = false;
        return sr;
    }

    private TextMesh CreateIconCountText(string name, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(_dotIconContainer);
        obj.transform.localPosition = new Vector3(DOT_ICON_TEXT_OFFSET, 0f, 0f);
        var tm = obj.AddComponent<TextMesh>();
        tm.text = "";
        tm.fontSize = 40;
        tm.fontStyle = FontStyle.Bold;
        tm.characterSize = 0.06f;
        tm.alignment = TextAlignment.Left;
        tm.anchor = TextAnchor.MiddleLeft;
        tm.color = color;
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var mr = obj.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 15;
        obj.SetActive(false);
        return tm;
    }

    // #6 几何形状 Sprite 生成（静态缓存）
    private static Sprite CreatePolygonSprite(int sides, float radius, string name)
    {
        int pxSize = 64;
        var tex = new Texture2D(pxSize, pxSize, TextureFormat.RGBA32, false);
        for (int x = 0; x < pxSize; x++)
            for (int y = 0; y < pxSize; y++)
                tex.SetPixel(x, y, Color.clear);

        float cx = pxSize / 2f;
        float cy = pxSize / 2f;
        float r = pxSize * radius;

        // 填充多边形
        for (int x = 0; x < pxSize; x++)
        {
            for (int y = 0; y < pxSize; y++)
            {
                float dx = x - cx;
                float dy = y - cy;
                // 角度检测：判断点是否在多边形内
                if (IsInsidePolygon(dx, dy, r, sides))
                    tex.SetPixel(x, y, Color.white);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, pxSize, pxSize), new Vector2(0.5f, 0.5f), pxSize);
    }

    private static bool IsInsidePolygon(float px, float py, float r, int sides)
    {
        float dist = Mathf.Sqrt(px * px + py * py);
        if (dist > r) return false;
        if (dist < 0.01f) return true;
        float angle = Mathf.Atan2(py, px);
        if (angle < 0) angle += 2f * Mathf.PI;
        float sectorAngle = 2f * Mathf.PI / sides;
        float halfSector = sectorAngle / 2f;
        float relAngle = angle % sectorAngle;
        float edgeDist = r * Mathf.Cos(halfSector) / Mathf.Cos(relAngle - halfSector);
        return dist <= edgeDist;
    }

    private static Sprite GetTriangleSprite()
    {
        if (_triangleSprite == null)
            _triangleSprite = CreatePolygonSprite(3, 0.45f, "Triangle");
        return _triangleSprite;
    }

    private static Sprite GetDiamondSprite()
    {
        if (_diamondSprite == null)
            _diamondSprite = CreatePolygonSprite(4, 0.45f, "Diamond");
        return _diamondSprite;
    }

    private static Sprite GetPentagonSprite()
    {
        if (_pentagonSprite == null)
            _pentagonSprite = CreatePolygonSprite(5, 0.45f, "Pentagon");
        return _pentagonSprite;
    }

    private static Sprite GetHexagonSprite()
    {
        if (_hexagonSprite == null)
            _hexagonSprite = CreatePolygonSprite(6, 0.45f, "Hexagon");
        return _hexagonSprite;
    }

    /// <summary>
    /// #6 更新 DOT 图标显示（在 UpdateDotIndicators 末尾调用）
    /// </summary>
    private void UpdateDotIcons(bool hasBleed, bool hasPoison, int poisonStacks,
        bool hasBurn, int burnStacks, bool hasFrost, int frostStacks)
    {
        // 流血图标
        if (_bleedIcon != null)
        {
            _bleedIcon.enabled = hasBleed;
            if (hasBleed) _bleedIcon.transform.localPosition = new Vector3(-DOT_ICON_GAP * 1.5f, 0f, 0f);
        }
        if (_bleedCountText != null)
        {
            _bleedCountText.gameObject.SetActive(hasBleed);
            if (hasBleed)
            {
                _bleedCountText.transform.localPosition = new Vector3(-DOT_ICON_GAP * 1.5f + DOT_ICON_TEXT_OFFSET, 0f, 0f);
                _bleedCountText.text = "";
            }
        }

        // 中毒图标
        if (_poisonIcon != null)
        {
            _poisonIcon.enabled = hasPoison;
            if (hasPoison) _poisonIcon.transform.localPosition = new Vector3(-DOT_ICON_GAP * 0.5f, 0f, 0f);
        }
        // 移动已有中毒层数文字到图标旁
        if (_poisonStackText != null && hasPoison)
        {
            _poisonStackText.transform.localPosition = new Vector3(-DOT_ICON_GAP * 0.5f + DOT_ICON_TEXT_OFFSET, 0f, 0f);
            _poisonStackText.characterSize = 0.06f;
        }

        // 燃烧图标
        if (_burnIcon != null)
        {
            _burnIcon.enabled = hasBurn;
            if (hasBurn)
            {
                _burnIcon.transform.localPosition = new Vector3(DOT_ICON_GAP * 0.5f, 0f, 0f);
                // 燃烧脉冲
                float pulse = Mathf.Sin(Time.time * 8f) * 0.15f;
                _burnIcon.color = new Color(1f, Mathf.Clamp01(0.5f + pulse), 0f);
            }
        }
        if (_burnCountText != null)
        {
            _burnCountText.gameObject.SetActive(hasBurn);
            if (hasBurn)
            {
                _burnCountText.transform.localPosition = new Vector3(DOT_ICON_GAP * 0.5f + DOT_ICON_TEXT_OFFSET, 0f, 0f);
                _burnCountText.text = burnStacks > 0 ? $"x{burnStacks}" : "";
            }
        }

        // 霜冻图标
        if (_frostIcon != null)
        {
            _frostIcon.enabled = hasFrost;
            if (hasFrost) _frostIcon.transform.localPosition = new Vector3(DOT_ICON_GAP * 1.5f, 0f, 0f);
        }
        if (_frostCountText != null)
        {
            _frostCountText.gameObject.SetActive(hasFrost);
            if (hasFrost)
            {
                _frostCountText.transform.localPosition = new Vector3(DOT_ICON_GAP * 1.5f + DOT_ICON_TEXT_OFFSET, 0f, 0f);
                _frostCountText.text = frostStacks > 0 ? $"x{frostStacks}" : "";
            }
        }

        // 全部隐藏时隐藏容器
        if (_dotIconContainer != null)
            _dotIconContainer.gameObject.SetActive(hasBleed || hasPoison || hasBurn || hasFrost);
    }

    /// <summary>
    /// 获取 StatusEffectType 对应的颜色
    /// </summary>
    private Color GetEffectColor(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Bleed:
            case StatusEffectType.Rend:
                return new Color(0.9f, 0.1f, 0.1f);
            case StatusEffectType.Poison:
                return new Color(0.1f, 0.9f, 0.2f);
            case StatusEffectType.Burn:
            case StatusEffectType.Immolate:
                return new Color(1f, 0.5f, 0f);
            case StatusEffectType.Frostbite:
                return new Color(0.3f, 0.6f, 1f);
            case StatusEffectType.Corrosion:
            case StatusEffectType.Erosion:
                return new Color(0.5f, 0.8f, 0.2f);
            case StatusEffectType.Curse:
            case StatusEffectType.Wither:
                return new Color(0.4f, 0f, 0.6f);
            case StatusEffectType.Agony:
                return new Color(0.6f, 0f, 0.3f);
            case StatusEffectType.Radiate:
                return new Color(0f, 1f, 0.5f);
            case StatusEffectType.Contaminate:
                return new Color(0.3f, 0.5f, 0.3f);
            case StatusEffectType.WindErosion:
                return new Color(0.7f, 0.85f, 1f);
            default:
                return Color.white;
        }
    }

    private void LateUpdate()
    {
        if (_damageable == null || _barTransform == null) return;

        UpdateFill();
        UpdateDotIndicators();

        // 保持血条水平（不随敌人旋转）
        _barTransform.rotation = Quaternion.identity;
    }

    private void UpdateFill()
    {
        if (_damageable == null || _fillRenderer == null) return;

        float percent = _damageable.HpPercent;
        percent = Mathf.Clamp01(percent);

        // 中心 pivot：填充条始终与背景条对齐居中，scale.x 控制宽度
        var fillScale = _fillRenderer.transform.localScale;
        fillScale.x = _barWidth * percent;
        _fillRenderer.transform.localScale = fillScale;

        // 颜色渐变：荧光青 → 洋红 → 亮粉
        if (percent > 0.6f)
        {
            _fillRenderer.color = Color.Lerp(UIColorTheme.AccentMagenta, UIColorTheme.AccentCyan, (percent - 0.6f) / 0.4f);
        }
        else if (percent > 0.3f)
        {
            _fillRenderer.color = Color.Lerp(UIColorTheme.AccentPink, UIColorTheme.AccentMagenta, (percent - 0.3f) / 0.3f);
        }
        else
        {
            _fillRenderer.color = UIColorTheme.AccentPink;
        }

        // 满血时隐藏血条
        if (percent >= 1f)
        {
            _backgroundRenderer.enabled = false;
            _fillRenderer.enabled = false;
        }
        else
        {
            _backgroundRenderer.enabled = true;
            _fillRenderer.enabled = true;
        }
    }

    /// <summary>
    /// 更新 DOT 效果指示器的显示状态。
    /// 同时检查独立 DOT 组件和 StatusEffectManager 中的所有效果，
    /// 在血条上方按优先级排列显示所有活跃的持续伤害效果。
    /// </summary>
    private void UpdateDotIndicators()
    {
        if (_dotContainer == null) return;

        // ── 收集所有活跃 DOT ──
        // 独立组件效果
        bool hasPoisonStack = false;
        int poisonStacks = 0;
        bool hasBurnStack = false;
        bool hasBleedComponent = false;
        bool hasFrostComponent = false;

        // StatusEffectManager 中的效果（按类型去重）
        var semActiveTypes = new HashSet<StatusEffectType>();

        // 检查 PoisonStackEffect（中毒 - 持续到死亡）
        var poison = GetComponent<PoisonStackEffect>();
        if (poison != null && poison.StackCount > 0)
        {
            hasPoisonStack = true;
            poisonStacks = poison.StackCount;
        }

        // #6 燃烧/霜冻层数追踪
        int burnStacks = 0;
        int frostStacks = 0;

        // 检查 BurnStackEffect（燃烧）
        var burn = GetComponent<BurnStackEffect>();
        if (burn != null && burn.StackCount > 0)
        {
            hasBurnStack = true;
            burnStacks = burn.StackCount;
        }

        // 检查 BleedEffect（流血）
        var bleed = GetComponent<BleedEffect>();
        if (bleed != null)
            hasBleedComponent = true;

        // 检查 FrostEffect（霜冻）
        var frost = GetComponent<FrostEffect>();
        if (frost != null)
        {
            hasFrostComponent = true;
            // FrostEffect 可能有层数（通过 StatusEffectManager 查询）
        }

        // 检查 StatusEffectManager 中的所有 DOT
        var sem = GetComponent<StatusEffectManager>();
        if (sem != null && sem.HasAnyDot)
        {
            foreach (var effect in sem.ActiveEffects)
            {
                semActiveTypes.Add(effect.type);
            }
        }

        // ── 构建指示器列表 ──
        // 存储 (类型名, 是否活跃, 颜色, 宽度比例, 是否脉冲)
        var indicatorEntries = new List<IndicatorEntry>();

        // 独立组件指示器
        if (hasPoisonStack)
        {
            float widthScale = Mathf.Clamp01(poisonStacks / 10f);
            indicatorEntries.Add(new IndicatorEntry("PoisonStack", true,
                new Color(0.1f, 0.9f, 0.2f), Mathf.Max(0.3f, widthScale), true, 6f));

            // #21 显示中毒层数文字
            if (_poisonStackText != null)
            {
                _poisonStackText.gameObject.SetActive(true);
                _poisonStackText.text = "x" + poisonStacks;
                float intensity = Mathf.Clamp01(poisonStacks / 15f);
                _poisonStackText.color = Color.Lerp(new Color(0.2f, 1f, 0.3f), new Color(0.8f, 1f, 0.1f), intensity);
            }
        }
        else
        {
            if (_poisonStackText != null) _poisonStackText.gameObject.SetActive(false);
        }
        if (hasBleedComponent)
        {
            indicatorEntries.Add(new IndicatorEntry("BleedComp", true,
                new Color(0.9f, 0.1f, 0.1f), 1f, false, 0f));
        }
        if (hasBurnStack)
        {
            indicatorEntries.Add(new IndicatorEntry("BurnStack", true,
                new Color(1f, 0.5f, 0f), 1f, true, 10f));
        }
        if (hasFrostComponent)
        {
            indicatorEntries.Add(new IndicatorEntry("FrostComp", true,
                new Color(0.3f, 0.6f, 1f), 1f, false, 0f));
        }

        // StatusEffectManager 指示器（排除已被独立组件覆盖的类型）
        foreach (var type in semActiveTypes)
        {
            // 如果独立组件已经覆盖了该类型，跳过
            if (type == StatusEffectType.Poison && hasPoisonStack) continue;
            if ((type == StatusEffectType.Burn || type == StatusEffectType.Immolate) && hasBurnStack) continue;
            if ((type == StatusEffectType.Bleed || type == StatusEffectType.Rend) && hasBleedComponent) continue;
            if (type == StatusEffectType.Frostbite && hasFrostComponent) continue;

            Color color = GetEffectColor(type);
            bool pulse = (type == StatusEffectType.Burn || type == StatusEffectType.Immolate ||
                         type == StatusEffectType.Poison);
            float pulseSpeed = (type == StatusEffectType.Poison) ? 6f : 10f;
            indicatorEntries.Add(new IndicatorEntry(type.ToString(), true, color, 1f, pulse, pulseSpeed));
        }

        // ── 布局和显示 ──
        int activeCount = indicatorEntries.Count;
        if (activeCount == 0)
        {
            HideAllIndicators();
            return;
        }

        float totalWidth = DOT_INDICATOR_WIDTH;
        // 自动缩放：指示器越多，每个越窄
        float entryWidth = totalWidth * Mathf.Clamp01(4f / activeCount);

        float startX = -((activeCount - 1) * (entryWidth + DOT_GAP)) * 0.5f;

        // 显示独立组件指示器
        SetIndicatorActive(_poisonStackIndicator, false);
        SetIndicatorActive(_burnStackIndicator, false);
        SetIndicatorActive(_bleedComponentIndicator, false);
        SetIndicatorActive(_frostComponentIndicator, false);

        // 隐藏所有 SEM 指示器
        foreach (var kvp in _semIndicators)
            kvp.Value.enabled = false;

        for (int i = 0; i < activeCount; i++)
        {
            var entry = indicatorEntries[i];
            float posX = startX + i * (entryWidth + DOT_GAP);
            float actualWidth = entryWidth * entry.widthScale;

            SpriteRenderer sr = null;

            // 优先使用独立组件指示器
            switch (entry.name)
            {
                case "PoisonStack": sr = _poisonStackIndicator; break;
                case "BurnStack": sr = _burnStackIndicator; break;
                case "BleedComp": sr = _bleedComponentIndicator; break;
                case "FrostComp": sr = _frostComponentIndicator; break;
            }

            // 如果不是独立组件，使用 SEM 指示器
            if (sr == null)
            {
                // 查找对应的 StatusEffectType
                foreach (StatusEffectType t in System.Enum.GetValues(typeof(StatusEffectType)))
                {
                    if (t.ToString() == entry.name)
                    {
                        sr = GetOrCreateSemIndicator(t, entry.color);
                        break;
                    }
                }
            }

            if (sr == null) continue;

            sr.enabled = true;
            sr.transform.localPosition = new Vector3(posX, 0f, 0f);
            sr.transform.localScale = new Vector3(actualWidth, DOT_INDICATOR_HEIGHT, 1f);

            // 脉冲效果
            if (entry.hasPulse)
            {
                float pulse = Mathf.Sin(Time.time * entry.pulseSpeed) * 0.2f;
                sr.color = new Color(
                    entry.color.r,
                    Mathf.Clamp01(entry.color.g + pulse * 0.3f),
                    entry.color.b);
            }
            else
            {
                sr.color = entry.color;
            }
        }

        // #6 更新 DOT 图标显示
        UpdateDotIcons(hasBleedComponent, hasPoisonStack, poisonStacks,
            hasBurnStack, burnStacks, hasFrostComponent, frostStacks);
    }

    private void SetIndicatorActive(SpriteRenderer sr, bool active)
    {
        if (sr != null) sr.enabled = active;
    }

    private void HideAllIndicators()
    {
        if (_poisonStackIndicator != null) _poisonStackIndicator.enabled = false;
        if (_burnStackIndicator != null) _burnStackIndicator.enabled = false;
        if (_bleedComponentIndicator != null) _bleedComponentIndicator.enabled = false;
        if (_frostComponentIndicator != null) _frostComponentIndicator.enabled = false;
        foreach (var kvp in _semIndicators)
            kvp.Value.enabled = false;

        // #6 隐藏所有 DOT 图标
        if (_bleedIcon != null) _bleedIcon.enabled = false;
        if (_poisonIcon != null) _poisonIcon.enabled = false;
        if (_burnIcon != null) _burnIcon.enabled = false;
        if (_frostIcon != null) _frostIcon.enabled = false;
        if (_bleedCountText != null) _bleedCountText.gameObject.SetActive(false);
        if (_burnCountText != null) _burnCountText.gameObject.SetActive(false);
        if (_frostCountText != null) _frostCountText.gameObject.SetActive(false);
        if (_dotIconContainer != null) _dotIconContainer.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_barTransform != null)
        {
            Destroy(_barTransform.gameObject);
        }
    }

    /// <summary>
    /// 指示器条目数据
    /// </summary>
    private struct IndicatorEntry
    {
        public string name;
        public bool active;
        public Color color;
        public float widthScale;
        public bool hasPulse;
        public float pulseSpeed;

        public IndicatorEntry(string name, bool active, Color color, float widthScale, bool hasPulse, float pulseSpeed)
        {
            this.name = name;
            this.active = active;
            this.color = color;
            this.widthScale = widthScale;
            this.hasPulse = hasPulse;
            this.pulseSpeed = pulseSpeed;
        }
    }
}