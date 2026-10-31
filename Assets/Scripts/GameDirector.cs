using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// Runs the whole game: title, intro, then each day (memo, shift, ledger,
// newspaper), then an ending. Reads like the game's script: each step waits
// for the one before it.
//
// Ways it can end: you finish the last day (you keep the job), your record
// hits -10 (fired), you can't make rent two nights (evicted), or Abuela goes
// three nights without her medicine.
public class GameDirector : MonoBehaviour
{
    enum Ending { KeptJob, Fired, Evicted, Abuela }

    [Header("Scene")]
    [SerializeField] CheckpointStage stage;
    [SerializeField] XRayPanel xray;
    [SerializeField] DeskUI desk;
    [SerializeField] TitleScreen title;
    [SerializeField] NoteScreen note;
    [SerializeField] TutorialGuide guide;
    [SerializeField] LedgerScreen ledger;
    [SerializeField] NewspaperScreen newspaper;
    [SerializeField] EndingScreen ending;

    [Header("Days")]
    [Tooltip("The days in order. Each one turns on a new rule.")]
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

    [TextArea(6, 14)]
    [SerializeField] string introText =
        "AUGUST, 2027. SANTA TERESA PORT OF ENTRY.\n\n" +
        "The crossing is hiring. Your cousin José works security there, and he vouched for you.\n\n" +
        "Rent is due every night. It's that kind of apartment. Abuela's medicine is not cheap. Your nephew has outgrown his shoes.\n\n" +
        "Your older brother Memo was sent back across eight years ago. Abuela still thinks he's in Houston.\n\n" +
        "Report to Booth 3 at 07:00.";

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
    bool tutorialClicks; // José comments on wrong clicks while he's teaching the X-ray

    // The shift clock: 07:00 to 19:00 over the day's shiftSeconds.
    bool clockRunning;
    float shiftElapsed;
    int lastHour;

    bool Fired => totalPoints <= Scoring.FiredAt;
    bool ShiftOver => currentDay != null && shiftElapsed >= currentDay.shiftSeconds;

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
        yield return note.Show("", introText, "BEGIN", typed: true);

        for (int i = 0; i < days.Count; i++)
        {
            DayData day = days[i];
            yield return RunDay(day);
            if (Fired)
            {
                ShowEnding(Ending.Fired);
                yield break;
            }

            yield return EndOfDay(day);
            if (household.Evicted)
            {
                ShowEnding(Ending.Evicted);
                yield break;
            }
            if (household.LostAbuela)
            {
                ShowEnding(Ending.Abuela);
                yield break;
            }

            bool lastDay = i == days.Count - 1;
            yield return newspaper.Show($"EVENING EDITION  ·  {day.todaysDate}  ·  25¢",
                Edition(day.dayNumber, lastDay), lastDay ? "CONTINUE" : "NEXT DAY");
        }

        ShowEnding(Ending.KeptJob);
    }

    void Update()
    {
        // The clock only runs while you're working (not while José is explaining something).
        if (!clockRunning || guide.IsWaiting) return;
        shiftElapsed += Time.deltaTime;
        UpdateClock();
    }

    // ---------- A day at the booth ----------

    IEnumerator RunDay(DayData day)
    {
        currentDay = day;
        dayPoints = 0;
        bribesToday = 0;
        shiftElapsed = 0f;
        lastHour = 7;
        desk.SetRulesForDay(day);
        UpdateClock();
        RefreshHud();

        yield return desk.ShowDayBanner($"DAY {day.dayNumber}\n<size=50%>{day.todaysDate}</size>", dayBannerSeconds);
        if (!string.IsNullOrEmpty(day.memo))
            yield return note.Show(day.memoTitle, day.memo, "BEGIN SHIFT", typed: false);

        // Some travelers only come if something happened earlier.
        List<TravelerData> travelers = day.travelers.Where(ShowsUp).ToList();
        stage.OpenLane(travelers);
        Coroutine ambience = StartCoroutine(stage.Ambience());

        yield return MorningVisitFromJose(day);
        clockRunning = true;

        int served = 0;
        while (stage.AnyoneLeft && !ShiftOver && !Fired)
        {
            bool teach = served == 0 && (day.dayNumber == 1 || (day.xrayScanner && !days.Any(d => d.dayNumber < day.dayNumber && d.xrayScanner)));
            if (teach && day.dayNumber == 1)
                guide.Say("When you're ready for a car, click the megaphone over the booth. ¡Siguiente!");

            yield return desk.WaitForNext(() => ShiftOver);
            if (ShiftOver) break;
            yield return RunTraveler(teach);
            served++;
        }

        clockRunning = false;
        StopCoroutine(ambience);
        if (!Fired)
        {
            Sfx.Play(Sound.Whistle);
            yield return stage.CloseLane();
        }
        else
        {
            yield return stage.CloseLane();
        }
    }

    // José comes by when there's news: your first day, your brother, the new scanner.
    IEnumerator MorningVisitFromJose(DayData day)
    {
        if (day.dayNumber == 1)
        {
            yield return guide.WalkOver();
            yield return guide.SayAndWait("¡Primo! Over here! It's me, José. Security now. Look, they gave me a hat.");
            yield return guide.SayAndWait("I pulled a lot of strings to get you this booth, so don't make me look bad, eh? Rent is due, Abuela needs her medicine, and your nephew needs shoes.");
            yield return guide.SayAndWait("Everybody you talk to shows up right here, in this box on the left, and their face is in the window. Even mine. See? Handsome.");
        }
        else if (day.dayNumber == 2)
        {
            yield return guide.WalkOver();
            yield return guide.SayAndWait("Primo. Psst. Before you open.");
            yield return guide.SayAndWait("I heard something. Memo's in Juárez. Has been for a while.");
            yield return guide.SayAndWait("If he shows up at your window... I didn't tell you anything. I'm not even here.");
            yield return guide.WalkBack();
        }
        else if (day.xrayScanner && !days.Any(d => d.dayNumber < day.dayNumber && d.xrayScanner))
        {
            yield return guide.WalkOver();
            yield return guide.SayAndWait("¡Primo! The new toy came in. The X-ray! Bottom right. Every car gets scanned now, while the driver walks to your window.");
        }
    }

    IEnumerator RunTraveler(bool teach)
    {
        bool firstDay = currentDay.dayNumber == 1;
        bool xrayLesson = teach && currentDay.xrayScanner;
        tutorialClicks = xrayLesson;

        yield return stage.PullUpToScanner();
        TravelerData traveler = stage.CurrentCar.Traveler;

        // The driver walks in while the car is scanned. They start talking
        // when they reach the booth; you can decide once the scan is done.
        Coroutine scan = currentDay.xrayScanner ? StartCoroutine(stage.ScanCar()) : null;
        yield return stage.DriverWalksToBooth();
        desk.ShowTraveler(traveler, BoothLinesFor(traveler));
        if (scan != null) yield return scan;

        if (teach && firstDay)
        {
            yield return guide.SayAndWait("Day one, it's just the PASSPORT. Two things. The EXPIRES date can't be before today's date, top left. And the PHOTO has to be the face in the window.");
            yield return guide.SayAndWait("Papers good: APPROVE. Anything wrong: REJECT. Forget something? The RULES book is up top. And watch the clock. The shift ends at 19:00, and they pay per person.");
            yield return guide.SayAndWait("Every mistake is a citation. Ten points of citations and they take your badge. So... no mistakes, eh? This one's an abuela with a lot of tamales. Your call.");
        }
        else if (xrayLesson)
        {
            yield return guide.SayAndWait("If something is hiding in a car, a person, a package, anything that isn't part of the car, you CLICK on it. Top picture is the car from the side, bottom is from above.");
            yield return guide.SayAndWait("But every click on normal car stuff, an engine, a seat, a spare tire, is ONE POINT off your record. The jefe hates wasted time. Only click what doesn't belong.");
            yield return guide.SayAndWait("Found something hidden? IMPRISON. I walk them out myself, and the tow truck takes the car. This one's the trumpet guy again. Metal glows bright. Bright doesn't mean illegal.");
        }

        decision = null;
        bribeTaken = false;
        bool bribeOffered = false;
        desk.SetDecisionsEnabled(true);

        while (decision == null)
        {
            // Smugglers get nervous once you've spotted something on the X-ray.
            if (!bribeOffered && traveler.OffersBribe && currentDay.xrayScanner && xray.FoundCount > 0)
            {
                bribeOffered = true;
                desk.OfferBribe(traveler);
            }
            yield return null;
        }

        desk.SetDecisionsEnabled(false);
        Decision finalDecision = decision.Value;
        bool correct = finalDecision == Rules.CorrectDecision(traveler, currentDay);

        List<Citation> citations = Scoring.CitationsFor(traveler, currentDay, finalDecision, xray.FoundCount);
        foreach (Citation citation in citations)
            AddPoints(citation.points);

        if (bribeTaken)
        {
            bribesToday += traveler.bribeAmount;
            RefreshHud();
        }
        if (traveler.isFamily && finalDecision == Decision.Approve)
            household.memoHome = true;

        history.Add(new DecisionRecord(currentDay.dayNumber, traveler, finalDecision, correct, bribeTaken, citations, xray.WrongClicks));
        foreach (NewsStory story in traveler.NewsFor(finalDecision))
            pendingNews.Add(new PendingStory { day = currentDay.dayNumber + story.daysLater, story = story });

        tutorialClicks = false;
        desk.Say(FarewellLine(traveler, finalDecision));
        desk.ShowCitations(citations);

        if (teach && (firstDay || xrayLesson))
        {
            yield return new WaitForSeconds(1.5f); // let their goodbye land first
            yield return guide.SayAndWait(correct
                ? "¡Eso! See? Easy. Okay, back to my post. You got this, primo."
                : "Ay, primo... That's coming out of your pay. Okay, back to my post. Careful, eh? Abuela's counting on you.");
        }

        // If you imprison someone, José walks them out. Otherwise he heads back to his post.
        if (finalDecision == Decision.Imprison)
            guide.LeftWithPrisoner();
        else
            StartCoroutine(guide.WalkBack());

        yield return stage.SendAway(finalDecision);
        desk.ClearTraveler();
    }

    bool ShowsUp(TravelerData traveler)
    {
        if (traveler.onlyIfApproved != null && !WasApproved(traveler.onlyIfApproved)) return false;
        if (traveler.onlyIfNotApproved != null && WasApproved(traveler.onlyIfNotApproved)) return false;
        return true;
    }

    bool WasApproved(TravelerData traveler) =>
        history.Any(r => r.traveler == traveler && r.decision == Decision.Approve);

    void UpdateClock()
    {
        float k = currentDay != null ? Mathf.Clamp01(shiftElapsed / currentDay.shiftSeconds) : 0f;
        int minutes = Mathf.FloorToInt(k * 12f * 60f / 10f) * 10; // ticks in 10-minute steps
        int hour = 7 + minutes / 60;
        desk.SetClock($"{hour:00}:{minutes % 60:00}");
        if (hour != lastHour)
        {
            lastHour = hour;
            Sfx.Play(Sound.Tick, 0.6f);
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
        if (today.Count == 0)
            text.AppendLine("Nobody made it to your window.");
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
        text.AppendLine($"<b>Points today: {dayPoints}</b>     Record: {totalPoints} (fired at {Scoring.FiredAt})");
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

    // ---------- Endings ----------

    // The last page states what happened and the numbers, then shows home. It never explains.
    void ShowEnding(Ending how)
    {
        int docked = Mathf.Min(0, totalPoints) * dollarsPerPoint;
        int finalSavings = household.savings + docked;
        var text = new StringBuilder();
        string heading;

        switch (how)
        {
            case Ending.Fired:
                heading = "BADGE, POR FAVOR";
                text.AppendLine($"The jefe holds out his hand. You give him the badge. It's {currentDay.todaysDate}.");
                break;
            case Ending.Evicted:
                heading = "THE LOCKS WERE CHANGED";
                text.AppendLine("The landlord's nephew changed the locks on a Tuesday. Your things are in boxes on the porch.");
                break;
            case Ending.Abuela:
                heading = "AN EMPTY CHAIR";
                text.AppendLine("Abuela's chair at the kitchen table stays empty now. Nobody sits in it. Nobody moves it.");
                break;
            default:
                heading = "CONTRACT RENEWED";
                text.AppendLine("The jefe signs you on for another month. He doesn't look up.");
                break;
        }

        text.AppendLine(totalPoints == 0
            ? "Your record is clean."
            : $"Citations on your record: <b>{totalPoints}</b>. {Money.Format(-docked)} comes out of your last check.");
        text.AppendLine($"In the coffee can: <b>{Money.Format(finalSavings)}</b>");
        text.AppendLine();

        foreach (string line in HomeImages(finalSavings, how))
        {
            text.AppendLine(line);
            text.AppendLine();
        }

        ending.Show(heading, text.ToString(), kept: how == Ending.KeptJob);
    }

    IEnumerable<string> HomeImages(int finalSavings, Ending how)
    {
        if (finalSavings < 0 && how != Ending.Evicted)
            yield return "There's an envelope from the landlord on the kitchen table. You leave it for tomorrow.";
        if (how != Ending.Abuela)
        {
            if (household.daysWithoutMedicine >= 2)
                yield return "Abuela's pill box is still full. She says she feels fine. She doesn't get up to say it.";
            else if (household.daysWithoutMedicine == 1)
                yield return "Abuela coughs through the night.";
        }
        if (household.daysNephewWentWithout >= 1)
            yield return "Your nephew's shoes are by the door, taped at the toe.";
        if (household.daysHungry >= 1)
            yield return "Dinner is beans again. Your nephew says he isn't hungry.";
        if (history.Any(r => r.bribeTaken))
            yield return "Folded in your jacket, a few bills that didn't come from your paycheck.";

        // Memo. Never said outright.
        if (household.memoHome)
            yield return "Memo's boots are by the door, next to your nephew's.";
        else
        {
            DecisionRecord truck = history.FirstOrDefault(r => r.traveler.carriesFamily);
            if (truck != null && truck.decision == Decision.Imprison)
                yield return "A letter comes from Otero County with no name on it. Abuela can't read it. She keeps it in her apron.";
            else
                yield return "Abuela asks if anyone has heard from Houston.";
        }

        yield return how == Ending.Evicted
            ? "Your cousin José's couch is shorter than you are."
            : "The porch light is on. Someone waited up.";
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
            desk.SetHud(currentDay.dayNumber, currentDay.todaysDate, totalPoints, household.savings + bribesToday);
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

    // Someone you've met before remembers what you did to them last time.
    List<string> BoothLinesFor(TravelerData traveler)
    {
        foreach (ReturnVisit visit in traveler.returnVisits)
            if (history.Any(r => r.traveler == visit.earlierVisit && r.decision == visit.decisionThen))
                return visit.boothLines;
        return traveler.boothLines;
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
