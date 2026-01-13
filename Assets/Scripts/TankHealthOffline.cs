using UnityEngine;

public class TankHealthOffline : MonoBehaviour
{
    public float maxHealth = 100f;
    public float armour = 0f;

    public float Health { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Health = maxHealth;    
    }

    public void TakeDamage(float rawDamage)
    {
        float finalDamage = ComputeDamage(rawDamage, armour);
        Health = Mathf.Max(0f, Health - finalDamage);

        if (Health <= 0f)
        {
            Die();
        }
    }

    private static float ComputeDamage(float damage, float armour)
    {
        float multiplier = 100f / (100f + Mathf.Max(0f, armour));
        return Mathf.Max(1f, damage * multiplier);
    }

    private void Die()
    {
        gameObject.SetActive(false);
    }
}
