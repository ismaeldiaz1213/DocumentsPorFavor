using System.Collections.Generic;
using UnityEngine;

// One handwritten traveler (one car) at the checkpoint.
// Create one in the Project window: right-click > Create > Documents Por Favor > Traveler.
[CreateAssetMenu(fileName = "NewTraveler", menuName = "Documents Por Favor/Traveler")]
public class TravelerData : ScriptableObject
{
    [Header("Passport")]
    public string passportName;
    public string passportNumber;
    [Tooltip("Format: YYYY-MM-DD, e.g. 2027-01-31")]
    public string passportExpiry;

    [Header("Car registration")]
    public string registrationOwner;
    public string registrationPlate;

    [Header("The actual car at the booth")]
    [Tooltip("The plate painted on the car. Should match the registration plate.")]
    public string vehiclePlate;
    public Color vehicleColor = Color.gray;

    [Header("What the X-ray will show")]
    public List<ContrabandItem> contraband = new List<ContrabandItem>();

    [Header("Dialogue")]
    [Tooltip("What they say in the booth, one line per entry, in order.")]
    [TextArea] public List<string> boothLines = new List<string>();
    [TextArea] public string lineIfApproved;
    [TextArea] public string lineIfRejected;
    [TextArea] public string lineIfImprisoned;

    [Header("Bribe (leave amount at 0 for no bribe)")]
    public int bribeAmount;
    [TextArea] public string bribeLine;

    public bool HasContraband => contraband.Count > 0;
    public bool OffersBribe => bribeAmount > 0;
}
