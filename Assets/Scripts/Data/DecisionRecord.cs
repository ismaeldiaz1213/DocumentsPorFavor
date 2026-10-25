using System.Collections.Generic;

// What happened with one traveler. Used by the end-of-day ledger,
// the newspaper, and the ending.
public class DecisionRecord
{
    public readonly int day;
    public readonly TravelerData traveler;
    public readonly Decision decision;
    public readonly bool correct;
    public readonly bool bribeTaken;
    public readonly List<Citation> citations;
    public readonly int wrongClicks;

    public DecisionRecord(int day, TravelerData traveler, Decision decision, bool correct, bool bribeTaken,
        List<Citation> citations, int wrongClicks)
    {
        this.day = day;
        this.traveler = traveler;
        this.decision = decision;
        this.correct = correct;
        this.bribeTaken = bribeTaken;
        this.citations = citations;
        this.wrongClicks = wrongClicks;
    }
}
