using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 管理器，显示波次、等级、金币等信息。
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("Info Text")]
    [SerializeField] private Text _levelText;
    [SerializeField] private Text _waveText;
    [SerializeField] private Text _coinText;

    private PlayerLevelSystem _levelSystem;
    private SpawnManager _spawnManager;

    private void Start()
    {
        _levelSystem = GameReferences.Player?.GetComponent<PlayerLevelSystem>();
        _spawnManager = GameReferences.SpawnManager;

        // 订阅事件
        EventManager.OnLevelUp += OnLevelUp;
        EventManager.OnWaveStart += OnWaveStart;
        EventManager.OnCoinChanged += OnCoinChanged;

        UpdateAllHUD();
    }

    private void OnDestroy()
    {
        EventManager.OnLevelUp -= OnLevelUp;
        EventManager.OnWaveStart -= OnWaveStart;
        EventManager.OnCoinChanged -= OnCoinChanged;
    }

    private void OnLevelUp(int level) { UpdateLevelText(); }
    private void OnWaveStart(int wave) { UpdateWaveText(); }
    private void OnCoinChanged(int coins) { UpdateCoinText(); }

    private void UpdateLevelText()
    {
        if (_levelText != null && _levelSystem != null)
        {
            _levelText.text = $"Lv.{_levelSystem.Level}";
        }
    }

    private void UpdateWaveText()
    {
        if (_waveText != null && _spawnManager != null)
        {
            _waveText.text = $"Wave {_spawnManager.CurrentWave}";
        }
    }

    private void UpdateCoinText()
    {
        if (_coinText != null)
        {
            _coinText.text = $"Coins: {Coin.TotalCoins}";
        }
    }

    private void UpdateAllHUD()
    {
        UpdateLevelText();
        UpdateWaveText();
        UpdateCoinText();
    }
}
