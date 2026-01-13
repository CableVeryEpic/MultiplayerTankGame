using UnityEngine;
using UnityEngine.InputSystem;

public class TankAbilityOffline : MonoBehaviour
{
    [SerializeField] private InputActionReference abilityAction;

    [SerializeField] private float cooldownSeconds = 5f;

    private float nextReadyTime;

    private void OnEnable()
    {
        abilityAction?.action?.Enable();
        if (abilityAction != null)
            abilityAction.action.performed += OnAbilityPerformed;
    }

    private void OnDisable()
    {
        if (abilityAction != null)
            abilityAction.action.performed += OnAbilityPerformed;
        abilityAction?.action?.Disable();
    }

    private void OnAbilityPerformed(InputAction.CallbackContext ctx)
    {
        TryActivate();
    }

    private void TryActivate()
    {
        float now = Time.time;
        if (now < nextReadyTime)
        {
            float remaining = nextReadyTime - now;
            return;
        }

        nextReadyTime = now + cooldownSeconds;
    }

    public float CooldownRemaining()
    {
        return Mathf.Max(0f, nextReadyTime - Time.time);
    }

    public bool IsReady()
    {
        return Time.time >= nextReadyTime;
    }
}
