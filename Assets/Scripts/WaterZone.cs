using UnityEngine;

public class WaterZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TankMotorOffline motor = other.GetComponent<TankMotorOffline>();
            if (motor != null)
            {
                motor.EnterMud();
                Debug.Log("In Mud");
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TankMotorOffline motor = other.GetComponent<TankMotorOffline>();
            if (motor != null)
            {
                motor.ExitMud();
                Debug.Log("Out Mud");
            }
        }
    }
}
