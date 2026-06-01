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
    [SerializeField] private float _size = 180f;             // 小地图像素大小
    [SerializeField] private float _worldRange = 60f;        // 显示的世界范围半径
    [SerializeField] private float _margin = 10f;            // 屏幕边距
    [SerializeField] private bool _enabled = true;           // 是否启用

    [Header("颜色")]
    [SerializeField] private Color _bgColor = new Color(0.1f, 0.1f, 0.2f, 0.8f);
    [SerializeField] private Color _playerColor = new Color(0.3f, 0.8f, 1f);
    [SerializeField] private Color _enemyColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color _lootColor = new Color(0.5f, 1f, 0.5f);
    [SerializeField] private Color _borderColor = new Color(0.4f, 0.4f, 0.6f);

    // 缓存的 Texture2D 用于绘制
    private Texture2D _dotTexture;
    private Texture2D _bgTexture;

    private void Awake()
    {
        // 创建 1x1 像素纹理用于绘制点
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

    /// <summary>
    /// 渲染小地图（在 OnGUI 中调用）
    /// </summary>
    public void DrawMinimap()
    {
        if (!_enabled) return;

        var player = GameReferences.Player;
        if (player == null) return;

        Vector3 playerPos = player.transform.position;

        // 小地图区域（右上角）
        float mapX = Screen.width - _size - _margin;
        float mapY = _margin;
        Rect mapRect = new Rect(mapX, mapY, _size, _size);

        // ── 背景 ──
        GUI.color = _bgColor;
        GUI.DrawTexture(mapRect, _bgTexture);
        GUI.color = Color.white;

        // ── 边框 ──
        DrawBorder(mapRect, _borderColor);

        // 开始裁剪区域
        GUI.BeginGroup(mapRect);

        float center = _size / 2f;
        float scale = _size / (_worldRange * 2f);

        // ── 地图边界线 ──
        DrawMapBounds(center, scale, playerPos);

        // ── 掉落物（绿色/黄色小点）──
        DrawLootDots(center, scale, playerPos);

        // ── 敌人（红色小点）──
        DrawEnemyDots(center, scale, playerPos);

        // ── 玩家（中心蓝色大点）──
        DrawDot(center - 3, center - 3, 6, _playerColor);

        GUI.EndGroup();

        // ── 标签 ──
        GUI.color = new Color(0.7f, 0.7f, 0.8f);
        var labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Label(new Rect(mapX, mapY + _size + 2, _size, 15), "MINIMAP", labelStyle);
        GUI.color = Color.white;
    }

    /// <summary>
    /// 绘制敌人点
    /// </summary>
    private void DrawEnemyDots(float center, float scale, Vector3 playerPos)
    {
        var enemies = FindObjectsByType<EnemyBase>();
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            Vector3 offset = enemy.transform.position - playerPos;
            float dist = offset.magnitude;
            if (dist > _worldRange) continue;

            float px = center + offset.x * scale;
            float py = center - offset.y * scale; // Y 轴翻转

            // Boss 用更大的点
            float dotSize = (enemy is BossEnemy) ? 5f : 3f;
            Color dotColor = (enemy is BossEnemy) ? new Color(1f, 0f, 0f) : _enemyColor;
            DrawDot(px - dotSize / 2, py - dotSize / 2, dotSize, dotColor);
        }
    }

    /// <summary>
    /// 绘制掉落物点
    /// </summary>
    private void DrawLootDots(float center, float scale, Vector3 playerPos)
    {
        // 经验宝石
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

        // 金币
        var coins = FindObjectsByType<Coin>();
        foreach (var coin in coins)
        {
            if (coin == null) continue;
            Vector3 offset = coin.transform.position - playerPos;
            if (offset.magnitude > _worldRange) continue;

            float px = center + offset.x * scale;
            float py = center - offset.y * scale;
            DrawDot(px - 1, py - 1, 2, new Color(1f, 0.85f, 0f));
        }
    }

    /// <summary>
    /// 绘制地图边界
    /// </summary>
    private void DrawMapBounds(float center, float scale, Vector3 playerPos)
    {
        // 假设地图边界为 ±50 单位
        float mapHalfSize = 50f;
        float left = center + (-mapHalfSize - playerPos.x) * scale;
        float right = center + (mapHalfSize - playerPos.x) * scale;
        float top = center - (mapHalfSize - playerPos.y) * scale;
        float bottom = center - (-mapHalfSize - playerPos.y) * scale;

        GUI.color = new Color(_borderColor.r, _borderColor.g, _borderColor.b, 0.3f);
        // 上边
        GUI.DrawTexture(new Rect(left, top, right - left, 1), _dotTexture);
        // 下边
        GUI.DrawTexture(new Rect(left, bottom, right - left, 1), _dotTexture);
        // 左边
        GUI.DrawTexture(new Rect(left, top, 1, bottom - top), _dotTexture);
        // 右边
        GUI.DrawTexture(new Rect(right, top, 1, bottom - top), _dotTexture);
        GUI.color = Color.white;
    }

    /// <summary>
    /// 绘制单个点
    /// </summary>
    private void DrawDot(float x, float y, float size, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(new Rect(x, y, size, size), _dotTexture);
    }

    /// <summary>
    /// 绘制边框
    /// </summary>
    private void DrawBorder(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1), _dotTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1, rect.width, 1), _dotTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, 1, rect.height), _dotTexture);
        GUI.DrawTexture(new Rect(rect.xMax - 1, rect.y, 1, rect.height), _dotTexture);
        GUI.color = Color.white;
    }

    /// <summary>
    /// 切换小地图显示
    /// </summary>
    public void Toggle()
    {
        _enabled = !_enabled;
    }

    /// <summary>
    /// 设置是否启用
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
    }
}