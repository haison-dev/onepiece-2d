using System;
using System.Collections;
using UnityEngine;

public sealed class PlayerStats : MonoBehaviour, IDamageable
{
    [SerializeField] private string characterName = "Roronoa Zoro";
    [SerializeField, Min(1)] private int level = 1;
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(0)] private int maxMana = 100;
    [SerializeField, Min(1)] private int experienceToNextLevel = 100;

    [Header("Hit Reaction")]
    [SerializeField, Min(0.1f)] private float hitReactionDuration = 0.8f;

    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeadHash = Animator.StringToHash("Dead");
    private Animator animator;
    private PlayerMovement2D movement;
    private Coroutine hitReactionRoutine;

    public string CharacterName => characterName;
    public int Level => level;
    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public int CurrentMana { get; private set; }
    public int MaxMana => maxMana;
    public int CurrentExperience { get; private set; }
    public int ExperienceToNextLevel => experienceToNextLevel;
    public bool IsDead => CurrentHealth <= 0;
    public event Action StatsChanged;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement2D>();
        CurrentHealth = maxHealth;
        CurrentMana = maxMana;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        StatsChanged?.Invoke();

        if (IsDead)
            PlayDeath();
        else
            PlayHitReaction();
    }

    private void PlayDeath()
    {
        if (hitReactionRoutine != null)
        {
            StopCoroutine(hitReactionRoutine);
            hitReactionRoutine = null;
        }

        if (movement != null)
            movement.SetMovementLocked(true);

        if (animator != null)
        {
            animator.ResetTrigger(HitHash);
            animator.ResetTrigger(DeadHash);
            animator.SetTrigger(DeadHash);
        }
    }
    private void PlayHitReaction()
    {
        if (animator != null)
        {
            animator.ResetTrigger(HitHash);
            animator.SetTrigger(HitHash);
        }

        if (hitReactionRoutine != null)
            StopCoroutine(hitReactionRoutine);

        hitReactionRoutine = StartCoroutine(LockMovementDuringHitReaction());
    }

    private IEnumerator LockMovementDuringHitReaction()
    {
        if (movement != null)
            movement.SetMovementLocked(true);

        yield return new WaitForSeconds(hitReactionDuration);

        if (movement != null)
            movement.SetMovementLocked(false);

        hitReactionRoutine = null;
    }

    private void OnDisable()
    {
        if (movement != null)
            movement.SetMovementLocked(false);

        hitReactionRoutine = null;
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        StatsChanged?.Invoke();
    }

    public bool TrySpendMana(int amount)
    {
        if (amount <= 0 || amount > CurrentMana) return false;
        CurrentMana -= amount;
        StatsChanged?.Invoke();
        return true;
    }

    public void RestoreMana(int amount)
    {
        if (amount <= 0) return;
        CurrentMana = Mathf.Min(maxMana, CurrentMana + amount);
        StatsChanged?.Invoke();
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0) return;
        CurrentExperience += amount;
        while (CurrentExperience >= experienceToNextLevel)
        {
            CurrentExperience -= experienceToNextLevel;
            level++;
            experienceToNextLevel = Mathf.Max(1, Mathf.RoundToInt(experienceToNextLevel * 1.25f));
            maxHealth += 10;
            maxMana += 5;
            CurrentHealth = maxHealth;
            CurrentMana = maxMana;
        }
        StatsChanged?.Invoke();
    }

    private void OnValidate()
    {
        level = Mathf.Max(1, level);
        maxHealth = Mathf.Max(1, maxHealth);
        maxMana = Mathf.Max(0, maxMana);
        experienceToNextLevel = Mathf.Max(1, experienceToNextLevel);
        hitReactionDuration = Mathf.Max(0.1f, hitReactionDuration);
    }
}
