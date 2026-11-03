using System.Collections.Generic;
using System.Linq;

// One penalty slip, like the citations in Papers, Please.
// reason: what you did wrong. why: what you should have seen.
public struct Citation
{
    public string reason;
    public string why;
    public int points;

    public Citation(string reason, int points, string why = "")
    {
        this.reason = reason;
        this.points = points;
        this.why = why;
    }
}

// How many points each mistake costs. Points work like license points:
// the counter goes down with every citation, and at the end of the job
// every point below zero is docked from your final pay.
public static class Scoring
{
    public const int MissedContraband = -1; // found NOTHING on an X-ray that had something hidden
    public const int LetInCriminal = -3;    // approved a car with contraband (bribe or not)
    public const int WrongDecision = -1;    // any other wrong call
    public const int WrongClick = -1;       // clicked X-ray on something that isn't contraband (applied live)
    public const int FiredAt = -10;         // a record this low and they take your badge (never shown to the player)

    // Citations earned when a car leaves the booth.
    public static List<Citation> CitationsFor(TravelerData traveler, DayData day, Decision decision, int contrabandFound)
    {
        var citations = new List<Citation>();

        // Hidden goods: spotting even ONE piece is enough. You only get cited if you
        // missed all of it. (Before the scanner arrives, nobody can be blamed for what they couldn't see.)
        if (day.xrayScanner && traveler.HasContraband && contrabandFound == 0)
            citations.Add(new Citation("Missed the hidden goods on the X-ray", MissedContraband,
                $"Hidden in the car: {Hidden(traveler)}."));

        Decision correct = Rules.CorrectDecision(traveler, day);
        if (decision == correct) return citations;

        List<string> problems = Rules.FindPaperworkProblems(traveler, day);
        string paperwork = string.Join(" ", problems);

        switch (correct)
        {
            case Decision.Approve:
                citations.Add(decision == Decision.Reject
                    ? new Citation("Turned away a valid traveler", WrongDecision, "Their papers were all in order.")
                    : new Citation("Arrested an innocent traveler", WrongDecision, "Nothing was hidden, and their papers were in order."));
                break;

            case Decision.Reject:
                citations.Add(decision == Decision.Approve
                    ? new Citation("Approved invalid paperwork", WrongDecision, paperwork)
                    : new Citation("Arrested someone over paperwork", WrongDecision, paperwork + " Bad papers mean REJECT."));
                break;

            default: // should have been IMPRISON
                citations.Add(decision == Decision.Approve
                    ? new Citation("Let in a smuggler", LetInCriminal, $"Hidden in the car: {Hidden(traveler)}.")
                    : new Citation("Let a smuggler turn around", WrongDecision, $"Hidden in the car: {Hidden(traveler)}. That means IMPRISON."));
                break;
        }

        return citations;
    }

    // "package under the seat, hidden passenger x3"
    static string Hidden(TravelerData traveler) => string.Join(", ",
        traveler.contraband
            .GroupBy(item => item.label.ToLowerInvariant())
            .Select(group => group.Count() > 1 ? $"{group.Key} x{group.Count()}" : group.Key));
}
