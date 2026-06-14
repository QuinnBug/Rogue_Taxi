using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public float steering = 0.0f;
    public float throttle = 0.0f;
    public bool brake = false;
    public bool fire = false;

    public bool phoneToggle = false;
    public Vector2 phoneNav = Vector2.zero;
    public bool phoneSelect = false;
    [Space]
    public Dictionary<string, InputAction> actions = new Dictionary<string, InputAction>(); 

    private void Start()
    {
        var ip = GetComponent<PlayerInput>();

        foreach (var action in ip.currentActionMap.actions)
        {
            actions[action.name] = action;
        }
    }

    private void Update()
    {
        fire = actions["Fire"].WasPressedThisFrame();
        phoneToggle = actions["TogglePhone"].WasPressedThisFrame();
        phoneSelect = actions["Select"].WasPressedThisFrame();
    }

    public void ThrottleInput(InputAction.CallbackContext context)
    {
        float _input = context.ReadValue<float>();

        throttle = _input;
    }

    public void TurningInput(InputAction.CallbackContext context)
    {
        Vector2 _input = context.ReadValue<Vector2>();
        steering = _input.x;
    }

    public void BrakingInput(InputAction.CallbackContext context)
    {
        brake = context.ReadValueAsButton();
    }

    public void PhoneNavInput(InputAction.CallbackContext context)
    {
        Vector2 _input = context.ReadValue<Vector2>();
        phoneNav = _input;
    }
}
