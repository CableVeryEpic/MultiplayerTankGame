using Unity.Netcode;
using UnityEngine;

public class TankTurret : NetworkBehaviour
{
    [SerializeField] private Transform turretPivot;
    [SerializeField] private Transform barrelPivot;
    [SerializeField] private TankAimInput aimInput;

    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 25f;

    [SerializeField] private float yawSpeed = 0f;
    [SerializeField] private float pitchSpeed = 0f;

    private NetworkVariable<float> NetYaw = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> NetPitch = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (aimInput == null) aimInput = GetComponent<TankAimInput>();
    }

    // Update is called once per frame
    void Update()
    {
        if (turretPivot == null || barrelPivot == null || aimInput == null) return;

        if (IsServer)
        {
            ComputeAndSetAnglesServer(aimInput.AimPoint.Value);
        }

        ApplyAngles(NetYaw.Value, NetPitch.Value);
    }

    private void ComputeAndSetAnglesServer(Vector3 aimPointWorld)
    {
        Vector3 localTarget = transform.InverseTransformPoint(aimPointWorld);
        localTarget.y = 0f;
        if (localTarget.sqrMagnitude > 0.0001f)
        {
            float yaw = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
            NetYaw.Value = yaw;
        }


        Vector3 dirWorld = aimPointWorld - barrelPivot.position;
        if (dirWorld.sqrMagnitude > 0.0001f)
        {
            Vector3 dirLocal = turretPivot.InverseTransformDirection(dirWorld.normalized);
            float pitch = -Mathf.Atan2(dirLocal.y, dirLocal.z) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            NetPitch.Value = pitch;
        }
    }

    private void ApplyAngles(float yawDeg, float pitchDeg)
    {
        Quaternion yawLocal = Quaternion.AngleAxis(yawDeg, Vector3.up);
        Quaternion pitchLocal = Quaternion.AngleAxis(pitchDeg, Vector3.right);

        if (yawSpeed > 0)
            turretPivot.localRotation = Quaternion.RotateTowards(turretPivot.localRotation, yawLocal, yawSpeed * Time.deltaTime);
        else 
            turretPivot.localRotation = yawLocal;

        if (pitchSpeed > 0)
            barrelPivot.localRotation = Quaternion.RotateTowards(barrelPivot.localRotation, pitchLocal, pitchSpeed * Time.deltaTime);
        else
            barrelPivot.localRotation = pitchLocal;
    }
}
