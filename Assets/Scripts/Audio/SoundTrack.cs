using UnityEngine;
using System;

/// <summary>
/// 音效配置包 — 统一管理所有游戏音效的 AudioClip、音量、音高偏移和冷却时间。
/// 作为 ScriptableObject 创建，可在编辑器中通过 Assets → Create → Audio → SoundTrack 创建。
/// 由 SFXManager 和 BGMManager 引用，实现音效配置与播放逻辑分离。
/// 
/// 使用方式：
/// 1. 在 Project 窗口右键 Create → Audio → SoundTrack 创建配置资产
/// 2. 将音频文件拖拽到对应字段
/// 3. 调整音量、音高偏移、冷却时间等参数
/// 4. 将 SoundTrack 资产拖拽到 SFXManager 的 _soundTrack 字段
/// </summary>
[CreateAssetMenu(fileName = "SoundTrack", menuName = "Audio/SoundTrack")]
public class SoundTrack : ScriptableObject
{
    // ════════════════════════════════════════════════════════════════
    // 音效条目数据结构
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 单个音效条目，包含音频剪辑和播放参数
    /// </summary>
    [Serializable]
    public class SoundEntry
    {
        [Tooltip("音频剪辑")]
        public AudioClip clip;

        [Tooltip("基础音量（0~1）")]
        [Range(0f, 1f)]
        public float volume = 1f;

        [Tooltip("音高随机偏移范围（±值），0表示不随机")]
        [Range(0f, 0.2f)]
        public float pitchVariance = 0.05f;

        [Tooltip("冷却时间（秒），防止音效轰炸。0表示无冷却")]
        [Min(0f)]
        public float cooldown = 0f;

        /// <summary>
        /// 是否有有效的音频剪辑
        /// </summary>
        public bool IsValid => clip != null;
    }

    // ════════════════════════════════════════════════════════════════
    // 全局音量设置
    // ════════════════════════════════════════════════════════════════

    [Header("全局音量")]
    [Tooltip("主音量")]
    [Range(0f, 1f)]
    public float masterVolume = 0.7f;

    [Tooltip("音效音量")]
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    [Tooltip("BGM 音量")]
    [Range(0f, 1f)]
    public float bgmVolume = 0.5f;

    // ════════════════════════════════════════════════════════════════
    // 战斗音效
    // ════════════════════════════════════════════════════════════════

    [Header("═══ 战斗音效 ═══")]
    [Tooltip("子弹命中音效")]
    public SoundEntry hit = new SoundEntry { volume = 0.5f, cooldown = 0.05f };

    [Tooltip("暴击音效")]
    public SoundEntry crit = new SoundEntry { volume = 1f, cooldown = 0.05f };

    [Tooltip("引爆音效（Mage引爆系统）")]
    public SoundEntry detonate = new SoundEntry { volume = 1f };

    [Tooltip("DOT持续伤害音效")]
    public SoundEntry dotTick = new SoundEntry { volume = 0.2f, cooldown = 0.1f };

    // ════════════════════════════════════════════════════════════════
    // 敌人音效
    // ════════════════════════════════════════════════════════════════

    [Header("═══ 敌人音效 ═══")]
    [Tooltip("敌人死亡音效")]
    public SoundEntry enemyDeath = new SoundEntry { volume = 0.7f, cooldown = 0.08f };

    [Tooltip("Boss出场音效")]
    public SoundEntry bossSpawn = new SoundEntry { volume = 1f };

    // ════════════════════════════════════════════════════════════════
    // 玩家音效
    // ════════════════════════════════════════════════════════════════

    [Header("═══ 玩家音效 ═══")]
    [Tooltip("玩家受伤音效")]
    public SoundEntry playerHurt = new SoundEntry { volume = 0.8f };

    [Tooltip("玩家治疗音效")]
    public SoundEntry playerHeal = new SoundEntry { volume = 0.6f };

    [Tooltip("升级音效")]
    public SoundEntry levelUp = new SoundEntry { volume = 1f };

    [Tooltip("拾取道具音效")]
    public SoundEntry pickup = new SoundEntry { volume = 0.5f };

    [Tooltip("拾取金币音效")]
    public SoundEntry coin = new SoundEntry { volume = 0.3f };

    // ════════════════════════════════════════════════════════════════
    // UI音效
    // ════════════════════════════════════════════════════════════════

    [Header("═══ UI音效 ═══")]
    [Tooltip("选择/悬停音效")]
    public SoundEntry select = new SoundEntry { volume = 0.6f };

    [Tooltip("确认音效")]
    public SoundEntry confirm = new SoundEntry { volume = 0.8f };

    [Tooltip("暂停音效")]
    public SoundEntry pause = new SoundEntry { volume = 0.5f };

    // ════════════════════════════════════════════════════════════════
    // 环境音效
    // ════════════════════════════════════════════════════════════════

    [Header("═══ 环境音效 ═══")]
    [Tooltip("波次开始音效")]
    public SoundEntry waveStart = new SoundEntry { volume = 0.8f };

    [Tooltip("波次完成音效")]
    public SoundEntry waveComplete = new SoundEntry { volume = 0.7f };

    // ════════════════════════════════════════════════════════════════
    // BGM 曲目
    // ════════════════════════════════════════════════════════════════

    [Header("═══ 背景音乐 ═══")]
    [Tooltip("BGM 曲目列表（按顺序或随机播放）")]
    public AudioClip[] bgmClips;

    [Tooltip("是否随机打乱 BGM 播放顺序")]
    public bool bgmShuffle = true;

    // ════════════════════════════════════════════════════════════════
    // 技能音效（扩展）
    // ════════════════════════════════════════════════════════════════

    [Header("═══ 技能音效 ═══")]
    [Tooltip("技能释放通用音效")]
    public SoundEntry skillCast = new SoundEntry { volume = 0.7f };

    [Tooltip("传送技能音效")]
    public SoundEntry skillTeleport = new SoundEntry { volume = 0.8f };

    [Tooltip("暴风雪技能音效")]
    public SoundEntry skillFrostNova = new SoundEntry { volume = 0.8f };

    [Tooltip("闪电风暴技能音效")]
    public SoundEntry skillLightning = new SoundEntry { volume = 0.8f };

    [Tooltip("重力井技能音效")]
    public SoundEntry skillGravity = new SoundEntry { volume = 0.7f };

    [Tooltip("死亡光环技能音效")]
    public SoundEntry skillDeathAura = new SoundEntry { volume = 0.7f };

    [Tooltip("狂暴技能音效")]
    public SoundEntry skillBerserk = new SoundEntry { volume = 0.9f };

    [Tooltip("时停技能音效")]
    public SoundEntry skillTheWorld = new SoundEntry { volume = 1f };

    [Tooltip("风浪技能音效")]
    public SoundEntry skillWindWave = new SoundEntry { volume = 0.7f };

    // ════════════════════════════════════════════════════════════════
    // 元素DOT音效（扩展）
    // ════════════════════════════════════════════════════════════════

    [Header("═══ 元素DOT音效 ═══")]
    [Tooltip("流血DOT音效")]
    public SoundEntry dotBleed = new SoundEntry { volume = 0.3f, cooldown = 0.15f };

    [Tooltip("中毒DOT音效")]
    public SoundEntry dotPoison = new SoundEntry { volume = 0.3f, cooldown = 0.15f };

    [Tooltip("燃烧DOT音效")]
    public SoundEntry dotBurn = new SoundEntry { volume = 0.3f, cooldown = 0.1f };

    [Tooltip("霜冻DOT音效")]
    public SoundEntry dotFrost = new SoundEntry { volume = 0.3f, cooldown = 0.2f };

    [Tooltip("雷电DOT音效")]
    public SoundEntry dotLightning = new SoundEntry { volume = 0.4f, cooldown = 0.1f };

    [Tooltip("黑暗DOT音效")]
    public SoundEntry dotDark = new SoundEntry { volume = 0.3f, cooldown = 0.2f };

    [Tooltip("光明DOT音效")]
    public SoundEntry dotLight = new SoundEntry { volume = 0.3f, cooldown = 0.15f };

    // ════════════════════════════════════════════════════════════════
    // 特殊音效（扩展）
    // ════════════════════════════════════════════════════════════════

    [Header("═══ 特殊音效 ═══")]
    [Tooltip("连击音效")]
    public SoundEntry combo = new SoundEntry { volume = 0.6f };

    [Tooltip("商店购买音效")]
    public SoundEntry shopBuy = new SoundEntry { volume = 0.7f };

    [Tooltip("成就解锁音效")]
    public SoundEntry achievement = new SoundEntry { volume = 0.8f };

    [Tooltip("警告音效（Boss接近等）")]
    public SoundEntry warning = new SoundEntry { volume = 0.9f };

    // ════════════════════════════════════════════════════════════════
    // 查询 API
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 获取音效条目。如果条目无效（clip为空），返回null。
    /// </summary>
    /// <param name="entry">音效条目</param>
    /// <returns>有效的音效条目，或null</returns>
    public SoundEntry GetEntry(SoundEntry entry)
    {
        return entry != null && entry.IsValid ? entry : null;
    }

    /// <summary>
    /// 获取所有有效的音效条目（用于编辑器验证）
    /// </summary>
    public int GetValidEntryCount()
    {
        int count = 0;
        if (hit.IsValid) count++;
        if (crit.IsValid) count++;
        if (detonate.IsValid) count++;
        if (dotTick.IsValid) count++;
        if (enemyDeath.IsValid) count++;
        if (bossSpawn.IsValid) count++;
        if (playerHurt.IsValid) count++;
        if (playerHeal.IsValid) count++;
        if (levelUp.IsValid) count++;
        if (pickup.IsValid) count++;
        if (coin.IsValid) count++;
        if (select.IsValid) count++;
        if (confirm.IsValid) count++;
        if (pause.IsValid) count++;
        if (waveStart.IsValid) count++;
        if (waveComplete.IsValid) count++;
        if (skillCast.IsValid) count++;
        if (skillTeleport.IsValid) count++;
        if (skillFrostNova.IsValid) count++;
        if (skillLightning.IsValid) count++;
        if (skillGravity.IsValid) count++;
        if (skillDeathAura.IsValid) count++;
        if (skillBerserk.IsValid) count++;
        if (skillTheWorld.IsValid) count++;
        if (skillWindWave.IsValid) count++;
        if (dotBleed.IsValid) count++;
        if (dotPoison.IsValid) count++;
        if (dotBurn.IsValid) count++;
        if (dotFrost.IsValid) count++;
        if (dotLightning.IsValid) count++;
        if (dotDark.IsValid) count++;
        if (dotLight.IsValid) count++;
        if (combo.IsValid) count++;
        if (shopBuy.IsValid) count++;
        if (achievement.IsValid) count++;
        if (warning.IsValid) count++;
        return count;
    }
}