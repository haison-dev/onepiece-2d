using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public enum DamagePopupType
{
    Normal,
    Critical,
    PlayerDamage
}

public sealed class DamagePopup : MonoBehaviour
{
    private const float Lifetime = 1.05f;
    private const int BaseSortingOrder = 1000;

    private static readonly Color DarkBlue = new Color32(24, 35, 78, 255);
    private static readonly Color Gold = new Color32(255, 211, 71, 255);
    private static readonly Color SoftRed = new Color32(224, 105, 79, 255);
    private static readonly Color DeepRed = new Color32(116, 36, 38, 255);
    private static readonly Color PlayerRed = new Color32(255, 87, 75, 255);
    private static readonly Color CritLabel = new Color32(255, 244, 202, 255);

    private readonly List<TextMesh> textLayers = new List<TextMesh>();
    private Vector3 startPosition;
    private float horizontalDrift;
    private float elapsed;
    private float baseScale;

    public static void Show(Vector3 worldPosition, int amount, DamagePopupType type)
    {
        if (amount <= 0)
            return;

        GameObject popupObject = new GameObject($"DamagePopup_{type}");
        popupObject.transform.position = new Vector3(
            worldPosition.x + Random.Range(-0.18f, 0.18f),
            worldPosition.y,
            -5f
        );

        popupObject.AddComponent<DamagePopup>().Initialize(amount, type);
    }

    public static Vector3 PositionAbove(Collider2D targetCollider)
    {
        Bounds bounds = targetCollider.bounds;
        return new Vector3(bounds.center.x, bounds.max.y + 0.35f, -5f);
    }

    private void Initialize(int amount, DamagePopupType type)
    {
        startPosition = transform.position;
        horizontalDrift = Random.Range(-0.2f, 0.2f);
        baseScale = type == DamagePopupType.Critical ? 1.12f : 0.94f;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        string value = amount.ToString(CultureInfo.InvariantCulture);

        switch (type)
        {
            case DamagePopupType.Critical:
                CreateOutlinedText(
                    "CriticalValue",
                    value,
                    font,
                    0.074f,
                    Gold,
                    DarkBlue,
                    0.018f,
                    Vector3.zero,
                    BaseSortingOrder + 20
                );
                CreateOutlinedText(
                    "CriticalLabel",
                    "CH\u00cd M\u1ea0NG",
                    font,
                    0.031f,
                    CritLabel,
                    SoftRed,
                    0.009f,
                    new Vector3(0f, 0.26f, 0f),
                    BaseSortingOrder + 30
                );
                break;

            case DamagePopupType.PlayerDamage:
                CreateOutlinedText(
                    "PlayerDamageValue",
                    $"-{value}",
                    font,
                    0.061f,
                    PlayerRed,
                    DeepRed,
                    0.014f,
                    Vector3.zero,
                    BaseSortingOrder + 10
                );
                break;

            default:
                CreateOutlinedText(
                    "DamageValue",
                    value,
                    font,
                    0.058f,
                    SoftRed,
                    DeepRed,
                    0.013f,
                    Vector3.zero,
                    BaseSortingOrder + 10
                );
                break;
        }

        transform.localScale = Vector3.one * (baseScale * 0.55f);
    }

    private void CreateOutlinedText(
        string objectName,
        string value,
        Font font,
        float characterSize,
        Color fillColor,
        Color outlineColor,
        float outlineWidth,
        Vector3 localPosition,
        int sortingOrder
    )
    {
        CreateTextLayer(
            $"{objectName}_Shadow",
            value,
            font,
            characterSize,
            new Color(0.05f, 0.06f, 0.12f, 0.8f),
            localPosition + new Vector3(outlineWidth * 1.35f, -outlineWidth * 1.75f, 0.02f),
            sortingOrder - 2
        );

        Vector2[] directions =
        {
            Vector2.left,
            Vector2.right,
            Vector2.up,
            Vector2.down,
            new Vector2(-0.707f, 0.707f),
            new Vector2(0.707f, 0.707f),
            new Vector2(-0.707f, -0.707f),
            new Vector2(0.707f, -0.707f)
        };

        for (int i = 0; i < directions.Length; i++)
        {
            Vector2 offset = directions[i] * outlineWidth;
            CreateTextLayer(
                $"{objectName}_Outline_{i}",
                value,
                font,
                characterSize,
                outlineColor,
                localPosition + new Vector3(offset.x, offset.y, 0.01f),
                sortingOrder - 1
            );
        }

        CreateTextLayer(
            objectName,
            value,
            font,
            characterSize,
            fillColor,
            localPosition,
            sortingOrder
        );
    }

    private void CreateTextLayer(
        string objectName,
        string value,
        Font font,
        float characterSize,
        Color color,
        Vector3 localPosition,
        int sortingOrder
    )
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(transform, false);
        textObject.transform.localPosition = localPosition;

        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = value;
        textMesh.font = font;
        textMesh.fontSize = 64;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.characterSize = characterSize;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = color;
        textMesh.richText = false;

        MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
        if (font != null)
            renderer.sharedMaterial = font.material;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = sortingOrder;

        textLayers.Add(textMesh);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / Lifetime);
        float easedRise = 1f - (1f - progress) * (1f - progress);

        transform.position = startPosition
            + Vector3.up * (0.82f * easedRise)
            + Vector3.right * (horizontalDrift * progress);

        float popScale;
        if (progress < 0.14f)
        {
            float popProgress = progress / 0.14f;
            popScale = Mathf.Lerp(0.55f, 1.18f, EaseOutCubic(popProgress));
        }
        else if (progress < 0.3f)
        {
            float settleProgress = (progress - 0.14f) / 0.16f;
            popScale = Mathf.Lerp(1.18f, 1f, EaseOutCubic(settleProgress));
        }
        else
        {
            popScale = 1f;
        }

        transform.localScale = Vector3.one * (baseScale * popScale);

        float alpha = progress < 0.62f
            ? 1f
            : 1f - Mathf.InverseLerp(0.62f, 1f, progress);
        SetAlpha(alpha);

        if (elapsed >= Lifetime)
            Destroy(gameObject);
    }

    private void SetAlpha(float alpha)
    {
        for (int i = 0; i < textLayers.Count; i++)
        {
            TextMesh textMesh = textLayers[i];
            if (textMesh == null)
                continue;

            Color color = textMesh.color;
            color.a = alpha;
            textMesh.color = color;
        }
    }

    private static float EaseOutCubic(float value)
    {
        float inverse = 1f - Mathf.Clamp01(value);
        return 1f - inverse * inverse * inverse;
    }
}