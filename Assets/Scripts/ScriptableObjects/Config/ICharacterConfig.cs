/// <summary>
/// 角色配置接口 — 每个角色有自己的升级配置 ScriptableObject。
///
/// MageUpgradeConfig 实现此接口。
/// 新角色（WarriorUpgradeConfig 等）也实现此接口。
/// </summary>
public interface ICharacterConfig
{
    /// <summary>角色ID（与 CharacterData.characterId 一致）</summary>
    string CharacterId { get; }

    /// <summary>获取所有升级选项</summary>
    CharacterUpgradeOption[] GetUpgradeOptions();

    /// <summary>获取所有 DOT 枪械条目（Mage 专属，其他角色可返回空）</summary>
    DotGunEntry[] GetGunEntries();
}
