using UnityEngine;
using UnityEngine.UI;
using System.Text;

[DisallowMultipleComponent]
public sealed class InventoryEquipmentHUD : MonoBehaviour
{
    private const int InventorySlots = 24;
    private const int EquipmentSlots = 6;
    private static readonly string[] EquipmentNames = { "Vũ khí", "Đầu", "Áo", "Quần", "Giày", "Phụ kiện" };

    private NetworkPlayerController controller;
    private SpriteRenderer portraitRenderer;
    private GameObject canvasObject;
    private GameObject window;
    private Image portraitImage;
    private Text characterNameText;
    private readonly Button[] inventoryButtons = new Button[InventorySlots];
    private readonly Image[] inventoryIcons = new Image[InventorySlots];
    private readonly Text[] inventoryQuantities = new Text[InventorySlots];
    private readonly Image[] equipmentIcons = new Image[EquipmentSlots];
    private readonly Text[] equipmentNames = new Text[EquipmentSlots];
    private RectTransform windowRect;
    private RectTransform tooltipRect;
    private Text tooltipTitle;
    private Text tooltipMeta;
    private Text tooltipStats;
    private Text tooltipDescription;
    private Text capacityText;
    private Image detailIcon;
    private Text detailTitle;
    private Text detailDescription;
    private Button primaryButton;
    private Text primaryButtonText;
    private Button dropButton;
    private int selectedInventoryIndex = -1;
    private int hoveredInventoryIndex = -1;
    private int hoveredEquipmentIndex = -1;
    private bool hudVisible = true;

    private void Awake()
    {
        portraitRenderer = GetComponent<SpriteRenderer>();
        BuildHud();
    }

    private void Update()
    {
        if (hudVisible && Input.GetKeyDown(KeyCode.I))
            Toggle();
        if (window != null && window.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Close();
        if (window != null && window.activeSelf && portraitImage != null && portraitRenderer != null)
        {
            portraitImage.sprite = portraitRenderer.sprite;
            portraitImage.enabled = portraitImage.sprite != null;
        }
    }

    public void Bind(NetworkPlayerController networkController)
    {
        if (controller != null)
            controller.InventoryChanged -= Refresh;
        controller = networkController;
        if (controller != null)
            controller.InventoryChanged += Refresh;
        Refresh();
    }

    public void SetHudVisible(bool visible)
    {
        hudVisible = visible;
        if (!visible)
            Close();
    }

    private void Toggle()
    {
        bool open = window != null && !window.activeSelf;
        canvasObject?.SetActive(open);
        window?.SetActive(open);
        if (open)
            Refresh();
    }

    private void Close()
    {
        HideTooltip();
        window?.SetActive(false);
        canvasObject?.SetActive(false);
    }

    private void BuildHud()
    {
        canvasObject = PirateUIRuntime.CreateCanvas("Inventory Equipment Canvas", 160);
        RectTransform root = PirateUIRuntime.Window(canvasObject.transform, "Inventory Window", "HÀNH TRANG", new Vector2(1540f, 880f));
        windowRect = root;
        window = root.gameObject;

        Button close = PirateUIRuntime.Button("Close", root, "×", new Vector2(-52f, -51f), new Vector2(52f, 48f), 28);
        RectTransform closeRect = close.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        close.onClick.AddListener(Close);

        BuildCharacterPanel(root);
        BuildBagPanel(root);
        BuildTooltip(root);
        Close();
    }

    private void BuildCharacterPanel(Transform root)
    {
        RectTransform panel = PirateUIRuntime.Panel("Character Equipment", root, new Vector2(72f, -148f), new Vector2(505f, 646f));

        characterNameText = PirateUIRuntime.Text("Character Name", panel, 26, TextAnchor.MiddleCenter);
        PirateUIRuntime.Set(characterNameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(18f, -12f), new Vector2(-36f, 40f), new Vector2(0f, 1f));
        characterNameText.text = "KIẾM SĨ";
        characterNameText.color = PirateUIRuntime.Gold;
        PirateUIRuntime.Outline(characterNameText, 2f);

        Text heading = PirateUIRuntime.Text("Equipment Heading", panel, 16, TextAnchor.MiddleCenter);
        PirateUIRuntime.Set(heading.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(18f, -52f), new Vector2(-36f, 28f), new Vector2(0f, 1f));
        heading.text = "TRANG BỊ";
        heading.color = PirateUIRuntime.Cream;

        RectTransform portrait = PirateUIRuntime.Rect("Character Portrait", panel);
        PirateUIRuntime.Set(portrait, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -12f), new Vector2(236f, 380f), new Vector2(0.5f, 0.5f));
        Image portraitBack = portrait.gameObject.AddComponent<Image>();
        portraitBack.color = new Color(0.07f, 0.015f, 0.01f, 0.8f);
        Outline outline = portrait.gameObject.AddComponent<Outline>();
        outline.effectColor = PirateUIRuntime.Gold;
        outline.effectDistance = new Vector2(3f, -3f);
        portraitImage = PirateUIRuntime.Image("Portrait", portrait, Color.white);
        PirateUIRuntime.Stretch(portraitImage.rectTransform, 10f);
        portraitImage.preserveAspect = true;

        for (int i = 0; i < EquipmentSlots; i++)
        {
            bool left = i < 3;
            int row = i % 3;
            BuildEquipmentSlot(panel, i, new Vector2(left ? 16f : 381f, -116f - row * 146f));
        }

        Text hint = PirateUIRuntime.Text("Hint", panel, 14, TextAnchor.MiddleCenter);
        PirateUIRuntime.Set(hint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(18f, 16f), new Vector2(-36f, 29f), new Vector2(0f, 0f));
        hint.text = "Chọn trang bị để tháo  •  C: chỉ số";
        hint.color = PirateUIRuntime.Muted;
    }

    private void BuildEquipmentSlot(Transform parent, int index, Vector2 position)
    {
        Button slot = PirateUIRuntime.Slot("Equipment " + EquipmentNames[index], parent, position, new Vector2(108f, 122f));
        int captured = index;
        slot.onClick.AddListener(() => controller?.RequestUnequipItem((EquipmentSlot)captured));
        BindEquipmentHover(slot.gameObject, captured);

        Text label = PirateUIRuntime.Text("Slot Label", slot.transform, 12, TextAnchor.MiddleCenter);
        PirateUIRuntime.Set(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(3f, -3f), new Vector2(-6f, 22f), new Vector2(0f, 1f));
        label.text = EquipmentNames[index].ToUpperInvariant();
        label.color = PirateUIRuntime.Gold;

        Image icon = PirateUIRuntime.Image("Icon", slot.transform, Color.white);
        PirateUIRuntime.Set(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -4f), new Vector2(72f, 72f), new Vector2(0.5f, 0.5f));
        icon.preserveAspect = true;
        equipmentIcons[index] = icon;

        Text itemName = PirateUIRuntime.Text("Item Name", slot.transform, 11, TextAnchor.MiddleCenter);
        PirateUIRuntime.Set(itemName.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(3f, 3f), new Vector2(-6f, 22f), new Vector2(0f, 0f));
        equipmentNames[index] = itemName;
    }

    private void BuildBagPanel(Transform root)
    {
        RectTransform panel = PirateUIRuntime.Panel("Bag", root, new Vector2(610f, -148f), new Vector2(858f, 646f));

        Text heading = PirateUIRuntime.Text("Bag Heading", panel, 27, TextAnchor.MiddleLeft);
        PirateUIRuntime.Set(heading.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(20f, -12f), new Vector2(-210f, 40f), new Vector2(0f, 1f));
        heading.text = "TÚI ĐỒ";
        heading.color = PirateUIRuntime.Gold;

        capacityText = PirateUIRuntime.Text("Capacity", panel, 17, TextAnchor.MiddleRight);
        PirateUIRuntime.Set(capacityText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-20f, -15f), new Vector2(180f, 32f), new Vector2(1f, 1f));
        capacityText.color = PirateUIRuntime.Cream;

        for (int i = 0; i < InventorySlots; i++)
        {
            int captured = i;
            float x = 20f + (i % 6) * 137f;
            float y = -65f - (i / 6) * 106f;
            Button button = PirateUIRuntime.Slot("Inventory Slot " + (i + 1), panel, new Vector2(x, y), new Vector2(112f, 96f));
            inventoryButtons[i] = button;
            button.onClick.AddListener(() => SelectInventorySlot(captured));
            BindInventoryHover(button.gameObject, captured);

            Text index = PirateUIRuntime.Text("Index", button.transform, 11, TextAnchor.UpperLeft);
            PirateUIRuntime.Stretch(index.rectTransform, 6f);
            index.text = (i + 1).ToString();
            index.color = PirateUIRuntime.Muted;

            Image icon = PirateUIRuntime.Image("Icon", button.transform, Color.white);
            PirateUIRuntime.Set(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(64f, 64f), new Vector2(0.5f, 0.5f));
            icon.preserveAspect = true;
            inventoryIcons[i] = icon;

            Text quantity = PirateUIRuntime.Text("Quantity", button.transform, 15, TextAnchor.LowerRight);
            PirateUIRuntime.Stretch(quantity.rectTransform, 6f);
            quantity.color = Color.white;
            PirateUIRuntime.Outline(quantity, 2f);
            inventoryQuantities[i] = quantity;
        }

        BuildDetails(panel);
    }

    private void BuildTooltip(Transform root)
    {
        tooltipRect = PirateUIRuntime.Panel("Item Tooltip", root, Vector2.zero, new Vector2(410f, 286f));
        tooltipRect.anchorMin = tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipRect.pivot = new Vector2(0f, 1f);

        CanvasGroup canvasGroup = tooltipRect.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        tooltipTitle = PirateUIRuntime.Text("Tooltip Title", tooltipRect, 24, TextAnchor.MiddleLeft);
        PirateUIRuntime.Set(tooltipTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(18f, -12f), new Vector2(-36f, 34f), new Vector2(0f, 1f));
        PirateUIRuntime.Outline(tooltipTitle, 1f);

        tooltipMeta = PirateUIRuntime.Text("Tooltip Meta", tooltipRect, 15, TextAnchor.MiddleLeft);
        PirateUIRuntime.Set(tooltipMeta.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(18f, -50f), new Vector2(-36f, 26f), new Vector2(0f, 1f));
        tooltipMeta.color = PirateUIRuntime.Cream;

        Image divider = PirateUIRuntime.Image("Divider", tooltipRect, new Color(0.86f, 0.58f, 0.2f, 0.75f));
        PirateUIRuntime.Set(divider.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(16f, -80f), new Vector2(-32f, 2f), new Vector2(0f, 1f));

        tooltipStats = PirateUIRuntime.Text("Tooltip Stats", tooltipRect, 16, TextAnchor.UpperLeft);
        PirateUIRuntime.Set(tooltipStats.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(18f, -92f), new Vector2(-36f, 126f), new Vector2(0f, 1f));
        tooltipStats.color = Color.white;
        tooltipStats.lineSpacing = 1.05f;

        tooltipDescription = PirateUIRuntime.Text("Tooltip Description", tooltipRect, 14, TextAnchor.UpperLeft);
        PirateUIRuntime.Set(tooltipDescription.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(18f, 14f), new Vector2(-36f, 56f), new Vector2(0f, 0f));
        tooltipDescription.color = PirateUIRuntime.Muted;
        tooltipDescription.fontStyle = FontStyle.Italic;

        tooltipRect.gameObject.SetActive(false);
    }

    private void BindInventoryHover(GameObject target, int index)
    {
        InventoryItemHover hover = target.AddComponent<InventoryItemHover>();
        hover.Entered = eventData =>
        {
            hoveredInventoryIndex = index;
            hoveredEquipmentIndex = -1;
            ShowInventoryTooltip(index, eventData.position);
        };
        hover.Moved = eventData => PositionTooltip(eventData.position);
        hover.Exited = _ => HideTooltip();
    }

    private void BindEquipmentHover(GameObject target, int index)
    {
        InventoryItemHover hover = target.AddComponent<InventoryItemHover>();
        hover.Entered = eventData =>
        {
            hoveredInventoryIndex = -1;
            hoveredEquipmentIndex = index;
            ShowEquipmentTooltip(index, eventData.position);
        };
        hover.Moved = eventData => PositionTooltip(eventData.position);
        hover.Exited = _ => HideTooltip();
    }

    private void BuildDetails(Transform panel)
    {
        RectTransform details = PirateUIRuntime.ParchmentCard("Item Details", panel, new Vector2(20f, -502f), new Vector2(818f, 124f));

        detailIcon = PirateUIRuntime.Image("Detail Icon", details, Color.white);
        PirateUIRuntime.Set(detailIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(15f, 0f), new Vector2(92f, 92f), new Vector2(0f, 0.5f));
        detailIcon.preserveAspect = true;

        detailTitle = PirateUIRuntime.Text("Detail Title", details, 20, TextAnchor.MiddleLeft);
        PirateUIRuntime.Set(detailTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(124f, -12f), new Vector2(-390f, 30f), new Vector2(0f, 1f));
        detailTitle.color = PirateUIRuntime.Ink;

        detailDescription = PirateUIRuntime.Text("Detail Description", details, 14, TextAnchor.UpperLeft);
        PirateUIRuntime.Set(detailDescription.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(124f, 11f), new Vector2(-390f, -50f), new Vector2(0f, 0f));
        detailDescription.color = new Color(0.34f, 0.19f, 0.08f, 1f);

        primaryButton = PirateUIRuntime.Button("Primary Action", details, "TRANG BỊ", new Vector2(-188f, 18f), new Vector2(166f, 48f), 17);
        RectTransform primaryRect = primaryButton.GetComponent<RectTransform>();
        primaryRect.anchorMin = primaryRect.anchorMax = new Vector2(1f, 0f);
        primaryRect.pivot = new Vector2(1f, 0f);
        primaryButtonText = primaryButton.GetComponentInChildren<Text>();
        primaryButton.onClick.AddListener(PerformPrimaryAction);

        dropButton = PirateUIRuntime.Button("Drop", details, "BỎ", new Vector2(-15f, 18f), new Vector2(155f, 48f), 17);
        RectTransform dropRect = dropButton.GetComponent<RectTransform>();
        dropRect.anchorMin = dropRect.anchorMax = new Vector2(1f, 0f);
        dropRect.pivot = new Vector2(1f, 0f);
        dropButton.onClick.AddListener(DropSelectedItem);
    }

    private void SelectInventorySlot(int index)
    {
        selectedInventoryIndex = index;
        Refresh();
    }

    private void ShowInventoryTooltip(int index, Vector2 screenPosition)
    {
        if (controller == null)
        {
            HideTooltip();
            return;
        }

        InventorySlotState slot = controller.GetInventorySlot(index);
        ItemDefinition definition = ItemCatalog.Get(slot.ItemId);
        if (definition == null || slot.IsEmpty)
        {
            HideTooltip();
            return;
        }

        ItemDefinition equipped = null;
        if (definition.Kind == ItemKind.Equipment)
        {
            InventorySlotState equippedSlot = controller.GetEquipmentSlot(definition.EquipmentSlot);
            equipped = ItemCatalog.Get(equippedSlot.ItemId);
        }

        PopulateTooltip(definition, slot.Quantity, equipped, false);
        PositionTooltip(screenPosition);
    }

    private void ShowEquipmentTooltip(int index, Vector2 screenPosition)
    {
        if (controller == null)
        {
            HideTooltip();
            return;
        }

        InventorySlotState slot = controller.GetEquipmentSlot((EquipmentSlot)index);
        ItemDefinition definition = ItemCatalog.Get(slot.ItemId);
        if (definition == null || slot.IsEmpty)
        {
            HideTooltip();
            return;
        }

        PopulateTooltip(definition, slot.Quantity, null, true);
        PositionTooltip(screenPosition);
    }

    private void PopulateTooltip(ItemDefinition definition, int quantity, ItemDefinition equipped, bool isEquipped)
    {
        tooltipTitle.text = definition.DisplayName;
        tooltipTitle.color = GetRarityColor(definition.Rarity);
        tooltipMeta.text = $"Cấp {definition.Level}  •  {GetRarityName(definition.Rarity)}";

        StringBuilder stats = new StringBuilder();
        if (definition.Kind == ItemKind.Equipment)
        {
            if (isEquipped)
                stats.AppendLine("<color=#F3C969>Đang trang bị</color>");
            else
                stats.AppendLine(equipped != null
                    ? $"So với: <color=#F3C969>{equipped.DisplayName}</color>"
                    : "So với: <color=#A7E68A>Ô trống</color>");

            bool compare = !isEquipped;
            AppendStat(stats, "Sức mạnh", definition.Strength, equipped?.Strength ?? 0, compare);
            AppendStat(stats, "Sinh lực", definition.Vitality, equipped?.Vitality ?? 0, compare);
            AppendStat(stats, "Nhanh nhẹn", definition.Agility, equipped?.Agility ?? 0, compare);
            AppendStat(stats, "Năng lượng", definition.Energy, equipped?.Energy ?? 0, compare);
        }
        else
        {
            stats.AppendLine($"Hồi phục: <color=#A7E68A>+{definition.HealAmount} HP</color>");
            if (quantity > 1)
                stats.AppendLine($"Số lượng: {quantity}");
        }

        tooltipStats.text = stats.ToString().TrimEnd();
        tooltipDescription.text = definition.Description;
        tooltipRect.gameObject.SetActive(true);
        tooltipRect.SetAsLastSibling();
    }

    private static void AppendStat(StringBuilder builder, string label, int candidate, int current, bool showComparison)
    {
        if (candidate == 0 && current == 0)
            return;

        builder.Append(label).Append(": ").Append(candidate >= 0 ? "+" : string.Empty).Append(candidate);
        if (showComparison)
        {
            int difference = candidate - current;
            if (difference > 0)
                builder.Append("  <color=#78D879>(+").Append(difference).Append(")</color>");
            else if (difference < 0)
                builder.Append("  <color=#F07167>(").Append(difference).Append(")</color>");
            else
                builder.Append("  <color=#C9BDA9>(=)</color>");
        }
        builder.AppendLine();
    }

    private void PositionTooltip(Vector2 screenPosition)
    {
        if (tooltipRect == null || windowRect == null || !tooltipRect.gameObject.activeSelf)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(windowRect, screenPosition, null, out Vector2 localPoint))
            return;

        const float padding = 12f;
        Vector2 desired = localPoint + new Vector2(24f, -20f);
        Rect bounds = windowRect.rect;
        float maxX = bounds.xMax - tooltipRect.rect.width - padding;
        float minY = bounds.yMin + tooltipRect.rect.height + padding;
        desired.x = Mathf.Clamp(desired.x, bounds.xMin + padding, maxX);
        desired.y = Mathf.Clamp(desired.y, minY, bounds.yMax - padding);
        tooltipRect.anchoredPosition = desired;
    }

    private void HideTooltip()
    {
        hoveredInventoryIndex = -1;
        hoveredEquipmentIndex = -1;
        if (tooltipRect != null)
            tooltipRect.gameObject.SetActive(false);
    }

    private static string GetRarityName(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Uncommon => "Không phổ biến",
            ItemRarity.Rare => "Hiếm",
            ItemRarity.Epic => "Sử thi",
            ItemRarity.Legendary => "Huyền thoại",
            _ => "Thường"
        };
    }

    private static Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Uncommon => new Color(0.43f, 0.86f, 0.38f),
            ItemRarity.Rare => new Color(0.3f, 0.64f, 1f),
            ItemRarity.Epic => new Color(0.72f, 0.42f, 1f),
            ItemRarity.Legendary => new Color(1f, 0.58f, 0.16f),
            _ => Color.white
        };
    }

    private void PerformPrimaryAction()
    {
        if (controller == null || selectedInventoryIndex < 0)
            return;
        InventorySlotState slot = controller.GetInventorySlot(selectedInventoryIndex);
        ItemDefinition definition = ItemCatalog.Get(slot.ItemId);
        if (definition == null)
            return;
        if (definition.Kind == ItemKind.Equipment)
            controller.RequestEquipItem(selectedInventoryIndex);
        else
            controller.RequestUseItem(selectedInventoryIndex);
    }

    private void DropSelectedItem()
    {
        if (controller != null && selectedInventoryIndex >= 0)
            controller.RequestDropItem(selectedInventoryIndex);
    }

    private void Refresh()
    {
        if (controller == null || detailTitle == null)
            return;

        PlayerStats stats = GetComponent<PlayerStats>();
        characterNameText.text = stats != null ? $"{stats.CharacterName.ToUpperInvariant()}  •  Lv.{stats.Level}" : "KIẾM SĨ";

        int occupied = 0;
        for (int i = 0; i < InventorySlots; i++)
        {
            InventorySlotState slot = controller.GetInventorySlot(i);
            ItemDefinition definition = ItemCatalog.Get(slot.ItemId);
            bool hasItem = definition != null && !slot.IsEmpty;
            if (hasItem) occupied++;
            inventoryIcons[i].sprite = hasItem ? ItemIconLibrary.Get(slot.ItemId) : null;
            inventoryIcons[i].enabled = hasItem && inventoryIcons[i].sprite != null;
            inventoryQuantities[i].text = hasItem && slot.Quantity > 1 ? $"×{slot.Quantity}" : string.Empty;
            inventoryButtons[i].image.color = i == selectedInventoryIndex
                ? new Color(1f, 0.78f, 0.34f, 1f)
                : Color.white;
        }
        capacityText.text = $"{occupied} / {InventorySlots} Ô";

        for (int i = 0; i < EquipmentSlots; i++)
        {
            InventorySlotState slot = controller.GetEquipmentSlot((EquipmentSlot)i);
            ItemDefinition definition = ItemCatalog.Get(slot.ItemId);
            bool equipped = definition != null && !slot.IsEmpty;
            equipmentIcons[i].sprite = equipped ? ItemIconLibrary.Get(slot.ItemId) : null;
            equipmentIcons[i].enabled = equipped && equipmentIcons[i].sprite != null;
            equipmentNames[i].text = equipped ? string.Empty : "—";
            equipmentNames[i].color = PirateUIRuntime.Muted;
        }

        InventorySlotState selected = controller.GetInventorySlot(selectedInventoryIndex);
        ItemDefinition selectedDefinition = ItemCatalog.Get(selected.ItemId);
        bool hasSelection = selectedDefinition != null && !selected.IsEmpty;
        detailIcon.sprite = hasSelection ? ItemIconLibrary.Get(selected.ItemId) : null;
        detailIcon.enabled = hasSelection && detailIcon.sprite != null;
        detailTitle.text = hasSelection ? selectedDefinition.DisplayName : "CHỌN VẬT PHẨM";
        detailDescription.text = hasSelection
            ? $"{selectedDefinition.Description}\nSố lượng: {selected.Quantity}"
            : "Chọn một ô trong túi để xem thông tin và thao tác.";
        primaryButton.gameObject.SetActive(hasSelection);
        dropButton.gameObject.SetActive(hasSelection);
        if (hasSelection)
            primaryButtonText.text = selectedDefinition.Kind == ItemKind.Equipment ? "TRANG BỊ" : "SỬ DỤNG";

        if (hoveredInventoryIndex >= 0)
            ShowInventoryTooltip(hoveredInventoryIndex, Input.mousePosition);
        else if (hoveredEquipmentIndex >= 0)
            ShowEquipmentTooltip(hoveredEquipmentIndex, Input.mousePosition);
    }

    private void OnDestroy()
    {
        if (controller != null)
            controller.InventoryChanged -= Refresh;
        if (canvasObject != null)
            Destroy(canvasObject);
    }
}


