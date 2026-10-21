using UnityEngine;

// Goes on the Main Camera. Keeps the picture at a fixed shape (16:9 by default)
// no matter the window size, and fills the leftover space with black bars.
[RequireComponent(typeof(Camera))]
public class FixedAspectCamera : MonoBehaviour
{
    [SerializeField] float aspectWidth = 16f;
    [SerializeField] float aspectHeight = 9f;

    Camera cam;
    int lastScreenWidth;
    int lastScreenHeight;

    void Awake()
    {
        cam = GetComponent<Camera>();
        CreateBarsCamera();
        Apply();
    }

    void Update()
    {
        // Only recalculate when the window actually changes size.
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            Apply();
    }

    void Apply()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        float targetAspect = aspectWidth / aspectHeight;
        float windowAspect = (float)Screen.width / Screen.height;

        if (windowAspect > targetAspect)
        {
            // Window is too wide: bars on the left and right.
            float width = targetAspect / windowAspect;
            cam.rect = new Rect((1f - width) / 2f, 0f, width, 1f);
        }
        else
        {
            // Window is too tall: bars on the top and bottom.
            float height = windowAspect / targetAspect;
            cam.rect = new Rect(0f, (1f - height) / 2f, 1f, height);
        }
    }

    // A second camera that sees nothing and just paints the whole window black.
    // It draws first, then the main camera draws the game on top in its 16:9 box.
    void CreateBarsCamera()
    {
        var barsObject = new GameObject("Letterbox Bars");
        barsObject.transform.SetParent(transform, false);

        Camera bars = barsObject.AddComponent<Camera>();
        bars.cullingMask = 0;
        bars.clearFlags = CameraClearFlags.SolidColor;
        bars.backgroundColor = Color.black;
        bars.depth = cam.depth - 1;
    }
}
