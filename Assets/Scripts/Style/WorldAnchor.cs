using UnityEngine;

// Keeps a UI element sitting on top of a spot in the world (the NEXT megaphone
// hangs on the booth), even when the window is resized or letterboxed.
[RequireComponent(typeof(RectTransform))]
public class WorldAnchor : MonoBehaviour
{
    [SerializeField] Transform target;
    [Tooltip("World units from the target, e.g. (0.8, 1.6) = up and to the right of it.")]
    [SerializeField] Vector3 offset;

    RectTransform rect;
    Canvas canvas;

    void Awake()
    {
        rect = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>().rootCanvas;
    }

    void LateUpdate()
    {
        if (target == null) return;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Camera worldCam = cam != null ? cam : Camera.main;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(worldCam, target.position + offset);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rect.parent, screen, cam, out Vector2 local))
            rect.localPosition = local;
    }
}
