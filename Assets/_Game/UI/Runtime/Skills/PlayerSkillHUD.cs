using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(ZoroAttackInput))]
public sealed class PlayerSkillHUD : MonoBehaviour
{
    [Header("Artwork")]
    [SerializeField] private Sprite skill1Icon;
    [SerializeField] private Sprite skill2Icon;
    [SerializeField] private Sprite skill3Icon;
    [SerializeField] private Sprite skill4Icon;
    [SerializeField] private Sprite skill1Frame;
    [SerializeField] private Sprite skill2Frame;
    [SerializeField] private Sprite skill3Frame;
    [SerializeField] private Sprite skill4Frame;
    [SerializeField] private Sprite basicAttack;

    [Header("Layout")]
    [SerializeField] private Vector2 groupOffset = new Vector2(-30f, -20f);

    private const float SkillButtonSize = 148f;
    private const float BasicAttackButtonSize = 300f;

    private readonly Button[] skillButtons = new Button[4];
    private readonly Image[] skillIconImages = new Image[4];
    private ZoroAttackInput attackInput;
    private GameObject canvasObject;
    private RectTransform safeAreaRoot;
    private Image basicAttackImage;
    private Text skill4CooldownText;
    private Rect lastSafeArea;
    private bool lastAutoAttackState;
    private bool previewDirty;

    public event Action<int> SkillPressed;

    private void Awake()
    {
        attackInput = GetComponent<ZoroAttackInput>();
        if (Application.isPlaying)
            BuildHud();
    }

    private void OnEnable()
    {
        attackInput = GetComponent<ZoroAttackInput>();
        if (!Application.isPlaying)
            RebuildPreview();
    }

    private void Update()
    {
        if (!Application.isPlaying && previewDirty)
        {
            previewDirty = false;
            RebuildPreview();
            return;
        }

        if (safeAreaRoot != null && Screen.safeArea != lastSafeArea)
            ApplySafeArea();

        bool autoAttackEnabled = attackInput != null && attackInput.IsAutoAttackEnabled;
        if (autoAttackEnabled != lastAutoAttackState)
        {
            lastAutoAttackState = autoAttackEnabled;
            RefreshBasicAttackState();
        }

        RefreshSkill4Cooldown();
    }

    private void OnDestroy()
    {
        DestroyCanvas();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
            DestroyCanvas();
    }

    public void SetSkillInteractable(int skillNumber, bool interactable)
    {
        int index = skillNumber - 1;
        if (index >= 0 && index < skillButtons.Length && skillButtons[index] != null)
            skillButtons[index].interactable = interactable;
    }

    public void SetSkillIcon(int skillNumber, Sprite icon)
    {
        int index = skillNumber - 1;
        if (index < 0 || index >= skillIconImages.Length)
            return;

        switch (index)
        {
            case 0: skill1Icon = icon; break;
            case 1: skill2Icon = icon; break;
            case 2: skill3Icon = icon; break;
            case 3: skill4Icon = icon; break;
        }

        if (skillIconImages[index] != null)
            skillIconImages[index].sprite = icon;
    }

    private void BuildHud()
    {
        if (canvasObject != null)
            return;

        if (skill1Frame == null || skill2Frame == null || skill3Frame == null || skill4Frame == null ||
            basicAttack == null)
        {
            Debug.LogError("PlayerSkillHUD needs a frame for every skill slot and the basic attack sprite.", this);
            return;
        }

        canvasObject = new GameObject("Player Skill HUD Canvas", typeof(RectTransform));
        if (!Application.isPlaying)
            canvasObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        if (Application.isPlaying)
            EnsureEventSystem();

        safeAreaRoot = Rect("Skill Safe Area", canvasObject.transform);
        ApplySafeArea();

        // Positions form the same open ring used by the reference MMORPG layout.
        skillButtons[0] = CreateSkillButton("Skill 1", skill1Icon, skill1Frame, new Vector2(-75f, 500f), 1);
        skillButtons[1] = CreateSkillButton("Skill 2", skill2Icon, skill2Frame, new Vector2(-240f, 550f), 2);
        skillButtons[2] = CreateSkillButton("Skill 3", skill3Icon, skill3Frame, new Vector2(-400f, 480f), 3);
        skillButtons[3] = CreateSkillButton("Skill 4", skill4Icon, skill4Frame, new Vector2(-426f, 300f), 4);

        Button attackButton = CreateButton("Basic Attack", basicAttack,
            new Vector2(-192f, 300f), BasicAttackButtonSize);
        basicAttackImage = attackButton.targetGraphic as Image;
        attackButton.onClick.AddListener(ToggleBasicAttack);
        RefreshBasicAttackState();
    }

    [ContextMenu("Rebuild Skill HUD Preview")]
    private void RebuildPreview()
    {
        if (Application.isPlaying)
            return;

        DestroyCanvas();
        BuildHud();
    }

    private void DestroyCanvas()
    {
        if (canvasObject == null)
            return;

        if (Application.isPlaying)
            Destroy(canvasObject);
        else
            DestroyImmediate(canvasObject);

        canvasObject = null;
        safeAreaRoot = null;
        basicAttackImage = null;
        skill4CooldownText = null;
        for (int i = 0; i < skillButtons.Length; i++)
        {
            skillButtons[i] = null;
            skillIconImages[i] = null;
        }
    }

    private Button CreateSkillButton(
        string objectName,
        Sprite icon,
        Sprite frame,
        Vector2 position,
        int skillNumber)
    {
        RectTransform rect = CreateButtonRoot(objectName, position, SkillButtonSize);
        Image iconImage = CreateLayerImage("Icon", rect, icon);
        CreateLayerImage("Frame", rect, frame);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = iconImage;
        ConfigureButtonColors(button);
        skillIconImages[skillNumber - 1] = iconImage;
        button.onClick.AddListener(() => SkillPressed?.Invoke(skillNumber));

        if (skillNumber == 4)
            skill4CooldownText = CreateCooldownText(button.transform);

        return button;
    }

    private static Text CreateCooldownText(Transform parent)
    {
        RectTransform rect = Rect("Cooldown", parent);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text text = rect.gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 52;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, .95f);
        outline.effectDistance = new Vector2(3f, -3f);
        outline.useGraphicAlpha = true;
        return text;
    }

    private Button CreateButton(string objectName, Sprite sprite, Vector2 position, float size)
    {
        RectTransform rect = CreateButtonRoot(objectName, position, size);
        Image image = rect.GetComponent<Image>();
        image.color = Color.white;
        image.sprite = sprite;
        image.preserveAspect = true;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ConfigureButtonColors(button);
        return button;
    }

    private RectTransform CreateButtonRoot(string objectName, Vector2 position, float size)
    {
        RectTransform rect = Rect(objectName, safeAreaRoot);
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position + groupOffset;
        rect.sizeDelta = new Vector2(size, size);

        Image hitTarget = rect.gameObject.AddComponent<Image>();
        hitTarget.color = Color.clear;
        hitTarget.raycastTarget = true;
        return rect;
    }

    private static Image CreateLayerImage(string objectName, Transform parent, Sprite sprite)
    {
        RectTransform rect = Rect(objectName, parent);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static void ConfigureButtonColors(Button button)
    {
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, .94f, .72f, 1f);
        colors.pressedColor = new Color(.75f, .9f, 1f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(.35f, .35f, .35f, .65f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = .08f;
        button.colors = colors;
    }

    private void ToggleBasicAttack()
    {
        if (attackInput != null)
            attackInput.ToggleAutoAttack();
    }

    private void RefreshBasicAttackState()
    {
        if (basicAttackImage == null)
            return;

        basicAttackImage.color = lastAutoAttackState
            ? new Color(1f, .9f, .48f, 1f)
            : Color.white;
    }

    private void RefreshSkill4Cooldown()
    {
        if (skill4CooldownText == null || attackInput == null)
            return;

        float remaining = attackInput.IsSkill4Active
            ? attackInput.Skill4ActiveTimeRemaining
            : attackInput.Skill4CooldownRemaining;

        skill4CooldownText.text = remaining > 0f
            ? Mathf.CeilToInt(remaining).ToString()
            : string.Empty;
        skill4CooldownText.color = attackInput.IsSkill4Active
            ? new Color(1f, .4f, .45f, 1f)
            : Color.white;
    }

    private void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        float screenWidth = Mathf.Max(1f, Screen.width);
        float screenHeight = Mathf.Max(1f, Screen.height);
        safeAreaRoot.anchorMin = new Vector2(safeArea.xMin / screenWidth, safeArea.yMin / screenHeight);
        safeAreaRoot.anchorMax = new Vector2(safeArea.xMax / screenWidth, safeArea.yMax / screenHeight);
        safeAreaRoot.offsetMin = Vector2.zero;
        safeAreaRoot.offsetMax = Vector2.zero;
        lastSafeArea = safeArea;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystemObject = new GameObject("Event System");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static RectTransform Rect(string objectName, Transform parent)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.hideFlags = parent.gameObject.hideFlags;
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private void OnValidate()
    {
        previewDirty = true;
    }
}
