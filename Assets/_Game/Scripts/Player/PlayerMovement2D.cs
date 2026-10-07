using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public sealed class PlayerMovement2D : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 5f;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Vector2 movementInput;
    private Vector2 autoMoveInput;
    private bool isAutoMoving;
    private bool isMovementLocked;

    public bool IsMovementLocked => isMovementLocked;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // Top-down movement does not use gravity and should never rotate.
        body.freezeRotation = true;
        body.gravityScale = 0f;
        body.linearDamping = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Update()
    {
        if (isMovementLocked)
        {
            movementInput = Vector2.zero;
            animator.SetBool(IsMovingHash, false);
            return;
        }

        Vector2 manualInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;

        // Manual movement always takes priority over an automatic approach.
        if (manualInput.sqrMagnitude > 0.01f)
        {
            isAutoMoving = false;
            movementInput = manualInput;
        }
        else
        {
            movementInput = isAutoMoving ? autoMoveInput : Vector2.zero;
        }

        animator.SetBool(IsMovingHash, movementInput.sqrMagnitude > 0.01f);

        if (spriteRenderer != null && !Mathf.Approximately(movementInput.x, 0f))
        {
            spriteRenderer.flipX = movementInput.x < 0f;
        }
    }

    public void SetAutoMoveDirection(Vector2 direction)
    {
        if (isMovementLocked)
            return;

        autoMoveInput = direction.normalized;
        isAutoMoving = autoMoveInput.sqrMagnitude > 0.01f;
    }

    public void StopAutoMovement()
    {
        isAutoMoving = false;
        autoMoveInput = Vector2.zero;
        movementInput = Vector2.zero;

        if (body != null)
            body.linearVelocity = Vector2.zero;

        if (animator != null)
            animator.SetBool(IsMovingHash, false);
    }

    public void FaceHorizontal(float directionX)
    {
        if (spriteRenderer != null && !Mathf.Approximately(directionX, 0f))
            spriteRenderer.flipX = directionX < 0f;
    }

    public void SetMovementLocked(bool locked)
    {
        isMovementLocked = locked;

        if (locked)
            StopAutoMovement();
    }

    private void FixedUpdate()
    {
        // Let Rigidbody2D resolve collisions with the four map walls.
        body.linearVelocity = isMovementLocked
            ? Vector2.zero
            : movementInput * moveSpeed;
    }

    private void OnDisable()
    {
        isAutoMoving = false;
        isMovementLocked = false;
        autoMoveInput = Vector2.zero;
        movementInput = Vector2.zero;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }
}