using System;
using UnityEngine;

public class TankHealthOffline : MonoBehaviour
{
    public event Action<TankHealthOffline> OnDeath;

    public float maxHealth = 100f;
    public float armour = 0f;

    public float Health { get; private set; }
    public bool IsAlive => Health > 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Health = maxHealth;    
    }

    public void ResetHealth()
    {
        Health = maxHealth;
    }

    public void TakeDamage(float rawDamage)
    {
        if (!IsAlive) return;

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
        Debug.Log($"{gameObject.name} Died.");
        OnDeath?.Invoke(this);
    }
}
