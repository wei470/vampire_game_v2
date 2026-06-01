using UnityEngine;

public class WaveRewardUI : MonoBehaviour
{
    private enum RewardType { Heal, Attack, Speed, Cooldown, MaxHP, Coins, XPBonus }
    private struct RewardOption { public RewardType Type; public string Label; public float Value; }

    private bool _showing = false;
    private RewardOption[] _currentOptions = new RewardOption[3];
    private GUIStyle _panelStyle;
    private GUIStyle _btnStyle;
    private GUIStyle _titleStyle;

    private PlayerController _player;
    private WeaponController _weapon;
    private Texture2D _rewardTex1;
    private Texture2D _rewardTex2;
    private Texture2D _rewardTex3;

    private void OnEnable() { EventManager.OnWaveComplete += OnWaveComplete; }
    private void OnDisable() { EventManager.OnWaveComplete -= OnWaveComplete; }
    private void Start() { _player = GameReferences.Player; _weapon = _player?.GetComponent<WeaponController>(); InitRewardTextures(); }

    private void OnWaveComplete(int wave)
    {
        if (wave % 5 == 0 || wave % 2 != 0) return;
        GenerateRewardOptions(wave);
        _showing = true;
        Time.timeScale = 0f;
    }

    private void GenerateRewardOptions(int wave)
    {
        int tier = wave <= 5 ? 0 : (wave <= 12 ? 1 : 2);
        float[] tierValues = { 1f, 2f, 3f };
        var allTypes = new RewardType[] { RewardType.Heal, RewardType.Attack, RewardType.Speed, RewardType.Cooldown, RewardType.MaxHP, RewardType.Coins, RewardType.XPBonus };
        for (int i = allTypes.Length - 1; i > 0; i--) { int j = Random.Range(0, i + 1); var t = allTypes[i]; allTypes[i] = allTypes[j]; allTypes[j] = t; }
        for (int i = 0; i < 3; i++) _currentOptions[i] = CreateReward(allTypes[i], tierValues[tier]);
    }

    private RewardOption CreateReward(RewardType type, float mult) => type switch
    {
        RewardType.Heal => new RewardOption { Type = type, Label = $"HP 恢复 {10 * mult}%", Value = 10f * mult },
        RewardType.Attack => new RewardOption { Type = type, Label = $"攻击力 +{5 * mult}%", Value = 5f * mult },
        RewardType.Speed => new RewardOption { Type = type, Label = $"移速 +{5 * mult}%", Value = 5f * mult },
        RewardType.Cooldown => new RewardOption { Type = type, Label = $"冷却缩减 -{5 * mult}%", Value = 5f * mult },
        RewardType.MaxHP => new RewardOption { Type = type, Label = $"最大 HP +{10 * mult}", Value = 10f * mult },
        RewardType.Coins => new RewardOption { Type = type, Label = $"金币 +{20 * mult}", Value = 20f * mult },
        RewardType.XPBonus => new RewardOption { Type = type, Label = $"经验加成 +{10 * mult}%", Value = 10f * mult },
        _ => new RewardOption { Type = type, Label = "未知", Value = 0 }
    };

    private void ApplyReward(RewardOption option)
    {
        var dmg = _player?.Damageable;
        switch (option.Type)
        {
            case RewardType.Heal: if (dmg != null) dmg.Heal(Mathf.RoundToInt(dmg.MaxHp * option.Value / 100f)); break;
            case RewardType.Attack: if (_weapon != null) _weapon.DamageMultiplier += option.Value / 100f; break;
            case RewardType.Speed: if (_player != null) _player.MoveSpeed *= (1f + option.Value / 100f); break;
            case RewardType.Cooldown: if (_weapon != null) _weapon.CooldownMultiplier *= (1f - option.Value / 100f); break;
            case RewardType.MaxHP: if (dmg != null) { int n = dmg.MaxHp + Mathf.RoundToInt(option.Value); dmg.SetMaxHp(n); dmg.Heal(Mathf.RoundToInt(option.Value)); } break;
            case RewardType.Coins: EventManager.TriggerCoinChanged(Mathf.RoundToInt(option.Value)); break;
            case RewardType.XPBonus: EventManager.TriggerXPGained(Mathf.RoundToInt(option.Value * 10)); break;
        }
    }

    private void InitRewardTextures() { _rewardTex1 = UIColorTheme.MakeTexture(UIColorTheme.AccentCyan); _rewardTex2 = UIColorTheme.MakeTexture(UIColorTheme.AccentMagenta); _rewardTex3 = UIColorTheme.MakeTexture(UIColorTheme.AccentPink); }

    private void OnGUI()
    {
        if (!_showing) return;
        GUIScaleHelper.BeginScale();
        InitStyles();

        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", _panelStyle);
        float panelW = 600f, panelH = 350f;
        float panelX = (Screen.width - panelW) / 2f, panelY = (Screen.height - panelH) / 2f;
        GUI.Label(new Rect(panelX, panelY, panelW, 50), "波次完成！选择奖励", _titleStyle);

        float btnW = 160f, btnH = 120f;
        float startX = panelX + (panelW - btnW * 3 - 30f) / 2f;
        Texture2D[] texes = { _rewardTex1, _rewardTex2, _rewardTex3 };

        for (int i = 0; i < 3; i++)
        {
            float x = startX + i * (btnW + 15f), y = panelY + 80f;
            _btnStyle.normal.background = texes[i];
            Rect btnRect = new Rect(x, y, btnW, btnH);
            if (btnRect.Contains(Event.current.mousePosition)) UIColorTheme.DrawButtonGlow(btnRect);
            if (GUI.Button(btnRect, _currentOptions[i].Label, _btnStyle)) { ApplyReward(_currentOptions[i]); _showing = false; Time.timeScale = 1f; }
        }
        GUIScaleHelper.EndScale();
    }

    private void InitStyles()
    {
        if (_panelStyle != null) return;
        _panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = MakeTex(1, 1, UIColorTheme.OverlayDark) } };
        _titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 32, fontStyle = FontStyle.Bold, normal = { textColor = UIColorTheme.AccentCyan } };
        _btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, wordWrap = true, normal = { textColor = UIColorTheme.TextPrimary, background = UIColorTheme.MakeTexture(UIColorTheme.AccentCyan) }, hover = { textColor = UIColorTheme.AccentCyan, background = UIColorTheme.MakeTexture(UIColorTheme.ButtonHover) } };
    }

    private Texture2D MakeTex(int w, int h, Color c) { var t = new Texture2D(w, h); for (int i = 0; i < w * h; i++) t.SetPixel(i % w, i / w, c); t.Apply(); return t; }
}