using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The top half of the screen: the line of cars, the scanner, the booth,
// and the three ways a car can leave. Every step is a coroutine, so a
// "director" script can play them in order with yield return.
//
// Cars arrive over the shift and wait in line until you call NEXT.
// Primo José (security) walks over when there's something to explain,
// and takes away anyone you imprison while the tow truck takes their car.
public class CheckpointStage : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] CarView carPrefab;

    [Header("Spots (empty GameObjects you place in the scene)")]
    [Tooltip("Where the first waiting car sits. The rest line up to its left.")]
    [SerializeField] Transform queueFront;
    [Tooltip("Where a car stops to be scanned.")]
    [SerializeField] Transform scannerSpot;
    [Tooltip("Where the driver walks to before stepping inside the booth.")]
    [SerializeField] Transform boothDoor;
    [Tooltip("Off-screen right (USA side). Approved cars and the tow truck leave here.")]
    [SerializeField] Transform exitRight;
    [Tooltip("Off-screen left, on the far lane. Rejected cars drive back here.")]
    [SerializeField] Transform exitLeft;

    [Header("Scanner")]
    [Tooltip("Thin bar that sweeps over the car while scanning.")]
    [SerializeField] Transform scanBeam;
    [SerializeField] float scanDuration = 4f;
    [Tooltip("How far the beam travels, centered on the car.")]
    [SerializeField] float beamSweepWidth = 3f;

    [Header("Tow truck")]
    [Tooltip("Starts off-screen right, facing LEFT.")]
    [SerializeField] Transform towTruck;
    [Tooltip("Distance from the car's center to the truck's center when hitched.")]
    [SerializeField] float hitchDistance = 2.4f;

    [Header("Arrivals")]
    [Tooltip("Cars already waiting when the shift starts.")]
    [SerializeField] int carsAtStart = 2;
    [Tooltip("Average seconds between new cars joining the line.")]
    [SerializeField] float secondsBetweenArrivals = 20f;

    [Header("Feel")]
    [SerializeField] float carSpeed = 4f;
    [SerializeField] float walkSpeed = 1.5f;
    [SerializeField] float queueSpacing = 2.8f;

    readonly List<CarView> queue = new List<CarView>();
    readonly List<TravelerData> stillToArrive = new List<TravelerData>();
    CarView currentCar;
    Vector3 towTruckHome;
    Transform guard;
    Coroutine arrivals;

    // 0 = scan not started, 1 = scan finished. The desk X-ray will read this.
    public float ScanProgress { get; private set; }
    public CarView CurrentCar => currentCar;
    public int CarsWaiting => queue.Count;
    public bool AnyoneLeft => queue.Count > 0 || stillToArrive.Count > 0;

    void Awake()
    {
        towTruckHome = towTruck.position;
        towTruck.gameObject.SetActive(false);
        scanBeam.gameObject.SetActive(false);

        // Primo José, in his security uniform. Drawn in code (see PortraitArt).
        guard = new GameObject("Guard (José)").transform;
        var renderer = guard.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = PortraitArt.Body(PortraitArt.Guard());
        renderer.sortingOrder = 6;
        guard.gameObject.SetActive(false);
    }

    void Update()
    {
        // Waiting cars roll forward to their spot in line, slowing as they get close.
        for (int i = 0; i < queue.Count; i++)
        {
            Transform car = queue[i].transform;
            Vector3 slot = QueueSlot(i);
            float distance = Vector3.Distance(car.position, slot);
            float speed = Mathf.Min(carSpeed, distance * 2.5f + 0.3f);
            car.position = Vector3.MoveTowards(car.position, slot, speed * Time.deltaTime);
        }
    }

    // ---------- The line ----------

    // Start of the shift: a couple of cars are already waiting, the rest drive in over time.
    public void OpenLane(List<TravelerData> travelers)
    {
        stillToArrive.Clear();
        stillToArrive.AddRange(travelers);
        for (int i = 0; i < carsAtStart && stillToArrive.Count > 0; i++)
            Arrive(alreadyWaiting: true);
        arrivals = StartCoroutine(Arrivals());
    }

    IEnumerator Arrivals()
    {
        while (stillToArrive.Count > 0)
        {
            float wait = secondsBetweenArrivals * Random.Range(0.7f, 1.3f);
            while (wait > 0f)
            {
                wait -= Time.deltaTime;
                if (queue.Count == 0) wait = Mathf.Min(wait, 2f); // an empty line hurries the next car along
                yield return null;
            }
            Arrive(alreadyWaiting: false);
        }
    }

    void Arrive(bool alreadyWaiting)
    {
        TravelerData traveler = stillToArrive[0];
        stillToArrive.RemoveAt(0);

        // New cars come in from off-screen left, in the near lane, and roll up to the back of the line.
        Vector3 start = alreadyWaiting
            ? QueueSlot(queue.Count)
            : new Vector3(exitLeft.position.x, queueFront.position.y, queueFront.position.z);
        CarView car = Instantiate(carPrefab, start, Quaternion.identity);
        car.Setup(traveler);
        queue.Add(car);
        if (!alreadyWaiting) Sfx.Play(Sound.CarPass, 0.3f);
    }

    // 17:00: everyone still in line has to turn around and go home, and they let you hear about it.
    // shout(car) is called as each driver leans on the horn.
    public IEnumerator CloseLane(System.Action<Transform> shout = null)
    {
        if (arrivals != null) StopCoroutine(arrivals);
        stillToArrive.Clear();
        if (queue.Count == 0) yield break;

        var leaving = new List<CarView>(queue);
        queue.Clear();
        Sfx.Play(Sound.CarPass, 0.6f);
        var trips = new List<Coroutine>();
        for (int i = 0; i < leaving.Count; i++)
        {
            CarView car = leaving[i];
            Sfx.Play(Sound.Horn, Random.Range(0.45f, 0.7f), Random.Range(0.85f, 1.15f));
            shout?.Invoke(car.transform);
            trips.Add(StartCoroutine(TurnAround(car)));
            yield return new WaitForSeconds(Random.Range(0.25f, 0.5f));
        }
        // One last long, angry honk from somebody.
        Sfx.Play(Sound.Horn, 0.6f, 0.8f);
        foreach (Coroutine trip in trips)
            yield return trip;
    }

    IEnumerator TurnAround(CarView car)
    {
        car.FaceLeft();
        Vector3 farLane = new Vector3(car.transform.position.x, exitLeft.position.y, car.transform.position.z);
        yield return MoveTo(car.transform, farLane, carSpeed * 0.5f);
        yield return MoveTo(car.transform, exitLeft.position, carSpeed);
        Destroy(car.gameObject);
    }

    // ---------- One traveler ----------

    // Waits for a car if the line is empty, then drives the first one up to the scanner.
    public IEnumerator PullUpToScanner()
    {
        while (queue.Count == 0)
            yield return null;

        currentCar = queue[0];
        queue.RemoveAt(0);
        ScanProgress = 0f;

        Sfx.Play(Sound.CarPass, 0.5f);
        yield return MoveTo(currentCar.transform, scannerSpot.position, carSpeed);
        Sfx.Play(Sound.Brake);
    }

    public IEnumerator DriverWalksToBooth()
    {
        Transform driver = currentCar.Driver;
        Sfx.Play(Sound.Door);
        driver.position = currentCar.SeatPosition;
        driver.gameObject.SetActive(true);
        yield return Walk(driver, boothDoor.position);
        // They stay right there, standing at your window, until you decide.
    }

    // One left-to-right sweep over the car.
    public IEnumerator ScanCar()
    {
        Vector3 carCenter = currentCar.transform.position;
        float beamY = scanBeam.position.y;
        scanBeam.gameObject.SetActive(true);
        Sfx.Play(Sound.ScanSweep);

        float elapsed = 0f;
        while (elapsed < scanDuration)
        {
            elapsed += Time.deltaTime;
            ScanProgress = Mathf.Clamp01(elapsed / scanDuration);
            float x = carCenter.x + Mathf.Lerp(-beamSweepWidth / 2f, beamSweepWidth / 2f, ScanProgress);
            scanBeam.position = new Vector3(x, beamY, scanBeam.position.z);
            yield return null;
        }

        scanBeam.gameObject.SetActive(false);
        Sfx.Play(Sound.ScanDone);
    }

    public IEnumerator SendAway(Decision decision)
    {
        CarView car = currentCar;

        switch (decision)
        {
            case Decision.Approve:
                yield return DriverReturnsToCar(car);
                yield return StartEngine();
                Sfx.Play(Sound.CarPass);
                yield return MoveTo(car.transform, exitRight.position, carSpeed);
                break;

            case Decision.Reject:
                yield return DriverReturnsToCar(car);
                yield return StartEngine();
                car.FaceLeft();
                Sfx.Play(Sound.CarPass);
                // Change to the far lane first, then drive back toward Mexico.
                Vector3 farLane = new Vector3(car.transform.position.x, exitLeft.position.y, car.transform.position.z);
                yield return MoveTo(car.transform, farLane, carSpeed * 0.5f);
                yield return MoveTo(car.transform, exitLeft.position, carSpeed);
                break;

            case Decision.Imprison:
                // José walks the driver off to the right while the tow truck takes the car.
                Transform driver = car.Driver;
                driver.SetParent(null, true); // keep the driver when the car is destroyed
                Coroutine escort = StartCoroutine(EscortAway(driver));
                yield return TowAway(car);
                yield return escort;
                Destroy(driver.gameObject);
                break;
        }

        Destroy(car.gameObject);
        currentCar = null;
    }

    IEnumerator DriverReturnsToCar(CarView car)
    {
        Transform driver = car.Driver;
        driver.position = boothDoor.position;
        driver.gameObject.SetActive(true);
        yield return Walk(driver, car.SeatPosition);
        driver.gameObject.SetActive(false);
        Sfx.Play(Sound.Door);
    }

    // Ding-ding-ding... the key turns... the engine catches.
    static IEnumerator StartEngine()
    {
        yield return new WaitForSeconds(0.3f);
        Sfx.Play(Sound.KeyChime, 0.8f);
        yield return new WaitForSeconds(0.9f);
        Sfx.Play(Sound.EngineStart);
        yield return new WaitForSeconds(0.9f);
    }

    IEnumerator EscortAway(Transform driver)
    {
        Vector3 offRight = new Vector3(exitRight.position.x, boothDoor.position.y, boothDoor.position.z);
        if (!guard.gameObject.activeSelf) // he may already be standing at the booth
        {
            guard.position = offRight;
            guard.gameObject.SetActive(true);
            yield return Walk(guard, boothDoor.position + Vector3.right * 0.6f);
        }

        driver.position = boothDoor.position;
        driver.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.4f);

        // Out to the right, the guard a step behind.
        Coroutine walkDriver = StartCoroutine(Walk(driver, offRight + Vector3.right * 0.6f));
        yield return new WaitForSeconds(0.35f);
        yield return Walk(guard, offRight + Vector3.right * 0.6f);
        yield return walkDriver;
        guard.gameObject.SetActive(false);
    }

    IEnumerator TowAway(CarView car)
    {
        towTruck.position = towTruckHome;
        SetFacingLeft(towTruck, true);
        towTruck.gameObject.SetActive(true);
        Sfx.Play(Sound.CarPass, 0.7f);

        // Drive in from the right and stop just in front of the car.
        Vector3 hitch = car.transform.position + Vector3.right * hitchDistance;
        yield return MoveTo(towTruck, hitch, carSpeed);

        // Turn around, hook the car, and haul it off.
        Sfx.Play(Sound.TowBeep);
        yield return new WaitForSeconds(0.3f);
        SetFacingLeft(towTruck, false);
        yield return new WaitForSeconds(0.3f);
        car.transform.SetParent(towTruck, true);
        Sfx.Play(Sound.CarPass, 0.7f);
        yield return MoveTo(towTruck, exitRight.position + Vector3.right * hitchDistance, carSpeed * 0.6f);

        car.transform.SetParent(null, true);
        towTruck.gameObject.SetActive(false);
    }

    // ---------- Primo José ----------

    // He walks over from his post (off-screen right) and stands by your booth.
    public IEnumerator GuardWalksOver()
    {
        guard.position = new Vector3(exitRight.position.x, boothDoor.position.y, boothDoor.position.z);
        guard.gameObject.SetActive(true);
        yield return Walk(guard, boothDoor.position + Vector3.right * 0.7f);
    }

    public IEnumerator GuardWalksBack()
    {
        if (!guard.gameObject.activeSelf) yield break;
        yield return Walk(guard, new Vector3(exitRight.position.x, boothDoor.position.y, boothDoor.position.z));
        guard.gameObject.SetActive(false);
    }

    // ---------- Life on the road ----------

    // Between cars. Never on a schedule, and mostly nothing at all: the desert is
    // quiet, with the odd long lull. When the wind does come, every gust is its own
    // loudness and pitch, usually soft, so it sits under the game instead of on top.
    public IEnumerator Ambience()
    {
        while (true)
        {
            bool lull = Random.value < 0.3f;
            yield return new WaitForSeconds(lull ? Random.Range(40f, 70f) : Random.Range(15f, 32f));

            float roll = Random.value;
            if (roll < 0.4f)
                Sfx.Play(Sound.Breeze, Soft(0.1f, 0.5f), Random.Range(0.8f, 1.1f));
            else if (roll < 0.5f)
            {
                Sfx.Play(Sound.Gust, Soft(0.2f, 0.6f), Random.Range(0.85f, 1.1f));
                if (Random.value < 0.4f) RollTumbleweed(); // a strong gust sometimes brings company
            }
            else if (roll < 0.6f)
                RollTumbleweed();
            else if (roll < 0.66f && queue.Count > 0)
                Sfx.Play(Sound.Horn, 0.25f); // someone in line is getting impatient
            // otherwise: nothing.
        }
    }

    // A random volume between min and max, leaning toward the quiet end.
    static float Soft(float min, float max) => Mathf.Lerp(min, max, Random.value * Random.value);

    // Across the far lane, in either direction.
    void RollTumbleweed()
    {
        float y = exitLeft.position.y + 0.4f;
        var left = new Vector3(exitLeft.position.x - 1f, y, 0f);
        var right = new Vector3(exitRight.position.x + 1f, y, 0f);
        if (Random.value < 0.5f) Tumbleweed.Roll(left, right);
        else Tumbleweed.Roll(right, left);
    }

    // ---------- Moving things ----------

    Vector3 QueueSlot(int index) => queueFront.position + Vector3.left * (queueSpacing * index);

    // Walking: steady pace, a little bob on every step, and a footstep sound.
    IEnumerator Walk(Transform walker, Vector3 destination)
    {
        Vector3 start = walker.position;
        float duration = Vector3.Distance(start, destination) / walkSpeed;
        float elapsed = 0f;
        float nextStep = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 position = Vector3.Lerp(start, destination, elapsed / duration);
            position.y += Mathf.Abs(Mathf.Sin(elapsed * Mathf.PI * 3.1f)) * 0.06f;
            walker.position = position;

            if (elapsed >= nextStep)
            {
                Sfx.PlayVaried(Sound.Step, 0.6f, 0.12f);
                nextStep += 0.32f;
            }
            yield return null;
        }

        walker.position = destination;
    }

    static void SetFacingLeft(Transform t, bool left)
    {
        Vector3 scale = t.localScale;
        scale.x = Mathf.Abs(scale.x) * (left ? -1f : 1f);
        t.localScale = scale;
    }

    // Move at roughly `speed` units per second, easing in and out so it feels weighty.
    static IEnumerator MoveTo(Transform target, Vector3 destination, float speed)
    {
        Vector3 start = target.position;
        float duration = Vector3.Distance(start, destination) / speed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float eased = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            target.position = Vector3.Lerp(start, destination, eased);
            yield return null;
        }

        target.position = destination;
    }
}
