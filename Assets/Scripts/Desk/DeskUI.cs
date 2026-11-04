using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

// Everything on the desk except the X-ray: the booth window, the conversation,
// documents, plate cam, decision buttons, the NEXT megaphone, HUD and clock,
// rulebook, citation slip and day banner. (The ledger, newspaper and other
// full-screen pages live in Scripts/Screens.)
// Built by the menu "Documents Por Favor > Build Desk", which fills these slots.
//
// All talking happens in ONE place, the box at the left: travelers, and Primo
// José when he leans in ("Aside"). After José is done, the traveler's line comes back.
//
// Built in code at startup (not by the menu): the illustrated rulebook, the
// "under construction" cover on the plate cam, the green check / red X verdict
// stamp, and the keyboard shortcuts:
//     N next car   SPACE/ENTER next line   A approve   R reject   I imprison
//     T take bribe   TAB rulebook (ARROWS turn pages, ESC closes)
public class DeskUI : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] TMP_Text dayText;
    [SerializeField] TMP_Text pointsText;
    [SerializeField] TMP_Text moneyText;
    [SerializeField] TMP_Text clockText;

    [Header("Booth window (the face of whoever is talking)")]
    [SerializeField] Image windowFace;
    [SerializeField] float faceSlideSeconds = 0.3f;

    [Header("Documents")]
    [SerializeField] GameObject passport;
    [SerializeField] Image passportPhoto;
    [SerializeField] TMP_Text passportName;
    [SerializeField] TMP_Text passportNumber;
    [SerializeField] TMP_Text passportNationality;
    [SerializeField] TMP_Text passportExpiry;
    [SerializeField] GameObject permit;
    [SerializeField] TMP_Text permitName;
    [SerializeField] TMP_Text permitPurpose;
    [SerializeField] TMP_Text permitExpiry;
    [SerializeField] GameObject registration;
    [SerializeField] TMP_Text registrationOwner;
    [SerializeField] TMP_Text registrationPlate;
    [SerializeField] TMP_Text registrationModel;

    [Header("Plate cam")]
    [SerializeField] TMP_Text plateText;

    [Header("Dialogue")]
    [SerializeField] TMP_Text speakerText;
    [SerializeField] TMP_Text lineText;
    [SerializeField] Button nextLineButton;
    [SerializeField] TMP_Text nextLineLabel;

    [Header("Decisions")]
    [SerializeField] Button approveButton;
    [SerializeField] Button rejectButton;
    [SerializeField] Button imprisonButton;
    [SerializeField] Button bribeButton;
    [SerializeField] TMP_Text bribeLabel;

    [Header("NEXT megaphone")]
    [SerializeField] Button megaphoneButton;
    [SerializeField] GameObject megaphoneShout;

    [Header("Scanner (covered until the day it's installed)")]
    [SerializeField] GameObject scannerCover;
    [SerializeField] GameObject xrayHint;

    [Header("Rulebook")]
    [SerializeField] Button rulebookButton;
    [SerializeField] GameObject rulebookPage;
    [SerializeField] TMP_Text rulebookText; // the old text-only page; kept hidden (the illustrated Rulebook replaces it)
    [SerializeField] Button rulebookCloseButton;

    [Header("Citation slip")]
    [SerializeField] GameObject citationSlip;
    [SerializeField] TMP_Text citationText;
    [SerializeField] float citationSeconds = 4f;

    [Header("Day banner")]
    [SerializeField] GameObject dayBanner;
    [SerializeField] TMP_Text dayBannerText;

    public event Action<Decision> DecisionClicked;
    public event Action BribeClicked;

    readonly Queue<string> pendingLines = new Queue<string>();
    Coroutine citationRoutine;
    Coroutine faceRoutine;
    DayData today;
    bool nextCalled;

    // Who is at the window ("host"), and what they were last saying.
    string hostName;
    Face hostFace;
    string hostLine = "";

    // José leaning in.
    bool asideActive;
    bool asideAcknowledged;

    public bool AsideWaiting => asideActive;

    // Built in code (see the top of the file).
    Rulebook book;
    GameObject plateCover;
    Image verdictMark;
    TextMeshProUGUI verdictLabel;
    Coroutine verdictRoutine;

    void Awake()
    {
        approveButton.onClick.AddListener(() => Decide(Decision.Approve));
        rejectButton.onClick.AddListener(() => Decide(Decision.Reject));
        imprisonButton.onClick.AddListener(() => Decide(Decision.Imprison));
        bribeButton.onClick.AddListener(() => { Sfx.Play(Sound.Cash); BribeClicked?.Invoke(); });
        nextLineButton.onClick.AddListener(NextLine);
        megaphoneButton.onClick.AddListener(() => nextCalled = true);
        rulebookCloseButton.onClick.AddListener(() => rulebookPage.SetActive(false));

        // The old text-only rulebook page stays hidden; the illustrated book replaces it.
        Transform overlay = rulebookPage.transform.parent;
        book = Rulebook.Create(overlay);
        book.transform.SetSiblingIndex(rulebookPage.transform.GetSiblingIndex() + 1);
        rulebookButton.onClick.AddListener(book.Toggle);

        BuildPlateCover();
        BuildVerdict(overlay);
        AddKeyHint(approveButton, "A");
        AddKeyHint(rejectButton, "R");
        AddKeyHint(imprisonButton, "I");
        AddKeyHint(bribeButton, "T");
        AddKeyHint(megaphoneButton, "N");
        // Keep the NEXT words clear of the little megaphone drawn on the left of the button.
        TMP_Text megaphoneLabel = megaphoneButton.GetComponentInChildren<TMP_Text>(true);
        if (megaphoneLabel != null) megaphoneLabel.rectTransform.offsetMin = new Vector2(80f, 0f);
        AddKeyHint(rulebookButton, "TAB");

        ClearTraveler();
        SetDecisionsEnabled(false);
        citationSlip.SetActive(false);
        clockText.enableAutoSizing = true; // room for "CLOSED" at 17:00
        clockText.fontSizeMin = 14f;
        clockText.fontSizeMax = clockText.fontSize;
        // A slip with a long "why" shrinks its text to fit instead of spilling off the paper.
        citationText.enableAutoSizing = true;
        citationText.fontSizeMin = 11f;
        citationText.fontSizeMax = citationText.fontSize;
        dayBanner.SetActive(false);
        megaphoneButton.gameObject.SetActive(false);
        megaphoneShout.SetActive(false);
        rulebookPage.SetActive(false);
    }

    void Decide(Decision decision)
    {
        Sfx.PlayVaried(Sound.Stamp, 1f, 0.05f);
        DecisionClicked?.Invoke(decision);
    }

    // ---------- HUD ----------

    // Just the day and your record. Nobody tells you where the line is, and money
    // only comes up at night, when the bills do.
    public void SetHud(int dayNumber, string date, int record)
    {
        dayText.text = $"DAY {dayNumber}  ·  {date}";
        string color = record <= Scoring.FiredAt / 2 ? "#A8503F" : "#E8C25A"; // turns red as it gets bad
        pointsText.text = $"RECORD <color={color}>{record}</color>";
        moneyText.gameObject.SetActive(false);
    }

    // Late in the shift the clock turns red.
    public void SetClock(string time, bool late) =>
        clockText.text = late ? $"<color=#A8503F>{time}</color>" : time;

    // ---------- Today's rules ----------

    // Turns on today's equipment and writes today's rulebook page.
    public void SetRulesForDay(DayData day)
    {
        today = day;
        imprisonButton.gameObject.SetActive(day.xrayScanner);
        scannerCover.SetActive(!day.xrayScanner);
        xrayHint.SetActive(day.xrayScanner);
        book.SetDay(day);
        // The plate cam only matters once registrations are checked.
        plateCover.SetActive(!day.registrationRequired);
    }

    // The rulebook's X-ray page shows real contraband from these days.
    public void UseExamplesFrom(IEnumerable<DayData> days) => book.UseExamplesFrom(days);

    // Before the shift: the rulebook opens on today's new pages. Waits for BEGIN SHIFT.
    public IEnumerator Briefing() => book.Brief();

    // ---------- Keyboard ----------

    void Update()
    {
        // A clicked button stays "selected", and SPACE/ENTER would press it again. Don't let it.
        EventSystem events = EventSystem.current;
        if (events != null && events.currentSelectedGameObject != null && events.currentSelectedGameObject.GetComponent<Button>() != null)
            events.SetSelectedGameObject(null);

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.tabKey.wasPressedThisFrame && rulebookButton.gameObject.activeInHierarchy)
            book.Toggle();
        if (book.IsOpen && !book.InBriefing) // the briefing reads its own keys
        {
            if (keyboard.escapeKey.wasPressedThisFrame) book.Close();
            if (keyboard.rightArrowKey.wasPressedThisFrame) book.Turn(1);
            if (keyboard.leftArrowKey.wasPressedThisFrame) book.Turn(-1);
        }

        Press(keyboard.nKey, megaphoneButton);
        Press(keyboard.aKey, approveButton);
        Press(keyboard.rKey, rejectButton);
        Press(keyboard.iKey, imprisonButton);
        Press(keyboard.tKey, bribeButton);
        Press(keyboard.spaceKey, nextLineButton);
        Press(keyboard.enterKey, nextLineButton);
        Press(keyboard.numpadEnterKey, nextLineButton);
    }

    // Same as clicking it, but only if it could be clicked right now.
    static void Press(KeyControl key, Button button)
    {
        if (key.wasPressedThisFrame && button.gameObject.activeInHierarchy && button.interactable)
            button.onClick.Invoke();
    }

    // "APPROVE" becomes "APPROVE [A]".
    static void AddKeyHint(Button button, string key)
    {
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null) return;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12f;
        label.fontSizeMax = label.fontSize;
        label.text = $"{label.text} <size=60%><alpha=#B0>[{key}]</size>";
    }

    // ---------- Plate cam cover ----------

    void BuildPlateCover()
    {
        Transform cam = plateText.transform.parent.parent; // Number > Plate > PlateCam
        RectTransform cover = UiKit.Stretch("UnderConstruction", cam);
        cover.gameObject.AddComponent<Image>().color = Palette.Ink;
        for (int i = 0; i < 4; i++)
        {
            Image stripe = UiKit.Panel("Stripe", cover, 0, 14 + i * 42, 390, 10, Palette.WithAlpha(Palette.Warning, 0.14f));
            stripe.raycastTarget = false;
        }
        UiKit.Label("Text", cover, "PLATE CAM\n<size=55%>UNDER CONSTRUCTION</size>", 34, Palette.Warning,
            TextAlignmentOptions.Center, 10, 20, 370, 135, bold: true);
        plateCover = cover.gameObject;
    }

    // ---------- Angry drivers ----------

    // A speech bubble that rides above a car in the world for a couple of seconds.
    public void Shout(Transform car, string line)
    {
        Transform overlay = rulebookPage.transform.parent;
        Image bubble = UiKit.Panel("Shout", overlay, 0, 0, 300, 54, Palette.Paper);
        bubble.raycastTarget = false;
        bubble.rectTransform.pivot = new Vector2(0.5f, 0f);
        bubble.transform.SetAsFirstSibling(); // behind the desk pages
        UiKit.Label("Text", bubble.transform, line, 22, Palette.Reject, TextAlignmentOptions.Center, 8, 4, 284, 46, bold: true);
        bubble.gameObject.AddComponent<WorldAnchor>().Follow(car, new Vector3(0f, 1.4f, 0f));
        StartCoroutine(FadeAndDestroy(bubble, 2.2f));
    }

    static IEnumerator FadeAndDestroy(Image bubble, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        CanvasGroup group = bubble.gameObject.AddComponent<CanvasGroup>();
        for (float e = 0f; e < 0.4f && bubble != null; e += Time.deltaTime)
        {
            group.alpha = 1f - e / 0.4f;
            yield return null;
        }
        if (bubble != null) Destroy(bubble.gameObject);
    }

    // ---------- Verdict stamp ----------

    void BuildVerdict(Transform overlay)
    {
        verdictMark = UiKit.Picture("Verdict", overlay, PixelArt.Check(), 300, 60, 240, 240);
        verdictMark.transform.SetAsFirstSibling(); // under pages and popups, over the desk
        verdictLabel = UiKit.Label("Label", verdictMark.transform, "", 34, Palette.Approve, TextAlignmentOptions.Center, -80, -44, 400, 44, bold: true);
        verdictMark.gameObject.SetActive(false);
    }

    // A big green check (right call, no citations) or a red X stamped over the papers.
    // unpaid: decided after 17:00, so even a right call earns nothing.
    public void ShowVerdict(bool good, bool unpaid = false)
    {
        verdictMark.sprite = good ? PixelArt.Check() : PixelArt.Cross();
        verdictLabel.text = (good ? "CORRECT" : "CITATION") + (unpaid ? "\n<size=60%>AFTER 17:00 · UNPAID</size>" : "");
        verdictLabel.color = good ? Palette.Approve : Palette.Reject;
        Sfx.Play(good ? Sound.Cash : Sound.Wrong, 0.8f);
        if (verdictRoutine != null) StopCoroutine(verdictRoutine);
        verdictRoutine = StartCoroutine(VerdictPop());
    }

    IEnumerator VerdictPop()
    {
        GameObject go = verdictMark.gameObject;
        Transform t = go.transform;
        go.SetActive(true);
        float angle = UnityEngine.Random.Range(-10f, 10f);
        for (float e = 0f; e < 0.18f; e += Time.deltaTime)
        {
            float k = e / 0.18f;
            t.localScale = Vector3.one * Mathf.Lerp(1.8f, 1f, k * k);
            t.localEulerAngles = new Vector3(0, 0, angle * k);
            SetVerdictAlpha(k);
            yield return null;
        }
        t.localScale = Vector3.one;
        SetVerdictAlpha(1f);
        yield return new WaitForSeconds(1.4f);
        for (float e = 0f; e < 0.4f; e += Time.deltaTime)
        {
            SetVerdictAlpha(1f - e / 0.4f);
            yield return null;
        }
        go.SetActive(false);
    }

    void SetVerdictAlpha(float a)
    {
        verdictMark.color = new Color(1f, 1f, 1f, a);
        verdictLabel.alpha = a;
    }

    // ---------- The traveler ----------

    public void ShowTraveler(TravelerData traveler, List<string> boothLines)
    {
        asideActive = false; // whoever steps up to the window takes over the conversation
        Sfx.Play(Sound.Paper);

        passport.SetActive(true);
        passportPhoto.sprite = PortraitArt.Portrait(traveler.PassportPhoto);
        passportName.text = traveler.passportName;
        passportNumber.text = traveler.passportNumber;
        passportNationality.text = traveler.nationality;
        passportExpiry.text = traveler.passportExpiry;

        // Documents only appear on the days they're checked. A missing permit is itself the clue.
        bool showPermit = today != null && today.permitsRequired && traveler.hasPermit;
        permit.SetActive(showPermit);
        permitName.text = traveler.permitName;
        permitPurpose.text = traveler.permitPurpose;
        permitExpiry.text = traveler.permitExpiry;

        bool showRegistration = today != null && today.registrationRequired;
        registration.SetActive(showRegistration);
        registrationOwner.text = traveler.registrationOwner;
        registrationPlate.text = traveler.registrationPlate;
        registrationModel.text = traveler.vehicleModel != null ? traveler.vehicleModel.name : "-";

        plateText.text = traveler.vehiclePlate;

        hostName = traveler.Speaker;
        hostFace = traveler.face;
        hostLine = "";
        ShowSpeaker(hostName, hostFace);

        pendingLines.Clear();
        foreach (string line in boothLines)
            pendingLines.Enqueue(line);
        Typewriter.On(lineText).Type("", null); // also stops a line that was still typing
        NextLine();
    }

    public void NextLine()
    {
        Typewriter typewriter = Typewriter.On(lineText);
        if (typewriter.IsTyping)
        {
            typewriter.Finish(); // first click shows the whole line, the next one moves on
            return;
        }

        if (asideActive)
        {
            asideAcknowledged = true;
            EndAside();
            return;
        }

        if (pendingLines.Count == 0) return;
        hostLine = pendingLines.Dequeue();
        typewriter.Type(hostLine, hostName);
        RefreshNextButton();
    }

    // Interrupts the conversation with one specific line (bribe offer, goodbye...).
    public void Say(string line)
    {
        if (asideActive) EndAside();
        pendingLines.Clear();
        hostLine = line;
        Typewriter.On(lineText).Type(line, hostName);
        RefreshNextButton();
    }

    public void ClearTraveler()
    {
        passport.SetActive(false);
        permit.SetActive(false);
        registration.SetActive(false);
        plateText.text = "- - -";
        hostName = null;
        hostFace = null;
        hostLine = "";
        pendingLines.Clear();
        if (!asideActive)
        {
            speakerText.text = "";
            Typewriter.On(lineText).Type("", null);
            HideFace();
        }
        RefreshNextButton();
    }

    // ---------- José leans in ----------

    // Someone else talks in the box for a moment. Stays until the player clicks OK.
    public void Aside(string speaker, Face face, string line)
    {
        asideActive = true;
        asideAcknowledged = false;
        ShowSpeaker(speaker, face);
        Typewriter.On(lineText).Type(line, speaker);
        RefreshNextButton();
    }

    public IEnumerator AsideAndWait(string speaker, Face face, string line)
    {
        Aside(speaker, face, line);
        while (!asideAcknowledged)
            yield return null;
    }

    // Back to whoever is at the window (or an empty window).
    public void EndAside()
    {
        if (!asideActive) return;
        asideActive = false;
        if (hostName != null)
        {
            ShowSpeaker(hostName, hostFace);
            Typewriter.On(lineText).Show(hostLine); // their line is just there again, no retyping
        }
        else
        {
            speakerText.text = "";
            Typewriter.On(lineText).Type("", null);
            HideFace();
        }
        RefreshNextButton();
    }

    void RefreshNextButton()
    {
        bool show = asideActive || pendingLines.Count > 0;
        nextLineButton.gameObject.SetActive(show);
        nextLineLabel.text = asideActive ? "OK" : ">>";
    }

    // ---------- The booth window ----------

    void ShowSpeaker(string speaker, Face face)
    {
        speakerText.text = speaker;
        Sprite sprite = PortraitArt.Portrait(face);
        if (windowFace.sprite == sprite && windowFace.enabled) return;

        windowFace.sprite = sprite;
        windowFace.enabled = sprite != null;
        if (faceRoutine != null) StopCoroutine(faceRoutine);
        if (isActiveAndEnabled) faceRoutine = StartCoroutine(SlideFaceUp());
    }

    void HideFace()
    {
        if (faceRoutine != null) StopCoroutine(faceRoutine);
        windowFace.enabled = false;
        windowFace.sprite = null;
    }

    // They step up to the glass: the face rises into the window frame.
    IEnumerator SlideFaceUp()
    {
        RectTransform rect = windowFace.rectTransform;
        float height = rect.rect.height;
        float elapsed = 0f;
        while (elapsed < faceSlideSeconds)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, elapsed / faceSlideSeconds);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, Mathf.Lerp(-height * 0.6f, 0f, k));
            yield return null;
        }
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 0f);
    }

    // ---------- Buttons ----------

    public void SetDecisionsEnabled(bool enabled)
    {
        approveButton.interactable = enabled;
        rejectButton.interactable = enabled;
        imprisonButton.interactable = enabled;
        if (!enabled) bribeButton.gameObject.SetActive(false);
    }

    public void OfferBribe(TravelerData traveler)
    {
        bribeLabel.text = $"TAKE ${traveler.bribeAmount}";
        bribeButton.gameObject.SetActive(true);
        Say(traveler.bribeLine);
    }

    // The megaphone waits for a click, then shouts "¡Siguiente!" at the line.
    // Stops waiting (without shouting) if giveUp() becomes true, e.g. the shift ended.
    public IEnumerator WaitForNext(Func<bool> giveUp)
    {
        nextCalled = false;
        megaphoneButton.gameObject.SetActive(true);
        Transform icon = megaphoneButton.transform;
        float t = 0f;
        while (!nextCalled)
        {
            if (giveUp()) { megaphoneButton.gameObject.SetActive(false); yield break; }
            t += Time.deltaTime;
            icon.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(t * 5f)); // a gentle "click me" pulse
            yield return null;
        }
        icon.localScale = Vector3.one;
        megaphoneButton.gameObject.SetActive(false);

        Sfx.Play(Sound.Megaphone);
        StartCoroutine(ShowFor(megaphoneShout, 1.3f));
    }

    // ---------- Slips and banners ----------

    public void ShowCitations(List<Citation> citations)
    {
        if (citations.Count == 0) return;

        var text = new StringBuilder();
        foreach (Citation citation in citations)
        {
            text.AppendLine($"<b>{citation.reason}   {citation.points}</b>");
            if (!string.IsNullOrEmpty(citation.why))
                text.AppendLine($"<size=75%>{citation.why}</size>"); // what you should have seen
        }
        citationText.text = text.ToString();
        Sfx.Play(Sound.Printer);

        // Longer slips stay up longer: about two extra seconds per citation.
        float seconds = Mathf.Max(citationSeconds, 4f + 2f * citations.Count);
        if (citationRoutine != null) StopCoroutine(citationRoutine);
        citationRoutine = StartCoroutine(ShowFor(citationSlip, seconds));
    }

    public IEnumerator ShowDayBanner(string text, float seconds)
    {
        dayBannerText.text = text;
        Sfx.Play(Sound.Bell);
        yield return ShowFor(dayBanner, seconds);
    }

    static IEnumerator ShowFor(GameObject target, float seconds)
    {
        target.SetActive(true);
        yield return new WaitForSeconds(seconds);
        target.SetActive(false);
    }
}
