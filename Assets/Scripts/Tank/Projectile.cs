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

    public void SetInitialDataServer(float dmg, float spd, ulong shooter)
    {
        if (!IsServer) return;
        damage = dmg;
        speed = spd;
        shooterClientId = shooter;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            deathTime = Time.time + lifeSeconds;
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;
        transform.position += transform.forward * (speed * Time.fixedDeltaTime);

        if (Time.time >= deathTime)
            NetworkObject.Despawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        TankHealth tank = other.GetComponentInParent<TankHealth>();
        if (tank != null)
        {
            tank.ApplyDamageServer(damage, shooterClientId);
        }
        NetworkObject.Despawn();
    }
}