using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Everything on the desk except the X-ray: documents, plate cam, dialogue,
// decision buttons, HUD, citation slip and day banner. (The ledger, newspaper
// and other full-screen pages live in Scripts/Screens.)
// Built by the menu "Documents Por Favor > Build Desk", which fills these slots.
public class DeskUI : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] TMP_Text dayText;
    [SerializeField] TMP_Text pointsText;
    [SerializeField] TMP_Text moneyText;

    [Header("Documents")]
    [SerializeField] GameObject passport;
    [SerializeField] TMP_Text passportName;
    [SerializeField] TMP_Text passportNumber;
    [SerializeField] TMP_Text passportExpiry;
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

    [Header("Decisions")]
    [SerializeField] Button approveButton;
    [SerializeField] Button rejectButton;
    [SerializeField] Button imprisonButton;
    [SerializeField] Button bribeButton;
    [SerializeField] TMP_Text bribeLabel;

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

    void Awake()
    {
        approveButton.onClick.AddListener(() => DecisionClicked?.Invoke(Decision.Approve));
        rejectButton.onClick.AddListener(() => DecisionClicked?.Invoke(Decision.Reject));
        imprisonButton.onClick.AddListener(() => DecisionClicked?.Invoke(Decision.Imprison));
        bribeButton.onClick.AddListener(() => BribeClicked?.Invoke());
        nextLineButton.onClick.AddListener(NextLine);

        ClearTraveler();
        SetDecisionsEnabled(false);
        citationSlip.SetActive(false);
        dayBanner.SetActive(false);
    }

    public void SetHud(int dayNumber, string date, int points, int money)
    {
        dayText.text = $"DAY {dayNumber}  ·  {date}";
        pointsText.text = $"POINTS {points}";
        moneyText.text = $"SAVINGS {Money.Format(money)}";
    }

    public void ShowTraveler(TravelerData traveler, List<string> boothLines)
    {
        passport.SetActive(true);
        passportName.text = traveler.passportName;
        passportNumber.text = traveler.passportNumber;
        passportExpiry.text = traveler.passportExpiry;

        registration.SetActive(true);
        registrationOwner.text = traveler.registrationOwner;
        registrationPlate.text = traveler.registrationPlate;
        registrationModel.text = traveler.vehicleModel != null ? traveler.vehicleModel.name : "-";

        plateText.text = traveler.vehiclePlate;

        speakerText.text = traveler.passportName;
        pendingLines.Clear();
        foreach (string line in boothLines)
            pendingLines.Enqueue(line);
        lineText.text = "";
        NextLine();
    }

    public void NextLine()
    {
        if (pendingLines.Count == 0) return;
        lineText.text = pendingLines.Dequeue();
        nextLineButton.gameObject.SetActive(pendingLines.Count > 0);
    }

    // Interrupts the conversation with one specific line (bribe offer, goodbye...).
    public void Say(string line)
    {
        pendingLines.Clear();
        lineText.text = line;
        nextLineButton.gameObject.SetActive(false);
    }

    public void ClearTraveler()
    {
        passport.SetActive(false);
        registration.SetActive(false);
        plateText.text = "- - -";
        speakerText.text = "";
        lineText.text = "";
        pendingLines.Clear();
        nextLineButton.gameObject.SetActive(false);
    }

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

    public void ShowCitations(List<Citation> citations)
    {
        if (citations.Count == 0) return;

        var text = new StringBuilder();
        foreach (Citation citation in citations)
            text.AppendLine($"{citation.reason}   <b>{citation.points}</b>");
        citationText.text = text.ToString();

        if (citationRoutine != null) StopCoroutine(citationRoutine);
        citationRoutine = StartCoroutine(ShowFor(citationSlip, citationSeconds));
    }

    public IEnumerator ShowDayBanner(string text, float seconds)
    {
        dayBannerText.text = text;
        yield return ShowFor(dayBanner, seconds);
    }

    static IEnumerator ShowFor(GameObject target, float seconds)
    {
        target.SetActive(true);
        yield return new WaitForSeconds(seconds);
        target.SetActive(false);
    }
}
