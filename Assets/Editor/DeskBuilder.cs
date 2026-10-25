using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menu "Documents Por Favor > Build Desk".
// Lays out the desk UI and every full-screen page (title, tutorial, ledger,
// newspaper, ending) inside the "Desk" canvas, and wires them to a GameDirector.
// Safe to run again: it deletes what it built last time and rebuilds.
// Your hand-built XRayPanel and BossBark are kept (BossBark gets moved).
//
// Layout is in canvas pixels (1920 x 1080), measured from the bottom-left.
// The desk is the bottom half: y 0 to 540.
public static class DeskBuilder
{
    const string BackName = "Generated";         // drawn BEHIND your X-ray panel
    const string FrontName = "GeneratedOverlay"; // drawn IN FRONT of everything

    [MenuItem("Documents Por Favor/Build Desk")]
    public static void Build()
    {
        Canvas canvas = FindCanvas("Desk");
        CheckpointStage stage = Object.FindFirstObjectByType<CheckpointStage>();
        XRayPanel xray = Object.FindFirstObjectByType<XRayPanel>();
        BossBark boss = Object.FindFirstObjectByType<BossBark>();

        if (canvas == null || stage == null || xray == null)
        {
            EditorUtility.DisplayDialog("Build Desk",
                "Couldn't find the Desk canvas, the CheckpointStage or the XRayPanel.\nOpen the Checkpoint scene first.", "OK");
            return;
        }

        DestroyChild(canvas.transform, BackName);
        DestroyChild(canvas.transform, FrontName);

        RectTransform back = Stretch(BackName, canvas.transform);
        back.SetAsFirstSibling();
        RectTransform front = Stretch(FrontName, canvas.transform);
        front.SetAsLastSibling();

        DeskUI desk = back.gameObject.AddComponent<DeskUI>();
        var ui = new SerializedObject(desk);
        BuildDeskSurface(back);
        BuildDialogue(back, ui);
        BuildDocuments(back, ui);
        BuildPlateCam(back, ui);
        BuildButtons(back, ui);
        BuildHud(front, ui);
        BuildCitationSlip(front, ui);
        BuildDayBanner(front, ui);
        ui.ApplyModifiedPropertiesWithoutUndo();

        // Always-visible reminder on the counter, right above the X-ray monitor.
        Label("XRayHint", front, "X-RAY  ·  CLICK ANYTHING HIDDEN IN THE CAR  ·  WRONG CLICK = -1 POINT",
            15, Palette.Warning, TextAlignmentOptions.Center, 1250, 521, 640, 20, bold: true).transform.SetAsFirstSibling();

        // Full-screen pages, in drawing order (the title ends up on top).
        TutorialGuide guide = BuildTutorialGuide(front);
        LedgerScreen ledger = BuildLedger(front);
        NewspaperScreen newspaper = BuildNewspaper(front);
        EndingScreen ending = BuildEnding(front);
        TitleScreen title = BuildTitle(front);

        if (boss != null)
            Place((RectTransform)boss.transform, 40, 548, 1150, 60);

        GameObject placeholder = GameObject.Find("DeskPlaceholder");
        if (placeholder != null) placeholder.SetActive(false);

        WireDirector(stage, xray, desk, title, guide, ledger, newspaper, ending);
        AddSceneToBuild(canvas.gameObject.scene.path);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("Desk built. Press Ctrl+S to save the scene.");
    }

    // ---------- Desk ----------

    static void BuildDeskSurface(Transform parent)
    {
        // Use the pixel-art wood if "Generate And Apply Art" has made it; flat color otherwise.
        Sprite wood = AssetDatabase.LoadAssetAtPath<Sprite>(ArtGenerator.Folder + "/desk_wood.png");
        Image surface = Panel("DeskSurface", parent, 0, 0, 1920, 540, wood != null ? Color.white : Palette.DeskWood);
        surface.sprite = wood;
        if (wood == null)
            foreach (float y in new[] { 110f, 250f, 395f })
                Panel("Grain", parent, 0, y, 1920, 3, Palette.WithAlpha(Palette.DeskWoodDark, 0.45f));
        Panel("CounterEdge", parent, 0, 522, 1920, 18, Palette.DeskWoodDark);
    }

    static void BuildDialogue(Transform parent, SerializedObject ui)
    {
        Image box = Panel("Dialogue", parent, 40, 330, 760, 175, Palette.WithAlpha(Palette.Speech, 0.95f));
        Ref(ui, "speakerText", Label("Speaker", box.transform, "", 24, Palette.Warning, TextAlignmentOptions.Left, 20, 128, 720, 34, bold: true));
        Ref(ui, "lineText", AutoSize(Label("Line", box.transform, "", 26, Palette.Paper, TextAlignmentOptions.TopLeft, 20, 12, 640, 112), 18));
        Ref(ui, "nextLineButton", MakeButton("Next", box.transform, 680, 14, 64, 46, Palette.Metal, ">>", 22, out _));
    }

    static void BuildDocuments(Transform parent, SerializedObject ui)
    {
        // Passport
        Image passport = Panel("Passport", parent, 40, 40, 370, 262, Palette.Paper);
        Ref(ui, "passport", passport.gameObject);
        DocumentHeader(passport.transform, "PASAPORTE · PASSPORT", Palette.PassportGreen);
        Image photo = Panel("Photo", passport.transform, 18, 46, 92, 150, Palette.PaperShadow);
        Panel("Head", photo.transform, 28, 84, 36, 40, Palette.WithAlpha(Palette.PaperInk, 0.35f));
        Panel("Shoulders", photo.transform, 14, 16, 64, 60, Palette.WithAlpha(Palette.PaperInk, 0.35f));

        Ref(ui, "passportName", Field(passport.transform, "NAME", 125, 166, 232, 22));
        Ref(ui, "passportNumber", Field(passport.transform, "PASSPORT No.", 125, 108, 232, 22));
        Ref(ui, "passportExpiry", Field(passport.transform, "EXPIRES", 125, 50, 232, 22));

        // Registration
        Image registration = Panel("Registration", parent, 430, 40, 370, 262, Palette.Paper);
        Ref(ui, "registration", registration.gameObject);
        DocumentHeader(registration.transform, "VEHICLE REGISTRATION", Palette.RegistrationBlue);

        Ref(ui, "registrationOwner", Field(registration.transform, "OWNER", 20, 166, 330, 22));
        Ref(ui, "registrationPlate", Field(registration.transform, "PLATE", 20, 108, 330, 28));
        Ref(ui, "registrationModel", Field(registration.transform, "MODEL", 20, 50, 330, 22));
    }

    static void DocumentHeader(Transform document, string title, Color color)
    {
        Image header = Panel("Header", document, 0, 214, 370, 48, color);
        TextMeshProUGUI label = Label("Title", header.transform, title, 20, Palette.Paper, TextAlignmentOptions.Center, 10, 4, 350, 40, bold: true);
        AutoSize(label, 12);
    }

    static void BuildPlateCam(Transform parent, SerializedObject ui)
    {
        Image cam = Panel("PlateCam", parent, 830, 330, 390, 175, Palette.Ink);
        Label("Title", cam.transform, "PLATE CAM", 18, Palette.Warning, TextAlignmentOptions.Left, 16, 136, 360, 30, bold: true);
        Image plate = Panel("Plate", cam.transform, 60, 22, 270, 104, Palette.Plate);
        Panel("Stripe", plate.transform, 0, 86, 270, 18, Palette.RegistrationBlue);
        Ref(ui, "plateText", Label("Number", plate.transform, "- - -", 46, Palette.Ink, TextAlignmentOptions.Center, 0, 0, 270, 86, bold: true));
    }

    static void BuildButtons(Transform parent, SerializedObject ui)
    {
        Ref(ui, "approveButton", MakeButton("Approve", parent, 830, 246, 390, 56, Palette.Approve, "APPROVE", 26, out _));
        Ref(ui, "rejectButton", MakeButton("Reject", parent, 830, 180, 390, 56, Palette.Reject, "REJECT", 26, out _));
        Ref(ui, "imprisonButton", MakeButton("Imprison", parent, 830, 114, 390, 56, Palette.Imprison, "IMPRISON", 26, out _));

        Button bribe = MakeButton("Bribe", parent, 830, 40, 390, 56, Palette.Bribe, "TAKE BRIBE", 26, out TextMeshProUGUI bribeLabel);
        bribe.gameObject.SetActive(false); // only appears when a bribe is offered
        Ref(ui, "bribeButton", bribe);
        Ref(ui, "bribeLabel", bribeLabel);
    }

    static void BuildHud(Transform parent, SerializedObject ui)
    {
        Image hud = Panel("HUD", parent, 20, 988, 560, 72, Palette.WithAlpha(Palette.Ink, 0.8f));
        hud.raycastTarget = false;
        Ref(ui, "dayText", Label("Day", hud.transform, "DAY 1", 24, Palette.Paper, TextAlignmentOptions.Left, 16, 36, 530, 32, bold: true));
        Ref(ui, "pointsText", Label("Points", hud.transform, "POINTS 0", 22, Palette.Warning, TextAlignmentOptions.Left, 16, 6, 250, 30));
        Ref(ui, "moneyText", Label("Money", hud.transform, "SAVINGS $0", 22, Palette.Bribe, TextAlignmentOptions.Left, 280, 6, 270, 30));
    }

    static void BuildCitationSlip(Transform parent, SerializedObject ui)
    {
        Image slip = Panel("CitationSlip", parent, 830, 520, 420, 190, Palette.Paper);
        slip.raycastTarget = false;
        Ref(ui, "citationSlip", slip.gameObject);
        Image header = Panel("Header", slip.transform, 0, 150, 420, 40, Palette.Reject);
        Label("Title", header.transform, "CITATION", 22, Palette.Paper, TextAlignmentOptions.Center, 0, 0, 420, 40, bold: true);
        Ref(ui, "citationText", Label("Body", slip.transform, "", 20, Palette.PaperInk, TextAlignmentOptions.TopLeft, 16, 10, 388, 132));
        slip.gameObject.SetActive(false);
    }

    static void BuildDayBanner(Transform parent, SerializedObject ui)
    {
        RectTransform banner = Dim("DayBanner", parent, 0.92f);
        Ref(ui, "dayBanner", banner.gameObject);
        TextMeshProUGUI text = Label("Text", banner, "DAY 1", 96, Palette.Paper, TextAlignmentOptions.Center, 0, 0, 0, 0, bold: true);
        StretchFull(text.rectTransform);
        Ref(ui, "dayBannerText", text);
        banner.gameObject.SetActive(false);
    }

    // ---------- Pages ----------

    static TitleScreen BuildTitle(Transform parent)
    {
        RectTransform root = Dim("TitleScreen", parent, 0.82f);
        Label("Title", root, "DOCUMENTS, POR FAVOR!", 110, Palette.Warning, TextAlignmentOptions.Center, 0, 700, 1920, 150, bold: true);
        Label("Subtitle", root, "Three days at the border.", 36, Palette.Paper, TextAlignmentOptions.Center, 0, 630, 1920, 60);

        Image note = Panel("ContentNote", root, 510, 330, 900, 210, Palette.WithAlpha(Palette.Speech, 0.95f));
        Label("Heading", note.transform, "CONTENT NOTE", 22, Palette.Warning, TextAlignmentOptions.Center, 20, 160, 860, 36, bold: true);
        Label("Text", note.transform,
            "This game deals with immigration, human smuggling, detention and death. " +
            "Nothing graphic is ever shown, but some of it is heavy. The jokes are real too.",
            24, Palette.Paper, TextAlignmentOptions.Center, 40, 20, 820, 130);

        Button start = MakeButton("Start", root, 780, 170, 360, 84, Palette.Approve, "START SHIFT", 32, out _);

        TitleScreen title = parent.gameObject.AddComponent<TitleScreen>();
        var so = new SerializedObject(title);
        Ref(so, "root", root.gameObject);
        Ref(so, "startButton", start);
        so.ApplyModifiedPropertiesWithoutUndo();

        root.gameObject.SetActive(false);
        return title;
    }

    static TutorialGuide BuildTutorialGuide(Transform parent)
    {
        Image box = Panel("TutorialGuide", parent, 1180, 810, 720, 240, Palette.WithAlpha(Palette.Speech, 0.96f));

        // A tiny portrait of Primo José: hat, face, mustache.
        Image portrait = Panel("Portrait", box.transform, 20, 64, 120, 156, Palette.Glass);
        Panel("Shirt", portrait.transform, 14, 0, 92, 40, Palette.Reject);
        Panel("Face", portrait.transform, 32, 40, 56, 62, new Color32(0xC6, 0x9C, 0x72, 0xFF));
        Panel("Mustache", portrait.transform, 40, 56, 40, 10, Palette.Ink);
        Panel("Eyes", portrait.transform, 44, 80, 32, 6, Palette.Ink);
        Panel("HatBrim", portrait.transform, 10, 100, 100, 12, Palette.DeskWood);
        Panel("HatCrown", portrait.transform, 34, 112, 52, 30, Palette.DeskWood);

        Label("Name", box.transform, "PRIMO JOSÉ", 24, Palette.Warning, TextAlignmentOptions.Left, 160, 192, 540, 34, bold: true);
        TextMeshProUGUI line = AutoSize(Label("Line", box.transform, "", 24, Palette.Paper, TextAlignmentOptions.TopLeft, 160, 64, 540, 124), 16);
        Button ok = MakeButton("OK", box.transform, 560, 12, 140, 44, Palette.Approve, "OK", 22, out _);

        TutorialGuide guide = parent.gameObject.AddComponent<TutorialGuide>();
        var so = new SerializedObject(guide);
        Ref(so, "root", box.gameObject);
        Ref(so, "lineText", line);
        Ref(so, "okButton", ok);
        so.ApplyModifiedPropertiesWithoutUndo();

        box.gameObject.SetActive(false);
        return guide;
    }

    static LedgerScreen BuildLedger(Transform parent)
    {
        RectTransform root = Dim("Ledger", parent, 0.85f);
        Image sheet = Panel("Sheet", root, 260, 90, 1400, 900, Palette.Paper);
        Transform s = sheet.transform;

        TextMeshProUGUI title = Label("Title", s, "END OF DAY", 44, Palette.PaperInk, TextAlignmentOptions.Center, 40, 810, 1320, 64, bold: true);
        Panel("Rule", s, 40, 798, 1320, 3, Palette.PaperShadow);
        Panel("Divider", s, 712, 140, 3, 640, Palette.PaperShadow);

        Label("LogHeading", s, "TODAY AT THE BOOTH", 24, Palette.PaperInk, TextAlignmentOptions.Left, 40, 744, 640, 36, bold: true);
        TextMeshProUGUI log = AutoSize(Label("Log", s, "", 22, Palette.PaperInk, TextAlignmentOptions.TopLeft, 40, 140, 640, 590), 14);

        Label("LedgerHeading", s, "LEDGER", 24, Palette.PaperInk, TextAlignmentOptions.Left, 740, 744, 620, 36, bold: true);
        TextMeshProUGUI income = Label("Income", s, "", 24, Palette.PaperInk, TextAlignmentOptions.TopLeft, 740, 590, 620, 146);

        Button food = MakeButton("Food", s, 740, 520, 620, 52, Palette.Approve, "", 22, out TextMeshProUGUI foodLabel);
        Button medicine = MakeButton("Medicine", s, 740, 458, 620, 52, Palette.Approve, "", 22, out TextMeshProUGUI medicineLabel);
        Button nephew = MakeButton("Nephew", s, 740, 396, 620, 52, Palette.Approve, "", 22, out TextMeshProUGUI nephewLabel);
        foreach (TextMeshProUGUI label in new[] { foodLabel, medicineLabel, nephewLabel })
        {
            label.alignment = TextAlignmentOptions.Left;
            label.rectTransform.offsetMin = new Vector2(18, 0);
        }
        Label("Hint", s, "Click a bill to pay or skip it.", 18, Palette.WithAlpha(Palette.PaperInk, 0.6f), TextAlignmentOptions.Left, 740, 362, 620, 28);

        Label("FamilyHeading", s, "AT HOME", 24, Palette.PaperInk, TextAlignmentOptions.Left, 740, 312, 620, 36, bold: true);
        TextMeshProUGUI family = Label("Family", s, "", 22, Palette.PaperInk, TextAlignmentOptions.TopLeft, 740, 212, 620, 96);
        family.fontStyle = FontStyles.Italic;

        Panel("TotalRule", s, 740, 196, 620, 3, Palette.PaperShadow);
        TextMeshProUGUI total = Label("Total", s, "", 28, Palette.PaperInk, TextAlignmentOptions.Left, 740, 140, 620, 50, bold: true);

        Button goHome = MakeButton("GoHome", s, 520, 40, 360, 70, Palette.Imprison, "GO HOME", 28, out _);

        LedgerScreen ledger = parent.gameObject.AddComponent<LedgerScreen>();
        var so = new SerializedObject(ledger);
        Ref(so, "root", root.gameObject);
        Ref(so, "titleText", title);
        Ref(so, "logText", log);
        Ref(so, "incomeText", income);
        Ref(so, "familyText", family);
        Ref(so, "totalText", total);
        Ref(so, "foodButton", food);
        Ref(so, "foodLabel", foodLabel);
        Ref(so, "medicineButton", medicine);
        Ref(so, "medicineLabel", medicineLabel);
        Ref(so, "nephewButton", nephew);
        Ref(so, "nephewLabel", nephewLabel);
        Ref(so, "goHomeButton", goHome);
        so.ApplyModifiedPropertiesWithoutUndo();

        root.gameObject.SetActive(false);
        return ledger;
    }

    static NewspaperScreen BuildNewspaper(Transform parent)
    {
        RectTransform root = Dim("Newspaper", parent, 0.85f);
        Color newsprint = Color.Lerp(Palette.Paper, Color.white, 0.25f);
        Image sheet = Panel("Sheet", root, 360, 30, 1200, 1020, newsprint);
        Transform s = sheet.transform;

        Label("Masthead", s, "THE BORDER SENTINEL", 76, Palette.PaperInk, TextAlignmentOptions.Center, 40, 900, 1120, 100, bold: true);
        Label("Spanish", s, "EL CENTINELA DE LA FRONTERA", 22, Palette.WithAlpha(Palette.PaperInk, 0.7f), TextAlignmentOptions.Center, 40, 868, 1120, 32);
        Panel("RuleThick", s, 40, 858, 1120, 5, Palette.PaperInk);
        TextMeshProUGUI date = Label("Date", s, "", 20, Palette.PaperInk, TextAlignmentOptions.Center, 40, 822, 1120, 32);
        Panel("RuleThin", s, 40, 816, 1120, 2, Palette.PaperInk);

        TextMeshProUGUI mainHeadline = AutoSize(Label("MainHeadline", s, "", 54, Palette.PaperInk, TextAlignmentOptions.Center, 40, 680, 1120, 128, bold: true), 32);
        TextMeshProUGUI mainBody = AutoSize(Label("MainBody", s, "", 26, Palette.PaperInk, TextAlignmentOptions.Top, 80, 570, 1040, 104), 16);
        Panel("RuleMid", s, 40, 556, 1120, 2, Palette.PaperInk);

        var headlines = new List<Object>();
        var bodies = new List<Object>();
        for (int i = 0; i < 3; i++)
        {
            float x = 40 + i * 380;
            headlines.Add(AutoSize(Label($"SideHeadline{i}", s, "", 28, Palette.PaperInk, TextAlignmentOptions.TopLeft, x, 440, 360, 104, bold: true), 18));
            bodies.Add(AutoSize(Label($"SideBody{i}", s, "", 21, Palette.PaperInk, TextAlignmentOptions.TopLeft, x, 120, 360, 312), 14));
            if (i > 0) Panel("ColumnRule", s, x - 11, 120, 2, 424, Palette.PaperShadow);
        }

        Button next = MakeButton("Continue", s, 450, 28, 300, 64, Palette.Imprison, "NEXT DAY", 26, out TextMeshProUGUI nextLabel);

        NewspaperScreen paper = parent.gameObject.AddComponent<NewspaperScreen>();
        var so = new SerializedObject(paper);
        Ref(so, "root", root.gameObject);
        Ref(so, "dateText", date);
        Ref(so, "mainHeadline", mainHeadline);
        Ref(so, "mainBody", mainBody);
        RefArray(so, "sideHeadlines", headlines);
        RefArray(so, "sideBodies", bodies);
        Ref(so, "continueButton", next);
        Ref(so, "continueLabel", nextLabel);
        so.ApplyModifiedPropertiesWithoutUndo();

        root.gameObject.SetActive(false);
        return paper;
    }

    static EndingScreen BuildEnding(Transform parent)
    {
        RectTransform root = Dim("Ending", parent, 0.92f);
        Image sheet = Panel("Sheet", root, 360, 50, 1200, 980, Palette.Paper);
        Transform s = sheet.transform;

        TextMeshProUGUI title = Label("Title", s, "", 56, Palette.PaperInk, TextAlignmentOptions.Center, 40, 870, 1120, 80, bold: true);
        Panel("Rule", s, 40, 860, 1120, 3, Palette.PaperShadow);
        TextMeshProUGUI body = AutoSize(Label("Body", s, "", 24, Palette.PaperInk, TextAlignmentOptions.TopLeft, 60, 120, 1080, 724), 14);
        Button again = MakeButton("PlayAgain", s, 450, 36, 300, 64, Palette.Approve, "PLAY AGAIN", 26, out _);

        EndingScreen ending = parent.gameObject.AddComponent<EndingScreen>();
        var so = new SerializedObject(ending);
        Ref(so, "root", root.gameObject);
        Ref(so, "titleText", title);
        Ref(so, "bodyText", body);
        Ref(so, "playAgainButton", again);
        so.ApplyModifiedPropertiesWithoutUndo();

        root.gameObject.SetActive(false);
        return ending;
    }

    // ---------- Wiring ----------

    static void WireDirector(CheckpointStage stage, XRayPanel xray, DeskUI desk, TitleScreen title,
        TutorialGuide guide, LedgerScreen ledger, NewspaperScreen newspaper, EndingScreen ending)
    {
        GameObject host = stage.gameObject;

        foreach (StageTester tester in host.GetComponents<StageTester>())
            Object.DestroyImmediate(tester);

        GameDirector director = host.GetComponent<GameDirector>();
        if (director == null) director = host.AddComponent<GameDirector>();

        var so = new SerializedObject(director);
        Ref(so, "stage", stage);
        Ref(so, "xray", xray);
        Ref(so, "desk", desk);
        Ref(so, "title", title);
        Ref(so, "guide", guide);
        Ref(so, "ledger", ledger);
        Ref(so, "newspaper", newspaper);
        Ref(so, "ending", ending);

        SerializedProperty days = so.FindProperty("days");
        if (days.arraySize == 0)
        {
            days.arraySize = 1;
            days.GetArrayElementAtIndex(0).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DayData>("Assets/Data/Day1/Day1.asset");
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // The scene must be in Build Settings for "Play again" to reload it, and for the web build.
    static void AddSceneToBuild(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath)) return;
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.Where(s => s.path != scenePath).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        foreach (EditorBuildSettingsScene other in scenes.Skip(1)) other.enabled = false;
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ---------- Helpers ----------

    // A small grey caption with a bigger value underneath, like a form field.
    static TextMeshProUGUI Field(Transform parent, string caption, float x, float y, float width, float valueSize)
    {
        Label(caption, parent, caption, 14, Palette.WithAlpha(Palette.PaperInk, 0.6f), TextAlignmentOptions.Left, x, y + 26, width, 20, bold: true);
        TextMeshProUGUI value = Label(caption + " Value", parent, "", valueSize, Palette.PaperInk, TextAlignmentOptions.Left, x, y - 6, width, 32);
        return AutoSize(value, 12);
    }

    static TextMeshProUGUI AutoSize(TextMeshProUGUI label, float minSize)
    {
        label.enableAutoSizing = true;
        label.fontSizeMin = minSize;
        label.fontSizeMax = label.fontSize;
        return label;
    }

    static Button MakeButton(string name, Transform parent, float x, float y, float w, float h,
        Color color, string text, float textSize, out TextMeshProUGUI label)
    {
        Image image = Panel(name, parent, x, y, w, h, color);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
        button.colors = colors;

        label = Label("Label", image.transform, text, textSize, Palette.Paper, TextAlignmentOptions.Center, 0, 0, w, h, bold: true);
        StretchFull(label.rectTransform);
        return button;
    }

    static RectTransform Dim(string name, Transform parent, float alpha)
    {
        RectTransform rect = Stretch(name, parent);
        rect.gameObject.AddComponent<Image>().color = Palette.WithAlpha(Palette.Ink, alpha);
        return rect;
    }

    static Image Panel(string name, Transform parent, float x, float y, float w, float h, Color color)
    {
        RectTransform rect = NewRect(name, parent);
        Place(rect, x, y, w, h);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    static TextMeshProUGUI Label(string name, Transform parent, string text, float size, Color color,
        TextAlignmentOptions alignment, float x, float y, float w, float h, bool bold = false)
    {
        RectTransform rect = NewRect(name, parent);
        Place(rect, x, y, w, h);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        if (bold) label.fontStyle = FontStyles.Bold;
        return label;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    // Anchor and pivot at the parent's bottom-left, then position in pixels.
    static void Place(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);
    }

    static RectTransform Stretch(string name, Transform parent)
    {
        RectTransform rect = NewRect(name, parent);
        StretchFull(rect);
        return rect;
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void Ref(SerializedObject so, string field, Object value)
    {
        SerializedProperty property = so.FindProperty(field);
        if (property == null)
        {
            Debug.LogError($"Build Desk: {so.targetObject.GetType().Name} has no field '{field}'.");
            return;
        }
        property.objectReferenceValue = value;
    }

    static void RefArray(SerializedObject so, string field, List<Object> values)
    {
        SerializedProperty property = so.FindProperty(field);
        property.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    static void DestroyChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) Object.DestroyImmediate(child.gameObject);
    }

    static Canvas FindCanvas(string name)
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
            if (canvas.name == name) return canvas;
        return canvases.Length > 0 ? canvases[0] : null;
    }
}
