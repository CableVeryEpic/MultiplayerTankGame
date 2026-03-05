using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    private LayerMask tankMask = 1 << 7;
    public int Index {  get; set; }

    [Header("Rules")]
    public float cooldown = 5f;
    public float clearRadius = 4f;

    public float NextAvailableTime { get; private set; }

    public bool OffCooldown(float now) => now >= NextAvailableTime;

    public void MarkUsed(float now)
    {
        NextAvailableTime = now + cooldown;
    }

    public bool IsClear()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, clearRadius, tankMask, QueryTriggerInteraction.Ignore);
        return hits.Length == 0;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, clearRadius);
    }
}
