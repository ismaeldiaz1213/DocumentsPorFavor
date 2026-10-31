using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// TEMPORARY. Plays one day in the top half so we can tune the flow before
// the desk exists. Press A / R / I to decide. Delete once Lesson 4 adds buttons.
public class StageTester : MonoBehaviour
{
    [SerializeField] CheckpointStage stage;
    [SerializeField] DayData day;

    // Start can be a coroutine: Unity runs it step by step across frames.
    IEnumerator Start()
    {
        stage.OpenLane(day.travelers);

        for (int i = 0; i < day.travelers.Count; i++)
        {
            yield return stage.PullUpToScanner();

            // The driver walks in WHILE the car is being scanned.
            Coroutine scan = StartCoroutine(stage.ScanCar());
            yield return stage.DriverWalksToBooth();
            yield return scan;

            Debug.Log($"{stage.CurrentCar.Traveler.name}: press A = Approve, R = Reject, I = Imprison");
            Decision? decision = null;
            while (decision == null)
            {
                yield return null;
                decision = ReadDecisionKey();
            }

            yield return stage.SendAway(decision.Value);
        }

        Debug.Log("Day over!");
    }

    static Decision? ReadDecisionKey()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return null;
        if (keyboard.aKey.wasPressedThisFrame) return Decision.Approve;
        if (keyboard.rKey.wasPressedThisFrame) return Decision.Reject;
        if (keyboard.iKey.wasPressedThisFrame) return Decision.Imprison;
        return null;
    }
}
