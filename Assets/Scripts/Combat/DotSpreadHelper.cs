using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DOT 传播公共方法 — DarkMarkEffect 和 CurseSpreadSystem 共用。
/// </summary>
public static class DotSpreadHelper
{
    private static readonly List<Collider2D> _buffer = new List<Collider2D>(16);

    /// <summary>
    /// 将源敌人的 DOT 效果传播给周围敌人
    /// </summary>
    /// <param name="source">源敌人（携带DOT的敌人）</param>
    /// <param name="position">传播中心位置</param>
    /// <param name="radius">传播范围</param>
    /// <param name="efficiency">传播效率（0~1）</param>
    /// <returns>传播目标数量</returns>
    public static int SpreadEffects(GameObject source, Vector2 position, float radius, float efficiency)
    {
        int spreadCount = 0;
        int count = PhysicsHelper.OverlapCircle(position, radius, _buffer);
        
        for (int i = 0; i < count; i++)
        {
            var col = _buffer[i];
            if (!col.CompareTag("Enemy")) continue;
            if (col.gameObject == source) continue;
            
            var d = col.GetComponent<Damageable>();
            if (d == null || d.CurrentHp <= 0) continue;

            CopyEffects(source, col.gameObject, efficiency);
            spreadCount++;
        }
        
        return spreadCount;
    }

    private static void CopyEffects(GameObject source, GameObject target, float efficiency)
    {
        // Copy burn
        var srcBurn = source.GetComponent<BurnStackEffect>();
        if (srcBurn != null && srcBurn.StackCount > 0)
        {
            var tgtBurn = target.GetComponent<BurnStackEffect>();
            if (tgtBurn == null) tgtBurn = target.AddComponent<BurnStackEffect>();
            int stacks = Mathf.Max(1, Mathf.RoundToInt(srcBurn.StackCount * efficiency));
            for (int s = 0; s < stacks; s++)
                tgtBurn.AddStack(srcBurn.baseDps * efficiency, srcBurn.duration, false, 0f, 0f);
        }

        // Copy poison
        var srcPoison = source.GetComponent<PoisonStackEffect>();
        if (srcPoison != null && srcPoison.StackCount > 0)
        {
            var tgtPoison = target.GetComponent<PoisonStackEffect>();
            if (tgtPoison == null) tgtPoison = target.AddComponent<PoisonStackEffect>();
            int stacks = Mathf.Max(1, Mathf.RoundToInt(srcPoison.StackCount * efficiency));
            for (int s = 0; s < stacks; s++)
                tgtPoison.AddStack(0, 0, false, 0f, 0f);
        }

        // Copy frost
        var srcFrost = source.GetComponent<FrostEffect>();
        if (srcFrost != null && srcFrost.StackCount > 0)
        {
            var tgtFrost = target.GetComponent<FrostEffect>();
            if (tgtFrost == null) tgtFrost = target.AddComponent<FrostEffect>();
            int stacks = Mathf.Max(1, Mathf.RoundToInt(srcFrost.StackCount * efficiency));
            for (int s = 0; s < stacks; s++)
                tgtFrost.ApplyFreeze(0, 0.3f, 0, false, 0f, 0f);
        }

        // Copy static
        var srcStatic = source.GetComponent<StaticStackEffect>();
        if (srcStatic != null && srcStatic.StackCount > 0)
        {
            var tgtStatic = target.GetComponent<StaticStackEffect>();
            if (tgtStatic == null) tgtStatic = target.AddComponent<StaticStackEffect>();
            int stacks = Mathf.Max(1, Mathf.RoundToInt(srcStatic.StackCount * efficiency));
            for (int s = 0; s < stacks; s++)
                tgtStatic.AddStack();
        }
    }
}
