using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed;
    public Vector2 turnSpeed;
    public Vector2 turningDeadZone;
    [Space]
    public GameObject head;

    internal Rigidbody rb;
    Vector3 movementInput;
    Vector3 rotationInput;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        Movement();
        Rotation();
    }

    public void Movement()
    {
        Vector3 movement = rb.linearVelocity;
        movement.x = movementInput.x * moveSpeed;
        movement.z = movementInput.z * moveSpeed;

        rb.AddForce(transform.rotation * movement);
    }

    public void Rotation() 
    {
        if (Mathf.Abs(rotationInput.x) > turningDeadZone.x) transform.Rotate(Vector3.up, rotationInput.x * turnSpeed.x * Time.deltaTime);
        if (Mathf.Abs(rotationInput.y) > turningDeadZone.y) head.transform.Rotate(Vector3.right, rotationInput.y * turnSpeed.y * Time.deltaTime);
        //transform.rotation = Quaternion.AngleAxis(rotationInput.y * turnSpeed * Time.deltaTime, transform.up) * transform.rotation;
        //head.transform.rotation = Quaternion.AngleAxis(rotationInput.x * turnSpeed, head.transform.right) * head.transform.rotation;
    }

    public void MovementInput(InputAction.CallbackContext context)
    {
        Vector2 _input = context.ReadValue<Vector2>();

        movementInput.x = _input.x;
        movementInput.z = _input.y;
    }

    public void RotationInput(InputAction.CallbackContext context)
    {
        Vector2 _input = context.ReadValue<Vector2>();

        rotationInput.y = Mathf.Clamp(_input.y * -1.0f, -1, 1);
        rotationInput.x = Mathf.Clamp(_input.x, -1, 1);
    }
}
