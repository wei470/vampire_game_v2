#pragma warning disable CS0414
using UnityEngine;
using System.Collections;

/// <summary>
/// 波次挑战系统 — 每 5 波（非 Boss 波前）提供可选挑战。
///
/// 功能：
/// - 挑战内容：敌人血量 ×2、速度 ×1.5、或额外 Boss、精英波
/// - 接受挑战奖励：额外金币 + 永久加成
/// - 拒绝挑战：正常进行
///
/// 使用方式：由 SpawnManager 在波次间歇期自动调用
/// </summary>
public class WaveChallengeSystem : MonoBehaviour
{
    public enum ChallengeType
    {
        DoubleHp, SpeedBoost, ExtraBoss, EliteWave,
        // ── 新增6种挑战 ──
        DarkFall,        // 黑暗降临：视野缩小
        ElementStorm,    // 元素风暴：地图出现元素区域
        MirrorEnemy,     // 镜像敌人：击杀分裂
        CurseRing,       // 诅咒之环：敌人死亡爆炸
        TimeRewind,      // 时间回溯：敌人复活
        GravityAnomaly   // 重力异常：移速波动
    }

    public class ChallengeData
    {
        public ChallengeType type;
        public string title;
        public string description;
        public string rewardText;
        public int rewardCoins;
        public float rewardBonus;
        public string rewardBonusAttr;
    }

    private bool _challengeActive = false;
    private ChallengeData _currentChallenge;
    private bool _challengeAccepted = false;
    private bool _challengeResolved = false;

    private Texture2D _bgTex;
    private Texture2D _borderTex;

    public float ChallengeHpMultiplier { get; private set; } = 1f;
    public float ChallengeSpeedMultiplier { get; private set; } = 1f;
    public bool ChallengeExtraBoss { get; private set; } = false;
    public int ChallengeEliteArmor { get; private set; } = 0;

    // ── 新增挑战效果属性 ──
    /// <summary>黑暗降临：视野缩小倍率（0.5 = 缩小50%）</summary>
    public float ChallengeViewScale { get; private set; } = 1f;
    /// <summary>元素风暴：是否激活</summary>
    public bool ChallengeElementStorm { get; private set; } = false;
    /// <summary>镜像敌人：击杀分裂概率</summary>
    public float ChallengeMirrorChance { get; private set; } = 0f;
    /// <summary>诅咒之环：敌人死亡爆炸半径</summary>
    public float ChallengeCurseExplosionRadius { get; private set; } = 0f;
    /// <summary>时间回溯：敌人复活概率</summary>
    public float ChallengeReviveChance { get; private set; } = 0f;
    /// <summary>重力异常：移速波动幅度（±0.3）</summary>
    public float ChallengeGravityVariance { get; private set; } = 0f;
    public bool HasActiveChallenge => _challengeActive && !_challengeResolved;

    public int ChallengesAccepted { get; private set; }
    public int ChallengesDeclined { get; private set; }

    public static bool ShouldOfferChallenge(int wave)
    {
        return wave > 0 && wave % 5 == 0;
    }

    public bool OfferChallenge(int wave)
    {
        if (!ShouldOfferChallenge(wave))
            return false;

        _currentChallenge = GenerateChallenge(wave);
        if (_currentChallenge == null) return false;

        _challengeActive = true;
        _challengeResolved = false;
        _challengeAccepted = false;

        DebugHelper.Log($"[WaveChallenge] Challenge offered: {_currentChallenge.title} (Wave {wave})");
        return true;
    }

    public IEnumerator WaitForChallengeResolution()
    {
        while (!_challengeResolved) yield return null;
    }

    private ChallengeData GenerateChallenge(int wave)
    {
        int seed = wave / 5;
        ChallengeType type = (ChallengeType)(seed % 10); // 10种挑战随机
        var challenge = new ChallengeData();
        challenge.rewardCoins = 100 + wave * 5;
        challenge.rewardBonus = 0.01f;
        challenge.rewardBonusAttr = "max_hp";

        switch (type)
        {
            case ChallengeType.DoubleHp:
                challenge.type = ChallengeType.DoubleHp;
                challenge.title = "\U0001fa78 坚韧试炼";
                challenge.description = "下一波所有敌人血量 x2!";
                challenge.rewardText = $"奖励: +{challenge.rewardCoins} 金币";
                break;
            case ChallengeType.SpeedBoost:
                challenge.type = ChallengeType.SpeedBoost;
                challenge.title = "\u26a1 疾风试炼";
                challenge.description = "下一波所有敌人速度 x1.5!";
                challenge.rewardText = $"奖励: +{challenge.rewardCoins} 金币";
                break;
            case ChallengeType.ExtraBoss:
                challenge.type = ChallengeType.ExtraBoss;
                challenge.title = "\U0001f479 双王试炼";
                challenge.description = "下一波将额外出现一个 Boss!";
                challenge.rewardText = $"奖励: +{challenge.rewardCoins} 金币 + +1% 伤害";
                challenge.rewardBonusAttr = "attack_damage";
                break;
            case ChallengeType.EliteWave:
                challenge.type = ChallengeType.EliteWave;
                challenge.title = "\U0001f6e1 精英试炼";
                challenge.description = "下一波所有敌人获得 +10 护甲!";
                challenge.rewardText = $"奖励: +{challenge.rewardCoins} 金币";
                break;
            case ChallengeType.DarkFall:
                challenge.type = ChallengeType.DarkFall;
                challenge.title = "\U0001f311 黑暗降临";
                challenge.description = "视野缩小 50%，你能看清敌人吗？";
                challenge.rewardText = $"奖励: +{challenge.rewardCoins * 2} 金币";
                challenge.rewardCoins *= 2;
                break;
            case ChallengeType.ElementStorm:
                challenge.type = ChallengeType.ElementStorm;
                challenge.title = "\u2728 元素风暴";
                challenge.description = "地图随机出现元素区域，DOT伤害+20%!";
                challenge.rewardText = $"奖励: +{challenge.rewardCoins} 金币 + DOT伤害+20%";
                challenge.rewardBonus = 0.2f;
                challenge.rewardBonusAttr = "dot_damage";
                break;
            case ChallengeType.MirrorEnemy:
                challenge.type = ChallengeType.MirrorEnemy;
                challenge.title = "\U0001f47e 镜像敌人";
                challenge.description = "击杀敌人 20% 概率分裂出新敌人!";
                challenge.rewardText = $"奖励: +{challenge.rewardCoins} 金币 + XP+30%";
                challenge.rewardBonus = 0.3f;
                challenge.rewardBonusAttr = "xp_gain";
                break;
            case ChallengeType.CurseRing:
                challenge.type = ChallengeType.CurseRing;
                challenge.title = "\U0001f480 诅咒之环";
                challenge.description = "敌人死亡时爆炸，对周围造成伤害!";
                challenge.rewardText = $"奖励: +{challenge.rewardCoins} 金币 + 暴击率+5%";
                challenge.rewardBonus = 0.05f;
                challenge.rewardBonusAttr = "crit_chance";
                break;
            case ChallengeType.TimeRewind:
                challenge.type = ChallengeType.TimeRewind;
                challenge.title = "\u23f0 时间回溯";
                challenge.description = "敌人死后 10% 概率复活!";
                challenge.rewardText = $"奖励: +{challenge.rewardCoins} 金币 + 技能CD-15%";
                challenge.rewardBonus = 0.15f;
                challenge.rewardBonusAttr = "skill_cooldown";
                break;
            case ChallengeType.GravityAnomaly:
                challenge.type = ChallengeType.GravityAnomaly;
                challenge.title = "\U0001f30c 重力异常";
                challenge.description = "你的移速将随机波动 ±30%!";
                challenge.rewardText = $"奖励: +{challenge.rewardCoins} 金币 + 穿透+2";
                challenge.rewardBonus = 2f;
                challenge.rewardBonusAttr = "pierce";
                break;
        }
        return challenge;
    }

    public void AcceptChallenge()
    {
        if (_challengeResolved || _currentChallenge == null) return;
        _challengeAccepted = true;
        _challengeResolved = true;
        ChallengesAccepted++;

        switch (_currentChallenge.type)
        {
            case ChallengeType.DoubleHp:
                ChallengeHpMultiplier = 2f; ChallengeSpeedMultiplier = 1f;
                ChallengeExtraBoss = false; ChallengeEliteArmor = 0; break;
            case ChallengeType.SpeedBoost:
                ChallengeHpMultiplier = 1f; ChallengeSpeedMultiplier = 1.5f;
                ChallengeExtraBoss = false; ChallengeEliteArmor = 0; break;
            case ChallengeType.ExtraBoss:
                ChallengeHpMultiplier = 1f; ChallengeSpeedMultiplier = 1f;
                ChallengeExtraBoss = true; ChallengeEliteArmor = 0; break;
            case ChallengeType.EliteWave:
                ChallengeHpMultiplier = 1f; ChallengeSpeedMultiplier = 1f;
                ChallengeExtraBoss = false; ChallengeEliteArmor = 10; break;
            case ChallengeType.DarkFall:
                ChallengeViewScale = 0.5f; break;
            case ChallengeType.ElementStorm:
                ChallengeElementStorm = true; break;
            case ChallengeType.MirrorEnemy:
                ChallengeMirrorChance = 0.2f; break;
            case ChallengeType.CurseRing:
                ChallengeCurseExplosionRadius = 3f; break;
            case ChallengeType.TimeRewind:
                ChallengeReviveChance = 0.1f; break;
            case ChallengeType.GravityAnomaly:
                ChallengeGravityVariance = 0.3f; break;
        }

        if (SaveManager.Instance != null)
            SaveManager.Instance.AddCoins(_currentChallenge.rewardCoins);

        DebugHelper.Log($"[WaveChallenge] Accepted: {_currentChallenge.title}, HPx{ChallengeHpMultiplier}");
    }

    public void DeclineChallenge()
    {
        if (_challengeResolved) return;
        _challengeAccepted = false;
        _challengeResolved = true;
        ChallengesDeclined++;
        ResetChallengeEffects();
        DebugHelper.Log("[WaveChallenge] Declined");
    }

    public void ResetChallengeEffects()
    {
        ChallengeHpMultiplier = 1f;
        ChallengeSpeedMultiplier = 1f;
        ChallengeExtraBoss = false;
        ChallengeEliteArmor = 0;
        ChallengeViewScale = 1f;
        ChallengeElementStorm = false;
        ChallengeMirrorChance = 0f;
        ChallengeCurseExplosionRadius = 0f;
        ChallengeReviveChance = 0f;
        ChallengeGravityVariance = 0f;
    }

    public void DrawChallengeUI()
    {
        if (!_challengeActive || _challengeResolved) return;
        InitTextures();

        float w = 450, h = 260;
        float x = (Screen.width - w) / 2f;
        float y = (Screen.height - h) / 2f - 50f;

        GUI.color = new Color(0, 0, 0, 0.6f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _bgTex);
        GUI.color = Color.white;

        GUI.color = new Color(0.06f, 0.08f, 0.15f, 0.97f);
        GUI.DrawTexture(new Rect(x, y, w, h), _bgTex);
        GUI.color = Color.white;
        DrawBorder(new Rect(x, y, w, h), new Color(1f, 0.85f, 0.2f, 0.6f));

        var titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 0.85f, 0.2f) } };
        GUI.Label(new Rect(x, y + 15, w, 35), "⚔️ 波次挑战 ⚔️", titleStyle);

        var nameStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 0.4f, 0.3f) } };
        GUI.Label(new Rect(x, y + 55, w, 28), _currentChallenge.title, nameStyle);

        var descStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = new Color(0.8f, 0.9f, 1f) } };
        GUI.Label(new Rect(x + 20, y + 90, w - 40, 40), _currentChallenge.description, descStyle);

        var rewardStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic, normal = { textColor = new Color(0.3f, 1f, 0.5f) } };
        GUI.Label(new Rect(x, y + 135, w, 22), "✨ " + _currentChallenge.rewardText, rewardStyle);

        float btnW = 160, btnH = 42;
        float btnY = y + h - 65;

        var acceptRect = new Rect(x + w / 2f - btnW - 15, btnY, btnW, btnH);
        GUI.color = new Color(0.1f, 0.6f, 0.2f, 0.9f);
        GUI.DrawTexture(acceptRect, _bgTex);
        GUI.color = Color.white;
        DrawBorder(acceptRect, new Color(0.3f, 1f, 0.4f, 0.7f));
        var btnStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        GUI.Label(acceptRect, "⚔️ 接受挑战", btnStyle);
        if (GUI.Button(acceptRect, "", GUIStyle.none)) AcceptChallenge();

        var declineRect = new Rect(x + w / 2f + 15, btnY, btnW, btnH);
        GUI.color = new Color(0.5f, 0.15f, 0.15f, 0.9f);
        GUI.DrawTexture(declineRect, _bgTex);
        GUI.color = Color.white;
        DrawBorder(declineRect, new Color(0.8f, 0.3f, 0.3f, 0.7f));
        GUI.Label(declineRect, "🏠 安全通过", btnStyle);
        if (GUI.Button(declineRect, "", GUIStyle.none)) DeclineChallenge();
    }

    private void InitTextures()
    {
        if (_bgTex == null) { _bgTex = new Texture2D(1, 1); _bgTex.SetPixel(0, 0, Color.white); _bgTex.Apply(); }
        if (_borderTex == null) { _borderTex = new Texture2D(1, 1); _borderTex.SetPixel(0, 0, Color.white); _borderTex.Apply(); }
    }

    private void DrawBorder(Rect r, Color c)
    {
        GUI.color = c;
        float t = 2f;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), _borderTex);
        GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), _borderTex);
        GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), _borderTex);
        GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), _borderTex);
        GUI.color = Color.white;
    }
}