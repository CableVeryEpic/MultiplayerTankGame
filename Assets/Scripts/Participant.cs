using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Participant : MonoBehaviour
{
    [SerializeField] private bool isPlayerControlled = false;
    private TankHealthOffline health;

    [SerializeField] private Behaviour[] disableOnDeath;

    private Rigidbody rb;

    public bool IsAlive => health != null && health.IsAlive;

    void Awake()
    {
        health = GetComponent<TankHealthOffline>();

        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (health != null) health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null) health.OnDeath -= HandleDeath;
    }

    private void HandleDeath(TankHealthOffline _)
    {
        SetControlsEnabled(false);
    }

    public void ResetForRound(SpawnPoint sp)
    {
        if (sp == null) return;

        transform.SetPositionAndRotation(sp.transform.position, sp.transform.rotation);

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        health.ResetHealth();
        SetControlsEnabled(true);
    }

    public void SetControlsEnabled(bool enabled)
    {
        if (disableOnDeath == null) return;

        for (int i = 0; i < disableOnDeath.Length; i++)
        {
            if (disableOnDeath[i] != null)
                disableOnDeath[i].enabled = enabled;
        }
    }
}
