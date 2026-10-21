using UnityEngine;

// One hidden thing inside a vehicle that the player has to find on the X-ray.
// [System.Serializable] lets Unity show this inside the Inspector of a TravelerData.
[System.Serializable]
public class ContrabandItem
{
    [Tooltip("Short name, e.g. 'Hidden passenger' or 'Package'. Shown when found.")]
    public string label;

    [Header("Side X-ray")]
    public bool showInSideView = true;
    [Tooltip("Where it sits on the side X-ray. (0,0) = bottom-left corner, (1,1) = top-right corner.")]
    public Vector2 sideViewPosition = new Vector2(0.5f, 0.5f);

    [Header("Top X-ray")]
    public bool showInTopView = true;
    [Tooltip("Where it sits on the top X-ray. (0,0) = bottom-left corner, (1,1) = top-right corner.")]
    public Vector2 topViewPosition = new Vector2(0.5f, 0.5f);
}
