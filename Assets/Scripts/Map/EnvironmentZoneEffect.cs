using UnityEngine;

/// <summary>
/// 环境区域效果工具类，封装各种环境效果的应用/移除逻辑。
/// 从 EnvironmentZone 中提取，减少主类代码量。
/// </summary>
public static class EnvironmentZoneEffect
{
    /// <summary>
    /// 应用/移除减速效果
    /// </summary>
    public static void ApplySlow(Collider2D entity, float effectStrength, bool apply)
    {
        var player = entity.GetComponent<PlayerController>();
        if (player == null) return;

        if (apply)
        {
            float slowMultiplier = 1f - effectStrength;
            player.MoveSpeed *= slowMultiplier;
            DebugHelper.Log($"[EnvironmentZone] Applied slow to {entity.name}, speed reduced by {effectStrength * 100}%");
        }
        else
        {
            float slowMultiplier = 1f - effectStrength;
            player.MoveSpeed /= slowMultiplier;
            DebugHelper.Log($"[EnvironmentZone] Removed slow from {entity.name}");
        }
    }

    /// <summary>
    /// 应用/移除加速效果
    /// </summary>
    public static void ApplySpeed(Collider2D entity, float effectStrength, bool apply)
    {
        var player = entity.GetComponent<PlayerController>();
        if (player == null) return;

        if (apply)
        {
            player.MoveSpeed *= (1f + effectStrength);
            DebugHelper.Log($"[EnvironmentZone] Applied speed boost +{effectStrength * 100}%");
        }
        else
        {
            player.MoveSpeed /= (1f + effectStrength);
            DebugHelper.Log($"[EnvironmentZone] Removed speed boost");
        }
    }

    /// <summary>
    /// 应用/移除 DOT 增强效果
    /// </summary>
    public static void ApplyDotEnhance(Collider2D entity, float effectStrength, bool apply)
    {
        var sem = entity.GetComponent<StatusEffectManager>();
        if (sem == null) return;

        if (apply)
        {
            sem.DotDamageMultiplier *= (1f + effectStrength);
            DebugHelper.Log($"[EnvironmentZone] Applied DOT enhance +{effectStrength * 100}%");
        }
        else
        {
            sem.DotDamageMultiplier /= (1f + effectStrength);
            DebugHelper.Log($"[EnvironmentZone] Removed DOT enhance");
        }
    }

    /// <summary>
    /// 应用伤害效果
    /// </summary>
    public static void ApplyDamage(Collider2D entity, float effectStrength, float tickInterval)
    {
        var damageable = entity.GetComponent<Damageable>();
        if (damageable == null || damageable.CurrentHp <= 0) return;

        int damage = Mathf.RoundToInt(effectStrength * tickInterval);
        damageable.TakeDamage(Mathf.Max(1, damage));
    }

    /// <summary>
    /// 应用治疗效果
    /// </summary>
    public static void ApplyHeal(Collider2D entity, float effectStrength, float tickInterval)
    {
        var damageable = entity.GetComponent<Damageable>();
        if (damageable == null || damageable.CurrentHp <= 0) return;

        int heal = Mathf.RoundToInt(effectStrength * tickInterval);
        damageable.Heal(heal);
    }

    /// <summary>
    /// 应用岩浆伤害（高伤害）
    /// </summary>
    public static void ApplyLavaDamage(Collider2D entity, float effectStrength, float tickInterval)
    {
        var damageable = entity.GetComponent<Damageable>();
        if (damageable == null || damageable.CurrentHp <= 0) return;

        int damage = Mathf.RoundToInt(effectStrength * tickInterval);
        damageable.TakeDamage(Mathf.Max(1, damage));
    }

    /// <summary>
    /// 获取区域类型对应的默认颜色
    /// </summary>
    public static Color GetDefaultColor(MapThemeData.EnvironmentZoneType zoneType, float alpha)
    {
        switch (zoneType)
        {
            case MapThemeData.EnvironmentZoneType.Slow:
                return new Color(0.5f, 0.8f, 1f, alpha);
            case MapThemeData.EnvironmentZoneType.Damage:
                return new Color(1f, 0.3f, 0.3f, alpha);
            case MapThemeData.EnvironmentZoneType.Heal:
                return new Color(0.3f, 1f, 0.3f, alpha);
            case MapThemeData.EnvironmentZoneType.Lava:
                return new Color(1f, 0.5f, 0f, alpha);
            case MapThemeData.EnvironmentZoneType.Speed:
                return new Color(0.3f, 0.5f, 1f, alpha);
            case MapThemeData.EnvironmentZoneType.DotEnhance:
                return new Color(0.6f, 0f, 1f, alpha);
            default:
                return new Color(1f, 1f, 1f, alpha);
        }
    }

    /// <summary>
    /// 获取区域类型对应的默认效果强度
    /// </summary>
    public static float GetDefaultStrength(MapThemeData.EnvironmentZoneType zoneType, float currentStrength)
    {
        if (currentStrength > 0) return currentStrength;

        switch (zoneType)
        {
            case MapThemeData.EnvironmentZoneType.Slow:
            case MapThemeData.EnvironmentZoneType.Speed:
                return 0.5f;
            case MapThemeData.EnvironmentZoneType.Damage:
                return 5f;
            case MapThemeData.EnvironmentZoneType.Heal:
                return 3f;
            case MapThemeData.EnvironmentZoneType.Lava:
                return 15f;
            case MapThemeData.EnvironmentZoneType.DotEnhance:
                return 0.3f;
            default:
                return 5f;
        }
    }
}