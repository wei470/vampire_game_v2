using UnityEngine;

/// <summary>
/// 调试覆盖层 — 显示游戏运行时状态面板（右上角）。
/// 
/// 从 GameSceneBootstrap 中拆分而来，独立负责调试信息的渲染。
/// 仅在 _showDebugGUI 为 true 且选择完成后显示。
/// </summary>
public class DebugOverlay : MonoBehaviour
{
    [Header("显示控制")]
    [SerializeField] private bool _showDebugGUI = true;

    [Header("运行时状态（只读）")]
    [SerializeField] private int _totalEnemiesKilled = 0;
    [SerializeField] private int _currentWave = 0;

    private PlayerController _player;
    private SpawnManager _spawnManager;

    /// <summary>
    /// 总击杀数
    /// </summary>
    public int TotalEnemiesKilled => _totalEnemiesKilled;

    /// <summary>
    /// 是否显示调试 GUI
    /// </summary>
    public bool ShowDebugGUI { get => _showDebugGUI; set => _showDebugGUI = value; }

    /// <summary>
    /// 初始化引用
    /// </summary>
    public void Setup(PlayerController player, SpawnManager spawnManager)
    {
        _player = player;
        _spawnManager = spawnManager;
    }

    private void OnEnable()
    {
        EventManager.OnEnemyKilled += OnEnemyKilled;
        EventManager.OnWaveStart += OnWaveStart;
    }

    private void OnDisable()
    {
        EventManager.OnEnemyKilled -= OnEnemyKilled;
        EventManager.OnWaveStart -= OnWaveStart;
    }

    private void OnEnemyKilled(Vector3 deathPosition, int xpReward, int coinReward)
    {
        _totalEnemiesKilled++;
    }

    private void OnWaveStart(int wave)
    {
        _currentWave = wave;
    }

    /// <summary>
    /// 渲染调试面板（在 OnGUI 中调用）
    /// </summary>
    public void DrawDebugGUI()
    {
        if (!_showDebugGUI) return;

        var spawnMgr = _spawnManager;
        int currentWave = spawnMgr != null ? spawnMgr.CurrentWave : _currentWave;
        int enemiesAlive = spawnMgr != null ? spawnMgr.EnemiesAlive : 0;
        bool waveInProgress = spawnMgr != null ? spawnMgr.WaveInProgress : false;

        // 右上角状态面板
        GUI.skin.label.fontSize = 18;
        GUILayout.BeginArea(new Rect(Screen.width - 350, 10, 340, 400));

        GUI.color = Color.cyan;
        GUILayout.Label("═══ Game Scene Status ═══");

        if (_player != null)
        {
            var dmg = _player.Damageable;
            if (dmg != null)
            {
                GUI.color = dmg.HpPercent > 0.5f ? Color.green : (dmg.HpPercent > 0.2f ? Color.yellow : Color.red);
                GUILayout.Label($"HP: {dmg.CurrentHp}/{dmg.MaxHp} ({dmg.HpPercent:P0})");
                GUILayout.Label($"Armor: {dmg.Armor}");
            }

            var lvlSys = _player.GetComponent<PlayerLevelSystem>();
            if (lvlSys != null)
            {
                GUI.color = Color.yellow;
                GUILayout.Label($"Level: {lvlSys.Level}  XP: {lvlSys.CurrentExp}/{lvlSys.ExpToLevelUp}");
            }

            // 玩家位置
            GUI.color = new Color(0.7f, 0.9f, 1f);
            var pos = _player.transform.position;
            GUILayout.Label($"Pos: ({pos.x:F1}, {pos.y:F1})");
        }

        // ── Combat 区域 ──
        GUI.color = Color.white;
        GUILayout.Label("── Combat ──");
        GUILayout.Label($"Wave: {currentWave}  Kills: {_totalEnemiesKilled}");
        GUILayout.Label($"Enemies Alive: {enemiesAlive}");
        GUILayout.Label($"Wave In Progress: {waveInProgress}");

        // Boss 波提示
        if (currentWave > 0 && currentWave % 5 == 0 && waveInProgress)
        {
            GUI.color = new Color(1f, 0.2f, 0.2f);
            GUILayout.Label("⚠ BOSS WAVE! ⚠");
        }

        GUI.color = Color.gray;
        GUILayout.Label("WASD=Move Mouse=Aim E=Detonate R=Restart ESC=Pause");

        GUILayout.EndArea();
    }
}