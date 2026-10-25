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
    [Tooltip("Which kind of car. Its parts always show on the X-ray.")]
    public VehicleModel vehicleModel;

    [Header("What the X-ray will show")]
    [Tooltip("Harmless things only this traveler carries (luggage, a tamal pot...).")]
    public List<XRayItem> harmlessExtras = new List<XRayItem>();
    [Tooltip("Things the player must find. Any contraband means arrest.")]
    public List<XRayItem> contraband = new List<XRayItem>();

    [Header("Dialogue")]
    [Tooltip("What they say in the booth, one line per entry, in order.")]
    [TextArea] public List<string> boothLines = new List<string>();
    [TextArea] public string lineIfApproved;
    [TextArea] public string lineIfRejected;
    [TextArea] public string lineIfImprisoned;

    [Header("Bribe (leave amount at 0 for no bribe)")]
    public int bribeAmount;
    [TextArea] public string bribeLine;

    [Header("Newspaper (what happens after your decision)")]
    public List<NewsStory> newsIfApproved = new List<NewsStory>();
    public List<NewsStory> newsIfRejected = new List<NewsStory>();
    public List<NewsStory> newsIfImprisoned = new List<NewsStory>();

    public bool HasContraband => contraband.Count > 0;

    public List<NewsStory> NewsFor(Decision decision)
    {
        switch (decision)
        {
            case Decision.Approve: return newsIfApproved;
            case Decision.Reject: return newsIfRejected;
            default: return newsIfImprisoned;
        }
    }
    public bool OffersBribe => bribeAmount > 0;
}
