using UnityEngine;
using Unity.Netcode;

public class TankAimVisuals : NetworkBehaviour
{

    [SerializeField] private LineRenderer aimLine;
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject aimMarkerPrefab;
    [SerializeField] private TankAimInput aimInput;

    private GameObject aimMarker;

    private Camera cam;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        if (aimInput == null) aimInput = GetComponent<TankAimInput>();
        if (aimMarker == null && aimMarkerPrefab != null) aimMarker = Instantiate(aimMarkerPrefab);

    }

    private void LateUpdate()
    {
        if (!IsOwner) return;
        if (InputManager.Instance == null) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        if (muzzle == null) return;

        bool hasGroundHit = Physics.Raycast(cam.ScreenPointToRay(InputManager.Instance.Look), out RaycastHit groundHit, aimInput.maxDistance, aimInput.aimMask, QueryTriggerInteraction.Ignore);

        if (!hasGroundHit)
        {
            if (aimMarker != null) aimMarker.gameObject.SetActive(false);
            if (aimLine != null) aimLine.enabled = false;
            return;
        }

        Vector3 markerPoint = groundHit.point;

        if (aimMarker != null)
        {
            aimMarker.gameObject.SetActive(true);
            aimMarker.transform.position = markerPoint;
        }

        float maxLineDistance = Vector3.Distance(muzzle.position, markerPoint);

        Vector3 start = muzzle.position;
        Vector3 dir = muzzle.forward;

        Vector3 end = start + dir * maxLineDistance;

        if (Physics.Raycast(start, dir, out RaycastHit muzzleHit, maxLineDistance, aimInput.aimMask, QueryTriggerInteraction.Ignore))
        {
            end = muzzleHit.point;
        }

        if (aimLine != null)
        {
            aimLine.enabled = true;
            aimLine.SetPosition(0, start);
            aimLine.SetPosition(1, end);
        }
    }
}
