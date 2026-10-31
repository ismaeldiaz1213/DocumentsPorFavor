using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Day1StoryWriter;
using static Days2And3StoryWriter;

// Editor-only helper. Menu "Documents Por Favor > Write Story".
// Writes the whole 4-day story: the original cast (from Day1StoryWriter and
// Days2And3StoryWriter), plus faces, nationalities and entry permits for everyone,
// the new travelers, each day's rules and memo, and puts the days on the GameDirector.
//
//   Day 1  passports only (expiry, photo)        Lupita, Beto, El Huracán, "Héctor", El Flaco
//   Day 2  + entry permits for non-US citizens   Gary, Chuy, Rosa, Memo, Lucía
//   Day 3  + the X-ray scanner                   Beto, Manny, Chuy, Skyler
//   Day 4  + vehicle registration                Marisol, El Huracán, Chuy, the furniture truck
//
// Car colors you picked in the Inspector are kept.
public static class StoryWriter
{
    const string Day1 = "Assets/Data/Day1";
    const string Day2 = "Assets/Data/Day2";
    const string Day3 = "Assets/Data/Day3";
    const string Day4 = "Assets/Data/Day4";

    [MenuItem("Documents Por Favor/Write Story")]
    static void WriteWithConfirmation()
    {
        if (EditorUtility.DisplayDialog("Write Story",
                "This overwrites every traveler's documents, dialogue, X-ray contents and news, " +
                "and the 4 days (car colors are kept). Continue?", "Write it", "Cancel"))
            WriteAll();
    }

    public static void WriteAll()
    {
        // The original writing first, then everything below adjusts it for 4 days.
        Day1StoryWriter.WriteAll();
        Days2And3StoryWriter.WriteAll();
        Folder(Day4);

        VehicleModel sedan = Vehicle("Sedan");
        VehicleModel van = Vehicle("Van");

        TravelerData lupita = Load(Day1, "T1_Clean");
        TravelerData chuy = Load(Day1, "T2_BadPapers");   // now Day 2
        TravelerData flaco = Load(Day1, "T3_Smuggler");
        TravelerData beto = Load(Day1, "T4_Expired");
        TravelerData gary = Load(Day2, "D2_Gary");
        TravelerData marisol = Load(Day2, "D2_Marisol");  // now Day 4
        TravelerData chuy2 = Load(Day2, "D2_Chuy");       // now Day 3
        TravelerData rosa = Load(Day2, "D2_Rosa");
        TravelerData huracan = Load(Day2, "D2_Huracan");  // now Day 1
        TravelerData beto3 = Load(Day3, "D3_Beto");
        TravelerData skyler = Load(Day3, "D3_Skyler");
        TravelerData lucia = Load(Day3, "D3_Lucia");      // now Day 2
        TravelerData manny = Load(Day3, "D3_Manny");
        TravelerData chuy3 = Load(Day3, "D3_Chuy");       // now Day 4

        TravelerData hector = Traveler("D1_Hector", new Color(0.55f, 0.6f, 0.7f), Day1);
        TravelerData memo = Traveler("D2_Memo", new Color(0.58f, 0.55f, 0.5f), Day2);
        TravelerData huracan4 = Traveler("D4_Huracan", new Color(0.85f, 0.7f, 0.25f), Day4);
        TravelerData truck = Traveler("D4_Truck", new Color(0.82f, 0.8f, 0.74f), Day4);
        TravelerData truckWithoutMemo = Traveler("D4_TruckB", new Color(0.82f, 0.8f, 0.74f), Day4);

        // ----- Faces, nationalities, permits for the original cast -----
        Face chuyFace = F(3, Hair.Short, "201814", Headwear.Cap, "A8503F", shirt: "5D7A99");
        Face betoFace = F(2, Hair.Short, "3A3530", Headwear.Cowboy, "26252E", mustache: true, wrinkles: true, shirt: "3A3A44");
        Face huracanFace = F(2, Hair.Bald, "201814", Headwear.LuchaMask, "A8503F", shirt: "E8C25A");

        Person(lupita, F(2, Hair.Bun, "B9B4AA", glasses: true, lipstick: true, wrinkles: true, shirt: "D98C8C"));
        Person(beto, betoFace);
        Person(flaco, F(2, Hair.Short, "201814", Headwear.Cap, "8F897B", shirt: "E6E6E0"));
        Person(huracan, huracanFace);

        Person(gary, F(0, Hair.Short, "C9A86A", Headwear.Cowboy, "E6E1D3", wrinkles: true, shirt: "A8503F"), "USA");
        Person(chuy, chuyFace, permit: ("WORK", "2027-08-01")); // expired yesterday
        chuy.boothLines.Insert(2, "The permit? Expired yesterday. The renewal is eighty dollars. The job is so I HAVE eighty dollars. You see the problem.");
        Person(rosa, F(3, Hair.Long, "201814", shirt: "5E8C88"), permit: ("WORK", "2028-01-15"));
        Person(lucia, F(2, Hair.Long, "5A3B28", lipstick: true, shirt: "E3C07A"), permit: ("VISIT", "2028-03-01"));
        for (int i = 0; i < lucia.boothLines.Count; i++)
            lucia.boothLines[i] = lucia.boothLines[i].Replace("Three days.", "Two days.");

        Person(beto3, betoFace, permit: ("WORK · MUSICIAN", "2028-06-30"));
        Person(chuy2, chuyFace, permit: ("WORK", "2028-08-02"));
        Person(skyler, F(0, Hair.Long, "D8B86A", beard: true, shirt: "8C6BA8"), "USA");
        // Manny carries his brother Ricardo's passport: alike, but not the same man.
        Person(manny, F(3, Hair.Gray, "B9B4AA", mustache: true, wrinkles: true, shirt: "6D7E5C"), permit: ("VISIT", "2028-11-11"));
        manny.photoIsSomeoneElse = true;
        manny.photoFace = F(3, Hair.Short, "3A3530", glasses: true, shirt: "D9CFB8");

        Person(marisol, F(1, Hair.Bun, "201814", glasses: true, shirt: "6E9C93"), permit: ("WORK · NURSE", "2029-01-09"));
        Person(chuy3, chuyFace, permit: ("WORK", "2028-08-02"));

        // ----- New travelers -----
        WriteHector(hector, sedan);
        WriteMemo(memo, sedan);
        WriteHuracanDay4(huracan4, huracan, van, huracanFace);
        WriteTruck(truck, van, memo, withMemo: true);
        WriteTruck(truckWithoutMemo, van, memo, withMemo: false);

        foreach (TravelerData t in new[] { lupita, chuy, flaco, beto, gary, marisol, chuy2, rosa, huracan, beto3, skyler, lucia, manny, chuy3 })
            EditorUtility.SetDirty(t);

        // ----- The 4 days -----
        DayData day1 = Day(Day1, "Day1", 1, "2027-08-01", lupita, beto, huracan, hector, flaco);
        Rules(day1, permits: false, xray: false, registration: false, 420f, "MEMO  ·  DAY 1",
            "Welcome to Booth 3.\n\n" +
            "Today you check PASSPORTS only:\n" +
            "    ·  Not expired. Today's date is at the top of your screen.\n" +
            "    ·  The photo must be the face at your window.\n\n" +
            "All good: APPROVE. Anything wrong: REJECT.\n\n" +
            "You are paid for every correct decision. Mistakes are citations. Reach -10 and you are done here.\n\n" +
            "The shift ends at 19:00. The line does not care about your feelings.\n\n" +
            "-- El Jefe");

        DayData day2 = Day(Day2, "Day2", 2, "2027-08-02", gary, chuy, rosa, memo, lucia);
        Rules(day2, permits: true, xray: false, registration: false, 360f, "MEMO  ·  DAY 2",
            "NEW RULE: ENTRY PERMITS.\n\n" +
            "Anyone who is NOT a US citizen needs an ENTRY PERMIT:\n" +
            "    ·  The name must match the passport.\n" +
            "    ·  It must not be expired.\n\n" +
            "US citizens need no permit. They will tell you this. Loudly.\n\n" +
            "No permit, wrong name, or expired: REJECT.\n\n" +
            "-- El Jefe");

        DayData day3 = Day(Day3, "Day3", 3, "2027-08-03", beto3, manny, chuy2, skyler);
        Rules(day3, permits: true, xray: true, registration: false, 420f, "MEMO  ·  DAY 3",
            "NEW EQUIPMENT: X-RAY SCANNER.\n\n" +
            "Every car is scanned as it pulls in. Watch the monitor:\n" +
            "    ·  Click anything hidden that is not part of the car.\n" +
            "    ·  Hidden goods or hidden PEOPLE: IMPRISON.\n" +
            "    ·  Clicking on car parts wastes my time. -1 each.\n\n" +
            "The scanner cost more than your house. Do not touch it with tamal hands.\n\n" +
            "-- El Jefe");

        DayData day4 = Day(Day4, "Day4", 4, "2027-08-04", marisol, huracan4, chuy3, truck, truckWithoutMemo);
        Rules(day4, permits: true, xray: true, registration: true, 420f, "MEMO  ·  DAY 4",
            "NEW RULE: VEHICLE REGISTRATION.\n\n" +
            "    ·  The OWNER must match the passport.\n" +
            "    ·  The PLATE must match the PLATE CAM.\n\n" +
            "Also: Washington says smuggling is up this week. Washington says a lot of things. Check every truck.\n\n" +
            "-- El Jefe");

        AssetDatabase.SaveAssets();
        AddDaysToDirector(day1, day2, day3, day4);
        Debug.Log("Story written: 4 days. Save the scene (Ctrl+S) to keep the GameDirector's day list.");
    }

    // ==================== New travelers ====================

    // Day 1. A sixteen-year-old with his father's passport and a mustache drawn on with eyebrow pencil.
    static void WriteHector(TravelerData t, VehicleModel car)
    {
        Documents(t, "Héctor Lozano Peña", "MX-1971-HL", "2030-02-02", "Héctor Lozano Peña", "CHH-7710", "CHH-7710", car);
        Person(t, F(1, Hair.Curly, "3A2A20", mustache: true, shirt: "7E7E86"));
        t.photoIsSomeoneElse = true;
        t.photoFace = F(1, Hair.Gray, "B9B4AA", mustache: true, glasses: true, wrinkles: true, shirt: "EADFC4");

        t.harmlessExtras = new List<XRayItem>
        {
            Item("Backpack", XRayShape.Box, 0.35f, V(0.36f, 0.55f), V(0.08f, 0.17f), V(0.36f, 0.62f), V(0.08f, 0.17f), icon: "suitcase"),
        };
        t.contraband = new List<XRayItem>();

        t.boothLines = new List<string>
        {
            "Good afternoon, sir. I am Héctor Lozano Peña. Fifty-four years old. A grown man. With a mortgage.",
            "The mustache? Yes. It's real. I grew it this morning. ...Over many years. This morning I finished.",
            "I'm going to El Paso for business. Grown man business. Taxes. And then, after the taxes, a concert.",
            "Please don't call my wife. She's very strict. She's my mom. My WIFE. She's my wife.",
        };
        t.lineIfApproved = "YES! I mean: thank you, citizen. I will drive safely. As a grown man.";
        t.lineIfRejected = "...Can you not tell my dad? My dad is me. Can you not tell me?";
        t.lineIfImprisoned = "I'M SIXTEEN! Okay? I'm sixteen. Please call my mom. Actually, don't.";
        NoBribe(t);

        t.newsIfApproved = Stories(Story("Teen Crowd-Surfs at El Paso Concert in Father's Reading Glasses",
            "His parents learned about it from the local news. This is the local news."));
        t.newsIfRejected = Stories(Story("Juárez Father Finds Passport in Son's Backpack",
            "\"Next to it, an eyebrow pencil,\" he said. \"I don't want to know.\""));
        t.newsIfImprisoned = Stories(Story("Minor Held Overnight, Released to Parents",
            "His mother described the drive home as \"long.\""));
        EditorUtility.SetDirty(t);
    }

    // Day 2. Your older brother. No permit. The rules say no.
    static void WriteMemo(TravelerData t, VehicleModel car)
    {
        Documents(t, "Guillermo Castañeda Ríos", "MX-0919-GC", "2029-09-19", "Guillermo Castañeda Ríos", "CHH-1109", "CHH-1109", car);
        Person(t, F(2, Hair.Short, "201814", beard: true, shirt: "6B6B70"));
        t.speakerName = "Memo";
        t.hasPermit = false;
        t.isFamily = true;

        t.harmlessExtras = new List<XRayItem>
        {
            Item("Duffel bag", XRayShape.Box, 0.35f, V(0.36f, 0.55f), V(0.09f, 0.17f), V(0.36f, 0.62f), V(0.09f, 0.17f), icon: "suitcase"),
        };
        t.contraband = new List<XRayItem>();

        t.boothLines = new List<string>
        {
            "...Hermano.",
            "Don't look at me like that. I know. I know what the paper says.",
            "Eight years. They sent me back with a plastic bag and a bus ticket. I haven't seen Abuela since.",
            "José says she's sick. José says she asks for me. She thinks I'm in Houston. I let her think it.",
            "I still have her rosary. The one she gave me at the bus station. I want to put it back in her hands myself.",
            "One car. One day. Nobody has to know. Please, hermano.",
        };
        t.lineIfApproved = "...Gracias, hermano. Don't tell José. He'd cry. He cries at everything.";
        t.lineIfRejected = "Está bien. You do your job, hermano. ...I'll find another way.";
        t.lineIfImprisoned = "Hermano?! ...Okay. Okay. Tell her I'm in Houston.";
        NoBribe(t);

        t.newsIfApproved = Stories(Story("Grandmother, 79, Dances at Neighbor's Birthday",
            "\"First time in years,\" said the neighbor. \"She wouldn't say what changed.\""));
        t.newsIfRejected = Stories(Story("Price of Crossing Triples, Juárez Residents Say",
            "A trip north now costs three times what it did in the spring. Some families have started selling their furniture.",
            daysLater: 1));
        t.newsIfImprisoned = new List<NewsStory>();
        EditorUtility.SetDirty(t);
    }

    // Day 4. El Huracán returns, still registered under his ring name. Now that gets checked.
    static void WriteHuracanDay4(TravelerData t, TravelerData firstVisit, VehicleModel car, Face face)
    {
        Documents(t, "Hugo Bermúdez Paz", "MX-1998-HB", "2029-05-05", "El Huracán de Hermosillo", "SON-0001", "SON-0001", car);
        Person(t, face, permit: ("WORK · ATHLETE", "2028-12-31"));
        t.harmlessExtras = new List<XRayItem>(firstVisit.harmlessExtras);
        t.contraband = new List<XRayItem>();

        var rest = new List<string>
        {
            "Tonight, the rematch. He says I cheated. I did not cheat. I used a CHAIR. Chairs are legal in my heart.",
            "The registration? Same as always. In my TRUE name. The people's name.",
        };
        t.boothLines = Lines("¡EL HURACÁN RETURNS! Did you see the fight? I won. Of course I won. El Gringo Loco cried. Then he filed his taxes.", rest);
        t.returnVisits = new List<ReturnVisit>
        {
            Visit(firstVisit, Decision.Reject,
                Lines("The Huracán... has returned. Last time you sent me back. El Gringo Loco took my belt. Tonight I take it back.", rest)),
        };
        t.lineIfApproved = "¡EL HURACÁN AGRADECE! Tonight, the belt comes home!";
        t.lineIfRejected = "The government name AGAIN? ...The Huracán will return. The Huracán always returns.";
        t.lineIfImprisoned = "You cannot cage a HURACÁN! ...Okay, you can. The mask stays on.";
        NoBribe(t);

        t.newsIfApproved = Stories(Story("Masked Champion Wins Rematch With Steel Chair",
            "The referee ruled the chair \"technically furniture.\" El Gringo Loco has filed a complaint, and an extension."));
        t.newsIfRejected = Stories(Story("El Gringo Loco Keeps Belt After No-Show",
            "\"He's scared,\" said Loco, who then checked his phone for the twentieth time to see if anyone had arrived."));
        t.newsIfImprisoned = Stories(Story("Masked Detainee Leads Exercise Class",
            "Guards report morale is \"weirdly high.\""));
        EditorUtility.SetDirty(t);
    }

    // Day 4. A furniture truck with people hidden between the chairs. If you turned your
    // brother away, one of them holds a small rosary. The game never says who it is.
    static void WriteTruck(TravelerData t, VehicleModel car, TravelerData memo, bool withMemo)
    {
        Documents(t, "Ezequiel Fonseca Lara", "MX-1960-EF", "2029-12-01", "Ezequiel Fonseca Lara", "CHH-5150", "CHH-5150", car);
        Person(t, F(2, Hair.Gray, "B9B4AA", Headwear.Cowboy, "C9A86A", mustache: true, wrinkles: true, shirt: "8A5A3C"),
            permit: ("WORK · FREIGHT", "2028-05-31"));
        t.onlyIfApproved = withMemo ? null : memo;
        t.onlyIfNotApproved = withMemo ? memo : null;
        t.carriesFamily = withMemo;

        t.harmlessExtras = new List<XRayItem>
        {
            Item("Crate of chairs", XRayShape.Box, 0.30f, V(0.18f, 0.40f), V(0.12f, 0.40f), V(0.18f, 0.50f), V(0.12f, 0.60f), icon: "crate"),
            Item("Crate of chairs", XRayShape.Box, 0.30f, V(0.66f, 0.40f), V(0.12f, 0.40f), V(0.66f, 0.50f), V(0.12f, 0.60f), icon: "crate"),
            Item("Table", XRayShape.Box, 0.40f, V(0.42f, 0.78f), V(0.40f, 0.05f), V(0.42f, 0.15f), V(0.40f, 0.10f)),
        };
        if (withMemo)
        {
            // A tiny bright cross, held by the middle passenger. Two thin bars.
            t.harmlessExtras.Add(Item("Rosary", XRayShape.Box, 0.95f, V(0.445f, 0.50f), V(0.008f, 0.06f), V(0.445f, 0.42f), V(0.008f, 0.06f)));
            t.harmlessExtras.Add(Item("Rosary", XRayShape.Box, 0.95f, V(0.445f, 0.515f), V(0.026f, 0.009f), V(0.445f, 0.435f), V(0.026f, 0.009f)));
        }
        t.contraband = new List<XRayItem>
        {
            Item("Hidden passenger", XRayShape.Box, 0.50f, V(0.32f, 0.42f), V(0.10f, 0.34f), V(0.32f, 0.35f), V(0.10f, 0.30f), icon: "person"),
            Item("Hidden passenger", XRayShape.Box, 0.50f, V(0.43f, 0.42f), V(0.10f, 0.34f), V(0.43f, 0.38f), V(0.10f, 0.30f), icon: "person_b"),
            // Behind the others from the side: TOP view only.
            Item("Hidden passenger", XRayShape.Box, 0.50f, null,            V(0f, 0f),       V(0.54f, 0.68f), V(0.10f, 0.30f), icon: "person"),
        };

        t.boothLines = new List<string>
        {
            "Buenas. Furniture for a store in Odessa. Chairs, tables. Very heavy wood.",
            "Twenty years I drive this road. You're new. The new ones are always slow. It's okay. I'm patient.",
            "You have family, joven? Then you understand. Everybody is just trying to get home.",
        };
        t.lineIfApproved = "God bless you, joven. Drive safe. ...I mean, I'll drive safe.";
        t.lineIfRejected = "Back, then. ...They won't like going back. Nobody does.";
        t.lineIfImprisoned = withMemo
            ? "Okay. Okay. They're not mine, you understand? I just drive. ...Be gentle with the young one. He talks about his grandmother the whole way."
            : "Okay. Okay. They're not mine, you understand? I just drive.";
        t.bribeAmount = 300;
        t.bribeLine = "Three hundred. For your abuela's medicine. Everybody has an abuela, no?";

        t.newsIfApproved = Stories(Story("Box Truck Found Near Van Horn; Cooling Unit Had Failed",
            withMemo
                ? "Investigators recovered nine people from the trailer. Rescuers said one young man was holding a small wooden rosary. He would not let go of it."
                : "Investigators recovered nine people from the trailer. The driver has not been found.",
            front: true));
        t.newsIfRejected = Stories(Story("Furniture Truck Abandoned on Juárez Outskirts",
            withMemo
                ? "The cargo doors were open. Footprints led off in several directions. A rosary hung from the rearview mirror."
                : "The cargo doors were open. Footprints led off in several directions.",
            front: true));
        t.newsIfImprisoned = Stories(Story("Nine Taken Into Custody From Furniture Truck",
            withMemo
                ? "Officials said most will be returned within the week. One man asked a guard to get a message to his grandmother in El Paso. He no longer knew her address."
                : "Officials said most will be returned within the week. None of them would give a name.",
            front: true));
        EditorUtility.SetDirty(t);
    }

    // ==================== Helpers ====================

    static TravelerData Load(string folder, string assetName) =>
        AssetDatabase.LoadAssetAtPath<TravelerData>($"{folder}/{assetName}.asset");

    // A face, written compactly: skin 0-4, hair, hair color, then the extras.
    static Face F(int skin, Hair hair, string hairHex, Headwear headwear = Headwear.None, string headwearHex = "5A4636",
        bool mustache = false, bool beard = false, bool glasses = false, bool lipstick = false, bool wrinkles = false,
        string shirt = "D9CFB8") => new Face
    {
        skin = skin, hair = hair, hairColor = Hex(hairHex), headwear = headwear, headwearColor = Hex(headwearHex),
        mustache = mustache, beard = beard, glasses = glasses, lipstick = lipstick, wrinkles = wrinkles, shirt = Hex(shirt),
    };

    // Face, nationality and (optionally) an entry permit in their own name.
    static void Person(TravelerData t, Face face, string nationality = "MEX", (string purpose, string expiry)? permit = null)
    {
        t.face = face;
        t.nationality = nationality;
        t.photoIsSomeoneElse = false;
        t.photoFace = new Face();
        t.speakerName = "";
        t.hasPermit = permit.HasValue;
        t.permitName = permit.HasValue ? t.passportName : "";
        t.permitPurpose = permit.HasValue ? permit.Value.purpose : "";
        t.permitExpiry = permit.HasValue ? permit.Value.expiry : "";
        t.onlyIfApproved = null;
        t.onlyIfNotApproved = null;
        t.isFamily = false;
        t.carriesFamily = false;
    }

    static void Rules(DayData day, bool permits, bool xray, bool registration, float shiftSeconds, string memoTitle, string memo)
    {
        day.permitsRequired = permits;
        day.xrayScanner = xray;
        day.registrationRequired = registration;
        day.shiftSeconds = shiftSeconds;
        day.memoTitle = memoTitle;
        day.memo = memo;
        EditorUtility.SetDirty(day);
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        return color;
    }
}
