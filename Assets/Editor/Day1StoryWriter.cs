using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Editor-only helper (files in an "Editor" folder never ship in the game).
// Adds the menu item "Documents Por Favor > Write Day 1 Story", which fills the
// Day 1 traveler assets with the story from Design/Day1_Travelers.md.
public static class Day1StoryWriter
{
    const string DayFolder = "Assets/Data/Day1";
    const string VehicleFolder = "Assets/Data/Vehicles";

    [MenuItem("Documents Por Favor/Write Day 1 Story")]
    static void Write()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Write Day 1 Story",
            "This overwrites the documents, dialogue and X-ray contents of T1_Clean, T2_BadPapers and T3_Smuggler " +
            "(car colors are kept). It also creates Assets/Data/Vehicles/Sedan.\n\nContinue?",
            "Write it", "Cancel");
        if (!confirmed) return;

        if (!AssetDatabase.IsValidFolder(VehicleFolder))
            AssetDatabase.CreateFolder("Assets/Data", "Vehicles");

        VehicleModel sedan = LoadOrCreate<VehicleModel>(VehicleFolder + "/Sedan.asset");
        sedan.baseItems = SedanParts();
        EditorUtility.SetDirty(sedan);

        WriteLupita(Traveler("T1_Clean"), sedan);
        WriteChuy(Traveler("T2_BadPapers"), sedan);
        WriteFlaco(Traveler("T3_Smuggler"), sedan);

        AssetDatabase.SaveAssets();
        Debug.Log("Day 1 story written.");
    }

    // ---------- Vehicle ----------

    static List<XRayItem> SedanParts() => new List<XRayItem>
    {
        //    label               shape             bright  side position   side size       top position    top size
        Item("Body",              XRayShape.Box,    0.22f,  V(0.50f, 0.42f), V(0.94f, 0.42f), V(0.50f, 0.50f), V(0.94f, 0.80f)),
        Item("Cabin",             XRayShape.Box,    0.15f,  V(0.46f, 0.72f), V(0.48f, 0.26f), V(0.46f, 0.50f), V(0.48f, 0.70f)),
        Item("Back-left wheel",   XRayShape.Round,  0.70f,  V(0.22f, 0.20f), V(0.12f, 0.33f), V(0.22f, 0.08f), V(0.12f, 0.14f)),
        Item("Back-right wheel",  XRayShape.Round,  0.70f,  null,            V(0f, 0f),       V(0.22f, 0.92f), V(0.12f, 0.14f)),
        Item("Front-left wheel",  XRayShape.Round,  0.70f,  V(0.78f, 0.20f), V(0.12f, 0.33f), V(0.78f, 0.08f), V(0.12f, 0.14f)),
        Item("Front-right wheel", XRayShape.Round,  0.70f,  null,            V(0f, 0f),       V(0.78f, 0.92f), V(0.12f, 0.14f)),
        Item("Engine",            XRayShape.Box,    0.60f,  V(0.87f, 0.45f), V(0.14f, 0.25f), V(0.87f, 0.50f), V(0.14f, 0.50f)),
        Item("Driver seat",       XRayShape.Box,    0.45f,  V(0.56f, 0.55f), V(0.08f, 0.30f), V(0.56f, 0.30f), V(0.08f, 0.25f)),
        Item("Passenger seat",    XRayShape.Box,    0.45f,  null,            V(0f, 0f),       V(0.56f, 0.70f), V(0.08f, 0.25f)),
        Item("Back seat",         XRayShape.Box,    0.45f,  V(0.36f, 0.52f), V(0.10f, 0.26f), V(0.36f, 0.50f), V(0.10f, 0.70f)),
        Item("Spare tire",        XRayShape.Round,  0.50f,  V(0.08f, 0.42f), V(0.08f, 0.22f), V(0.08f, 0.50f), V(0.08f, 0.30f)),
        Item("Gas tank",          XRayShape.Box,    0.40f,  V(0.25f, 0.30f), V(0.12f, 0.10f), V(0.25f, 0.50f), V(0.12f, 0.40f)),
    };

    // ---------- Travelers ----------

    static void WriteLupita(TravelerData t, VehicleModel sedan)
    {
        t.passportName = "Guadalupe Ramírez Ochoa";
        t.passportNumber = "MX-0451-LR";
        t.passportExpiry = "2029-08-02";
        t.registrationOwner = "Guadalupe Ramírez Ochoa";
        t.registrationPlate = "GTO-4417";
        t.vehiclePlate = "GTO-4417";
        t.vehicleModel = sedan;

        t.harmlessExtras = new List<XRayItem>
        {
            Item("Tamal pot", XRayShape.Round, 0.65f, V(0.38f, 0.60f), V(0.09f, 0.25f), V(0.38f, 0.62f), V(0.08f, 0.30f)),
            Item("Gift box",  XRayShape.Box,   0.35f, V(0.15f, 0.50f), V(0.08f, 0.18f), V(0.15f, 0.40f), V(0.08f, 0.25f)),
        };
        t.contraband = new List<XRayItem>();

        t.boothLines = new List<string>
        {
            "Buenos días, mijo. Ay, you look so skinny. Are they feeding you in this little box?",
            "I'm going to my granddaughter's quinceañera. Fifteen years old! When I was fifteen I was already married. Different times. Worse times. But better tamales.",
            "Two hundred tamales. Pork, chicken, and rajas. The rajas are for the vegetarian boyfriend. Nobody likes him.",
            "Here, take one. No? You'll take one. I'll leave it on the counter. You'll eat it when I'm gone. I know.",
        };
        t.lineIfApproved = "Que Dios te bendiga, mijo. Eat the tamal.";
        t.lineIfRejected = "...Okay. I'll call her. She'll understand.";
        t.lineIfImprisoned = "For the TAMALES?! My grandson is a lawyer! ...He's in his first semester, but still!";
        t.bribeAmount = 0;
        t.bribeLine = "";

        EditorUtility.SetDirty(t);
    }

    static void WriteChuy(TravelerData t, VehicleModel sedan)
    {
        t.passportName = "Jesús Valdez Mora";
        t.passportNumber = "MX-7781-JV";
        t.passportExpiry = "2027-11-20";
        t.registrationOwner = "Ernesto Valdez Mora"; // his cousin Neto: the mismatch
        t.registrationPlate = "CHH-2093";
        t.vehiclePlate = "CHH-2093";
        t.vehicleModel = sedan;

        t.harmlessExtras = new List<XRayItem>
        {
            // Metal toolbox in the trunk: glows bright, a classic false alarm.
            Item("Toolbox", XRayShape.Box, 0.75f, V(0.14f, 0.45f), V(0.12f, 0.20f), V(0.14f, 0.50f), V(0.10f, 0.35f)),
        };
        t.contraband = new List<XRayItem>();

        t.boothLines = new List<string>
        {
            "Hey, boss! Long line today, eh? I've been practicing my handshake in the mirror for forty minutes.",
            "Job interview. Diesel mechanic. They said 'come in person.' So. Here's my person.",
            "The car? It's my cousin Neto's. He said it's fine. Neto says a lot of things are fine. Neto set his own kitchen on fire. Twice.",
            "If I'm late I'll just tell them I was stuck at the border. They'll understand. ...Right? They'll understand?",
        };
        t.lineIfApproved = "¡Gracias, jefe! If I get the job, I'll fix your car for free. If you have a car. Do they pay you enough for a car?";
        t.lineIfRejected = "No, no, no, it's... okay. Okay. I'll call Neto. Neto will fix it.";
        t.lineIfImprisoned = "For borrowing a CAR?! Neto! NETO, THIS IS YOUR FAULT!";
        t.bribeAmount = 0;
        t.bribeLine = "";

        EditorUtility.SetDirty(t);
    }

    static void WriteFlaco(TravelerData t, VehicleModel sedan)
    {
        t.passportName = "Ramón Treviño Garza";
        t.passportNumber = "MX-3302-RT";
        t.passportExpiry = "2028-02-10";
        t.registrationOwner = "Ramón Treviño Garza";
        t.registrationPlate = "NLE-5560";
        t.vehiclePlate = "NLE-5560";
        t.vehicleModel = sedan;

        t.harmlessExtras = new List<XRayItem>
        {
            Item("Mango crates", XRayShape.Box, 0.30f, V(0.38f, 0.55f), V(0.14f, 0.30f), V(0.38f, 0.50f), V(0.14f, 0.70f)),
        };
        t.contraband = new List<XRayItem>
        {
            // Two people curled up in the trunk. From the side, A hides B,
            // so B only shows on the TOP view.
            Item("Hidden passenger", XRayShape.Box, 0.50f, V(0.16f, 0.45f), V(0.12f, 0.22f), V(0.17f, 0.32f), V(0.12f, 0.30f)),
            Item("Hidden passenger", XRayShape.Box, 0.50f, null,            V(0f, 0f),       V(0.17f, 0.68f), V(0.12f, 0.30f)),
        };

        t.boothLines = new List<string>
        {
            "Mangos. It's mangos. Ataulfo. The good ones. Very... mango.",
            "Sorry, it's hot. Is it hot in here? It's hot. You should get a fan. I could get you a fan.",
            "No, I didn't load it. A guy loaded it. A different guy. A guy I don't know. Normal stuff.",
        };
        t.lineIfApproved = "Thank you. Thank you. Don't... don't think about it, okay? I'm trying not to.";
        t.lineIfRejected = "Back? I can't go back with them. You don't understand what they'll... okay. Okay.";
        t.lineIfImprisoned = "...Are they gonna be okay? The ones in the back. Somebody should give them water. Please.";
        t.bribeAmount = 200;
        t.bribeLine = "Look... jefe. Those are... very heavy mangos. Two hundred dollars says they're mangos. I don't want trouble. I just want to drive.";

        EditorUtility.SetDirty(t);
    }

    // ---------- Helpers ----------

    // Pass null for a position to hide the item in that view.
    static XRayItem Item(string label, XRayShape shape, float brightness,
        Vector2? sidePosition, Vector2 sideSize, Vector2? topPosition, Vector2 topSize)
    {
        var item = new XRayItem
        {
            label = label,
            shape = shape,
            brightness = brightness,
            showInSideView = sidePosition.HasValue,
            sideViewSize = sideSize,
            showInTopView = topPosition.HasValue,
            topViewSize = topSize,
        };
        if (sidePosition.HasValue) item.sideViewPosition = sidePosition.Value;
        if (topPosition.HasValue) item.topViewPosition = topPosition.Value;
        return item;
    }

    static Vector2 V(float x, float y) => new Vector2(x, y);

    static TravelerData Traveler(string assetName) =>
        AssetDatabase.LoadAssetAtPath<TravelerData>($"{DayFolder}/{assetName}.asset");

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }
}
