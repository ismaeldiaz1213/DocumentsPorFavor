using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum XRayAngle { Side, Top }

// One X-ray screen (side OR top). Draws the items as glowing shapes, reveals them
// left to right as the scan progresses, and reports where the player clicked.
// Goes on a UI Image (the dark screen background), together with a Rect Mask 2D.
[RequireComponent(typeof(RectMask2D))]
public class XRayView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] XRayAngle angle = XRayAngle.Side;
    [SerializeField] Color glowColor = new Color(0.85f, 0.95f, 1f);
    [SerializeField] Color foundColor = new Color(1f, 0.25f, 0.2f);
    [SerializeField] Color missColor = new Color(1f, 0.8f, 0.2f);
    [Tooltip("Opacity of a fully bright item. Lower = more see-through, so overlapping things stand out.")]
    [SerializeField, Range(0f, 1f)] float maxOpacity = 0.6f;
    [Tooltip("Extra clickable margin around each item, as a fraction of the view.")]
    [SerializeField] float clickMargin = 0.015f;

    RectTransform rectTransform;
    RectMask2D mask;
    RectTransform content;
    RectTransform scanLine;
    float revealed;

    // Fired with the click position in 0..1 view coordinates.
    public event Action<XRayView, Vector2> Clicked;

    void Awake()
    {
        rectTransform = (RectTransform)transform;
        mask = GetComponent<RectMask2D>();

        content = CreateRect("Content", transform);
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;

        Image line = CreateImage("ScanLine", transform, Vector2.zero, Vector2.zero, null, glowColor);
        scanLine = line.rectTransform;
        scanLine.pivot = new Vector2(1f, 0.5f);
        scanLine.sizeDelta = new Vector2(4f, 0f);
        scanLine.gameObject.SetActive(false);

        SetReveal(0f);
    }

    public void Show(IEnumerable<XRayItem> items)
    {
        Clear();
        foreach (XRayItem item in items)
        {
            if (!TryGetRect(item, out Vector2 position, out Vector2 size)) continue;
            Color color = glowColor;
            color.a = item.brightness * maxOpacity;
            CreateImage(item.label, content, position, size, SpriteFor(item.shape), color);
        }
    }

    public void Clear()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    // 0 = nothing visible, 1 = whole X-ray visible.
    public void SetReveal(float progress)
    {
        revealed = progress;
        float hiddenWidth = (1f - progress) * rectTransform.rect.width;
        mask.padding = new Vector4(0f, 0f, hiddenWidth, 0f);

        bool scanning = progress > 0f && progress < 1f;
        scanLine.gameObject.SetActive(scanning);
        if (scanning)
        {
            scanLine.anchorMin = new Vector2(progress, 0f);
            scanLine.anchorMax = new Vector2(progress, 1f);
            scanLine.anchoredPosition = Vector2.zero;
        }
    }

    public void MarkFound(XRayItem item)
    {
        if (TryGetRect(item, out Vector2 position, out Vector2 size))
            CreateImage("Found: " + item.label, content, position, size * 1.35f, XRaySprites.Ring, foundColor);
    }

    public void ShowMiss(Vector2 point) => StartCoroutine(MissMarker(point));

    public bool Contains(XRayItem item, Vector2 point)
    {
        if (!TryGetRect(item, out Vector2 position, out Vector2 size)) return false;
        Vector2 half = size / 2f + Vector2.one * clickMargin;
        return Mathf.Abs(point.x - position.x) <= half.x && Mathf.Abs(point.y - position.y) <= half.y;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 local))
            return;

        Rect r = rectTransform.rect;
        var point = new Vector2((local.x - r.x) / r.width, (local.y - r.y) / r.height);

        // You can't click on what the scan hasn't shown yet.
        if (point.x > revealed) return;

        Clicked?.Invoke(this, point);
    }

    IEnumerator MissMarker(Vector2 point)
    {
        Image marker = CreateImage("Miss", content, point, Vector2.zero, XRaySprites.Ring, missColor);
        marker.rectTransform.sizeDelta = new Vector2(24f, 24f);

        const float duration = 0.8f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (marker == null) yield break; // cleared because the car left
            Color c = missColor;
            c.a = 1f - t / duration;
            marker.color = c;
            yield return null;
        }

        if (marker != null) Destroy(marker.gameObject);
    }

    bool TryGetRect(XRayItem item, out Vector2 position, out Vector2 size)
    {
        bool visible = angle == XRayAngle.Side ? item.showInSideView : item.showInTopView;
        position = angle == XRayAngle.Side ? item.sideViewPosition : item.topViewPosition;
        size = angle == XRayAngle.Side ? item.sideViewSize : item.topViewSize;
        return visible;
    }

    static Sprite SpriteFor(XRayShape shape) => shape == XRayShape.Round ? XRaySprites.Circle : null;

    static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    // Anchors do the 0..1 placement for us: the shape stretches between
    // (center - size/2) and (center + size/2) of its parent.
    static Image CreateImage(string name, Transform parent, Vector2 center, Vector2 size, Sprite sprite, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.anchorMin = center - size / 2f;
        rect.anchorMax = center + size / 2f;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false; // clicks go to the view, not to individual shapes
        return image;
    }
}
