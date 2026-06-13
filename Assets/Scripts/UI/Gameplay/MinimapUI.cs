#pragma warning disable CS0414
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 小地图 UI — 右上角显示的缩略地图。
/// 
/// 功能：
/// - 显示玩家位置（中心点）
/// - 显示附近敌人（红点）
/// - 显示掉落物（绿/黄点）
/// - 显示地图边界
/// - 可缩放
/// 
/// 使用方式：由 GameSceneBootstrap 在 OnGUI 中渲染
/// </summary>
public class MinimapUI : MonoBehaviour
{
    [Header("小地图设置")]
    [SerializeField] private float _size = 180f;
    [SerializeField] private float _worldRange = 60f;
    [SerializeField] private float _margin = 10f;
    [SerializeField] private bool _enabled = true;
    [SerializeField] private float _minRange = 30f;   // #26 最小缩放
    [SerializeField] private float _maxRange = 120f;   // #26 最大缩放

    // #26 Boss 闪烁 + 缩放 + 生成方向
    private float _bossFlashTimer;
    private Color _bossFlashColor;
    private float _zoomSpeed = 10f;

    [Header("颜色")]
    [SerializeField] private Color _bgColor = new Color(0.004f, 0.137f, 0.149f, 0.8f);  // #012326
    [SerializeField] private Color _playerColor = new Color(0.02f, 0.949f, 0.859f);      // #05F2DB 荧光青
    [SerializeField] private Color _enemyColor = new Color(0.851f, 0.016f, 0.557f);      // #D9048E 洋红
    [SerializeField] private Color _lootColor = new Color(0.02f, 0.949f, 0.6f);          // 荧光青变体
    [SerializeField] private Color _borderColor = new Color(0.008f, 0.325f, 0.451f);     // #025373 边框

    private Texture2D _dotTexture;
    private Texture2D _bgTexture;
    private GUIStyle _labelStyle;
    private bool _stylesInit;

    private void Awake()
    {
        _dotTexture = new Texture2D(1, 1);
        _dotTexture.SetPixel(0, 0, Color.white);
        _dotTexture.Apply();

        _bgTexture = new Texture2D(1, 1);
        _bgTexture.SetPixel(0, 0, Color.white);
        _bgTexture.Apply();
    }

    private void OnDestroy()
    {
        if (_dotTexture != null) Destroy(_dotTexture);
        if (_bgTexture != null) Destroy(_bgTexture);
    }

    public void DrawMinimap()
    {
        if (!_enabled) return;
        EnsureStyles();

        var player = GameReferences.Player;
        if (player == null) return;

        Vector3 playerPos = player.transform.position;

        float mapX = Screen.width - _size - _margin;
        float mapY = _margin;
        Rect mapRect = new Rect(mapX, mapY, _size, _size);

        // ── 背景 ──
        GUI.color = _bgColor;
        GUI.DrawTexture(mapRect, _bgTexture);
        GUI.color = Color.white;

        // ── 边框 ──
        DrawBorder(mapRect, _borderColor);

        GUI.BeginGroup(mapRect);

        float center = _size / 2f;
        float scale = _size / (_worldRange * 2f);

        DrawMapBounds(center, scale, playerPos);
        DrawEnvironmentZones(center, scale, playerPos);
        DrawLootDots(center, scale, playerPos);
        DrawEnemyDots(center, scale, playerPos);

        // ── 玩家（荧光青色大点）──
        DrawDot(center - 3, center - 3, 6, _playerColor);

        GUI.EndGroup();

        // ── 标签 ──
        GUI.color = UIColorTheme.TextSecondary;
        GUI.Label(new Rect(mapX, mapY + _size + 2, _size, 15), "MINIMAP", _labelStyle);
        GUI.color = Color.white;
    }

    /// <summary>
    /// #26 Boss 出现时小地图上高亮闪烁标记
    /// </summary>
    public void NotifyBossSpawned()
    {
        _bossFlashTimer = 3f; // 闪烁 3 秒
    }

    private void EnsureStyles()
    {
        if (_stylesInit) return;
        _stylesInit = true;

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
    }

    /// <summary>
    /// #26 鼠标滚轮缩放小地图范围
    /// </summary>
    public void HandleZoom(float scrollDelta)
    {
        _worldRange = Mathf.Clamp(_worldRange - scrollDelta * _zoomSpeed, _minRange, _maxRange);
    }

    private void DrawEnemyDots(float center, float scale, Vector3 playerPos)
    {
        // #26 Boss 闪烁更新
        _bossFlashTimer -= Time.unscaledDeltaTime;
        bool bossFlash = _bossFlashTimer > 0f && Mathf.Sin(Time.unscaledTime * 12f) > 0f;

        // 优化：使用 SpawnManager.ActiveEnemies 代替 FindObjectsByType
        // 合并精英绘制到单次遍历，限制最大显示数量
        var spawnMgr = GameReferences.SpawnManager;
        IReadOnlyList<UnityEngine.GameObject> activeEnemies = spawnMgr?.ActiveEnemies;
        if (activeEnemies == null) return;

        int drawCount = 0;
        const int MAX_DOTS = 40; // 小地图最多显示40个敌人点

        for (int i = 0; i < activeEnemies.Count && drawCount < MAX_DOTS; i++)
        {
            var enemyGo = activeEnemies[i];
            if (enemyGo == null || !enemyGo.activeInHierarchy) continue;

            Vector3 offset = enemyGo.transform.position - playerPos;
            float dist = offset.magnitude;
            if (dist > _worldRange) continue;

            float px = center + offset.x * scale;
            float py = center - offset.y * scale;

            // 检查精英（合并 DrawEliteDots 逻辑）
            var elite = enemyGo.GetComponent<EliteModifierSystem>();
            bool isElite = elite != null && elite.IsElite;

            // 检查 Boss
            bool isBoss = enemyGo.GetComponent<BossEnemy>() != null;

            if (isBoss)
            {
                Color dotColor = bossFlash
                    ? new Color(1f, 0.2f, 0.2f, 1f)
                    : UIColorTheme.AccentPink;
                float bossSize = bossFlash ? 8f : 6f;
                DrawDot(px - bossSize / 2, py - bossSize / 2, bossSize, dotColor);
                DrawDot(px - 4, py - 8, 2, new Color(1f, 0.85f, 0.2f, 0.8f)); // 金色顶部标记
            }
            else if (isElite)
            {
                // 精英：橙色菱形
                DrawDot(px - 4, py - 4, 8, new Color(1f, 0.5f, 0f, 0.3f)); // 光晕
                DrawDot(px - 3, py - 3, 6, new Color(1f, 0.6f, 0.1f));     // 精英点
            }
            else
            {
                // 普通敌人
                DrawDot(px - 1.5f, py - 1.5f, 3, _enemyColor);
            }

            drawCount++;
        }
    }

    private void DrawLootDots(float center, float scale, Vector3 playerPos)
    {
        var gems = FindObjectsByType<XPGem>();
        foreach (var gem in gems)
        {
            if (gem == null) continue;
            Vector3 offset = gem.transform.position - playerPos;
            if (offset.magnitude > _worldRange) continue;

            float px = center + offset.x * scale;
            float py = center - offset.y * scale;
            DrawDot(px - 1, py - 1, 2, _lootColor);
        }

        var coins = FindObjectsByType<Coin>();
        foreach (var coin in coins)
        {
            if (coin == null) continue;
            Vector3 offset = coin.transform.position - playerPos;
            if (offset.magnitude > _worldRange) continue;

            float px = center + offset.x * scale;
            float py = center - offset.y * scale;
            DrawDot(px - 1, py - 1, 2, UIColorTheme.GoldText);
        }
    }

    // DrawEliteDots 已合并到 DrawEnemyDots 中（单次遍历）

    /// <summary>
    /// 绘制环境区域（半透明圆圈）
    /// </summary>
    private void DrawEnvironmentZones(float center, float scale, Vector3 playerPos)
    {
        var zones = FindObjectsByType<EnvironmentZone>();
        foreach (var zone in zones)
        {
            if (zone == null) continue;
            Vector3 offset = zone.transform.position - playerPos;
            if (offset.magnitude > _worldRange) continue;

            float px = center + offset.x * scale;
            float py = center - offset.y * scale;
            float radius = 3f; // 在小地图上显示3像素半径

            // 根据区域类型着色
            Color zoneColor = new Color(0.3f, 0.7f, 0.3f, 0.25f); // 默认绿色
            string zoneName = zone.gameObject.name.ToLower();
            if (zoneName.Contains("poison") || zoneName.Contains("toxic"))
                zoneColor = new Color(0.2f, 0.8f, 0.2f, 0.25f);
            else if (zoneName.Contains("fire") || zoneName.Contains("lava"))
                zoneColor = new Color(0.9f, 0.3f, 0.1f, 0.25f);
            else if (zoneName.Contains("ice") || zoneName.Contains("frost"))
                zoneColor = new Color(0.3f, 0.6f, 1f, 0.25f);
            else if (zoneName.Contains("lightning") || zoneName.Contains("thunder"))
                zoneColor = new Color(0.8f, 0.8f, 0.2f, 0.25f);
            else if (zoneName.Contains("holy") || zoneName.Contains("light"))
                zoneColor = new Color(1f, 1f, 0.8f, 0.25f);

            DrawDot(px - radius, py - radius, radius * 2, zoneColor);
        }
    }

    private void DrawMapBounds(float center, float scale, Vector3 playerPos)
    {
        float mapHalfSize = 50f;
        float left = center + (-mapHalfSize - playerPos.x) * scale;
        float right = center + (mapHalfSize - playerPos.x) * scale;
        float top = center - (mapHalfSize - playerPos.y) * scale;
        float bottom = center - (-mapHalfSize - playerPos.y) * scale;

        GUI.color = new Color(_borderColor.r, _borderColor.g, _borderColor.b, 0.3f);
        GUI.DrawTexture(new Rect(left, top, right - left, 1), _dotTexture);
        GUI.DrawTexture(new Rect(left, bottom, right - left, 1), _dotTexture);
        GUI.DrawTexture(new Rect(left, top, 1, bottom - top), _dotTexture);
        GUI.DrawTexture(new Rect(right, top, 1, bottom - top), _dotTexture);
        GUI.color = Color.white;
    }

    private void DrawDot(float x, float y, float size, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(new Rect(x, y, size, size), _dotTexture);
    }

    private void DrawBorder(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1), _dotTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1, rect.width, 1), _dotTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, 1, rect.height), _dotTexture);
        GUI.DrawTexture(new Rect(rect.xMax - 1, rect.y, 1, rect.height), _dotTexture);
        GUI.color = Color.white;
    }

    public void Toggle()
    {
        _enabled = !_enabled;
    }

    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
    }
}