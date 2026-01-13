using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TankMotorOffline : MonoBehaviour
{
    [Header("Movement")]
    public float turnSpeedDeg = 180f;
    public float maxSpeed = 6f;
    public float brake = 30f;

    private Vector2 move;
    [HideInInspector] public float currentSpeed;

    [Header("Acceleration")]
    public AnimationCurve accelerationCurve;
    public float accelDuration = 0.5f;

    [Header("Boost")]
    public AnimationCurve boostAccelerationCurve;
    public float boostDuration = 0.5f;
    public float maxSpeedToBoost = 1.0f;

    private bool boostHeld;

    // Profile Stuff
    public enum ProfileType { None, ForwardAccel, ReverseAccel, Boost }
    [HideInInspector] public ProfileType profile = ProfileType.None;

    private float profileStartTime;
    private float profileStartSpeed;
    private float profileTargetSpeed;
    private float profileDuration;
    private AnimationCurve profileCurve;

    private Rigidbody rb;

    // Extra UI data
    public bool boostAvailable;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        move = InputManager.Instance != null ? InputManager.Instance.Move : Vector2.zero;
        boostHeld = InputManager.Instance != null && InputManager.Instance.BoostHeld;
        boostAvailable = Mathf.Abs(currentSpeed) <= maxSpeedToBoost;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        float now = Time.time;

        int turn = 0;
        if (move.x > 0.5f) turn = 1;
        else if (move.x < -0.5f) turn = -1;

        float yaw = turn * turnSpeedDeg * dt;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yaw, 0f));

        int throttle = 0;
        if (move.y > 0.5f) throttle = 1;
        else if (move.y < -0.5f) throttle = -1;

        bool forwardHeld = throttle == 1;
        bool canStartBoost = boostHeld && forwardHeld && Mathf.Abs(currentSpeed) <= maxSpeedToBoost;

        if (profile == ProfileType.Boost && (!boostHeld || !forwardHeld))
        {
            StartForwardAccel(now);
        }

        if (profile != ProfileType.Boost && canStartBoost)
        {
            StartBoost(now);
        }

        if (profile != ProfileType.Boost)
        {
            if (throttle == 0)
            {
                profile = ProfileType.None;
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brake * dt);
            }
            else if (throttle > 0)
            {
                if (profile != ProfileType.ForwardAccel)
                    StartForwardAccel(now);
            }
            else
            {
                if (profile != ProfileType.ReverseAccel)
                    StartReverseAccel(now);
            }
        }

        if (profileCurve != null && profileDuration > 0f && profile != ProfileType.None)
        {
            float t = Mathf.Clamp01((now - profileStartTime) / profileDuration);
            float f = profileCurve.Evaluate(t);

            currentSpeed = Mathf.LerpUnclamped(profileStartSpeed, profileTargetSpeed, f);

            if (t >= 1f && profile != ProfileType.Boost)
            {
                currentSpeed = profileTargetSpeed;
            }

            if (t >= 1f && profile == ProfileType.Boost)
            {
                StartForwardAccel(now);
                currentSpeed = profileTargetSpeed = currentSpeed;
            }
        }

        Vector3 forward = rb.rotation * Vector3.forward;
        Vector3 v = forward * currentSpeed;
        rb.linearVelocity = new Vector3(v.x, 0f, v.z);
    }

    private void StartForwardAccel(float now)
    {
        profile = ProfileType.ForwardAccel;
        profileStartTime = now;
        profileDuration = Mathf.Max(0.01f, accelDuration);
        profileCurve = accelerationCurve;

        profileStartSpeed = currentSpeed;
        profileTargetSpeed = maxSpeed;
    }

    private void StartReverseAccel(float now)
    {
        profile = ProfileType.ReverseAccel;
        profileStartTime = now;
        profileDuration = Mathf.Max(0.01f, accelDuration);
        profileCurve = accelerationCurve;

        profileStartSpeed = currentSpeed;
        profileTargetSpeed = -maxSpeed;
    }

    private void StartBoost(float now)
    {
        profile = ProfileType.Boost;
        profileStartTime = now;
        profileDuration = Mathf.Max(0.01f, boostDuration);
        profileCurve = boostAccelerationCurve;

        profileStartSpeed = currentSpeed;
        profileTargetSpeed = maxSpeed;
    }
}
