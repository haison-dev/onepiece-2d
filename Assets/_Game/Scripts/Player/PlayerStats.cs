using System;
using System.Collections;
using UnityEngine;

public enum PotentialStat
{
    Strength = 0,
    Vitality = 1,
    Agility = 2,
    Energy = 3
}

public sealed class PlayerStats : MonoBehaviour, IDamageable
{
    [SerializeField] private string characterName = "Roronoa Zoro";
    [SerializeField, Min(1)] private int level = 1;
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(0)] private int maxMana = 100;
    [SerializeField, Min(1)] private int experienceToNextLevel = 100;

    [Header("Progression")]
    [SerializeField, Min(1)] private int potentialPointsPerLevel = 5;
    [SerializeField, Min(0)] private int strength;
    [SerializeField, Min(0)] private int vitality;
    [SerializeField, Min(0)] private int agility;
    [SerializeField, Min(0)] private int energy;

    [Header("Hit Reaction")]
    [SerializeField, Min(0.1f)] private float hitReactionDuration = 0.8f;

    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeadHash = Animator.StringToHash("Dead");
    private Animator animator;
    private PlayerMovement2D movement;
    private Coroutine hitReactionRoutine;
    private HealthSystem health;
    private int equipmentStrength;
    private int equipmentVitality;
    private int equipmentAgility;
    private int equipmentEnergy;

    public string CharacterName => characterName;
    public int Level => level;
    public int CurrentHealth => health?.CurrentHealth ?? MaxHealth;
    public int MaxHealth => maxHealth + Vitality * 20;
    public int CurrentMana { get; private set; }
    public int MaxMana => maxMana + Energy * 10;
    public int CurrentExperience { get; private set; }
    public int ExperienceToNextLevel => experienceToNextLevel;
    public int UnspentPotentialPoints { get; private set; }
    public int AllocatedStrength => strength;
    public int AllocatedVitality => vitality;
    public int AllocatedAgility => agility;
    public int AllocatedEnergy => energy;
    public int Strength => strength + equipmentStrength;
    public int Vitality => vitality + equipmentVitality;
    public int Agility => agility + equipmentAgility;
    public int Energy => energy + equipmentEnergy;
    public int PhysicalDamageBonus => Strength * 2;
    public float CriticalChanceBonus => Agility * 0.0025f;
    public bool IsDead => health != null && health.IsDead;
    public event Action StatsChanged;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement2D>();
        health = new HealthSystem(MaxHealth);
        health.HealthChanged += HandleHealthChanged;
        CurrentMana = MaxMana;
    }

    public void TakeDamage(int damage)
    {
        if (health == null || damage <= 0 || IsDead)
            return;

        int appliedDamage = health.TakeDamage(damage);
        if (appliedDamage <= 0)
            return;

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
        if (health == null)
            return;

        health.Heal(amount);
    }

    public void ApplyNetworkHealth(int currentHealth)
    {
        health?.SetCurrentHealth(currentHealth);
    }

    public void ApplyNetworkMana(int currentMana)
    {
        int clampedMana = Mathf.Clamp(currentMana, 0, MaxMana);
        if (clampedMana == CurrentMana)
            return;

        CurrentMana = clampedMana;
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
        CurrentMana = Mathf.Min(MaxMana, CurrentMana + amount);
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
            UnspentPotentialPoints += potentialPointsPerLevel;
        }
        StatsChanged?.Invoke();
    }

    public bool TryAllocatePotential(PotentialStat stat)
    {
        if (UnspentPotentialPoints <= 0)
            return false;

        switch (stat)
        {
            case PotentialStat.Strength:
                strength++;
                break;
            case PotentialStat.Vitality:
                vitality++;
                health?.SetMaxHealth(MaxHealth, false);
                break;
            case PotentialStat.Agility:
                agility++;
                break;
            case PotentialStat.Energy:
                energy++;
                break;
            default:
                return false;
        }

        UnspentPotentialPoints--;
        CurrentMana = Mathf.Min(CurrentMana, MaxMana);
        StatsChanged?.Invoke();
        return true;
    }

    public void ApplyNetworkProgression(
        int networkLevel,
        int currentExperience,
        int requiredExperience,
        int unspentPoints,
        int networkStrength,
        int networkVitality,
        int networkAgility,
        int networkEnergy)
    {
        level = Mathf.Max(1, networkLevel);
        CurrentExperience = Mathf.Max(0, currentExperience);
        experienceToNextLevel = Mathf.Max(1, requiredExperience);
        UnspentPotentialPoints = Mathf.Max(0, unspentPoints);
        strength = Mathf.Max(0, networkStrength);
        vitality = Mathf.Max(0, networkVitality);
        agility = Mathf.Max(0, networkAgility);
        energy = Mathf.Max(0, networkEnergy);

        health?.SetMaxHealth(MaxHealth, false);
        CurrentMana = Mathf.Clamp(CurrentMana, 0, MaxMana);
        StatsChanged?.Invoke();
    }

    public void ApplyEquipmentBonuses(
        int bonusStrength,
        int bonusVitality,
        int bonusAgility,
        int bonusEnergy)
    {
        equipmentStrength = Mathf.Max(0, bonusStrength);
        equipmentVitality = Mathf.Max(0, bonusVitality);
        equipmentAgility = Mathf.Max(0, bonusAgility);
        equipmentEnergy = Mathf.Max(0, bonusEnergy);
        health?.SetMaxHealth(MaxHealth, false);
        CurrentMana = Mathf.Clamp(CurrentMana, 0, MaxMana);
        StatsChanged?.Invoke();
    }

    private void OnValidate()
    {
        level = Mathf.Max(1, level);
        maxHealth = Mathf.Max(1, maxHealth);
        maxMana = Mathf.Max(0, maxMana);
        experienceToNextLevel = Mathf.Max(1, experienceToNextLevel);
        potentialPointsPerLevel = Mathf.Max(1, potentialPointsPerLevel);
        strength = Mathf.Max(0, strength);
        vitality = Mathf.Max(0, vitality);
        agility = Mathf.Max(0, agility);
        energy = Mathf.Max(0, energy);
        hitReactionDuration = Mathf.Max(0.1f, hitReactionDuration);
    }

    private void HandleHealthChanged(int currentHealth, int currentMaxHealth)
    {
        StatsChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (health != null)
            health.HealthChanged -= HandleHealthChanged;
    }
}
