using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class PirateUIRuntime
{
    public static readonly Color Burgundy = new Color(0.18f, 0.035f, 0.025f, 0.96f);
    public static readonly Color DeepRed = new Color(0.28f, 0.055f, 0.025f, 1f);
    public static readonly Color Parchment = new Color(0.88f, 0.72f, 0.46f, 0.98f);
    public static readonly Color DarkBrown = new Color(0.25f, 0.105f, 0.035f, 1f);
    public static readonly Color Gold = new Color(1f, 0.72f, 0.14f, 1f);
    public static readonly Color Cream = new Color(1f, 0.92f, 0.72f, 1f);
    public static readonly Color Ink = new Color(0.17f, 0.065f, 0.025f, 1f);
    public static readonly Color Muted = new Color(0.72f, 0.61f, 0.49f, 1f);

    private static Sprite frameSprite;
    private static Sprite slotSprite;
    private static Font uiFont;

    public static GameObject CreateCanvas(string name, int sortingOrder)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        Image dimmer = Image("Dimmer", root.transform, new Color(0.025f, 0.008f, 0f, 0.74f));
        Stretch(dimmer.rectTransform, 0f);
        return root;
    }

    public static RectTransform Window(Transform parent, string name, string title, Vector2 size)
    {
        RectTransform root = Rect(name, parent);
        Set(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, size, new Vector2(0.5f, 0.5f));
        Image image = root.gameObject.AddComponent<Image>();
        image.sprite = GetFrameSprite();
        image.color = image.sprite == null ? DeepRed : Color.white;
        image.raycastTarget = false;
        Shadow shadow = root.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.82f);
        shadow.effectDistance = new Vector2(16f, -16f);

        Text heading = Text("Title", root, 38, TextAnchor.MiddleCenter);
        Set(heading.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -35f), new Vector2(520f, 64f), new Vector2(0.5f, 1f));
        heading.text = title;
        heading.color = Cream;
        Outline(heading, 3f);
        return root;
    }

    public static RectTransform Panel(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform panel = Rect(name, parent);
        Set(panel, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size, new Vector2(0f, 1f));
        Image image = panel.gameObject.AddComponent<Image>();
        image.color = Burgundy;
        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.78f, 0.49f, 0.12f, 0.92f);
        outline.effectDistance = new Vector2(2f, -2f);
        return panel;
    }

    public static RectTransform ParchmentCard(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform card = Rect(name, parent);
        Set(card, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size, new Vector2(0f, 1f));
        Image image = card.gameObject.AddComponent<Image>();
        image.color = Parchment;
        Outline outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = DarkBrown;
        outline.effectDistance = new Vector2(2f, -2f);
        Shadow shadow = card.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.08f, 0.02f, 0.005f, 0.45f);
        shadow.effectDistance = new Vector2(2f, -2f);
        Image highlight = Image("Parchment Highlight", card, new Color(1f, 0.92f, 0.7f, 0.3f));
        Set(highlight.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(3f, -3f), new Vector2(-6f, 2f), new Vector2(0f, 1f));
        return card;
    }

    public static Button Button(string name, Transform parent, string label, Vector2 position, Vector2 size, int fontSize = 20)
    {
        RectTransform rect = Rect(name, parent);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = DeepRed;
        Outline border = rect.gameObject.AddComponent<Outline>();
        border.effectColor = new Color(0.88f, 0.57f, 0.12f, 0.95f);
        border.effectDistance = new Vector2(2f, -2f);
        Shadow shadow = rect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.08f, 0.015f, 0.005f, 0.8f);
        shadow.effectDistance = new Vector2(3f, -3f);
        Image shine = Image("Top Highlight", rect, new Color(1f, 0.73f, 0.24f, 0.36f));
        Set(shine.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(3f, -3f), new Vector2(-6f, 3f), new Vector2(0f, 1f));
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.68f, 0.25f, 0.07f, 1f);
        colors.pressedColor = new Color(0.32f, 0.08f, 0.025f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        Text text = Text("Label", rect, fontSize, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform, 3f);
        text.text = label;
        text.color = Cream;
        Outline(text, 2f);
        return button;
    }

    public static Button Slot(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform rect = Rect(name, parent);
        Set(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size, new Vector2(0f, 1f));
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = GetSlotSprite();
        image.color = image.sprite == null ? new Color(0.23f, 0.09f, 0.025f, 0.97f) : Color.white;
        image.preserveAspect = false;
        if (image.sprite == null)
        {
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.9f, 0.62f, 1f);
        colors.pressedColor = new Color(1f, 0.68f, 0.25f, 1f);
        colors.selectedColor = new Color(1f, 0.82f, 0.42f, 1f);
        colors.disabledColor = new Color(0.5f, 0.42f, 0.32f, 0.8f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        return button;
    }

    public static RectTransform Rect(string name, Transform parent)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    public static Image Image(string name, Transform parent, Color color)
    {
        Image image = Rect(name, parent).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static Text Text(string name, Transform parent, int size, TextAnchor alignment)
    {
        Text text = Rect(name, parent).gameObject.AddComponent<Text>();
        if (uiFont == null)
            uiFont = Resources.Load<Font>("Fonts/BarlowCondensed-Bold");
        text.font = uiFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = FontStyle.Normal;
        text.alignment = alignment;
        text.lineSpacing = 0.9f;
        text.raycastTarget = false;
        return text;
    }

    public static void Set(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size, Vector2 pivot)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    public static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    public static void Outline(Text text, float distance)
    {
        Outline outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.12f, 0.02f, 0.005f, 0.95f);
        outline.effectDistance = new Vector2(distance, -distance);
    }

    private static Sprite GetFrameSprite()
    {
        if (frameSprite != null)
            return frameSprite;
        Texture2D texture = Resources.Load<Texture2D>("UI/PirateWindowFrame");
        if (texture != null)
            frameSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
        return frameSprite;
    }

    private static Sprite GetSlotSprite()
    {
        if (slotSprite != null)
            return slotSprite;
        Texture2D texture = Resources.Load<Texture2D>("UI/PirateItemSlot");
        if (texture != null)
            slotSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
        return slotSprite;
    }
    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;
        GameObject eventSystem = new GameObject("Event System");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }
}


