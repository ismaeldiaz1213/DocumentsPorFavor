using UnityEngine;

public enum XRayShape { Box, Round }

// One thing that shows up on the X-ray: a car part, harmless luggage, or contraband.
// Positions and sizes go from 0 to 1 across the X-ray view: (0,0) = bottom-left, (1,1) = top-right.
// The car always faces RIGHT on the X-ray, so x = 1 is the front bumper.
[System.Serializable]
public class XRayItem
{
    [Tooltip("Short name, e.g. 'Spare tire' or 'Hidden passenger'.")]
    public string label;
    public XRayShape shape = XRayShape.Box;
    [Tooltip("How bright it glows on the X-ray. Dense things (metal, people) glow brighter.")]
    [Range(0f, 1f)] public float brightness = 0.5f;

    [Header("Side X-ray")]
    public bool showInSideView = true;
    public Vector2 sideViewPosition = new Vector2(0.5f, 0.5f);
    public Vector2 sideViewSize = new Vector2(0.1f, 0.2f);

    [Header("Top X-ray")]
    public bool showInTopView = true;
    public Vector2 topViewPosition = new Vector2(0.5f, 0.5f);
    public Vector2 topViewSize = new Vector2(0.1f, 0.2f);
}
