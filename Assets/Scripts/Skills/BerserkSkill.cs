using UnityEngine;

/// <summary>
/// 狂暴技能 - 临时提升攻击力和移动速度。
/// 对应 Python: Berserk skill
/// 
/// 效果：
/// - 持续时间内：攻击力倍率提升
/// - 持续时间内：移动速度提升
/// - 视觉：玩家变红
/// </summary>
public class BerserkSkill : BaseSkill
{
    private float _originalMoveSpeed;
    private float _originalDamageMultiplier;
    private SpriteRenderer _playerSprite;
    private Color _originalColor;

    protected override void Activate()
    {
        if (_playerController == null) return;

        float buffMultiplier = GetEffectStrength();

        // 保存原始值
        _originalMoveSpeed = _playerController.MoveSpeed;

        // 提升移动速度
        _playerController.MoveSpeed *= (1f + buffMultiplier * 0.3f);

        // 提升全局伤害倍率
        if (CombatManager.Instance != null)
        {
            _originalDamageMultiplier = CombatManager.Instance.GlobalDamageMultiplier;
            CombatManager.Instance.GlobalDamageMultiplier *= (1f + buffMultiplier * 0.5f);
        }

        // 视觉效果 - 变红
        _playerSprite = _playerController.GetComponent<SpriteRenderer>();
        if (_playerSprite != null)
        {
            _originalColor = _playerSprite.color;
            _playerSprite.color = new Color(1f, 0.3f, 0.3f, 1f);
        }

        DebugHelper.Log($"[Berserk] Activated! ATK x{1f + buffMultiplier * 0.5f:F2}, Speed x{1f + buffMultiplier * 0.3f:F2}");
    }

    protected override void Deactivate()
    {
        base.Deactivate();

        // 恢复原始值
        if (_playerController != null)
        {
            _playerController.MoveSpeed = _originalMoveSpeed;
        }

        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.GlobalDamageMultiplier = _originalDamageMultiplier;
        }

        // 恢复颜色
        if (_playerSprite != null)
        {
            _playerSprite.color = _originalColor;
        }

        DebugHelper.Log("[Berserk] Deactivated.");
    }
}