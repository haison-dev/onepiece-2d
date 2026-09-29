using System;
using UnityEngine;

public sealed class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField] private bool destroyOnDeath = true;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0;

    public event Action<int, int> HealthChanged;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        Debug.Log(
            $"{name} received {damage} damage. " +
            $"Health: {CurrentHealth}/{maxHealth}",
            this
        );

        if (IsDead)
            Die();
    }

    private void Die()
    {
        Debug.Log($"{name} was defeated.", this);

        if (destroyOnDeath)
            Destroy(gameObject);
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
    }
}
