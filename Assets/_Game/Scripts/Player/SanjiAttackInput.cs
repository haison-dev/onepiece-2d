using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SanjiAttackInput : MonoBehaviour
{
    private Animator animator;

    private static readonly int AttackHash =
        Animator.StringToHash("Attack");

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        HandleAttackInput();
    }

    private void HandleAttackInput()
    {
        if (!Input.GetKeyDown(KeyCode.J))
            return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        // Không cho kích hoạt lại khi Sanji đang đánh.
        if (state.IsName("Sanji_Attack"))
            return;

        animator.ResetTrigger(AttackHash);
        animator.SetTrigger(AttackHash);
    }
}