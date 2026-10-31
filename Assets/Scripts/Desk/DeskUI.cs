using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Everything on the desk except the X-ray: the booth window, the conversation,
// documents, plate cam, decision buttons, the NEXT megaphone, HUD and clock,
// rulebook, citation slip and day banner. (The ledger, newspaper and other
// full-screen pages live in Scripts/Screens.)
// Built by the menu "Documents Por Favor > Build Desk", which fills these slots.
//
// All talking happens in ONE place, the box at the left: travelers, and Primo
// José when he leans in ("Aside"). After José is done, the traveler's line comes back.
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
    [SerializeField] TMP_Text rulebookText;
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

    void Awake()
    {
        approveButton.onClick.AddListener(() => Decide(Decision.Approve));
        rejectButton.onClick.AddListener(() => Decide(Decision.Reject));
        imprisonButton.onClick.AddListener(() => Decide(Decision.Imprison));
        bribeButton.onClick.AddListener(() => { Sfx.Play(Sound.Cash); BribeClicked?.Invoke(); });
        nextLineButton.onClick.AddListener(NextLine);
        megaphoneButton.onClick.AddListener(() => nextCalled = true);
        rulebookButton.onClick.AddListener(() => { Sfx.Play(Sound.Paper); rulebookPage.SetActive(!rulebookPage.activeSelf); });
        rulebookCloseButton.onClick.AddListener(() => rulebookPage.SetActive(false));

        ClearTraveler();
        SetDecisionsEnabled(false);
        citationSlip.SetActive(false);
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

    public void SetHud(int dayNumber, string date, int record, int money)
    {
        dayText.text = $"DAY {dayNumber}  ·  {date}";
        string color = record <= Scoring.FiredAt / 2 ? "#A8503F" : "#E8C25A"; // red once you're halfway to fired
        pointsText.text = $"RECORD <color={color}>{record}</color> <size=70%>/ {Scoring.FiredAt} = FIRED</size>";
        moneyText.text = $"SAVINGS {Money.Format(money)}";
    }

    public void SetClock(string time) => clockText.text = time;

    // ---------- Today's rules ----------

    // Turns on today's equipment and writes today's rulebook page.
    public void SetRulesForDay(DayData day)
    {
        today = day;
        imprisonButton.gameObject.SetActive(day.xrayScanner);
        scannerCover.SetActive(!day.xrayScanner);
        xrayHint.SetActive(day.xrayScanner);
        rulebookText.text = RulebookText(day);
    }

    static string RulebookText(DayData day)
    {
        var text = new StringBuilder();
        text.AppendLine("<b>PASSPORT</b> (everyone)");
        text.AppendLine("  · Not expired. Compare with today's date.");
        text.AppendLine("  · The photo must be the person at your window.");
        if (day.permitsRequired)
        {
            text.AppendLine("<b>ENTRY PERMIT</b> (everyone who is not a US citizen)");
            text.AppendLine("  · Must have one. Name must match the passport.");
            text.AppendLine("  · Not expired.");
        }
        if (day.registrationRequired)
        {
            text.AppendLine("<b>VEHICLE REGISTRATION</b>");
            text.AppendLine("  · Owner must match the passport.");
            text.AppendLine("  · Plate must match the PLATE CAM.");
        }
        if (day.xrayScanner)
        {
            text.AppendLine("<b>X-RAY</b>");
            text.AppendLine("  · Click anything hidden that isn't part of the car.");
            text.AppendLine("  · Hidden goods or hidden people: IMPRISON.");
        }
        text.AppendLine();
        text.AppendLine("Anything wrong with the papers: <b>REJECT</b>.  All good: <b>APPROVE</b>.");
        return text.ToString();
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
            text.AppendLine($"{citation.reason}   <b>{citation.points}</b>");
        citationText.text = text.ToString();
        Sfx.Play(Sound.Printer);

        if (citationRoutine != null) StopCoroutine(citationRoutine);
        citationRoutine = StartCoroutine(ShowFor(citationSlip, citationSeconds));
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
