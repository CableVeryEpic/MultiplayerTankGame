using System.Collections.Specialized;
using UnityEngine;

public class ProjectileOffline : MonoBehaviour
{
    private float damage;
    private float speed;
    private GameObject owner;

    [Header("Explosion")]
    [SerializeField] private bool explodeOnHit = true;
    [SerializeField] private float explosionRadius = 4f;
    [SerializeField] private float explosionForce = 600f;
    [SerializeField] private float upwardModifier = 0.5f;
    [SerializeField] private LayerMask explosionMask = ~0;
    [SerializeField] private AnimationCurve falloff = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private Rigidbody rb;

    public void Init(float damage, float speed, GameObject owner)
    {
        this.damage = damage;
        this.speed = speed;
        this.owner = owner;

        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        rb.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner != null && other.GetComponentInParent<Transform>() == owner.transform)
        {
            return;
        }

        var health = other.GetComponentInParent<TankHealthOffline>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }

        if (explodeOnHit)
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Explode(hitPoint);
        }
        Destroy(gameObject);
    }

    private void Explode(Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(center, explosionRadius, explosionMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            TreePhysics tree = hits[i].GetComponent<TreePhysics>();
            if (tree == null) tree = hits[i].GetComponentInParent<TreePhysics>();
            if (tree == null) continue;

            Vector3 closest = hits[i].ClosestPoint(center);
            Vector3 dir = (closest - center);
            float dist = dir.magnitude;

            if (dist < 0.0001f)
                dir = Random.onUnitSphere;
            else
                dir /= dist;

            float t = Mathf.Clamp01(dist / explosionRadius);
            float strength = explosionForce * Mathf.Clamp01(falloff.Evaluate(t));
            tree.ApplyExplosion(center, strength, explosionRadius, upwardModifier);
        }
    }
}
