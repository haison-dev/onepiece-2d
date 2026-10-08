using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TargetBossHUD : MonoBehaviour
{
    [SerializeField] private Vector2 panelSize = new Vector2(680f, 96f);
    [SerializeField, Min(0f)] private float topMargin = 22f;
    [SerializeField] private Color frameColor = new Color(0.82f, 0.61f, 0.16f, 1f);
    [SerializeField] private Color panelColor = new Color(0.035f, 0.035f, 0.045f, 0.94f);
    [SerializeField] private Color healthColor = new Color(0.78f, 0.055f, 0.055f, 1f);

    private EnemyHealth target;
    private GameObject canvasObject;
    private RectTransform safeAreaRoot;
    private Rect lastSafeArea;
    private Text bossNameText;
    private Text healthText;
    private Image healthFill;
    private bool hudVisible = true;

    public void SetTarget(EnemyHealth newTarget)
    {
        if (target == newTarget)
        {
            Refresh();
            UpdateVisibility();
            return;
        }

        UnsubscribeFromTarget();
        target = newTarget;

        if (target == null || target.IsDead)
        {
            target = null;
            UpdateVisibility();
            return;
        }

        target.HealthChanged += HandleHealthChanged;
        target.Died += HandleTargetDied;
        BuildHud();
        Refresh();
        UpdateVisibility();
    }

    public void ClearTarget()
    {
        UnsubscribeFromTarget();
        target = null;
        UpdateVisibility();
    }

    public void SetHudVisible(bool visible)
    {
        hudVisible = visible;
        UpdateVisibility();
    }

    private void Update()
    {
        if (safeAreaRoot != null && Screen.safeArea != lastSafeArea)
            ApplySafeArea();

        if (target == null && canvasObject != null && canvasObject.activeSelf)
            UpdateVisibility();
    }

    private void BuildHud()
    {
        if (canvasObject != null)
            return;

        canvasObject = new GameObject("Target Boss HUD Canvas", typeof(RectTransform));
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        safeAreaRoot = CreateRect("Safe Area", canvasObject.transform);
        ApplySafeArea();

        RectTransform panel = CreateRect("Boss Info", safeAreaRoot);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(0f, -topMargin);
        panel.sizeDelta = panelSize;

        Image frame = panel.gameObject.AddComponent<Image>();
        frame.color = frameColor;
        frame.raycastTarget = false;

        Image background = CreateImage("Background", panel, panelColor);
        Stretch(background.rectTransform, 3f);

        bossNameText = CreateLabel("Boss Name", panel, 26, TextAnchor.MiddleCenter);
        bossNameText.rectTransform.anchorMin = new Vector2(0f, 1f);
        bossNameText.rectTransform.anchorMax = new Vector2(1f, 1f);
        bossNameText.rectTransform.pivot = new Vector2(0.5f, 1f);
        bossNameText.rectTransform.anchoredPosition = new Vector2(0f, -7f);
        bossNameText.rectTransform.sizeDelta = new Vector2(-24f, 34f);
        bossNameText.color = new Color(1f, 0.84f, 0.38f, 1f);
        AddOutline(bossNameText, Color.black, 2f);

        RectTransform bar = CreateRect("Health Bar", panel);
        bar.anchorMin = new Vector2(0f, 0f);
        bar.anchorMax = new Vector2(1f, 0f);
        bar.pivot = new Vector2(0.5f, 0f);
        bar.anchoredPosition = new Vector2(0f, 12f);
        bar.sizeDelta = new Vector2(-28f, 34f);

        Image barBackground = bar.gameObject.AddComponent<Image>();
        barBackground.color = new Color(0.12f, 0.015f, 0.02f, 1f);
        barBackground.raycastTarget = false;

        healthFill = CreateImage("Health Fill", bar, healthColor);
        Stretch(healthFill.rectTransform, 3f);
        healthFill.rectTransform.pivot = new Vector2(0f, 0.5f);

        healthText = CreateLabel("Health Value", bar, 20, TextAnchor.MiddleCenter);
        Stretch(healthText.rectTransform, 0f);
        healthText.color = Color.white;
        AddOutline(healthText, Color.black, 1.5f);
    }

    private void Refresh()
    {
        if (target == null || bossNameText == null || healthText == null || healthFill == null)
            return;

        bossNameText.text = target.DisplayName;
        healthText.text = $"{target.CurrentHealth:N0} / {target.MaxHealth:N0}";
        float healthRatio = target.MaxHealth <= 0
            ? 0f
            : Mathf.Clamp01((float)target.CurrentHealth / target.MaxHealth);
        healthFill.rectTransform.localScale = new Vector3(healthRatio, 1f, 1f);
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth <= 0)
        {
            ClearTarget();
            return;
        }

        Refresh();
    }

    private void HandleTargetDied()
    {
        ClearTarget();
    }

    private void UpdateVisibility()
    {
        if (canvasObject != null)
            canvasObject.SetActive(hudVisible && target != null && !target.IsDead);
    }

    private void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        float width = Mathf.Max(1f, Screen.width);
        float height = Mathf.Max(1f, Screen.height);
        safeAreaRoot.anchorMin = new Vector2(safeArea.xMin / width, safeArea.yMin / height);
        safeAreaRoot.anchorMax = new Vector2(safeArea.xMax / width, safeArea.yMax / height);
        safeAreaRoot.offsetMin = Vector2.zero;
        safeAreaRoot.offsetMax = Vector2.zero;
        lastSafeArea = safeArea;
    }

    private void UnsubscribeFromTarget()
    {
        if (target == null)
            return;

        target.HealthChanged -= HandleHealthChanged;
        target.Died -= HandleTargetDied;
    }

    private void OnDestroy()
    {
        UnsubscribeFromTarget();
        if (canvasObject != null)
            Destroy(canvasObject);
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        Image image = CreateRect(objectName, parent).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateLabel(
        string objectName,
        Transform parent,
        int fontSize,
        TextAnchor alignment)
    {
        Text text = CreateRect(objectName, parent).gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static void AddOutline(Text text, Color color, float distance)
    {
        Outline outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, -distance);
        outline.useGraphicAlpha = true;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private void OnValidate()
    {
        panelSize.x = Mathf.Max(320f, panelSize.x);
        panelSize.y = Mathf.Max(72f, panelSize.y);
        topMargin = Mathf.Max(0f, topMargin);
    }
}
