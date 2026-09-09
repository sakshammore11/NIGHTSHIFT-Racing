using UnityEngine;
using System.Collections;

/// <summary>
/// NIGHTSHIFT — Ultra High-Speed Arcade Vehicle Controller
/// Hyper-fast 10x acceleration, silky smooth physics, and neon underglow graphics.
/// Controls: W/S=Throttle/Brake, A/D=Steer, Space=Handbrake, R=Reset
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider wheelFL;
    public WheelCollider wheelFR;
    public WheelCollider wheelRL;
    public WheelCollider wheelRR;

    [Header("Wheel Meshes")]
    public Transform meshFL;
    public Transform meshFR;
    public Transform meshRL;
    public Transform meshRR;

    [Header("Hyper Drive Settings")]
    [Tooltip("Maximum motor torque applied to drive wheels")]
    public float motorTorque = 12000f;
    [Tooltip("Maximum brake torque")]
    public float brakeTorque = 8000f;
    [Tooltip("Maximum steering angle in degrees")]
    public float maxSteerAngle = 38f;
    [Tooltip("Top speed in km/h")]
    public float maxSpeedKMH = 500f;
    [Tooltip("Handbrake torque for rear wheels")]
    public float handbrakeTorque = 10000f;
    [Tooltip("How fast steering returns to center")]
    public float steerReturnSpeed = 5f;

    [Header("Physics & Downforce")]
    [Tooltip("Center of mass Y offset — lower = more stable at 400+ km/h")]
    public float centerOfMassY = -0.6f;
    [Tooltip("Downforce multiplier at speed")]
    public float downforce = 120f;

    [Header("Spawn")]
    public Vector3 spawnPosition = new Vector3(0f, 0.5f, 0f);
    public Quaternion spawnRotation = Quaternion.identity;

    [Header("Brake Lights")]
    public Renderer[] brakeLightRenderers;
    public Material brakeLightOn;
    public Material brakeLightOff;

    // Public state (read by other scripts)
    [HideInInspector] public float currentSpeedKMH;
    [HideInInspector] public bool isHandbraking;
    [HideInInspector] public bool isBraking;
    [HideInInspector] public bool inputEnabled = true;

    private Rigidbody rb;
    private Transform visualModel;
    private float throttleInput;
    private float steerInput;
    private float brakeInput;
    private float handbrakeInput;
    private float currentSteerAngle;
    private Vector3 originalSpawnPos;
    private Quaternion originalSpawnRot;
    private Light underglowLight;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.None;
            rb.centerOfMass = new Vector3(0f, centerOfMassY, 0f);
            rb.mass = 1400f;
            rb.linearDamping = 0.01f;
            rb.angularDamping = 1.2f;
        }

        // Auto-assign WheelColliders if missing from Inspector
        WheelCollider[] wheels = GetComponentsInChildren<WheelCollider>();
        if (wheels != null && wheels.Length >= 4)
        {
            if (wheelFL == null) wheelFL = wheels[0];
            if (wheelFR == null) wheelFR = wheels[1];
            if (wheelRL == null) wheelRL = wheels[2];
            if (wheelRR == null) wheelRR = wheels[3];
        }

        originalSpawnPos = transform.position;
        originalSpawnRot = transform.rotation;
        inputEnabled = true;
    }

    void Start()
    {
        ConfigureWheels();
        SetupNeonUnderglow();
        inputEnabled = true;
    }

    void SetupNeonUnderglow()
    {
        // Add smooth cyan underglow light beneath the chassis for neon night look
        Transform ug = transform.Find("NeonUnderglow");
        if (ug == null)
        {
            GameObject ugObj = new GameObject("NeonUnderglow");
            ugObj.transform.SetParent(transform);
            ugObj.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            underglowLight = ugObj.AddComponent<Light>();
            underglowLight.type = LightType.Point;
            underglowLight.color = new Color(0f, 0.85f, 1f); // Neon Cyan
            underglowLight.intensity = 4.0f;
            underglowLight.range = 8.0f;
        }
    }

    void ConfigureWheels()
    {
        WheelCollider[] wheels = { wheelFL, wheelFR, wheelRL, wheelRR };
        foreach (var w in wheels)
        {
            if (w == null) continue;
            JointSpring suspension = w.suspensionSpring;
            suspension.spring = 45000f;
            suspension.damper = 5500f;
            suspension.targetPosition = 0.5f;
            w.suspensionSpring = suspension;
            w.suspensionDistance = 0.18f;

            WheelFrictionCurve fwdFriction = w.forwardFriction;
            fwdFriction.stiffness = 2.2f;
            w.forwardFriction = fwdFriction;

            WheelFrictionCurve sideFriction = w.sidewaysFriction;
            sideFriction.stiffness = 2.2f;
            w.sidewaysFriction = sideFriction;
        }
    }

    void Update()
    {
        // Out of bounds / void safety check: reset if falling into void or going far off-road
        if (transform.position.y < -5f || Mathf.Abs(transform.position.x) > 55f)
        {
            Debug.LogWarning("[NIGHTSHIFT] Car out of bounds! Performing Smart Highway Respawn.");
            ResetCar();
            return;
        }

        GatherInput();

        if (Input.GetKeyDown(KeyCode.R))
            ResetCar();
    }

    void GatherInput()
    {
        throttleInput = 0f;
        brakeInput = 0f;

        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");

        // Direct Key Fallback for W, S, A, D and Arrow Keys
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            vertical = 1f;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            vertical = -1f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            horizontal = -1f;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            horizontal = 1f;

        handbrakeInput = Input.GetKey(KeyCode.Space) ? 1f : 0f;

        if (vertical > 0f)
        {
            throttleInput = vertical;
            brakeInput = 0f;
        }
        else if (vertical < 0f)
        {
            if (currentSpeedKMH > 2f)
            {
                brakeInput = Mathf.Abs(vertical);
                throttleInput = 0f;
            }
            else
            {
                throttleInput = vertical; // Reverse gear
                brakeInput = 0f;
            }
        }

        steerInput = horizontal;
        isBraking = brakeInput > 0.1f || handbrakeInput > 0f;
        isHandbraking = handbrakeInput > 0f;
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        currentSpeedKMH = rb.linearVelocity.magnitude * 3.6f;

        ApplyMotor();
        ApplySteering();
        ApplyBrakes();
        ApplyDownforce();
        SyncWheelMeshes();
        UpdateBrakeLights();
    }

    void ApplyMotor()
    {
        // 10x Physical Hyper Acceleration (Rapid response on W / Up Arrow)
        if (Mathf.Abs(throttleInput) > 0.05f && currentSpeedKMH < maxSpeedKMH)
        {
            Vector3 accelForce = transform.forward * (throttleInput * 180f);
            rb.AddForce(accelForce, ForceMode.Acceleration);
        }

        // 4WD High Torque Motor
        float torque = throttleInput * motorTorque;
        if (wheelRL != null) wheelRL.motorTorque = torque;
        if (wheelRR != null) wheelRR.motorTorque = torque;
        if (wheelFL != null) wheelFL.motorTorque = torque * 0.7f;
        if (wheelFR != null) wheelFR.motorTorque = torque * 0.7f;
    }

    void ApplySteering()
    {
        // Smooth speed-sensitive steering
        float speedFactor = Mathf.Clamp01(currentSpeedKMH / maxSpeedKMH);
        float effectiveMaxSteer = Mathf.Lerp(maxSteerAngle, maxSteerAngle * 0.45f, speedFactor);
        float targetSteer = steerInput * effectiveMaxSteer;

        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteer, Time.fixedDeltaTime * 12f);

        if (wheelFL != null) wheelFL.steerAngle = currentSteerAngle;
        if (wheelFR != null) wheelFR.steerAngle = currentSteerAngle;

        // Smooth Arcade Yaw Rotation
        if (Mathf.Abs(steerInput) > 0.05f)
        {
            float turnSpeed = Mathf.Lerp(85f, 45f, speedFactor);
            float effectiveTurn = steerInput * turnSpeed * Time.fixedDeltaTime;
            transform.Rotate(0f, effectiveTurn, 0f, Space.World);
        }

        // Smooth Body Roll Lean
        if (visualModel == null) visualModel = transform.Find("VisualModel");
        if (visualModel != null)
        {
            float targetRoll = -steerInput * 5.0f;
            Quaternion currentLocalRot = visualModel.localRotation;
            Quaternion targetLocalRot = Quaternion.Euler(0f, 0f, targetRoll);
            visualModel.localRotation = Quaternion.Slerp(currentLocalRot, targetLocalRot, Time.fixedDeltaTime * 10f);
        }

        if (meshFL != null) meshFL.localRotation = Quaternion.Euler(meshFL.localEulerAngles.x, currentSteerAngle, 0f);
        if (meshFR != null) meshFR.localRotation = Quaternion.Euler(meshFR.localEulerAngles.x, currentSteerAngle, 0f);
    }

    void ApplyBrakes()
    {
        if (isHandbraking)
        {
            if (wheelRL != null) wheelRL.brakeTorque = handbrakeTorque;
            if (wheelRR != null) wheelRR.brakeTorque = handbrakeTorque;
            if (wheelFL != null) wheelFL.brakeTorque = brakeTorque * 0.4f;
            if (wheelFR != null) wheelFR.brakeTorque = brakeTorque * 0.4f;

            if (wheelRL != null)
            {
                WheelFrictionCurve sideFriction = wheelRL.sidewaysFriction;
                sideFriction.stiffness = 0.6f;
                wheelRL.sidewaysFriction = sideFriction;
            }
            if (wheelRR != null)
            {
                WheelFrictionCurve sideFriction = wheelRR.sidewaysFriction;
                sideFriction.stiffness = 0.6f;
                wheelRR.sidewaysFriction = sideFriction;
            }
        }
        else if (brakeInput > 0.1f)
        {
            float brakeTq = brakeInput * brakeTorque;
            if (wheelFL != null) wheelFL.brakeTorque = brakeTq;
            if (wheelFR != null) wheelFR.brakeTorque = brakeTq;
            if (wheelRL != null) wheelRL.brakeTorque = brakeTq;
            if (wheelRR != null) wheelRR.brakeTorque = brakeTq;

            if (wheelRL != null) wheelRL.motorTorque = 0f;
            if (wheelRR != null) wheelRR.motorTorque = 0f;
            if (wheelFL != null) wheelFL.motorTorque = 0f;
            if (wheelFR != null) wheelFR.motorTorque = 0f;
        }
        else
        {
            if (wheelFL != null) wheelFL.brakeTorque = 0f;
            if (wheelFR != null) wheelFR.brakeTorque = 0f;
            if (wheelRL != null) wheelRL.brakeTorque = 0f;
            if (wheelRR != null) wheelRR.brakeTorque = 0f;

            if (wheelRL != null)
            {
                WheelFrictionCurve sideFriction = wheelRL.sidewaysFriction;
                sideFriction.stiffness = 2.2f;
                wheelRL.sidewaysFriction = sideFriction;
            }
            if (wheelRR != null)
            {
                WheelFrictionCurve sideFriction = wheelRR.sidewaysFriction;
                sideFriction.stiffness = 2.2f;
                wheelRR.sidewaysFriction = sideFriction;
            }
        }
    }

    void ApplyDownforce()
    {
        if (rb == null) return;
        rb.AddForce(-transform.up * downforce * rb.linearVelocity.magnitude);
    }

    void SyncWheelMeshes()
    {
        UpdateWheelMesh(wheelFL, meshFL);
        UpdateWheelMesh(wheelFR, meshFR);
        UpdateWheelMesh(wheelRL, meshRL);
        UpdateWheelMesh(wheelRR, meshRR);
    }

    void UpdateWheelMesh(WheelCollider col, Transform mesh)
    {
        if (col == null || mesh == null) return;
        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }

    void UpdateBrakeLights()
    {
        if (brakeLightRenderers == null || brakeLightRenderers.Length == 0) return;
        Material mat = isBraking ? brakeLightOn : brakeLightOff;
        foreach (var r in brakeLightRenderers)
        {
            if (r != null) r.material = mat;
        }
    }

    public void ResetCar()
    {
        float safeZ = Mathf.Max(0f, transform.position.z + 5f);
        Vector3 targetPos = new Vector3(0f, 0.6f, safeZ);

        RaycastHit hit;
        if (Physics.Raycast(new Vector3(0f, 20f, safeZ), Vector3.down, out hit, 50f))
        {
            targetPos = hit.point + Vector3.up * 0.5f;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.forward * 15f; // Fast seamless resume
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = targetPos;
        transform.rotation = Quaternion.identity;
    }

    public void ResetToStart()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        transform.position = originalSpawnPos;
        transform.rotation = originalSpawnRot;
    }
}
