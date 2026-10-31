using UnityEngine;

// Goes on the root of the Car prefab. Knows which traveler is driving
// and where the driver sits, so the driver can get out and back in.
public class CarView : MonoBehaviour
{
    [Tooltip("The car's main body sprite. Tinted with the traveler's vehicle color.")]
    [SerializeField] SpriteRenderer body;
    [Tooltip("Glass, wheels and lights on top of the body. Never tinted.")]
    [SerializeField] SpriteRenderer details;
    [Tooltip("The little driver shape. Hidden while they're inside the car or the booth.")]
    [SerializeField] Transform driver;

    Vector3 seatLocalPosition;

    public TravelerData Traveler { get; private set; }
    public Transform Driver => driver;
    public Vector3 SeatPosition => transform.TransformPoint(seatLocalPosition);

    void Awake()
    {
        seatLocalPosition = driver.localPosition;
        driver.gameObject.SetActive(false);
    }

    public void Setup(TravelerData traveler)
    {
        Traveler = traveler;
        body.color = traveler.vehicleColor;
        name = "Car - " + traveler.name;

        // The driver wears their own face and clothes (drawn by PortraitArt).
        Sprite walker = PortraitArt.Body(traveler.face);
        if (walker != null) driver.GetComponent<SpriteRenderer>().sprite = walker;

        // Swap in this traveler's kind of vehicle (sedan, pickup, van...).
        VehicleModel model = traveler.vehicleModel;
        if (model != null && model.bodySprite != null)
        {
            body.sprite = model.bodySprite;
            if (details != null) details.sprite = model.detailSprite;
        }
    }

    // Flip the whole car so it faces left (used when a rejected car turns around).
    public void FaceLeft()
    {
        Vector3 scale = transform.localScale;
        scale.x = -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }
}
