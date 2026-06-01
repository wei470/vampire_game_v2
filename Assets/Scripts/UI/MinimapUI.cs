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

    [Header("颜色")]
    [SerializeField] private Color _bgColor = new Color(0.004f, 0.137f, 0.149f, 0.8f);  // #012326
    [SerializeField] private Color _playerColor = new Color(0.02f, 0.949f, 0.859f);      // #05F2DB 荧光青
    [SerializeField] private Color _enemyColor = new Color(0.851f, 0.016f, 0.557f);      // #D9048E 洋红
    [SerializeField] private Color _lootColor = new Color(0.02f, 0.949f, 0.6f);          // 荧光青变体
    [SerializeField] private Color _borderColor = new Color(0.008f, 0.325f, 0.451f);     // #025373 边框

    private Texture2D _dotTexture;
    private Texture2D _bgTexture;

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
        DrawLootDots(center, scale, playerPos);
        DrawEnemyDots(center, scale, playerPos);

        // ── 玩家（荧光青色大点）──
        DrawDot(center - 3, center - 3, 6, _playerColor);

        GUI.EndGroup();

        // ── 标签 ──
        GUI.color = UIColorTheme.TextSecondary;
        var labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Label(new Rect(mapX, mapY + _size + 2, _size, 15), "MINIMAP", labelStyle);
        GUI.color = Color.white;
    }

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
            float py = center - offset.y * scale;

            float dotSize = (enemy is BossEnemy) ? 5f : 3f;
            // Boss 用亮粉，普通敌人用洋红
            Color dotColor = (enemy is BossEnemy) ? UIColorTheme.AccentPink : _enemyColor;
            DrawDot(px - dotSize / 2, py - dotSize / 2, dotSize, dotColor);
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