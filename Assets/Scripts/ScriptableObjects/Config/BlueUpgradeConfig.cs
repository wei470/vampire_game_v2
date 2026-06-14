using UnityEngine;

/// <summary>
/// 蓝色角色升级配置 — 继承 CharacterUpgradeConfig。
/// 使用蓝色简单子弹，无 DOT 效果。
/// </summary>
[CreateAssetMenu(fileName = "BlueUpgradeConfig", menuName = "VampireGame/Blue Upgrade Config")]
public class BlueUpgradeConfig : CharacterUpgradeConfig
{
    private void OnEnable()
    {
        characterId = "blue";
        displayName = "蓝色战士";
        description = "基础战士 — 使用蓝色子弹直接伤害，简单直接。";
        passiveDescription = "使用蓝色子弹，命中直接扣血";
        characterColor = new Color(0.3f, 0.5f, 1f);

        if (upgradeEntries == null || upgradeEntries.Length == 0)
        {
            upgradeEntries = new UpgradeEntry[]
            {
                new UpgradeEntry
                {
                    upgradeId = "haste", upgradeName = "急速 (Haste)",
                    description = "攻速+15%",
                    category = CharacterUpgradeOption.UpgradeCategory.AttackSpeed,
                    value1 = 0.15f, maxStacks = 0
                },
                new UpgradeEntry
                {
                    upgradeId = "barrage", upgradeName = "弹幕 (Barrage)",
                    description = "子弹+1",
                    category = CharacterUpgradeOption.UpgradeCategory.BulletCount,
                    value1 = 1f, maxStacks = 0
                },
                new UpgradeEntry
                {
                    upgradeId = "ricochet", upgradeName = "贯穿弹 (Penetrate)",
                    description = "穿透+1",
                    category = CharacterUpgradeOption.UpgradeCategory.Ricochet,
                    value1 = 1f, maxStacks = 3
                },
                new UpgradeEntry
                {
                    upgradeId = "bullet_size", upgradeName = "弹体增大 (Bullet Size)",
                    description = "子弹体积+20%",
                    category = CharacterUpgradeOption.UpgradeCategory.BulletSize,
                    value1 = 0.20f, maxStacks = 0
                },
                new UpgradeEntry
                {
                    upgradeId = "move_speed", upgradeName = "移速 (Move Speed)",
                    description = "移动速度+10%",
                    category = CharacterUpgradeOption.UpgradeCategory.MoveSpeed,
                    value1 = 0.10f, maxStacks = 0
                },
            };
        }
    }
}
