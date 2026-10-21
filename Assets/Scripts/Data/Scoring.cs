using System.Collections.Generic;

// One penalty slip, like the citations in Papers, Please.
public struct Citation
{
    public string reason;
    public int points;

    public Citation(string reason, int points)
    {
        this.reason = reason;
        this.points = points;
    }
}

// How many points each mistake costs. Points work like license points:
// the day's counter goes down with every citation.
public static class Scoring
{
    public const int MissedContraband = -1;   // per item not found on the X-ray
    public const int LetInCriminal = -3;      // approved a car with contraband (bribe or not)
    public const int MissedBadPaperwork = -1; // approved invalid documents
    public const int WrongClick = -1;         // clicked X-ray on something that isn't contraband (applied live)

    // Citations earned when a car leaves the booth.
    public static List<Citation> CitationsFor(TravelerData traveler, string todaysDate, Decision decision, int contrabandFound)
    {
        var citations = new List<Citation>();

        int missed = traveler.contraband.Count - contrabandFound;
        for (int i = 0; i < missed; i++)
            citations.Add(new Citation("Missed contraband on X-ray", MissedContraband));

        if (decision == Decision.Approve)
        {
            if (traveler.HasContraband)
                citations.Add(new Citation("Let in a smuggler", LetInCriminal));

            if (Rules.FindPaperworkProblems(traveler, todaysDate).Count > 0)
                citations.Add(new Citation("Approved invalid paperwork", MissedBadPaperwork));
        }

        return citations;
    }
}
