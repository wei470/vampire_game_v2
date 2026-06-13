/// <summary>
/// 进化处理器接口 — 每个角色实现自己的进化效果。
///
/// EvolutionSystem 在触发进化里程碑时调用对应处理器，
/// 而非硬编码每个角色的进化逻辑。
/// </summary>
public interface IEvolutionHandler
{
    /// <summary>
    /// 应用进化效果
    /// </summary>
    /// <param name="milestone">触发的里程碑</param>
    /// <param name="character">角色被动组件</param>
    void ApplyEvolution(EvolutionMilestone milestone, ICharacterPassive character);

    /// <summary>
    /// 重置进化状态（新一局游戏时调用）
    /// </summary>
    void ResetEvolutions();
}
