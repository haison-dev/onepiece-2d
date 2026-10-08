using System;
using UnityEngine;

/// <summary>
/// Owns health state without depending on a scene object. Network-facing
/// components can later feed authoritative values into this class.
/// </summary>
public sealed class HealthSystem
{
    public int CurrentHealth { get; private set; }
    public int MaxHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    public event Action<int, int> HealthChanged;
    public event Action Died;

    public HealthSystem(int maxHealth)
    {
        MaxHealth = Mathf.Max(1, maxHealth);
        CurrentHealth = MaxHealth;
    }

    public int TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead)
            return 0;

        int previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        int appliedAmount = previousHealth - CurrentHealth;

        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        if (IsDead)
            Died?.Invoke();

        return appliedAmount;
    }

    public int Heal(int amount)
    {
        if (amount <= 0 || IsDead)
            return 0;

        int previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        int appliedAmount = CurrentHealth - previousHealth;

        if (appliedAmount > 0)
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);

        return appliedAmount;
    }

    public void SetMaxHealth(int maxHealth, bool restoreToFull)
    {
        MaxHealth = Mathf.Max(1, maxHealth);
        CurrentHealth = restoreToFull
            ? MaxHealth
            : Mathf.Clamp(CurrentHealth, 0, MaxHealth);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void SetCurrentHealth(int currentHealth)
    {
        int clampedHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
        if (clampedHealth == CurrentHealth)
            return;

        bool wasDead = IsDead;
        CurrentHealth = clampedHealth;
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);

        if (!wasDead && IsDead)
            Died?.Invoke();
    }
}
