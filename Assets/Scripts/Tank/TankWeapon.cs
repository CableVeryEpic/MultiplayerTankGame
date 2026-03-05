using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

public class TankWeapon : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private Projectile projectilePrefab;

    [Header("Stats")]
    public float fireRate = 3f;
    public float damage = 25f;
    public float projectileSpeed = 18f;

    [Header("Ammo")]
    public int ammoCapacity = 31;
    public int magCapacity = 1;
    public float reloadTime = 2f;

    [SerializeField] private bool holdToFire = false;

    public NetworkVariable<int> AmmoReserve = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> AmmoLoaded = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> Reloading = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float nextFireTime;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            AmmoReserve.Value = ammoCapacity;
            AmmoLoaded.Value = magCapacity;
            Reloading.Value = false;
            nextFireTime = 0f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner || !IsClient) return;
        if (InputManager.Instance == null) return;

        if (holdToFire)
        {
            if (InputManager.Instance.ShootHeld)
            {
                RequestFireServerRpc();
            }
        } else
        {
            if (InputManager.Instance.ShootPressedThisFrame)
            {
                RequestFireServerRpc();
            }
        }

        // Add Reload logic key call here
    }

    [ServerRpc]
    private void RequestFireServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!CanFire()) return;

        DoFire();
    }

    private bool CanFire()
    {
        if (!IsServer) return false;
        if (muzzle == null || projectilePrefab == null) return false;
        if (Reloading.Value) return false;

        float now = Time.time;
        if (now < nextFireTime) return false;

        if (AmmoLoaded.Value <= 0)
        {
            if (AmmoReserve.Value > 0)
                StartReload();
            return false;
        }

        return true;
    }

    private void DoFire()
    {
        float now = Time.time;
        nextFireTime = now + 1f / Mathf.Max(0.01f, fireRate);
        AmmoLoaded.Value -= 1;

        Projectile proj = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);
        proj.SetInitialDataServer(damage, projectileSpeed, OwnerClientId);
        proj.NetworkObject.Spawn(true);

        if (AmmoLoaded.Value <= 0 && AmmoReserve.Value > 0)
            StartReload();
    }

    private void StartReload()
    {
        if (Reloading.Value) return;
        Reloading.Value = true;
        Invoke(nameof(FinishReload), reloadTime);
    }

    private void FinishReload()
    {
        if (!IsServer) return;

        int need = magCapacity;
        int take = Mathf.Min(need, AmmoReserve.Value);

        AmmoReserve.Value -= take;
        AmmoLoaded.Value += take;

        Reloading.Value = false;
    }
}
