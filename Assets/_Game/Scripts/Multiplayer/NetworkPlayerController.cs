using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(NetworkAnimator))]
[RequireComponent(typeof(PlayerMovement2D))]
public sealed class NetworkPlayerController : NetworkBehaviour
{
    private const int InventorySlotCount = 24;
    private const int EquipmentSlotCount = 6;
    [SerializeField, Min(0.05f)] private float inputResendInterval = 0.1f;
    [SerializeField, Min(0f)] private float attackCooldown = 0.55f;
    [SerializeField, Min(0.1f)] private float targetSearchRadius = 8f;
    [SerializeField, Min(0)] private int basicAttackManaCost = 1;
    [SerializeField, Min(0)] private int skill4ManaCost = 20;
    [SerializeField, Min(0.1f)] private float skill4ActiveDuration = 20f;
    [SerializeField, Min(0f)] private float skill4Cooldown = 1f;
    [SerializeField, Min(1f)] private float skill4DamageMultiplier = 2f;
    [SerializeField, Min(0f)] private float playerSpawnSpacing = 2f;

    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AsuraUltimateHash = Animator.StringToHash("AsuraUltimate");

    private readonly NetworkVariable<bool> facingLeft = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<int> synchronizedHealth = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<int> synchronizedMana = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<int> synchronizedLevel = new NetworkVariable<int>(1);
    private readonly NetworkVariable<int> synchronizedExperience = new NetworkVariable<int>(0);
    private readonly NetworkVariable<int> synchronizedExperienceToNext = new NetworkVariable<int>(100);
    private readonly NetworkVariable<int> synchronizedPotentialPoints = new NetworkVariable<int>(0);
    private readonly NetworkVariable<int> synchronizedStrength = new NetworkVariable<int>(0);
    private readonly NetworkVariable<int> synchronizedVitality = new NetworkVariable<int>(0);
    private readonly NetworkVariable<int> synchronizedAgility = new NetworkVariable<int>(0);
    private readonly NetworkVariable<int> synchronizedEnergy = new NetworkVariable<int>(0);

    private readonly NetworkVariable<bool> synchronizedAutoAttack = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<bool> synchronizedSkill4Active = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<double> synchronizedSkill4ActiveUntil = new NetworkVariable<double>(
        0d,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<double> synchronizedSkill4ReadyAt = new NetworkVariable<double>(
        0d,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkList<InventorySlotState> inventory = new NetworkList<InventorySlotState>();
    private readonly NetworkList<InventorySlotState> equipment = new NetworkList<InventorySlotState>();

    private PlayerMovement2D movement;
    private PlayerAttackHitbox2D attackHitbox;
    private ZoroAttackInput attackInput;
    private PlayerStats stats;
    private TargetSystem targetSystem;
    private TargetBossHUD targetHud;
    private CharacterProgressionHUD progressionHud;
    private InventoryEquipmentHUD inventoryHud;
    private PlayerHUD playerHud;
    private PlayerSkillHUD skillHud;
    private SpriteRenderer spriteRenderer;
    private NetworkAnimator networkAnimator;
    private Vector2 lastSentInput;
    private float nextInputSendAt;
    private float nextAttackAt;
    private EnemyHealth selectedClientTarget;
    private NetworkObject selectedServerTarget;
    private bool isServerAutoAttackEnabled;
    private bool hasPresentedSkill4State;
    private bool lastPresentedSkill4Active;

    public event System.Action InventoryChanged;
    public int InventoryCapacity => inventory.Count;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement2D>();
        attackHitbox = GetComponent<PlayerAttackHitbox2D>();
        attackInput = GetComponent<ZoroAttackInput>();
        stats = GetComponent<PlayerStats>();
        targetSystem = GetComponent<TargetSystem>();
        if (targetSystem == null)
            targetSystem = gameObject.AddComponent<TargetSystem>();
        targetHud = GetComponent<TargetBossHUD>();
        if (targetHud == null)
            targetHud = gameObject.AddComponent<TargetBossHUD>();
        progressionHud = GetComponent<CharacterProgressionHUD>();
        if (progressionHud == null)
            progressionHud = gameObject.AddComponent<CharacterProgressionHUD>();
        inventoryHud = GetComponent<InventoryEquipmentHUD>();
        if (inventoryHud == null)
            inventoryHud = gameObject.AddComponent<InventoryEquipmentHUD>();
        playerHud = GetComponent<PlayerHUD>();
        skillHud = GetComponent<PlayerSkillHUD>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        networkAnimator = GetComponent<NetworkAnimator>();
    }

    public override void OnNetworkSpawn()
    {
        facingLeft.OnValueChanged += HandleFacingChanged;
        synchronizedHealth.OnValueChanged += HandleHealthChanged;
        synchronizedMana.OnValueChanged += HandleManaChanged;
        SubscribeProgressionVariables();
        synchronizedAutoAttack.OnValueChanged += HandleAutoAttackChanged;
        synchronizedSkill4Active.OnValueChanged += HandleSkill4ActiveChanged;
        synchronizedSkill4ActiveUntil.OnValueChanged += HandleSkill4TimingChanged;
        synchronizedSkill4ReadyAt.OnValueChanged += HandleSkill4TimingChanged;
        inventory.OnListChanged += HandleInventoryListChanged;
        equipment.OnListChanged += HandleEquipmentListChanged;
        ApplyFacing(facingLeft.Value);

        movement.ConfigureNetworkControl(IsServer);
        attackHitbox.SetDamageEnabled(IsServer);
        attackHitbox.SetDamagePopupEnabled(false);

        if (IsServer)
        {
            stats.StatsChanged += HandleServerStatsChanged;
            attackHitbox.DamageApplied += HandleServerDamageApplied;
            attackHitbox.EnemyDefeated += HandleServerEnemyDefeated;
            synchronizedHealth.Value = stats.CurrentHealth;
            synchronizedMana.Value = stats.CurrentMana;
            SynchronizeServerProgression();
            synchronizedAutoAttack.Value = false;
            synchronizedSkill4Active.Value = false;
            synchronizedSkill4ActiveUntil.Value = 0d;
            synchronizedSkill4ReadyAt.Value = 0d;
            InitializeServerInventory();
        }
        else
        {
            ApplySynchronizedProgression();

            if (synchronizedHealth.Value > 0)
                stats.ApplyNetworkHealth(synchronizedHealth.Value);

            stats.ApplyNetworkMana(synchronizedMana.Value);
        }

        // Initial NetworkList contents may be synchronized before OnNetworkSpawn.
        // Apply equipped bonuses now as well as on later list changes.
        RefreshEquipmentBonuses();

        // Network input is routed by this component. The existing offline
        // controller remains available when this prefab is not network-spawned.
        if (attackInput != null)
            attackInput.enabled = false;

        attackInput.SetNetworkAutoAttackVisual(synchronizedAutoAttack.Value);

        if (playerHud != null)
            playerHud.SetHudVisible(IsOwner);

        targetHud.SetHudVisible(IsOwner);
        progressionHud.SetHudVisible(IsOwner);
        if (IsOwner)
        {
            progressionHud.Bind(this);
            progressionHud.PotentialPointRequested += RequestPotentialPoint;
        }

        inventoryHud.SetHudVisible(IsOwner);
        if (IsOwner)
            inventoryHud.Bind(this);

        if (skillHud != null)
        {
            skillHud.SetHudVisible(IsOwner);
            skillHud.enabled = IsOwner;
            if (IsOwner)
            {
                skillHud.SkillPressed += HandleNetworkSkillPressed;
                skillHud.BasicAttackPressed += RequestToggleAutoAttack;
            }
        }

        if (IsServer)
        {
            Vector3 spawnPosition = transform.position;
            spawnPosition.x += OwnerClientId * playerSpawnSpacing;
            transform.position = spawnPosition;
        }

        if (IsOwner)
            AttachMainCamera();
    }

    public override void OnNetworkDespawn()
    {
        facingLeft.OnValueChanged -= HandleFacingChanged;
        synchronizedHealth.OnValueChanged -= HandleHealthChanged;
        synchronizedMana.OnValueChanged -= HandleManaChanged;
        UnsubscribeProgressionVariables();
        synchronizedAutoAttack.OnValueChanged -= HandleAutoAttackChanged;
        synchronizedSkill4Active.OnValueChanged -= HandleSkill4ActiveChanged;
        synchronizedSkill4ActiveUntil.OnValueChanged -= HandleSkill4TimingChanged;
        synchronizedSkill4ReadyAt.OnValueChanged -= HandleSkill4TimingChanged;
        inventory.OnListChanged -= HandleInventoryListChanged;
        equipment.OnListChanged -= HandleEquipmentListChanged;

        if (skillHud != null)
        {
            skillHud.SkillPressed -= HandleNetworkSkillPressed;
            skillHud.BasicAttackPressed -= RequestToggleAutoAttack;
        }

        if (progressionHud != null)
            progressionHud.PotentialPointRequested -= RequestPotentialPoint;

        if (stats != null)
            stats.StatsChanged -= HandleServerStatsChanged;

        if (attackHitbox != null)
        {
            attackHitbox.DamageApplied -= HandleServerDamageApplied;
            attackHitbox.EnemyDefeated -= HandleServerEnemyDefeated;
        }

        if (IsServer)
            StopServerAutoAttack(true);
    }

    private void Update()
    {
        if (!IsSpawned || NetworkManager == null || !NetworkManager.IsListening)
            return;

        if (IsSpawned)
            UpdateNetworkSkill4Presentation();

        if (IsServer && IsSpawned)
        {
            UpdateServerSkill4Lifetime();
            UpdateServerAutoAttack();
        }

        if (!IsOwner || !IsSpawned)
            return;

        Vector2 input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;

        bool inputChanged = (input - lastSentInput).sqrMagnitude > 0.0001f;
        if (inputChanged || Time.unscaledTime >= nextInputSendAt)
        {
            lastSentInput = input;
            nextInputSendAt = Time.unscaledTime + inputResendInterval;
            SubmitMovementRpc(input);
        }

        if (Input.GetKeyDown(KeyCode.Tab))
            SelectNextClientTarget();

        if (Input.GetKeyDown(KeyCode.Q))
            RequestToggleAutoAttack();

        if (Input.GetKeyDown(KeyCode.Alpha4))
            RequestSkill4Rpc();
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
    private void SubmitMovementRpc(Vector2 input)
    {
        Vector2 safeInput = Vector2.ClampMagnitude(input, 1f);

        if (isServerAutoAttackEnabled)
        {
            if (safeInput.sqrMagnitude <= 0.0001f)
                return;

            StopServerAutoAttack(false);
        }

        movement.SetNetworkMovementInput(safeInput);

        if (!Mathf.Approximately(safeInput.x, 0f))
            facingLeft.Value = safeInput.x < 0f;
    }

    [Rpc(SendTo.Server)]
    private void ToggleAutoAttackRpc()
    {
        if (isServerAutoAttackEnabled)
        {
            StopServerAutoAttack(false);
            return;
        }

        if (stats == null || stats.IsDead || targetSystem == null)
            return;

        EnemyHealth enemy = targetSystem.FindNearestEnemy(targetSearchRadius);
        if (enemy == null ||
            !enemy.TryGetComponent(out NetworkObject targetObject) ||
            !targetObject.IsSpawned)
            return;

        selectedServerTarget = targetObject;
        isServerAutoAttackEnabled = true;
        synchronizedAutoAttack.Value = true;
    }

    private void UpdateServerAutoAttack()
    {
        if (!isServerAutoAttackEnabled)
            return;

        if (stats == null || stats.IsDead || selectedServerTarget == null ||
            !selectedServerTarget.IsSpawned)
        {
            StopServerAutoAttack(true);
            return;
        }

        EnemyHealth target = selectedServerTarget.GetComponent<EnemyHealth>();
        if (targetSystem == null ||
            !targetSystem.IsValid(target, targetSearchRadius))
        {
            StopServerAutoAttack(true);
            return;
        }

        // Getting hit only pauses auto attack. The flag and target are kept,
        // so the server resumes approaching/attacking when the lock ends.
        if (movement.IsMovementLocked)
        {
            movement.SetNetworkMovementInput(Vector2.zero);
            return;
        }

        Vector2 toTarget = target.transform.position - transform.position;
        if (!Mathf.Approximately(toTarget.x, 0f))
            facingLeft.Value = toTarget.x < 0f;

        if (!attackHitbox.IsTargetInRange(target))
        {
            movement.SetNetworkMovementInput(toTarget.normalized);
            return;
        }

        movement.SetNetworkMovementInput(Vector2.zero);
        if (Time.time < nextAttackAt)
            return;

        if (basicAttackManaCost > 0 &&
            !stats.TrySpendMana(basicAttackManaCost))
        {
            StopServerAutoAttack(false);
            return;
        }

        nextAttackAt = Time.time + attackCooldown;
        networkAnimator.ResetTrigger(AttackHash);
        networkAnimator.SetTrigger(AttackHash);
    }

    private void StopServerAutoAttack(bool clearTarget)
    {
        isServerAutoAttackEnabled = false;
        if (IsServer)
            synchronizedAutoAttack.Value = false;
        if (clearTarget)
            selectedServerTarget = null;

        if (movement != null)
            movement.SetNetworkMovementInput(Vector2.zero);
    }

    private void SelectNextClientTarget()
    {
        if (targetSystem == null)
            return;

        selectedClientTarget = targetSystem.FindNextEnemy(
            selectedClientTarget,
            targetSearchRadius
        );
        targetHud.SetTarget(selectedClientTarget);

        if (selectedClientTarget != null &&
            selectedClientTarget.TryGetComponent(out NetworkObject targetObject))
        {
            SelectAutoAttackTargetRpc(new NetworkObjectReference(targetObject));
        }
    }

    [Rpc(SendTo.Server)]
    private void SelectAutoAttackTargetRpc(NetworkObjectReference targetReference)
    {
        if (!isServerAutoAttackEnabled ||
            !targetReference.TryGet(out NetworkObject targetObject, NetworkManager) ||
            targetObject == null || !targetObject.IsSpawned)
            return;

        EnemyHealth enemy = targetObject.GetComponent<EnemyHealth>();
        if (targetSystem.IsValid(enemy, targetSearchRadius))
            selectedServerTarget = targetObject;
    }

    private void RequestToggleAutoAttack()
    {
        // The local lookup updates this client's target HUD immediately.
        // The server performs its own nearest-target lookup for authority.
        selectedClientTarget = targetSystem.FindNearestEnemy(targetSearchRadius);
        targetHud.SetTarget(selectedClientTarget);
        ToggleAutoAttackRpc();
    }

    [Rpc(SendTo.Server)]
    private void RequestSkill4Rpc()
    {
        double now = NetworkManager.ServerTime.Time;
        if (stats == null || stats.IsDead || movement.IsMovementLocked ||
            synchronizedSkill4Active.Value || now < synchronizedSkill4ReadyAt.Value)
            return;

        if (skill4ManaCost > 0 && !stats.TrySpendMana(skill4ManaCost))
            return;

        StopServerAutoAttack(false);
        movement.SetNetworkMovementInput(Vector2.zero);
        attackHitbox.SetDamageMultiplier(skill4DamageMultiplier);

        synchronizedSkill4ActiveUntil.Value = now + skill4ActiveDuration;
        synchronizedSkill4ReadyAt.Value =
            synchronizedSkill4ActiveUntil.Value + skill4Cooldown;
        synchronizedSkill4Active.Value = true;

        networkAnimator.ResetTrigger(AttackHash);
        networkAnimator.ResetTrigger(AsuraUltimateHash);
        networkAnimator.SetTrigger(AsuraUltimateHash);
    }

    private void UpdateServerSkill4Lifetime()
    {
        if (!synchronizedSkill4Active.Value ||
            NetworkManager.ServerTime.Time < synchronizedSkill4ActiveUntil.Value)
            return;

        synchronizedSkill4Active.Value = false;
        attackHitbox.SetDamageMultiplier(1f);
    }

    private void UpdateNetworkSkill4Presentation()
    {
        if (attackInput == null)
            return;

        double now = NetworkManager.ServerTime.Time;
        bool active = synchronizedSkill4Active.Value &&
            now < synchronizedSkill4ActiveUntil.Value;
        float activeRemaining = Mathf.Max(
            0f,
            (float)(synchronizedSkill4ActiveUntil.Value - now)
        );
        float readyRemaining = Mathf.Max(
            0f,
            (float)(synchronizedSkill4ReadyAt.Value - now)
        );

        if (!hasPresentedSkill4State || active != lastPresentedSkill4Active)
        {
            attackInput.ApplyNetworkSkill4State(
                active,
                activeRemaining,
                readyRemaining
            );
            hasPresentedSkill4State = true;
            lastPresentedSkill4Active = active;
        }

        attackInput.TickNetworkSkill4Presentation();
    }

    private void HandleNetworkSkillPressed(int skillNumber)
    {
        if (skillNumber == 4)
            RequestSkill4Rpc();
    }

    private void HandleFacingChanged(bool previousValue, bool newValue)
    {
        ApplyFacing(newValue);
    }

    private void HandleServerStatsChanged()
    {
        if (!IsServer)
            return;

        if (synchronizedHealth.Value != stats.CurrentHealth)
            synchronizedHealth.Value = stats.CurrentHealth;

        if (synchronizedMana.Value != stats.CurrentMana)
            synchronizedMana.Value = stats.CurrentMana;

        SynchronizeServerProgression();
    }

    private void HandleHealthChanged(int previousValue, int newValue)
    {
        if (!IsServer)
            stats.ApplyNetworkHealth(newValue);
    }

    private void HandleManaChanged(int previousValue, int newValue)
    {
        if (!IsServer)
            stats.ApplyNetworkMana(newValue);
    }

    private void HandleServerEnemyDefeated(EnemyHealth enemy)
    {
        if (!IsServer || enemy == null)
            return;

        stats.AddExperience(enemy.ExperienceReward);
        var rewards = enemy.RollLoot();
        Vector3 dropOrigin = enemy.transform.position;
        for (int i = 0; i < rewards.Count; i++)
        {
            float angle = rewards.Count <= 1 ? 0f : i * Mathf.PI * 2f / rewards.Count;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.6f, 0f) * 0.7f;
            InventorySlotState reward = rewards[i];

            // Keep rewards recoverable if the pickup prefab has not been generated yet.
            if (!NetworkLootPickup.SpawnServer(reward, dropOrigin + offset))
                TryAddItemServer(reward.ItemId, reward.Quantity);
        }
    }

    private void RequestPotentialPoint(PotentialStat stat)
    {
        AllocatePotentialRpc((int)stat);
    }

    [Rpc(SendTo.Server)]
    private void AllocatePotentialRpc(int statValue)
    {
        if (stats == null || stats.IsDead ||
            !System.Enum.IsDefined(typeof(PotentialStat), statValue))
            return;

        stats.TryAllocatePotential((PotentialStat)statValue);
    }

    private void SynchronizeServerProgression()
    {
        if (!IsServer)
            return;

        synchronizedLevel.Value = stats.Level;
        synchronizedExperience.Value = stats.CurrentExperience;
        synchronizedExperienceToNext.Value = stats.ExperienceToNextLevel;
        synchronizedPotentialPoints.Value = stats.UnspentPotentialPoints;
        synchronizedStrength.Value = stats.AllocatedStrength;
        synchronizedVitality.Value = stats.AllocatedVitality;
        synchronizedAgility.Value = stats.AllocatedAgility;
        synchronizedEnergy.Value = stats.AllocatedEnergy;
    }

    private void ApplySynchronizedProgression()
    {
        if (IsServer || stats == null)
            return;

        stats.ApplyNetworkProgression(
            synchronizedLevel.Value,
            synchronizedExperience.Value,
            synchronizedExperienceToNext.Value,
            synchronizedPotentialPoints.Value,
            synchronizedStrength.Value,
            synchronizedVitality.Value,
            synchronizedAgility.Value,
            synchronizedEnergy.Value
        );
    }

    private void HandleProgressionChanged(int previousValue, int newValue)
    {
        ApplySynchronizedProgression();
    }

    private void SubscribeProgressionVariables()
    {
        synchronizedLevel.OnValueChanged += HandleProgressionChanged;
        synchronizedExperience.OnValueChanged += HandleProgressionChanged;
        synchronizedExperienceToNext.OnValueChanged += HandleProgressionChanged;
        synchronizedPotentialPoints.OnValueChanged += HandleProgressionChanged;
        synchronizedStrength.OnValueChanged += HandleProgressionChanged;
        synchronizedVitality.OnValueChanged += HandleProgressionChanged;
        synchronizedAgility.OnValueChanged += HandleProgressionChanged;
        synchronizedEnergy.OnValueChanged += HandleProgressionChanged;
    }

    private void UnsubscribeProgressionVariables()
    {
        synchronizedLevel.OnValueChanged -= HandleProgressionChanged;
        synchronizedExperience.OnValueChanged -= HandleProgressionChanged;
        synchronizedExperienceToNext.OnValueChanged -= HandleProgressionChanged;
        synchronizedPotentialPoints.OnValueChanged -= HandleProgressionChanged;
        synchronizedStrength.OnValueChanged -= HandleProgressionChanged;
        synchronizedVitality.OnValueChanged -= HandleProgressionChanged;
        synchronizedAgility.OnValueChanged -= HandleProgressionChanged;
        synchronizedEnergy.OnValueChanged -= HandleProgressionChanged;
    }

    public InventorySlotState GetInventorySlot(int index)
    {
        return index >= 0 && index < inventory.Count
            ? inventory[index]
            : InventorySlotState.Empty;
    }

    public InventorySlotState GetEquipmentSlot(EquipmentSlot slot)
    {
        int index = (int)slot;
        return index >= 0 && index < equipment.Count
            ? equipment[index]
            : InventorySlotState.Empty;
    }

    public void RequestEquipItem(int inventoryIndex)
    {
        EquipItemRpc(inventoryIndex);
    }

    public void RequestUnequipItem(EquipmentSlot slot)
    {
        UnequipItemRpc((int)slot);
    }

    public void RequestUseItem(int inventoryIndex)
    {
        UseItemRpc(inventoryIndex);
    }

    public void RequestDropItem(int inventoryIndex)
    {
        DropItemRpc(inventoryIndex);
    }

    [Rpc(SendTo.Server)]
    private void EquipItemRpc(int inventoryIndex)
    {
        if (!IsValidInventoryIndex(inventoryIndex))
            return;

        InventorySlotState selected = inventory[inventoryIndex];
        ItemDefinition definition = ItemCatalog.Get(selected.ItemId);
        if (selected.IsEmpty || definition == null || definition.Kind != ItemKind.Equipment)
            return;

        int equipmentIndex = (int)definition.EquipmentSlot;
        InventorySlotState previouslyEquipped = equipment[equipmentIndex];
        equipment[equipmentIndex] = new InventorySlotState(selected.ItemId, 1);
        inventory[inventoryIndex] = previouslyEquipped.IsEmpty
            ? InventorySlotState.Empty
            : new InventorySlotState(previouslyEquipped.ItemId, 1);
    }

    [Rpc(SendTo.Server)]
    private void UnequipItemRpc(int equipmentIndex)
    {
        if (equipmentIndex < 0 || equipmentIndex >= equipment.Count)
            return;

        InventorySlotState equippedItem = equipment[equipmentIndex];
        if (equippedItem.IsEmpty || !TryAddItemServer(equippedItem.ItemId, 1))
            return;

        equipment[equipmentIndex] = InventorySlotState.Empty;
    }

    [Rpc(SendTo.Server)]
    private void UseItemRpc(int inventoryIndex)
    {
        if (!IsValidInventoryIndex(inventoryIndex) || stats.IsDead)
            return;

        InventorySlotState selected = inventory[inventoryIndex];
        ItemDefinition definition = ItemCatalog.Get(selected.ItemId);
        if (selected.IsEmpty || definition == null ||
            definition.Kind != ItemKind.Consumable || definition.HealAmount <= 0 ||
            stats.CurrentHealth >= stats.MaxHealth)
            return;

        stats.Heal(definition.HealAmount);
        DecreaseInventorySlot(inventoryIndex, 1);
    }

    [Rpc(SendTo.Server)]
    private void DropItemRpc(int inventoryIndex)
    {
        if (IsValidInventoryIndex(inventoryIndex))
            inventory[inventoryIndex] = InventorySlotState.Empty;
    }

    private void InitializeServerInventory()
    {
        if (inventory.Count == 0)
        {
            for (int i = 0; i < InventorySlotCount; i++)
                inventory.Add(InventorySlotState.Empty);

            inventory[0] = new InventorySlotState(ItemId.HealthPotion, 3);
        }

        if (equipment.Count == 0)
        {
            for (int i = 0; i < EquipmentSlotCount; i++)
                equipment.Add(InventorySlotState.Empty);
        }

        RefreshEquipmentBonuses();
    }

    public bool TryCollectLootServer(InventorySlotState droppedItem)
    {
        if (!IsServer || droppedItem.IsEmpty)
            return false;

        return TryAddItemServer(droppedItem.ItemId, droppedItem.Quantity);
    }

    private bool TryAddItemServer(ItemId itemId, int quantity)
    {
        ItemDefinition definition = ItemCatalog.Get(itemId);
        if (definition == null || quantity <= 0 || !HasInventoryCapacity(definition, itemId, quantity))
            return false;

        int remaining = quantity;
        if (definition.MaxStack > 1)
        {
            for (int i = 0; i < inventory.Count && remaining > 0; i++)
            {
                InventorySlotState slot = inventory[i];
                if (slot.ItemId != itemId || slot.Quantity >= definition.MaxStack)
                    continue;

                int added = Mathf.Min(remaining, definition.MaxStack - slot.Quantity);
                inventory[i] = new InventorySlotState(itemId, slot.Quantity + added);
                remaining -= added;
            }
        }

        for (int i = 0; i < inventory.Count && remaining > 0; i++)
        {
            if (!inventory[i].IsEmpty)
                continue;

            int added = Mathf.Min(remaining, definition.MaxStack);
            inventory[i] = new InventorySlotState(itemId, added);
            remaining -= added;
        }

        return remaining == 0;
    }

    private bool HasInventoryCapacity(
        ItemDefinition definition,
        ItemId itemId,
        int quantity)
    {
        int availableSpace = 0;
        for (int i = 0; i < inventory.Count; i++)
        {
            InventorySlotState slot = inventory[i];
            if (slot.IsEmpty)
                availableSpace += definition.MaxStack;
            else if (slot.ItemId == itemId && definition.MaxStack > 1)
                availableSpace += Mathf.Max(0, definition.MaxStack - slot.Quantity);

            if (availableSpace >= quantity)
                return true;
        }

        return false;
    }

    private void DecreaseInventorySlot(int index, int amount)
    {
        InventorySlotState slot = inventory[index];
        int remaining = slot.Quantity - Mathf.Max(1, amount);
        inventory[index] = remaining > 0
            ? new InventorySlotState(slot.ItemId, remaining)
            : InventorySlotState.Empty;
    }

    private bool IsValidInventoryIndex(int index)
    {
        return index >= 0 && index < inventory.Count && !inventory[index].IsEmpty;
    }

    private void HandleInventoryListChanged(NetworkListEvent<InventorySlotState> changeEvent)
    {
        InventoryChanged?.Invoke();
    }

    private void HandleEquipmentListChanged(NetworkListEvent<InventorySlotState> changeEvent)
    {
        RefreshEquipmentBonuses();
        InventoryChanged?.Invoke();
    }

    private void RefreshEquipmentBonuses()
    {
        int strength = 0;
        int vitality = 0;
        int agility = 0;
        int energy = 0;

        for (int i = 0; i < equipment.Count; i++)
        {
            ItemDefinition definition = ItemCatalog.Get(equipment[i].ItemId);
            if (definition == null)
                continue;

            strength += definition.Strength;
            vitality += definition.Vitality;
            agility += definition.Agility;
            energy += definition.Energy;
        }

        stats.ApplyEquipmentBonuses(strength, vitality, agility, energy);
    }

    private void HandleAutoAttackChanged(bool previousValue, bool newValue)
    {
        attackInput?.SetNetworkAutoAttackVisual(newValue);
    }

    private void HandleSkill4ActiveChanged(bool previousValue, bool newValue)
    {
        hasPresentedSkill4State = false;
    }

    private void HandleSkill4TimingChanged(double previousValue, double newValue)
    {
        hasPresentedSkill4State = false;
    }

    private void HandleServerDamageApplied(Vector3 worldPosition, DamageResult result)
    {
        ShowEnemyDamageRpc(worldPosition, result.AppliedDamage, result.IsCritical);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ShowEnemyDamageRpc(Vector3 worldPosition, int amount, bool isCritical)
    {
        DamagePopup.Show(
            worldPosition,
            amount,
            isCritical ? DamagePopupType.Critical : DamagePopupType.Normal
        );
    }

    private void ApplyFacing(bool isFacingLeft)
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = isFacingLeft;
    }

    private void AttachMainCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        CameraFollow2D cameraFollow = mainCamera.GetComponent<CameraFollow2D>();
        if (cameraFollow == null)
            cameraFollow = mainCamera.gameObject.AddComponent<CameraFollow2D>();

        cameraFollow.SetTarget(transform);
    }

    private void OnValidate()
    {
        inputResendInterval = Mathf.Max(0.05f, inputResendInterval);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        targetSearchRadius = Mathf.Max(0.1f, targetSearchRadius);
        basicAttackManaCost = Mathf.Max(0, basicAttackManaCost);
        skill4ManaCost = Mathf.Max(0, skill4ManaCost);
        skill4ActiveDuration = Mathf.Max(0.1f, skill4ActiveDuration);
        skill4Cooldown = Mathf.Max(0f, skill4Cooldown);
        skill4DamageMultiplier = Mathf.Max(1f, skill4DamageMultiplier);
        playerSpawnSpacing = Mathf.Max(0f, playerSpawnSpacing);
    }
}

