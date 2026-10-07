using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class KizaruAoeAttack : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Sprite[] attackFrames;
    [SerializeField, Min(1f)] private float attackFrameRate = 12f;
    [SerializeField, Min(0)] private int impactFrame = 4;
    [SerializeField] private string idleStateName = "Kizaru_Idle";

    [Header("AOE Effect")]
    [SerializeField] private SpriteRenderer effectRenderer;
    [SerializeField] private Sprite[] effectFrames;
    [SerializeField, Min(1f)] private float effectFrameRate = 12f;
    [SerializeField] private Vector2 effectOffset = new Vector2(0f, -1.5f);
    [SerializeField, Min(0.01f)] private float effectScale = 1.5f;
    [SerializeField] private int effectSortingOrder = 1;

    [Header("Combat")]
    [SerializeField, Min(1)] private int damage = 15000;
    [SerializeField, Min(0.1f)] private float castRange = 7f;
    [SerializeField, Min(0.1f)] private float aoeRadius = 3.5f;
    [SerializeField, Min(0f)] private float attackCooldown = 3f;
    [SerializeField] private LayerMask playerLayer = 1;

    private readonly HashSet<IDamageable> hitTargets = new();
    private Animator animator;
    private SpriteRenderer bossRenderer;
    private Coroutine attackRoutine;
    private bool ownsEffectRenderer;
    private Vector2 lastImpactPosition;
    private float nextAttackAt;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        bossRenderer = GetComponent<SpriteRenderer>();
        SetupEffectRenderer();
    }

    private void Update()
    {
        if (attackRoutine != null || Time.time < nextAttackAt)
            return;

        PlayerStats player = FindFirstObjectByType<PlayerStats>();
        if (player == null || player.IsDead)
            return;

        if ((player.transform.position - transform.position).sqrMagnitude <= castRange * castRange)
            attackRoutine = StartCoroutine(PlayAttack(player));
    }

    private IEnumerator PlayAttack(PlayerStats target)
    {
        if (attackFrames == null || attackFrames.Length == 0)
        {
            attackRoutine = null;
            yield break;
        }

        animator.enabled = false;
        SetEffectVisible(false);

        int safeImpactFrame = Mathf.Clamp(impactFrame, 0, attackFrames.Length - 1);
        float attackDuration = attackFrames.Length / attackFrameRate;
        float impactTime = safeImpactFrame / attackFrameRate;
        float effectDuration = effectFrames == null ? 0f : effectFrames.Length / effectFrameRate;
        float totalDuration = Mathf.Max(attackDuration, impactTime + effectDuration);
        float elapsed = 0f;
        int lastAttackFrame = -1;
        int lastEffectFrame = -1;
        bool damageApplied = false;

        while (elapsed < totalDuration)
        {
            int attackIndex = Mathf.Min(
                Mathf.FloorToInt(elapsed * attackFrameRate),
                attackFrames.Length - 1
            );
            if (attackIndex != lastAttackFrame)
            {
                bossRenderer.sprite = attackFrames[attackIndex];
                lastAttackFrame = attackIndex;
            }

            if (elapsed >= impactTime)
            {
                if (!damageApplied)
                {
                    lastImpactPosition = target != null
                        ? (Vector2)target.transform.position + effectOffset
                        : (Vector2)transform.position + effectOffset;
                    MoveEffectTo(lastImpactPosition);
                    ApplyAoeDamage(lastImpactPosition);
                    damageApplied = true;
                }

                if (effectFrames != null && effectFrames.Length > 0)
                {
                    int effectIndex = Mathf.FloorToInt((elapsed - impactTime) * effectFrameRate);
                    if (effectIndex >= 0 && effectIndex < effectFrames.Length && effectIndex != lastEffectFrame)
                    {
                        effectRenderer.sprite = effectFrames[effectIndex];
                        effectRenderer.enabled = true;
                        lastEffectFrame = effectIndex;
                    }
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        SetEffectVisible(false);
        animator.enabled = true;
        animator.Play(idleStateName, 0, 0f);
        nextAttackAt = Time.time + attackCooldown;
        attackRoutine = null;
    }

    private void ApplyAoeDamage(Vector2 center)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, aoeRadius, playerLayer);
        hitTargets.Clear();

        foreach (Collider2D hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || !hitTargets.Add(target))
                continue;

            target.TakeDamage(damage);
            DamagePopup.Show(
                DamagePopup.PositionAbove(hit),
                damage,
                DamagePopupType.PlayerDamage
            );
        }
    }

    private void SetupEffectRenderer()
    {
        if (effectRenderer == null)
        {
            Transform effectTransform = transform.Find("KizaruLightAoeEffect");
            if (effectTransform == null)
            {
                GameObject effectObject = new GameObject("KizaruLightAoeEffect");
                effectTransform = effectObject.transform;
                ownsEffectRenderer = true;
            }

            effectRenderer = effectTransform.GetComponent<SpriteRenderer>();
            if (effectRenderer == null)
                effectRenderer = effectTransform.gameObject.AddComponent<SpriteRenderer>();
        }

        effectRenderer.transform.SetParent(null, true);
        effectRenderer.transform.localScale = Vector3.one * effectScale;
        MoveEffectTo((Vector2)transform.position + effectOffset);
        effectRenderer.sortingLayerID = bossRenderer.sortingLayerID;
        effectRenderer.sortingOrder = Mathf.Max(bossRenderer.sortingOrder + 1, effectSortingOrder);
        SetEffectVisible(false);
    }

    private void MoveEffectTo(Vector2 worldPosition)
    {
        if (effectRenderer != null)
            effectRenderer.transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
    }

    private void SetEffectVisible(bool visible)
    {
        if (effectRenderer == null)
            return;

        effectRenderer.enabled = visible;
        if (!visible)
            effectRenderer.sprite = null;
    }

    private void OnDisable()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        SetEffectVisible(false);
        if (animator != null)
            animator.enabled = true;
    }

    private void OnDestroy()
    {
        if (ownsEffectRenderer && effectRenderer != null)
            Destroy(effectRenderer.gameObject);
    }

    private void OnValidate()
    {
        attackFrameRate = Mathf.Max(1f, attackFrameRate);
        effectFrameRate = Mathf.Max(1f, effectFrameRate);
        damage = Mathf.Max(1, damage);
        castRange = Mathf.Max(0.1f, castRange);
        aoeRadius = Mathf.Max(0.1f, aoeRadius);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        effectScale = Mathf.Max(0.01f, effectScale);
        if (string.IsNullOrWhiteSpace(idleStateName))
            idleStateName = "Kizaru_Idle";
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying
            ? lastImpactPosition
            : transform.position + (Vector3)effectOffset;
        Gizmos.color = new Color(1f, 0.8f, 0.1f, 1f);
        Gizmos.DrawWireSphere(center, aoeRadius);
        Gizmos.color = new Color(1f, 0.9f, 0.4f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, castRange);
    }
}