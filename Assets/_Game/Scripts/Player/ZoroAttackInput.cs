using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerMovement2D))]
[RequireComponent(typeof(PlayerAttackHitbox2D))]
[RequireComponent(typeof(PlayerStats))]
public class ZoroAttackInput : MonoBehaviour
{
    [Header("Auto Attack")]
    [SerializeField, Min(0.1f)] private float targetSearchRadius = 8f;
    [SerializeField] private KeyCode autoAttackKey = KeyCode.J;
    [SerializeField] private string attackStateName = "Zoro_Attack";
    [SerializeField, Min(0)] private int basicAttackManaCost = 1;

    [Header("Ultimate Asura")]
    [SerializeField] private KeyCode skill4Key = KeyCode.Alpha4;
    [SerializeField] private string skill4StateName = "Zoro_Asura_Ultimate";
    [SerializeField] private SpriteRenderer skill4EffectRenderer;
    [SerializeField] private Sprite[] skill4AuraFrames;
    [SerializeField, Min(1f)] private float skill4AuraFrameRate = 12f;
    [SerializeField] private Vector2 skill4AuraOffset = new Vector2(0f, -0.55f);
    [SerializeField, Min(0.01f)] private float skill4AuraScale = 1f;
    [SerializeField] private int skill4AuraSortingOrder = 1;
    [SerializeField, Min(0.1f)] private float skill4ActiveDuration = 20f;
    [SerializeField, Min(0f)] private float skill4Cooldown = 1f;

    private Animator animator;
    private PlayerMovement2D movement;
    private PlayerAttackHitbox2D attackHitbox;
    private PlayerStats stats;
    private PlayerSkillHUD skillHud;
    private EnemyHealth currentTarget;
    private bool isAutoAttackEnabled;
    private bool isAttackInProgress;
    private bool hasEnteredAttackState;
    private int attackStateHash;
    private bool isSkill4InProgress;
    private bool hasEnteredSkill4State;
    private int skill4StateHash;
    private bool isSkill4Active;
    private float skill4ActiveUntil;
    private float skill4ReadyAt;
    private float skill4AuraStartedAt;
    private SpriteRenderer skill4AuraRenderer;

    public bool IsAutoAttackEnabled => isAutoAttackEnabled;
    public bool IsSkill4Active => isSkill4Active;
    public bool CanActivateSkill4 => !isSkill4Active && Time.time >= skill4ReadyAt;
    public float Skill4ActiveTimeRemaining => isSkill4Active
        ? Mathf.Max(0f, skill4ActiveUntil - Time.time)
        : 0f;
    public float Skill4CooldownRemaining => !isSkill4Active
        ? Mathf.Max(0f, skill4ReadyAt - Time.time)
        : 0f;

    private static readonly int AttackHash =
        Animator.StringToHash("Attack");

    private static readonly int AsuraUltimateHash =
        Animator.StringToHash("AsuraUltimate");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement2D>();
        attackHitbox = GetComponent<PlayerAttackHitbox2D>();
        stats = GetComponent<PlayerStats>();
        skillHud = GetComponent<PlayerSkillHUD>();
        if (skill4EffectRenderer == null)
        {
            Transform effectTransform = transform.Find("AsuraUltimateEffect");
            if (effectTransform != null)
                skill4EffectRenderer = effectTransform.GetComponent<SpriteRenderer>();
        }

        SetupSkill4AuraRenderer();

        attackStateHash = Animator.StringToHash(attackStateName);
        skill4StateHash = Animator.StringToHash(skill4StateName);
    }

    private void OnEnable()
    {
        if (skillHud == null)
            skillHud = GetComponent<PlayerSkillHUD>();

        if (skillHud != null)
        {
            skillHud.SkillPressed += HandleSkillPressed;
            skillHud.SetSkillInteractable(4, CanActivateSkill4);
        }
    }

    private void Update()
    {
        UpdateSkill4Lifetime();

        if (movement.IsMovementLocked)
        {
            if (isAutoAttackEnabled)
                StopAutoAttack();

            return;
        }

        if (Input.GetKeyDown(skill4Key))
            ActivateSkill4();

        UpdateSkill4Cycle();
        if (isSkill4InProgress)
            return;

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

    public void ActivateSkill4()
    {
        if (!CanActivateSkill4 || isSkill4InProgress || movement.IsMovementLocked)
            return;

        SetAutoAttackEnabled(false);
        movement.StopAutoMovement();
        if (skill4EffectRenderer != null)
        {
            SpriteRenderer playerRenderer = GetComponent<SpriteRenderer>();
            skill4EffectRenderer.flipX = playerRenderer != null && playerRenderer.flipX;
        }

        isSkill4InProgress = true;
        hasEnteredSkill4State = false;
        isSkill4Active = true;
        skill4ActiveUntil = Time.time + skill4ActiveDuration;
        skill4ReadyAt = skill4ActiveUntil + skill4Cooldown;
        StartSkill4Aura();
        if (skillHud != null)
            skillHud.SetSkillInteractable(4, false);

        animator.ResetTrigger(AttackHash);
        animator.ResetTrigger(AsuraUltimateHash);
        animator.SetTrigger(AsuraUltimateHash);
    }

    private void UpdateSkill4Lifetime()
    {
        if (isSkill4Active && Time.time >= skill4ActiveUntil)
        {
            isSkill4Active = false;
            StopSkill4Aura();
        }

        if (!isSkill4Active && Time.time >= skill4ReadyAt && skillHud != null)
            skillHud.SetSkillInteractable(4, true);
    }

    private void LateUpdate()
    {
        UpdateSkill4Aura();
    }

    private void SetupSkill4AuraRenderer()
    {
        KeepStableSkill4AuraFrames();
        if (skill4AuraFrames == null || skill4AuraFrames.Length == 0)
            return;

        Transform auraTransform = transform.Find("AsuraUltimateAura");
        if (auraTransform == null)
        {
            GameObject auraObject = new GameObject("AsuraUltimateAura");
            auraTransform = auraObject.transform;
            auraTransform.SetParent(transform, false);
        }

        auraTransform.localPosition = new Vector3(skill4AuraOffset.x, skill4AuraOffset.y, 0f);
        auraTransform.localRotation = Quaternion.identity;
        auraTransform.localScale = Vector3.one * skill4AuraScale;

        skill4AuraRenderer = auraTransform.GetComponent<SpriteRenderer>();
        if (skill4AuraRenderer == null)
            skill4AuraRenderer = auraTransform.gameObject.AddComponent<SpriteRenderer>();

        SpriteRenderer playerRenderer = GetComponent<SpriteRenderer>();
        if (playerRenderer != null)
        {
            int auraSortingOrder = Mathf.Max(1, skill4AuraSortingOrder);
            skill4AuraRenderer.sortingLayerID = playerRenderer.sortingLayerID;
            skill4AuraRenderer.sortingOrder = auraSortingOrder;
            playerRenderer.sortingOrder = Mathf.Max(
                playerRenderer.sortingOrder,
                auraSortingOrder + 1
            );
        }

        skill4AuraRenderer.sprite = null;
        skill4AuraRenderer.enabled = false;
    }

    private void KeepStableSkill4AuraFrames()
    {
        if (skill4AuraFrames == null)
            return;

        if (skill4AuraFrames.Length == 4 &&
            System.Array.TrueForAll(skill4AuraFrames, IsStableSkill4AuraFrame))
            return;

        skill4AuraFrames = System.Array.FindAll(
            skill4AuraFrames,
            IsStableSkill4AuraFrame
        );
    }

    private static bool IsStableSkill4AuraFrame(Sprite frame)
    {
        return frame != null &&
               (frame.name.EndsWith("_4") ||
                frame.name.EndsWith("_5") ||
                frame.name.EndsWith("_6") ||
                frame.name.EndsWith("_7"));
    }

    private void StartSkill4Aura()
    {
        if (skill4AuraRenderer == null)
            SetupSkill4AuraRenderer();

        if (skill4AuraRenderer == null || skill4AuraFrames == null || skill4AuraFrames.Length == 0)
            return;

        skill4AuraStartedAt = Time.time;
        skill4AuraRenderer.sprite = skill4AuraFrames[0];
        skill4AuraRenderer.enabled = true;
    }

    private void UpdateSkill4Aura()
    {
        KeepStableSkill4AuraFrames();
        if (!isSkill4Active || skill4AuraRenderer == null ||
            skill4AuraFrames == null || skill4AuraFrames.Length == 0)
            return;

        int frameIndex = Mathf.FloorToInt(
            (Time.time - skill4AuraStartedAt) * skill4AuraFrameRate
        ) % skill4AuraFrames.Length;

        skill4AuraRenderer.sprite = skill4AuraFrames[frameIndex];

        SpriteRenderer playerRenderer = GetComponent<SpriteRenderer>();
        if (playerRenderer != null)
        {
            int auraSortingOrder = Mathf.Max(1, skill4AuraSortingOrder);
            skill4AuraRenderer.sortingLayerID = playerRenderer.sortingLayerID;
            skill4AuraRenderer.sortingOrder = auraSortingOrder;
            playerRenderer.sortingOrder = Mathf.Max(
                playerRenderer.sortingOrder,
                auraSortingOrder + 1
            );
            skill4AuraRenderer.flipX = playerRenderer.flipX;
        }
    }

    private void StopSkill4Aura()
    {
        if (skill4AuraRenderer == null)
            return;

        skill4AuraRenderer.enabled = false;
        skill4AuraRenderer.sprite = null;
    }

    private void HandleSkillPressed(int skillNumber)
    {
        if (skillNumber == 4)
            ActivateSkill4();
    }

    private void UpdateSkill4Cycle()
    {
        if (!isSkill4InProgress)
            return;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        bool isInSkill4State = currentState.shortNameHash == skill4StateHash;

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
            isInSkill4State |= nextState.shortNameHash == skill4StateHash;
        }

        if (isInSkill4State)
        {
            hasEnteredSkill4State = true;
            return;
        }

        if (hasEnteredSkill4State)
        {
            isSkill4InProgress = false;
            hasEnteredSkill4State = false;
        }
    }

    public void ToggleAutoAttack()
    {
        SetAutoAttackEnabled(!isAutoAttackEnabled);
    }

    public void SetAutoAttackEnabled(bool enabled)
    {
        if (enabled && movement.IsMovementLocked)
            return;

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
        if (basicAttackManaCost > 0 && !stats.TrySpendMana(basicAttackManaCost))
        {
            StopAutoAttack();
            return;
        }

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
        if (skillHud != null)
            skillHud.SkillPressed -= HandleSkillPressed;

        isSkill4InProgress = false;
        hasEnteredSkill4State = false;
        isSkill4Active = false;
        skill4ActiveUntil = 0f;
        skill4ReadyAt = 0f;
        if (animator != null)
            animator.ResetTrigger(AsuraUltimateHash);
        if (skill4EffectRenderer != null)
            skill4EffectRenderer.sprite = null;
        StopSkill4Aura();
        if (skillHud != null)
            skillHud.SetSkillInteractable(4, true);

        StopAutoAttack();
    }

    private void OnValidate()
    {
        targetSearchRadius = Mathf.Max(0.1f, targetSearchRadius);
        basicAttackManaCost = Mathf.Max(0, basicAttackManaCost);
        skill4AuraFrameRate = Mathf.Max(1f, skill4AuraFrameRate);
        skill4AuraScale = Mathf.Max(0.01f, skill4AuraScale);
        skill4AuraSortingOrder = Mathf.Max(1, skill4AuraSortingOrder);
        skill4ActiveDuration = Mathf.Max(0.1f, skill4ActiveDuration);
        skill4Cooldown = Mathf.Max(0f, skill4Cooldown);

        if (string.IsNullOrWhiteSpace(attackStateName))
            attackStateName = "Zoro_Attack";

        if (string.IsNullOrWhiteSpace(skill4StateName))
            skill4StateName = "Zoro_Asura_Ultimate";

        attackStateHash = Animator.StringToHash(attackStateName);
        skill4StateHash = Animator.StringToHash(skill4StateName);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, targetSearchRadius);
    }
}
