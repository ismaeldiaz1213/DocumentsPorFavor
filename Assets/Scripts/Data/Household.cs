// Your money and your family: Abuela, your nephew, and the fridge.
// Every bill you skip makes things a little worse at home.
public class Household
{
    public int savings;
    public int daysHungry;
    public int daysWithoutMedicine;
    public int daysNephewWentWithout;
    public int nightsInDebt;
    public bool memoHome; // your brother made it across

    public Household(int startingSavings)
    {
        savings = startingSavings;
    }

    public void RecordBills(bool paidFood, bool paidMedicine, bool paidNephew)
    {
        if (savings < 0) nightsInDebt++;
        if (!paidFood) daysHungry++;
        if (!paidMedicine) daysWithoutMedicine++;
        if (!paidNephew) daysNephewWentWithout++;
    }

    // The ways a family can fall apart. Each one ends the game.
    public bool Evicted => nightsInDebt >= 2;
    public bool LostAbuela => daysWithoutMedicine >= 3;

    public string FoodStatus
    {
        get
        {
            switch (daysHungry)
            {
                case 0: return "The family is eating.";
                case 1: return "The family went to bed hungry.";
                default: return "The family is starving.";
            }
        }
    }

    public string GrandmaStatus
    {
        get
        {
            switch (daysWithoutMedicine)
            {
                case 0: return "Abuela is doing okay.";
                case 1: return "Abuela is coughing.";
                case 2: return "Abuela is very sick.";
                default: return "Abuela is in the hospital.";
            }
        }
    }

    public string NephewStatus
    {
        get
        {
            switch (daysNephewWentWithout)
            {
                case 0: return "Your nephew is happy.";
                case 1: return "Your nephew's shoes have holes in them.";
                default: return "Your nephew stopped going to school.";
            }
        }
    }

    public string Report() =>
        $"{GrandmaStatus}\n{NephewStatus}\n{FoodStatus}" +
        (memoHome ? "\nMemo is asleep on the couch. Abuela hasn't stopped smiling." : "") +
        (savings < 0 ? "\nThe landlord knocked twice." : "");
}
