using Unity.Netcode;
using UnityEngine;

public class TankHealth : NetworkBehaviour
{
    public NetworkVariable<float> Health = new(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> Armour = new(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public void ApplyDamageServer(float amount, ulong attackerId)
    {
        if (!IsServer) return;

        Health.Value = Mathf.Max(0f, Health.Value - amount);

        if (Health.Value <= 0f)
        {
            // TODO: notify round manager, disable tank, etc.
            // For now: just stop movement or despawn.
            NetworkObject.Despawn();
        }
    }
}
