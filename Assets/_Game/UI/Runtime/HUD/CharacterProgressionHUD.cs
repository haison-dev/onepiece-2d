using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats))]
public sealed class CharacterProgressionHUD : MonoBehaviour
{
    private const int EquipmentSlotCount = 6;
    private static readonly string[] EquipmentNames = { "Vũ khí", "Đầu", "Áo", "Quần", "Giày", "Phụ kiện" };

    private PlayerStats stats;
    private PlayerAttackHitbox2D attackHitbox;
    private PlayerMovement2D movement;
    private SpriteRenderer portraitRenderer;
    private NetworkPlayerController controller;
    private GameObject canvasObject;
    private GameObject window;
    private GameObject potentialContent;
    private GameObject statsContent;
    private Image portraitImage;
    private Text characterNameText;
    private Text levelText;
    private Text availablePointsText;
    private Text experienceText;
    private Image experienceFill;
    private Button potentialTab;
    private Button statsTab;
    private readonly Image[] equipmentIcons = new Image[EquipmentSlotCount];
    private readonly Text[] equipmentNames = new Text[EquipmentSlotCount];
    private readonly Text[] potentialValues = new Text[4];
    private readonly Button[] potentialButtons = new Button[4];
    private Text healthText;
    private Text manaText;
    private Text damageText;
    private Text criticalText;
    private Text speedText;
    private bool hudVisible = true;

    public event Action<PotentialStat> PotentialPointRequested;

    private void Awake()
    {
        CacheReferences();
        BuildHud();
    }

    private void OnEnable()
    {
        CacheReferences();
        if (stats != null)
        {
            stats.StatsChanged -= Refresh;
            stats.StatsChanged += Refresh;
        }
        Refresh();
    }

    private void OnDisable()
    {
        if (stats != null)
            stats.StatsChanged -= Refresh;
    }

    private void Update()
    {
        if (!hudVisible)
            return;
        if (window != null && window.activeSelf)
            Refresh();
        if (Input.GetKeyDown(KeyCode.C))
            Toggle();
        if (window != null && window.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Close();
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

    private void CacheReferences()
    {
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (attackHitbox == null) attackHitbox = GetComponent<PlayerAttackHitbox2D>();
        if (movement == null) movement = GetComponent<PlayerMovement2D>();
        if (portraitRenderer == null) portraitRenderer = GetComponent<SpriteRenderer>();
    }

    private void Toggle()
    {
        bool open = window != null && !window.activeSelf;
        canvasObject?.SetActive(open);
        window?.SetActive(open);
        if (open)
        {
            ShowTab(true);
            Refresh();
        }
    }

    private void Close()
    {
        window?.SetActive(false);
        canvasObject?.SetActive(false);
    }

    private void BuildHud()
    {
        canvasObject = PirateUIRuntime.CreateCanvas("Character Progression Canvas", 150);
        RectTransform root = PirateUIRuntime.Window(canvasObject.transform, "Character Window", "NHÂN VẬT", new Vector2(1440f, 850f));
        window = root.gameObject;

        Button close = PirateUIRuntime.Button("Close", root, "×", new Vector2(-49f, -48f), new Vector2(52f, 48f), 28);
        RectTransform closeRect = close.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        close.onClick.AddListener(Close);

        BuildCharacterSide(root);
        BuildDetailsSide(root);
        Close();
    }

    private void BuildCharacterSide(Transform root)
    {
        RectTransform panel = PirateUIRuntime.Panel("Character And Equipment", root, new Vector2(70f, -142f), new Vector2(430f, 620f));

        characterNameText = PirateUIRuntime.Text("Character Name", panel, 28, TextAnchor.MiddleCenter);
        PirateUIRuntime.Set(characterNameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(18f, -12f), new Vector2(-36f, 42f), new Vector2(0f, 1f));
        characterNameText.color = PirateUIRuntime.Gold;
        PirateUIRuntime.Outline(characterNameText, 2f);

        levelText = PirateUIRuntime.Text("Level", panel, 18, TextAnchor.MiddleCenter);
        PirateUIRuntime.Set(levelText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(18f, -55f), new Vector2(-36f, 28f), new Vector2(0f, 1f));
        levelText.color = PirateUIRuntime.Cream;

        RectTransform portrait = PirateUIRuntime.Rect("Character Portrait", panel);
        PirateUIRuntime.Set(portrait, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -5f), new Vector2(202f, 330f), new Vector2(0.5f, 0.5f));
        Image portraitBack = portrait.gameObject.AddComponent<Image>();
        portraitBack.color = new Color(0.08f, 0.018f, 0.012f, 0.82f);
        Outline portraitOutline = portrait.gameObject.AddComponent<Outline>();
        portraitOutline.effectColor = PirateUIRuntime.Gold;
        portraitOutline.effectDistance = new Vector2(3f, -3f);
        portraitImage = PirateUIRuntime.Image("Portrait", portrait, Color.white);
        PirateUIRuntime.Stretch(portraitImage.rectTransform, 10f);
        portraitImage.preserveAspect = true;

        for (int i = 0; i < EquipmentSlotCount; i++)
        {
            bool left = i < 3;
            int row = i % 3;
            BuildEquipmentSlot(panel, i, new Vector2(left ? 14f : 316f, -110f - row * 139f));
        }

        Text hint = PirateUIRuntime.Text("Hint", panel, 14, TextAnchor.MiddleCenter);
        PirateUIRuntime.Set(hint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(18f, 14f), new Vector2(-36f, 28f), new Vector2(0f, 0f));
        hint.text = "Bấm trang bị để tháo";
        hint.color = PirateUIRuntime.Muted;
    }

    private void BuildEquipmentSlot(Transform parent, int index, Vector2 position)
    {
        Button button = PirateUIRuntime.Slot("Equipment " + EquipmentNames[index], parent, position, new Vector2(100f, 116f));
        int captured = index;
        button.onClick.AddListener(() => controller?.RequestUnequipItem((EquipmentSlot)captured));

        Text label = PirateUIRuntime.Text("Slot Label", button.transform, 12, TextAnchor.MiddleCenter);
        PirateUIRuntime.Set(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(3f, -3f), new Vector2(-6f, 22f), new Vector2(0f, 1f));
        label.text = EquipmentNames[index].ToUpperInvariant();
        label.color = PirateUIRuntime.Gold;

        Image icon = PirateUIRuntime.Image("Icon", button.transform, Color.white);
        PirateUIRuntime.Set(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -4f), new Vector2(68f, 68f), new Vector2(0.5f, 0.5f));
        icon.preserveAspect = true;
        equipmentIcons[index] = icon;

        Text item = PirateUIRuntime.Text("Item Name", button.transform, 11, TextAnchor.MiddleCenter);
        PirateUIRuntime.Set(item.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(3f, 3f), new Vector2(-6f, 22f), new Vector2(0f, 0f));
        equipmentNames[index] = item;
    }

    private void BuildDetailsSide(Transform root)
    {
        RectTransform panel = PirateUIRuntime.Panel("Character Details", root, new Vector2(535f, -142f), new Vector2(833f, 620f));
        potentialTab = BuildTab(panel, "TIỀM NĂNG", 0f);
        statsTab = BuildTab(panel, "CHỈ SỐ", 0.5f);
        potentialTab.onClick.AddListener(() => ShowTab(true));
        statsTab.onClick.AddListener(() => ShowTab(false));

        potentialContent = PirateUIRuntime.Rect("Potential Content", panel).gameObject;
        PirateUIRuntime.Set(potentialContent.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
            new Vector2(18f, 16f), new Vector2(-36f, -92f), Vector2.zero);
        BuildPotentialContent(potentialContent.transform);

        statsContent = PirateUIRuntime.Rect("Stats Content", panel).gameObject;
        PirateUIRuntime.Set(statsContent.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
            new Vector2(18f, 16f), new Vector2(-36f, -92f), Vector2.zero);
        BuildStatsContent(statsContent.transform);
        ShowTab(true);
    }

    private Button BuildTab(Transform parent, string label, float minX)
    {
        RectTransform rect = PirateUIRuntime.Rect(label + " Tab", parent);
        rect.anchorMin = new Vector2(minX, 1f);
        rect.anchorMax = new Vector2(minX + 0.5f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 72f);
        Image image = rect.gameObject.AddComponent<Image>();
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Text text = PirateUIRuntime.Text("Label", rect, 23, TextAnchor.MiddleCenter);
        PirateUIRuntime.Stretch(text.rectTransform, 3f);
        text.text = label;
        text.color = PirateUIRuntime.Cream;
        PirateUIRuntime.Outline(text, 2f);
        return button;
    }

    private void BuildPotentialContent(Transform parent)
    {
        availablePointsText = PirateUIRuntime.Text("Available Points", parent, 20, TextAnchor.MiddleRight);
        PirateUIRuntime.Set(availablePointsText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -4f), new Vector2(0f, 32f), new Vector2(0f, 1f));
        availablePointsText.color = PirateUIRuntime.Gold;

        BuildPotentialRow(parent, PotentialStat.Strength, "SỨC MẠNH", "Tăng sát thương vật lý", 0, "STR");
        BuildPotentialRow(parent, PotentialStat.Vitality, "SINH LỰC", "Tăng HP tối đa", 1, "VIT");
        BuildPotentialRow(parent, PotentialStat.Agility, "NHANH NHẸN", "Tăng tỉ lệ chí mạng", 2, "AGI");
        BuildPotentialRow(parent, PotentialStat.Energy, "NĂNG LƯỢNG", "Tăng MP tối đa", 3, "ENE");
    }

    private void BuildPotentialRow(Transform parent, PotentialStat stat, string label, string effect, int index, string symbol)
    {
        RectTransform row = PirateUIRuntime.ParchmentCard(label, parent, new Vector2(0f, -51f - index * 105f), new Vector2(797f, 89f));
        row.GetComponent<Image>().color = index % 2 == 0 ? new Color(0.91f, 0.76f, 0.5f, 0.99f) : new Color(0.84f, 0.66f, 0.4f, 0.99f);
        Text icon = PirateUIRuntime.Text("Symbol", row, 19, TextAnchor.MiddleCenter);
        PirateUIRuntime.Set(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(13f, 0f), new Vector2(65f, 65f), new Vector2(0f, 0.5f));
        icon.text = symbol;
        icon.color = PirateUIRuntime.DarkBrown;

        Text name = PirateUIRuntime.Text("Name", row, 20, TextAnchor.MiddleLeft);
        PirateUIRuntime.Set(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(90f, -11f), new Vector2(300f, 31f), new Vector2(0f, 1f));
        name.text = label;
        name.color = PirateUIRuntime.Ink;
        Text desc = PirateUIRuntime.Text("Effect", row, 14, TextAnchor.MiddleLeft);
        PirateUIRuntime.Set(desc.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(90f, 10f), new Vector2(320f, 27f), new Vector2(0f, 0f));
        desc.text = effect;
        desc.color = new Color(0.36f, 0.2f, 0.09f, 1f);

        potentialValues[index] = PirateUIRuntime.Text("Value", row, 28, TextAnchor.MiddleCenter);
        PirateUIRuntime.Set(potentialValues[index].rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-132f, 0f), new Vector2(72f, 56f), new Vector2(0.5f, 0.5f));
        potentialValues[index].color = PirateUIRuntime.Ink;

        Button plus = PirateUIRuntime.Button("Add " + label, row, "+", new Vector2(-18f, 0f), new Vector2(58f, 58f), 27);
        RectTransform plusRect = plus.GetComponent<RectTransform>();
        plusRect.anchorMin = plusRect.anchorMax = new Vector2(1f, 0.5f);
        plusRect.pivot = new Vector2(1f, 0.5f);
        plus.onClick.AddListener(() => RequestPotentialPoint(stat));
        potentialButtons[index] = plus;
    }

    private void BuildStatsContent(Transform parent)
    {
        Text heading = PirateUIRuntime.Text("Heading", parent, 22, TextAnchor.MiddleLeft);
        PirateUIRuntime.Set(heading.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(3f, -3f), new Vector2(-6f, 34f), new Vector2(0f, 1f));
        heading.text = "THÔNG SỐ CHIẾN ĐẤU";
        heading.color = PirateUIRuntime.Gold;

        healthText = BuildStatCard(parent, "SINH LỰC", "HP", new Vector2(0f, -51f), new Color(0.75f, 0.12f, 0.08f, 1f));
        manaText = BuildStatCard(parent, "NĂNG LƯỢNG", "MP", new Vector2(405f, -51f), new Color(0.08f, 0.35f, 0.7f, 1f));
        damageText = BuildStatCard(parent, "SÁT THƯƠNG", "ATK", new Vector2(0f, -160f), new Color(0.55f, 0.17f, 0.06f, 1f));
        criticalText = BuildStatCard(parent, "CHÍ MẠNG", "CRIT", new Vector2(405f, -160f), new Color(0.7f, 0.42f, 0.05f, 1f));
        speedText = BuildStatCard(parent, "TỐC ĐỘ", "SPD", new Vector2(0f, -269f), new Color(0.08f, 0.48f, 0.32f, 1f));

        RectTransform exp = PirateUIRuntime.ParchmentCard("Experience", parent, new Vector2(0f, -381f), new Vector2(797f, 92f));
        Text label = PirateUIRuntime.Text("Label", exp, 18, TextAnchor.MiddleLeft);
        PirateUIRuntime.Set(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(18f, -8f), new Vector2(-36f, 27f), new Vector2(0f, 1f));
        label.text = "KINH NGHIỆM";
        label.color = PirateUIRuntime.Ink;
        RectTransform bar = PirateUIRuntime.Rect("EXP Bar", exp);
        PirateUIRuntime.Set(bar, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(18f, 15f), new Vector2(-36f, 28f), new Vector2(0f, 0f));
        bar.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.05f, 0.02f, 1f);
        experienceFill = PirateUIRuntime.Image("EXP Fill", bar, PirateUIRuntime.Gold);
        PirateUIRuntime.Stretch(experienceFill.rectTransform, 3f);
        experienceFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        experienceText = PirateUIRuntime.Text("EXP Value", bar, 14, TextAnchor.MiddleCenter);
        PirateUIRuntime.Stretch(experienceText.rectTransform, 0f);
        experienceText.color = Color.white;
        PirateUIRuntime.Outline(experienceText, 1f);
    }

    private Text BuildStatCard(Transform parent, string label, string shortName, Vector2 position, Color accent)
    {
        RectTransform card = PirateUIRuntime.ParchmentCard(label, parent, position, new Vector2(392f, 94f));
        Image stripe = PirateUIRuntime.Image("Accent", card, accent);
        PirateUIRuntime.Set(stripe.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(9f, 0f), new Vector2(0f, 0.5f));
        Text name = PirateUIRuntime.Text("Name", card, 17, TextAnchor.MiddleLeft);
        PirateUIRuntime.Set(name.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(24f, 12f), new Vector2(-48f, 31f), new Vector2(0f, 0f));
        name.text = label;
        name.color = PirateUIRuntime.Ink;
        Text shortText = PirateUIRuntime.Text("Short", card, 13, TextAnchor.MiddleLeft);
        PirateUIRuntime.Set(shortText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(24f, -9f), new Vector2(-48f, 24f), new Vector2(0f, 1f));
        shortText.text = shortName;
        shortText.color = accent;
        Text value = PirateUIRuntime.Text("Value", card, 22, TextAnchor.MiddleRight);
        PirateUIRuntime.Stretch(value.rectTransform, 17f);
        value.color = PirateUIRuntime.Ink;
        return value;
    }

    private void ShowTab(bool potential)
    {
        potentialContent?.SetActive(potential);
        statsContent?.SetActive(!potential);
        if (potentialTab != null) potentialTab.image.color = potential ? new Color(0.58f, 0.14f, 0.045f, 1f) : PirateUIRuntime.DarkBrown;
        if (statsTab != null) statsTab.image.color = potential ? PirateUIRuntime.DarkBrown : new Color(0.58f, 0.14f, 0.045f, 1f);
    }

    private void RequestPotentialPoint(PotentialStat stat)
    {
        if (PotentialPointRequested != null)
            PotentialPointRequested.Invoke(stat);
        else
            stats?.TryAllocatePotential(stat);
    }

    private void Refresh()
    {
        if (stats == null || characterNameText == null)
            return;

        if (portraitImage != null && portraitRenderer != null)
        {
            portraitImage.sprite = portraitRenderer.sprite;
            portraitImage.enabled = portraitImage.sprite != null;
        }

        characterNameText.text = stats.CharacterName.ToUpperInvariant();
        levelText.text = $"Lv.{stats.Level}  •  KIẾM SĨ";

        for (int i = 0; i < EquipmentSlotCount; i++)
        {
            InventorySlotState slot = controller != null ? controller.GetEquipmentSlot((EquipmentSlot)i) : InventorySlotState.Empty;
            ItemDefinition definition = ItemCatalog.Get(slot.ItemId);
            bool equipped = definition != null && !slot.IsEmpty;
            equipmentIcons[i].sprite = equipped ? ItemIconLibrary.Get(slot.ItemId) : null;
            equipmentIcons[i].enabled = equipped && equipmentIcons[i].sprite != null;
            equipmentNames[i].text = equipped ? string.Empty : "—";
            equipmentNames[i].color = PirateUIRuntime.Muted;
        }

        availablePointsText.text = $"ĐIỂM CÒN LẠI: {stats.UnspentPotentialPoints}";
        potentialValues[0].text = stats.Strength.ToString();
        potentialValues[1].text = stats.Vitality.ToString();
        potentialValues[2].text = stats.Agility.ToString();
        potentialValues[3].text = stats.Energy.ToString();
        bool canAllocate = stats.UnspentPotentialPoints > 0;
        for (int i = 0; i < potentialButtons.Length; i++)
            potentialButtons[i].interactable = canAllocate;

        int damage = attackHitbox != null ? attackHitbox.CurrentDamage : stats.PhysicalDamageBonus;
        float critical = attackHitbox != null ? attackHitbox.CurrentCriticalChance * 100f : stats.CriticalChanceBonus * 100f;
        healthText.text = $"{stats.CurrentHealth:N0} / {stats.MaxHealth:N0}";
        manaText.text = $"{stats.CurrentMana:N0} / {stats.MaxMana:N0}";
        damageText.text = damage.ToString("N0");
        criticalText.text = $"{critical:0.##}%";
        speedText.text = movement != null ? movement.MoveSpeed.ToString("0.##") : "0";
        experienceText.text = $"{stats.CurrentExperience:N0} / {stats.ExperienceToNextLevel:N0}";
        float ratio = stats.ExperienceToNextLevel <= 0 ? 0f : Mathf.Clamp01((float)stats.CurrentExperience / stats.ExperienceToNextLevel);
        experienceFill.rectTransform.localScale = new Vector3(ratio, 1f, 1f);
    }

    private void OnDestroy()
    {
        if (stats != null) stats.StatsChanged -= Refresh;
        if (controller != null) controller.InventoryChanged -= Refresh;
        if (canvasObject != null) Destroy(canvasObject);
    }
}

