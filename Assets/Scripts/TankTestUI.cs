using UnityEngine;
using TMPro;

public class TankTestUI : MonoBehaviour
{
    private GameObject tank;
    private TankMotorOffline tankMotor;
    private TankAbilityOffline tankAbility;

    [Header("Movement")]
    public TMP_Text currentSpeed;
    public TMP_Text boostAvailable;
    public TMP_Text profile;

    [Header("Ability")]
    public TMP_Text abilityReady;
    public TMP_Text abilityCooldown;

    // Update is called once per frame
    void Update()
    {
        if (tank == null)
        {
            tank = GameObject.Find("Tank_1");
            if (tank != null)
            {
                tankMotor = tank.GetComponent<TankMotorOffline>();
                tankAbility = tank.GetComponent<TankAbilityOffline>();
            }
        }
        currentSpeed.text = $"Current Speed: {tankMotor.currentSpeed:F2}";
        boostAvailable.text = $"Boost Available: {tankMotor.boostAvailable}";
        profile.text = $"Profile: {tankMotor.profile}";

        abilityReady.text = $"Ability Ready: {tankAbility.IsReady()}";
        abilityCooldown.text = $"Ability Cooldown: {tankAbility.CooldownRemaining():F2}";
    }
}
