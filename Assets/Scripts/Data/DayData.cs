using System.Collections.Generic;
using UnityEngine;

// One day at the booth: which travelers show up, in order, and which rules are in force.
// Each day adds one new thing to check, like a new page in the rulebook.
// Create one in the Project window: right-click > Create > Documents Por Favor > Day.
[CreateAssetMenu(fileName = "Day1", menuName = "Documents Por Favor/Day")]
public class DayData : ScriptableObject
{
    public int dayNumber = 1;

    [Tooltip("The in-game date, YYYY-MM-DD. Passports expiring before this date are invalid.")]
    public string todaysDate = "2026-01-01";

    [Header("Rules in force today (passports are always checked)")]
    [Tooltip("Non-US citizens need a valid entry permit in their own name.")]
    public bool permitsRequired;
    [Tooltip("The X-ray scanner works: hidden goods or people mean IMPRISON.")]
    public bool xrayScanner;
    [Tooltip("The registration owner must match the passport, and its plate the car.")]
    public bool registrationRequired;

    [Header("Morning memo from the jefe")]
    public string memoTitle = "MEMO";
    [TextArea(4, 12)] public string memo;

    [Header("Shift")]
    [Tooltip("Real seconds from 07:00 to 19:00. Whoever is still in line at 19:00 goes home.")]
    public float shiftSeconds = 360f;

    [Tooltip("Travelers in the order they pull up to the booth.")]
    public List<TravelerData> travelers = new List<TravelerData>();
}
