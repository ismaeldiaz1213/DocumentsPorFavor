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
// the counter goes down with every citation, and at the end of the job
// every point below zero is docked from your final pay.
public static class Scoring
{
    public const int MissedContraband = -1; // per item not found on the X-ray
    public const int LetInCriminal = -3;    // approved a car with contraband (bribe or not)
    public const int WrongDecision = -1;    // any other wrong call
    public const int WrongClick = -1;       // clicked X-ray on something that isn't contraband (applied live)

    // Citations earned when a car leaves the booth.
    public static List<Citation> CitationsFor(TravelerData traveler, string todaysDate, Decision decision, int contrabandFound)
    {
        var citations = new List<Citation>();

        int missed = traveler.contraband.Count - contrabandFound;
        for (int i = 0; i < missed; i++)
            citations.Add(new Citation("Missed contraband on X-ray", MissedContraband));

        Decision correct = Rules.CorrectDecision(traveler, todaysDate);
        if (decision == correct) return citations;

        if (decision == Decision.Approve && traveler.HasContraband)
            citations.Add(new Citation("Let in a smuggler", LetInCriminal));
        else
            citations.Add(new Citation(WrongDecisionReason(correct, decision), WrongDecision));

        return citations;
    }

    static string WrongDecisionReason(Decision correct, Decision made)
    {
        switch (correct)
        {
            case Decision.Approve:
                return made == Decision.Reject ? "Turned away a valid traveler" : "Arrested an innocent traveler";
            case Decision.Reject:
                return made == Decision.Approve ? "Approved invalid paperwork" : "Arrested someone over paperwork";
            default:
                return "Let a smuggler turn around";
        }
    }
}
