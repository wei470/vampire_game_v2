using UnityEngine;

/// <summary>
/// 风波技能 - 以玩家为中心向外推开并伤害周围敌人。
/// 对应 Python: WindWave skill
/// 
/// 效果：
/// - 以玩家为中心产生圆形冲击波
/// - 推开范围内所有敌人（击退）
/// - 造成伤害
/// </summary>
public class WindWaveSkill : BaseSkill
{
    protected override void Activate()
    {
        if (_playerTransform == null) return;

        Vector2 center = _playerTransform.position;
        int damage = GetDamage();
        float radius = _skillData.effectRadius;
        float knockback = _skillData.effectStrength * 5f;

        // 使用 CombatManager 的 AoE 伤害
        int hitCount = CombatManager.DealAoEDamage(center, radius, damage, 1f, knockback);

        // 视觉效果 - 扩散圆环
        CreateWindEffect(center, radius);

        DebugHelper.Log($"[WindWave] Hit {hitCount} enemies, damage={damage}, radius={radius}");
    }

    private void CreateWindEffect(Vector2 center, float radius)
    {
        CombatManager.CreateExplosionEffect(center, radius, _skillData.skillColor, 0.4f);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        // 升级后效果范围增加
        // 由 GetDamage() 和 effectRadius 在 SkillData 中通过等级计算
    }
}