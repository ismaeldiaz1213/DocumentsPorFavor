using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public enum Decision { Approve, Reject, Imprison }

// The rulebook: what the government says the "correct" call is.
// Pure logic, no scene objects, so it is easy to read and test.
public static class Rules
{
    const string DateFormat = "yyyy-MM-dd";

    // Every reason the paperwork is invalid. Empty list = paperwork is fine.
    public static List<string> FindPaperworkProblems(TravelerData traveler, string todaysDate)
    {
        var problems = new List<string>();

        if (!SameText(traveler.passportName, traveler.registrationOwner))
            problems.Add("Passport name does not match registration owner");

        if (!SameText(traveler.registrationPlate, traveler.vehiclePlate))
            problems.Add("Registration plate does not match the car's plate");

        if (!TryParseDate(traveler.passportExpiry, out DateTime expiry))
        {
            Debug.LogWarning($"{traveler.name}: passport expiry '{traveler.passportExpiry}' is not YYYY-MM-DD.", traveler);
            problems.Add("Passport expiry date is unreadable");
        }
        else if (!TryParseDate(todaysDate, out DateTime today))
        {
            Debug.LogWarning($"Today's date '{todaysDate}' is not YYYY-MM-DD.");
        }
        else if (expiry < today)
        {
            problems.Add("Passport is expired");
        }

        return problems;
    }

    // Contraband -> arrest. Bad paperwork -> turn back. Otherwise -> let them in.
    public static Decision CorrectDecision(TravelerData traveler, string todaysDate)
    {
        if (traveler.HasContraband)
            return Decision.Imprison;

        if (FindPaperworkProblems(traveler, todaysDate).Count > 0)
            return Decision.Reject;

        return Decision.Approve;
    }

    static bool SameText(string a, string b) =>
        string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase);

    // InvariantCulture: parse the same way on every machine, whatever its language settings.
    static bool TryParseDate(string text, out DateTime date) =>
        DateTime.TryParseExact((text ?? "").Trim(), DateFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out date);
}
