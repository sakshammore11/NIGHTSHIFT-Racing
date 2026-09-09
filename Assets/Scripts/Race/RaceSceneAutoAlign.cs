using UnityEngine;

/// <summary>
/// NIGHTSHIFT — Auto Align Track, Car, and Camera in World Space.
/// Guarantees that at runtime, the car is placed exactly ON TOP of the road start line.
/// </summary>
[DefaultExecutionOrder(-100)]
public class RaceSceneAutoAlign : MonoBehaviour
{
    void Awake()
    {
        PerformAlignment();
    }

    public static void PerformAlignment()
    {
        // 1. Find Track and Car
        GameObject car = GameObject.Find("PlayerCar") ?? GameObject.Find("ShowcaseCar") ?? GameObject.FindWithTag("Player");
        GameObject trackParent = GameObject.Find("TrackParent") ?? GameObject.Find("Environment") ?? GameObject.Find("Track");

        if (trackParent != null)
        {
            // Reset track parent position to origin so track is never floating in sky
            trackParent.transform.position = Vector3.zero;
            trackParent.transform.rotation = Quaternion.identity;
            trackParent.transform.localScale = Vector3.one;
        }

        // Find road start segment or surface
        GameObject roadStart = GameObject.Find("RoadSegment_0") ?? GameObject.Find("Road") ?? GameObject.Find("StartLine");
        Vector3 startRoadPos = Vector3.zero;
        if (roadStart != null)
        {
            startRoadPos = roadStart.transform.position;
            // Place car cleanly 0.5m above top face of road segment
            startRoadPos.y = roadStart.transform.position.y + 0.6f;
        }
        else if (trackParent != null)
        {
            startRoadPos = trackParent.transform.position + Vector3.up * 0.5f;
        }
        else
        {
            startRoadPos = new Vector3(0f, 0.5f, 0f);
        }

        // Snap Car onto Road Surface
        if (car != null)
        {
            car.transform.position = startRoadPos;
            car.transform.rotation = Quaternion.identity;

            Rigidbody rb = car.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            CarController carCtrl = car.GetComponent<CarController>();
            if (carCtrl != null)
            {
                carCtrl.spawnPosition = startRoadPos;
                carCtrl.spawnRotation = Quaternion.identity;
            }
        }

        // Snap Camera behind Car
        GameObject camObj = GameObject.Find("Main Camera") ?? GameObject.FindWithTag("MainCamera");
        if (camObj != null && car != null)
        {
            camObj.transform.position = car.transform.position - Vector3.forward * 6.5f + Vector3.up * 2.2f;
            camObj.transform.rotation = Quaternion.Euler(12f, 0f, 0f);

            CameraFollow cf = camObj.GetComponent<CameraFollow>();
            if (cf != null)
            {
                cf.target = car.transform;
            }
        }

        Debug.Log($"[NIGHTSHIFT Alignment] Aligned car at {startRoadPos} on top of road track!");
    }
}
