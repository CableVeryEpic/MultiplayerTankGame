using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [SerializeField] private InputActionReference shootAction;
    [SerializeField] private InputActionReference lookAction;

    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference boostAction;

    [SerializeField] private InputActionReference abilityAction;

    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool BoostHeld { get; private set; }
    public bool ShootHeld { get; private set; }
    public bool AbilityPressedThisFrame { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (Instance != null) 
        { 
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        Move = moveAction.action.ReadValue<Vector2>();
        Look = lookAction.action.ReadValue<Vector2>();

        BoostHeld = boostAction.action.IsPressed();
        ShootHeld = shootAction.action.IsPressed();

        AbilityPressedThisFrame = abilityAction.action.WasPressedThisFrame();
    }

    public bool ConsumeAbilityPressed()
    {
        if (!AbilityPressedThisFrame) return false;
        AbilityPressedThisFrame = false;
        return true;
    }
}
