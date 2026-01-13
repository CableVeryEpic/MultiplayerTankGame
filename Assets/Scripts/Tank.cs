using UnityEngine;
using Unity.Netcode;

public class Tank : NetworkBehaviour
{
    [SerializeField] private TankDatabase database;
    [SerializeField] private TankMotor motor;
    [SerializeField] private Transform turret;
    [SerializeField] private Transform muzzle;

    public NetworkVariable<int> TankId = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> Health = new(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private TankDefinition def;

    private float nextShotTime;
    private int currentAmmo;
    private bool reloading;

    public override void OnNetworkSpawn()
    {
        TankId.OnValueChanged += (_, __) => ApplyDefinition();
    }

    private void ApplyDefinition()
    {
        def = database.GetById(TankId.Value);
        if (def == null) return;

        if (IsServer)
        {
            Health.Value = def.maxHealth;
            currentAmmo = def.magSize;
            reloading = false;
            nextShotTime = 0f;
            motor.SetMoveStats(def.moveSpeed, def.turnSpeed);
        }
    }

    public void TakeDamageServer(float rawDamage)
    {
        if (!IsServer || def == null) return;

        float finalDamage = ComputeDamage(rawDamage, def.armour);
        Health.Value = Mathf.Max(0f, Health.Value - finalDamage);
        if (Health.Value <= 0f)
        {
            // Handle tank destruction (not implemented here)
            NetworkObject.Despawn();
        }
    }

    private static float ComputeDamage(float damage, float armour)
    {
        float multiplier = 100f / (100f + Mathf.Max(0f, armour));
        return Mathf.Max(1f, damage * multiplier);
    }

    [ServerRpc]
    public void FireServerRpc()
    {
        if (def == null) return;
        if (reloading) return;

        float time = Time.time;
        float secondsPerShot = 1f / Mathf.Max(0.0f, def.fireRate);
        if (time < nextShotTime) return;

        if (currentAmmo <= 0)
        {
            StartReload();
            return;
        }
        nextShotTime = time + secondsPerShot;
        currentAmmo--;
        SpawnProjectile();

        if (currentAmmo <= 0)
        {
            StartReload();
        }
    }

    private void StartReload()
    {
        if (!IsServer || reloading || def == null) return;
        reloading = true;
        Invoke(nameof(FinishReload), def.reloadTime);
    }

    private void FinishReload()
    {
        if (!IsServer || def == null) return;
        currentAmmo = def.magSize;
        reloading = false;
    }

    private void SpawnProjectile()
    {
        if (!IsServer || def == null || def.projectilePrefab == null) return;

        var go = Instantiate(def.projectilePrefab, muzzle.position, muzzle.rotation);
        var no = go.GetComponent<NetworkObject>();
        no.Spawn();

        var proj = go.GetComponent<Projectile>();
        proj.Init(def.damage, def.projectileSpeed, OwnerClientId);
    }
}
