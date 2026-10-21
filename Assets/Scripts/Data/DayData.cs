using System.Collections.Generic;
using UnityEngine;

// One day at the booth: which travelers show up, in order.
// Create one in the Project window: right-click > Create > Documents Por Favor > Day.
[CreateAssetMenu(fileName = "Day1", menuName = "Documents Por Favor/Day")]
public class DayData : ScriptableObject
{
    public int dayNumber = 1;

    [Tooltip("The in-game date, YYYY-MM-DD. Passports expiring before this date are invalid.")]
    public string todaysDate = "2026-01-01";

    [Tooltip("Travelers in the order they pull up to the booth.")]
    public List<TravelerData> travelers = new List<TravelerData>();
}
