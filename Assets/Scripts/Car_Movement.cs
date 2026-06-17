using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Car_Movement : MonoBehaviour
{
    public float moveSpeed;
    public float turnSpeed;

    internal float fuel;
    internal float fuelDrain;

    private SuspensionSystem suspension;

    internal Rigidbody rb;
    Vector3 movementInput;
    internal bool active = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        suspension = GetComponent<SuspensionSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        rb.useGravity = active;

        if (!active) return;

        if (movementInput.sqrMagnitude >= 0.1f) 
        {
            Movement();
            Rotation();
        }
    }

    public void Movement() 
    {
        if (fuel <= 0)
        {
            return;
        }

        foreach (Wheel wheel in suspension.wheels)
        {
            if (wheel.grounded)
            {
                wheel.torque += movementInput.z * moveSpeed;
                //rb.AddForce(transform.forward * movementInput.z * moveSpeed);
                //PlayerManager.Instance.stats.currentFuel -= fuelDrain * Time.deltaTime;
                break;
            }
        }
    }

    public void Rotation() 
    {
        if (fuel <= 0)
        {
            return;
        }

        //if (movementInput.sqrMagnitude > 0.1f)
        //{
        //    if (suspension.GroundedPercent() > 0f)
        //    {
        //        transform.rotation = Quaternion.Lerp(transform.rotation,
        //            Quaternion.LookRotation(movementInput, Vector3.up), turnSpeed * Time.deltaTime);
        //    }
        //}

        if (suspension.GroundedPercent() > 0f)
        {
            transform.Rotate(Vector3.up, movementInput.x * turnSpeed * Time.deltaTime);
        }
    }

    public void MovementInput(InputAction.CallbackContext context) 
    {
        Vector2 _input = context.ReadValue<Vector2>();

        movementInput.x = _input.x;
        movementInput.z = _input.y;
    }
}
