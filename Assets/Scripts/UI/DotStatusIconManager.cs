using UnityEngine;

/// <summary>
/// DOT 图标管理器 — 从 DotStatusIndicator 拆分而来
/// 负责创建和更新敌人血条上方的 DOT 类型小图标（几何形状 + 层数文字）
/// </summary>
public class DotStatusIconManager
{
    private Transform _dotIconContainer;

    // 图标 SpriteRenderers（小几何形状）
    private SpriteRenderer _bleedIcon;
    private SpriteRenderer _poisonIcon;
    private SpriteRenderer _burnIcon;
    private SpriteRenderer _frostIcon;

    // 层数文字
    private TextMesh _bleedCountText;
    private TextMesh _burnCountText;
    private TextMesh _frostCountText;

    // 常量
    private const float DOT_ICON_SIZE = 0.25f;
    private const float DOT_ICON_GAP = 0.35f;
    private const float DOT_ICON_TEXT_OFFSET = 0.18f;

    /// <summary>
    /// 创建 DOT 图标容器和所有图标
    /// </summary>
    public void Create(Transform parentTransform, float barHeight, float indicatorHeight, float offsetAboveBar)
    {
        _dotIconContainer = new GameObject("DOT_Icons").transform;
        _dotIconContainer.SetParent(parentTransform);
        _dotIconContainer.localPosition = new Vector3(0f, barHeight + offsetAboveBar + indicatorHeight + 0.15f, 0f);
        _dotIconContainer.localRotation = Quaternion.identity;

        _bleedIcon = CreateShapeIcon("BleedIcon", HealthBarSpriteHelper.GetTriangleSprite(), new Color(0.9f, 0.1f, 0.1f));
        _poisonIcon = CreateShapeIcon("PoisonIcon", HealthBarSpriteHelper.GetDiamondSprite(), new Color(0.1f, 0.9f, 0.2f));
        _burnIcon = CreateShapeIcon("BurnIcon", HealthBarSpriteHelper.GetPentagonSprite(), new Color(1f, 0.5f, 0f));
        _frostIcon = CreateShapeIcon("FrostIcon", HealthBarSpriteHelper.GetHexagonSprite(), new Color(0.3f, 0.6f, 1f));

        _bleedCountText = CreateIconCountText("BleedCount", new Color(1f, 0.3f, 0.3f));
        _burnCountText = CreateIconCountText("BurnCount", new Color(1f, 0.7f, 0.2f));
        _frostCountText = CreateIconCountText("FrostCount", new Color(0.5f, 0.8f, 1f));
    }

    /// <summary>
    /// 更新 DOT 图标显示
    /// </summary>
    public void Update(bool hasBleed, bool hasPoison, int poisonStacks,
        bool hasBurn, int burnStacks, bool hasFrost, int frostStacks,
        TextMesh poisonStackText)
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
        if (poisonStackText != null && hasPoison)
        {
            poisonStackText.transform.localPosition = new Vector3(-DOT_ICON_GAP * 0.5f + DOT_ICON_TEXT_OFFSET, 0f, 0f);
            poisonStackText.characterSize = 0.06f;
        }

        // 燃烧图标
        if (_burnIcon != null)
        {
            _burnIcon.enabled = hasBurn;
            if (hasBurn)
            {
                _burnIcon.transform.localPosition = new Vector3(DOT_ICON_GAP * 0.5f, 0f, 0f);
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
    /// 隐藏所有图标
    /// </summary>
    public void HideAll()
    {
        if (_bleedIcon != null) _bleedIcon.enabled = false;
        if (_poisonIcon != null) _poisonIcon.enabled = false;
        if (_burnIcon != null) _burnIcon.enabled = false;
        if (_frostIcon != null) _frostIcon.enabled = false;
        if (_bleedCountText != null) _bleedCountText.gameObject.SetActive(false);
        if (_burnCountText != null) _burnCountText.gameObject.SetActive(false);
        if (_frostCountText != null) _frostCountText.gameObject.SetActive(false);
        if (_dotIconContainer != null) _dotIconContainer.gameObject.SetActive(false);
    }

    // ═══ 内部方法 ═══

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
}