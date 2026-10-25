using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// Runs the whole game: title, then each day (cars, ledger, newspaper), then the ending.
// Reads like the game's script: each step waits for the one before it.
public class GameDirector : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] CheckpointStage stage;
    [SerializeField] XRayPanel xray;
    [SerializeField] DeskUI desk;
    [SerializeField] TitleScreen title;
    [SerializeField] TutorialGuide guide;
    [SerializeField] LedgerScreen ledger;
    [SerializeField] NewspaperScreen newspaper;
    [SerializeField] EndingScreen ending;

    [Header("Days")]
    [Tooltip("The days in order. Add Day2, Day3... here.")]
    [SerializeField] List<DayData> days = new List<DayData>();
    [SerializeField] float dayBannerSeconds = 2.5f;

    [Header("Money")]
    [SerializeField] int startingSavings = 20;
    [SerializeField] int wagePerCorrectDecision = 15;
    [SerializeField] int rent = 25;
    [SerializeField] int food = 10;
    [SerializeField] int medicine = 15;
    [SerializeField] int nephewCosts = 10;
    [Tooltip("Dollars docked from your final pay for every point below zero.")]
    [SerializeField] int dollarsPerPoint = 5;

    class PendingStory
    {
        public int day;
        public NewsStory story;
    }

    readonly List<DecisionRecord> history = new List<DecisionRecord>();
    readonly List<PendingStory> pendingNews = new List<PendingStory>();
    Household household;
    DayData currentDay;
    int dayPoints;
    int totalPoints;
    int bribesToday;

    Decision? decision;
    bool bribeTaken;
    bool tutorialClicks; // José comments on wrong clicks during the first traveler

    void OnEnable()
    {
        desk.DecisionClicked += OnDecisionClicked;
        desk.BribeClicked += OnBribeClicked;
        xray.WrongClicked += OnWrongClick;
    }

    void OnDisable()
    {
        desk.DecisionClicked -= OnDecisionClicked;
        desk.BribeClicked -= OnBribeClicked;
        xray.WrongClicked -= OnWrongClick;
    }

    IEnumerator Start()
    {
        household = new Household(startingSavings);
        yield return title.Show();

        for (int i = 0; i < days.Count; i++)
        {
            bool lastDay = i == days.Count - 1;
            yield return RunDay(days[i], tutorial: i == 0);
            yield return EndOfDay(days[i]);
            yield return newspaper.Show($"EVENING EDITION  ·  {days[i].todaysDate}  ·  25¢",
                Edition(days[i].dayNumber, lastDay), lastDay ? "CONTINUE" : "NEXT DAY");
        }

        ending.Show("YOUR LAST SHIFT", EndingText());
    }

    // ---------- A day at the booth ----------

    IEnumerator RunDay(DayData day, bool tutorial)
    {
        currentDay = day;
        dayPoints = 0;
        bribesToday = 0;
        RefreshHud();

        yield return desk.ShowDayBanner($"DAY {day.dayNumber}\n<size=50%>{day.todaysDate}</size>", dayBannerSeconds);

        if (tutorial)
        {
            yield return guide.SayAndWait("¡Primo! Your first day! I pulled a lot of strings to get you this job, so don't make me look bad, eh?");
            yield return guide.SayAndWait("Rent is due, Abuela needs her medicine, and your nephew needs shoes. Every mistake is a citation, and citations come out of your pay. So... no mistakes.");
            guide.Say("Here comes your first one. Every car goes through the scanner first.");
        }

        stage.LineUp(day.travelers);
        for (int i = 0; i < day.travelers.Count; i++)
            yield return RunTraveler(day.travelers[i], tutorial && i == 0);
    }

    IEnumerator RunTraveler(TravelerData traveler, bool tutorial)
    {
        tutorialClicks = tutorial;
        yield return stage.PullUpToScanner();

        if (tutorial)
            guide.Say("See the X-ray monitor, bottom right? It develops while they walk in. Top picture is the car from the side, bottom is from above.");

        // The driver walks in while the car is scanned. They start talking
        // when they reach the booth; you can decide once the scan is done.
        Coroutine scan = StartCoroutine(stage.ScanCar());
        yield return stage.DriverWalksToBooth();
        desk.ShowTraveler(traveler);
        yield return scan;

        if (tutorial)
        {
            yield return guide.SayAndWait("If something is hiding in a car, a person, a package, anything that isn't part of a car, you CLICK on it in the X-ray.");
            yield return guide.SayAndWait("But listen: every click on normal car stuff, an engine, a seat, a spare tire, is ONE POINT off your record. The jefe hates wasted time. Only click what doesn't belong.");
            yield return guide.SayAndWait("This one? Looks like an abuela and a lot of tamales. Nothing to click. Now the papers: the NAME on the passport must match the OWNER on the registration, the PLATE must match the plate cam, and the passport can't be expired. Today's date is top left.");
            guide.Say("Clean car, clean papers: APPROVE. Bad papers: REJECT. Something hidden in the car: IMPRISON. Your call, primo.");
        }

        decision = null;
        bribeTaken = false;
        bool bribeOffered = false;
        desk.SetDecisionsEnabled(true);

        while (decision == null)
        {
            // Smugglers get nervous once you've spotted something on the X-ray.
            if (!bribeOffered && traveler.OffersBribe && xray.FoundCount > 0)
            {
                bribeOffered = true;
                desk.OfferBribe(traveler);
            }
            yield return null;
        }

        desk.SetDecisionsEnabled(false);
        Decision finalDecision = decision.Value;
        bool correct = finalDecision == Rules.CorrectDecision(traveler, currentDay.todaysDate);

        List<Citation> citations = Scoring.CitationsFor(traveler, currentDay.todaysDate, finalDecision, xray.FoundCount);
        foreach (Citation citation in citations)
            AddPoints(citation.points);

        if (bribeTaken)
        {
            bribesToday += traveler.bribeAmount;
            RefreshHud();
        }

        history.Add(new DecisionRecord(currentDay.dayNumber, traveler, finalDecision, correct, bribeTaken, citations, xray.WrongClicks));
        foreach (NewsStory story in traveler.NewsFor(finalDecision))
            pendingNews.Add(new PendingStory { day = currentDay.dayNumber + story.daysLater, story = story });

        if (tutorial)
        {
            tutorialClicks = false;
            guide.Hide();
        }
        desk.Say(FarewellLine(traveler, finalDecision));
        desk.ShowCitations(citations);
        yield return stage.SendAway(finalDecision);
        desk.ClearTraveler();

        if (tutorial)
        {
            yield return guide.SayAndWait(correct
                ? "¡Eso! See? Easy. I gotta get back to my own booth. You got this, primo. And hey... Abuela's counting on you."
                : "Ay, primo... That's coming out of your pay. Okay, I gotta get back to my own booth. Be careful, eh? Abuela's counting on you.");
            guide.Hide();
        }
    }

    // ---------- End of the day ----------

    IEnumerator EndOfDay(DayData day)
    {
        List<DecisionRecord> today = history.Where(r => r.day == day.dayNumber).ToList();
        int correctCount = today.Count(r => r.correct);
        int salary = correctCount * wagePerCorrectDecision;
        int totalBeforeBills = household.savings + salary + bribesToday - rent;

        string income =
            $"Savings<pos=72%>{Money.Format(household.savings)}\n" +
            $"Salary ({correctCount} correct)<pos=72%>+${salary}\n" +
            $"Bribes<pos=72%>+${bribesToday}\n" +
            $"Rent<pos=72%>-${rent}";

        yield return ledger.Show($"END OF DAY {day.dayNumber}", DayLog(today), income, totalBeforeBills,
            food, medicine, nephewCosts, household.Report());

        household.savings = ledger.TotalAfterBills;
        household.RecordBills(ledger.PayFood, ledger.PayMedicine, ledger.PayNephew);
        bribesToday = 0;
        RefreshHud();
    }

    string DayLog(List<DecisionRecord> today)
    {
        var text = new StringBuilder();
        foreach (DecisionRecord record in today)
        {
            text.AppendLine($"<b>{record.traveler.passportName}</b>");
            text.AppendLine($"    {Outcome(record)}");
            if (record.wrongClicks > 0)
                text.AppendLine($"    Wasted the boss's time x{record.wrongClicks}<pos=85%>{record.wrongClicks * Scoring.WrongClick}");
            foreach (Citation citation in record.citations)
                text.AppendLine($"    {citation.reason}<pos=85%>{citation.points}");
        }
        text.AppendLine();
        text.AppendLine($"<b>Points today: {dayPoints}</b>     Total: {totalPoints}");
        return text.ToString();
    }

    // Tonight's paper: stories due today (plus everything still waiting, if this is
    // the last paper), front-page stories first, then a word about you, then filler.
    List<NewsStory> Edition(int dayNumber, bool finalEdition)
    {
        List<NewsStory> stories = pendingNews
            .Where(p => finalEdition ? p.day >= dayNumber : p.day == dayNumber)
            .Select(p => p.story)
            .OrderByDescending(s => s.frontPage)
            .ToList();

        stories.Add(dayPoints == 0
            ? new NewsStory
            {
                headline = "Local Inspector Commended for Perfect Record",
                body = "Supervisors praised the checkpoint's newest hire for a flawless shift. \"By the book,\" said one. \"Exactly by the book.\"",
            }
            : new NewsStory
            {
                headline = "Checkpoint Delays Frustrate Commuters",
                body = "Supervisors blamed \"new staff.\" They did not elaborate.",
            });

        stories.Add(new NewsStory
        {
            headline = "Heat Wave Continues; Highs of 108°F Expected",
            body = "Officials urge residents to stay indoors and drink water. Officials did not say what people without water should do.",
        });

        return stories;
    }

    // The last page states the numbers, then shows home. It never explains.
    string EndingText()
    {
        int docked = Mathf.Min(0, totalPoints) * dollarsPerPoint;
        int finalSavings = household.savings + docked;
        var text = new StringBuilder();

        text.AppendLine("You hand in your badge.");
        text.AppendLine(totalPoints == 0
            ? "Your record is clean."
            : $"Citations on your record: <b>{totalPoints}</b>. {Money.Format(-docked)} comes out of your last check.");
        text.AppendLine($"In the coffee can: <b>{Money.Format(finalSavings)}</b>");
        text.AppendLine();

        foreach (string line in HomeImages(finalSavings))
        {
            text.AppendLine(line);
            text.AppendLine();
        }

        return text.ToString();
    }

    IEnumerable<string> HomeImages(int finalSavings)
    {
        if (finalSavings < 0)
            yield return "There's an envelope from the landlord on the kitchen table. You leave it for tomorrow.";
        if (household.daysWithoutMedicine >= 2)
            yield return "Abuela's pill box is still full. She says she feels fine. She doesn't get up to say it.";
        else if (household.daysWithoutMedicine == 1)
            yield return "Abuela coughs through the night.";
        if (household.daysNephewWentWithout >= 1)
            yield return "Your nephew's shoes are by the door, taped at the toe.";
        if (household.daysHungry >= 1)
            yield return "Dinner is beans again. Your nephew says he isn't hungry.";
        if (history.Any(r => r.bribeTaken))
            yield return "Folded in your jacket, a few bills that didn't come from your paycheck.";
        yield return "The porch light is on. Someone waited up.";
    }


    // ---------- Small stuff ----------

    void OnDecisionClicked(Decision clicked) => decision = clicked;

    void OnBribeClicked()
    {
        bribeTaken = true;
        decision = Decision.Approve;
    }

    void OnWrongClick()
    {
        AddPoints(Scoring.WrongClick);
        if (tutorialClicks && !guide.IsWaiting)
            guide.Say("¡Oye! That's just part of the car, primo. That click cost you a point. Only click what doesn't belong.");
    }

    void AddPoints(int points)
    {
        dayPoints += points;
        totalPoints += points;
        RefreshHud();
    }

    void RefreshHud()
    {
        if (currentDay != null && household != null)
            desk.SetHud(currentDay.dayNumber, currentDay.todaysDate, dayPoints, household.savings + bribesToday);
    }

    static string Outcome(DecisionRecord record)
    {
        if (record.bribeTaken) return $"APPROVED (took ${record.traveler.bribeAmount})";
        switch (record.decision)
        {
            case Decision.Approve: return "APPROVED";
            case Decision.Reject: return "REJECTED";
            default: return "IMPRISONED";
        }
    }

    static string FarewellLine(TravelerData traveler, Decision decision)
    {
        switch (decision)
        {
            case Decision.Approve: return traveler.lineIfApproved;
            case Decision.Reject: return traveler.lineIfRejected;
            default: return traveler.lineIfImprisoned;
        }
    }
}
