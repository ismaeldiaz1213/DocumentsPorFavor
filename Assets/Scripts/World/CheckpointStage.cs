using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The top half of the screen: the line of cars, the scanner, the booth,
// and the three ways a car can leave. Every step is a coroutine, so a
// "director" script can play them in order with yield return.
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

    [Header("Feel")]
    [SerializeField] float carSpeed = 4f;
    [SerializeField] float walkSpeed = 1.5f;
    [SerializeField] float queueSpacing = 2.8f;

    readonly List<CarView> queue = new List<CarView>();
    CarView currentCar;
    Vector3 towTruckHome;

    // 0 = scan not started, 1 = scan finished. The desk X-ray will read this.
    public float ScanProgress { get; private set; }
    public CarView CurrentCar => currentCar;

    void Awake()
    {
        towTruckHome = towTruck.position;
        towTruck.gameObject.SetActive(false);
        scanBeam.gameObject.SetActive(false);
    }

    // Spawn one car per traveler, lined up behind the queue front.
    public void LineUp(List<TravelerData> travelers)
    {
        for (int i = 0; i < travelers.Count; i++)
        {
            CarView car = Instantiate(carPrefab, QueueSlot(i), Quaternion.identity);
            car.Setup(travelers[i]);
            queue.Add(car);
        }
    }

    public IEnumerator PullUpToScanner()
    {
        currentCar = queue[0];
        queue.RemoveAt(0);
        ScanProgress = 0f;

        // Everyone behind creeps forward one slot at the same time.
        for (int i = 0; i < queue.Count; i++)
            StartCoroutine(MoveTo(queue[i].transform, QueueSlot(i), carSpeed));

        yield return MoveTo(currentCar.transform, scannerSpot.position, carSpeed);
    }

    public IEnumerator DriverWalksToBooth()
    {
        Transform driver = currentCar.Driver;
        driver.position = currentCar.SeatPosition;
        driver.gameObject.SetActive(true);
        yield return MoveTo(driver, boothDoor.position, walkSpeed);
        driver.gameObject.SetActive(false); // they're inside the booth now
    }

    // One left-to-right sweep over the car.
    public IEnumerator ScanCar()
    {
        Vector3 carCenter = currentCar.transform.position;
        float beamY = scanBeam.position.y;
        scanBeam.gameObject.SetActive(true);

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
    }

    public IEnumerator SendAway(Decision decision)
    {
        CarView car = currentCar;

        switch (decision)
        {
            case Decision.Approve:
                yield return DriverReturnsToCar(car);
                yield return MoveTo(car.transform, exitRight.position, carSpeed);
                break;

            case Decision.Reject:
                yield return DriverReturnsToCar(car);
                car.FaceLeft();
                // Change to the far lane first, then drive back toward Mexico.
                Vector3 farLane = new Vector3(car.transform.position.x, exitLeft.position.y, car.transform.position.z);
                yield return MoveTo(car.transform, farLane, carSpeed * 0.5f);
                yield return MoveTo(car.transform, exitLeft.position, carSpeed);
                break;

            case Decision.Imprison:
                // The driver stays in the booth. The car gets towed.
                yield return TowAway(car);
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
        yield return MoveTo(driver, car.SeatPosition, walkSpeed);
        driver.gameObject.SetActive(false);
    }

    IEnumerator TowAway(CarView car)
    {
        towTruck.position = towTruckHome;
        SetFacingLeft(towTruck, true);
        towTruck.gameObject.SetActive(true);

        // Drive in from the right and stop just in front of the car.
        Vector3 hitch = car.transform.position + Vector3.right * hitchDistance;
        yield return MoveTo(towTruck, hitch, carSpeed);

        // Turn around, hook the car, and haul it off.
        yield return new WaitForSeconds(0.3f);
        SetFacingLeft(towTruck, false);
        yield return new WaitForSeconds(0.3f);
        car.transform.SetParent(towTruck, true);
        yield return MoveTo(towTruck, exitRight.position + Vector3.right * hitchDistance, carSpeed * 0.6f);

        car.transform.SetParent(null, true);
        towTruck.gameObject.SetActive(false);
    }

    Vector3 QueueSlot(int index) => queueFront.position + Vector3.left * (queueSpacing * index);

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
