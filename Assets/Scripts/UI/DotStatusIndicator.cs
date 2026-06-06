using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DOT 状态指示器 — 在敌人血条上方显示所有活跃的持续伤害类型图标和层数。
///
/// 支持的 DOT 类型：
/// - 🟢 中毒（显示层数条 + 层数文字）
/// - 🔴 流血
/// - 🟠 燃烧（脉冲闪烁）
/// - 🔵 霜冻
/// - 其他 StatusEffectManager 中的 DOT 类型
///
/// 使用方式：由 EnemyHealthBar 在 Setup 时创建并委托调用。
/// DOT 图标系统委托给 DotStatusIconManager。
/// </summary>
public class DotStatusIndicator
{
    // DOT 效果指示器容器（与血条同等大小）
    private Transform _dotContainer;
    private const float DOT_INDICATOR_HEIGHT = 0.22f;
    private const float DOT_INDICATOR_WIDTH = 2.2f;
    private const float DOT_GAP = 0.04f;
    private const float DOT_OFFSET_ABOVE_BAR = 0.04f;

    // 独立 DOT 组件指示器
    private SpriteRenderer _poisonStackIndicator;
    private SpriteRenderer _burnStackIndicator;
    private SpriteRenderer _bleedComponentIndicator;
    private SpriteRenderer _frostComponentIndicator;
    // 中毒层数文字显示
    private TextMesh _poisonStackText;

    // DOT 图标管理器（委托给 DotStatusIconManager）
    private DotStatusIconManager _iconManager;

    // StatusEffectManager 动态指示器缓存
    private Dictionary<StatusEffectType, SpriteRenderer> _semIndicators
        = new Dictionary<StatusEffectType, SpriteRenderer>();

    // 指示器条目数据
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

    /// <summary>
    /// 创建 DOT 指示器（在血条上方），由 EnemyHealthBar.Setup 调用
    /// </summary>
    /// <param name="parentTransform">血条父容器 Transform</param>
    /// <param name="barHeight">血条高度，用于定位上方偏移</param>
    public void Create(Transform parentTransform, float barHeight)
    {
        _dotContainer = new GameObject("DOT_Indicators").transform;
        _dotContainer.SetParent(parentTransform);
        _dotContainer.localPosition = new Vector3(0f, barHeight + DOT_OFFSET_ABOVE_BAR, 0f);
        _dotContainer.localRotation = Quaternion.identity;

        // 独立 DOT 组件指示器
        _poisonStackIndicator = CreateSingleIndicator("PoisonStack", new Color(0.1f, 0.9f, 0.2f));
        _burnStackIndicator = CreateSingleIndicator("BurnStack", new Color(1f, 0.5f, 0f));
        _bleedComponentIndicator = CreateSingleIndicator("BleedComp", new Color(0.9f, 0.1f, 0.1f));
        _frostComponentIndicator = CreateSingleIndicator("FrostComp", new Color(0.3f, 0.6f, 1f));

        // 创建中毒层数文字
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

        // 创建 DOT 图标系统（委托给 DotStatusIconManager）
        _iconManager = new DotStatusIconManager();
        _iconManager.Create(_dotContainer.parent, barHeight, DOT_INDICATOR_HEIGHT, DOT_OFFSET_ABOVE_BAR);
    }

    /// <summary>
    /// 更新所有 DOT 指示器的显示状态
    /// </summary>
    /// <param name="target">敌人 GameObject（用于查询 DOT 组件）</param>
    public void Update(GameObject target)
    {
        if (_dotContainer == null || target == null) return;

        // ── 收集所有活跃 DOT ──
        bool hasPoisonStack = false;
        int poisonStacks = 0;
        bool hasBurnStack = false;
        bool hasBleedComponent = false;
        bool hasFrostComponent = false;

        var semActiveTypes = new HashSet<StatusEffectType>();

        // 检查 PoisonStackEffect（中毒 - 持续到死亡）
        var poison = target.GetComponent<PoisonStackEffect>();
        if (poison != null && poison.StackCount > 0)
        {
            hasPoisonStack = true;
            poisonStacks = poison.StackCount;
        }

        int burnStacks = 0;
        int frostStacks = 0;

        // 检查 BurnStackEffect（燃烧）
        var burn = target.GetComponent<BurnStackEffect>();
        if (burn != null && burn.StackCount > 0)
        {
            hasBurnStack = true;
            burnStacks = burn.StackCount;
        }

        // 检查 BleedEffect（流血）
        var bleed = target.GetComponent<BleedEffect>();
        if (bleed != null)
            hasBleedComponent = true;

        // 检查 FrostEffect（霜冻）
        var frost = target.GetComponent<FrostEffect>();
        if (frost != null)
            hasFrostComponent = true;

        // 检查 StatusEffectManager 中的所有 DOT
        var sem = target.GetComponent<StatusEffectManager>();
        if (sem != null && sem.HasAnyDot)
        {
            foreach (var effect in sem.ActiveEffects)
            {
                semActiveTypes.Add(effect.type);
            }
        }

        // ── 构建指示器列表 ──
        var indicatorEntries = new List<IndicatorEntry>();

        // 独立组件指示器
        if (hasPoisonStack)
        {
            float widthScale = Mathf.Clamp01(poisonStacks / 10f);
            indicatorEntries.Add(new IndicatorEntry("PoisonStack", true,
                new Color(0.1f, 0.9f, 0.2f), Mathf.Max(0.3f, widthScale), true, 6f));

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
            if (type == StatusEffectType.Poison && hasPoisonStack) continue;
            if ((type == StatusEffectType.Burn || type == StatusEffectType.Immolate) && hasBurnStack) continue;
            if ((type == StatusEffectType.Bleed || type == StatusEffectType.Rend) && hasBleedComponent) continue;
            if (type == StatusEffectType.Frostbite && hasFrostComponent) continue;

            Color color = HealthBarSpriteHelper.GetEffectColor(type);
            bool pulse = (type == StatusEffectType.Burn || type == StatusEffectType.Immolate ||
                         type == StatusEffectType.Poison);
            float pulseSpeed = (type == StatusEffectType.Poison) ? 6f : 10f;
            indicatorEntries.Add(new IndicatorEntry(type.ToString(), true, color, 1f, pulse, pulseSpeed));
        }

        // ── 布局和显示 ──
        int activeCount = indicatorEntries.Count;
        if (activeCount == 0)
        {
            HideAll();
            return;
        }

        float totalWidth = DOT_INDICATOR_WIDTH;
        float entryWidth = totalWidth * Mathf.Clamp01(4f / activeCount);
        float startX = -((activeCount - 1) * (entryWidth + DOT_GAP)) * 0.5f;

        // 隐藏所有独立组件指示器
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

        // 更新 DOT 图标显示（委托给 DotStatusIconManager）
        _iconManager?.Update(hasBleedComponent, hasPoisonStack, poisonStacks,
            hasBurnStack, burnStacks, hasFrostComponent, frostStacks, _poisonStackText);
    }

    /// <summary>
    /// 隐藏所有 DOT 指示器
    /// </summary>
    public void HideAll()
    {
        if (_poisonStackIndicator != null) _poisonStackIndicator.enabled = false;
        if (_burnStackIndicator != null) _burnStackIndicator.enabled = false;
        if (_bleedComponentIndicator != null) _bleedComponentIndicator.enabled = false;
        if (_frostComponentIndicator != null) _frostComponentIndicator.enabled = false;
        foreach (var kvp in _semIndicators)
            kvp.Value.enabled = false;

        // 隐藏所有 DOT 图标（委托给 DotStatusIconManager）
        _iconManager?.HideAll();
    }

    /// <summary>
    /// 销毁所有 DOT 指示器 GameObject
    /// </summary>
    public void Destroy()
    {
        if (_dotContainer != null)
            Object.Destroy(_dotContainer.gameObject);
    }

    // ═══ 内部方法 ═══

    private SpriteRenderer CreateSingleIndicator(string name, Color color)
    {
        var obj = new GameObject("DOT_" + name);
        obj.transform.SetParent(_dotContainer);
        obj.transform.localPosition = Vector3.zero;
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = HealthBarSpriteHelper.GetWhiteSprite();
        sr.color = color;
        sr.sortingOrder = 12;
        sr.enabled = false;
        return sr;
    }

    private SpriteRenderer GetOrCreateSemIndicator(StatusEffectType type, Color color)
    {
        if (_semIndicators.TryGetValue(type, out var existing))
            return existing;

        var sr = CreateSingleIndicator(type.ToString(), color);
        _semIndicators[type] = sr;
        return sr;
    }

    private void SetIndicatorActive(SpriteRenderer sr, bool active)
    {
        if (sr != null) sr.enabled = active;
    }
}