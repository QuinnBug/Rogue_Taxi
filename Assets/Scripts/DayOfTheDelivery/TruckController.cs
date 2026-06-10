using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class TruckController : MonoBehaviour
{
    public Transform m_bodyTransform;
    public Transform[] m_aWheelTransforms;
    public Rigidbody m_physics;
    public SuspensionSystem m_suspension;
    [Space]
    public float m_fRotationSpeed = 0;
    [Space]
    public float m_fTurningTorque = 1;
    public float m_fTurningVelocityMin = 1;
    [Space]
    public TruckStats m_stats;

    private float m_acceleration = 0;
    
    
    private float m_turning = 0;

    private Vector2 m_turnInput;
    private Vector2 m_moveInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (m_physics == null) { Debug.LogError("[SC] Rigidbody not assigned"); }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        m_physics.useGravity = false;
        Event_Manager.Instance.AddListener(E_Event.Buildings, E_Action.Finished, EnableInput);
    }

    // Update is called once per frame
    void Update()
    {
        m_stats.Update();

        AccelInputHandling();
        TurnInputHandling();

        PhysicsUpdate();
        ModelUpdate();
    }

    private void ModelUpdate()
    {
        float steeringDir = m_turning * m_stats.fTurnLimit;

        //Wheel Rot
        float wheelDir = Mathf.Clamp(steeringDir, -m_stats.fWheelLimit, m_stats.fWheelLimit);
        foreach (Transform wheelTf in m_aWheelTransforms)
        {
            wheelTf.localRotation = Quaternion.Euler(0, wheelDir, 0);
        }

        //BodyRot
        //float bodyRot = steeringDir - wheelDir;
        //m_bodyTransform.localRotation = Quaternion.Euler(0, bodyRot, 0);
    }

    private void PhysicsUpdate()
    {
        // Steering //
        float steeringDir = m_turning * m_stats.fTurnLimit * m_fTurningTorque;
        m_physics.AddTorque(0, steeringDir, 0);

        // Movement //
        foreach (var wheel in m_suspension.wheels)
        {
            if (!wheel.grounded) { continue; }

            m_physics.AddForce(wheel.forward * m_acceleration);
        }
    }

    void EnableInput() 
    {
        m_physics.useGravity = true;
    }

    private void AccelInputHandling()
    {
        //Input//
        m_acceleration = m_moveInput.y * m_stats.acceleration;
    }

    private void TurnInputHandling()
    {
        if (m_turnInput.x != 0)
        {
            m_turning += m_turnInput.x * m_stats.turnSpeed * Time.deltaTime;
            m_turning = Mathf.Clamp(m_turning, -1, 1);
        }
        else if (Mathf.Abs(m_turning) <= 0.1f && m_moveInput.y != 0.0f)
        {
            m_turning = 0;
        }
        else
        {
            m_turning = Mathf.Lerp(m_turning, 0, m_stats.turnResetRate * Time.deltaTime);
        }
    }

    public void MovementInput(InputAction.CallbackContext context)
    {
        float _input = context.ReadValue<float>();

        m_moveInput.y = _input;
    }

    public void TurningInput(InputAction.CallbackContext context)
    {
        Vector2 _input = context.ReadValue<Vector2>();

        m_turnInput.x = _input.x;
        //m_turnInput.y = _input.y;
    }

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere( transform.position + m_physics.centerOfMass, 0.5f);
    }
}
