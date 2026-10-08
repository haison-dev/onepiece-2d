using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(CircleCollider2D))]
public sealed class NetworkLootPickup : NetworkBehaviour
{
    private const string ResourceName = "NetworkLootPickup";

    [SerializeField, Min(0.25f)] private float pickupRadius = 0.55f;
    [SerializeField, Min(0f)] private float bobHeight = 0.12f;
    [SerializeField, Min(0f)] private float bobSpeed = 2.5f;

    private readonly NetworkVariable<InventorySlotState> item = new NetworkVariable<InventorySlotState>(
        InventorySlotState.Empty,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private static Sprite pickupSprite;
    private Transform visualRoot;
    private SpriteRenderer orbRenderer;
    private TextMesh label;
    private Vector3 visualStartPosition;
    private float bobPhase;
    private float nextPickupCheckAt;
    private float collectableAt;

    public float PickupRadius => pickupRadius;

    public void ConfigurePickupRadius(float radius)
    {
        pickupRadius = Mathf.Max(0.25f, radius);
        CircleCollider2D pickupCollider = GetComponent<CircleCollider2D>();
        if (pickupCollider != null)
            pickupCollider.radius = pickupRadius;
    }

    private void Awake()
    {
        CircleCollider2D pickupCollider = GetComponent<CircleCollider2D>();
        pickupCollider.isTrigger = true;
        pickupCollider.radius = pickupRadius;
        BuildVisual();
    }

    public override void OnNetworkSpawn()
    {
        item.OnValueChanged += HandleItemChanged;
        bobPhase = (float)(NetworkObjectId % 17u) * 0.37f;
        collectableAt = Time.time + 0.75f;
        RefreshVisual();
    }

    public override void OnNetworkDespawn()
    {
        item.OnValueChanged -= HandleItemChanged;
    }

    private void Update()
    {
        if (visualRoot != null)
        {
            float bob = Mathf.Sin(Time.time * bobSpeed + bobPhase) * bobHeight;
            visualRoot.localPosition = visualStartPosition + Vector3.up * bob;
        }

        if (!IsServer || !IsSpawned || Time.time < collectableAt || Time.time < nextPickupCheckAt)
            return;

        nextPickupCheckAt = Time.time + 0.12f;
        TryCollectForNearbyPlayer();
    }

    public void InitializeServer(InventorySlotState droppedItem)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        item.Value = droppedItem;
        RefreshVisual();
    }

    public static bool SpawnServer(InventorySlotState droppedItem, Vector3 position)
    {
        if (droppedItem.IsEmpty || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return false;

        GameObject prefabObject = Resources.Load<GameObject>(ResourceName);
        NetworkLootPickup prefab = prefabObject != null
            ? prefabObject.GetComponent<NetworkLootPickup>()
            : null;
        if (prefab == null)
        {
            Debug.LogError($"Missing Resources/{ResourceName} network prefab.");
            return false;
        }

        NetworkLootPickup pickup = Instantiate(prefab, position, Quaternion.identity);
        pickup.NetworkObject.Spawn(true);
        pickup.InitializeServer(droppedItem);
        return true;
    }

    private void TryCollectForNearbyPlayer()
    {
        InventorySlotState currentItem = item.Value;
        if (currentItem.IsEmpty)
            return;

        NetworkPlayerController[] players = FindObjectsByType<NetworkPlayerController>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (NetworkPlayerController player in players)
        {
            if (!player.IsSpawned || Vector2.Distance(transform.position, player.transform.position) > pickupRadius)
                continue;

            if (!player.TryCollectLootServer(currentItem))
                continue;

            NetworkObject.Despawn(true);
            return;
        }
    }

    private void HandleItemChanged(InventorySlotState previousValue, InventorySlotState newValue)
    {
        RefreshVisual();
    }

    private void BuildVisual()
    {
        GameObject root = new GameObject("Loot Visual");
        visualRoot = root.transform;
        visualRoot.SetParent(transform, false);
        visualStartPosition = Vector3.zero;

        GameObject orbObject = new GameObject("Loot Orb");
        orbObject.transform.SetParent(visualRoot, false);
        orbObject.transform.localScale = Vector3.one * 0.55f;
        orbRenderer = orbObject.AddComponent<SpriteRenderer>();
        orbRenderer.sprite = GetOrCreatePickupSprite();
        orbRenderer.sortingOrder = 120;

        GameObject labelObject = new GameObject("Loot Label");
        labelObject.transform.SetParent(visualRoot, false);
        labelObject.transform.localPosition = new Vector3(0f, 0.48f, 0f);
        label = labelObject.AddComponent<TextMesh>();
        label.anchor = TextAnchor.LowerCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 40;
        label.characterSize = 0.045f;
        label.color = Color.white;
        label.GetComponent<MeshRenderer>().sortingOrder = 121;
    }

    private void RefreshVisual()
    {
        if (label == null || orbRenderer == null)
            return;

        InventorySlotState currentItem = item.Value;
        ItemDefinition definition = ItemCatalog.Get(currentItem.ItemId);
        label.text = definition == null
            ? "Loot"
            : $"{definition.DisplayName} x{currentItem.Quantity}\nĐến gần để nhặt";

        orbRenderer.color = currentItem.ItemId == ItemId.HealthPotion
            ? new Color(0.95f, 0.18f, 0.2f, 1f)
            : new Color(1f, 0.78f, 0.15f, 1f);
    }

    private static Sprite GetOrCreatePickupSprite()
    {
        if (pickupSprite != null)
            return pickupSprite;

        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Runtime Loot Orb";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.46f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius - distance);
                float shine = Mathf.Clamp01(1f - distance / radius);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha * (0.6f + shine * 0.4f));
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        pickupSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size
        );
        pickupSprite.name = "Runtime Loot Orb";
        return pickupSprite;
    }

    private void OnValidate()
    {
        pickupRadius = Mathf.Max(0.25f, pickupRadius);
        bobHeight = Mathf.Max(0f, bobHeight);
        bobSpeed = Mathf.Max(0f, bobSpeed);
    }
}