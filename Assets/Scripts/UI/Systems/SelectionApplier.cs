using UnityEngine;

/// <summary>
/// 选择应用器 — 监听 SelectionFlowManager 的完成事件，
/// 将玩家选择的角色/武器/技能应用到游戏中的玩家实体上。
/// 
/// 解决问题：选择流程完成后，选择数据需要真正影响游戏玩法：
/// 1. 角色属性（HP/速度/护甲/攻击等）应用到 PlayerController + Damageable
/// 2. 武器切换到玩家选择的武器（锁定1-8切换）
/// 3. 技能绑定到 PlayerSkillManager
/// 
/// 使用方式：挂载到场景中的 GameObject 上
/// </summary>
public class SelectionApplier : MonoBehaviour
{
    [Header("引用（自动查找）")]
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private WeaponController _weaponController;
    [SerializeField] private PlayerSkillManager _skillManager;
    [SerializeField] private Damageable _playerDamageable;

    private void OnEnable()
    {
        EventManager.OnSelectionComplete += OnSelectionComplete;
    }

    private void OnDisable()
    {
        EventManager.OnSelectionComplete -= OnSelectionComplete;
    }

    private void Start()
    {
        // 延迟查找引用（确保场景中所有对象已初始化）
        FindReferences();
    }

    private void FindReferences()
    {
        // 使用 Unity null 检查而非 C# ?. 运算符，避免已销毁对象的 MissingReferenceException
        var player = GameReferences.Player;
        if (player != null)
        {
            if (_playerController == null)
                _playerController = player;
            if (_weaponController == null)
                _weaponController = player.GetComponent<WeaponController>();
            if (_skillManager == null)
                _skillManager = player.GetComponent<PlayerSkillManager>();
            if (_playerDamageable == null && _playerController != null)
                _playerDamageable = _playerController.GetComponent<Damageable>();
        }
    }

    /// <summary>
    /// 选择完成回调 — 应用所有选择到玩家
    /// </summary>
    private void OnSelectionComplete(CharacterData character, WeaponData weapon, SkillData skill)
    {
        FindReferences();

        DebugHelper.Log("[SelectionApplier] Applying selections to player...");

        // 1. 应用角色属性
        if (character != null)
        {
            ApplyCharacter(character);
        }

        // 2. 应用武器选择
        if (weapon != null)
        {
            ApplyWeapon(weapon);
        }

        // 3. 应用技能选择
        if (skill != null)
        {
            ApplySkill(skill);
        }

        DebugHelper.Log("[SelectionApplier] All selections applied successfully!");
    }

    /// <summary>
    /// 应用角色属性到玩家
    /// </summary>
    private void ApplyCharacter(CharacterData character)
    {
        if (_playerController == null || _playerDamageable == null)
        {
            DebugHelper.LogWarning("[SelectionApplier] PlayerController or Damageable not found, cannot apply character.");
            return;
        }

        // 设置移动速度
        _playerController.MoveSpeed = character.moveSpeed;

        // 设置 HP
        _playerDamageable.SetMaxHp(character.maxHP);
        _playerDamageable.Heal(character.maxHP); // 恢复到满血

        // 设置护甲
        _playerDamageable.SetArmor(character.armor);

        // 设置玩家颜色
        var sr = _playerController.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = character.characterColor;
        }

        DebugHelper.Log(string.Format("[SelectionApplier] Character applied: {0} (HP:{1}, Speed:{2}, Armor:{3}, ATK:{4})",
            character.characterName, character.maxHP, character.moveSpeed, character.armor, character.attackDamage));
    }

    /// <summary>
    /// 应用武器选择到玩家
    /// </summary>
    private void ApplyWeapon(WeaponData weapon)
    {
        if (_weaponController == null)
        {
            DebugHelper.LogWarning("[SelectionApplier] WeaponController not found, cannot apply weapon.");
            return;
        }

        // 设置当前武器
        _weaponController.SetWeapon(weapon);

        // 锁定武器切换（禁止1-8手动切换）
        _weaponController.SelectionLocked = true;

        DebugHelper.Log(string.Format("[SelectionApplier] Weapon applied: {0} (Damage:{1}, CD:{2}s, Type:{3})",
            weapon.weaponName, weapon.baseDamage, weapon.cooldown, weapon.projectileType));
    }

    /// <summary>
    /// 应用技能选择到玩家
    /// 注意：GameSceneBootstrap 已经在 OnSelectionComplete 事件之前应用了技能，
    /// 所以这里只在技能尚未添加时才应用（避免重复 ClearAll + Add 导致失败）。
    /// </summary>
    private void ApplySkill(SkillData skill)
    {
        if (_skillManager == null)
        {
            DebugHelper.LogWarning("[SelectionApplier] PlayerSkillManager not found, cannot apply skill.");
            return;
        }

        // 如果已经有技能（Bootstrap 已添加），跳过重复添加
        if (_skillManager.ActiveSkills.Count > 0)
        {
            DebugHelper.Log(string.Format("[SelectionApplier] Skill already applied by Bootstrap: {0}", skill.skillName));
            return;
        }

        // 清除已有技能，然后添加选择的技能
        _skillManager.ClearAllSkills();
        var addedSkill = _skillManager.AddSkillByData(skill);

        if (addedSkill != null)
        {
            DebugHelper.Log(string.Format("[SelectionApplier] Skill applied: {0} (Damage:{1}, CD:{2}s, Radius:{3})",
                skill.skillName, skill.baseDamage, skill.cooldown, skill.effectRadius));
        }
        else
        {
            DebugHelper.LogWarning("[SelectionApplier] Failed to add skill: " + skill.skillName);
        }
    }
}