using UnityEngine;

/// <summary>
/// Boss 阶段逻辑辅助类 — 从 BossEnemy 提取。
/// 职责：阶段更新、技能执行、视觉特效、类型配置。
/// </summary>
internal static class BossPhaseHelper
{
    // ── 阶段状态机 ──

    internal static void UpdatePhase(BossEnemy boss, float hpPercent)
    {
        int newPhase;
        if (hpPercent > 0.66f) newPhase = 1;
        else if (hpPercent > 0.33f) newPhase = 2;
        else if (hpPercent > 0.15f) newPhase = 3;
        else if (hpPercent > 0.08f) newPhase = 4;
        else newPhase = 5;

        if (newPhase != boss._currentPhase)
        {
            boss._currentPhase = newPhase;
            DebugHelper.Log($"[BossEnemy] Phase transition → Phase {boss._currentPhase}!");
            EventManager.TriggerBossPhaseChange(boss._currentPhase, 5);
            PlayPhaseTransitionEffect(boss, boss._currentPhase);

            if (boss._currentPhase == 5)
            {
                boss._bossSpeed = boss._baseSpeed * boss._enrageSpeedMult;
                boss.Setup(boss._bossSpeed, boss._bossContactDamage, 0, 50);
            }
        }
    }

    // ── 阶段更新 ──

    internal static void CheckEnrage(BossEnemy boss)
    {
        if (boss._bossDamageable != null && boss._bossDamageable.HpPercent < 0.1f && !boss._enraged)
        {
            boss._enraged = true;
            boss.MoveSpeed *= 1.5f;
            boss._bulletDamage *= 2;
            boss._chargeDamage *= 2;
            boss._bossContactDamage *= 2;

            var sr = boss.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1f, 0.3f, 0.3f);
            CombatManager.CreateExplosionEffect(boss.transform.position, 3f, Color.red, 0.5f);

            DamagePopup.Create(
                boss.transform.position + Vector3.up * 2f,
                0,
                Color.red,
                false,
                "ENRAGED!"
            );

            DebugHelper.Log("[BossEnemy] Boss ENRAGED! Damage x2, Speed x1.5");
        }
    }

    internal static void HandlePhase1(BossEnemy boss)
    {
        if (boss.Type == BossEnemy.BossType.Phantom) UpdatePhantomBlink(boss);
        if (boss.Type == BossEnemy.BossType.Berserker) UpdateSplitBullets(boss);

        if (Time.time - boss._lastBarrageTime > boss._bulletBarrageInterval)
        {
            BossAbilities.FireBarrage(boss.transform.position, boss._bulletBarrageCount, boss._bulletSpeed, boss._bulletDamage, boss._playerTransform);
            boss._lastBarrageTime = Time.time;
        }
    }

    internal static void HandlePhase2(BossEnemy boss)
    {
        if (boss.Type == BossEnemy.BossType.Phantom) UpdatePhantomBlink(boss);
        if (boss.Type == BossEnemy.BossType.Berserker) UpdateSplitBullets(boss);

        if (Time.time - boss._lastBarrageTime > boss._bulletBarrageInterval * 0.7f)
        {
            BossAbilities.FireBarrage(boss.transform.position, boss._bulletBarrageCount, boss._bulletSpeed, boss._bulletDamage, boss._playerTransform);
            boss._lastBarrageTime = Time.time;
        }
        if (Time.time - boss._lastSummonTime > boss._summonInterval)
        {
            BossAbilities.SummonMinions(boss.transform.position, boss._summonCount, boss.gameObject.layer);
            boss._lastSummonTime = Time.time;
        }
    }

    internal static void HandlePhase3(BossEnemy boss)
    {
        if (boss.Type == BossEnemy.BossType.Phantom) UpdatePhantomBlink(boss);
        if (boss.Type == BossEnemy.BossType.Berserker) UpdateSplitBullets(boss);

        if (Time.time - boss._lastBarrageTime > boss._bulletBarrageInterval * 0.5f)
        {
            BossAbilities.FireBarrage(boss.transform.position, boss._bulletBarrageCount, boss._bulletSpeed, boss._bulletDamage, boss._playerTransform);
            boss._lastBarrageTime = Time.time;
        }
        if (Time.time - boss._lastChargeTime > boss._chargeInterval)
        {
            StartCharge(boss);
            boss._lastChargeTime = Time.time;
        }
        if (!boss._phase3AbilityUsed)
        {
            boss._phase3AbilityUsed = true;
            ExecutePhase3Ability(boss);
        }
    }

    internal static void HandlePhase4(BossEnemy boss)
    {
        if (boss.Type == BossEnemy.BossType.Phantom) UpdatePhantomBlink(boss);
        if (boss.Type == BossEnemy.BossType.Berserker) UpdateSplitBullets(boss);

        if (Time.time - boss._lastBarrageTime > boss._bulletBarrageInterval * 0.6f)
        {
            BossAbilities.FireBarrage(boss.transform.position, boss._bulletBarrageCount, boss._bulletSpeed, boss._bulletDamage, boss._playerTransform);
            boss._lastBarrageTime = Time.time;
        }
        if (Time.time - boss._lastPoisonTime > boss._poisonZoneInterval)
        {
            BossAbilities.SpawnPoisonZone(boss.transform.position, boss._poisonDamage, boss._poisonDuration, boss._poisonZoneRadius);
            boss._lastPoisonTime = Time.time;
        }
        if (Time.time - boss._lastShockwaveTime > boss._shockwaveInterval)
        {
            BossAbilities.FireShockwave(boss.transform.position, boss._shockwaveDamage, boss._shockwaveRadius);
            boss._lastShockwaveTime = Time.time;
        }
    }

    internal static void HandlePhase5(BossEnemy boss)
    {
        if (!boss._phase5AbilityTriggered)
        {
            boss._phase5AbilityTriggered = true;
            ExecutePhase5Ability(boss);
        }

        if (Time.time - boss._lastBarrageTime > boss._bulletBarrageInterval * boss._enrageAttackMult)
        {
            BossAbilities.FireBarrage(boss.transform.position, boss._bulletBarrageCount, boss._bulletSpeed, boss._bulletDamage, boss._playerTransform);
            boss._lastBarrageTime = Time.time;
        }

        if (boss._enrageChargesLeft <= 0 && Time.time - boss._lastChargeTime > boss._chargeInterval * 0.8f)
            boss._enrageChargesLeft = boss._enrageChargeCount;
        if (boss._enrageChargesLeft > 0 && !boss._isCharging && Time.time - boss._lastChargeTime > 0.5f)
        {
            StartCharge(boss);
            boss._lastChargeTime = Time.time;
            boss._enrageChargesLeft--;
        }

        if (Time.time - boss._lastPoisonTime > boss._poisonZoneInterval * 0.6f)
        {
            BossAbilities.SpawnPoisonZone(boss.transform.position, boss._poisonDamage, boss._poisonDuration, boss._poisonZoneRadius);
            boss._lastPoisonTime = Time.time;
        }

        if (boss.Type == BossEnemy.BossType.Berserker && boss._bossDamageable != null && boss._bossDamageable.HpPercent < 0.2f && !boss._berserkerUndyingActive)
        {
            boss._berserkerUndyingActive = true;
            boss._berserkerUndyingEndTime = Time.time + 5f;
            DebugHelper.Log("[BossEnemy] Berserker Phase 5: UNDYING STATE!");
        }
        if (boss._berserkerUndyingActive)
        {
            if (boss._bossDamageable != null) boss._bossDamageable.Heal(1);
            if (Time.time >= boss._berserkerUndyingEndTime)
            {
                boss._berserkerUndyingActive = false;
                DebugHelper.Log("[BossEnemy] Berserker: Undying state ended!");
            }
        }
    }

    // ── 冲锋 ──

    internal static void HandleCharging(BossEnemy boss)
    {
        var rb = boss.GetComponent<Rigidbody2D>();
        BossAbilities.UpdateCharge(rb, boss._chargeDirection, boss._chargeSpeed);
        if (Time.time > boss._chargeEndTime)
        {
            boss._isCharging = false;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }

    internal static void StartCharge(BossEnemy boss)
    {
        var result = BossAbilities.StartCharge(boss.transform.position, boss._playerTransform);
        boss._chargeDirection = result.dir;
        boss._chargeEndTime = result.endTime;
        boss._isCharging = result.endTime > Time.time;
    }

    // ── 阶段专属技能（委托） ──

    internal static void ExecutePhase3Ability(BossEnemy boss)
    {
        switch (boss.Type)
        {
            case BossEnemy.BossType.Juggernaut:
                BossAbilities.ExecuteJuggernautPhase3(boss.transform.position, boss.gameObject.layer);
                break;
            case BossEnemy.BossType.Sorcerer:
                BossAbilities.ExecuteSorcererPhase3(boss.transform.position);
                break;
            case BossEnemy.BossType.Phantom:
                boss._invisibleDuration = 3f;
                boss._blinkCooldown = 3f;
                DebugHelper.Log("[BossEnemy] Phantom Phase 3: Enhanced stealth!");
                break;
            case BossEnemy.BossType.Berserker:
                boss._berserkerLeaveFireTrail = true;
                DebugHelper.Log("[BossEnemy] Berserker Phase 3: Fire trail on charge!");
                break;
        }
    }

    internal static void ExecutePhase5Ability(BossEnemy boss)
    {
        switch (boss.Type)
        {
            case BossEnemy.BossType.Juggernaut:
                boss._bossSpeed = boss._baseSpeed * 2f;
                boss._chargeInterval = 0.5f;
                boss.Setup(boss._bossSpeed, boss._bossContactDamage, 0, 50);
                DebugHelper.Log("[BossEnemy] Juggernaut Phase 5: BERSERK!");
                break;
            case BossEnemy.BossType.Sorcerer:
                BossAbilities.ExecuteSorcererPhase5(boss.transform.position, boss._bulletBarrageCount, boss._bulletSpeed, boss._bulletDamage,
                    boss._playerTransform, boss._summonCount, boss._shockwaveDamage, boss._shockwaveRadius, boss.gameObject.layer);
                break;
            case BossEnemy.BossType.Phantom:
                BossAbilities.ExecutePhantomPhase5(boss, boss._bossDamageable, boss._bossSpeed, boss._bulletBarrageCount);
                break;
            case BossEnemy.BossType.Berserker:
                DebugHelper.Log("[BossEnemy] Berserker Phase 5: Undying mode ready!");
                break;
        }
    }

    // ── 类型配置 ──

    internal static void ApplyBossTypeConfig(BossEnemy boss)
    {
        var sr = boss.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = BossFactory.BossColors[(int)boss.Type];

        switch (boss.Type)
        {
            case BossEnemy.BossType.Juggernaut:
                boss._bossHP = Mathf.RoundToInt(boss._bossHP * 1.5f);
                boss._bossSpeed *= 0.8f;
                boss._chargeSpeed *= 1.3f;
                boss._chargeDamage = Mathf.RoundToInt(boss._chargeDamage * 1.3f);
                if (boss._bossDamageable != null) { boss._bossDamageable.SetMaxHp(boss._bossHP); boss._bossDamageable.Heal(boss._bossHP); }
                boss.transform.localScale = Vector3.one * 2.5f;
                break;
            case BossEnemy.BossType.Sorcerer:
                boss._bulletBarrageCount = 12;
                boss._bulletSpeed *= 1.3f;
                boss._summonCount = 5;
                boss._shockwaveRadius *= 1.3f;
                break;
            case BossEnemy.BossType.Phantom:
                boss._bossSpeed *= 1.3f;
                boss._bulletBarrageCount = 16;
                boss._bossHP = Mathf.RoundToInt(boss._bossHP * 0.7f);
                if (boss._bossDamageable != null) { boss._bossDamageable.SetMaxHp(boss._bossHP); boss._bossDamageable.Heal(boss._bossHP); }
                if (sr != null) { var c = sr.color; c.a = 0.6f; sr.color = c; }
                break;
            case BossEnemy.BossType.Berserker:
                boss._bossSpeed *= 1.5f;
                boss._chargeInterval *= 0.6f;
                boss._enrageSpeedMult = 2f;
                boss._enrageChargeCount = 5;
                boss._bossHP = Mathf.RoundToInt(boss._bossHP * 0.8f);
                if (boss._bossDamageable != null) { boss._bossDamageable.SetMaxHp(boss._bossHP); boss._bossDamageable.Heal(boss._bossHP); }
                break;
        }

        boss.Setup(boss._bossSpeed, boss._bossContactDamage, 0, 50);
    }

    // ── 幽灵型闪现 ──

    internal static void UpdatePhantomBlink(BossEnemy boss)
    {
        if (boss._isInvisible)
        {
            BossAbilities.UpdateInvisibleMovement(boss, boss._playerTransform, boss._bossSpeed);
            return;
        }
        if (Time.time >= boss._nextBlinkTime)
        {
            boss._isInvisible = true;
            BossAbilities.StartBlink(boss, boss._playerTransform, boss._invisibleDuration);
            boss.Invoke("EndBlink", boss._invisibleDuration);
        }
    }

    internal static void EndBlink(BossEnemy boss)
    {
        boss._isInvisible = false;
        BossAbilities.EndBlink(boss);
        BossAbilities.FireBarrage(boss.transform.position, boss._bulletBarrageCount, boss._bulletSpeed, boss._bulletDamage, boss._playerTransform);
        boss._nextBlinkTime = Time.time + boss._blinkCooldown;
    }

    // ── 狂战型分裂弹幕 ──

    internal static void UpdateSplitBullets(BossEnemy boss)
    {
        if (Time.time - boss._lastSplitTime > boss._splitBulletInterval)
        {
            BossAbilities.FireSplitBarrage(boss.transform.position, boss._playerTransform, boss._bulletSpeed, boss._bulletDamage);
            boss._lastSplitTime = Time.time;
        }
    }

    // ── 视觉特效 ──

    /// <summary>
    /// Boss出场效果：屏幕震动+红色闪光+专属音效+血条显示
    /// </summary>
    internal static void PlayEntranceEffects(BossEnemy boss)
    {
        var cam = GameReferences.MainCamera;
        if (cam != null)
        {
            var shake = cam.GetComponent<ScreenShake>();
            if (shake != null) shake.Shake(3f, 1f);
        }

        DamageFlashEffect.Show(0.2f, new Color(1f, 0.1f, 0.1f, 0.4f));

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayLevelUp();

        var player = GameReferences.Player;
        if (player != null)
        {
            DamagePopup.Create(
                player.transform.position + Vector3.up * 5f,
                0,
                BossFactory.BossColors[(int)boss.Type],
                false,
                $"★ BOSS: {boss.Type}!"
            );
        }

        var minimap = Object.FindAnyObjectByType<MinimapUI>();
        if (minimap != null) minimap.NotifyBossSpawned();
    }

    /// <summary>
    /// 阶段切换增强效果：屏幕震动+闪光+阶段提示
    /// </summary>
    internal static void PlayPhaseTransitionEffect(BossEnemy boss, int phase)
    {
        var cam = GameReferences.MainCamera;
        if (cam != null)
        {
            var shake = cam.GetComponent<ScreenShake>();
            if (shake != null)
            {
                float intensity = 1.5f + phase * 0.5f;
                float duration = 0.3f + phase * 0.1f;
                shake.Shake(intensity, duration);
            }
        }

        Color flashColor;
        switch (phase)
        {
            case 2: flashColor = new Color(1f, 0.5f, 0f, 0.3f); break;
            case 3: flashColor = new Color(1f, 0.2f, 0f, 0.35f); break;
            case 4: flashColor = new Color(0.8f, 0f, 0.8f, 0.3f); break;
            case 5: flashColor = new Color(1f, 0f, 0f, 0.5f); break;
            default: flashColor = new Color(1f, 1f, 0f, 0.2f); break;
        }
        DamageFlashEffect.Show(0.15f, flashColor);

        var player = GameReferences.Player;
        if (player != null)
        {
            string phaseText = phase == 5 ? "★ ENRAGE!" : $"Phase {phase}";
            DamagePopup.Create(
                player.transform.position + Vector3.up * 4f,
                0,
                flashColor,
                false,
                phaseText
            );
        }

        DebugHelper.Log($"[BossEnemy] Phase {phase} transition effect played");
    }

    /// <summary>
    /// Boss死亡增强效果：慢动作特写+大量粒子+金币爆发
    /// </summary>
    internal static void PlayDeathEffects(BossEnemy boss)
    {
        Time.timeScale = 0.3f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        boss.StartCoroutine(RestoreTimeScaleAfterDelay(0.5f));

        CombatManager.CreateExplosionEffect(boss.transform.position, 8f,
            BossFactory.BossColors[(int)boss.Type], 1f);

        var cam = GameReferences.MainCamera;
        if (cam != null)
        {
            var shake = cam.GetComponent<ScreenShake>();
            if (shake != null) shake.Shake(5f, 1.5f);
        }

        DamageFlashEffect.Show(0.3f, new Color(1f, 0.85f, 0f, 0.5f));

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayLevelUp();

        DebugHelper.Log("[BossEnemy] Boss death effect played");
    }

    private static System.Collections.IEnumerator RestoreTimeScaleAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}
