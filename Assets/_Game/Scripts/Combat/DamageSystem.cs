using UnityEngine;

public readonly struct DamageRequest
{
    public int BaseDamage { get; }
    public float CriticalChance { get; }
    public float CriticalMultiplier { get; }

    public DamageRequest(
        int baseDamage,
        float criticalChance = 0f,
        float criticalMultiplier = 1f
    )
    {
        BaseDamage = Mathf.Max(0, baseDamage);
        CriticalChance = Mathf.Clamp01(criticalChance);
        CriticalMultiplier = Mathf.Max(1f, criticalMultiplier);
    }
}

public readonly struct DamageResult
{
    public static DamageResult None => new DamageResult(0, false, false);

    public int AppliedDamage { get; }
    public bool IsCritical { get; }
    public bool IsLethal { get; }
    public bool WasApplied => AppliedDamage > 0;

    public DamageResult(int appliedDamage, bool isCritical, bool isLethal)
    {
        AppliedDamage = Mathf.Max(0, appliedDamage);
        IsCritical = isCritical;
        IsLethal = isLethal;
    }
}

/// <summary>
/// Single entry point for resolving and applying combat damage. Multiplayer
/// code can call this only on the server without changing hit detection or UI.
/// </summary>
public static class DamageSystem
{
    public static DamageResult Apply(IDamageable target, DamageRequest request)
    {
        if (target == null || target.IsDead || request.BaseDamage <= 0)
            return DamageResult.None;

        bool isCritical = Random.value < request.CriticalChance;
        int resolvedDamage = isCritical
            ? Mathf.Max(1, Mathf.RoundToInt(request.BaseDamage * request.CriticalMultiplier))
            : request.BaseDamage;

        int healthBefore = target.CurrentHealth;
        target.TakeDamage(resolvedDamage);
        int appliedDamage = Mathf.Max(0, healthBefore - target.CurrentHealth);

        return new DamageResult(appliedDamage, isCritical, target.IsDead);
    }
}
