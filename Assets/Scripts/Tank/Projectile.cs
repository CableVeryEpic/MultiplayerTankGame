using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Projectile : NetworkBehaviour
{
    private float damage;
    private float speed;
    private ulong shooterClientId;

    [SerializeField] private float lifeSeconds = 10f;
    private float deathTime;

    [Header("Explosion")]
    [SerializeField] private bool explodeOnHit = true;
    [SerializeField] private float explosionRadius = 4f;
    [SerializeField] private float explosionForce = 600f;
    [SerializeField] private float upwardModifier = 0.5f;
    [SerializeField] private LayerMask explosionMask = ~0;
    [SerializeField] private AnimationCurve falloff = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private Rigidbody rb;

    public void SetInitialDataServer(float dmg, float spd, ulong shooter)
    {
        damage = dmg;
        speed = spd;
        shooterClientId = shooter;
    }

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();

        if (IsServer)
        {
            deathTime = Time.time + lifeSeconds;

            rb.linearVelocity = transform.forward * speed;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.None;
        }
        else
        {
            rb.isKinematic = true;
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        if (Time.time >= deathTime)
            NetworkObject.Despawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        NetworkObject tank = other.GetComponentInParent<NetworkObject>();
        if (tank != null && tank.OwnerClientId == shooterClientId)
            return;

        TankHealth health = other.GetComponentInParent<TankHealth>();
        if (health != null)
            health.ApplyDamageServer(damage, shooterClientId);

        Vector3 hitPoint = transform.position;
        try { hitPoint = other.ClosestPoint(transform.position); } catch { }

        if (explodeOnHit)
            ExplodeServer(hitPoint);

        NetworkObject.Despawn();
    }

    private void ExplodeServer(Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(center, explosionRadius, explosionMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            TreePhysics tree = hits[i].GetComponent<TreePhysics>();
            if (tree == null) tree = hits[i].GetComponentInParent<TreePhysics>();
            if (tree == null) continue;

            Vector3 closest = center;
            try { closest = hits[i].ClosestPoint(center); } catch { closest = hits[i].transform.position; }

            Vector3 dir = (closest - center);
            float dist = dir.magnitude;

            if (dist < 0.0001f) dir = Random.onUnitSphere;
            else dir /= dist;

            float t = Mathf.Clamp01(dist / explosionRadius);
            float strength = explosionForce * Mathf.Clamp01(falloff.Evaluate(t));
            tree.ApplyExplosion(center, strength, explosionRadius, upwardModifier);
        }
    }
}