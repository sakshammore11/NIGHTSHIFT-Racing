using UnityEngine;

/// <summary>
/// NIGHTSHIFT — Third-Person Cinematic Camera
/// Smooth follow with look-ahead, speed-based FOV, and collision avoidance.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow Settings")]
    public float distance = 7f;
    public float height = 2.5f;
    public float followSmoothing = 6f;
    public float rotationSmoothing = 5f;

    [Header("Look-Ahead")]
    [Tooltip("How far ahead of the car the camera looks")]
    public float lookAheadDistance = 4f;
    public float lookAheadSmoothing = 4f;

    [Header("FOV")]
    public float baseFOV = 60f;
    public float maxFOV = 78f;
    [Tooltip("Speed (km/h) at which max FOV is reached")]
    public float maxFOVSpeed = 200f;
    public float fovSmoothing = 3f;

    [Header("Collision")]
    public LayerMask collisionLayers;
    public float collisionRadius = 0.3f;

    private Camera cam;
    private CarController carController;
    private Vector3 currentVelocity;
    private Vector3 lookAheadPos;
    private float currentFOV;
    private Vector3 currentDesiredPos;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = GetComponentInChildren<Camera>();
        currentFOV = baseFOV;
    }

    void Start()
    {
        if (target != null)
        {
            carController = target.GetComponent<CarController>();
            // Snap to initial position
            Vector3 startPos = GetDesiredPosition();
            transform.position = startPos;
            transform.LookAt(target.position + Vector3.up * 0.5f);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        UpdatePosition();
        UpdateRotation();
        UpdateFOV();
    }

    Vector3 GetDesiredPosition()
    {
        // Camera stays behind the car based on car's rotation
        Vector3 behind = target.position
            - target.forward * distance
            + Vector3.up * height;
        return behind;
    }

    void UpdatePosition()
    {
        Vector3 desiredPos = GetDesiredPosition();

        // Collision check: push camera forward if blocked by environment (ignoring the car itself)
        Vector3 dirToCamera = (desiredPos - target.position).normalized;
        float desiredDist = Vector3.Distance(target.position, desiredPos);

        if (Physics.SphereCast(target.position, collisionRadius, dirToCamera,
            out RaycastHit hit, desiredDist, collisionLayers))
        {
            // Ignore the target car and all its child objects/wheels
            if (hit.transform != target && !hit.transform.IsChildOf(target))
            {
                desiredPos = hit.point - dirToCamera * collisionRadius;
            }
        }

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPos, ref currentVelocity,
            1f / followSmoothing, Mathf.Infinity, Time.deltaTime);
    }

    void UpdateRotation()
    {
        if (carController != null)
        {
            // Look-ahead: offset look target based on car velocity direction
            float speed = carController.currentSpeedKMH;
            Vector3 velocityDir = target.forward;
            Vector3 targetLookAhead = target.position
                + velocityDir * Mathf.Clamp(speed / 30f, 0f, lookAheadDistance)
                + Vector3.up * 0.5f;

            lookAheadPos = Vector3.Lerp(lookAheadPos, targetLookAhead,
                Time.deltaTime * lookAheadSmoothing);
        }
        else
        {
            lookAheadPos = target.position + Vector3.up * 0.5f;
        }

        // Smooth look at
        Quaternion targetRot = Quaternion.LookRotation(
            (lookAheadPos - transform.position).normalized);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRot,
            Time.deltaTime * rotationSmoothing);
    }

    void UpdateFOV()
    {
        if (cam == null || carController == null) return;

        float speedNorm = Mathf.Clamp01(carController.currentSpeedKMH / maxFOVSpeed);
        float targetFOV = Mathf.Lerp(baseFOV, maxFOV, speedNorm);
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * fovSmoothing);
        cam.fieldOfView = currentFOV;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        carController = target?.GetComponent<CarController>();
    }
}
