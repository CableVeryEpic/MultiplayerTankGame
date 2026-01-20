using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TreePhysics : MonoBehaviour
{
    public int TreeId { get; private set; }
    public void Init(int id) => TreeId = id;
    [SerializeField] private float linearSpeedToLock = 0.05f;
    [SerializeField] private float angularSpeedToLock = 2f;
    [SerializeField] private float settleTime = 0.75f;

    [SerializeField] private float unlockMinimumTime = 1f;

    private Rigidbody rb;
    private float settleTimer;
    private float unlockedUntil;

    public bool IsLocked => rb.isKinematic;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        Lock();
    }

    // Update is called once per frame
    void Update()
    {
        if (rb.isKinematic) return;

        if (Time.time < unlockedUntil) return;

        float lin2 = rb.linearVelocity.sqrMagnitude;
        float ang2 = rb.angularVelocity.sqrMagnitude;

        float linThresh2 = linearSpeedToLock * linearSpeedToLock;
        float angThresh = angularSpeedToLock * Mathf.Deg2Rad;
        float angThresh2 = angThresh * angThresh;

        if (lin2 <= linThresh2 && ang2 <= angThresh2)
        {
            settleTimer += Time.deltaTime;
            if (settleTimer >= settleTime)
            {
                Lock();
            }
        }
        else
        {
            settleTimer = 0f;
        }
    }

    public void ApplyExplosion(Vector3 center, float force, float radius, float upward = 0f)
    {
        Unlock();

        unlockedUntil = Time.time + unlockMinimumTime;

        rb.AddExplosionForce(force, center, radius, upward, ForceMode.Impulse);
    }

    public void Lock()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        settleTimer = 0f;
    }

    private void Unlock()
    {
        if (!rb.isKinematic) return;
        rb.isKinematic = false;
        rb.WakeUp();
        settleTimer = 0f;
    }
}
