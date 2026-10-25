using System.Collections.Generic;
using UnityEngine;

// The X-ray monitor on the desk. Watches the checkpoint: when a new car pulls up,
// it loads that car's X-ray into both views and reveals them in sync with the beam.
// Handles clicks: contraband gets circled, anything else is a wrong click.
public class XRayPanel : MonoBehaviour
{
    [SerializeField] CheckpointStage stage;
    [SerializeField] XRayView sideView;
    [SerializeField] XRayView topView;
    [SerializeField] BossBark boss;

    readonly HashSet<XRayItem> found = new HashSet<XRayItem>();
    CarView shownCar;

    public int FoundCount => found.Count;
    public int WrongClicks { get; private set; }

    void OnEnable()
    {
        sideView.Clicked += HandleClick;
        topView.Clicked += HandleClick;
    }

    void OnDisable()
    {
        sideView.Clicked -= HandleClick;
        topView.Clicked -= HandleClick;
    }

    void Update()
    {
        CarView car = stage.CurrentCar;
        if (car != shownCar)
            ShowCar(car);

        float progress = car != null ? stage.ScanProgress : 0f;
        sideView.SetReveal(progress);
        topView.SetReveal(progress);
    }

    void ShowCar(CarView car)
    {
        shownCar = car;
        found.Clear();
        WrongClicks = 0;

        if (car == null)
        {
            sideView.Clear();
            topView.Clear();
            return;
        }

        List<XRayItem> items = AllItems(car.Traveler);
        sideView.Show(items);
        topView.Show(items);
    }

    void HandleClick(XRayView view, Vector2 point)
    {
        if (shownCar == null) return;

        foreach (XRayItem item in shownCar.Traveler.contraband)
        {
            if (!view.Contains(item, point)) continue;

            // Found in one view = found in both.
            if (found.Add(item))
            {
                sideView.MarkFound(item);
                topView.MarkFound(item);
                Debug.Log($"Found contraband: {item.label} ({found.Count}/{shownCar.Traveler.contraband.Count})");
            }
            return;
        }

        WrongClicks++;
        view.ShowMiss(point);
        if (boss != null) boss.Yell();
        Debug.Log($"Wrong click! {Scoring.WrongClick} point (total wrong clicks: {WrongClicks})");
    }

    static List<XRayItem> AllItems(TravelerData traveler)
    {
        var items = new List<XRayItem>();
        if (traveler.vehicleModel != null)
            items.AddRange(traveler.vehicleModel.baseItems);
        items.AddRange(traveler.harmlessExtras);
        items.AddRange(traveler.contraband);
        return items;
    }
}
