using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The RULES book: one tab per rule in force today, each with GOOD and BAD examples
// drawn like the real documents, plus pictures of what contraband looks like on the X-ray.
//
// Two ways it opens:
//   - The morning briefing (Brief): before each shift it opens in the middle of the
//     screen on just today's NEW pages, with a line from the jefe. BEGIN SHIFT closes it.
//   - The RULES button / TAB during the shift: it opens over the right side of the
//     screen, so the papers on the desk stay visible for comparing.
// Built in code by DeskUI; nothing to set up in the scene.
public class Rulebook : MonoBehaviour
{
    enum Tab { Basics, Passport, Permit, XRay, Registration }

    // Page layout, in canvas pixels. The book stops below the HUD so the RULES button stays visible.
    const float W = 1080f, H = 950f;
    const float ShiftX = 830f, BriefX = 420f, Y = 20f;
    const float ContentBottom = 88f;          // above the page-turn buttons
    const float ContentHeight = 665f;         // up to just under the page title
    const float CardScale = 0.82f;

    RectTransform rect;
    RectTransform content;
    RectTransform tabRow;
    TextMeshProUGUI headerText;
    TextMeshProUGUI pageTitle;
    TextMeshProUGUI closeLabel;
    GameObject turnButtons;

    readonly List<Tab> tabs = new List<Tab>();        // every rule in force today
    readonly List<Tab> newToday = new List<Tab>();    // the ones that started today
    List<Tab> Shown => briefing ? newToday : tabs;
    int current;
    DayData today;
    DayData yesterday;

    bool briefing;
    bool briefingDone;

    // Contraband and harmless things seen anywhere in the game, for the X-ray page.
    readonly List<(string title, XRayItem item)> contrabandExamples = new List<(string, XRayItem)>();
    readonly List<XRayItem> harmlessExamples = new List<XRayItem>();

    public bool IsOpen => gameObject.activeSelf;
    public bool InBriefing => briefing;

    public static Rulebook Create(Transform parent)
    {
        RectTransform rect = UiKit.Rect("IllustratedRulebook", parent);
        UiKit.Place(rect, ShiftX, Y, W, H);
        Rulebook book = rect.gameObject.AddComponent<Rulebook>();
        book.rect = rect;
        book.Build();
        rect.gameObject.SetActive(false);
        return book;
    }

    void Build()
    {
        Image page = gameObject.AddComponent<Image>();
        page.color = Palette.Paper; // also blocks clicks to the X-ray underneath
        Transform t = transform;

        Image header = UiKit.Panel("Header", t, 0, H - 64, W, 64, Palette.PassportGreen);
        headerText = UiKit.Label("Title", header.transform, "", 26, Palette.Paper, TextAlignmentOptions.Center, 16, 0, W - 32, 64, bold: true);

        tabRow = UiKit.Rect("Tabs", t);
        UiKit.Place(tabRow, 20, H - 126, W - 40, 52);

        pageTitle = UiKit.Label("PageTitle", t, "", 28, Palette.PaperInk, TextAlignmentOptions.Left, 40, H - 180, W - 80, 44, bold: true);
        UiKit.Panel("Rule", t, 40, H - 186, W - 80, 3, Palette.PaperShadow).raycastTarget = false;

        content = UiKit.Rect("Content", t);
        UiKit.Place(content, 0, ContentBottom, W, ContentHeight);

        RectTransform turn = UiKit.Rect("Turn", t);
        UiKit.Place(turn, 40, 20, 230, 52);
        turnButtons = turn.gameObject;
        Button prev = UiKit.Button("Prev", turn, 0, 0, 110, 52, Palette.Metal, "<", 30, out _);
        prev.onClick.AddListener(() => Turn(-1));
        Button next = UiKit.Button("Next", turn, 120, 0, 110, 52, Palette.Metal, ">", 30, out _);
        next.onClick.AddListener(() => Turn(1));
        Button close = UiKit.Button("Close", t, W - 280, 20, 240, 52, Palette.Imprison, "CLOSE", 22, out closeLabel);
        close.onClick.AddListener(CloseClicked);
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
                if (item.sideSprite != null && seenGroups.Add(ContrabandGroup(item.label)))
                    contrabandExamples.Add((ContrabandGroup(item.label), item));
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
        if (yesterday == null) newToday.AddRange(tabs); // the first day, everything is new
        AddIf(Tab.Permit, d => d.permitsRequired);
        AddIf(Tab.XRay, d => d.xrayScanner);
        AddIf(Tab.Registration, d => d.registrationRequired);

        // During the shift, the book opens on the newest rule first.
        current = newToday.Count > 0 ? tabs.IndexOf(newToday.Last()) : 0;
        if (IsOpen) Redraw();
    }

    void AddIf(Tab tab, Func<DayData, bool> rule)
    {
        if (!rule(today)) return;
        tabs.Add(tab);
        if (yesterday == null || !rule(yesterday)) newToday.Add(tab);
    }

    // The morning briefing: today's new pages, one after another, then BEGIN SHIFT.
    public IEnumerator Brief()
    {
        if (today == null || newToday.Count == 0) yield break;
        briefing = true;
        briefingDone = false;
        current = 0;
        UiKit.Place(rect, BriefX, Y, W, H);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Sfx.Play(Sound.Paper);
        Redraw();

        while (!briefingDone)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (NoteScreen.ContinuePressed()) CloseClicked();
                else if (keyboard.rightArrowKey.wasPressedThisFrame) Turn(1);
                else if (keyboard.leftArrowKey.wasPressedThisFrame) Turn(-1);
            }
            yield return null;
        }

        briefing = false;
        gameObject.SetActive(false);
        UiKit.Place(rect, ShiftX, Y, W, H);
        current = tabs.IndexOf(newToday.Last());
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (today == null || briefing) return;
        Sfx.Play(Sound.Paper);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Redraw();
    }

    public void Close()
    {
        if (!IsOpen || briefing) return;
        Sfx.Play(Sound.Paper, 0.7f);
        gameObject.SetActive(false);
    }

    // In the briefing the button reads NEXT PAGE, then BEGIN SHIFT.
    void CloseClicked()
    {
        if (!briefing)
        {
            Close();
            return;
        }
        if (current < newToday.Count - 1) Turn(1);
        else
        {
            Sfx.Play(Sound.Stamp);
            briefingDone = true;
        }
    }

    public void Turn(int direction)
    {
        List<Tab> shown = Shown;
        if (shown.Count < 2) return;
        current = Mathf.Clamp(current + direction, 0, shown.Count - 1);
        Sfx.Play(Sound.Rustle, 0.8f);
        Redraw();
    }

    // ---------- Drawing ----------

    void Redraw()
    {
        Clear(tabRow);
        Clear(content);
        List<Tab> shown = Shown;
        current = Mathf.Clamp(current, 0, shown.Count - 1);

        // The briefing is just one page at a time and one button. No tabs, no page turning.
        headerText.text = briefing
            ? $"MORNING BRIEFING  ·  DAY {today.dayNumber}" + (shown.Count > 1 ? $"  <size=70%>({current + 1}/{shown.Count})</size>" : "")
            : "RULEBOOK";
        closeLabel.text = !briefing ? "CLOSE" : current < shown.Count - 1 ? "NEXT PAGE" : "BEGIN SHIFT";
        tabRow.gameObject.SetActive(!briefing);
        turnButtons.SetActive(!briefing);

        float tabWidth = Mathf.Min(260f, (W - 40 - (shown.Count - 1) * 8) / shown.Count);
        for (int i = 0; i < shown.Count; i++)
        {
            int index = i;
            bool selected = i == current;
            bool isNew = !briefing && newToday.Contains(shown[i]) && today.dayNumber > 1;
            string name = TabName(shown[i]) + (isNew ? "  <color=#B08A35>NEW</color>" : "");
            Button tab = UiKit.Button(shown[i].ToString(), tabRow, i * (tabWidth + 8), 0, tabWidth, 52,
                selected ? Palette.PaperInk : Palette.PaperShadow, name, 20, out TextMeshProUGUI label);
            label.color = selected ? Palette.Paper : Palette.PaperInk;
            tab.onClick.AddListener(() => { current = index; Sfx.Play(Sound.Rustle, 0.8f); Redraw(); });
        }
        Sfx.HookButtons();

        switch (shown[current])
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

    // Just the keys and what they do.
    void DrawBasics()
    {
        pageTitle.text = "EACH CAR";
        var steps = new List<(string key, Sprite icon, string text)>
        {
            ("N", PixelArt.MegaphoneIcon(), "Call the next car."),
            ("SPACE", PixelArt.SpeechIcon(), "Hear what the driver says."),
            ("TAB", PixelArt.BookIcon(), "Check their papers against this rulebook."),
            ("A", PixelArt.Check(), "Everything looks good? <color=#6E8B57><b>APPROVE</b></color>."),
            ("R", PixelArt.Cross(), "Anything wrong? <color=#A8503F><b>REJECT</b></color>."),
        };
        if (today.xrayScanner)
            steps.Add(("I", PixelArt.CuffsIcon(), "Something hidden in the car? Click it on the X-ray, then <b>IMPRISON</b>."));

        float y = ContentHeight - 90;
        foreach ((string key, Sprite icon, string text) in steps)
        {
            UiKit.Key(content, key, 120, y, 52f);
            UiKit.Picture("Icon", content, icon, 270, y - 6, 60, 60);
            UiKit.Label("Step", content, text, 30, Palette.PaperInk, TextAlignmentOptions.Left, 360, y - 8, 660, 60);
            y -= 100;
        }
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
            UiKit.Label("Passport", card, "NATIONALITY:  <b>MEX</b>", 22, Palette.PaperInk, TextAlignmentOptions.Center, 0, 600, 320, 40);
            UiKit.Frame("Missing", card, 25, 240, 270, 300, 4, Palette.WithAlpha(Palette.Reject, 0.7f));
            UiKit.Label("Text", card, "NO PERMIT", 36, Palette.Reject, TextAlignmentOptions.Center, 25, 240, 270, 300, bold: true);
        });
        Note("<b>USA</b> on the passport? Citizens never need a permit. Don't reject them for not having one.");
    }

    void DrawXRay()
    {
        pageTitle.text = "X-RAY  ·  CLICK WHAT DOESN'T BELONG, THEN IMPRISON";

        UiKit.Label("Bad", content, "<color=#A8503F>CONTRABAND</color>  ·  click it on the X-ray, then IMPRISON",
            22, Palette.PaperInk, TextAlignmentOptions.Left, 40, ContentHeight - 40, W - 80, 34, bold: true);
        int shown = Mathf.Min(4, contrabandExamples.Count);
        for (int i = 0; i < shown; i++)
            XRayTile(i, ContentHeight - 250, contrabandExamples[i].title, contrabandExamples[i].item, contraband: true);

        UiKit.Label("Good", content, "<color=#6E8B57>NOT CONTRABAND</color>  ·  luggage and car parts. Leave them alone: a wrong click costs a point",
            22, Palette.PaperInk, TextAlignmentOptions.Left, 40, ContentHeight - 300, W - 80, 34, bold: true);
        int harmless = Mathf.Min(4, harmlessExamples.Count);
        for (int i = 0; i < harmless; i++)
            XRayTile(i, ContentHeight - 510, harmlessExamples[i].label.ToUpperInvariant(), harmlessExamples[i], contraband: false);

        Note("Engine, seats, spare tire and gas tank are part of every car. Look for what's hiding IN or BEHIND them.");
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
        Note("The driver's passport says <b>LUPE ORTIZ</b> in all three examples.");
    }

    void Note(string text) =>
        UiKit.Label("Note", content, text, 20, Palette.PaperInk, TextAlignmentOptions.Center, 40, 0, W - 80, 34);

    // ---------- Example cards ----------

    // One of three columns: a GOOD/BAD banner, the example, the verdict mark and why.
    // Drawn at 320 x 760 and scaled down to fit the page.
    void Card(int column, bool good, string heading, string why, Action<RectTransform> draw)
    {
        const float cardW = 320f, cardH = 760f;
        RectTransform card = UiKit.Rect(heading, content);
        float columnWidth = (W - 80) / 3f;
        UiKit.Place(card, 40 + column * columnWidth + (columnWidth - cardW * CardScale) / 2f, 40, cardW, cardH);
        card.localScale = Vector3.one * CardScale;

        Image back = card.gameObject.AddComponent<Image>();
        back.color = Palette.WithAlpha(good ? Palette.Approve : Palette.Reject, 0.10f);
        back.raycastTarget = false;
        Image banner = UiKit.Panel("Banner", card, 0, cardH - 56, cardW, 56, good ? Palette.Approve : Palette.Reject);
        banner.raycastTarget = false;
        UiKit.Label("Heading", banner.transform, heading, 26, Palette.Paper, TextAlignmentOptions.Center, 8, 0, cardW - 16, 56, bold: true);

        draw(card);

        UiKit.Picture("Mark", card, good ? PixelArt.Check() : PixelArt.Cross(), 16, 40, 100, 100);
        UiKit.Label("Why", card, why, 24, Palette.PaperInk, TextAlignmentOptions.Left, 124, 20, cardW - 132, 150);
    }

    static void Window(RectTransform card, Face face)
    {
        UiKit.Label("AtWindow", card, "AT THE WINDOW", 17, Palette.WithAlpha(Palette.PaperInk, 0.7f), TextAlignmentOptions.Center, 0, 672, 320, 26, bold: true);
        Image frame = UiKit.Panel("Window", card, 110, 560, 100, 110, Palette.Metal);
        frame.raycastTarget = false;
        Image glass = UiKit.Panel("Glass", frame.transform, 6, 6, 88, 98, Palette.Glass);
        glass.raycastTarget = false;
        UiKit.Picture("Face", glass.transform, PortraitArt.Portrait(face), 4, 0, 80, 96);
    }

    static RectTransform Document(RectTransform card, string title, Color color)
    {
        Image doc = UiKit.Panel("Document", card, 25, 240, 270, 300, Palette.Plate);
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
        UiKit.Label("Passport", card, $"PASSPORT SAYS:  <b>{passportName}</b>", 22, Palette.PaperInk,
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
        UiKit.Label("Cam", card, "PLATE CAM", 17, Palette.WithAlpha(Palette.PaperInk, 0.7f), TextAlignmentOptions.Center, 0, 672, 320, 26, bold: true);
        Image cam = UiKit.Panel("PlateCam", card, 60, 566, 200, 100, Palette.Ink);
        cam.raycastTarget = false;
        Image plateImage = UiKit.Panel("Plate", cam.transform, 20, 16, 160, 68, Palette.Plate);
        plateImage.raycastTarget = false;
        UiKit.Label("Number", plateImage.transform, carPlate, 30, Palette.Ink, TextAlignmentOptions.Center, 0, 0, 160, 68, bold: true);
        if (badPlate) Circle(cam.rectTransform, 12, 8, 176, 84);

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

    // The red-pen box around what's wrong.
    static void Circle(RectTransform parent, float x, float y, float w, float h) =>
        UiKit.Frame("Wrong", parent, x, y, w, h, 4, Palette.Reject);

    // One X-ray picture on a dark screen, the way it looks on the scanner.
    void XRayTile(int column, float y, string title, XRayItem item, bool contraband)
    {
        const float tileW = 235f, tileH = 200f;
        float x = 40 + column * (tileW + 13);
        Image screen = UiKit.Panel(title, content, x, y, tileW, tileH, new Color(0.07f, 0.09f, 0.12f));
        screen.raycastTarget = false;
        var glow = new Color(0.85f, 0.95f, 1f, 0.95f);
        UiKit.Picture("Item", screen.transform, item.sideSprite, 24, 44, tileW - 48, tileH - 60, glow);
        if (contraband)
            UiKit.Picture("Ring", screen.transform, XRaySprites.Ring, 10, 34, tileW - 20, tileH - 40, new Color(1f, 0.25f, 0.2f));
        UiKit.Label("Name", screen.transform, title, 18, contraband ? new Color(1f, 0.55f, 0.5f) : glow,
            TextAlignmentOptions.Center, 6, 6, tileW - 12, 32, bold: true);
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
