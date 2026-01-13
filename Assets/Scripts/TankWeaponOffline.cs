using UnityEngine;
using UnityEngine.InputSystem;

public class TankWeaponOffline : MonoBehaviour
{
    [SerializeField] private Transform muzzle;
    [SerializeField] private ProjectileOffline projectilePrefab;

    public float fireRate = 3f;
    public float damage = 25f;
    public float projectileSpeed = 18f;

    private float nextFireTime;

    // Update is called once per frame
    void Update()
    {
        if (InputManager.Instance != null && InputManager.Instance.ShootHeld)
        {
            TryFire();
        }
    }

    private void TryFire()
    {
        if (Time.time < nextFireTime) return;
        if (muzzle == null || projectilePrefab == null) return;

        nextFireTime = Time.time + 1f / Mathf.Max(0.01f, fireRate);

        var projectile = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);
        projectile.Init(damage, projectileSpeed, gameObject);
    }
}
