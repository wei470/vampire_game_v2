using UnityEngine;

/// <summary>
/// Boss 工厂 — 从 BossEnemy 提取。
/// 职责：Boss 预制体创建 + 类型选择 + 颜色配置。
/// </summary>
public static class BossFactory
{
    /// <summary>#36 Boss 类型颜色映射</summary>
    public static readonly Color[] BossColors = new Color[]
    {
        new Color(0.6f, 0.1f, 0.1f),   // Juggernaut: 深红
        new Color(0.4f, 0.1f, 0.6f),   // Sorcerer: 深紫
        new Color(0.3f, 0.5f, 0.6f),   // Phantom: 暗青
        new Color(0.8f, 0.3f, 0f),     // Berserker: 橙红
    };

    /// <summary>
    /// #36 根据波次自动选择 Boss 类型
    /// </summary>
    public static BossEnemy.BossType SelectBossTypeForWave(int waveNumber)
    {
        int bossIndex = (waveNumber / 5) % 4;
        return (BossEnemy.BossType)bossIndex;
    }

    /// <summary>
    /// 静态工厂方法：在指定位置生成 Boss
    /// </summary>
    public static BossEnemy CreateBoss(Vector3 position, int hp = 500)
    {
        var go = new GameObject("BOSS");
        go.transform.position = position;
        go.tag = "Enemy";

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Square;
        sr.color = new Color(0.8f, 0f, 0f);
        go.transform.localScale = Vector3.one * 2f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 0.9f);

        go.AddComponent<BaseEntity>();
        var dmg = go.AddComponent<Damageable>();
        dmg.SetMaxHp(hp);
        dmg.Heal(hp);
        go.AddComponent<KillRewarder>();

        var boss = go.AddComponent<BossEnemy>();
        boss._bossHP = hp;

        return boss;
    }
}