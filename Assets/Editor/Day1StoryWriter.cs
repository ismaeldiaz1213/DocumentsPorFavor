using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Editor-only helper (files in an "Editor" folder never ship in the game).
// Menu "Documents Por Favor > Write Day 1 Story" fills the vehicle models and the
// Day 1 travelers: documents, dialogue, X-ray contents and newspaper stories.
// Car colors you picked in the Inspector are kept.
public static class Day1StoryWriter
{
    const string DayFolder = "Assets/Data/Day1";
    public const string VehicleFolder = "Assets/Data/Vehicles";

    [MenuItem("Documents Por Favor/Write Day 1 Story")]
    static void WriteWithConfirmation()
    {
        if (EditorUtility.DisplayDialog("Write Day 1 Story",
                "This overwrites the documents, dialogue, X-ray contents and news of the Day 1 travelers " +
                "(car colors are kept). Continue?", "Write it", "Cancel"))
            WriteAll();
    }

    public static void WriteAll()
    {
        VehicleModel sedan = Vehicle("Sedan");
        sedan.baseItems = SedanParts();
        VehicleModel pickup = Vehicle("Pickup");
        pickup.baseItems = PickupParts();
        VehicleModel van = Vehicle("Van");
        van.baseItems = VanParts();
        foreach (VehicleModel model in new[] { sedan, pickup, van })
            EditorUtility.SetDirty(model);

        TravelerData lupita = Traveler("T1_Clean", new Color(0.55f, 0.66f, 0.5f));
        TravelerData chuy = Traveler("T2_BadPapers", new Color(0.72f, 0.38f, 0.3f));
        TravelerData beto = Traveler("T4_Expired", new Color(0.45f, 0.52f, 0.66f));
        TravelerData flaco = Traveler("T3_Smuggler", new Color(0.9f, 0.9f, 0.86f));

        WriteLupita(lupita, sedan);
        WriteChuy(chuy, pickup);
        WriteBeto(beto, sedan);
        WriteFlaco(flaco, van);

        DayData day = AssetDatabase.LoadAssetAtPath<DayData>($"{DayFolder}/Day1.asset");
        day.travelers = new List<TravelerData> { lupita, chuy, beto, flaco };
        EditorUtility.SetDirty(day);

        AssetDatabase.SaveAssets();
        Debug.Log("Day 1 story written: Lupita, Chuy, Beto, El Flaco.");
    }

    // ---------- Vehicles (X-ray layouts; the car faces RIGHT, x = 1 is the front) ----------

    static List<XRayItem> SedanParts() => new List<XRayItem>
    {
        //    label               shape             bright  side position     side size         top position      top size
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

    static List<XRayItem> PickupParts() => new List<XRayItem>
    {
        Item("Body",              XRayShape.Box,    0.20f,  V(0.50f, 0.38f), V(0.94f, 0.36f), V(0.50f, 0.50f), V(0.94f, 0.80f)),
        Item("Cab",               XRayShape.Box,    0.15f,  V(0.68f, 0.72f), V(0.26f, 0.30f), V(0.68f, 0.50f), V(0.26f, 0.70f)),
        Item("Bed",               XRayShape.Box,    0.12f,  V(0.28f, 0.52f), V(0.48f, 0.22f), V(0.28f, 0.50f), V(0.48f, 0.76f)),
        Item("Back-left wheel",   XRayShape.Round,  0.70f,  V(0.21f, 0.20f), V(0.13f, 0.36f), V(0.21f, 0.08f), V(0.13f, 0.14f)),
        Item("Back-right wheel",  XRayShape.Round,  0.70f,  null,            V(0f, 0f),       V(0.21f, 0.92f), V(0.13f, 0.14f)),
        Item("Front-left wheel",  XRayShape.Round,  0.70f,  V(0.80f, 0.20f), V(0.13f, 0.36f), V(0.80f, 0.08f), V(0.13f, 0.14f)),
        Item("Front-right wheel", XRayShape.Round,  0.70f,  null,            V(0f, 0f),       V(0.80f, 0.92f), V(0.13f, 0.14f)),
        Item("Engine",            XRayShape.Box,    0.65f,  V(0.90f, 0.42f), V(0.13f, 0.28f), V(0.90f, 0.50f), V(0.13f, 0.50f)),
        Item("Bench seat",        XRayShape.Box,    0.45f,  V(0.66f, 0.55f), V(0.07f, 0.32f), V(0.66f, 0.50f), V(0.07f, 0.65f)),
        Item("Spare tire",        XRayShape.Round,  0.50f,  V(0.33f, 0.22f), V(0.10f, 0.16f), V(0.33f, 0.50f), V(0.10f, 0.32f)),
        Item("Gas tank",          XRayShape.Box,    0.40f,  V(0.47f, 0.26f), V(0.10f, 0.10f), V(0.47f, 0.30f), V(0.10f, 0.25f)),
    };

    static List<XRayItem> VanParts() => new List<XRayItem>
    {
        Item("Body",              XRayShape.Box,    0.20f,  V(0.47f, 0.52f), V(0.84f, 0.80f), V(0.47f, 0.50f), V(0.84f, 0.80f)),
        Item("Cab",               XRayShape.Box,    0.16f,  V(0.92f, 0.34f), V(0.12f, 0.42f), V(0.92f, 0.50f), V(0.12f, 0.74f)),
        Item("Back-left wheel",   XRayShape.Round,  0.70f,  V(0.21f, 0.15f), V(0.12f, 0.28f), V(0.21f, 0.08f), V(0.12f, 0.14f)),
        Item("Back-right wheel",  XRayShape.Round,  0.70f,  null,            V(0f, 0f),       V(0.21f, 0.92f), V(0.12f, 0.14f)),
        Item("Front-left wheel",  XRayShape.Round,  0.70f,  V(0.77f, 0.15f), V(0.12f, 0.28f), V(0.77f, 0.08f), V(0.12f, 0.14f)),
        Item("Front-right wheel", XRayShape.Round,  0.70f,  null,            V(0f, 0f),       V(0.77f, 0.92f), V(0.12f, 0.14f)),
        Item("Engine",            XRayShape.Box,    0.65f,  V(0.93f, 0.28f), V(0.10f, 0.22f), V(0.93f, 0.50f), V(0.10f, 0.45f)),
        Item("Driver seat",       XRayShape.Box,    0.45f,  V(0.80f, 0.42f), V(0.06f, 0.26f), V(0.80f, 0.30f), V(0.06f, 0.24f)),
        Item("Passenger seat",    XRayShape.Box,    0.45f,  null,            V(0f, 0f),       V(0.80f, 0.70f), V(0.06f, 0.24f)),
        Item("Cargo shelf",       XRayShape.Box,    0.35f,  V(0.30f, 0.68f), V(0.40f, 0.04f), V(0.30f, 0.12f), V(0.40f, 0.08f)),
        Item("Spare tire",        XRayShape.Round,  0.50f,  V(0.07f, 0.30f), V(0.07f, 0.18f), V(0.07f, 0.50f), V(0.07f, 0.28f)),
    };

    // ---------- Travelers ----------

    static void WriteLupita(TravelerData t, VehicleModel car)
    {
        Documents(t, "Guadalupe Ramírez Ochoa", "MX-0451-LR", "2029-08-02", "Guadalupe Ramírez Ochoa", "GTO-4417", "GTO-4417", car);

        t.harmlessExtras = new List<XRayItem>
        {
            Item("Tamal pot", XRayShape.Round, 0.65f, V(0.38f, 0.60f), V(0.09f, 0.25f), V(0.38f, 0.62f), V(0.08f, 0.30f), icon: "pot"),
            Item("Gift box",  XRayShape.Box,   0.35f, V(0.15f, 0.50f), V(0.08f, 0.18f), V(0.15f, 0.40f), V(0.08f, 0.25f), icon: "gift"),
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
        NoBribe(t);

        // Newspaper rule: imply, never explain. No story mentions the checkpoint or the inspector.
        t.newsIfApproved = Stories(Story("Quinceañera in San Antonio Draws 300 Guests",
            "Guests praised the food. \"Two hundred tamales,\" said the DJ. \"Gone in an hour.\""));
        t.newsIfRejected = Stories(Story("San Antonio Quinceañera Ends Early",
            "The birthday girl left her own party before the cake. One chair at the family table stayed empty all night."));
        t.newsIfImprisoned = Stories(Story("Otero County Detention Facility Reaches Capacity",
            "Among the newest detainees is a 71-year-old woman. Staff say she has been teaching the guards to cook.", front: true));

        EditorUtility.SetDirty(t);
    }

    static void WriteChuy(TravelerData t, VehicleModel car)
    {
        // His cousin Neto owns the truck: the registration name doesn't match.
        Documents(t, "Jesús Valdez Mora", "MX-7781-JV", "2027-11-20", "Ernesto Valdez Mora", "CHH-2093", "CHH-2093", car);

        t.harmlessExtras = new List<XRayItem>
        {
            // Metal toolbox in the bed: glows bright, a classic false alarm.
            Item("Toolbox", XRayShape.Box, 0.75f, V(0.22f, 0.55f), V(0.12f, 0.18f), V(0.20f, 0.50f), V(0.10f, 0.35f), icon: "toolbox"),
        };
        t.contraband = new List<XRayItem>();

        t.boothLines = new List<string>
        {
            "Hey, boss! Long line today, eh? I've been practicing my handshake in the mirror for forty minutes.",
            "Job interview. Diesel mechanic. They said 'come in person.' So. Here's my person.",
            "The truck? It's my cousin Neto's. He said it's fine. Neto says a lot of things are fine. Neto set his own kitchen on fire. Twice.",
            "If I'm late I'll just tell them I was stuck at the border. They'll understand. ...Right? They'll understand?",
        };
        t.lineIfApproved = "¡Gracias, jefe! If I get the job, I'll fix your car for free. If you have a car. Do they pay you enough for a car?";
        t.lineIfRejected = "No, no, no, it's... okay. Okay. I'll call Neto. Neto will fix it.";
        t.lineIfImprisoned = "For borrowing a TRUCK?! Neto! NETO, THIS IS YOUR FAULT!";
        NoBribe(t);

        t.newsIfApproved = Stories(Story("El Paso Truck Shop Adds Night Shift",
            "\"Finally found a mechanic who shows up early,\" said the owner. \"He asked if we had coffee before he asked about the pay.\""));
        t.newsIfRejected = Stories(Story("Man Recovered from Rio Grande Near Anapra",
            "He carried no identification. In his pocket: a folded listing for a diesel mechanic job in El Paso, and the keys to a red pickup.",
            daysLater: 1, front: true));
        t.newsIfImprisoned = Stories(Story("Juárez Man Reports Truck \"Missing\" for Third Time This Year",
            "Ernesto Valdez says he lent it to family. \"They always bring it back,\" he said. \"Usually.\""));

        EditorUtility.SetDirty(t);
    }

    static void WriteBeto(TravelerData t, VehicleModel car)
    {
        // Passport expired two weeks before Day 1 (2027-08-01).
        Documents(t, "Roberto Salinas Cruz", "MX-1958-RS", "2027-07-15", "Roberto Salinas Cruz", "JAL-0771", "JAL-0771", car);

        t.harmlessExtras = new List<XRayItem>
        {
            Item("Guitarrón case", XRayShape.Round, 0.35f, V(0.36f, 0.62f), V(0.16f, 0.30f), V(0.36f, 0.50f), V(0.16f, 0.34f), icon: "guitar"),
            // A trumpet glows bright like metal contraband would. Don't click it.
            Item("Trumpet", XRayShape.Box, 0.85f, V(0.12f, 0.45f), V(0.09f, 0.10f), V(0.12f, 0.40f), V(0.09f, 0.08f), icon: "trumpet"),
        };
        t.contraband = new List<XRayItem>();

        t.boothLines = new List<string>
        {
            "¡Buenas tardes, oficial! Roberto Salinas, trumpet, Mariachi Los Desvelados. Forty-two years. You've heard of us. No? Your mother has.",
            "We have a wedding tonight in Las Cruces. The groom's family paid half up front. The other half depends on me hitting the high note in 'El Son de la Negra.'",
            "My passport? Ah, yes. Expired. Two weeks. But my face is the same, look. Older, maybe. Sadder. Same face.",
            "Do you want me to play something? It helps with the waiting. No? Okay. I'll hum.",
        };
        t.lineIfApproved = "¡Ajúa! I'll play one for you tonight, oficial. You won't hear it, but I'll play it.";
        t.lineIfRejected = "Two weeks... Okay. The bride will have to walk down the aisle to a cell phone.";
        t.lineIfImprisoned = "For an EXPIRED PASSPORT? At least let me bring the trumpet. The cell needs music.";
        NoBribe(t);

        t.newsIfApproved = Stories(Story("Las Cruces Wedding Goes Until Dawn",
            "Neighbors complained about a trumpet at 4 a.m. Nobody called the police. Some of them danced."));
        t.newsIfRejected = Stories(Story("Las Cruces Wedding Proceeds Without Band",
            "The bride walked down the aisle to music from a phone. It cut out halfway. Someone got a text."));
        t.newsIfImprisoned = Stories(Story("Detention Center Guards Request Earplugs",
            "Staff describe a detainee who hums the same song every night. \"It's beautiful,\" said one guard. \"That's the problem.\""));

        EditorUtility.SetDirty(t);
    }

    static void WriteFlaco(TravelerData t, VehicleModel car)
    {
        Documents(t, "Ramón Treviño Garza", "MX-3302-RT", "2028-02-10", "Ramón Treviño Garza", "NLE-5560", "NLE-5560", car);

        t.harmlessExtras = new List<XRayItem>
        {
            Item("Mango crate", XRayShape.Box, 0.30f, V(0.22f, 0.32f), V(0.12f, 0.26f), V(0.22f, 0.30f), V(0.12f, 0.34f), icon: "crate"),
            Item("Mango crate", XRayShape.Box, 0.30f, V(0.22f, 0.58f), V(0.12f, 0.26f), V(0.22f, 0.70f), V(0.12f, 0.34f), icon: "crate"),
            Item("Mango crate", XRayShape.Box, 0.30f, V(0.38f, 0.32f), V(0.12f, 0.26f), V(0.38f, 0.30f), V(0.12f, 0.34f), icon: "crate"),
            Item("Mango crate", XRayShape.Box, 0.30f, V(0.38f, 0.58f), V(0.12f, 0.26f), V(0.38f, 0.70f), V(0.12f, 0.34f), icon: "crate"),
        };
        t.contraband = new List<XRayItem>
        {
            // Two people behind the crates. From the side, one hides the other,
            // so the second only shows on the TOP view.
            Item("Hidden passenger", XRayShape.Box, 0.50f, V(0.56f, 0.40f), V(0.10f, 0.34f), V(0.57f, 0.32f), V(0.10f, 0.30f), icon: "person"),
            Item("Hidden passenger", XRayShape.Box, 0.50f, null,            V(0f, 0f),       V(0.57f, 0.68f), V(0.10f, 0.30f), icon: "person_b"),
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
        t.bribeAmount = 40;
        t.bribeLine = "Look... jefe. Those are... very heavy mangos. Forty dollars says they're mangos. It's all I have. I don't want trouble. I just want to drive.";

        t.newsIfApproved = Stories(Story("Abandoned Produce Van Found Outside Odessa",
            "The engine was off. The back doors were locked from the outside. Between the crates of mangos, two people were still breathing. The driver has not been found.", front: true));
        t.newsIfRejected = Stories(Story("Burned Van Found Near Ciudad Juárez",
            "\"It smelled like fruit,\" said a firefighter. No one was inside. No one has reported it missing.", daysLater: 1, front: true));
        t.newsIfImprisoned = Stories(
            Story("Two Taken Into Federal Custody",
                "Officials did not release their names. A crate of mangos was entered as evidence."),
            Story("Deportation Hearings Scheduled for Thursday",
                "Two of the cases have no attorney assigned. The court calendar lists them only by number.", daysLater: 2));

        EditorUtility.SetDirty(t);
    }

    // ---------- Helpers ----------

    static void Documents(TravelerData t, string name, string number, string expiry, string owner, string registrationPlate, string carPlate, VehicleModel car)
    {
        t.passportName = name;
        t.passportNumber = number;
        t.passportExpiry = expiry;
        t.registrationOwner = owner;
        t.registrationPlate = registrationPlate;
        t.vehiclePlate = carPlate;
        t.vehicleModel = car;
    }

    static void NoBribe(TravelerData t)
    {
        t.bribeAmount = 0;
        t.bribeLine = "";
    }

    static NewsStory Story(string headline, string body, int daysLater = 0, bool front = false) =>
        new NewsStory { headline = headline, body = body, daysLater = daysLater, frontPage = front };

    static List<NewsStory> Stories(params NewsStory[] stories) => new List<NewsStory>(stories);

    // Pass null for a position to hide the item in that view.
    // icon: name of a detailed X-ray picture made by XRayArt (e.g. "person"), or null for a plain shape.
    static XRayItem Item(string label, XRayShape shape, float brightness,
        Vector2? sidePosition, Vector2 sideSize, Vector2? topPosition, Vector2 topSize, string icon = null)
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
        if (icon != null)
        {
            item.sideSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtGenerator.Folder}/xray_{icon}_side.png");
            item.topSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtGenerator.Folder}/xray_{icon}_top.png");
        }
        return item;
    }

    static Vector2 V(float x, float y) => new Vector2(x, y);

    // Loads a traveler asset, creating it (with a starting car color) if it doesn't exist yet.
    static TravelerData Traveler(string assetName, Color colorIfNew)
    {
        string path = $"{DayFolder}/{assetName}.asset";
        TravelerData traveler = AssetDatabase.LoadAssetAtPath<TravelerData>(path);
        if (traveler == null)
        {
            traveler = ScriptableObject.CreateInstance<TravelerData>();
            traveler.vehicleColor = colorIfNew;
            AssetDatabase.CreateAsset(traveler, path);
        }
        return traveler;
    }

    public static VehicleModel Vehicle(string modelName)
    {
        if (!AssetDatabase.IsValidFolder(VehicleFolder))
            AssetDatabase.CreateFolder("Assets/Data", "Vehicles");

        string path = $"{VehicleFolder}/{modelName}.asset";
        VehicleModel model = AssetDatabase.LoadAssetAtPath<VehicleModel>(path);
        if (model == null)
        {
            model = ScriptableObject.CreateInstance<VehicleModel>();
            AssetDatabase.CreateAsset(model, path);
        }
        return model;
    }
}
