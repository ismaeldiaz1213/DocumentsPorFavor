using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Day1StoryWriter;

// Editor-only helper. Menu "Documents Por Favor > Write Days 2 and 3 Story" fills the
// Day 2 and Day 3 travelers and adds all three days to the GameDirector in the open scene.
// Run "Write Day 1 Story" first: Days 2 and 3 remember what you did to Day 1's travelers.
// Car colors you picked in the Inspector are kept.
public static class Days2And3StoryWriter
{
    const string Day1Folder = "Assets/Data/Day1";
    const string Day2Folder = "Assets/Data/Day2";
    const string Day3Folder = "Assets/Data/Day3";

    // Chuy and Beto drive the same cars every day.
    static readonly Color ChuyRed = new Color(0.72f, 0.38f, 0.3f);
    static readonly Color BetoBlue = new Color(0.45f, 0.52f, 0.66f);

    [MenuItem("Documents Por Favor/Write Days 2 and 3 Story")]
    static void WriteWithConfirmation()
    {
        if (EditorUtility.DisplayDialog("Write Days 2 and 3 Story",
                "This overwrites the documents, dialogue, X-ray contents and news of the Day 2 and Day 3 travelers " +
                "(car colors are kept), and sets the days on the GameDirector. Continue?", "Write it", "Cancel"))
            WriteAll();
    }

    public static void WriteAll()
    {
        VehicleModel sedan = Vehicle("Sedan");
        VehicleModel pickup = Vehicle("Pickup");
        VehicleModel van = Vehicle("Van");

        TravelerData chuyDay1 = AssetDatabase.LoadAssetAtPath<TravelerData>($"{Day1Folder}/T2_BadPapers.asset");
        TravelerData betoDay1 = AssetDatabase.LoadAssetAtPath<TravelerData>($"{Day1Folder}/T4_Expired.asset");

        // ----- Day 2: 2 approve, 2 reject, 1 imprison -----
        TravelerData gary = Traveler("D2_Gary", new Color(0.86f, 0.86f, 0.82f), Folder(Day2Folder));
        TravelerData marisol = Traveler("D2_Marisol", new Color(0.6f, 0.6f, 0.64f), Day2Folder);
        TravelerData chuy2 = Traveler("D2_Chuy", ChuyRed, Day2Folder);
        TravelerData rosa = Traveler("D2_Rosa", new Color(0.76f, 0.68f, 0.5f), Day2Folder);
        TravelerData huracan = Traveler("D2_Huracan", new Color(0.85f, 0.7f, 0.25f), Day2Folder);
        WriteGary(gary, pickup);
        WriteMarisol(marisol, sedan);
        WriteChuyDay2(chuy2, pickup, chuyDay1);
        WriteRosa(rosa, sedan);
        WriteHuracan(huracan, van);
        DayData day2 = Day(Day2Folder, "Day2", 2, "2027-08-02", gary, marisol, chuy2, rosa, huracan);

        // ----- Day 3: 1 approve, 2 reject, 2 imprison -----
        TravelerData beto3 = Traveler("D3_Beto", BetoBlue, Folder(Day3Folder));
        TravelerData skyler = Traveler("D3_Skyler", new Color(0.4f, 0.56f, 0.46f), Day3Folder);
        TravelerData lucia = Traveler("D3_Lucia", new Color(0.8f, 0.56f, 0.6f), Day3Folder);
        TravelerData manny = Traveler("D3_Manny", new Color(0.5f, 0.52f, 0.38f), Day3Folder);
        TravelerData chuy3 = Traveler("D3_Chuy", ChuyRed, Day3Folder);
        WriteBetoDay3(beto3, sedan, betoDay1);
        WriteSkyler(skyler, sedan);
        WriteLucia(lucia, sedan);
        WriteManny(manny, pickup);
        WriteChuyDay3(chuy3, pickup);
        DayData day3 = Day(Day3Folder, "Day3", 3, "2027-08-03", beto3, skyler, lucia, manny, chuy3);

        AssetDatabase.SaveAssets();
        AddDaysToDirector(AssetDatabase.LoadAssetAtPath<DayData>($"{Day1Folder}/Day1.asset"), day2, day3);
        Debug.Log("Days 2 and 3 written. Save the scene (Ctrl+S) to keep the GameDirector's day list.");
    }

    // ==================== DAY 2 ====================

    // The loud American. Clean car, clean papers: the rulebook says let him in.
    static void WriteGary(TravelerData t, VehicleModel car)
    {
        Documents(t, "Gary Dale Hutchins", "US-5521-GH", "2031-04-12", "Gary Dale Hutchins", "TX-8KD-4421", "TX-8KD-4421", car);

        t.harmlessExtras = new List<XRayItem>
        {
            Item("Cooler",        XRayShape.Box, 0.45f, V(0.14f, 0.53f), V(0.09f, 0.19f), V(0.14f, 0.30f), V(0.09f, 0.17f), icon: "cooler"),
            Item("Crate of soda", XRayShape.Box, 0.35f, V(0.25f, 0.52f), V(0.08f, 0.17f), V(0.24f, 0.30f), V(0.08f, 0.22f), icon: "crate"),
            // Golf clubs glow bright and look like weapons at a glance.
            Item("Golf bag",      XRayShape.Box, 0.75f, V(0.38f, 0.48f), V(0.15f, 0.11f), V(0.32f, 0.70f), V(0.15f, 0.11f), icon: "golf"),
        };
        t.contraband = new List<XRayItem>();

        t.boothLines = new List<string>
        {
            "Howdy. You speak English? Good. Good for you. Real proud of you.",
            "Been down in Juárez getting my teeth done. Eight crowns, three hundred bucks. Dr. Ramírez. Don't tell anybody, they'll all want to go.",
            "Now don't take this personal, but y'all need to build that wall higher. Not you. You're one of the good ones. You got a badge.",
            "Cooler's beer. Crate's Mexican Coke, the real-sugar kind. Y'all got that one thing right.",
        };
        t.lineIfApproved = "Appreciate it, amigo. See? System works when the right folks run it.";
        t.lineIfRejected = "Rejected? I'm AMERICAN. I'll be on the news by tonight. Not in a good way. For YOU.";
        t.lineIfImprisoned = "I was BORN here! ...Is this about the wall thing? That was a joke! Mostly!";
        NoBribe(t);

        t.newsIfApproved = Stories(Story("Letter to the Editor: \"Secure Our Border\"",
            "\"Anybody can waltz right in. Nobody checks a thing.\" G. Hutchins, Las Cruces. P.S. \"Dr. Ramírez in Juárez does crowns for $300. Ask for Lupe.\""));
        t.newsIfRejected = Stories(Story("Las Cruces Man's Video About \"Night in Juárez\" Goes Viral",
            "In 47 minutes he praises the tacos, the hotel staff, and a stranger who lent him a phone charger. He ends by calling for a wall."));
        t.newsIfImprisoned = Stories(Story("County Commission Candidate Suspends Campaign",
            "Gary Hutchins, who ran on border security, says he needs \"time with family.\" Reporters described his new teeth as \"very white.\""));

        EditorUtility.SetDirty(t);
    }

    // An ICU nurse with plates that don't match. Innocent, and the rulebook still says no.
    static void WriteMarisol(TravelerData t, VehicleModel car)
    {
        // Registration says ...6602, the car says ...6620. Easy to miss.
        Documents(t, "Marisol Fuentes Ibarra", "MX-6610-MF", "2030-01-09", "Marisol Fuentes Ibarra", "CHH-6602", "CHH-6620", car);

        t.harmlessExtras = new List<XRayItem>
        {
            Item("Work bag", XRayShape.Box, 0.35f, V(0.13f, 0.47f), V(0.09f, 0.19f), V(0.13f, 0.50f), V(0.09f, 0.17f), icon: "suitcase"),
        };
        t.contraband = new List<XRayItem>();

        t.boothLines = new List<string>
        {
            "Hi. Night shift, ICU, University Medical Center. Six patients tonight. Can we make this quick?",
            "I bought the car last month. Used. The guy who sold it to me had a cousin at the DMV, he said that made it easier.",
            "...That's bad, isn't it? You're making a face. You have the same face as my patients' families.",
        };
        t.lineIfApproved = "Thank you. If you ever end up in the ICU, ask for me. ...Don't end up in the ICU.";
        t.lineIfRejected = "Okay. I'll call in. They'll pull someone from the ER. The ER is already short. Okay.";
        t.lineIfImprisoned = "For a PLATE? I have a man on a ventilator waiting for me!";
        NoBribe(t);

        t.newsIfApproved = Stories(Story("Nurses Work Through the Night After Crash on I-10",
            "Eleven were brought to University Medical Center. All survived. \"We had everyone we needed,\" a nurse said. \"Barely.\""));
        t.newsIfRejected = Stories(Story("Short-Staffed ICU Diverts Ambulances Overnight",
            "One unit ran a nurse short. A spokesperson said patient outcomes are \"under review.\" Two families were asked to wait in the hallway.",
            daysLater: 1));
        t.newsIfImprisoned = Stories(Story("Juárez Car Lot Sold the Same Plates Twice, Police Say",
            "At least four buyers have come forward. One of them is still in federal custody."));

        EditorUtility.SetDirty(t);
    }

    // Chuy again. His own papers this time. Somebody has noticed he crosses every day.
    static void WriteChuyDay2(TravelerData t, VehicleModel car, TravelerData chuyDay1)
    {
        Documents(t, "Jesús Valdez Mora", "MX-7781-JV", "2027-11-20", "Jesús Valdez Mora", "CHH-2093", "CHH-2093", car);

        t.harmlessExtras = new List<XRayItem>
        {
            Item("Toolbox",              XRayShape.Box, 0.75f, V(0.22f, 0.55f), V(0.12f, 0.18f), V(0.20f, 0.50f), V(0.10f, 0.35f), icon: "toolbox"),
            // Iron discs: the brightest thing on the screen, and completely legal.
            Item("Box of brake rotors",  XRayShape.Box, 0.85f, V(0.40f, 0.50f), V(0.07f, 0.14f), V(0.40f, 0.40f), V(0.07f, 0.19f), icon: "rotors"),
        };
        t.contraband = new List<XRayItem>();

        var rest = new List<string>
        {
            "The registration's mine now, look. Neto signed it over. Cost me a carne asada and four hours of listening to Neto.",
            "The box? Brake rotors. For a guy at the shop. Very legal. Very boring.",
            "Some guys at the gas station asked if I cross every day. I said yeah. They said \"good.\" Just \"good.\" Weird, right?",
        };
        // Default: you turned him away on Day 1 (the correct call).
        t.boothLines = Lines("¡Jefe! Remember me? Interview guy. I got there at noon, the job was gone. But the owner felt bad. Trial day today. So!", rest);
        t.returnVisits = new List<ReturnVisit>
        {
            Visit(chuyDay1, Decision.Approve,
                Lines("¡Jefe! It's me! I GOT THE JOB! Diesel mechanic! Look at my hands. Real grease. Professional grease.", rest)),
            Visit(chuyDay1, Decision.Imprison,
                Lines("Jefe. It's me. They let me out this morning. Neto came with the papers. He cried more than me. A lot more.", rest)),
        };

        t.lineIfApproved = "¡Gracias, jefe! See you tomorrow. Same Chuy, same truck. Same time!";
        t.lineIfRejected = "But it's MINE now! Neto signed it! ...Okay. Okay. Tomorrow, then.";
        t.lineIfImprisoned = "Brake rotors, jefe! They're BRAKE ROTORS! Ask anyone! Ask a car!";
        NoBribe(t);

        t.newsIfApproved = Stories(Story("Commuters Report Strangers Asking About Their Routes",
            "Drivers who cross every day say men at Juárez gas stations have been asking about schedules. One offered to pay for a woman's gas. \"Nobody pays for your gas,\" she said."));
        t.newsIfRejected = Stories(Story("Diesel Shop Owner: \"Can't Find Anyone Reliable\"",
            "\"The good ones get stuck at the bridge,\" he said. \"Then somebody else gets to them first.\""));
        t.newsIfImprisoned = Stories(Story("Brake Rotors Entered Into Evidence; Charges Dropped",
            "A spokesperson confirmed the parts were \"just brake rotors.\" The truck's owner could not be reached. His cousin says he is \"thinking about some offers.\""));

        EditorUtility.SetDirty(t);
    }

    // A cleaning woman with a work permit, and her son folded up under the back seat.
    static void WriteRosa(TravelerData t, VehicleModel car)
    {
        Documents(t, "Rosa Elena Medina Paz", "MX-2214-RM", "2028-09-03", "Rosa Elena Medina Paz", "SON-1187", "SON-1187", car);

        t.harmlessExtras = new List<XRayItem>
        {
            Item("Cleaning bag", XRayShape.Box, 0.35f, V(0.13f, 0.47f), V(0.09f, 0.19f), V(0.13f, 0.50f), V(0.09f, 0.17f), icon: "suitcase"),
        };
        t.contraband = new List<XRayItem>
        {
            // Under the back seat, tangled up with the seat and the gas tank. Small. Look closely.
            Item("Hidden child", XRayShape.Box, 0.50f, V(0.39f, 0.37f), V(0.07f, 0.22f), V(0.41f, 0.62f), V(0.07f, 0.19f), icon: "child"),
        };

        t.boothLines = new List<string>
        {
            "Buenas tardes. I clean offices in El Paso. Five years, every morning, this bridge. Maybe you've seen me.",
            "My papers are good. Work permit, everything. I am very legal. Very, very legal.",
            "No radio, sorry. It's broken. Can we go? I'm very tired. It's... very quiet in the car today.",
        };
        t.lineIfApproved = "Gracias. Gracias. You'll never see him. I promise. He'll be so good.";
        t.lineIfRejected = "Okay. We go back. ...There are other ways across. Worse ones. But there are.";
        t.lineIfImprisoned = "He's EIGHT. His father is not a good man. Please don't put him with the men. He's scared of the dark. Please tell them he's scared of the dark.";
        t.bribeAmount = 25;
        t.bribeLine = "Please. Twenty-five dollars. It's what I have until Friday. He's very quiet. He knows how to be quiet.";

        t.newsIfApproved = Stories(Story("Lamar Elementary Welcomes New Third-Grader",
            "His teacher says he barely speaks yet, but he draws all day. Mostly bridges. Mostly the same one.", daysLater: 1));
        t.newsIfRejected = Stories(Story("Two Sets of Footprints Lead Into the Desert Near Sunland Park",
            "One large, one small. The search was called off at dusk. The heat index reached 112.", daysLater: 1, front: true));
        t.newsIfImprisoned = Stories(
            Story("Children's Shelter Reports Record Intake",
                "A volunteer said one boy, about eight, asked if the lights could stay on. They could not.", front: true),
            Story("Cleaning Company Reports Worker Missing",
                "\"Five years and she never missed a morning,\" said her supervisor. \"Her locker still has her son's drawings in it.\"", daysLater: 1));

        EditorUtility.SetDirty(t);
    }

    // The silly one: a luchador whose registration is in his ring name.
    static void WriteHuracan(TravelerData t, VehicleModel car)
    {
        Documents(t, "Hugo Bermúdez Paz", "MX-1998-HB", "2029-05-05", "El Huracán de Hermosillo", "SON-0001", "SON-0001", car);

        t.harmlessExtras = new List<XRayItem>
        {
            Item("Gear bag",           XRayShape.Box, 0.35f, V(0.18f, 0.33f), V(0.09f, 0.19f), V(0.18f, 0.55f), V(0.09f, 0.17f), icon: "suitcase"),
            // Bright metal. Not contraband. Unless you ask El Gringo Loco.
            Item("Championship belt",  XRayShape.Box, 0.80f, V(0.32f, 0.40f), V(0.10f, 0.11f), V(0.32f, 0.30f), V(0.10f, 0.11f), icon: "belt"),
            Item("Steel chair",        XRayShape.Box, 0.85f, V(0.50f, 0.42f), V(0.07f, 0.22f), V(0.50f, 0.65f), V(0.07f, 0.19f), icon: "chair"),
        };
        t.contraband = new List<XRayItem>();

        t.boothLines = new List<string>
        {
            "¡Buenas tardes, ciudadano! I am EL HURACÁN DE HERMOSILLO! Three-time champion of the Arena Coliseo!",
            "The mask stays on. The mask ALWAYS stays on. My own mother has not seen my face since 1998. She says thank God.",
            "The registration? In my name. My TRUE name. The one the people chant. The other name is for the government.",
            "Tonight I fight El Gringo Loco in El Paso. He's from Lubbock. Very loco. He does taxes the rest of the week.",
        };
        t.lineIfApproved = "¡EL HURACÁN AGRADECE! The ring awaits!";
        t.lineIfRejected = "The Huracán... retreats. A STRATEGIC retreat. Tell El Gringo Loco he got lucky.";
        t.lineIfImprisoned = "You cannot cage a HURACÁN! ...Okay, you can. Can I keep the mask on? It's very important to me.";
        NoBribe(t);

        t.newsIfApproved = Stories(Story("Masked Wrestler Keeps Title in El Paso",
            "He dedicated the win to his mother, who was not in attendance. \"She doesn't watch,\" he said. \"She prays.\""));
        t.newsIfRejected = Stories(Story("El Paso Lucha Card Goes On Without Headliner",
            "El Gringo Loco declared himself champion by default, then left early to file a tax extension."));
        t.newsIfImprisoned = Stories(Story("Detainee Refuses to Remove Mask for Booking Photo",
            "After six hours, officials agreed to photograph him from behind. \"Most polite man we've ever had,\" said a guard."));

        EditorUtility.SetDirty(t);
    }

    // ==================== DAY 3 ====================

    // Beto the trumpet player comes back with a brand-new passport. A breather.
    static void WriteBetoDay3(TravelerData t, VehicleModel car, TravelerData betoDay1)
    {
        Documents(t, "Roberto Salinas Cruz", "MX-2027-RS", "2037-07-29", "Roberto Salinas Cruz", "JAL-0771", "JAL-0771", car);

        t.harmlessExtras = new List<XRayItem>
        {
            Item("Guitarrón case", XRayShape.Round, 0.35f, V(0.36f, 0.62f), V(0.16f, 0.30f), V(0.36f, 0.50f), V(0.16f, 0.34f), icon: "guitar"),
            Item("Trumpet",        XRayShape.Box,   0.85f, V(0.12f, 0.45f), V(0.09f, 0.10f), V(0.12f, 0.40f), V(0.09f, 0.08f), icon: "trumpet"),
        };
        t.contraband = new List<XRayItem>();

        var rest = new List<string>
        {
            "The photo is terrible. I look like my father. My father looked like a potato with a mustache.",
            "Today is a baptism in Anthony. Smaller crowd. Babies don't tip. But the grandmothers do.",
        };
        // Default: you turned him away on Day 1 (the correct call).
        t.boothLines = Lines("¡Oficial! Remember me? Roberto, trumpet. The expired one. Not anymore! Renewed! It took all day and I had to play for the clerk.", rest);
        t.returnVisits = new List<ReturnVisit>
        {
            Visit(betoDay1, Decision.Approve,
                Lines("¡Oficial! Remember me? I hit the high note. The bride cried. The groom's mother cried. I cried. And look: new passport, like you never saw the old one.", rest)),
            Visit(betoDay1, Decision.Imprison,
                Lines("¡Oficial! It's me. They let me go. They said I was \"a disturbance.\" I was IN TUNE. Look: new passport. Please.", rest)),
        };

        t.lineIfApproved = "¡Ajúa! This time I play one for you, oficial. Right here. ...Okay, in the car.";
        t.lineIfRejected = "But it's NEW. It still smells like the printer!";
        t.lineIfImprisoned = "AGAIN?! ...At least I know the guards' names now.";
        NoBribe(t);

        t.newsIfApproved = Stories(Story("Baptism in Anthony Features Surprise Trumpet Solo",
            "The baby slept through it. \"Best review I ever got,\" said the trumpet player."));
        t.newsIfRejected = Stories(Story("Baptism Goes On With Recorder Solo by Cousin, 9",
            "Witnesses described the performance as \"brave.\""));
        t.newsIfImprisoned = Stories(Story("Detention Center Hears Familiar Humming",
            "\"He's back,\" said one guard. \"Honestly? Good.\""));

        EditorUtility.SetDirty(t);
    }

    // The well-meaning American with a sacred trunk full of cactus.
    static void WriteSkyler(TravelerData t, VehicleModel car)
    {
        Documents(t, "Skyler Brennan-Oakes", "US-7734-SB", "2033-02-14", "Skyler Brennan-Oakes", "OR-482-KMB", "OR-482-KMB", car);

        t.harmlessExtras = new List<XRayItem>
        {
            Item("Suitcase", XRayShape.Box, 0.35f, V(0.10f, 0.47f), V(0.09f, 0.19f), V(0.10f, 0.35f), V(0.09f, 0.17f), icon: "suitcase"),
        };
        t.contraband = new List<XRayItem>
        {
            Item("Sack of peyote", XRayShape.Round, 0.40f, V(0.19f, 0.47f), V(0.08f, 0.17f), V(0.17f, 0.66f), V(0.08f, 0.19f), icon: "peyote"),
            // Under the passenger seat: hidden from the side, only the TOP view shows it.
            Item("Sack of peyote", XRayShape.Round, 0.40f, null,            V(0f, 0f),       V(0.62f, 0.72f), V(0.08f, 0.19f), icon: "peyote"),
        };

        t.boothLines = new List<string>
        {
            "Hey! Hi. Hola. Before anything, I just want to say: I see you. This system is violent, and you're part of it, but, like, with dignity.",
            "I was in Oaxaca for a retreat. Plant medicine. So healing. My facilitator says I'm basically indigenous now. Spiritually.",
            "Everything in the trunk is ceremonial. It's sacred? So it's not really \"drugs.\" It's more like... a relationship.",
            "Do you know the history of this land? I could tell you. I took a class. It's a whole thing.",
        };
        t.lineIfApproved = "Thank you. I'm going to sit with this. Literally. I'm going to sit with all of it.";
        t.lineIfRejected = "That's fine. Honestly? I was thinking about moving to Oaxaca anyway. It's SO cheap.";
        t.lineIfImprisoned = "Wait. This is actually so meaningful? I'm experiencing the system? Can I get my phone back, for documentation?";
        t.bribeAmount = 100;
        t.bribeLine = "Okay, so I'm deeply against bribery as a colonial structure. But hypothetically, is a hundred enough? That's like one sound bath. I can Venmo.";

        t.newsIfApproved = Stories(Story("Three Hospitalized After Austin \"Plant Ceremony\"",
            "Organizers called it \"a misunderstanding about the dosage.\" Tickets were $1,200. They were not refunded."));
        t.newsIfRejected = Stories(Story("American Opens Wellness Retreat in Oaxaca",
            "He bought the land from a local family \"for basically nothing.\" \"They were so grateful,\" he said. The family now works the front desk."));
        t.newsIfImprisoned = Stories(Story("Released After Four Hours, Austin Man Launches Podcast",
            "The first episode, \"My Time Inside,\" runs three hours. Detainees who were held longer than four hours were not interviewed."));

        EditorUtility.SetDirty(t);
    }

    // In labor, three days past her passport.
    static void WriteLucia(TravelerData t, VehicleModel car)
    {
        // Expired 2027-07-31; Day 3 is 2027-08-03.
        Documents(t, "Lucía Herrera Domínguez", "MX-4409-LH", "2027-07-31", "Lucía Herrera Domínguez", "CHH-3318", "CHH-3318", car);

        t.harmlessExtras = new List<XRayItem>
        {
            Item("Hospital bag", XRayShape.Box, 0.35f, V(0.36f, 0.62f), V(0.09f, 0.19f), V(0.36f, 0.35f), V(0.09f, 0.17f), icon: "suitcase"),
            Item("Baby gift",    XRayShape.Box, 0.35f, V(0.14f, 0.48f), V(0.08f, 0.18f), V(0.14f, 0.50f), V(0.08f, 0.25f), icon: "gift"),
        };
        t.contraband = new List<XRayItem>();

        t.boothLines = new List<string>
        {
            "Buenos días. Sorry. I'm fine. It's just... (breathes) ...contractions. They're far apart. Kind of far.",
            "My husband is already in El Paso. He works there. The baby's room is there. Everything is there. Even the little socks.",
            "The passport? I know. Three days. I was going to renew it, but look at me. I've been a little busy being a house.",
            "Please be fast. Not for me. For her. She's very impatient. Like her father.",
        };
        t.lineIfApproved = "Gracias. Ay... gracias. I'll tell her about you someday. The good parts.";
        t.lineIfRejected = "Okay. Okay. The hospital in Juárez. It's fine. It's fine. Somebody call my husband. Please, somebody call him.";
        t.lineIfImprisoned = "No, no, no, I need a HOSPITAL! Please, she's coming. She's coming NOW.";
        NoBribe(t);

        t.newsIfApproved = Stories(Story("First Baby of August at Thomason Is a Girl",
            "Seven pounds, two ounces. Her father fainted. Her mother says she \"already has opinions.\""));
        t.newsIfRejected = Stories(Story("Baby Born in Car on the Bridge of the Americas",
            "Traffic stopped both ways for forty minutes. Mother and daughter were taken to a hospital in Juárez. The father watched from the other side.", front: true));
        t.newsIfImprisoned = Stories(Story("Detainee Gives Birth in Holding Cell",
            "No medical staff were on duty. A spokesperson said the matter is \"under review.\" The infant is a U.S. citizen.", front: true));

        EditorUtility.SetDirty(t);
    }

    // A deported veteran on his brother's passport, going to his daughter's wedding.
    static void WriteManny(TravelerData t, VehicleModel car)
    {
        // Passport is his brother Ricardo's; the truck is registered to him, Manuel.
        Documents(t, "Ricardo Ortega Luna", "MX-1966-RO", "2030-11-11", "Manuel Ortega Luna", "CHH-0412", "CHH-0412", car);

        t.harmlessExtras = new List<XRayItem>
        {
            Item("Garment bag",  XRayShape.Box, 0.30f, V(0.20f, 0.52f), V(0.09f, 0.19f), V(0.20f, 0.50f), V(0.09f, 0.17f), icon: "suitcase"),
            Item("Folded flag",  XRayShape.Box, 0.40f, V(0.70f, 0.62f), V(0.08f, 0.06f), V(0.70f, 0.40f), V(0.08f, 0.14f), icon: "flag"),
        };
        t.contraband = new List<XRayItem>();

        t.boothLines = new List<string>
        {
            "Afternoon. Sergeant Ortega. Well. Was. Two tours, Iraq. Fallujah, 2004. You were in diapers, probably.",
            "My daughter's getting married Saturday. El Paso. She asked me to walk her down the aisle. She's asked every year since she was six.",
            "They deported me in 2011. A DUI. I served my time, then they served me to Juárez. Fourteen years. I still say \"y'all.\"",
            "The passport's my brother's. We look alike. Everybody says so. ...He's handsomer. Don't tell him.",
        };
        t.lineIfApproved = "Thank you, son. Hooah.";
        t.lineIfRejected = "...Copy that. She said she'd do a video call. That's nice of her. That's real nice.";
        t.lineIfImprisoned = "Twenty years, they'll give me. For walking my daughter down an aisle. ...Put in the report that I didn't fight you.";
        NoBribe(t);

        t.newsIfApproved = Stories(Story("Bride Walks Down the Aisle With Father in Dress Uniform",
            "Guests say he kept looking at the doors. He danced anyway."));
        t.newsIfRejected = Stories(Story("Deported Veterans Hold Vigil at Friendship Park",
            "One man held a phone up to the fence for an hour. On the screen, someone on the other side was getting married.", front: true));
        t.newsIfImprisoned = Stories(Story("Veteran, 58, Faces Federal Reentry Charge",
            "Prosecutors are seeking twenty years. At his first hearing he wore a borrowed suit and stood when the judge came in. Nobody else did.", front: true));

        EditorUtility.SetDirty(t);
    }

    // Chuy's last crossing. Five packages hidden all over the truck.
    static void WriteChuyDay3(TravelerData t, VehicleModel car)
    {
        Documents(t, "Jesús Valdez Mora", "MX-7781-JV", "2027-11-20", "Jesús Valdez Mora", "CHH-2093", "CHH-2093", car);

        t.harmlessExtras = new List<XRayItem>
        {
            Item("Toolbox", XRayShape.Box, 0.75f, V(0.22f, 0.55f), V(0.12f, 0.18f), V(0.20f, 0.50f), V(0.10f, 0.35f), icon: "toolbox"),
        };
        Vector2 side = V(0.05f, 0.083f), top = V(0.05f, 0.11f);
        t.contraband = new List<XRayItem>
        {
            Item("Package under the seat",  XRayShape.Box, 0.45f, V(0.66f, 0.44f), side, V(0.66f, 0.36f), top, icon: "brick"),
            Item("Package in the spare",    XRayShape.Box, 0.45f, V(0.33f, 0.22f), side, V(0.33f, 0.52f), top, icon: "brick"),
            Item("Package in the gas tank", XRayShape.Box, 0.45f, V(0.47f, 0.27f), side, V(0.47f, 0.28f), top, icon: "brick"),
            // Under the toolbox: the toolbox hides it from the side. TOP view only.
            Item("Package under the tools", XRayShape.Box, 0.45f, null,            side, V(0.20f, 0.62f), top, icon: "brick"),
            // Inside the door panel: the roof hides it from above. SIDE view only.
            Item("Package in the door",     XRayShape.Box, 0.45f, V(0.76f, 0.50f), side, null,            top, icon: "brick"),
        };

        t.boothLines = new List<string>
        {
            "Hey, jefe. ...Hey. Third time, eh? We're basically friends now. Are we friends?",
            "Hot today. Is it hot? It's hot. The truck has no AC. That's why I'm sweating. That's the only reason.",
            "Just going to work. Normal day. Normal Chuy. Normal truck. Nothing in it. I mean, normal stuff in it. Tools.",
            "Can you go a little fast today? Not for any reason. I'm just late. Normal late.",
        };
        t.lineIfApproved = "...Thank you. One time. They said just one time. Then I'm out. ...They said one time, right?";
        t.lineIfRejected = "Back? Jefe, I can't go back with it. And I can't go back without it. ...Okay. I'll figure it out. I always figure it out.";
        t.lineIfImprisoned = "No, no, no, jefe, please! They know where my mom lives! They sent me a picture of Neto's kids outside their school. If this doesn't get there, they don't come for me. They go for THEM. Please!";
        t.bribeAmount = 200;
        t.bribeLine = "Jefe. Please. Two hundred. It's not even mine, it's theirs. They don't care about the money. They care that it gets there.";

        t.newsIfApproved = Stories(
            Story("Four Dead in Albuquerque Overdose Cluster",
                "The pills were pressed to look like ordinary painkillers. Neighbors at two of the homes remember a red pickup with Chihuahua plates.", front: true),
            Story("Diesel Shop Loses Its Best Mechanic",
                "\"Said he found something that paid better,\" the owner said. \"He didn't look happy about it.\""));
        t.newsIfRejected = Stories(Story("Red Pickup Found on Juárez Overpass, Engine Running",
            "The driver's door was open. On the seat: a toolbox, a phone with 31 missed calls, and a photo of two children outside a school.", front: true));
        t.newsIfImprisoned = Stories(
            Story("Record Drug Seizure at the Bridge",
                "Federal officials called it \"a major win.\" The driver, 26, did not ask for a lawyer. He asked for a phone.", front: true),
            Story("Juárez Family Leaves Home Overnight",
                "Neighbors found the door open and a pot still warm on the stove. Before dawn, a man was seen putting two children in a car. He did not say where they were going."));

        EditorUtility.SetDirty(t);
    }

    // ==================== Helpers ====================

    static List<string> Lines(string first, List<string> rest)
    {
        var lines = new List<string> { first };
        lines.AddRange(rest);
        return lines;
    }

    static ReturnVisit Visit(TravelerData earlierVisit, Decision decisionThen, List<string> boothLines) =>
        new ReturnVisit { earlierVisit = earlierVisit, decisionThen = decisionThen, boothLines = boothLines };

    // Makes sure the folder exists, then returns its path.
    static string Folder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder("Assets/Data", System.IO.Path.GetFileName(path));
        return path;
    }

    static DayData Day(string folder, string assetName, int number, string date, params TravelerData[] travelers)
    {
        string path = $"{folder}/{assetName}.asset";
        DayData day = AssetDatabase.LoadAssetAtPath<DayData>(path);
        if (day == null)
        {
            day = ScriptableObject.CreateInstance<DayData>();
            AssetDatabase.CreateAsset(day, path);
        }
        day.dayNumber = number;
        day.todaysDate = date;
        day.travelers = new List<TravelerData>(travelers);
        EditorUtility.SetDirty(day);
        return day;
    }

    // Puts Day 1, 2 and 3 in the GameDirector's "Days" list in the open scene.
    static void AddDaysToDirector(params DayData[] days)
    {
        GameDirector director = Object.FindFirstObjectByType<GameDirector>();
        if (director == null)
        {
            Debug.LogWarning("No GameDirector in the open scene. Open the Checkpoint scene and run this again.");
            return;
        }

        var so = new SerializedObject(director);
        SerializedProperty list = so.FindProperty("days");
        list.arraySize = days.Length;
        for (int i = 0; i < days.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = days[i];
        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
    }
}
