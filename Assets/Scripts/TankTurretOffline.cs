using UnityEngine;
using UnityEngine.InputSystem;

public class TankTurretOffline : MonoBehaviour
{
    [SerializeField] private Transform turretPivot;
    [SerializeField] private Transform barrelPivot;

    [SerializeField] private LayerMask aimMask = ~0;

    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 25f;

    [SerializeField] private float yawSpeed = 0f;
    [SerializeField] private float pitchSpeed = 0f;

    private Camera cam;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (cam == null || turretPivot == null || barrelPivot == null) return;
        if (InputManager.Instance == null) return;

        Ray ray = cam.ScreenPointToRay(InputManager.Instance.Look);

        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, aimMask, QueryTriggerInteraction.Ignore)) return;

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
}
