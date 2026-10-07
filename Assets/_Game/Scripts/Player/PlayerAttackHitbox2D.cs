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

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
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

            bool isCritical = Random.value < criticalChance;
            int finalDamage = isCritical
                ? Mathf.Max(1, Mathf.RoundToInt(damage * criticalDamageMultiplier))
                : damage;

            target.TakeDamage(finalDamage);
            DamagePopup.Show(
                DamagePopup.PositionAbove(hit),
                finalDamage,
                isCritical ? DamagePopupType.Critical : DamagePopupType.Normal
            );
        }
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
