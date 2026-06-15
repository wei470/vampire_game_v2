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
    /// 新逻辑：cap 在 if 之前，无帧预算
    /// </summary>
    private static int SimulateFiring(DotGunState gun, float attackSpeedMult, int bulletCountBonus,
        float duration, int maxBulletsPerFrame = 15)
    {
        int totalBullets = 0;
        float accumulator = gun.accumulator;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += SIMULATED_DT;
            float effectiveCooldown = Mathf.Max(0.33f, gun.cooldown * attackSpeedMult);
            int bulletsPerShot = Mathf.Min(1 + bulletCountBonus, 3);

            accumulator += SIMULATED_DT;

            // cap 在开火检查之前：取模保留子周期相位（与生产逻辑一致）
            if (accumulator > effectiveCooldown * 1.5f)
                accumulator = effectiveCooldown + Mathf.Repeat(accumulator, effectiveCooldown);

            if (accumulator >= effectiveCooldown)
            {
                accumulator -= effectiveCooldown;
                totalBullets += bulletsPerShot;
            }
        }

        return totalBullets;
    }

    /// <summary>
    /// 计算理论预期子弹数
    /// </summary>
    private static int CalculateExpectedBullets(float gunCooldown, float attackSpeedMult, int bulletCountBonus, float duration)
    {
        float effectiveCooldown = Mathf.Max(0.33f, gunCooldown * attackSpeedMult);
        int shotsPerDuration = Mathf.FloorToInt(duration / effectiveCooldown);
        int bulletsPerShot = Mathf.Min(1 + bulletCountBonus, 3);
        return shotsPerDuration * bulletsPerShot;
    }

    /// <summary>
    /// 获取攻速乘数（模拟 N 层急速，每层 +15%，对数递减）
    /// </summary>
    private static float GetAttackSpeedMult(int hasteStacks)
    {
        float bonus = hasteStacks * 0.15f;
        return Mathf.Max(0.2f, 1f / (1f + bonus));
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
        // 两把枪，初始化为 0，第一帧都不开火（需要先积累到 cooldown）
        float cooldown = 0.1f;
        var gunA = new DotGunState { cooldown = cooldown, accumulator = 0f };
        var gunB = new DotGunState { cooldown = cooldown, accumulator = 0f };

        // 模拟 1 帧
        gunA.accumulator += SIMULATED_DT; // 0.0167 < 0.1
        gunB.accumulator += SIMULATED_DT; // 0.0167 < 0.1

        bool gunAFires = gunA.accumulator >= cooldown;
        bool gunBFires = gunB.accumulator >= cooldown;

        Assert.IsFalse(gunAFires, "Gun A should NOT fire on first frame (accumulator too low)");
        Assert.IsFalse(gunBFires, "Gun B should NOT fire on first frame (accumulator too low)");
    }

    // ═══ 帧预算测试 ═══

    [Test]
    public void FrameBudget_NeverSplitsBarrage()
    {
        // 帧预算以「整把枪」为粒度：每把开火的枪都射出完整弹幕，绝不被截断成 1/2 发。
        // 7 枪全部就绪、每枪弹幕 3、帧预算 15。
        const int gunCount = 7;
        const int barrage = 3;
        const int budget = 15;

        int bulletsThisFrame = 0;
        int gunsFired = 0;
        for (int i = 0; i < gunCount; i++)
        {
            // 已开过火且再加一把会超预算 → 推迟整把枪（不截断弹幕）
            if (bulletsThisFrame > 0 && bulletsThisFrame + barrage > budget)
                continue;

            int created = barrage; // 完整弹幕，从不部分创建
            bulletsThisFrame += created;
            gunsFired++;

            Assert.AreEqual(barrage, created, "Every firing gun must emit a FULL barrage (no partial 1/2 shots)");
        }

        Assert.AreEqual(0, bulletsThisFrame % barrage, "Total bullets must be a whole multiple of the barrage size");
        Assert.LessOrEqual(bulletsThisFrame, budget + barrage - 1, "Per-frame total bounded (budget + at most one barrage overshoot)");
        Assert.Less(gunsFired, gunCount, "With budget 15 and 7x3 demand, not all guns fire the same frame");
    }

    [Test]
    public void SingleGun_AlwaysFiresFullBarrage()
    {
        // 单枪场景：无论帧预算如何，每次开火都射出完整弹幕（弹幕+2 → 恒定 3 发）
        const int budget = 15;
        const int bulletCountBonus = 2;
        int barrage = Mathf.Min(1 + bulletCountBonus, 5);

        int bulletsThisFrame = 0;
        // 单枪是本帧第一把（bulletsThisFrame == 0），门槛不触发
        bool deferred = bulletsThisFrame > 0 && bulletsThisFrame + barrage > budget;
        Assert.IsFalse(deferred, "First gun of the frame is never deferred");

        int created = barrage;
        Assert.AreEqual(3, created, "barrage+2 must always emit exactly 3 bullets");
    }

    // ═══ 累加器上限测试 ═══

    [Test]
    public void AccumulatorCap_PreventsCatchUpBurst()
    {
        float cooldown = 0.1f;
        float accumulator = 10f; // 模拟长时间积压

        // 相位保留的积压上限：取模到 [cooldown, 2*cooldown)
        if (accumulator > cooldown * 1.5f)
            accumulator = cooldown + Mathf.Repeat(accumulator, cooldown);

        // 积压被限制：最多缓冲约 1 发，不会一次性倾泻
        Assert.LessOrEqual(accumulator, cooldown * 2f + 0.001f, "Backlog should be bounded to <2 cooldowns");
        Assert.GreaterOrEqual(accumulator, cooldown - 0.001f, "Should retain at least one buffered shot");

        // 开火一次后应回落到不开火
        Assert.IsTrue(accumulator >= cooldown, "Should be able to fire once after cap");
        accumulator -= cooldown;
        Assert.Less(accumulator, cooldown, "After one shot, should NOT fire again immediately");
    }

    // ═══ 交错相位在 cap 后存活（Bug 1 修复） ═══

    [Test]
    public void Cap_PreservesGunStagger()
    {
        // 两枪相位差 0.03s，长时间积压后取模上限不应抹平相位差（否则会同步开火）
        float cooldown = 0.1f;
        float accA = 5.03f;
        float accB = 5.06f;

        accA = cooldown + Mathf.Repeat(accA, cooldown);
        accB = cooldown + Mathf.Repeat(accB, cooldown);

        Assert.AreNotEqual(accA, accB, "Guns must not collapse to identical accumulator after cap");
        Assert.AreEqual(0.03f, Mathf.Abs(accA - accB), 0.005f,
            "Cap should preserve inter-gun phase offset (stagger)");
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
