using UnityEngine;

/// <summary>
/// Glow 回收助手 — 挂在 Glow 对象上，当 Glow 的父对象（子弹）被销毁/禁用时自动回到池中
/// 解决 #15 Glow 对象池子弹销毁时未回收问题
/// </summary>
public class GlowReturnHelper : MonoBehaviour
{
    private void OnDisable()
    {
        // 当 Glow 被禁用（父对象销毁或禁用时自动触发），回到池中
        MagePassive.ReturnGlowToPool(gameObject);
    }
}
