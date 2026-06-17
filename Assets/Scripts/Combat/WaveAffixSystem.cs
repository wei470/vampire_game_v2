using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 波次词缀系统 — 难度6+解锁。
/// 每波开始随机1个负面词缀生效，持续整波。
/// </summary>
public static class WaveAffixSystem
{
    public enum AffixType
    {
        ShadowEmbrace,      // 暗影之拥：敌人死后留下毒圈
        FrostCurse,         // 冰霜诅咒：玩家移速-20%
        FlameFury,          // 烈焰之怒：敌人被点燃时爆炸
        ThunderJudgment,    // 雷霆审判：每5秒随机位置落雷
        VoidErosion        // 虚空侵蚀：玩家每秒失去1%当前HP
    }

    private static AffixType _currentAffix;
    private static bool _affixActive = false;
    private static readonly List<Collider2D> _overlapBuffer = new List<Collider2D>(16);
    private static float _lastThunderTime;
    private static float _lastVoidTick;

    private static readonly AffixType[] _allAffixes = (AffixType[])System.Enum.GetValues(typeof(AffixType));

    /// <summary>当前激活的词缀</summary>
    public static AffixType CurrentAffix => _currentAffix;

    /// <summary>是否有词缀生效</summary>
    public static bool IsAffixActive => _affixActive;

    /// <summary>
    /// 波次开始时调用 — 随机选择词缀（难度6+生效）
    /// </summary>
    public static void OnWaveStart(int wave)
    {
        if (wave < 6)
        {
            _affixActive = false;
            return;
        }

        _currentAffix = _allAffixes[Random.Range(0, _allAffixes.Length)];
        _affixActive = true;
        _lastThunderTime = Time.time;
        _lastVoidTick = Time.time;

        DebugHelper.Log($"[WaveAffix] Wave {wave} affix: {_currentAffix}");
    }

    /// <summary>
    /// 波次结束时调用 — 清除词缀
    /// </summary>
    public static void OnWaveEnd()
    {
        _affixActive = false;
    }

    /// <summary>
    /// 每帧更新 — 处理持续性词缀效果
    /// </summary>
    public static void Update()
    {
        if (!_affixActive) return;

        switch (_currentAffix)
        {
            case AffixType.ThunderJudgment:
                UpdateThunderJudgment();
                break;
            case AffixType.VoidErosion:
                UpdateVoidErosion();
                break;
        }
    }

    /// <summary>
    /// 获取玩家移速倍率（冰霜诅咒词缀）
    /// </summary>
    public static float GetPlayerSpeedMult()
    {
        if (!_affixActive) return 1f;
        return _currentAffix == AffixType.FrostCurse ? 0.8f : 1f;
    }

    /// <summary>
    /// 敌人死亡时调用（暗影之拥词缀）
    /// </summary>
    public static void OnEnemyDeath(Vector3 position)
    {
        if (!_affixActive || _currentAffix != AffixType.ShadowEmbrace) return;

        FireZone.CreateDefault(position, 3, 4f, 2f, 0.5f);
    }

    /// <summary>
    /// 敌人被点燃时调用（烈焰之怒词缀）
    /// </summary>
    public static void OnEnemyIgnited(Vector3 position)
    {
        if (!_affixActive || _currentAffix != AffixType.FlameFury) return;

        CombatManager.CreateExplosionEffect(position, 2f, new Color(1f, 0.3f, 0f), 0.5f);
        int count = PhysicsHelper.OverlapCircle(position, 2f, _overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            var hit = _overlapBuffer[i];
            if (!hit.CompareTag("Enemy")) continue;
            var dmg = hit.GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp > 0)
                dmg.TakeDamage(10, new Color(1f, 0.5f, 0f));
        }
    }

    /// <summary>
    /// 获取词缀描述文本（UI显示用）
    /// </summary>
    public static string GetAffixDescription()
    {
        if (!_affixActive) return "";
        return _currentAffix switch
        {
            AffixType.ShadowEmbrace => "暗影之拥：敌人死后留下毒圈",
            AffixType.FrostCurse => "冰霜诅咒：玩家移速-20%",
            AffixType.FlameFury => "烈焰之怒：敌人被点燃时爆炸",
            AffixType.ThunderJudgment => "雷霆审判：每5秒随机落雷",
            AffixType.VoidErosion => "虚空侵蚀：每秒失去1%HP",
            _ => ""
        };
    }

    public static Color GetAffixColor()
    {
        if (!_affixActive) return Color.white;
        return _currentAffix switch
        {
            AffixType.ShadowEmbrace => new Color(0.3f, 0f, 0.5f),
            AffixType.FrostCurse => new Color(0.4f, 0.7f, 1f),
            AffixType.FlameFury => new Color(1f, 0.3f, 0f),
            AffixType.ThunderJudgment => new Color(1f, 1f, 0.3f),
            AffixType.VoidErosion => new Color(0.5f, 0f, 0.5f),
            _ => Color.white
        };
    }

    // ═══ 持续性词缀更新 ═══

    private static void UpdateThunderJudgment()
    {
        if (Time.time - _lastThunderTime < 5f) return;
        _lastThunderTime = Time.time;

        var player = GameReferences.Player;
        if (player == null) return;

        Vector2 randomPos = (Vector2)player.transform.position + Random.insideUnitCircle * 8f;
        CombatManager.CreateExplosionEffect(randomPos, 1.5f, new Color(1f, 1f, 0.3f), 0.3f);

        int count = PhysicsHelper.OverlapCircle(randomPos, 1.5f, _overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            var hit = _overlapBuffer[i];
            if (!hit.CompareTag("Enemy") && !hit.CompareTag("Player")) continue;
            var dmg = hit.GetComponent<Damageable>();
            if (dmg != null && dmg.CurrentHp > 0)
                dmg.TakeDamage(15, new Color(1f, 1f, 0.3f));
        }
    }

    private static void UpdateVoidErosion()
    {
        if (Time.time - _lastVoidTick < 1f) return;
        _lastVoidTick = Time.time;

        var playerDmg = GameReferences.PlayerDamageable;
        if (playerDmg != null && playerDmg.CurrentHp > 0)
        {
            int voidDmg = Mathf.Max(1, Mathf.RoundToInt(playerDmg.CurrentHp * 0.01f));
            playerDmg.TakeDamage(voidDmg, new Color(0.5f, 0f, 0.5f));
        }
    }
}
