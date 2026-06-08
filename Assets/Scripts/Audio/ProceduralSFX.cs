using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 程序化音效生成器 — 纯代码合成所有游戏音效，无需外部音频文件。
/// 使用正弦波、噪声、频率扫描等技术生成各种音效。
/// 在 SoundTrack 初始化时调用 GenerateAll() 生成所有 AudioClip。
/// 
/// 音效合成技术：
/// - 正弦波：基础音调
/// - 白噪声：爆炸/冲击
/// - 频率扫描（chirp）：上升/下降音效
/// - 包络（ADSR）：控制音量衰减
/// - 谐波叠加：丰富音色
/// </summary>
public static class ProceduralSFX
{
    private const int SAMPLE_RATE = 44100;

    // ════════════════════════════════════════════════════════════════
    // 主入口 — 生成所有音效并填充 SoundTrack
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 为 SoundTrack 生成所有程序化音效
    /// </summary>
    public static void GenerateAll(SoundTrack track)
    {
        if (track == null) return;

        // 战斗音效
        track.hit.clip = GenerateHit(0.12f, 800f, 200f);
        track.crit.clip = GenerateHit(0.15f, 1200f, 300f);
        track.detonate.clip = GenerateExplosion(0.6f, 150f);
        track.dotTick.clip = GenerateTick(0.08f, 600f);

        // 敌人音效
        track.enemyDeath.clip = GenerateEnemyDeath(0.35f);
        track.bossSpawn.clip = GenerateBossSpawn(1.0f);

        // 玩家音效
        track.playerHurt.clip = GenerateHurt(0.2f);
        track.playerHeal.clip = GenerateHeal(0.3f);
        track.levelUp.clip = GenerateLevelUp(0.5f);
        track.pickup.clip = GeneratePickup(0.15f);
        track.coin.clip = GenerateCoin(0.1f);

        // UI音效
        track.select.clip = GenerateUIBeep(0.1f, 523f);    // C5
        track.confirm.clip = GenerateUIConfirm(0.15f);
        track.pause.clip = GenerateUIBeep(0.12f, 440f);    // A4

        // 环境音效
        track.waveStart.clip = GenerateWaveStart(0.5f);
        track.waveComplete.clip = GenerateWaveComplete(0.4f);

        // 技能音效
        track.skillCast.clip = GenerateSkillCast(0.25f);
        track.skillTeleport.clip = GenerateTeleport(0.3f);
        track.skillFrostNova.clip = GenerateFrostNova(0.4f);
        track.skillLightning.clip = GenerateLightning(0.35f);
        track.skillGravity.clip = GenerateGravityWell(0.4f);
        track.skillDeathAura.clip = GenerateDeathAura(0.4f);
        track.skillBerserk.clip = GenerateBerserk(0.35f);
        track.skillTheWorld.clip = GenerateTheWorld(0.5f);
        track.skillWindWave.clip = GenerateWindWave(0.3f);

        // 元素DOT音效
        track.dotBleed.clip = GenerateDotBleed(0.15f);
        track.dotPoison.clip = GenerateDotPoison(0.15f);
        track.dotBurn.clip = GenerateDotBurn(0.15f);
        track.dotFrost.clip = GenerateDotFrost(0.15f);
        track.dotLightning.clip = GenerateDotLightning(0.15f);
        track.dotDark.clip = GenerateDotDark(0.15f);
        track.dotLight.clip = GenerateDotLight(0.15f);

        // 特殊音效
        track.combo.clip = GenerateCombo(0.2f);
        track.shopBuy.clip = GenerateShopBuy(0.2f);
        track.achievement.clip = GenerateAchievement(0.4f);
        track.warning.clip = GenerateWarning(0.3f);

        DebugHelper.Log("[ProceduralSFX] All sound effects generated successfully.");
    }

    // ════════════════════════════════════════════════════════════════
    // 战斗音效
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 命中音效：短促噪声 + 频率衰减
    /// </summary>
    public static AudioClip GenerateHit(float duration, float startFreq, float endFreq)
    {
        return CreateClip("SFX_Hit", duration, (t) =>
        {
            float env = ExponentialDecay(t, duration, 12f);
            float freq = Mathf.Lerp(startFreq, endFreq, t / duration);
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            float noise = Random.Range(-1f, 1f) * 0.3f;
            return (sine * 0.6f + noise * 0.4f) * env;
        });
    }

    /// <summary>
    /// 引爆音效：低频噪声爆炸 + 余震
    /// </summary>
    public static AudioClip GenerateExplosion(float duration, float baseFreq)
    {
        return CreateClip("SFX_Detonate", duration, (t) =>
        {
            float env = ExponentialDecay(t, duration, 4f);
            float noise = Random.Range(-1f, 1f);
            float bass = Mathf.Sin(2f * Mathf.PI * baseFreq * t * (1f - t / duration * 0.5f));
            float crackle = Random.Range(-1f, 1f) * Mathf.Pow(1f - t / duration, 3f) * 0.5f;
            return (bass * 0.4f + noise * 0.4f + crackle * 0.2f) * env;
        });
    }

    /// <summary>
    /// DOT tick音效：短促高频点击
    /// </summary>
    public static AudioClip GenerateTick(float duration, float freq)
    {
        return CreateClip("SFX_DotTick", duration, (t) =>
        {
            float env = ExponentialDecay(t, duration, 25f);
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            return sine * env;
        });
    }

    // ════════════════════════════════════════════════════════════════
    // 敌人音效
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 敌人死亡音效：下降频率 + 噪声
    /// </summary>
    public static AudioClip GenerateEnemyDeath(float duration)
    {
        return CreateClip("SFX_EnemyDeath", duration, (t) =>
        {
            float env = ExponentialDecay(t, duration, 6f);
            float freq = Mathf.Lerp(600f, 80f, t / duration);
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            float noise = Random.Range(-1f, 1f) * 0.3f;
            return (sine * 0.5f + noise * 0.5f) * env;
        });
    }

    /// <summary>
    /// Boss出场音效：深沉低频 + 轰鸣
    /// </summary>
    public static AudioClip GenerateBossSpawn(float duration)
    {
        return CreateClip("SFX_BossSpawn", duration, (t) =>
        {
            float env = Mathf.Clamp01(t < 0.3f ? t / 0.3f : 1f - (t - 0.3f) / 0.7f);
            float bass = Mathf.Sin(2f * Mathf.PI * 60f * t);
            float sub = Mathf.Sin(2f * Mathf.PI * 30f * t) * 0.5f;
            float rumble = Random.Range(-1f, 1f) * 0.2f;
            return (bass * 0.5f + sub * 0.3f + rumble * 0.2f) * env;
        });
    }

    // ════════════════════════════════════════════════════════════════
    // 玩家音效
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 玩家受伤音效：短促下降
    /// </summary>
    public static AudioClip GenerateHurt(float duration)
    {
        return CreateClip("SFX_Hurt", duration, (t) =>
        {
            float env = ExponentialDecay(t, duration, 10f);
            float freq = Mathf.Lerp(400f, 150f, t / duration);
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            float noise = Random.Range(-1f, 1f) * 0.2f;
            return (sine * 0.7f + noise * 0.3f) * env;
        });
    }

    /// <summary>
    /// 治疗音效：柔和上升音调
    /// </summary>
    public static AudioClip GenerateHeal(float duration)
    {
        return CreateClip("SFX_Heal", duration, (t) =>
        {
            float env = Mathf.Sin(Mathf.PI * t / duration);
            float freq = Mathf.Lerp(400f, 800f, t / duration);
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            float sine2 = Mathf.Sin(2f * Mathf.PI * freq * 1.5f * t) * 0.3f;
            return (sine * 0.6f + sine2 * 0.4f) * env;
        });
    }

    /// <summary>
    /// 升级音效：上升琶音
    /// </summary>
    public static AudioClip GenerateLevelUp(float duration)
    {
        return CreateClip("SFX_LevelUp", duration, (t) =>
        {
            float env = Mathf.Sin(Mathf.PI * t / duration);
            // 三个音符快速上升
            float f1 = 523f; // C5
            float f2 = 659f; // E5
            float f3 = 784f; // G5
            float freq;
            if (t < duration * 0.33f)
                freq = f1;
            else if (t < duration * 0.66f)
                freq = f2;
            else
                freq = f3;

            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            float harm = Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.3f;
            return (sine * 0.6f + harm * 0.4f) * env;
        });
    }

    /// <summary>
    /// 拾取音效：短促上升叮
    /// </summary>
    public static AudioClip GeneratePickup(float duration)
    {
        return CreateClip("SFX_Pickup", duration, (t) =>
        {
            float env = ExponentialDecay(t, duration, 12f);
            float freq = Mathf.Lerp(800f, 1400f, t / duration);
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            return sine * env;
        });
    }

    /// <summary>
    /// 金币音效：金属叮当
    /// </summary>
    public static AudioClip GenerateCoin(float duration)
    {
        return CreateClip("SFX_Coin", duration, (t) =>
        {
            float env = ExponentialDecay(t, duration, 20f);
            float sine1 = Mathf.Sin(2f * Mathf.PI * 2500f * t) * 0.4f;
            float sine2 = Mathf.Sin(2f * Mathf.PI * 3700f * t) * 0.3f;
            float sine3 = Mathf.Sin(2f * Mathf.PI * 5000f * t) * 0.2f;
            return (sine1 + sine2 + sine3) * env;
        });
    }

    // ════════════════════════════════════════════════════════════════
    // UI音效
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// UI Beep：纯正弦波
    /// </summary>
    public static AudioClip GenerateUIBeep(float duration, float freq)
    {
        return CreateClip("SFX_UIBeep", duration, (t) =>
        {
            float env = Mathf.Sin(Mathf.PI * t / duration) * 0.5f;
            return Mathf.Sin(2f * Mathf.PI * freq * t) * env;
        });
    }

    /// <summary>
    /// UI 确认音效：双音上升
    /// </summary>
    public static AudioClip GenerateUIConfirm(float duration)
    {
        return CreateClip("SFX_UIConfirm", duration, (t) =>
        {
            float env = Mathf.Sin(Mathf.PI * t / duration);
            float freq = t < duration * 0.5f ? 660f : 880f;
            return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.5f;
        });
    }

    // ════════════════════════════════════════════════════════════════
    // 环境音效
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 波次开始音效：号角式上升
    /// </summary>
    public static AudioClip GenerateWaveStart(float duration)
    {
        return CreateClip("SFX_WaveStart", duration, (t) =>
        {
            float env = t < 0.1f ? t / 0.1f : 1f - (t - 0.1f) / (duration - 0.1f);
            env = Mathf.Clamp01(env);
            float freq = 300f + 200f * Mathf.Sin(2f * Mathf.PI * 2f * t);
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            float harm = Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.3f;
            return (sine * 0.5f + harm * 0.3f) * env;
        });
    }

    /// <summary>
    /// 波次完成音效：胜利铃声
    /// </summary>
    public static AudioClip GenerateWaveComplete(float duration)
    {
        return CreateClip("SFX_WaveComplete", duration, (t) =>
        {
            float env = Mathf.Sin(Mathf.PI * t / duration);
            // C-E-G 和弦
            float c = Mathf.Sin(2f * Mathf.PI * 523f * t) * 0.33f;
            float e = Mathf.Sin(2f * Mathf.PI * 659f * t) * 0.33f;
            float g = Mathf.Sin(2f * Mathf.PI * 784f * t) * 0.33f;
            return (c + e + g) * env;
        });
    }

    // ════════════════════════════════════════════════════════════════
    // 技能音效
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 通用技能释放：魔力波动
    /// </summary>
    public static AudioClip GenerateSkillCast(float duration)
    {
        return CreateClip("SFX_SkillCast", duration, (t) =>
        {
            float env = t < 0.05f ? t / 0.05f : ExponentialDecay(t - 0.05f, duration - 0.05f, 6f);
            float freq = 300f + 400f * t / duration;
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            float noise = Random.Range(-1f, 1f) * 0.15f;
            return (sine * 0.7f + noise * 0.3f) * env;
        });
    }

    /// <summary>
    /// 传送音效：嗖声 + 相位偏移
    /// </summary>
    public static AudioClip GenerateTeleport(float duration)
    {
        return CreateClip("SFX_Teleport", duration, (t) =>
        {
            float env = t < duration * 0.3f ? t / (duration * 0.3f) : ExponentialDecay(t - duration * 0.3f, duration * 0.7f, 8f);
            float freq = Mathf.Lerp(200f, 2000f, t / duration);
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            float whoosh = Random.Range(-1f, 1f) * 0.2f * (1f - t / duration);
            return (sine * 0.6f + whoosh * 0.4f) * env;
        });
    }

    /// <summary>
    /// 暴风雪音效：寒风 + 冰晶
    /// </summary>
    public static AudioClip GenerateFrostNova(float duration)
    {
        return CreateClip("SFX_FrostNova", duration, (t) =>
        {
            float env = t < 0.05f ? t / 0.05f : 1f - (t - 0.05f) / (duration - 0.05f);
            env = Mathf.Clamp01(env);
            float wind = Random.Range(-1f, 1f) * 0.4f;
            float ice = Mathf.Sin(2f * Mathf.PI * 1200f * t) * 0.3f;
            float bass = Mathf.Sin(2f * Mathf.PI * 200f * t) * 0.3f;
            return (wind + ice + bass) * env;
        });
    }

    /// <summary>
    /// 闪电音效：电弧噼啪
    /// </summary>
    public static AudioClip GenerateLightning(float duration)
    {
        return CreateClip("SFX_Lightning", duration, (t) =>
        {
            float env = ExponentialDecay(t, duration, 5f);
            float crack = Random.Range(-1f, 1f);
            // 随机噼啪
            float pop = Mathf.Sin(2f * Mathf.PI * Random.Range(800f, 2000f) * t) * 0.3f;
            float buzz = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.2f;
            return (crack * 0.4f + pop + buzz) * env;
        });
    }

    /// <summary>
    /// 重力井音效：深沉嗡鸣 + 拉伸感
    /// </summary>
    public static AudioClip GenerateGravityWell(float duration)
    {
        return CreateClip("SFX_GravityWell", duration, (t) =>
        {
            float env = t < 0.2f ? t / 0.2f : 1f - (t - 0.2f) / (duration - 0.2f);
            env = Mathf.Clamp01(env);
            float freq = 80f + 40f * Mathf.Sin(2f * Mathf.PI * 3f * t);
            float bass = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f;
            float sub = Mathf.Sin(2f * Mathf.PI * 40f * t) * 0.3f;
            float hum = Random.Range(-1f, 1f) * 0.1f;
            return (bass + sub + hum) * env;
        });
    }

    /// <summary>
    /// 死亡光环音效：不祥低频脉冲
    /// </summary>
    public static AudioClip GenerateDeathAura(float duration)
    {
        return CreateClip("SFX_DeathAura", duration, (t) =>
        {
            float env = t < 0.1f ? t / 0.1f : 1f - (t - 0.1f) / (duration - 0.1f);
            env = Mathf.Clamp01(env);
            float pulse = Mathf.Sin(2f * Mathf.PI * 60f * t) * (0.5f + 0.3f * Mathf.Sin(2f * Mathf.PI * 4f * t));
            float dark = Random.Range(-1f, 1f) * 0.15f;
            return (pulse + dark) * env;
        });
    }

    /// <summary>
    /// 狂暴音效：激昂上升
    /// </summary>
    public static AudioClip GenerateBerserk(float duration)
    {
        return CreateClip("SFX_Berserk", duration, (t) =>
        {
            float env = t < 0.05f ? t / 0.05f : ExponentialDecay(t - 0.05f, duration - 0.05f, 5f);
            float freq = 200f + 600f * (t / duration);
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            float grit = Random.Range(-1f, 1f) * 0.3f;
            return (sine * 0.6f + grit * 0.4f) * env;
        });
    }

    /// <summary>
    /// 时停音效：时空扭曲
    /// </summary>
    public static AudioClip GenerateTheWorld(float duration)
    {
        return CreateClip("SFX_TheWorld", duration, (t) =>
        {
            float env = t < 0.1f ? t / 0.1f : ExponentialDecay(t - 0.1f, duration - 0.1f, 3f);
            // 频率从高到低再到高（扭曲感）
            float curve = Mathf.Sin(Mathf.PI * t / duration);
            float freq = 1000f - 800f * curve + 200f * Mathf.Sin(2f * Mathf.PI * 8f * t);
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            float reverb = Mathf.Sin(2f * Mathf.PI * freq * 0.5f * t) * 0.3f;
            return (sine * 0.5f + reverb * 0.3f) * env;
        });
    }

    /// <summary>
    /// 风浪音效：呼啸风声
    /// </summary>
    public static AudioClip GenerateWindWave(float duration)
    {
        return CreateClip("SFX_WindWave", duration, (t) =>
        {
            float env = t < 0.05f ? t / 0.05f : 1f - (t - 0.05f) / (duration - 0.05f);
            env = Mathf.Clamp01(env);
            float noise = Random.Range(-1f, 1f);
            float whoosh = Mathf.Sin(2f * Mathf.PI * 300f * t) * 0.3f;
            return (noise * 0.5f + whoosh * 0.5f) * env;
        });
    }

    // ════════════════════════════════════════════════════════════════
    // 元素DOT音效
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 流血DOT：湿润撕裂声
    /// </summary>
    public static AudioClip GenerateDotBleed(float duration)
    {
        return CreateClip("SFX_DotBleed", duration, (t) =>
        {
            float env = ExponentialDecay(t, duration, 15f);
            float squelch = Mathf.Sin(2f * Mathf.PI * 200f * t) * 0.3f;
            float noise = Random.Range(-1f, 1f) * 0.5f;
            return (squelch + noise) * env;
        });
    }

    /// <summary>
    /// 中毒DOT：冒泡声
    /// </summary>
    public static AudioClip GenerateDotPoison(float duration)
    {
        return CreateClip("SFX_DotPoison", duration, (t) =>
        {
            float env = ExponentialDecay(t, duration, 10f);
            float bubble = Mathf.Sin(2f * Mathf.PI * (400f + 200f * Mathf.Sin(2f * Mathf.PI * 15f * t)) * t);
            return bubble * 0.4f * env;
        });
    }

    /// <summary>
    /// 燃烧DOT：噼啪火焰声
    /// </summary>
    public static AudioClip GenerateDotBurn(float duration)
    {
        return CreateClip("SFX_DotBurn", duration, (t) =>
        {
            float env = ExponentialDecay(t, duration, 12f);
            float crackle = Random.Range(-1f, 1f) * 0.6f;
            float hiss = Mathf.Sin(2f * Mathf.PI * 3000f * t) * 0.2f * Random.Range(0.5f, 1f);
            return (crackle + hiss) * env;
        });
    }

    /// <summary>
    /// 霜冻DOT：冰晶叮当
    /// </summary>
    public static AudioClip GenerateDotFrost(float duration)
    {
        return CreateClip("SFX_DotFrost", duration, (t) =>
        {
            float env = ExponentialDecay(t, duration, 15f);
            float tinkle = Mathf.Sin(2f * Mathf.PI * 4000f * t) * 0.3f;
            float chime = Mathf.Sin(2f * Mathf.PI * 2500f * t) * 0.2f;
            return (tinkle + chime) * env;
        });
    }

    /// <summary>
    /// 雷电DOT：电弧声
    /// </summary>
    public static AudioClip GenerateDotLightning(float duration)
    {
        return CreateClip("SFX_DotLightning", duration, (t) =>
        {
            float env = ExponentialDecay(t, duration, 10f);
            float zap = Random.Range(-1f, 1f) * 0.5f;
            float buzz = Mathf.Sin(2f * Mathf.PI * 200f * t) * 0.3f;
            return (zap + buzz) * env;
        });
    }

    /// <summary>
    /// 黑暗DOT：低沉嗡鸣
    /// </summary>
    public static AudioClip GenerateDotDark(float duration)
    {
        return CreateClip("SFX_DotDark", duration, (t) =>
        {
            float env = ExponentialDecay(t, duration, 8f);
            float drone = Mathf.Sin(2f * Mathf.PI * 100f * t) * 0.4f;
            float whisper = Random.Range(-1f, 1f) * 0.2f;
            return (drone + whisper) * env;
        });
    }

    /// <summary>
    /// 光明DOT：清脆闪光
    /// </summary>
    public static AudioClip GenerateDotLight(float duration)
    {
        return CreateClip("SFX_DotLight", duration, (t) =>
        {
            float env = ExponentialDecay(t, duration, 12f);
            float shimmer = Mathf.Sin(2f * Mathf.PI * 3000f * t) * 0.3f;
            float bright = Mathf.Sin(2f * Mathf.PI * 1500f * t) * 0.3f;
            return (shimmer + bright) * env;
        });
    }

    // ════════════════════════════════════════════════════════════════
    // 特殊音效
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 连击音效：快速上升
    /// </summary>
    public static AudioClip GenerateCombo(float duration)
    {
        return CreateClip("SFX_Combo", duration, (t) =>
        {
            float env = ExponentialDecay(t, duration, 8f);
            float freq = 600f + 800f * (t / duration);
            return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.5f;
        });
    }

    /// <summary>
    /// 商店购买音效：收银机声
    /// </summary>
    public static AudioClip GenerateShopBuy(float duration)
    {
        return CreateClip("SFX_ShopBuy", duration, (t) =>
        {
            float env = ExponentialDecay(t, duration, 10f);
            float ding = Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.4f;
            float chaChing = Mathf.Sin(2f * Mathf.PI * 2400f * t) * 0.3f;
            return (ding + chaChing) * env;
        });
    }

    /// <summary>
    /// 成就解锁音效：华丽琶音
    /// </summary>
    public static AudioClip GenerateAchievement(float duration)
    {
        return CreateClip("SFX_Achievement", duration, (t) =>
        {
            float env = Mathf.Sin(Mathf.PI * t / duration);
            // C-E-G-C 和弦琶音
            float[] freqs = { 523f, 659f, 784f, 1047f };
            int idx = Mathf.FloorToInt(t / duration * freqs.Length);
            idx = Mathf.Clamp(idx, 0, freqs.Length - 1);
            float sine = Mathf.Sin(2f * Mathf.PI * freqs[idx] * t);
            float harm = Mathf.Sin(2f * Mathf.PI * freqs[idx] * 2f * t) * 0.2f;
            return (sine * 0.6f + harm * 0.2f) * env;
        });
    }

    /// <summary>
    /// 警告音效：急促脉冲
    /// </summary>
    public static AudioClip GenerateWarning(float duration)
    {
        return CreateClip("SFX_Warning", duration, (t) =>
        {
            float env = Mathf.Sin(Mathf.PI * t / duration);
            float pulse = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 8f * t)) * 0.5f + 0.5f;
            float freq = 800f;
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            return sine * pulse * env * 0.5f;
        });
    }

    // ════════════════════════════════════════════════════════════════
    // 底层工具方法
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 创建 AudioClip 并用采样函数填充数据
    /// </summary>
    public static AudioClip CreateClip(string name, float duration, System.Func<float, float> sampleFunc)
    {
        int sampleCount = Mathf.CeilToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            samples[i] = Mathf.Clamp(sampleFunc(t), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// 指数衰减包络
    /// </summary>
    public static float ExponentialDecay(float t, float duration, float rate)
    {
        return Mathf.Exp(-rate * t / duration);
    }
}