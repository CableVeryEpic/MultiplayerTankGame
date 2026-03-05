using UnityEngine;
using Unity.Netcode;

public class TankAimInput : NetworkBehaviour
{
    [Header("Aim Raycast")]
    [SerializeField] private float aimHeightOffset = 1.0f;

    public LayerMask aimMask = ~0;
    public float maxDistance = 500f;
    public NetworkVariable<Vector3> AimPoint = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private Camera cam;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            cam = Camera.main;
        }
    }

    void Update()
    {
        if (!IsOwner || !IsClient) return;
        if (InputManager.Instance == null) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(InputManager.Instance.Look);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, aimMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 p = hit.point;
            p += Vector3.up * aimHeightOffset;
            AimPoint.Value = p;
        } else
        {
            Plane plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y + aimHeightOffset, 0f));
            if (plane.Raycast(ray, out float enter)) 
                AimPoint.Value = ray.GetPoint(enter);
        }
    }
}
