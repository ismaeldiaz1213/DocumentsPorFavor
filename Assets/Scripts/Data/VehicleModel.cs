using System.Collections.Generic;
using UnityEngine;

// A kind of car (sedan, pickup, van...): how it looks at the checkpoint,
// and which parts show on every X-ray of that model, so contraband has
// something normal to hide among.
[CreateAssetMenu(fileName = "NewVehicleModel", menuName = "Documents Por Favor/Vehicle Model")]
public class VehicleModel : ScriptableObject
{
    [Header("Looks")]
    [Tooltip("Grayscale body, tinted with each traveler's car color.")]
    public Sprite bodySprite;
    [Tooltip("Glass, wheels and lights. Drawn on top, never tinted.")]
    public Sprite detailSprite;

    [Header("X-ray")]
    [Tooltip("Detailed X-ray of the empty vehicle, seen from the side.")]
    public Sprite xraySide;
    [Tooltip("Detailed X-ray of the empty vehicle, seen from above.")]
    public Sprite xrayTop;
    [Tooltip("Simple shapes used only if there are no X-ray sprites above.")]
    public List<XRayItem> baseItems = new List<XRayItem>();
}
