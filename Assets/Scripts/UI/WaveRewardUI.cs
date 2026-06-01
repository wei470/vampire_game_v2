using UnityEngine;

/// <summary>
/// 波次间奖励 UI — 每波完成后弹出 3 个随机奖励供玩家选择。
/// 
/// 奖励类型：
/// - HP 恢复（10%/20%/30%）
/// - 攻击力提升（+5%/+10%/+15%）
/// - 移速提升（+5%/+10%/+15%）
/// - 冷却缩减（-5%/-10%/-15%）
/// - 最大 HP 增加（+10/+20/+30）
/// - 金币奖励（+20/+40/+60）
/// - XP 加成（+10%/+20%/+30%）
/// 
/// 使用方式：挂载到场景中的 GameObject
/// </summary>
public class WaveRewardUI : MonoBehaviour
{
    private enum RewardType { Heal, Attack, Speed, Cooldown, MaxHP, Coins, XPBonus }

    private struct RewardOption
    {
        public RewardType Type;
        public string Label;
        public float Value;
    }

    private bool _showing = false;
    private RewardOption[] _currentOptions = new RewardOption[3];
    private GUIStyle _panelStyle;
    private GUIStyle _btnStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _descStyle;

    private PlayerController _player;
    private WeaponController _weapon;
    private PlayerLevelSystem _levelSystem;

    private void OnEnable()
    {
        EventManager.OnWaveComplete += OnWaveComplete;
    }

    private void OnDisable()
    {
        EventManager.OnWaveComplete -= OnWaveComplete;
    }

    private void Start()
    {
        _player = GameReferences.Player;
        _weapon = _player?.GetComponent<WeaponController>();
        _levelSystem = _player?.GetComponent<PlayerLevelSystem>();
    }

    private void OnWaveComplete(int wave)
    {
        // Boss 波（每 5 波）不弹奖励（Boss 已有掉落）
        if (wave % 5 == 0) return;

        // 每 2 波弹一次奖励
        if (wave % 2 != 0) return;

        GenerateRewardOptions(wave);
        _showing = true;

        // 暂停游戏
        Time.timeScale = 0f;
    }

    private void GenerateRewardOptions(int wave)
    {
        // 奖励等级随波次增加
        int tier = wave <= 5 ? 0 : (wave <= 12 ? 1 : 2);
        float[] tierValues = { 1f, 2f, 3f };

        // 随机选择 3 个不同奖励
        var allTypes = new RewardType[] {
            RewardType.Heal, RewardType.Attack, RewardType.Speed,
            RewardType.Cooldown, RewardType.MaxHP, RewardType.Coins, RewardType.XPBonus
        };

        // Fisher-Yates 洗牌
        for (int i = allTypes.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = allTypes[i];
            allTypes[i] = allTypes[j];
            allTypes[j] = temp;
        }

        for (int i = 0; i < 3; i++)
        {
            var type = allTypes[i];
            float mult = tierValues[tier];
            _currentOptions[i] = CreateReward(type, mult);
        }
    }

    private RewardOption CreateReward(RewardType type, float mult)
    {
        switch (type)
        {
            case RewardType.Heal:
                return new RewardOption { Type = type, Label = $"HP 恢复 {10 * mult}%", Value = 10f * mult };
            case RewardType.Attack:
                return new RewardOption { Type = type, Label = $"攻击力 +{5 * mult}%", Value = 5f * mult };
            case RewardType.Speed:
                return new RewardOption { Type = type, Label = $"移速 +{5 * mult}%", Value = 5f * mult };
            case RewardType.Cooldown:
                return new RewardOption { Type = type, Label = $"冷却缩减 -{5 * mult}%", Value = 5f * mult };
            case RewardType.MaxHP:
                return new RewardOption { Type = type, Label = $"最大 HP +{10 * mult}", Value = 10f * mult };
            case RewardType.Coins:
                return new RewardOption { Type = type, Label = $"金币 +{20 * mult}", Value = 20f * mult };
            case RewardType.XPBonus:
                return new RewardOption { Type = type, Label = $"经验加成 +{10 * mult}%", Value = 10f * mult };
            default:
                return new RewardOption { Type = type, Label = "未知", Value = 0 };
        }
    }

    private void ApplyReward(RewardOption option)
    {
        var dmg = _player?.Damageable;
        switch (option.Type)
        {
            case RewardType.Heal:
                if (dmg != null)
                {
                    int healAmount = Mathf.RoundToInt(dmg.MaxHp * option.Value / 100f);
                    dmg.Heal(healAmount);
                }
                break;
            case RewardType.Attack:
                if (_weapon != null)
                    _weapon.DamageMultiplier += option.Value / 100f;
                break;
            case RewardType.Speed:
                if (_player != null)
                    _player.MoveSpeed *= (1f + option.Value / 100f);
                break;
            case RewardType.Cooldown:
                if (_weapon != null)
                    _weapon.CooldownMultiplier *= (1f - option.Value / 100f);
                break;
            case RewardType.MaxHP:
                if (dmg != null)
                {
                    int newMax = dmg.MaxHp + Mathf.RoundToInt(option.Value);
                    dmg.SetMaxHp(newMax);
                    dmg.Heal(Mathf.RoundToInt(option.Value)); // 同时回复增加的部分
                }
                break;
            case RewardType.Coins:
                // 通过 EventManager 广播金币获取
                EventManager.TriggerCoinChanged(Mathf.RoundToInt(option.Value));
                break;
            case RewardType.XPBonus:
                // 直接给予经验
                EventManager.TriggerXPGained(Mathf.RoundToInt(option.Value * 10));
                break;
        }

        DebugHelper.Log($"[WaveRewardUI] Applied reward: {option.Label}");
    }

    private void OnGUI()
    {
        if (!_showing) return;

        InitStyles();

        // 半透明背景
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", _panelStyle);

        float panelW = 600f;
        float panelH = 350f;
        float panelX = (Screen.width - panelW) / 2f;
        float panelY = (Screen.height - panelH) / 2f;

        // 标题
        GUI.Label(new Rect(panelX, panelY, panelW, 50), "波次完成！选择奖励", _titleStyle);

        // 3 个选项按钮
        float btnW = 160f;
        float btnH = 120f;
        float startX = panelX + (panelW - btnW * 3 - 30f) / 2f;

        for (int i = 0; i < 3; i++)
        {
            float x = startX + i * (btnW + 15f);
            float y = panelY + 80f;

            if (GUI.Button(new Rect(x, y, btnW, btnH), _currentOptions[i].Label, _btnStyle))
            {
                ApplyReward(_currentOptions[i]);
                _showing = false;
                Time.timeScale = 1f;
            }
        }
    }

    private void InitStyles()
    {
        if (_panelStyle != null) return;

        _panelStyle = new GUIStyle(GUI.skin.box);
        _panelStyle.normal.background = MakeTex(1, 1, new Color(0, 0, 0, 0.7f));

        _titleStyle = new GUIStyle(GUI.skin.label);
        _titleStyle.alignment = TextAnchor.MiddleCenter;
        _titleStyle.fontSize = 28;
        _titleStyle.fontStyle = FontStyle.Bold;
        _titleStyle.normal.textColor = Color.yellow;

        _btnStyle = new GUIStyle(GUI.skin.button);
        _btnStyle.fontSize = 16;
        _btnStyle.wordWrap = true;
        _btnStyle.normal.textColor = Color.white;
        _btnStyle.normal.background = MakeTex(1, 1, new Color(0.2f, 0.3f, 0.5f, 0.9f));
        _btnStyle.hover.background = MakeTex(1, 1, new Color(0.3f, 0.5f, 0.7f, 0.95f));
    }

    private Texture2D MakeTex(int width, int height, Color color)
    {
        var tex = new Texture2D(width, height);
        var pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}