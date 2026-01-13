using UnityEngine;
using UnityEngine.InputSystem;

public class TankTurretOffline : MonoBehaviour
{
    [SerializeField] private InputActionReference aimAction;

    [SerializeField] private Transform turretPivot;
    [SerializeField] private LayerMask aimMask = ~0;

    private Camera cam;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (cam == null || turretPivot == null) return;

        Ray ray = cam.ScreenPointToRay(aimAction.action.ReadValue<Vector2>());

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, aimMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 target = hit.point;
            Vector3 flatDir = target - turretPivot.position;
            flatDir.y = 0f;

            if (flatDir.sqrMagnitude > 0.001f)
            {
                Quaternion look = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
                turretPivot.rotation = look;
            }
        }
    }
}
