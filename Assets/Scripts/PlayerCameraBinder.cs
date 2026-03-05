using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class PlayerCameraBinder : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        CinemachineCamera cam = Object.FindFirstObjectByType<CinemachineCamera>();
        if (cam == null) return;

        cam.Follow = transform;
        cam.LookAt = transform;
    }
}
