using UnityEngine; 

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

    [Header("Zone Modifiers")]
    public float mudSpeedMult = 0.7f;
    public float mudAccelMult = 0.4f;
    public float mudBrakeMult = 1.15f;
    public float mudTurnMult = 0.85f;

    private int waterCount;
    private bool InMud => waterCount > 0;
    private bool lastInMud;

    public void EnterMud() { waterCount++; }
    public void ExitMud() { waterCount = Mathf.Max(0, waterCount - 1); }

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
        lastInMud = InMud;
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

        float speedMult = InMud ? mudSpeedMult : 1f;
        float accelMult = InMud ? mudAccelMult : 1f;
        float brakeMult = InMud ? mudBrakeMult : 1f;
        float turnMult = InMud ? mudTurnMult : 1f;

        float effectiveMaxSpeed = maxSpeed * speedMult;
        float effectiveBrake = brake * brakeMult;
        float effectiveTurnSpeedDeg = turnSpeedDeg * turnMult;

        int turn = 0;
        if (move.x > 0.5f) turn = 1;
        else if (move.x < -0.5f) turn = -1;

        float yaw = turn * effectiveTurnSpeedDeg * dt;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yaw, 0f));

        int throttle = 0;
        if (move.y > 0.5f) throttle = 1;
        else if (move.y < -0.5f) throttle = -1;

        bool forwardHeld = throttle == 1;
        bool canStartBoost = boostHeld && forwardHeld && Mathf.Abs(currentSpeed) <= maxSpeedToBoost;

        if (InMud != lastInMud && profile != ProfileType.None)
        {
            RetargetActiveProfile(now, effectiveMaxSpeed, accelMult);
            lastInMud = InMud;
        }

        if (profile == ProfileType.Boost && (!boostHeld || !forwardHeld))
        {
            StartForwardAccel(now, effectiveMaxSpeed, accelMult);
        }

        if (profile != ProfileType.Boost && canStartBoost)
        {
            StartBoost(now, effectiveMaxSpeed);
        }

        if (profile != ProfileType.Boost)
        {
            if (throttle == 0)
            {
                profile = ProfileType.None;
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, effectiveBrake * dt);
            }
            else if (throttle > 0)
            {
                if (profile != ProfileType.ForwardAccel)
                    StartForwardAccel(now, effectiveMaxSpeed, accelMult);
            }
            else
            {
                if (profile != ProfileType.ReverseAccel)
                    StartReverseAccel(now, effectiveMaxSpeed, accelMult);
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
                StartForwardAccel(now, effectiveMaxSpeed, accelMult);
                profileStartSpeed = currentSpeed;
            }
        }

        Vector3 forward = rb.rotation * Vector3.forward;
        Vector3 v = forward * currentSpeed;
        rb.linearVelocity = new Vector3(v.x, rb.linearVelocity.y, v.z);
    }

    private void RetargetActiveProfile(float now, float effectiveMaxSpeed, float accelMult)
    {
        switch (profile)
        {
            case ProfileType.ForwardAccel:
                StartForwardAccel(now, effectiveMaxSpeed, accelMult ); 
                break;
            case ProfileType.ReverseAccel:
                StartReverseAccel(now, effectiveMaxSpeed, accelMult );
                break;
            case ProfileType.Boost:
                StartBoost(now, effectiveMaxSpeed);
                break;
        }
    }

    private void StartForwardAccel(float now, float effectiveMaxSpeed, float accelMult)
    {
        profile = ProfileType.ForwardAccel;
        profileStartTime = now;

        profileDuration = Mathf.Max(0.01f, accelDuration / Mathf.Max(0.01f, accelMult));
        profileCurve = accelerationCurve;

        profileStartSpeed = currentSpeed;
        profileTargetSpeed = effectiveMaxSpeed;
    }

    private void StartReverseAccel(float now, float effectiveMaxSpeed, float accelMult)
    {
        profile = ProfileType.ReverseAccel;
        profileStartTime = now;

        profileDuration = Mathf.Max(0.01f, accelDuration / Mathf.Max(0.01f, accelMult));
        profileCurve = accelerationCurve;

        profileStartSpeed = currentSpeed;
        profileTargetSpeed = -effectiveMaxSpeed;
    }

    private void StartBoost(float now, float effectiveMaxSpeed)
    {
        profile = ProfileType.Boost;
        profileStartTime = now;

        profileDuration = Mathf.Max(0.01f, boostDuration);
        profileCurve = boostAccelerationCurve;

        profileStartSpeed = currentSpeed;
        profileTargetSpeed = effectiveMaxSpeed;
    }
}
