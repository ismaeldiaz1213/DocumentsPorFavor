using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Day1StoryWriter;
using static StoryWriter;

// Editor-only helper, run by StoryWriter ("Documents Por Favor > Write Story").
// The everyday line: people going to work, to the store, home. Most of them
// say hello and nothing else. Some have a problem with their papers, and they
// never mention it. You find out what they knew only after you catch it.
//
//   Day 1  Abel (fine)        Nora (passport expired)
//   Day 2  Ofelia (fine)      Tomás (permit in a different name)
//   Day 3  Dolores (fine)     Kevin (fine, US)       Ana (packages hidden in the car)
//   Day 4  Pilar (fine)       Ernesto (plate doesn't match)   Jill (fine, US)
public static class CommutersWriter
{
    const string Day1 = "Assets/Data/Day1";
    const string Day2 = "Assets/Data/Day2";
    const string Day3 = "Assets/Data/Day3";
    const string Day4 = "Assets/Data/Day4";

    public struct Commuters
    {
        public TravelerData abel, nora, ofelia, tomas, dolores, kevin, ana, pilar, ernesto, jill;
    }

    public static Commuters WriteAll()
    {
        VehicleModel sedan = Vehicle("Sedan");
        VehicleModel pickup = Vehicle("Pickup");
        VehicleModel van = Vehicle("Van");
        var c = new Commuters();

        // ---------- Day 1 (2027-08-01): passports ----------

        c.abel = Commuter("C1_Abel", Day1, new Color(0.62f, 0.58f, 0.5f),
            "Abel Duarte Soto", "MX-2214-AD", "2031-03-14", "CHH-3381", sedan,
            F(2, Hair.Short, "201814", mustache: true, shirt: "8A8F7A"));
        Talk(c.abel, "Buenas tardes.",
            approved: "Gracias. Que le vaya bien.",
            rejected: "¿Qué? ...Bueno. Okay.",
            imprisoned: "¿Por qué? ¡No hice nada!");

        // Expired two weeks ago. She knows.
        c.nora = Commuter("C1_Nora", Day1, new Color(0.7f, 0.66f, 0.42f),
            "Nora Elizondo Vega", "MX-5530-NE", "2027-07-19", "CHH-9024", pickup,
            F(1, Hair.Long, "3A2A20", shirt: "B5654A"));
        Talk(c.nora, "Hi. Hope your day's going okay.",
            approved: "Thanks. You too.",
            rejected: "...Yeah. I was hoping you wouldn't notice.",
            imprisoned: "For THAT? It's a couple of weeks!");

        // ---------- Day 2 (2027-08-02): + entry permits ----------

        c.ofelia = Commuter("C2_Ofelia", Day2, new Color(0.5f, 0.6f, 0.68f),
            "Ofelia Cárdenas Luna", "MX-6105-OC", "2030-10-10", "CHH-7152", sedan,
            F(3, Hair.Bun, "B9B4AA", wrinkles: true, shirt: "7C6A9A"), permit: ("WORK", "2028-04-30"));
        Talk(c.ofelia, "Hola, buenas tardes.",
            approved: "Gracias, joven.",
            rejected: "Pero... my papers are fine. ¿Qué pasó?",
            imprisoned: "¡Dios mío! ¿Por qué?");

        // His permit says Ruiz. His passport says Gil.
        c.tomas = Commuter("C2_Tomas", Day2, new Color(0.4f, 0.42f, 0.45f),
            "Tomás Ibarra Gil", "MX-8830-TI", "2029-01-22", "CHH-6047", pickup,
            F(2, Hair.Short, "201814", Headwear.Cap, "2F4A6B", beard: true, shirt: "4E5A4A"), permit: ("WORK", "2028-02-28"));
        c.tomas.permitName = "Tomás Ibarra Ruiz";
        Talk(c.tomas, "Buenas.",
            approved: "Yeah.",
            rejected: "Unbelievable. Every day it's something with you people. You enjoy this, don't you? Little man in a little box.",
            imprisoned: "Over a NAME? Are you serious?");

        // ---------- Day 3 (2027-08-03): + the X-ray ----------

        c.dolores = Commuter("C3_Dolores", Day3, new Color(0.72f, 0.5f, 0.45f),
            "Dolores Peña Robles", "MX-1157-DP", "2032-05-05", "CHH-2286", sedan,
            F(3, Hair.Gray, "B9B4AA", glasses: true, wrinkles: true, shirt: "D9A441"), permit: ("VISIT", "2028-09-15"));
        c.dolores.harmlessExtras = new List<XRayItem>
        {
            Item("Grocery bags", XRayShape.Box, 0.30f, V(0.36f, 0.58f), V(0.09f, 0.16f), V(0.36f, 0.62f), V(0.09f, 0.16f), icon: "suitcase"),
        };
        Talk(c.dolores, "Buenas tardes, mijo. Hace calor, ¿no?",
            approved: "Gracias, mijo. Cuídate.",
            rejected: "¿Cómo que no? I cross every week, mijo.",
            imprisoned: "¡Son verduras! They're VEGETABLES!");

        c.kevin = Commuter("C3_Kevin", Day3, new Color(0.3f, 0.36f, 0.5f),
            "Kevin Marsh", "US-4471-KM", "2033-01-01", "TX-88K-210", pickup,
            F(0, Hair.Short, "8A5A3C", Headwear.Cap, "A8503F", shirt: "6E8FB0"), nationality: "USA");
        c.kevin.harmlessExtras = new List<XRayItem>
        {
            Item("Cooler", XRayShape.Box, 0.35f, V(0.25f, 0.55f), V(0.12f, 0.15f), V(0.25f, 0.45f), V(0.12f, 0.25f), icon: "suitcase"),
        };
        Talk(c.kevin, "Hey. Hot one today.",
            approved: "Thanks, man.",
            rejected: "Wait, what? I'm a citizen. What did I do?",
            imprisoned: "Whoa, whoa. I want a lawyer.");

        // Nothing to say. Two packages: under the back seat, and by the spare.
        c.ana = Commuter("C3_Ana", Day3, new Color(0.55f, 0.6f, 0.55f),
            "Ana Lucero Ríos", "MX-3398-AL", "2030-06-06", "CHH-5513", sedan,
            F(2, Hair.Long, "201814", shirt: "A8A39A"), permit: ("WORK", "2028-03-03"));
        Vector2 side = V(0.05f, 0.083f), top = V(0.05f, 0.11f);
        c.ana.contraband = new List<XRayItem>
        {
            Item("Package under the back seat", XRayShape.Box, 0.45f, V(0.38f, 0.40f), side, V(0.38f, 0.40f), top, icon: "brick"),
            Item("Package by the spare",        XRayShape.Box, 0.45f, V(0.14f, 0.46f), side, V(0.14f, 0.62f), top, icon: "brick"),
        };
        Talk(c.ana, "Hola.",
            approved: "Gracias.",
            rejected: "...Okay. Okay.",
            imprisoned: "...I didn't even want to do this. They paid my rent. That's all. They paid my rent.");
        c.ana.newsIfApproved = Stories(Story("Small Seizure in El Paso Parking Garage",
            "Police found two packages in a car registered to a Juárez woman. The car was empty. Its rent, a neighbor said, was paid through December.", daysLater: 1));

        // ---------- Day 4 (2027-08-04): + vehicle registration ----------

        c.pilar = Commuter("C4_Pilar", Day4, new Color(0.85f, 0.82f, 0.76f),
            "Pilar Montes Ochoa", "MX-4402-PM", "2031-11-30", "CHH-8125", van,
            F(1, Hair.Bun, "5A3B28", lipstick: true, shirt: "5E8C88"), permit: ("WORK", "2028-07-07"));
        c.pilar.harmlessExtras = new List<XRayItem>
        {
            Item("Boxes", XRayShape.Box, 0.30f, V(0.40f, 0.45f), V(0.14f, 0.30f), V(0.40f, 0.50f), V(0.14f, 0.40f), icon: "crate"),
        };
        Talk(c.pilar, "Buenas tardes. ¿Cómo está?",
            approved: "Gracias. Buen día.",
            rejected: "Seriously? Everything's there. Look again.",
            imprisoned: "What? No, no, no!");

        // The registration says 4410. The car says 4401.
        c.ernesto = Commuter("C4_Ernesto", Day4, new Color(0.48f, 0.3f, 0.3f),
            "Ernesto Salas Quiroga", "MX-7270-ES", "2030-02-02", "CHH-4410", sedan,
            F(2, Hair.Short, "3A3530", glasses: true, shirt: "C9B98A"), permit: ("WORK", "2028-10-10"));
        c.ernesto.vehiclePlate = "CHH-4401";
        c.ernesto.harmlessExtras = new List<XRayItem>
        {
            Item("Gym bag", XRayShape.Box, 0.35f, V(0.36f, 0.58f), V(0.09f, 0.16f), V(0.36f, 0.62f), V(0.09f, 0.16f), icon: "suitcase"),
        };
        Talk(c.ernesto, "Buenas tardes.",
            approved: "Gracias.",
            rejected: "Ay. My brother-in-law's plates. I KNEW it. I knew he'd do this to me.",
            imprisoned: "For the PLATES? Arrest my brother-in-law!");

        c.jill = Commuter("C4_Jill", Day4, new Color(0.75f, 0.45f, 0.55f),
            "Jill Hoffman", "US-9014-JH", "2029-04-04", "NM-552-KLR", pickup,
            F(0, Hair.Long, "C9A86A", glasses: true, shirt: "E3C07A"), nationality: "USA");
        Talk(c.jill, "Hi there!",
            approved: "Thank you! Have a good one.",
            rejected: "Oh. Um. Okay? Can I ask why?",
            imprisoned: "I'm sorry, WHAT?");

        foreach (TravelerData t in new[] { c.abel, c.nora, c.ofelia, c.tomas, c.dolores, c.kevin, c.ana, c.pilar, c.ernesto, c.jill })
            EditorUtility.SetDirty(t);
        return c;
    }

    // A plain traveler: their own face, their own car, a permit if given, nothing hidden, no news.
    static TravelerData Commuter(string assetName, string folder, Color carColor, string name, string number,
        string expiry, string plate, VehicleModel car, Face face, string nationality = "MEX",
        (string purpose, string expiry)? permit = null)
    {
        TravelerData t = Traveler(assetName, carColor, folder);
        Documents(t, name, number, expiry, name, plate, plate, car);
        Person(t, face, nationality, permit);
        t.harmlessExtras = new List<XRayItem>();
        t.contraband = new List<XRayItem>();
        t.returnVisits = new List<ReturnVisit>();
        t.newsIfApproved = new List<NewsStory>();
        t.newsIfRejected = new List<NewsStory>();
        t.newsIfImprisoned = new List<NewsStory>();
        NoBribe(t);
        return t;
    }

    static void Talk(TravelerData t, string hello, string approved, string rejected, string imprisoned)
    {
        t.boothLines = new List<string> { hello };
        t.lineIfApproved = approved;
        t.lineIfRejected = rejected;
        t.lineIfImprisoned = imprisoned;
    }
}
