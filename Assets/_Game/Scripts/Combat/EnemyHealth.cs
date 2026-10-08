using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private string displayName = "Boss";
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(0)] private int experienceReward = 100;
    [SerializeField] private LootDrop[] lootTable;
    [SerializeField] private bool destroyOnDeath = true;

    private HealthSystem health;

    public int CurrentHealth => health?.CurrentHealth ?? maxHealth;
    public int MaxHealth => maxHealth;
    public int ExperienceReward => experienceReward;
    public bool IsDead => health != null && health.IsDead;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName)
        ? gameObject.name
        : displayName;

    public event Action<int, int> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        health = new HealthSystem(maxHealth);
        health.HealthChanged += HandleHealthChanged;
        health.Died += HandleDied;
    }

    public void TakeDamage(int damage)
    {
        if (health == null || IsDead || damage <= 0)
            return;

        int appliedDamage = health.TakeDamage(damage);
        if (appliedDamage <= 0)
            return;

        Debug.Log(
            $"{name} received {appliedDamage} damage. " +
            $"Health: {CurrentHealth}/{maxHealth}",
            this
        );
    }

    private void HandleHealthChanged(int currentHealth, int currentMaxHealth)
    {
        HealthChanged?.Invoke(currentHealth, currentMaxHealth);
    }

    private void Die()
    {
        Debug.Log($"{name} was defeated.", this);

        if (destroyOnDeath)
            Destroy(gameObject);
    }

    public void ConfigureNetworkedDeath()
    {
        destroyOnDeath = false;
    }

    public void ApplyNetworkHealth(int currentHealth)
    {
        health?.SetCurrentHealth(currentHealth);
    }

    public List<InventorySlotState> RollLoot()
    {
        List<InventorySlotState> rewards = new List<InventorySlotState>();
        if (lootTable == null)
            return rewards;

        foreach (LootDrop drop in lootTable)
        {
            if (drop.itemId == ItemId.None || UnityEngine.Random.value > drop.dropChance)
                continue;

            int minimum = Mathf.Max(1, drop.minQuantity);
            int maximum = Mathf.Max(minimum, drop.maxQuantity);
            rewards.Add(new InventorySlotState(
                drop.itemId,
                UnityEngine.Random.Range(minimum, maximum + 1)
            ));
        }

        return rewards;
    }

    private void HandleDied()
    {
        Died?.Invoke();
        Die();
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        experienceReward = Mathf.Max(0, experienceReward);
    }

    private void OnDestroy()
    {
        if (health == null)
            return;

        health.HealthChanged -= HandleHealthChanged;
        health.Died -= HandleDied;
    }
}
