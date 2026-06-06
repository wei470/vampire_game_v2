using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 武器控制器 v2 - 支持全部 8 种武器类型。
/// 对应 Python: entities/weapons.py 中的武器系统
/// 
/// 功能：
/// - 读取 WeaponData ScriptableObject 配置
/// - 自动朝鼠标方向射击
/// - 支持切换武器（数字键1-8 / Q键循环）
/// - 支持多弹丸、散射角
/// 
/// 使用方式：挂载到玩家 GameObject 上
/// </summary>
public class WeaponController : MonoBehaviour
{
    [Header("当前武器")]
    [SerializeField] private WeaponData _currentWeapon;

    [Header("备用配置（无 WeaponData 时使用）")]
    [SerializeField] private int _baseDamage = 10;
    [SerializeField] private float _baseCooldown = 0.5f;
    [SerializeField] private float _projectileSpeed = 12f;
    [SerializeField] private float _projectileLifetime = 3f;
    [SerializeField] private int _pierce = 1;
    [SerializeField] private float _attackRange = 20f;
    [SerializeField] private float _knockbackForce = 2f;

    [Header("投射物")]
    [SerializeField] private GameObject _projectilePrefab;

    [Header("武器列表（Debug 切换用）")]
    [SerializeField] private WeaponData[] _weaponSlots = new WeaponData[8];

    [Header("运行时状态")]
    [SerializeField] private float _damageMultiplier = 1f;
    [SerializeField] private float _cooldownMultiplier = 1f;
    [SerializeField] private float _lastFireTime;
    [SerializeField] private int _currentWeaponIndex = 0;
    [SerializeField] private bool _selectionLocked = false;

    /// <summary>
    /// 选择锁定标志。锁定后禁止手动切换武器（1-8/Q键）。
    /// </summary>
    public bool SelectionLocked { get => _selectionLocked; set => _selectionLocked = value; }

    // 属性
    public float DamageMultiplier { get => _damageMultiplier; set => _damageMultiplier = value; }
    public float CooldownMultiplier { get => _cooldownMultiplier; set => _cooldownMultiplier = value; }
    public WeaponData CurrentWeapon => _currentWeapon;
    public int CurrentWeaponIndex => _currentWeaponIndex;

    // 当前武器的实际数值（优先用 WeaponData）
    public float CurrentCooldown => _currentWeapon != null
        ? _currentWeapon.cooldown * _cooldownMultiplier
        : _baseCooldown * _cooldownMultiplier;

    public int CurrentDamage => _currentWeapon != null
        ? Mathf.RoundToInt(_currentWeapon.baseDamage * _damageMultiplier)
        : Mathf.RoundToInt(_baseDamage * _damageMultiplier);

    private void Start()
    {
        _lastFireTime = Time.time;
    }

    private void Update()
    {
        // 检查游戏状态 — 只有 Playing 状态才允许射击
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            return;
        }

        // 武器切换已迁移到 GameInputHandler.HandleWeaponSwitchInput()

        // 自动射击
        if (Time.time - _lastFireTime >= CurrentCooldown)
        {
            Vector2 fireDir = GetFireDirection();
            if (fireDir.sqrMagnitude > 0.01f)
            {
                FireWeapon(fireDir);
                _lastFireTime = Time.time;
            }
        }
    }

    /// <summary>
    /// 切换武器
    /// </summary>
    public void SwitchWeapon(int index)
    {
        if (index < 0 || index >= _weaponSlots.Length) return;

        _currentWeaponIndex = index;
        if (_weaponSlots[index] != null)
        {
            _currentWeapon = _weaponSlots[index];
            DebugHelper.Log($"[WeaponController] Switched to weapon {index}: {_currentWeapon.weaponName}");
        }
        else
        {
            DebugHelper.Log($"[WeaponController] Weapon slot {index} is empty");
        }
    }

    /// <summary>
    /// 设置武器数据
    /// </summary>
    public void SetWeapon(WeaponData weapon)
    {
        _currentWeapon = weapon;
        // 重置射击计时器，防止切换武器后因旧的负值时间自动发射一发子弹
        _lastFireTime = Time.time;
    }

    /// <summary>
    /// 获取朝鼠标方向的射击方向（使用 GameInputHandler 统一计算的鼠标位置）
    /// </summary>
    private Vector2 GetFireDirection()
    {
        if (!GameInputHandler.MouseValid) return transform.right;

        Vector2 dir = (GameInputHandler.MouseWorldPosition - transform.position);
        return dir.normalized;
    }

    /// <summary>
    /// 根据武器类型发射投射物
    /// </summary>
    private void FireWeapon(Vector2 direction)
    {
        if (_currentWeapon != null)
        {
            FireWeaponData(direction);
        }
        else
        {
            // 无 WeaponData，使用旧逻辑（基础子弹）
            FireDefaultProjectile(direction);
        }

        DebugHelper.Log($"[WeaponController] Fired weapon, damage: {CurrentDamage}");
    }

    /// <summary>
    /// 根据 WeaponData 发射
    /// </summary>
    private void FireWeaponData(Vector2 direction)
    {
        var data = _currentWeapon;
        int count = data.projectileCount;
        float spread = data.spreadAngle;
        Vector2 pos = transform.position;

        for (int i = 0; i < count; i++)
        {
            Vector2 fireDir = direction;

            // 散射角度
            if (count > 1 && spread > 0)
            {
                float angleStep = spread / (count - 1);
                float angle = -spread / 2f + angleStep * i;
                fireDir = WeaponProjectileFactory.RotateVector2(direction, angle);
            }

            switch (data.projectileType)
            {
                case WeaponData.ProjectileType.Bullet:
                    WeaponProjectileFactory.SpawnBullet(pos, fireDir, CurrentDamage, data);
                    break;
                case WeaponData.ProjectileType.ChainLightning:
                    WeaponProjectileFactory.SpawnLightning(pos, fireDir, CurrentDamage, data);
                    break;
                case WeaponData.ProjectileType.Shockwave:
                    WeaponProjectileFactory.SpawnShockwave(pos, CurrentDamage, data);
                    break;
                case WeaponData.ProjectileType.HomingMissile:
                    WeaponProjectileFactory.SpawnHomingMissile(pos, fireDir, CurrentDamage, data);
                    break;
                case WeaponData.ProjectileType.MineTrap:
                    WeaponProjectileFactory.SpawnMineTrap(pos, GetFireDirection(), CurrentDamage, data);
                    break;
                case WeaponData.ProjectileType.Flamethrower:
                    WeaponProjectileFactory.SpawnFireZone(pos, fireDir, CurrentDamage, data);
                    break;
                case WeaponData.ProjectileType.FrostOrb:
                    WeaponProjectileFactory.SpawnFrostOrb(pos, fireDir, CurrentDamage, data);
                    break;
                case WeaponData.ProjectileType.VenomDart:
                    WeaponProjectileFactory.SpawnVenomDart(pos, fireDir, CurrentDamage, data);
                    break;
            }
        }
    }

    #region 默认发射（无 WeaponData）

    private void FireDefaultProjectile(Vector2 direction)
    {
        GameObject bulletGo;

        if (_projectilePrefab != null)
        {
            bulletGo = Instantiate(_projectilePrefab, transform.position, Quaternion.identity);
        }
        else
        {
            bulletGo = WeaponProjectileFactory.CreateDefaultProjectileGO(transform.position);
        }

        var proj = bulletGo.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Setup(CurrentDamage, _projectileSpeed, _projectileLifetime, _pierce);
            proj.SetDamageMultiplier(1f);
            proj.SetKnockback(_knockbackForce);
            proj.SetDirection(direction);
        }
    }

    #endregion

    /// <summary>
    /// 设置武器属性（来自升级或选择）
    /// </summary>
    public void Setup(int damage, float cooldown, float speed, int pierce)
    {
        _baseDamage = damage;
        _baseCooldown = cooldown;
        _projectileSpeed = speed;
        _pierce = pierce;
    }


    /// <summary>
    /// 刷新当前武器属性（武器升级后调用）
    /// 重新应用 WeaponData 的最新数值
    /// </summary>
    public void RefreshCurrentWeapon()
    {
        if (_currentWeapon == null) return;
        // WeaponData 是 ScriptableObject，数值已在 ApplyUpgrade 中直接修改
        // 这里只需重置射击计时器，让新冷却立即生效
        _lastFireTime = Time.time - CurrentCooldown;
        DebugHelper.Log($"[WeaponController] Weapon refreshed: {_currentWeapon.weaponName} Lv.{_currentWeapon.UpgradeLevel} DMG={CurrentDamage} CD={CurrentCooldown:F2}s");
    }

    /// <summary>
    /// 获取所有武器数据（供 SelectionFlowManager 使用）
    /// </summary>
    public WeaponData[] GetAllWeaponData()
    {
        return _weaponSlots;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        float range = _currentWeapon != null ? _currentWeapon.attackRange : _attackRange;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}