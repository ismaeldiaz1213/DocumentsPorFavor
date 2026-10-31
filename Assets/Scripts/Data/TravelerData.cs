using System.Collections.Generic;
using UnityEngine;

// One handwritten traveler (one car) at the checkpoint.
// Create one in the Project window: right-click > Create > Documents Por Favor > Traveler.
[CreateAssetMenu(fileName = "NewTraveler", menuName = "Documents Por Favor/Traveler")]
public class TravelerData : ScriptableObject
{
    [Header("Who they are")]
    [Tooltip("The face at the booth window (and their clothes when they walk).")]
    public Face face = new Face();
    [Tooltip("Who it is in the dialogue box. Leave empty to use the passport name.")]
    public string speakerName;

    [Header("Passport")]
    public string passportName;
    public string passportNumber;
    [Tooltip("USA, MEX... US citizens never need an entry permit.")]
    public string nationality = "MEX";
    [Tooltip("Format: YYYY-MM-DD, e.g. 2027-01-31")]
    public string passportExpiry;
    [Tooltip("Off: the passport photo is their own face. On: it shows photoFace instead (a borrowed passport).")]
    public bool photoIsSomeoneElse;
    public Face photoFace = new Face();

    [Header("Entry permit (non-US citizens, from the day permits start)")]
    public bool hasPermit;
    public string permitName;
    [Tooltip("WORK, VISIT, STUDY...")]
    public string permitPurpose;
    [Tooltip("Format: YYYY-MM-DD")]
    public string permitExpiry;

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

    [Header("Only shows up if...")]
    [Tooltip("Leave empty to always show up. Otherwise: only if you APPROVED this earlier traveler.")]
    public TravelerData onlyIfApproved;
    [Tooltip("Leave empty to always show up. Otherwise: only if you did NOT approve this earlier traveler (or never saw them).")]
    public TravelerData onlyIfNotApproved;

    [Header("Family (the endings remember these)")]
    [Tooltip("Your own family. Letting them in changes home.")]
    public bool isFamily;
    [Tooltip("Somewhere in this car is someone from your family.")]
    public bool carriesFamily;

    [Header("Coming back (someone you've met before)")]
    [Tooltip("If you made this decision on an earlier visit, they say these booth lines instead. First match wins.")]
    public List<ReturnVisit> returnVisits = new List<ReturnVisit>();

    [Header("Bribe (leave amount at 0 for no bribe)")]
    public int bribeAmount;
    [TextArea] public string bribeLine;

    [Header("Newspaper (what happens after your decision)")]
    public List<NewsStory> newsIfApproved = new List<NewsStory>();
    public List<NewsStory> newsIfRejected = new List<NewsStory>();
    public List<NewsStory> newsIfImprisoned = new List<NewsStory>();

    public bool HasContraband => contraband.Count > 0;
    public bool IsUSCitizen => (nationality ?? "").Trim().ToUpperInvariant() == "USA";
    public string Speaker => string.IsNullOrEmpty(speakerName) ? passportName : speakerName;
    public Face PassportPhoto => photoIsSomeoneElse ? photoFace : face;

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

// "If you approved/rejected/imprisoned me last time, I say this instead."
[System.Serializable]
public class ReturnVisit
{
    [Tooltip("The traveler asset from the earlier day (e.g. Day 1's Chuy).")]
    public TravelerData earlierVisit;
    public Decision decisionThen;
    [TextArea] public List<string> boothLines = new List<string>();
}
