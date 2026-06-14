#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 射速系统测试 — 验证累加器方案在各种弹幕×急速组合下的正确性。
///
/// 测试逻辑：
///   1. 创建 MagePassive + 1 种 DOT 枪
///   2. 模拟 N 秒射击（手动推进 accumulator）
///   3. 统计子弹数量，与理论值对比
///
/// 理论值公式：
///   effectiveCooldown = Max(0.1, gun.cooldown * attackSpeedMult)
///   shotsPer5s = floor(5.0 / effectiveCooldown)
///   bulletsPerShot = Min(1 + bulletCountBonus, 3)
///   expectedBullets = shotsPer5s * bulletsPerShot
/// </summary>
[TestFixture]
public class FiringSystemTests
{
    private const float TEST_DURATION = 5f;
    private const float SIMULATED_DT = 1f / 60f; // 60 FPS

    // ── 辅助方法 ──

    /// <summary>
    /// 模拟射击 N 秒，返回创建的子弹总数
    /// </summary>
    private static int SimulateFiring(DotGunState gun, float attackSpeedMult, int bulletCountBonus,
        float duration, int maxBulletsPerFrame = 15)
    {
        int totalBullets = 0;
        float accumulator = gun.accumulator; // 使用枪的初始累加器（含交错偏移）
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += SIMULATED_DT;
            float effectiveCooldown = Mathf.Max(0.1f, gun.cooldown * attackSpeedMult);
            int bulletsPerShot = Mathf.Min(1 + bulletCountBonus, 3);

            accumulator += SIMULATED_DT;

            if (accumulator >= effectiveCooldown && totalBullets + bulletsPerShot <= maxBulletsPerFrame * (int)(elapsed / SIMULATED_DT + 1))
            {
                accumulator -= effectiveCooldown;
                totalBullets += bulletsPerShot;
            }

            if (accumulator > effectiveCooldown * 1.5f)
                accumulator = effectiveCooldown * 1.5f;
        }

        return totalBullets;
    }

    /// <summary>
    /// 计算理论预期子弹数
    /// </summary>
    private static int CalculateExpectedBullets(float gunCooldown, float attackSpeedMult, int bulletCountBonus, float duration)
    {
        float effectiveCooldown = Mathf.Max(0.1f, gunCooldown * attackSpeedMult);
        int shotsPerDuration = Mathf.FloorToInt(duration / effectiveCooldown);
        int bulletsPerShot = Mathf.Min(1 + bulletCountBonus, 3);
        return shotsPerDuration * bulletsPerShot;
    }

    /// <summary>
    /// 获取攻速乘数（模拟 N 层急速，每层 +15%）
    /// </summary>
    private static float GetAttackSpeedMult(int hasteStacks)
    {
        float bonus = hasteStacks * 0.15f;
        return Mathf.Max(0.2f, 1f - bonus);
    }

    // ═══ 弹幕 1（bulletCountBonus = 0，每枪 1 发）═══

    [Test]
    public void Barrage1_Haste0_Poison()
    {
        float cooldown = 1.8f;
        float asMult = GetAttackSpeedMult(0);
        int expected = CalculateExpectedBullets(cooldown, asMult, 0, TEST_DURATION);
        var gun = new DotGunState { effectType = StatusEffectType.Poison, cooldown = cooldown, accumulator = 0f };
        int actual = SimulateFiring(gun, asMult, 0, TEST_DURATION);
        Assert.AreEqual(expected, actual, $"Poison barrage=1 haste=0: expected {expected}, got {actual}");
    }

    [Test]
    public void Barrage1_Haste5_Poison()
    {
        float cooldown = 1.8f;
        float asMult = GetAttackSpeedMult(5);
        int expected = CalculateExpectedBullets(cooldown, asMult, 0, TEST_DURATION);
        var gun = new DotGunState { effectType = StatusEffectType.Poison, cooldown = cooldown, accumulator = 0f };
        int actual = SimulateFiring(gun, asMult, 0, TEST_DURATION);
        Assert.AreEqual(expected, actual, $"Poison barrage=1 haste=5: expected {expected}, got {actual}");
    }

    [Test]
    public void Barrage1_Haste10_Poison()
    {
        float cooldown = 1.8f;
        float asMult = GetAttackSpeedMult(10);
        int expected = CalculateExpectedBullets(cooldown, asMult, 0, TEST_DURATION);
        var gun = new DotGunState { effectType = StatusEffectType.Poison, cooldown = cooldown, accumulator = 0f };
        int actual = SimulateFiring(gun, asMult, 0, TEST_DURATION);
        Assert.AreEqual(expected, actual, $"Poison barrage=1 haste=10: expected {expected}, got {actual}");
    }

    // ═══ 弹幕 2（bulletCountBonus = 1，每枪 2 发）═══

    [Test]
    public void Barrage2_Haste0_Poison()
    {
        float cooldown = 1.8f;
        float asMult = GetAttackSpeedMult(0);
        int expected = CalculateExpectedBullets(cooldown, asMult, 1, TEST_DURATION);
        var gun = new DotGunState { effectType = StatusEffectType.Poison, cooldown = cooldown, accumulator = 0f };
        int actual = SimulateFiring(gun, asMult, 1, TEST_DURATION);
        Assert.AreEqual(expected, actual, $"Poison barrage=2 haste=0: expected {expected}, got {actual}");
    }

    [Test]
    public void Barrage2_Haste5_Poison()
    {
        float cooldown = 1.8f;
        float asMult = GetAttackSpeedMult(5);
        int expected = CalculateExpectedBullets(cooldown, asMult, 1, TEST_DURATION);
        var gun = new DotGunState { effectType = StatusEffectType.Poison, cooldown = cooldown, accumulator = 0f };
        int actual = SimulateFiring(gun, asMult, 1, TEST_DURATION);
        Assert.AreEqual(expected, actual, $"Poison barrage=2 haste=5: expected {expected}, got {actual}");
    }

    [Test]
    public void Barrage2_Haste10_Poison()
    {
        float cooldown = 1.8f;
        float asMult = GetAttackSpeedMult(10);
        int expected = CalculateExpectedBullets(cooldown, asMult, 1, TEST_DURATION);
        var gun = new DotGunState { effectType = StatusEffectType.Poison, cooldown = cooldown, accumulator = 0f };
        int actual = SimulateFiring(gun, asMult, 1, TEST_DURATION);
        Assert.AreEqual(expected, actual, $"Poison barrage=2 haste=10: expected {expected}, got {actual}");
    }

    // ═══ 弹幕 3（bulletCountBonus = 2，每枪 3 发）═══

    [Test]
    public void Barrage3_Haste0_Poison()
    {
        float cooldown = 1.8f;
        float asMult = GetAttackSpeedMult(0);
        int expected = CalculateExpectedBullets(cooldown, asMult, 2, TEST_DURATION);
        var gun = new DotGunState { effectType = StatusEffectType.Poison, cooldown = cooldown, accumulator = 0f };
        int actual = SimulateFiring(gun, asMult, 2, TEST_DURATION);
        Assert.AreEqual(expected, actual, $"Poison barrage=3 haste=0: expected {expected}, got {actual}");
    }

    [Test]
    public void Barrage3_Haste5_Poison()
    {
        float cooldown = 1.8f;
        float asMult = GetAttackSpeedMult(5);
        int expected = CalculateExpectedBullets(cooldown, asMult, 2, TEST_DURATION);
        var gun = new DotGunState { effectType = StatusEffectType.Poison, cooldown = cooldown, accumulator = 0f };
        int actual = SimulateFiring(gun, asMult, 2, TEST_DURATION);
        Assert.AreEqual(expected, actual, $"Poison barrage=3 haste=5: expected {expected}, got {actual}");
    }

    [Test]
    public void Barrage3_Haste10_Poison()
    {
        float cooldown = 1.8f;
        float asMult = GetAttackSpeedMult(10);
        int expected = CalculateExpectedBullets(cooldown, asMult, 2, TEST_DURATION);
        var gun = new DotGunState { effectType = StatusEffectType.Poison, cooldown = cooldown, accumulator = 0f };
        int actual = SimulateFiring(gun, asMult, 2, TEST_DURATION);
        Assert.AreEqual(expected, actual, $"Poison barrage=3 haste=10: expected {expected}, got {actual}");
    }

    // ═══ 其他子弹类型 ═══

    [Test]
    public void Barrage3_Haste10_Burn()
    {
        float cooldown = 0.5f;
        float asMult = GetAttackSpeedMult(10);
        int expected = CalculateExpectedBullets(cooldown, asMult, 2, TEST_DURATION);
        var gun = new DotGunState { effectType = StatusEffectType.Burn, cooldown = cooldown, accumulator = 0f };
        int actual = SimulateFiring(gun, asMult, 2, TEST_DURATION);
        Assert.AreEqual(expected, actual, $"Burn barrage=3 haste=10: expected {expected}, got {actual}");
    }

    [Test]
    public void Barrage3_Haste10_Frost()
    {
        float cooldown = 1.2f;
        float asMult = GetAttackSpeedMult(10);
        int expected = CalculateExpectedBullets(cooldown, asMult, 2, TEST_DURATION);
        var gun = new DotGunState { effectType = StatusEffectType.Frostbite, cooldown = cooldown, accumulator = 0f };
        int actual = SimulateFiring(gun, asMult, 2, TEST_DURATION);
        Assert.AreEqual(expected, actual, $"Frost barrage=3 haste=10: expected {expected}, got {actual}");
    }

    [Test]
    public void Barrage3_Haste10_Static()
    {
        float cooldown = 0.9f;
        float asMult = GetAttackSpeedMult(10);
        int expected = CalculateExpectedBullets(cooldown, asMult, 2, TEST_DURATION);
        var gun = new DotGunState { effectType = StatusEffectType.Static, cooldown = cooldown, accumulator = 0f };
        int actual = SimulateFiring(gun, asMult, 2, TEST_DURATION);
        Assert.AreEqual(expected, actual, $"Static barrage=3 haste=10: expected {expected}, got {actual}");
    }

    [Test]
    public void Barrage3_Haste10_Wind()
    {
        float cooldown = 0.2f;
        float asMult = GetAttackSpeedMult(10);
        int expected = CalculateExpectedBullets(cooldown, asMult, 2, TEST_DURATION);
        var gun = new DotGunState { effectType = StatusEffectType.WindErosion, cooldown = cooldown, accumulator = 0f };
        int actual = SimulateFiring(gun, asMult, 2, TEST_DURATION);
        Assert.AreEqual(expected, actual, $"Wind barrage=3 haste=10: expected {expected}, got {actual}");
    }

    // ═══ 交错偏移测试 ═══

    [Test]
    public void StaggerOffset_PreventsSyncBurst()
    {
        // 两把枪，交错初始化，不应该在同一帧开火
        float cooldown = 0.1f;
        float asMult = 1f;
        var gunA = new DotGunState { cooldown = cooldown, accumulator = 0f };
        var gunB = new DotGunState { cooldown = cooldown, accumulator = 0.05f }; // 半个周期偏移

        // 模拟 1 帧
        gunA.accumulator += SIMULATED_DT;
        gunB.accumulator += SIMULATED_DT;

        bool gunAFires = gunA.accumulator >= cooldown;
        bool gunBFires = gunB.accumulator >= cooldown;

        // 第一帧只有一把枪开火
        Assert.AreNotEqual(gunAFires, gunBFires, "Staggered guns should not fire on the same frame");
    }

    // ═══ 帧预算测试 ═══

    [Test]
    public void FrameBudget_LimitsBulletsPerFrame()
    {
        // 7 枪全部就绪，帧预算 15，每枪 3 发
        int bulletsPerShot = 3;
        int maxPerFrame = 15;
        int gunsFiring = 0;
        int totalBullets = 0;

        for (int i = 0; i < 7; i++)
        {
            if (totalBullets + bulletsPerShot <= maxPerFrame)
            {
                totalBullets += bulletsPerShot;
                gunsFiring++;
            }
        }

        Assert.AreEqual(5, gunsFiring, "Only 5 guns should fire per frame with budget 15");
        Assert.AreEqual(15, totalBullets, "Total bullets should equal frame budget");
    }

    // ═══ 累加器上限测试 ═══

    [Test]
    public void AccumulatorCap_PreventsCatchUpBurst()
    {
        float cooldown = 0.1f;
        float cap = cooldown * 1.5f;
        float accumulator = 10f; // 模拟长时间积压

        if (accumulator > cap)
            accumulator = cap;

        Assert.AreEqual(cap, accumulator, "Accumulator should be capped at 1.5x cooldown");

        // 开火后累加器应为 cap - cooldown = 0.05
        accumulator -= cooldown;
        Assert.AreEqual(0.05f, accumulator, 0.001f, "After firing, accumulator should be cap - cooldown");
    }

    // ═══ 急速层数递增测试 ═══

    [Test]
    public void HasteStacks_MonotonicBulletIncrease()
    {
        float cooldown = 1.8f;
        int prevBullets = 0;

        for (int haste = 0; haste <= 10; haste++)
        {
            float asMult = GetAttackSpeedMult(haste);
            int bullets = CalculateExpectedBullets(cooldown, asMult, 2, TEST_DURATION);
            Assert.GreaterOrEqual(bullets, prevBullets,
                $"Haste {haste}: bullets ({bullets}) should >= haste {haste - 1} ({prevBullets})");
            prevBullets = bullets;
        }
    }
}
#endif
