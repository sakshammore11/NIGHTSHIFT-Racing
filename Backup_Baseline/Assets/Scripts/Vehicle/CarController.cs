using UnityEngine;
using System.Collections;

/// <summary>
/// NIGHTSHIFT — Arcade Vehicle Controller
/// Uses Unity WheelCollider for reliable physics.
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

    [Header("Drive Settings")]
    [Tooltip("Maximum motor torque applied to drive wheels")]
    public float motorTorque = 2000f;
    [Tooltip("Maximum brake torque")]
    public float brakeTorque = 4000f;
    [Tooltip("Maximum steering angle in degrees")]
    public float maxSteerAngle = 35f;
    [Tooltip("Top speed in km/h")]
    public float maxSpeedKMH = 200f;
    [Tooltip("Handbrake torque for rear wheels")]
    public float handbrakeTorque = 6000f;
    [Tooltip("How fast steering returns to center")]
    public float steerReturnSpeed = 3f;

    [Header("Physics")]
    [Tooltip("Center of mass Y offset — lower = more stable")]
    public float centerOfMassY = -0.5f;
    [Tooltip("Downforce multiplier at speed")]
    public float downforce = 50f;

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
    private float throttleInput;
    private float steerInput;
    private float brakeInput;
    private float handbrakeInput;
    private float currentSteerAngle;
    private Vector3 originalSpawnPos;
    private Quaternion originalSpawnRot;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.None;
            rb.centerOfMass = new Vector3(0f, centerOfMassY, 0f);
            rb.mass = 1200f;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.5f;
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
        inputEnabled = true;
    }

    void ConfigureWheels()
    {
        // Configure suspension for all wheels
        WheelCollider[] wheels = { wheelFL, wheelFR, wheelRL, wheelRR };
        foreach (var w in wheels)
        {
            if (w == null) continue;
            JointSpring suspension = w.suspensionSpring;
            suspension.spring = 35000f;
            suspension.damper = 4500f;
            suspension.targetPosition = 0.5f;
            w.suspensionSpring = suspension;
            w.suspensionDistance = 0.2f;

            WheelFrictionCurve fwdFriction = w.forwardFriction;
            fwdFriction.stiffness = 1.8f;
            w.forwardFriction = fwdFriction;

            WheelFrictionCurve sideFriction = w.sidewaysFriction;
            sideFriction.stiffness = 1.8f;
            w.sidewaysFriction = sideFriction;
        }
    }

    void Update()
    {
        // Out of bounds / void safety check: reset if falling into void or glitching
        if (transform.position.y < -10f)
        {
            Debug.LogWarning("[NIGHTSHIFT] Car fell into void! Auto-resetting to track.");
            ResetCar();
            return;
        }

        // Always gather input so driving controls are immediately active
        GatherInput();

        // Reset car
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
        // Direct Physical Arcade Acceleration (Guarantees movement on W / Up Arrow)
        if (Mathf.Abs(throttleInput) > 0.05f && currentSpeedKMH < maxSpeedKMH)
        {
            Vector3 accelForce = transform.forward * (throttleInput * 25f);
            rb.AddForce(accelForce, ForceMode.Acceleration);
        }

        // 4-Wheel Drive Motor Torque
        float torque = throttleInput * motorTorque;
        if (wheelRL != null) wheelRL.motorTorque = torque;
        if (wheelRR != null) wheelRR.motorTorque = torque;
        if (wheelFL != null) wheelFL.motorTorque = torque * 0.5f;
        if (wheelFR != null) wheelFR.motorTorque = torque * 0.5f;
    }

    void ApplySteering()
    {
        // Speed-sensitive steering
        float speedFactor = Mathf.Clamp01(currentSpeedKMH / maxSpeedKMH);
        float effectiveMaxSteer = Mathf.Lerp(maxSteerAngle, maxSteerAngle * 0.5f, speedFactor);
        float targetSteer = steerInput * effectiveMaxSteer;

        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteer, Time.fixedDeltaTime * 10f);

        // 1. WheelCollider Steering
        if (wheelFL != null) wheelFL.steerAngle = currentSteerAngle;
        if (wheelFR != null) wheelFR.steerAngle = currentSteerAngle;

        // 2. Direct Arcade Yaw Rotation (Guarantees 100% visible turning on A/D & Left/Right keys)
        if (Mathf.Abs(steerInput) > 0.05f)
        {
            float turnSpeed = 70f; // degrees per second
            // Allow turning even at low speeds so player is never stuck
            float effectiveTurn = steerInput * turnSpeed * Time.fixedDeltaTime;
            transform.Rotate(0f, effectiveTurn, 0f, Space.World);
        }

        // 3. Visual Wheel Mesh Alignment
        if (meshFL != null) meshFL.localRotation = Quaternion.Euler(meshFL.localEulerAngles.x, currentSteerAngle, 0f);
        if (meshFR != null) meshFR.localRotation = Quaternion.Euler(meshFR.localEulerAngles.x, currentSteerAngle, 0f);
    }

    void ApplyBrakes()
    {
        if (isHandbraking)
        {
            // Handbrake: lock rear wheels
            if (wheelRL != null) wheelRL.brakeTorque = handbrakeTorque;
            if (wheelRR != null) wheelRR.brakeTorque = handbrakeTorque;
            if (wheelFL != null) wheelFL.brakeTorque = brakeTorque * 0.3f;
            if (wheelFR != null) wheelFR.brakeTorque = brakeTorque * 0.3f;

            // Reduce rear lateral friction for drifting
            if (wheelRL != null)
            {
                WheelFrictionCurve sideFriction = wheelRL.sidewaysFriction;
                sideFriction.stiffness = 0.5f;
                wheelRL.sidewaysFriction = sideFriction;
            }
            if (wheelRR != null)
            {
                WheelFrictionCurve sideFriction = wheelRR.sidewaysFriction;
                sideFriction.stiffness = 0.5f;
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

            // Zero motor torque when braking
            if (wheelRL != null) wheelRL.motorTorque = 0f;
            if (wheelRR != null) wheelRR.motorTorque = 0f;
            if (wheelFL != null) wheelFL.motorTorque = 0f;
            if (wheelFR != null) wheelFR.motorTorque = 0f;
        }
        else
        {
            // Fully release all brakes!
            if (wheelFL != null) wheelFL.brakeTorque = 0f;
            if (wheelFR != null) wheelFR.brakeTorque = 0f;
            if (wheelRL != null) wheelRL.brakeTorque = 0f;
            if (wheelRR != null) wheelRR.brakeTorque = 0f;

            // Restore lateral friction
            if (wheelRL != null)
            {
                WheelFrictionCurve sideFriction = wheelRL.sidewaysFriction;
                sideFriction.stiffness = 1.8f;
                wheelRL.sidewaysFriction = sideFriction;
            }
            if (wheelRR != null)
            {
                WheelFrictionCurve sideFriction = wheelRR.sidewaysFriction;
                sideFriction.stiffness = 1.8f;
                wheelRR.sidewaysFriction = sideFriction;
            }
        }
    }

    void ApplyDownforce()
    {
        // Push car down at speed to improve grip
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
        // Find safe reset position slightly above road
        RaycastHit hit;
        Vector3 targetSpawn = spawnPosition != Vector3.zero ? spawnPosition : new Vector3(0f, 0.6f, 0f);
        Vector3 resetPos = targetSpawn;

        if (Physics.Raycast(targetSpawn + Vector3.up * 20f, Vector3.down, out hit, 50f))
        {
            resetPos = hit.point + Vector3.up * 0.5f;
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = resetPos;
        transform.rotation = spawnRotation != Quaternion.identity ? spawnRotation : Quaternion.identity;
    }

    public void ResetToStart()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = originalSpawnPos;
        transform.rotation = originalSpawnRot;
    }
}
