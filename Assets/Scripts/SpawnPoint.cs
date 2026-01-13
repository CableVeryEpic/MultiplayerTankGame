using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private float gizmoRadius = 0.5f;

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.5f);
    }
}
