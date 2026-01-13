using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    private float damage;
    private float speed;
    private ulong ownerId;

    public void Init(float damage, float speed, ulong ownerId)
    {
        this.damage = damage;
        this.speed = speed;
        this.ownerId = ownerId;
    }

    void Update()
    {
        if (!IsServer) return;
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        var tank = other.GetComponent<Tank>();
        if (tank != null && tank.OwnerClientId != ownerId)
        {
            tank.TakeDamageServer(damage);
            NetworkObject.Despawn();
        }
    }
}