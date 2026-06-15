using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 角色被动能力基类 — 提取所有角色共享的通用字段和方法。
///
/// 通用字段：攻速/弹数/弹体/击退/贯穿
/// 通用方法：GetAttackSpeedMultiplier / ApplyUpgrade / ClearAllDotGuns
/// DOT 方法给默认实现（非 DOT 角色无需重写）
///
/// 新角色继承此类，只需实现 CharacterId/DisplayName 和角色专属逻辑。
/// </summary>
public abstract class CharacterPassiveBase : MonoBehaviour, ICharacterPassive
{
    // ── 角色标识（子类必须实现）──
    public abstract string CharacterId { get; }
    public abstract string DisplayName { get; }

    // ── 通用子弹增强属性 ──
    [Header("子弹增强属性")]
    [SerializeField] protected float _attackSpeedBonus = 0f;
    [SerializeField] protected int _bulletCountBonus = 0;
    [SerializeField] protected float _bulletSizeBonus = 0f;
    [SerializeField] protected float _knockbackBonus = 0f;
    [SerializeField] protected int _penetrateCount = 0;

    // ── 进化系统属性 ──
    public virtual float DotDamageMultiplier { get; set; } = 0f;
    public virtual float CritChanceBonus { get; set; } = 0f;

    // ── 通用属性访问器 ──
    public float AttackSpeedBonus { get => _attackSpeedBonus; set => _attackSpeedBonus = value; }
    public int BulletCountBonus { get => _bulletCountBonus; set => _bulletCountBonus = value; }
    public int PiercingBonus { get; set; } = 0;
    public float BulletSizeBonus { get => _bulletSizeBonus; set => _bulletSizeBonus = value; }
    public float KnockbackBonus { get => _knockbackBonus; set => _knockbackBonus = value; }
    public int PenetrateCount { get => _penetrateCount; set => _penetrateCount = value; }

    // ── 武器控制器缓存 ──
    protected WeaponController _weaponController;

    private const float MIN_ATTACK_SPEED_MULT = 0.2f;

    // ── ICharacterPassive 通用实现 ──

    public virtual float GetAttackSpeedMultiplier()
    {
        // 对数递减：每层急速都有收益，不会触底
        // bonus=0→1.0, bonus=0.15→0.87, bonus=0.9→0.53, bonus=4.5→0.18
        float bonus = Mathf.Max(0f, _attackSpeedBonus);
        return Mathf.Max(MIN_ATTACK_SPEED_MULT, 1f / (1f + bonus));
    }

    public virtual int GetBulletCountBonus() => _bulletCountBonus;
    public virtual float GetBulletSizeBonus() => _bulletSizeBonus;
    public virtual float GetKnockbackBonus() => _knockbackBonus;
    public virtual int GetPenetrateCount() => _penetrateCount;

    /// <summary>
    /// 升级应用（子类重写以处理角色专属升级）
    /// </summary>
    public virtual bool ApplyUpgrade(string upgradeId) { return false; }

    // ── 生命周期 ──

    protected virtual void Awake()
    {
        _weaponController = GetComponent<WeaponController>();
    }

    /// <summary>
    /// 获取朝鼠标/最近敌人的射击方向
    /// </summary>
    protected Vector2 GetFireDirection()
    {
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            var cam = GameReferences.MainCamera;
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                Vector3 screenPos = mouse.position.ReadValue();
                screenPos.z = Mathf.Abs(cam.transform.position.z);
                Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);
                Vector2 dir = ((Vector2)worldPos - (Vector2)transform.position);
                if (dir.sqrMagnitude > 0.01f) return dir.normalized;
            }
        }

        float minDist = float.MaxValue;
        Vector2 nearest = Vector2.zero;
        var enemies = EnemyBase.AllAlive;
        for (int i = 0; i < enemies.Count; i++)
        {
            var eb = enemies[i];
            if (eb == null || !eb.Alive) continue;
            float d = Vector2.Distance(transform.position, eb.transform.position);
            if (d < minDist) { minDist = d; nearest = eb.transform.position; }
        }
        if (minDist < float.MaxValue)
            return (nearest - (Vector2)transform.position).normalized;

        return (Vector2)transform.right;
    }
}
