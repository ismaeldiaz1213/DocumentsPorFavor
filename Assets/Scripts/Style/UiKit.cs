using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Small helpers for building UI from code while the game runs (the intro cutscene,
// the illustrated rulebook, the verdict stamp). Same rules as the Build Desk menu:
// positions are canvas pixels (1920 x 1080) measured from the parent's bottom-left.
public static class UiKit
{
    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    public static RectTransform Stretch(string name, Transform parent)
    {
        RectTransform rect = Rect(name, parent);
        StretchFull(rect);
        return rect;
    }

    public static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void Place(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);
    }

    public static Image Panel(string name, Transform parent, float x, float y, float w, float h, Color color)
    {
        RectTransform rect = Rect(name, parent);
        Place(rect, x, y, w, h);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    // A picture that keeps its shape and its crisp pixels.
    public static Image Picture(string name, Transform parent, Sprite sprite, float x, float y, float w, float h, Color? tint = null)
    {
        Image image = Panel(name, parent, x, y, w, h, tint ?? Color.white);
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    public static TextMeshProUGUI Label(string name, Transform parent, string text, float size, Color color,
        TextAlignmentOptions alignment, float x, float y, float w, float h, bool bold = false)
    {
        RectTransform rect = Rect(name, parent);
        Place(rect, x, y, w, h);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.enableAutoSizing = true;
        label.fontSizeMin = Mathf.Min(12f, size);
        label.fontSizeMax = size;
        if (bold) label.fontStyle = FontStyles.Bold;
        return label;
    }

    public static Button Button(string name, Transform parent, float x, float y, float w, float h,
        Color color, string text, float textSize, out TextMeshProUGUI label)
    {
        Image image = Panel(name, parent, x, y, w, h, color);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
        button.colors = colors;

        label = Label("Label", image.transform, text, textSize, Palette.Paper, TextAlignmentOptions.Center, 0, 0, w, h, bold: true);
        StretchFull(label.rectTransform);
        return button;
    }

    // An empty box with a border, like a rubber stamp or a red-pen circle.
    // (Unity's Outline effect fills the whole box when the fill is see-through, so draw four bars instead.)
    public static RectTransform Frame(string name, Transform parent, float x, float y, float w, float h, float thickness, Color color)
    {
        RectTransform frame = Rect(name, parent);
        Place(frame, x, y, w, h);
        Panel("Bottom", frame, 0, 0, w, thickness, color).raycastTarget = false;
        Panel("Top", frame, 0, h - thickness, w, thickness, color).raycastTarget = false;
        Panel("Left", frame, 0, 0, thickness, h, color).raycastTarget = false;
        Panel("Right", frame, w - thickness, 0, thickness, h, color).raycastTarget = false;
        return frame;
    }

    // A keyboard key cap: [A]
    public static void Key(Transform parent, string key, float x, float y, float size = 44f)
    {
        float w = Mathf.Max(size, 18f + key.Length * size * 0.32f);
        Image shadow = Panel("Key " + key, parent, x, y - 4, w, size, Palette.PaperShadow);
        shadow.raycastTarget = false;
        Image cap = Panel("Cap", shadow.transform, 0, 4, w, size, Palette.Plate);
        cap.raycastTarget = false;
        Label("Letter", cap.transform, key, size * 0.5f, Palette.Ink, TextAlignmentOptions.Center, 0, 0, w, size, bold: true);
    }
}
