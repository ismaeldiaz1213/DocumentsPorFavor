using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public enum Decision { Approve, Reject, Imprison }

// The rulebook: what the government says the "correct" call is on a given day.
// Pure logic, no scene objects, so it is easy to read and test.
// Each day turns on more checks (see DayData): passports always, then permits,
// then the X-ray, then the vehicle registration.
public static class Rules
{
    const string DateFormat = "yyyy-MM-dd";

    // Every reason the paperwork is invalid today. Empty list = paperwork is fine.
    public static List<string> FindPaperworkProblems(TravelerData traveler, DayData day)
    {
        var problems = new List<string>();

        // Passport: always.
        if (traveler.photoIsSomeoneElse)
            problems.Add("The passport photo is not the person at the window.");
        if (IsExpired(traveler.passportExpiry, day.todaysDate, traveler, "passport expiry"))
            problems.Add($"The passport expired on {traveler.passportExpiry}.");

        // Entry permit: from the day permits start, for everyone who isn't a US citizen.
        if (day.permitsRequired && !traveler.IsUSCitizen)
        {
            if (!traveler.hasPermit)
                problems.Add("No entry permit, and they are not a US citizen.");
            else
            {
                if (!SameText(traveler.permitName, traveler.passportName))
                    problems.Add($"The permit says {traveler.permitName}. The passport says {traveler.passportName}.");
                if (IsExpired(traveler.permitExpiry, day.todaysDate, traveler, "permit expiry"))
                    problems.Add($"The entry permit expired on {traveler.permitExpiry}.");
            }
        }

        // Vehicle registration: from the day it starts.
        if (day.registrationRequired)
        {
            if (!SameText(traveler.passportName, traveler.registrationOwner))
                problems.Add($"The car is registered to {traveler.registrationOwner}, not {traveler.passportName}.");
            if (!SameText(traveler.registrationPlate, traveler.vehiclePlate))
                problems.Add($"The registration says plate {traveler.registrationPlate}. The car says {traveler.vehiclePlate}.");
        }

        return problems;
    }

    // Contraband (once the scanner works) -> arrest. Bad paperwork -> turn back. Otherwise -> let them in.
    public static Decision CorrectDecision(TravelerData traveler, DayData day)
    {
        if (day.xrayScanner && traveler.HasContraband)
            return Decision.Imprison;

        if (FindPaperworkProblems(traveler, day).Count > 0)
            return Decision.Reject;

        return Decision.Approve;
    }

    static bool IsExpired(string expiryText, string todaysDate, TravelerData traveler, string what)
    {
        if (!TryParseDate(expiryText, out DateTime expiry))
        {
            Debug.LogWarning($"{traveler.name}: {what} '{expiryText}' is not YYYY-MM-DD.", traveler);
            return true;
        }
        if (!TryParseDate(todaysDate, out DateTime today))
        {
            Debug.LogWarning($"Today's date '{todaysDate}' is not YYYY-MM-DD.");
            return false;
        }
        return expiry < today;
    }

    static bool SameText(string a, string b) =>
        string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase);

    // InvariantCulture: parse the same way on every machine, whatever its language settings.
    static bool TryParseDate(string text, out DateTime date) =>
        DateTime.TryParseExact((text ?? "").Trim(), DateFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out date);
}
