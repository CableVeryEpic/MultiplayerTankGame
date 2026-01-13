using UnityEngine;
using UnityEngine.InputSystem;

public class TankAbilityOffline : MonoBehaviour
{
    [SerializeField] private float cooldownSeconds = 5f;

    private float nextReadyTime;

    private void Update()
    {
        if (InputManager.Instance == null) return;

        if (InputManager.Instance.ConsumeAbilityPressed())
        {
            TryActivate();
        }
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
