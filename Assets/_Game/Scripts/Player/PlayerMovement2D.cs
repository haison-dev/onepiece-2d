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
        movementInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;

        animator.SetBool(IsMovingHash, movementInput.sqrMagnitude > 0.01f);

        if (spriteRenderer != null && !Mathf.Approximately(movementInput.x, 0f))
        {
            spriteRenderer.flipX = movementInput.x < 0f;
        }
    }

    private void FixedUpdate()
    {
        body.linearVelocity = movementInput * moveSpeed;
    }
}
