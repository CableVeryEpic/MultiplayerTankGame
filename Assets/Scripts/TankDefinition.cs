using UnityEngine;

[CreateAssetMenu(menuName = "Tanks/TankDefinition")]
public class TankDefinition : ScriptableObject
{
    [Header("Identity")]
    public int id;
    public string displayName;

    [Header("Movement")]
    public float moveSpeed;
    public float turnSpeed;

    [Header("Combat")]
    public float damage;
    public float fireRate;
    public float reloadTime;
    public int magSize;
    public int ammoCapacity;
    public float projectileSpeed;
    public float turretTurnSpeed;

    [Header("Defense")]
    public float maxHealth;
    public float armour;

    [Header("Prefabs/VFX")]
    public GameObject projectilePrefab;
}
