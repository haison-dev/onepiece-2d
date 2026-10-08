using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerAttackHitbox2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform attackPoint;

    [Header("Attack Settings")]
    [SerializeField, Min(1)] private int damage = 10;
    [SerializeField, Range(0f, 1f)] private float criticalChance = 0.2f;
    [SerializeField, Min(1f)] private float criticalDamageMultiplier = 2f;
    [SerializeField, Min(0.01f)] private float attackRadius = 0.3f;
    [SerializeField] private LayerMask enemyLayer;

    private readonly HashSet<IDamageable> hitTargets = new();

    private SpriteRenderer spriteRenderer;
    private SpriteRenderer attackEffectRenderer;
    private float attackPointDistance;
    private float attackPointHeight;
    private bool damageEnabled = true;
    private bool damagePopupEnabled = true;
    private float damageMultiplier = 1f;
    private PlayerStats stats;
    private int attributeDamageBonus;
    private float attributeCriticalChanceBonus;

    public event Action<Vector3, DamageResult> DamageApplied;
    public event Action<EnemyHealth> EnemyDefeated;
    public int CurrentDamage => Mathf.Max(
        1,
        Mathf.RoundToInt((damage + attributeDamageBonus) * damageMultiplier)
    );
    public float CurrentCriticalChance => Mathf.Clamp01(
        criticalChance + attributeCriticalChanceBonus
    );

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.StatsChanged += RefreshAttributeBonuses;
            RefreshAttributeBonuses();
        }
        CacheAttackPointPosition();
        CacheAttackEffectRenderer();
        UpdateAttackPointDirection();
    }

    private void LateUpdate()
    {
        UpdateAttackPointDirection();
    }

    // Called by an Animation Event on the attack impact frame.
    public void PerformAttackHit()
    {
        if (!damageEnabled)
            return;

        if (attackPoint == null)
        {
            Debug.LogWarning("PlayerAttackHitbox2D: AttackPoint has not been assigned.", this);
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRadius,
            enemyLayer
        );

        hitTargets.Clear();

        foreach (Collider2D hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();

            // One enemy may own multiple colliders, but receives one hit only.
            if (target == null || !hitTargets.Add(target))
                continue;

            DamageResult result = DamageSystem.Apply(
                target,
                new DamageRequest(
                    CurrentDamage,
                    CurrentCriticalChance,
                    criticalDamageMultiplier
                )
            );
            if (!result.WasApplied)
                continue;

            Vector3 popupPosition = DamagePopup.PositionAbove(hit);
            DamageApplied?.Invoke(popupPosition, result);
            if (result.IsLethal && target is EnemyHealth defeatedEnemy)
                EnemyDefeated?.Invoke(defeatedEnemy);

            if (damagePopupEnabled)
            {
                DamagePopup.Show(
                    popupPosition,
                    result.AppliedDamage,
                    result.IsCritical ? DamagePopupType.Critical : DamagePopupType.Normal
                );
            }
        }
    }

    public void SetDamageEnabled(bool enabled)
    {
        damageEnabled = enabled;
    }

    public void SetDamagePopupEnabled(bool enabled)
    {
        damagePopupEnabled = enabled;
    }

    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = Mathf.Max(0f, multiplier);
    }

    private void RefreshAttributeBonuses()
    {
        attributeDamageBonus = stats != null ? stats.PhysicalDamageBonus : 0;
        attributeCriticalChanceBonus = stats != null ? stats.CriticalChanceBonus : 0f;
    }

    private void OnDestroy()
    {
        if (stats != null)
            stats.StatsChanged -= RefreshAttributeBonuses;
    }

    public bool IsTargetInRange(IDamageable target)
    {
        if (attackPoint == null || target == null)
            return false;

        // Use the current facing direction immediately instead of waiting for LateUpdate.
        UpdateAttackPointDirection();

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRadius,
            enemyLayer
        );

        foreach (Collider2D hit in hits)
        {
            if (ReferenceEquals(hit.GetComponentInParent<IDamageable>(), target))
                return true;
        }

        return false;
    }

    private void CacheAttackPointPosition()
    {
        if (attackPoint == null)
            return;

        Vector3 localPosition = attackPoint.localPosition;
        attackPointDistance = Mathf.Abs(localPosition.x);
        attackPointHeight = localPosition.y;
    }

    private void CacheAttackEffectRenderer()
    {
        if (attackPoint != null)
            attackEffectRenderer = attackPoint.GetComponentInChildren<SpriteRenderer>(true);
    }

    private void UpdateAttackPointDirection()
    {
        if (attackPoint == null || spriteRenderer == null)
            return;

        float direction = spriteRenderer.flipX ? -1f : 1f;
        Vector3 localPosition = attackPoint.localPosition;

        localPosition.x = attackPointDistance * direction;
        localPosition.y = attackPointHeight;
        attackPoint.localPosition = localPosition;

        if (attackEffectRenderer != null)
            attackEffectRenderer.flipX = spriteRenderer.flipX;
    }

    private void OnValidate()
    {
        damage = Mathf.Max(1, damage);
        criticalChance = Mathf.Clamp01(criticalChance);
        criticalDamageMultiplier = Mathf.Max(1f, criticalDamageMultiplier);
        attackRadius = Mathf.Max(0.01f, attackRadius);
    }
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}
