using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The RULES book: one tab per rule in force today, each with GOOD and BAD examples
// drawn like the real documents, plus pictures of what contraband looks like on the X-ray.
// It opens over the right side of the screen, so the papers on the desk stay visible
// for comparing. Built in code by DeskUI; nothing to set up in the scene.
public class Rulebook : MonoBehaviour
{
    enum Tab { Basics, Passport, Permit, XRay, Registration }

    const float W = 1080f, H = 1040f;
    const float ContentTop = 880f;

    RectTransform content;
    RectTransform tabRow;
    TextMeshProUGUI pageTitle;
    readonly List<Tab> tabs = new List<Tab>();
    readonly HashSet<Tab> newToday = new HashSet<Tab>();
    int current;
    DayData today;
    DayData yesterday;

    // Contraband and harmless things seen anywhere in the game, for the X-ray page.
    readonly List<(string title, XRayItem item)> contrabandExamples = new List<(string, XRayItem)>();
    readonly List<XRayItem> harmlessExamples = new List<XRayItem>();

    public bool IsOpen => gameObject.activeSelf;

    public static Rulebook Create(Transform parent)
    {
        RectTransform rect = UiKit.Rect("IllustratedRulebook", parent);
        UiKit.Place(rect, 830, 20, W, H);
        Rulebook book = rect.gameObject.AddComponent<Rulebook>();
        book.Build();
        rect.gameObject.SetActive(false);
        return book;
    }

    void Build()
    {
        Image page = gameObject.AddComponent<Image>();
        page.color = Palette.Paper; // blocks clicks to the X-ray underneath
        Transform t = transform;

        Image header = UiKit.Panel("Header", t, 0, H - 64, W, 64, Palette.PassportGreen);
        UiKit.Label("Title", header.transform, "RULEBOOK  ·  SANTA TERESA PORT OF ENTRY", 26, Palette.Paper,
            TextAlignmentOptions.Center, 0, 0, W, 64, bold: true);

        tabRow = UiKit.Rect("Tabs", t);
        UiKit.Place(tabRow, 20, H - 128, W - 40, 54);

        pageTitle = UiKit.Label("PageTitle", t, "", 30, Palette.PaperInk, TextAlignmentOptions.Left, 40, ContentTop - 6, W - 80, 44, bold: true);
        UiKit.Panel("Rule", t, 40, ContentTop - 12, W - 80, 3, Palette.PaperShadow).raycastTarget = false;

        content = UiKit.Rect("Content", t);
        UiKit.Place(content, 0, 0, W, ContentTop - 16);

        Button prev = UiKit.Button("Prev", t, 40, 20, 120, 52, Palette.Metal, "<", 30, out _);
        prev.onClick.AddListener(() => Turn(-1));
        Button next = UiKit.Button("Next", t, 170, 20, 120, 52, Palette.Metal, ">", 30, out _);
        next.onClick.AddListener(() => Turn(1));
        UiKit.Label("Keys", t, "ARROW KEYS  turn pages     TAB / ESC  close", 18, Palette.WithAlpha(Palette.PaperInk, 0.6f),
            TextAlignmentOptions.Left, 310, 30, 460, 32);
        Button close = UiKit.Button("Close", t, W - 200, 20, 160, 52, Palette.Imprison, "CLOSE", 22, out _);
        close.onClick.AddListener(Close);
    }

    // ---------- What DeskUI calls ----------

    public void UseExamplesFrom(IEnumerable<DayData> days)
    {
        contrabandExamples.Clear();
        harmlessExamples.Clear();
        var seenGroups = new HashSet<string>();
        var seenHarmless = new HashSet<string>();
        foreach (TravelerData traveler in days.Where(d => d != null).SelectMany(d => d.travelers).Where(t => t != null))
        {
            foreach (XRayItem item in traveler.contraband)
            {
                string group = ContrabandGroup(item.label);
                if (item.sideSprite != null && seenGroups.Add(group))
                    contrabandExamples.Add((group, item));
            }
            foreach (XRayItem item in traveler.harmlessExtras)
                if (item.sideSprite != null && harmlessExamples.Count < 4 && seenHarmless.Add(item.label.ToLowerInvariant()))
                    harmlessExamples.Add(item);
        }
        // People first: it's the thing new inspectors miss the most.
        contrabandExamples.Sort((a, b) => Order(a.title).CompareTo(Order(b.title)));
    }

    public void SetDay(DayData day)
    {
        yesterday = today;
        today = day;

        tabs.Clear();
        newToday.Clear();
        tabs.Add(Tab.Basics);
        tabs.Add(Tab.Passport);
        AddIf(Tab.Permit, d => d.permitsRequired);
        AddIf(Tab.XRay, d => d.xrayScanner);
        AddIf(Tab.Registration, d => d.registrationRequired);

        // Open on the newest rule the first time today.
        current = newToday.Count > 0 ? tabs.IndexOf(newToday.Last()) : 0;
        if (IsOpen) Redraw();
    }

    void AddIf(Tab tab, Func<DayData, bool> rule)
    {
        if (!rule(today)) return;
        tabs.Add(tab);
        if (yesterday == null || !rule(yesterday)) newToday.Add(tab);
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (today == null) return;
        Sfx.Play(Sound.Paper);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Redraw();
    }

    public void Close()
    {
        if (!IsOpen) return;
        Sfx.Play(Sound.Paper, 0.7f);
        gameObject.SetActive(false);
    }

    public void Turn(int direction)
    {
        if (tabs.Count == 0) return;
        current = (current + direction + tabs.Count) % tabs.Count;
        Sfx.Play(Sound.Rustle, 0.8f);
        Redraw();
    }

    // ---------- Drawing ----------

    void Redraw()
    {
        Clear(tabRow);
        Clear(content);

        float tabWidth = (W - 40 - (tabs.Count - 1) * 8) / tabs.Count;
        for (int i = 0; i < tabs.Count; i++)
        {
            int index = i;
            bool selected = i == current;
            string name = TabName(tabs[i]) + (newToday.Contains(tabs[i]) ? "  <color=#E8C25A>NEW</color>" : "");
            Button tab = UiKit.Button(tabs[i].ToString(), tabRow, i * (tabWidth + 8), 0, tabWidth, 54,
                selected ? Palette.PaperInk : Palette.PaperShadow, name, 20, out TextMeshProUGUI label);
            label.color = selected ? Palette.Paper : Palette.PaperInk;
            tab.onClick.AddListener(() => { current = index; Sfx.Play(Sound.Rustle, 0.8f); Redraw(); });
        }
        Sfx.HookButtons();

        switch (tabs[current])
        {
            case Tab.Basics: DrawBasics(); break;
            case Tab.Passport: DrawPassport(); break;
            case Tab.Permit: DrawPermit(); break;
            case Tab.XRay: DrawXRay(); break;
            case Tab.Registration: DrawRegistration(); break;
        }
    }

    static string TabName(Tab tab)
    {
        switch (tab)
        {
            case Tab.Basics: return "HOW TO";
            case Tab.Passport: return "PASSPORT";
            case Tab.Permit: return "PERMIT";
            case Tab.XRay: return "X-RAY";
            default: return "REGISTRATION";
        }
    }

    void DrawBasics()
    {
        pageTitle.text = "YOU ARE THE INSPECTOR. EVERY CAR, THE SAME FOUR STEPS:";
        var steps = new (string key, string text)[]
        {
            ("N", "<b>Call the next car.</b> Click the yellow NEXT megaphone on the booth."),
            ("SPACE", "<b>Listen and read.</b> The driver hands you papers. They show up on the desk, bottom left."),
            ("TAB", "<b>Check each paper against this book.</b> Every tab here is a rule in force today."),
            ("A", "<b>Everything in order?</b> <color=#6E8B57>APPROVE</color>: they cross into the US."),
            ("R", "<b>Anything wrong?</b> <color=#A8503F>REJECT</color>: they turn back to Mexico."),
        };
        if (today.xrayScanner)
            steps = steps.Append(("I", "<b>Something hidden in the car?</b> Click it on the X-ray, then IMPRISON.")).ToArray();

        float y = ContentTop - 110;
        foreach ((string key, string text) in steps)
        {
            UiKit.Key(content, key, 60, y);
            UiKit.Label("Step", content, text, 26, Palette.PaperInk, TextAlignmentOptions.Left, 220, y - 10, 800, 60);
            y -= 92;
        }

        // How you know how you did.
        float by = 40;
        UiKit.Picture("Check", content, PixelArt.Check(), 60, by, 80, 80);
        UiKit.Label("Good", content, "Right call: a green check. You get paid for it tonight.", 22, Palette.PaperInk,
            TextAlignmentOptions.Left, 150, by + 10, 330, 70);
        UiKit.Picture("Cross", content, PixelArt.Cross(), 540, by, 80, 80);
        UiKit.Label("Bad", content, "Wrong call: a red X and a CITATION that says what you missed.", 22, Palette.PaperInk,
            TextAlignmentOptions.Left, 630, by + 10, 400, 70);
    }

    void DrawPassport()
    {
        pageTitle.text = $"PASSPORT  ·  EVERYONE  ·  TODAY IS <color=#A8503F>{today.todaysDate}</color>";
        Face lupe = ExampleFaces.A, other = ExampleFaces.B;

        Card(0, true, "GOOD", "Photo is the person at the window. Expires AFTER today.", card =>
        {
            Window(card, lupe);
            MiniPassport(card, lupe, "LUPE ORTIZ", "MEX", Date(+400));
        });
        Card(1, false, "BAD: EXPIRED", "The date is BEFORE today. That's REJECT.", card =>
        {
            Window(card, lupe);
            MiniPassport(card, lupe, "LUPE ORTIZ", "MEX", Date(-30), badExpiry: true);
        });
        Card(2, false, "BAD: NOT THEIR PHOTO", "Someone else's passport. Compare the faces. REJECT.", card =>
        {
            Window(card, lupe);
            MiniPassport(card, other, "LUPE ORTIZ", "MEX", Date(+400), badPhoto: true);
        });
    }

    void DrawPermit()
    {
        pageTitle.text = "ENTRY PERMIT  ·  EVERYONE WHO IS NOT A US CITIZEN";
        Card(0, true, "GOOD", "Name matches the passport letter for letter, and it hasn't expired.", card =>
            MiniPermit(card, "LUPE ORTIZ", "WORK", Date(+90), passportName: "LUPE ORTIZ"));
        Card(1, false, "BAD: WRONG NAME", "Permit and passport must say the SAME name.", card =>
            MiniPermit(card, "LUPE ORTEGA", "VISIT", Date(+90), passportName: "LUPE ORTIZ", badName: true));
        Card(2, false, "BAD: NO PERMIT", "Passport isn't USA and no permit was handed over. REJECT.", card =>
        {
            Image empty = UiKit.Panel("Missing", card, 30, 170, 260, 230, Palette.WithAlpha(Palette.PaperInk, 0.08f));
            empty.raycastTarget = false;
            UiKit.Label("Text", empty.transform, "NATIONALITY: <b>MEX</b>\n\n(no permit)", 24, Palette.Reject,
                TextAlignmentOptions.Center, 0, 0, 260, 230, bold: true);
        });
        UiKit.Label("Note", content, "<b>USA</b> on the passport? Citizens never need a permit. Don't reject them for not having one.",
            22, Palette.PaperInk, TextAlignmentOptions.Center, 40, 20, W - 80, 40);
    }

    void DrawXRay()
    {
        pageTitle.text = "X-RAY  ·  CLICK WHAT DOESN'T BELONG, THEN IMPRISON";

        UiKit.Label("Bad", content, "<color=#A8503F>CONTRABAND</color>  ·  click it on the X-ray, then press IMPRISON",
            24, Palette.PaperInk, TextAlignmentOptions.Left, 40, ContentTop - 70, W - 80, 36, bold: true);
        int shown = Mathf.Min(4, contrabandExamples.Count);
        for (int i = 0; i < shown; i++)
            XRayTile(i, ContentTop - 360, contrabandExamples[i].title, contrabandExamples[i].item, contraband: true);
        if (shown == 0)
            UiKit.Label("None", content, "Hidden people, drug packages, sacks of peyote.", 22, Palette.PaperInk,
                TextAlignmentOptions.Left, 40, ContentTop - 200, W - 80, 40);

        UiKit.Label("Good", content, "<color=#6E8B57>NOT CONTRABAND</color>  ·  luggage and car parts. Leave them alone (a wrong click costs a point)",
            24, Palette.PaperInk, TextAlignmentOptions.Left, 40, ContentTop - 420, W - 80, 36, bold: true);
        int harmless = Mathf.Min(4, harmlessExamples.Count);
        for (int i = 0; i < harmless; i++)
            XRayTile(i, ContentTop - 710, harmlessExamples[i].label.ToUpperInvariant(), harmlessExamples[i], contraband: false);

        UiKit.Label("Tip", content, "Engine, seats, spare tire and gas tank are part of every car. Look for what's hiding IN or BEHIND them.",
            20, Palette.PaperInk, TextAlignmentOptions.Center, 40, 20, W - 80, 50);
    }

    void DrawRegistration()
    {
        pageTitle.text = "VEHICLE REGISTRATION  ·  EVERY CAR";
        Card(0, true, "GOOD", "Owner = passport name. Plate = the PLATE CAM.", card =>
            MiniRegistration(card, "LUPE ORTIZ", "CHH-4821", "CHH-4821"));
        Card(1, false, "BAD: NOT THEIR CAR", "Registered to someone else. REJECT.", card =>
            MiniRegistration(card, "RAMÓN DÍAZ", "CHH-4821", "CHH-4821", badOwner: true));
        Card(2, false, "BAD: WRONG PLATE", "Paper says one plate, the car has another. REJECT.", card =>
            MiniRegistration(card, "LUPE ORTIZ", "CHH-4821", "CHH-4281", badPlate: true));
        UiKit.Label("Note", content, "Driver's passport says <b>LUPE ORTIZ</b> in all three examples.",
            22, Palette.PaperInk, TextAlignmentOptions.Center, 40, 20, W - 80, 40);
    }

    // ---------- Example cards ----------

    // One of three columns: a GOOD/BAD banner, the example, the verdict mark and why.
    void Card(int column, bool good, string heading, string why, Action<RectTransform> draw)
    {
        const float cardW = 320f, cardH = 760f;
        RectTransform card = UiKit.Rect(heading, content);
        UiKit.Place(card, 40 + column * (cardW + 20), 80, cardW, cardH);

        Image back = card.gameObject.AddComponent<Image>();
        back.color = Palette.WithAlpha(good ? Palette.Approve : Palette.Reject, 0.10f);
        back.raycastTarget = false;
        Image banner = UiKit.Panel("Banner", card, 0, cardH - 50, cardW, 50, good ? Palette.Approve : Palette.Reject);
        banner.raycastTarget = false;
        UiKit.Label("Heading", banner.transform, heading, 24, Palette.Paper, TextAlignmentOptions.Center, 8, 0, cardW - 16, 50, bold: true);

        draw(card);

        UiKit.Picture("Mark", card, good ? PixelArt.Check() : PixelArt.Cross(), 20, 30, 90, 90);
        UiKit.Label("Why", card, why, 21, Palette.PaperInk, TextAlignmentOptions.Left, 120, 20, cardW - 130, 110);
    }

    static void Window(RectTransform card, Face face)
    {
        UiKit.Label("AtWindow", card, "AT THE WINDOW", 15, Palette.WithAlpha(Palette.PaperInk, 0.7f), TextAlignmentOptions.Center, 0, 670, 320, 22, bold: true);
        Image frame = UiKit.Panel("Window", card, 110, 560, 100, 110, Palette.Metal);
        frame.raycastTarget = false;
        Image glass = UiKit.Panel("Glass", frame.transform, 6, 6, 88, 98, Palette.Glass);
        glass.raycastTarget = false;
        UiKit.Picture("Face", glass.transform, PortraitArt.Portrait(face), 4, 0, 80, 96);
    }

    static RectTransform Document(RectTransform card, string title, Color color, float top = 540f)
    {
        Image doc = UiKit.Panel("Document", card, 25, top - 300, 270, 300, Palette.Plate);
        doc.raycastTarget = false;
        Image header = UiKit.Panel("Header", doc.transform, 0, 256, 270, 44, color);
        header.raycastTarget = false;
        UiKit.Label("Title", header.transform, title, 18, Palette.Paper, TextAlignmentOptions.Center, 4, 0, 262, 44, bold: true);
        return doc.rectTransform;
    }

    static void MiniPassport(RectTransform card, Face photo, string name, string nationality, string expiry,
        bool badExpiry = false, bool badPhoto = false)
    {
        RectTransform doc = Document(card, "PASAPORTE · PASSPORT", Palette.PassportGreen);
        Image frame = UiKit.Panel("PhotoFrame", doc, 12, 120, 90, 110, Palette.PaperShadow);
        frame.raycastTarget = false;
        UiKit.Picture("Photo", frame.transform, PortraitArt.Portrait(photo), 5, 5, 80, 100);
        if (badPhoto) Circle(doc, 4, 112, 106, 126);

        Field(doc, "NAME", name, 112, 186);
        Field(doc, "NATIONALITY", nationality, 112, 126);
        Field(doc, "EXPIRES", expiry, 112, 66, badExpiry);
        if (badExpiry) Circle(doc, 104, 54, 160, 60);
    }

    static void MiniPermit(RectTransform card, string name, string purpose, string expiry, string passportName, bool badName = false)
    {
        UiKit.Label("Passport", card, $"PASSPORT NAME:  <b>{passportName}</b>", 20, Palette.PaperInk,
            TextAlignmentOptions.Center, 0, 600, 320, 40);
        RectTransform doc = Document(card, "ENTRY PERMIT · PERMISO", Palette.Bribe);
        Field(doc, "NAME", name, 20, 186, badName);
        if (badName) Circle(doc, 12, 174, 246, 60);
        Field(doc, "PURPOSE", purpose, 20, 126);
        Field(doc, "VALID UNTIL", expiry, 20, 66);
    }

    static void MiniRegistration(RectTransform card, string owner, string plate, string carPlate,
        bool badOwner = false, bool badPlate = false)
    {
        // The plate cam, on top, shows the plate on the actual car.
        UiKit.Label("Cam", card, "PLATE CAM", 15, Palette.WithAlpha(Palette.PaperInk, 0.7f), TextAlignmentOptions.Center, 0, 670, 320, 22, bold: true);
        Image cam = UiKit.Panel("PlateCam", card, 60, 570, 200, 96, Palette.Ink);
        cam.raycastTarget = false;
        Image plateImage = UiKit.Panel("Plate", cam.transform, 20, 14, 160, 68, Palette.Plate);
        plateImage.raycastTarget = false;
        UiKit.Label("Number", plateImage.transform, carPlate, 30, Palette.Ink, TextAlignmentOptions.Center, 0, 0, 160, 60, bold: true);
        if (badPlate) Circle(cam.rectTransform, 12, 8, 176, 80);

        RectTransform doc = Document(card, "VEHICLE REGISTRATION", Palette.RegistrationBlue);
        Field(doc, "OWNER", owner, 20, 186, badOwner);
        if (badOwner) Circle(doc, 12, 174, 246, 60);
        Field(doc, "PLATE", plate, 20, 126, badPlate);
        if (badPlate) Circle(doc, 12, 114, 246, 60);
        Field(doc, "MODEL", "Sedan", 20, 66);
    }

    // A small caption with the value underneath, like the real forms.
    static void Field(RectTransform doc, string caption, string value, float x, float y, bool bad = false)
    {
        UiKit.Label(caption, doc, caption, 13, Palette.WithAlpha(Palette.PaperInk, 0.6f), TextAlignmentOptions.Left, x, y + 26, 150, 18, bold: true);
        UiKit.Label(caption + " Value", doc, value, 20, bad ? Palette.Reject : Palette.PaperInk, TextAlignmentOptions.Left, x, y - 4, 150, 30, bold: bad);
    }

    // The red-pen circle around what's wrong.
    static void Circle(RectTransform parent, float x, float y, float w, float h)
    {
        Image ring = UiKit.Panel("Wrong", parent, x, y, w, h, new Color(0, 0, 0, 0));
        ring.raycastTarget = false;
        Outline outline = ring.gameObject.AddComponent<Outline>();
        outline.effectColor = Palette.Reject;
        outline.effectDistance = new Vector2(3, 3);
        outline.useGraphicAlpha = false;
        // An Outline needs something to outline: a nearly invisible fill.
        ring.color = Palette.WithAlpha(Palette.Reject, 0.08f);
    }

    // One X-ray picture on a dark screen, the way it looks on the scanner.
    void XRayTile(int column, float y, string title, XRayItem item, bool contraband)
    {
        const float tileW = 235f, tileH = 230f;
        float x = 40 + column * (tileW + 13);
        Image screen = UiKit.Panel(title, content, x, y, tileW, tileH, new Color(0.07f, 0.09f, 0.12f));
        screen.raycastTarget = false;
        var glow = new Color(0.85f, 0.95f, 1f, 0.95f);
        UiKit.Picture("Item", screen.transform, item.sideSprite, 20, 46, tileW - 40, tileH - 70, glow);
        if (contraband)
            UiKit.Picture("Ring", screen.transform, XRaySprites.Ring, 8, 34, tileW - 16, tileH - 46, new Color(1f, 0.25f, 0.2f));
        UiKit.Label("Name", screen.transform, title, 18, contraband ? new Color(1f, 0.55f, 0.5f) : glow,
            TextAlignmentOptions.Center, 6, 6, tileW - 12, 34, bold: true);
    }

    static string ContrabandGroup(string label)
    {
        string l = (label ?? "").ToLowerInvariant();
        if (l.Contains("child")) return "HIDDEN CHILD";
        if (l.Contains("passenger") || l.Contains("person")) return "HIDDEN PERSON";
        if (l.Contains("package")) return "DRUG PACKAGE";
        if (l.Contains("peyote")) return "PEYOTE";
        return (label ?? "").ToUpperInvariant();
    }

    static int Order(string group) =>
        group == "HIDDEN PERSON" ? 0 : group == "HIDDEN CHILD" ? 1 : group == "DRUG PACKAGE" ? 2 : 3;

    // A date some days from today, in the documents' YYYY-MM-DD format.
    string Date(int daysFromToday)
    {
        if (DateTime.TryParseExact(today.todaysDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
            return d.AddDays(daysFromToday).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return daysFromToday < 0 ? "2020-01-01" : "2030-01-01";
    }

    static void Clear(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    // Two people for the examples: the driver, and whoever's passport they borrowed.
    static class ExampleFaces
    {
        public static readonly Face A = new Face
        {
            skin = 2, hair = Hair.Long, hairColor = new Color(0.2f, 0.14f, 0.1f), lipstick = true,
            shirt = new Color(0.66f, 0.31f, 0.25f),
        };
        public static readonly Face B = new Face
        {
            skin = 3, hair = Hair.Curly, hairColor = new Color(0.12f, 0.1f, 0.09f), glasses = true,
            shirt = new Color(0.3f, 0.42f, 0.36f),
        };
    }
}
