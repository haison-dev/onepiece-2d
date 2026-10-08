using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(PlayerStats))]
public sealed class PlayerHUD : MonoBehaviour
{
    private const float ArtworkWidth = 2172f;
    private const float ArtworkHeight = 724f;

    private static readonly Rect HealthArtworkBounds = new Rect(37f, 250f, 2103f, 224f);
    private static readonly Rect ManaArtworkBounds = new Rect(53f, 268f, 2069f, 224f);
    private static readonly Rect HealthArea = new Rect(638f, 172f, 1380f, 124f);
    private static readonly Rect ManaArea = new Rect(620f, 330f, 1338f, 128f);
    private static readonly Rect LevelArea = new Rect(122f, 501f, 370f, 121f);
    private static readonly Vector2 HealthTextOffset = new Vector2(60f, 10f);
    private static readonly Vector2 ManaTextOffset = new Vector2(25f, 10f);
    private static readonly Vector2 LevelTextOffset = new Vector2(45f, 20f);

    [SerializeField] private PlayerStats stats;
    [SerializeField] private Sprite hudFrame;
    [SerializeField] private Sprite healthFillArtwork;
    [SerializeField] private Sprite manaFillArtwork;
    [SerializeField] private Vector2 hudSize = new Vector2(700f, 233.33f);
    [SerializeField] private Vector2 topLeftMargin = new Vector2(18f, 18f);

    private GameObject canvasObject;
    private RectTransform safeAreaRoot;
    private Rect lastSafeArea;
    private RectTransform healthMask;
    private RectTransform manaMask;
    private Text healthText, manaText, levelText;
    private bool previewDirty;

    private void Awake()
    {
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (Application.isPlaying)
            BuildHud();
    }

    private void OnEnable()
    {
        if (stats == null) stats = GetComponent<PlayerStats>();

        if (Application.isPlaying)
        {
            BuildHud();
            if (stats != null)
            {
                stats.StatsChanged -= Refresh;
                stats.StatsChanged += Refresh;
            }
            Refresh();
        }
        else
        {
            RebuildPreview();
        }
    }

    private void OnDisable()
    {
        if (stats != null) stats.StatsChanged -= Refresh;
        if (!Application.isPlaying)
            DestroyCanvas();
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
    }

    private void OnDestroy()
    {
        DestroyCanvas();
    }

    public void SetHudVisible(bool visible)
    {
        if (canvasObject != null)
            canvasObject.SetActive(visible);
    }

    [ContextMenu("Rebuild Player HUD Preview")]
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
        healthMask = null;
        manaMask = null;
        healthText = null;
        manaText = null;
        levelText = null;
    }

    private void BuildHud()
    {
        if (canvasObject != null)
            return;

        if (hudFrame == null || healthFillArtwork == null || manaFillArtwork == null)
        {
            Debug.LogError("PlayerHUD needs the frame, HP fill, and MP fill sprites.", this);
            return;
        }

        canvasObject = new GameObject("Player HUD Canvas", typeof(RectTransform));
        if (!Application.isPlaying)
            canvasObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        safeAreaRoot = Rect("Safe Area", canvasObject.transform);
        ApplySafeArea();

        RectTransform root = Rect("Player HUD", safeAreaRoot);
        root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(topLeftMargin.x, -topLeftMargin.y);
        root.sizeDelta = hudSize;

        healthMask = BuildArtworkFill(root, "HP", healthFillArtwork, HealthArea, HealthArtworkBounds);
        manaMask = BuildArtworkFill(root, "MP", manaFillArtwork, ManaArea, ManaArtworkBounds);

        Image frame = Image(root, "HUD Frame", hudFrame, Color.white);
        Stretch(frame.rectTransform);

        healthText = Label(root, "HP Value", 86, TextAnchor.MiddleCenter);
        SourceRect(healthText.rectTransform, HealthArea, HealthTextOffset);
        healthText.color = Color.white;
        AddOutline(healthText, new Color(0.08f, 0.025f, 0f, 1f), 2f);

        manaText = Label(root, "MP Value", 86, TextAnchor.MiddleCenter);
        SourceRect(manaText.rectTransform, ManaArea, ManaTextOffset);
        manaText.color = Color.white;
        AddOutline(manaText, new Color(0.08f, 0.025f, 0f, 1f), 2f);

        levelText = Label(root, "Level", 79, TextAnchor.MiddleCenter);
        SourceRect(levelText.rectTransform, LevelArea, LevelTextOffset);
        levelText.color = Color.white;
        AddOutline(levelText, Color.black, 2f);

        Refresh();
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

    private RectTransform BuildArtworkFill(
        RectTransform parent,
        string title,
        Sprite sprite,
        Rect targetArea,
        Rect artworkBounds)
    {
        RectTransform mask = Rect(title + " Fill Mask", parent);
        SourceRect(mask, targetArea);
        mask.gameObject.AddComponent<RectMask2D>();

        Image artwork = Image(mask, title + " Fill Artwork", sprite, Color.white);
        RectTransform artworkRect = artwork.rectTransform;
        artworkRect.anchorMin = artworkRect.anchorMax = new Vector2(0f, 1f);
        artworkRect.pivot = new Vector2(0f, 1f);

        float scaleX = targetArea.width / artworkBounds.width;
        float scaleY = targetArea.height / artworkBounds.height;
        artworkRect.anchoredPosition = new Vector2(
            -artworkBounds.x * scaleX,
            artworkBounds.y * scaleY);
        artworkRect.sizeDelta = new Vector2(ArtworkWidth * scaleX, ArtworkHeight * scaleY);
        return mask;
    }

    private void Refresh()
    {
        if (stats == null || healthMask == null || manaMask == null) return;

        int currentHealth = Application.isPlaying ? stats.CurrentHealth : stats.MaxHealth;
        int currentMana = Application.isPlaying ? stats.CurrentMana : stats.MaxMana;
        SetFill(healthMask, HealthArea.width, Ratio(currentHealth, stats.MaxHealth));
        SetFill(manaMask, ManaArea.width, Ratio(currentMana, stats.MaxMana));
        healthText.text = $"{currentHealth:N0}/{stats.MaxHealth:N0}";
        manaText.text = $"{currentMana:N0}/{stats.MaxMana:N0}";
        levelText.text = $"Lv{stats.Level}";
    }

    private static float Ratio(int value, int maximum)
    {
        return maximum <= 0 ? 0f : Mathf.Clamp01((float)value / maximum);
    }

    private void SetFill(RectTransform mask, float sourceWidth, float ratio)
    {
        Vector2 size = mask.sizeDelta;
        size.x = sourceWidth * hudSize.x / ArtworkWidth * ratio;
        mask.sizeDelta = size;
    }

    private static RectTransform Rect(string objectName, Transform parent)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.hideFlags = parent.gameObject.hideFlags;
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image Image(Transform parent, string objectName, Sprite sprite, Color color)
    {
        Image image = Rect(objectName, parent).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private Text Label(Transform parent, string objectName, int size, TextAnchor alignment)
    {
        Text text = Rect(objectName, parent).gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = Mathf.Max(1, Mathf.RoundToInt(size * hudSize.y / ArtworkHeight));
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

    private void SourceRect(RectTransform rect, Rect sourceRect, Vector2 sourceOffset = default)
    {
        float scaleX = hudSize.x / ArtworkWidth;
        float scaleY = hudSize.y / ArtworkHeight;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(
            (sourceRect.x + sourceOffset.x) * scaleX,
            -(sourceRect.y + sourceOffset.y) * scaleY);
        rect.sizeDelta = new Vector2(sourceRect.width * scaleX, sourceRect.height * scaleY);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void OnValidate()
    {
        hudSize.x = Mathf.Max(360f, hudSize.x);
        hudSize.y = Mathf.Max(120f, hudSize.y);
        topLeftMargin.x = Mathf.Max(0f, topLeftMargin.x);
        topLeftMargin.y = Mathf.Max(0f, topLeftMargin.y);
        previewDirty = true;
    }
}
