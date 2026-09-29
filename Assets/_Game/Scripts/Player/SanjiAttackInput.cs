using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerMovement2D))]
[RequireComponent(typeof(PlayerAttackHitbox2D))]
public class SanjiAttackInput : MonoBehaviour
{
    [Header("Auto Attack")]
    [SerializeField, Min(0.1f)] private float targetSearchRadius = 8f;

    private Animator animator;
    private PlayerMovement2D movement;
    private PlayerAttackHitbox2D attackHitbox;
    private EnemyHealth currentTarget;
    private bool isApproachingTarget;

    private static readonly int AttackHash =
        Animator.StringToHash("Attack");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement2D>();
        attackHitbox = GetComponent<PlayerAttackHitbox2D>();
    }

    private void Update()
    {
        if (isApproachingTarget && HasManualMovementInput())
            CancelAutoAttack();

        if (Input.GetKeyDown(KeyCode.J))
            StartAutoAttack();

        UpdateAutoAttack();
    }

    private void StartAutoAttack()
    {
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        // Không cho kích hoạt lại khi Sanji đang đánh.
        if (state.IsName("Sanji_Attack"))
            return;

        EnemyHealth nearestTarget = FindNearestTarget();
        if (nearestTarget == null)
        {
            CancelAutoAttack();
            return;
        }

        currentTarget = nearestTarget;
        isApproachingTarget = true;
    }

    private void UpdateAutoAttack()
    {
        if (!isApproachingTarget)
            return;

        if (currentTarget == null || currentTarget.IsDead)
        {
            CancelAutoAttack();
            return;
        }

        Vector2 toTarget = currentTarget.transform.position - transform.position;
        movement.FaceHorizontal(toTarget.x);

        if (!attackHitbox.IsTargetInRange(currentTarget))
        {
            movement.SetAutoMoveDirection(toTarget);
            return;
        }

        movement.StopAutoMovement();
        isApproachingTarget = false;

        animator.ResetTrigger(AttackHash);
        animator.SetTrigger(AttackHash);
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

    private void CancelAutoAttack()
    {
        isApproachingTarget = false;
        currentTarget = null;

        if (movement != null)
            movement.StopAutoMovement();
    }

    private void OnDisable()
    {
        CancelAutoAttack();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, targetSearchRadius);
    }
}
