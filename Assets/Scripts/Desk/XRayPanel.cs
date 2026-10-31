using System;
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
    public event Action WrongClicked;

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

        TravelerData traveler = car.Traveler;
        VehicleModel model = traveler.vehicleModel;
        sideView.Show(model != null ? model.xraySide : null, AllItems(traveler, model != null && model.xraySide != null));
        topView.Show(model != null ? model.xrayTop : null, AllItems(traveler, model != null && model.xrayTop != null));
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
                Sfx.Play(Sound.Found);
                sideView.MarkFound(item);
                topView.MarkFound(item);
            }
            return;
        }

        WrongClicks++;
        Sfx.Play(Sound.Wrong);
        view.ShowMiss(point);
        if (boss != null) boss.Yell();
        WrongClicked?.Invoke();
    }

    // The vehicle's plain base shapes are only needed when there's no detailed X-ray picture.
    static List<XRayItem> AllItems(TravelerData traveler, bool hasVehiclePicture)
    {
        var items = new List<XRayItem>();
        if (traveler.vehicleModel != null && !hasVehiclePicture)
            items.AddRange(traveler.vehicleModel.baseItems);
        items.AddRange(traveler.harmlessExtras);
        items.AddRange(traveler.contraband);
        return items;
    }
}
