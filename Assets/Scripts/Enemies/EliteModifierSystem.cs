using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 精英敌人词缀系统 — 从第5波开始随机生成带词缀的精英敌人。
///
/// 规则：
///   - 第5波起每波有 10% + (wave-5)×2% 概率生成精英
///   - 精英血量×3，体积+20%，击杀经验×3
///   - 精英带有1-2个词缀（第10波起可带2个）
///   - 精英头顶显示词缀图标
///
/// 使用方式：挂载到敌人 GameObject 上，由 SpawnManager 调用 ApplyElite()
/// </summary>
public class EliteModifierSystem : MonoBehaviour
{
    /// <summary>
    /// 词缀类型枚举
    /// </summary>
    public enum ModifierType
    {
        Swift,          // 疾风：移速+50%
        Regeneration,   // 再生：每秒恢复2%最大HP
        Split,          // 分裂：死亡后分裂为2个普通版
        Shield,         // 护盾：每5秒获得吸收盾(50HP)
        Berserk,        // 狂暴：HP<30%时攻击力×2
        Purify,         // 净化：附近敌人DOT效果被清除
        Vampiric,       // 吸血：攻击玩家时恢复自身HP
        Thorns,         // 反甲：受到DOT伤害时反弹30%给玩家
        Invisibility,   // 隐身：周期性隐身3秒
        Summoner,       // 召唤：每10秒召唤3个普通敌人
        HasteAura,      // 加速光环：附近敌人移速+30%
        ElementalShield // 元素护盾：免疫一种DOT类型
    }

    /// <summary>
    /// 词缀数据
    /// </summary>
    public class ModifierData
    {
        public ModifierType type;
        public string displayName;
        public string description;
        public Color iconColor;
    }

    /// <summary>
    /// 当前拥有的词缀列表
    /// </summary>
    public List<ModifierType> ActiveModifiers { get; private set; } = new List<ModifierType>();

    /// <summary>
    /// 是否为精英敌人
    /// </summary>
    public bool IsElite => ActiveModifiers.Count > 0;

    // ── 运行时状态 ──
    private EnemyBase _enemyBase;
    private Damageable _damageable;
    private SpriteRenderer _spriteRenderer;
    private Color _originalColor;
    private float _originalSpeed;
    private int _originalContactDamage;

    // ── 词缀效果计时器 ──
    private float _regenTimer;
    private float _shieldTimer;
    private float _invisTimer;
    private bool _isInvisible;
    private float _summonTimer;
    private float _hasteAuraTimer;
    private int _shieldHp;

    // ── 配置 ──
    private const float REGEN_INTERVAL = 1f;
    private const float REGEN_PERCENT = 0.02f;
    private const float SHIELD_INTERVAL = 5f;
    private const float SHIELD_AMOUNT = 50f;
    private const float INVIS_CYCLE = 8f;
    private const float INVIS_DURATION = 3f;
    private const float SUMMON_INTERVAL = 10f;
    private const float HASTE_AURA_INTERVAL = 0.5f;
    private const float HASTE_AURA_RADIUS = 5f;
    private const float HASTE_AURA_BONUS = 0.3f;
    private const float BERSERK_HP_THRESHOLD = 0.3f;
    private const float BERSERK_DAMAGE_MULT = 2f;
    private const float THORNS_REFLECT_PERCENT = 0.3f;
    private const float SPLIT_HP_PERCENT = 0.5f;

    // ── 缓存 ──
    private TextMesh _modifierLabel;
    private static Font _cachedFont;

    // ════════════════════════════════════════════════════════════════
    // 公共 API
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 判断当前波次是否应生成精英
    /// </summary>
    public static bool ShouldSpawnElite(int wave)
    {
        if (wave < 5) return false;
        float chance = 0.10f + (wave - 5) * 0.02f;
        return Random.value < chance;
    }

    /// <summary>
    /// 将敌人升级为精英，应用词缀
    /// </summary>
    public void ApplyElite(int wave)
    {
        Init();

        // 决定词缀数量
        int modifierCount = wave >= 10 ? (Random.value < 0.3f ? 2 : 1) : 1;

        // 随机选择词缀（不重复）
        var available = new List<ModifierType>((ModifierType[])System.Enum.GetValues(typeof(ModifierType)));
        for (int i = 0; i < modifierCount && available.Count > 0; i++)
        {
            int idx = Random.Range(0, available.Count);
            ActiveModifiers.Add(available[idx]);
            available.RemoveAt(idx);
        }

        // 应用基础精英加成
        ApplyEliteBaseStats();

        // 应用每个词缀的具体效果
        foreach (var mod in ActiveModifiers)
        {
            ApplyModifierEffect(mod);
        }

        // 更新视觉
        UpdateVisual();
        CreateModifierLabel();

        DebugHelper.Log($"[EliteModifier] Applied {ActiveModifiers.Count} modifiers to {gameObject.name}: {string.Join(", ", ActiveModifiers)}");
    }

    /// <summary>
    /// 获取词缀显示信息
    /// </summary>
    public static ModifierData GetModifierInfo(ModifierType type)
    {
        return type switch
        {
            ModifierType.Swift => new ModifierData { type = type, displayName = "疾风", description = "移速+50%", iconColor = new Color(0.3f, 0.8f, 1f) },
            ModifierType.Regeneration => new ModifierData { type = type, displayName = "再生", description = "每秒回血2%", iconColor = new Color(0.2f, 1f, 0.3f) },
            ModifierType.Split => new ModifierData { type = type, displayName = "分裂", description = "死亡分裂", iconColor = new Color(1f, 0.6f, 0.8f) },
            ModifierType.Shield => new ModifierData { type = type, displayName = "护盾", description = "吸收盾", iconColor = new Color(0.4f, 0.6f, 1f) },
            ModifierType.Berserk => new ModifierData { type = type, displayName = "狂暴", description = "低血狂暴", iconColor = new Color(1f, 0.2f, 0.2f) },
            ModifierType.Purify => new ModifierData { type = type, displayName = "净化", description = "清除DOT", iconColor = new Color(1f, 1f, 0.5f) },
            ModifierType.Vampiric => new ModifierData { type = type, displayName = "吸血", description = "攻击回血", iconColor = new Color(0.8f, 0.1f, 0.3f) },
            ModifierType.Thorns => new ModifierData { type = type, displayName = "反甲", description = "DOT反弹", iconColor = new Color(0.6f, 0.6f, 0.6f) },
            ModifierType.Invisibility => new ModifierData { type = type, displayName = "隐身", description = "周期隐身", iconColor = new Color(0.7f, 0.7f, 0.9f) },
            ModifierType.Summoner => new ModifierData { type = type, displayName = "召唤", description = "召唤小怪", iconColor = new Color(0.9f, 0.5f, 1f) },
            ModifierType.HasteAura => new ModifierData { type = type, displayName = "加速光环", description = "友军加速", iconColor = new Color(1f, 0.9f, 0.3f) },
            ModifierType.ElementalShield => new ModifierData { type = type, displayName = "元素护盾", description = "DOT免疫", iconColor = new Color(0.5f, 1f, 0.8f) },
            _ => new ModifierData { type = type, displayName = "未知", description = "", iconColor = Color.white }
        };
    }

    /// <summary>
    /// 受到伤害时调用（反甲词缀用）
    /// </summary>
    public void OnEliteDamaged(int damage, bool isDot)
    {
        if (!IsElite) return;

        // 反甲：DOT伤害反弹
        if (isDot && ActiveModifiers.Contains(ModifierType.Thorns))
        {
            int reflected = Mathf.RoundToInt(damage * THORNS_REFLECT_PERCENT);
            var player = GameReferences.Player;
            if (player != null)
            {
                var playerDmg = player.GetComponent<Damageable>();
                if (playerDmg != null)
                {
                    playerDmg.TakeDamage(reflected);
                }
            }
        }

        // 护盾：先扣盾血
        if (_shieldHp > 0)
        {
            _shieldHp = Mathf.Max(0, _shieldHp - damage);
        }
    }

    /// <summary>
    /// 获取当前吸收盾HP（UI显示用）
    /// </summary>
    public int ShieldHp => _shieldHp;

    // ════════════════════════════════════════════════════════════════
    // 生命周期
    // ════════════════════════════════════════════════════════════════

    private void Init()
    {
        if (_enemyBase == null)
        {
            _enemyBase = GetComponent<EnemyBase>();
            _damageable = GetComponent<Damageable>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
                _originalColor = _spriteRenderer.color;
            if (_enemyBase != null)
            {
                _originalSpeed = _enemyBase.MoveSpeed;
                _originalContactDamage = _enemyBase.ContactDamage;
            }
        }
    }

    private void OnEnable()
    {
        // 对象池回收时重置
        ActiveModifiers.Clear();
        _regenTimer = 0f;
        _shieldTimer = 0f;
        _invisTimer = 0f;
        _isInvisible = false;
        _summonTimer = 0f;
        _hasteAuraTimer = 0f;
        _shieldHp = 0;

        // 恢复原始颜色
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = _originalColor;
            _spriteRenderer.enabled = true;
        }

        // 移除词缀标签
        if (_modifierLabel != null)
        {
            Destroy(_modifierLabel.gameObject);
            _modifierLabel = null;
        }
    }

    private void Update()
    {
        if (!IsElite || _enemyBase == null || !_enemyBase.Alive) return;

        float dt = Time.deltaTime;

        // 再生
        if (ActiveModifiers.Contains(ModifierType.Regeneration) && _damageable != null)
        {
            _regenTimer += dt;
            if (_regenTimer >= REGEN_INTERVAL)
            {
                _regenTimer = 0f;
                int healAmount = Mathf.RoundToInt(_damageable.MaxHp * REGEN_PERCENT);
                _damageable.Heal(healAmount);
            }
        }

        // 护盾
        if (ActiveModifiers.Contains(ModifierType.Shield) && _damageable != null)
        {
            _shieldTimer += dt;
            if (_shieldTimer >= SHIELD_INTERVAL)
            {
                _shieldTimer = 0f;
                _shieldHp += Mathf.RoundToInt(SHIELD_AMOUNT);
            }
        }

        // 隐身
        if (ActiveModifiers.Contains(ModifierType.Invisibility))
        {
            _invisTimer += dt;
            if (!_isInvisible && _invisTimer >= INVIS_CYCLE)
            {
                _isInvisible = true;
                _invisTimer = 0f;
                SetInvisible(true);
            }
            else if (_isInvisible && _invisTimer >= INVIS_DURATION)
            {
                _isInvisible = false;
                _invisTimer = 0f;
                SetInvisible(false);
            }
        }

        // 召唤
        if (ActiveModifiers.Contains(ModifierType.Summoner))
        {
            _summonTimer += dt;
            if (_summonTimer >= SUMMON_INTERVAL)
            {
                _summonTimer = 0f;
                SummonMinions();
            }
        }

        // 加速光环
        if (ActiveModifiers.Contains(ModifierType.HasteAura))
        {
            _hasteAuraTimer += dt;
            if (_hasteAuraTimer >= HASTE_AURA_INTERVAL)
            {
                _hasteAuraTimer = 0f;
                ApplyHasteAura();
            }
        }

        // 狂暴
        if (ActiveModifiers.Contains(ModifierType.Berserk) && _enemyBase != null)
        {
            if (_damageable != null)
            {
                float hpPercent = (float)_damageable.CurrentHp / _damageable.MaxHp;
                if (hpPercent <= BERSERK_HP_THRESHOLD)
                {
                    // 低血量时增强
                    if (_spriteRenderer != null)
                        _spriteRenderer.color = Color.Lerp(_originalColor, Color.red, 0.5f);
                }
            }
        }
    }

    // ════════════════════════════════════════════════════════════════
    // 内部方法
    // ════════════════════════════════════════════════════════════════

    private void ApplyEliteBaseStats()
    {
        // 血量×3
        if (_damageable != null)
        {
            int eliteMaxHp = _damageable.MaxHp * 3;
            _damageable.SetMaxHp(eliteMaxHp);
            _damageable.Heal(eliteMaxHp);
        }

        // 体积+20%
        transform.localScale *= 1.2f;

        // 击杀经验×3
        var killRewarder = GetComponent<KillRewarder>();
        if (killRewarder != null)
        {
            killRewarder.SetRewards(killRewarder.XpReward * 3, killRewarder.CoinReward * 2);
        }
    }

    private void ApplyModifierEffect(ModifierType type)
    {
        switch (type)
        {
            case ModifierType.Swift:
                if (_enemyBase != null)
                    _enemyBase.MoveSpeed *= 1.5f;
                break;
            case ModifierType.Berserk:
                // Update() 中处理
                break;
        }
    }

    private void UpdateVisual()
    {
        // 精英颜色：偏暗红
        if (_spriteRenderer != null)
        {
            _originalColor = _spriteRenderer.color;
            _spriteRenderer.color = new Color(
                Mathf.Min(1f, _originalColor.r * 1.2f),
                _originalColor.g * 0.7f,
                _originalColor.b * 0.7f,
                _originalColor.a
            );
            // 保存调整后的颜色为原始颜色（用于恢复）
            _originalColor = _spriteRenderer.color;
        }
    }

    private void CreateModifierLabel()
    {
        if (_cachedFont == null)
            _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var labelGo = new GameObject("EliteModifierLabel");
        labelGo.transform.SetParent(transform);
        labelGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);

        _modifierLabel = labelGo.AddComponent<TextMesh>();
        _modifierLabel.font = _cachedFont;
        _modifierLabel.alignment = TextAlignment.Center;
        _modifierLabel.anchor = TextAnchor.MiddleCenter;
        _modifierLabel.characterSize = 0.15f;
        _modifierLabel.fontSize = 40;

        // 显示词缀名称
        var names = new List<string>();
        foreach (var mod in ActiveModifiers)
        {
            var info = GetModifierInfo(mod);
            names.Add(info.displayName);
        }
        _modifierLabel.text = string.Join(" ", names);
        _modifierLabel.color = ActiveModifiers.Count >= 2 ? new Color(1f, 0.85f, 0f) : new Color(1f, 0.4f, 0.3f);

        var mr = labelGo.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 95;
    }

    private void SetInvisible(bool invisible)
    {
        if (_spriteRenderer != null)
        {
            Color c = _spriteRenderer.color;
            c.a = invisible ? 0.15f : 1f;
            _spriteRenderer.color = c;
        }
    }

    private void SummonMinions()
    {
        if (GameReferences.SpawnManager == null) return;

        for (int i = 0; i < 3; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 2f;
            Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0);
            GameReferences.SpawnManager.SpawnSingleEnemy(spawnPos);
        }

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.9f, 0.5f, 1f);
    }

    private void ApplyHasteAura()
    {
        var allies = Physics2D.OverlapCircleAll(transform.position, HASTE_AURA_RADIUS);
        foreach (var col in allies)
        {
            if (col.gameObject == gameObject) continue;
            var enemy = col.GetComponent<EnemyBase>();
            if (enemy != null && enemy.Alive)
            {
                // 临时加速（持续到下次刷新）
                float baseSpeed = enemy.MoveSpeed;
                if (baseSpeed < _originalSpeed * (1f + HASTE_AURA_BONUS))
                {
                    enemy.MoveSpeed = baseSpeed * (1f + HASTE_AURA_BONUS);
                }
            }
        }
    }

    // ── Gizmos ──
    private void OnDrawGizmosSelected()
    {
        if (IsElite && ActiveModifiers.Contains(ModifierType.HasteAura))
        {
            Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, HASTE_AURA_RADIUS);
        }
    }
}