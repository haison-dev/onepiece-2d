using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerMovement2D))]
[RequireComponent(typeof(PlayerAttackHitbox2D))]
public class ZoroAttackInput : MonoBehaviour
{
    [Header("Auto Attack")]
    [SerializeField, Min(0.1f)] private float targetSearchRadius = 8f;
    [SerializeField] private KeyCode autoAttackKey = KeyCode.J;
    [SerializeField] private string attackStateName = "Zoro_Attack";

    private Animator animator;
    private PlayerMovement2D movement;
    private PlayerAttackHitbox2D attackHitbox;
    private EnemyHealth currentTarget;
    private bool isAutoAttackEnabled;
    private bool isAttackInProgress;
    private bool hasEnteredAttackState;
    private int attackStateHash;

    public bool IsAutoAttackEnabled => isAutoAttackEnabled;

    private static readonly int AttackHash =
        Animator.StringToHash("Attack");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement2D>();
        attackHitbox = GetComponent<PlayerAttackHitbox2D>();
        attackStateHash = Animator.StringToHash(attackStateName);
    }

    private void Update()
    {
        if (Input.GetKeyDown(autoAttackKey))
            ToggleAutoAttack();

        if (!isAutoAttackEnabled)
            return;

        if (HasManualMovementInput())
        {
            SetAutoAttackEnabled(false);
            return;
        }

        UpdateAttackCycle();
        UpdateAutoAttack();
    }

    public void ToggleAutoAttack()
    {
        SetAutoAttackEnabled(!isAutoAttackEnabled);
    }

    public void SetAutoAttackEnabled(bool enabled)
    {
        if (isAutoAttackEnabled == enabled)
            return;

        isAutoAttackEnabled = enabled;

        if (enabled)
        {
            currentTarget = FindNearestTarget();

            // Auto attack only stays active while there is a nearby target.
            if (currentTarget == null)
                StopAutoAttack();

            return;
        }

        StopAutoAttack();
    }

    private void UpdateAutoAttack()
    {
        if (!IsCurrentTargetValid())
        {
            currentTarget = FindNearestTarget();

            if (currentTarget == null)
            {
                StopAutoAttack();
                return;
            }
        }

        Vector2 toTarget = currentTarget.transform.position - transform.position;
        movement.FaceHorizontal(toTarget.x);

        // Wait for the current animation to finish before moving or attacking again.
        if (isAttackInProgress)
        {
            movement.StopAutoMovement();
            return;
        }

        if (!attackHitbox.IsTargetInRange(currentTarget))
        {
            movement.SetAutoMoveDirection(toTarget);
            return;
        }

        movement.StopAutoMovement();
        StartAttackCycle();
    }

    private void StartAttackCycle()
    {
        isAttackInProgress = true;
        hasEnteredAttackState = false;
        animator.ResetTrigger(AttackHash);
        animator.SetTrigger(AttackHash);
    }

    private void UpdateAttackCycle()
    {
        if (!isAttackInProgress)
            return;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        bool isInAttackState = currentState.shortNameHash == attackStateHash;

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
            isInAttackState |= nextState.shortNameHash == attackStateHash;
        }

        if (isInAttackState)
        {
            hasEnteredAttackState = true;
            return;
        }

        if (hasEnteredAttackState)
        {
            isAttackInProgress = false;
            hasEnteredAttackState = false;
        }
    }

    private bool IsCurrentTargetValid()
    {
        if (currentTarget == null || currentTarget.IsDead)
            return false;

        float sqrDistance =
            (currentTarget.transform.position - transform.position).sqrMagnitude;

        return sqrDistance <= targetSearchRadius * targetSearchRadius;
    }

    private EnemyHealth FindNearestTarget()
    {
        EnemyHealth nearestTarget = null;
        float nearestSqrDistance = targetSearchRadius * targetSearchRadius;

        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy.IsDead)
                continue;

            float sqrDistance =
                (enemy.transform.position - transform.position).sqrMagnitude;

            if (sqrDistance > nearestSqrDistance)
                continue;

            nearestSqrDistance = sqrDistance;
            nearestTarget = enemy;
        }

        return nearestTarget;
    }

    private static bool HasManualMovementInput()
    {
        return !Mathf.Approximately(Input.GetAxisRaw("Horizontal"), 0f) ||
               !Mathf.Approximately(Input.GetAxisRaw("Vertical"), 0f);
    }

    private void StopAutoAttack()
    {
        isAutoAttackEnabled = false;
        isAttackInProgress = false;
        hasEnteredAttackState = false;
        currentTarget = null;

        if (animator != null)
            animator.ResetTrigger(AttackHash);

        if (movement != null)
            movement.StopAutoMovement();
    }

    private void OnDisable()
    {
        StopAutoAttack();
    }

    private void OnValidate()
    {
        targetSearchRadius = Mathf.Max(0.1f, targetSearchRadius);

        if (string.IsNullOrWhiteSpace(attackStateName))
            attackStateName = "Zoro_Attack";

        attackStateHash = Animator.StringToHash(attackStateName);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, targetSearchRadius);
    }
}
