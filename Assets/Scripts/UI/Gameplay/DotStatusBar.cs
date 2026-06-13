using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 统一 DOT 叠层状态栏。
///
/// 职责：
/// - 通过 IStackEffect 接口查询所有活跃的 DOT 叠层效果
/// - 统一布局：血条上方显示叠层指示器（每行最多2个）
/// - 每种 DOT 有独立的图标形状 + 颜色 + "xN" 层数文字
/// - 仅在叠层数变化时更新（dirty flag 优化）
///
/// 布局结构（从下到上）：
///   [血条] ← EnemyHealthBar 管理
///   0.225f 间距
///   [叠层行1：最多2个（图标 + xN）] ← DotStatusBar 管理
///   [叠层行2：最多2个]
///   ...
/// </summary>
public class DotStatusBar
{
    private Transform _container;
    private List<StackDisplayEntry> _entries = new List<StackDisplayEntry>(6);
    private List<IStackEffect> _effectBuffer = new List<IStackEffect>(8);
    private bool _visible;
    private Camera _cachedCam;

    // ═══ 布局常量（150%缩放，每行2个，仅图标+文字）═══
    private const float ICON_SIZE = 0.3f;
    private const float ENTRY_GAP = 0.35f;
    private const float TEXT_CHARACTER_SIZE = 0.09f;
    private const float OFFSET_ABOVE_BAR = 0.225f;
    private const float ROW_SPACING = 0.15f;
    private const int MAX_PER_ROW = 2;
    private const float HIDE_DISTANCE_SQ = 400f;

    private struct StackDisplayEntry
    {
        public StatusEffectType effectType;
        public GameObject root;
        public SpriteRenderer icon;
        public TextMesh countText;
        public int lastStacks;
        public Color baseColor;
    }

    // ═══ 创建 ═══

    public void Create(Transform parentTransform, float barHeight)
    {
        _container = new GameObject("DotStatusBar").transform;
        _container.SetParent(parentTransform);
        _container.localPosition = new Vector3(0f, barHeight + OFFSET_ABOVE_BAR, 0f);
        _container.localRotation = Quaternion.identity;
        _container.gameObject.SetActive(false);
    }

    // ═══ 更新 ═══

    public void Update(GameObject target)
    {
        if (_container == null || target == null) return;

        // 远距离隐藏叠层UI（性能优化）
        if (_cachedCam == null) _cachedCam = Camera.main;
        var cam = _cachedCam;
        if (cam != null)
        {
            float distSq = (cam.transform.position - target.transform.position).sqrMagnitude;
            if (distSq > HIDE_DISTANCE_SQ)
            {
                if (_visible) HideAll();
                return;
            }
        }

        DotEffectRegistry.GetStackEffectsOnEnemy(target, _effectBuffer);

        if (_effectBuffer.Count == 0)
        {
            if (_visible) HideAll();
            return;
        }

        bool needsRebuild = NeedsRebuild();
        if (needsRebuild)
            RebuildEntries();

        for (int i = 0; i < _entries.Count; i++)
        {
            if (i >= _effectBuffer.Count) break;
            var entry = _entries[i];
            int currentStacks = _effectBuffer[i].StackCount;
            if (currentStacks != entry.lastStacks)
            {
                entry.lastStacks = currentStacks;
                _entries[i] = entry;
                UpdateEntryVisual(entry, currentStacks);
            }
        }

        if (!_visible)
        {
            _container.gameObject.SetActive(true);
            _visible = true;
        }
    }

    public void HideAll()
    {
        if (_container != null) _container.gameObject.SetActive(false);
        _visible = false;
    }

    public void Destroy()
    {
        if (_container != null) Object.Destroy(_container.gameObject);
    }

    // ═══ 内部方法 ═══

    private bool NeedsRebuild()
    {
        if (_entries.Count != _effectBuffer.Count) return true;
        for (int i = 0; i < _effectBuffer.Count; i++)
        {
            if (i >= _entries.Count) return true;
            if (_entries[i].effectType != _effectBuffer[i].EffectType) return true;
        }
        return false;
    }

    private void RebuildEntries()
    {
        SortEffects(_effectBuffer);

        int count = Mathf.Min(_effectBuffer.Count, MAX_PER_ROW * 4);

        while (_entries.Count < count)
        {
            var effect = _effectBuffer[_entries.Count];
            var entry = CreateEntry(effect, _entries.Count);
            _entries.Add(entry);
        }

        for (int i = count; i < _entries.Count; i++)
        {
            if (_entries[i].root != null) _entries[i].root.SetActive(false);
        }

        for (int i = 0; i < count; i++)
        {
            var effect = _effectBuffer[i];
            var entry = _entries[i];
            if (entry.effectType != effect.EffectType)
            {
                entry.effectType = effect.EffectType;
                entry.baseColor = GetEffectColor(effect.EffectType);
                entry.lastStacks = -1;
                if (entry.icon != null)
                {
                    entry.icon.sprite = GetEffectShape(effect.EffectType);
                    entry.icon.color = entry.baseColor;
                }
                if (entry.countText != null) entry.countText.color = entry.baseColor;
                _entries[i] = entry;
            }
            if (entry.root != null && !entry.root.activeSelf) entry.root.SetActive(true);
        }

        while (_entries.Count > count + MAX_PER_ROW)
        {
            int last = _entries.Count - 1;
            if (_entries[last].root != null) Object.Destroy(_entries[last].root);
            _entries.RemoveAt(last);
        }

        RelayoutEntries();
    }

    private StackDisplayEntry CreateEntry(IStackEffect effect, int index)
    {
        var entry = new StackDisplayEntry();
        entry.effectType = effect.EffectType;
        entry.lastStacks = -1;
        entry.baseColor = GetEffectColor(effect.EffectType);

        entry.root = new GameObject($"Stack_{effect.EffectType}");
        entry.root.transform.SetParent(_container);

        // 图标形状（居中偏左）
        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(entry.root.transform);
        iconObj.transform.localPosition = new Vector3(-0.12f, 0f, 0f);
        iconObj.transform.localScale = Vector3.one * ICON_SIZE;
        entry.icon = iconObj.AddComponent<SpriteRenderer>();
        entry.icon.sprite = GetEffectShape(effect.EffectType);
        entry.icon.color = entry.baseColor;
        entry.icon.sortingOrder = 14;

        // 层数文字（图标右侧）
        var textObj = new GameObject("Count");
        textObj.transform.SetParent(entry.root.transform);
        textObj.transform.localPosition = new Vector3(0.15f, 0f, 0f);
        entry.countText = textObj.AddComponent<TextMesh>();
        entry.countText.characterSize = TEXT_CHARACTER_SIZE;
        entry.countText.fontSize = 45;
        entry.countText.fontStyle = FontStyle.Bold;
        entry.countText.alignment = TextAlignment.Left;
        entry.countText.anchor = TextAnchor.MiddleLeft;
        entry.countText.color = entry.baseColor;
        entry.countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var mr = textObj.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 15;

        return entry;
    }

    private void RelayoutEntries()
    {
        int count = _entries.Count;
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            int row = i / MAX_PER_ROW;
            int col = i % MAX_PER_ROW;
            int itemsInRow = Mathf.Min(MAX_PER_ROW, count - row * MAX_PER_ROW);

            float entryWidth = ICON_SIZE + 0.3f;
            float rowWidth = itemsInRow * entryWidth + (itemsInRow - 1) * ENTRY_GAP;
            float startX = -rowWidth * 0.5f + entryWidth * 0.5f;

            float x = startX + col * (entryWidth + ENTRY_GAP);
            float y = -(row * (ICON_SIZE + ROW_SPACING));

            var entry = _entries[i];
            if (entry.root != null)
                entry.root.transform.localPosition = new Vector3(x, y, 0f);
        }
    }

    private void UpdateEntryVisual(StackDisplayEntry entry, int stacks)
    {
        if (stacks <= 0)
        {
            if (entry.root != null) entry.root.SetActive(false);
            return;
        }

        if (entry.root != null && !entry.root.activeSelf) entry.root.SetActive(true);

        float intensity = Mathf.Clamp01(stacks / 10f);

        // 图标脉冲
        if (entry.icon != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * (6f + intensity * 6f)) * 0.12f * intensity;
            entry.icon.transform.localScale = Vector3.one * ICON_SIZE * pulse;
        }

        // 层数文字
        if (entry.countText != null)
        {
            entry.countText.text = $"x{stacks}";
            float t = Mathf.Clamp01(stacks / 15f);
            entry.countText.color = Color.Lerp(entry.baseColor, Color.white, t * 0.5f);
        }
    }

    // ═══ 排序 ═══

    private static void SortEffects(List<IStackEffect> effects)
    {
        effects.Sort((a, b) => GetSortOrder(a.EffectType).CompareTo(GetSortOrder(b.EffectType)));
    }

    private static int GetSortOrder(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Poison: return 0;
            case StatusEffectType.Burn: return 1;
            case StatusEffectType.Frostbite: return 2;
            case StatusEffectType.Static: return 3;
            case StatusEffectType.WindErosion: return 4;
            case StatusEffectType.Dark: return 5;
            case StatusEffectType.Light: return 6;
            default: return 100;
        }
    }

    // ═══ 颜色 ═══

    private static Color GetEffectColor(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Poison: return DotColorBlender.POISON_GREEN;
            case StatusEffectType.Burn: return DotColorBlender.BURN_ORANGE;
            case StatusEffectType.Frostbite: return DotColorBlender.FROST_BLUE;
            case StatusEffectType.Static: return DotColorBlender.STATIC_CYAN;
            case StatusEffectType.WindErosion: return new Color(0.7f, 0.85f, 1f);
            case StatusEffectType.Dark: return DotColorBlender.DARK_PURPLE;
            case StatusEffectType.Light: return new Color(1f, 1f, 0.7f);
            default: return HealthBarSpriteHelper.GetEffectColor(type);
        }
    }

    // ═══ 图标形状 ═══

    private static Sprite GetEffectShape(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Poison: return HealthBarSpriteHelper.GetDiamondSprite();
            case StatusEffectType.Burn: return HealthBarSpriteHelper.GetPentagonSprite();
            case StatusEffectType.Frostbite: return HealthBarSpriteHelper.GetHexagonSprite();
            case StatusEffectType.Static: return HealthBarSpriteHelper.GetOctagonSprite();
            case StatusEffectType.WindErosion: return HealthBarSpriteHelper.GetStarSprite();
            case StatusEffectType.Dark: return HealthBarSpriteHelper.GetCircleSprite();
            case StatusEffectType.Light: return HealthBarSpriteHelper.GetStarSprite();
            default: return HealthBarSpriteHelper.GetWhiteSprite();
        }
    }
}
