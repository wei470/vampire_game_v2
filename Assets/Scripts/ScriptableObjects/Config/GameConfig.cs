using UnityEngine;

/// <summary>
/// 全局游戏配置 — 通过 ScriptableObject 数据驱动。
/// 在 Inspector 中调整所有基础参数。
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Vampire/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("玩家基础属性")]
    public int basePlayerHP = 100;
    public float basePlayerSpeed = 5f;
    public int basePlayerArmor = 0;
    public int basePlayerAttack = 10;

    [Header("经验系统")]
    public int baseLevelUpExp = 20;
    public int expPerLevel = 15;
    public float expPickupRadius = 2.5f;
    public float xpGemMagnetSpeed = 8f;

    [Header("金币系统")]
    public int baseCoinDrop = 5;
    public float coinPickupRadius = 2.0f;

    [Header("装备系统")]
    public float equipmentDropChance = 0.12f;
    public float legendaryDropChance = 0.02f;
    public int maxEquipmentSlots = 3;

    [Header("难度倍率")]
    public float damageScalingPerWave = 0.1f;
    public float hpScalingPerWave = 0.15f;
    public float speedScalingPerWave = 0.03f;

    [Header("Boss 配置")]
    public int bossWaveInterval = 5;
    public int bossBaseHP = 300;
    public int bossHPPerWave = 100;

    [Header("生成系统")]
    public float spawnRadius = 15f;               // 敌人生成距离（屏幕边缘外）
    public float enemyDespawnDistance = 50f;       // 敌人超出此距离自动销毁
    public int defaultContactDamage = 10;          // 默认碰撞伤害（EnemyBase/Player）

    [Header("跳波奖励")]
    public int skipWaveXpReward = 10;              // 跳波给予的经验
    public int skipWaveCoinReward = 5;             // 跳波给予的金币

    [Header("游戏设置")]
    public float restBetweenWaves = 2f;
    public float cameraOrthoSize = 12f;
    public float cameraFollowSpeed = 8f;
    public float screenShakeIntensity = 0.3f;

    [Header("DPS 测试木桩")]
    public long dpsDummyHP = 2000000000;            // 木桩HP（默认20亿）
    public int dpsDummyArmor = 0;                   // 木桩护甲
    public float dpsDummyRegen = 0f;                // 木桩每秒回血
    public bool dpsDummyInvincible = false;          // 木桩是否无敌（不死亡）
    public float dpsDummyRespawnDelay = 1f;          // 木桩死亡后重生延迟
}