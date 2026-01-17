using UnityEngine;
using UnityEngine.InputSystem;

public class TankWeaponOffline : MonoBehaviour
{
    [SerializeField] private Transform muzzle;
    [SerializeField] private ProjectileOffline projectilePrefab;

    public float fireRate = 3f;
    public float damage = 25f;
    public float projectileSpeed = 18f;
    public int ammoCapacity = 32;
    public int magCapacity = 1;
    public float reloadTime = 2f;

    private int currentAmmo;
    private int ammoLoaded;

    private bool reloading;
    private float reloadEndTime;

    private float nextFireTime;

    private void Awake()
    {
        currentAmmo = ammoCapacity - magCapacity;
        ammoLoaded = magCapacity;
    }

    // Update is called once per frame
    void Update()
    {
        if (InputManager.Instance == null) return;

        if (InputManager.Instance != null && InputManager.Instance.ShootHeld)
        {
            TryFire();
        }

        if (reloading && Time.time >= reloadEndTime)
        {
            FinishReload();
        }

        if (InputManager.Instance.ReloadPressedThisFrame)
            TryStartReload();


        if (InputManager.Instance.ShootHeld)
        {
            TryFire();
        }
    }

    private void TryFire()
    {
        if (reloading) return;
        if (Time.time < nextFireTime) return;
        if (muzzle == null || projectilePrefab == null) return;

        if (ammoLoaded <= 0)
        {
            TryStartReload();
            return;
        }

        nextFireTime = Time.time + 1f / Mathf.Max(0.01f, fireRate);
        ammoLoaded--;

        var projectile = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);
        projectile.Init(damage, projectileSpeed, gameObject);

        if (ammoLoaded <= 0)
        {
            TryStartReload();
        } 
    }

    private void TryStartReload()
    {
        if (reloading) return;
        if (currentAmmo <= 0) return;
        if (ammoLoaded >= magCapacity) return;
        reloading = true;
        reloadEndTime = Time.time + reloadTime;
    }

    private void FinishReload()
    {
        reloading = false;
        int neededAmmo = magCapacity - ammoLoaded;
        int ammoToLoad = Mathf.Min(neededAmmo, currentAmmo);
        ammoLoaded += ammoToLoad;
        currentAmmo -= ammoToLoad;
    }

    public int AmmoLoaded => ammoLoaded;
    public int CurrentAmmo => currentAmmo;
    public bool IsReloading => reloading;
    public float ReloadRemaining => reloading ? Mathf.Max(0f, reloadEndTime - Time.time) : 0f;
}
