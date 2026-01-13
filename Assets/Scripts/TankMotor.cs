using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TankMotor : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float turnSpeed = 180f;

    private Rigidbody rb;

    public NetworkVariable<TankInput> InputState = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!IsOwner || !IsClient) return;

        sbyte throttle = 0;
        if (Input.GetKey(KeyCode.W)) throttle = 1;
        else if (Input.GetKey(KeyCode.S)) throttle = 1;

        sbyte turn = 0;
        if (Input.GetKey(KeyCode.D)) turn = 1;
        else if (Input.GetKey(KeyCode.A)) turn = -1;

        InputState.Value = new TankInput
        {
            throttle = throttle,
            turn = turn
        };
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        var input = InputState.Value;
        float dt = Time.fixedDeltaTime;

        float yaw = input.turn * turnSpeed * dt;
        Quaternion rot = rb.rotation * Quaternion.Euler(0f, yaw, 0f);

        Vector3 forward = rot * Vector3.forward;
        Vector3 delta = forward * (input.throttle * moveSpeed * dt);

        rb.MoveRotation(rot);
        rb.MovePosition(rb.position + delta);
    }

    public void SetMoveStats(float newMoveSpeed, float newTurnSpeed)
    {
        moveSpeed = newMoveSpeed;
        turnSpeed = newTurnSpeed;
    }
}
