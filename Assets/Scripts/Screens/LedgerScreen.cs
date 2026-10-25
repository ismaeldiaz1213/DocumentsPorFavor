using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// End of the day: what happened, what you earned, and which bills you pay.
// Rent is automatic. Food, Abuela's medicine and your nephew are your choice.
public class LedgerScreen : MonoBehaviour
{
    [SerializeField] GameObject root;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text logText;
    [SerializeField] TMP_Text incomeText;
    [SerializeField] TMP_Text familyText;
    [SerializeField] TMP_Text totalText;
    [SerializeField] Button foodButton;
    [SerializeField] TMP_Text foodLabel;
    [SerializeField] Button medicineButton;
    [SerializeField] TMP_Text medicineLabel;
    [SerializeField] Button nephewButton;
    [SerializeField] TMP_Text nephewLabel;
    [SerializeField] Button goHomeButton;

    public bool PayFood { get; private set; }
    public bool PayMedicine { get; private set; }
    public bool PayNephew { get; private set; }

    int totalBeforeBills;
    int foodCost;
    int medicineCost;
    int nephewCost;
    bool closed;

    void Awake()
    {
        foodButton.onClick.AddListener(() => { PayFood = !PayFood; Refresh(); });
        medicineButton.onClick.AddListener(() => { PayMedicine = !PayMedicine; Refresh(); });
        nephewButton.onClick.AddListener(() => { PayNephew = !PayNephew; Refresh(); });
        goHomeButton.onClick.AddListener(() => closed = true);
        root.SetActive(false);
    }

    public IEnumerator Show(string title, string log, string income, int totalBeforeOptionalBills,
        int food, int medicine, int nephew, string family)
    {
        titleText.text = title;
        logText.text = log;
        incomeText.text = income;
        familyText.text = family;
        totalBeforeBills = totalBeforeOptionalBills;
        foodCost = food;
        medicineCost = medicine;
        nephewCost = nephew;
        PayFood = PayMedicine = PayNephew = true;
        Refresh();

        closed = false;
        root.SetActive(true);
        while (!closed)
            yield return null;
        root.SetActive(false);
    }

    public int TotalAfterBills =>
        totalBeforeBills - (PayFood ? foodCost : 0) - (PayMedicine ? medicineCost : 0) - (PayNephew ? nephewCost : 0);

    void Refresh()
    {
        SetRow(foodButton, foodLabel, "Food", foodCost, PayFood);
        SetRow(medicineButton, medicineLabel, "Abuela's medicine", medicineCost, PayMedicine);
        SetRow(nephewButton, nephewLabel, "Nephew's school shoes", nephewCost, PayNephew);

        int total = TotalAfterBills;
        string color = total < 0 ? ColorUtility.ToHtmlStringRGB(Palette.Reject) : ColorUtility.ToHtmlStringRGB(Palette.PaperInk);
        totalText.text = $"SAVINGS AFTER TONIGHT<pos=70%><color=#{color}>{Money.Format(total)}</color>";
    }

    static void SetRow(Button button, TMP_Text label, string bill, int cost, bool pay)
    {
        ((Image)button.targetGraphic).color = pay ? Palette.Approve : Palette.Reject;
        label.text = pay ? $"PAY  ·  {bill}<pos=72%>-${cost}" : $"SKIP  ·  {bill}<pos=72%>$0";
    }
}
