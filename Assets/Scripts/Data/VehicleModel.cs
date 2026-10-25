using System.Collections.Generic;
using UnityEngine;

// A kind of car (sedan, pickup, van...). Its parts show on every X-ray of that model,
// so contraband has something normal to hide among.
[CreateAssetMenu(fileName = "NewVehicleModel", menuName = "Documents Por Favor/Vehicle Model")]
public class VehicleModel : ScriptableObject
{
    [Tooltip("Harmless parts every car of this model shows on the X-ray (engine, seats, wheels...).")]
    public List<XRayItem> baseItems = new List<XRayItem>();
}
