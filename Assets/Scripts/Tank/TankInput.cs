using Unity.Netcode;
using UnityEngine;

public class TankInput : NetworkBehaviour
{
    public NetworkVariable<sbyte> Throttle = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<sbyte> Turn = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> boostHeld = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Update()
    {
        if (!IsOwner || !IsClient) return;
        if (InputManager.Instance == null) return;

        Vector2 move = InputManager.Instance.Move;
        bool boost = InputManager.Instance.BoostHeld;

        sbyte throttle = 0;
        if (move.y > 0.5f) throttle = 1;
        else if (move.y < -0.5f) throttle = -1;

        sbyte turn = 0;
        if (move.x > 0.5f) turn = 1;
        else if(move.x < -0.5f) turn = -1;

        Throttle.Value = throttle;
        Turn.Value = turn;
        boostHeld.Value = boost;
    }
}