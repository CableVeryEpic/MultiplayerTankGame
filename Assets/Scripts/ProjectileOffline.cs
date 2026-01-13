using UnityEngine;

public class ProjectileOffline : MonoBehaviour
{
    private float damage;
    private float speed;
    private GameObject owner;

    [SerializeField] private float lifeTime = 4f;

    public void Init(float damage, float speed, GameObject owner)
    {
        this.damage = damage;
        this.speed = speed;
        this.owner = owner;

        Destroy(gameObject, lifeTime);
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
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
        Destroy(gameObject);
    }
}
