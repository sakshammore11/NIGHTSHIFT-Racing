using UnityEngine;

/// <summary>
/// NIGHTSHIFT — Third-Person Hyper Cinematic Camera
/// Smooth follow with high-speed FOV warp expansion, speed pushback, and camera shake.
/// Configures deep night sky background color to guarantee NO green/cyan screen washout!
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow Settings")]
    public float distance = 7.5f;
    public float height = 2.4f;
    public float followSmoothing = 12f;
    public float rotationSmoothing = 10f;

    [Header("Look-Ahead")]
    [Tooltip("How far ahead of the car the camera looks")]
    public float lookAheadDistance = 6f;
    public float lookAheadSmoothing = 6f;

    [Header("Hyper FOV Warp")]
    public float baseFOV = 62f;
    public float maxFOV = 92f;
    [Tooltip("Speed (km/h) at which max FOV is reached")]
    public float maxFOVSpeed = 450f;
    public float fovSmoothing = 4f;

    [Header("Collision")]
    public LayerMask collisionLayers;
    public float collisionRadius = 0.3f;

    private Camera cam;
    private CarController carController;
    private Vector3 currentVelocity;
    private Vector3 lookAheadPos;
    private float currentFOV;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = GetComponentInChildren<Camera>();
        currentFOV = baseFOV;

        // Guarantee crisp deep midnight night sky (fixes any cyan/green camera background issue)
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.06f); // Deep Midnight Sky
            cam.farClipPlane = 1200f;
        }
    }

    void Start()
    {
        if (target != null)
        {
            carController = target.GetComponent<CarController>();
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
        float speed = carController != null ? carController.currentSpeedKMH : 0f;
        float speedPushBack = Mathf.Clamp(speed / 60f, 0f, 2.5f);

        Vector3 behind = target.position
            - target.forward * (distance + speedPushBack)
            + Vector3.up * height;

        // High-speed speed feel camera shake
        if (speed > 180f)
        {
            float shakeIntensity = (speed - 180f) * 0.0008f;
            Vector3 shake = Random.insideUnitSphere * Mathf.Clamp(shakeIntensity, 0f, 0.08f);
            behind += shake;
        }

        return behind;
    }

    void UpdatePosition()
    {
        Vector3 desiredPos = GetDesiredPosition();

        Vector3 dirToCamera = (desiredPos - target.position).normalized;
        float desiredDist = Vector3.Distance(target.position, desiredPos);

        if (Physics.SphereCast(target.position, collisionRadius, dirToCamera,
            out RaycastHit hit, desiredDist, collisionLayers))
        {
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
            float speed = carController.currentSpeedKMH;
            Vector3 velocityDir = target.forward;
            Vector3 targetLookAhead = target.position
                + velocityDir * Mathf.Clamp(speed / 40f, 0f, lookAheadDistance)
                + Vector3.up * 0.5f;

            lookAheadPos = Vector3.Lerp(lookAheadPos, targetLookAhead,
                Time.deltaTime * lookAheadSmoothing);
        }
        else
        {
            lookAheadPos = target.position + Vector3.up * 0.5f;
        }

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
