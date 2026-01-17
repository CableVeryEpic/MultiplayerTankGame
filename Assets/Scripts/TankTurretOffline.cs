using UnityEngine;
using UnityEngine.InputSystem;

public class TankTurretOffline : MonoBehaviour
{
    [SerializeField] private Transform turretPivot;
    [SerializeField] private Transform barrelPivot;
    [SerializeField] private Transform muzzle;

    [SerializeField] private LineRenderer aimLine;

    [SerializeField] private GameObject aimMarkerPrefab;
    private GameObject aimMarker;

    [SerializeField] private LayerMask aimMask = ~0;

    [SerializeField] private float aimLength = 500f;

    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 25f;

    [SerializeField] private float yawSpeed = 0f;
    [SerializeField] private float pitchSpeed = 0f;

    private Camera cam;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        cam = Camera.main;
        aimMarker = Instantiate(aimMarkerPrefab);
    }

    // Update is called once per frame
    void Update()
    {
        if (InputManager.Instance == null) return;
        if (cam == null || turretPivot == null || barrelPivot == null) return;

        Ray ray = cam.ScreenPointToRay(InputManager.Instance.Look);

        if (!Physics.Raycast(ray, out RaycastHit hit, aimLength, aimMask, QueryTriggerInteraction.Ignore)) return;

        Vector3 target = hit.point + Vector3.up * 1f;

        Vector3 dirWorld = target - barrelPivot.position;
        if (dirWorld.sqrMagnitude < 0.001f) return;

        Vector3 dirLocal = transform.InverseTransformDirection(dirWorld);
        dirLocal.y = 0f;
        if (dirLocal.sqrMagnitude < 0.001f) return;

        float yawDegrees = Mathf.Atan2(dirLocal.x, dirLocal.z) * Mathf.Rad2Deg;

        Quaternion targetYawLocal = Quaternion.AngleAxis(yawDegrees, Vector3.up);

        if (yawSpeed > 0f)
        {
            turretPivot.localRotation = Quaternion.RotateTowards(turretPivot.localRotation, targetYawLocal, yawSpeed * Time.deltaTime);
        }
        else
        {
            turretPivot.localRotation = targetYawLocal;
        }

        Vector3 dirLocalToPitch = turretPivot.InverseTransformDirection(dirWorld);

        float pitchDegrees = -Mathf.Atan2(dirLocalToPitch.y, dirLocalToPitch.z) * Mathf.Rad2Deg;
        pitchDegrees = Mathf.Clamp(pitchDegrees, minPitch, maxPitch);

        Quaternion targetLocalPitch = Quaternion.AngleAxis(pitchDegrees, Vector3.right);

        if (pitchSpeed > 0f)
        {
            barrelPivot.localRotation = Quaternion.RotateTowards(barrelPivot.localRotation, targetLocalPitch, pitchSpeed * Time.deltaTime);
        }
        else
        {
            barrelPivot.localRotation = targetLocalPitch;
        }
    }

    private void LateUpdate()
    {
        if (InputManager.Instance == null) return;
        if (muzzle == null) return;

        bool hasGroundHit = Physics.Raycast(cam.ScreenPointToRay(InputManager.Instance.Look), out RaycastHit groundHit, aimLength, aimMask, QueryTriggerInteraction.Ignore);

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

        if (Physics.Raycast(start, dir, out RaycastHit muzzleHit, maxLineDistance, aimMask, QueryTriggerInteraction.Ignore))
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

    private void SetVisualsEnabled(bool enabled)
    {
        if (aimLine != null && aimLine.enabled != enabled)
            aimLine.enabled = enabled;

        if (aimMarker != null && aimMarker.gameObject.activeSelf != enabled)
            aimMarker.gameObject.SetActive(enabled);
    }
}
