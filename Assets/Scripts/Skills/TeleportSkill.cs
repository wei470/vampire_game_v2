using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 瞬移技能 - 瞬间移动到鼠标位置。
/// 对应 Python: Teleport skill
/// 
/// 效果：
/// - 玩家瞬间移动到鼠标指向的位置
/// - 瞬移时对落点周围造成小范围伤害
/// - 视觉：闪现特效
/// </summary>
public class TeleportSkill : BaseSkill
{
    [SerializeField] private float _maxRange = 15f;

    protected override void Activate()
    {
        if (_playerTransform == null) return;

        // 获取鼠标位置并转换为世界坐标
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        var cam = GameReferences.MainCamera ?? Camera.main;
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f;

        // 限制最大距离
        Vector3 direction = mouseWorldPos - _playerTransform.position;
        float distance = direction.magnitude;
        if (distance > _maxRange)
        {
            mouseWorldPos = _playerTransform.position + direction.normalized * _maxRange;
        }

        // 瞬移前效果
        CombatManager.CreateExplosionEffect(
            _playerTransform.position, 1f,
            new Color(0.3f, 0.7f, 1f, 0.8f), 0.2f);

        // 执行瞬移
        Vector3 oldPos = _playerTransform.position;
        _playerTransform.position = mouseWorldPos;

        // 更新刚体位置
        var rb = _playerController.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // 瞬移后对落点造成小范围伤害
        int damage = GetDamage();
        if (damage > 0)
        {
            CombatManager.DealAoEDamage(mouseWorldPos, _skillData.effectRadius, damage);
        }

        // 瞬移后效果
        CombatManager.CreateExplosionEffect(
            mouseWorldPos, 1.5f,
            new Color(0.3f, 0.7f, 1f, 0.8f), 0.3f);

        DebugHelper.Log($"[Teleport] {oldPos} -> {mouseWorldPos}, damage={damage}");
    }
}