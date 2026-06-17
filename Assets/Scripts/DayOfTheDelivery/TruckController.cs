using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class TruckController : MonoBehaviour
{
    public Transform m_bodyTransform;
    public Transform[] m_aWheelTransforms;
    public Rigidbody m_physics;
    public SuspensionSystem m_suspension;
    public InputHandler m_inputs;
    [Space]
    public float m_fRotationSpeed = 0;
    [Space]
    public float m_fTurningTorque = 1;
    public float m_fTurningVelocityMin = 1;
    [Space]
    public TruckStats m_stats;
    [Space]
    public GameObject m_shotPrefab;
    public GameObject m_deliveryCannon;
    public float m_cannonOffset;
    public float m_cannonForce;
    
    [SerializeField]
    private float m_currentAccel = 0;
    [SerializeField]
    private float m_revs = 0;
    
    private float m_turning = 0;
    private bool m_braking = false;

    private float m_turnInput;
    private float m_moveInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (m_physics == null) { Debug.LogError("[SC] Rigidbody not assigned"); }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (Event_Manager.Instance != null) 
        {
            Event_Manager.Instance.AddListener(E_Event.Buildings, E_Action.Finished, EnableInput);
        }
    }

    // Update is called once per frame
    void Update()
    {
        m_stats.Update();

        m_moveInput = m_inputs.throttle;
        m_turnInput = m_inputs.steering;

        if (m_inputs.fire) { Shoot(); }

        AccelInputHandling();
        TurnInputHandling();

        ModelUpdate();
    }

    private void FixedUpdate()
    {
        PhysicsUpdate();
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
    }

    private void PhysicsUpdate()
    {
        // Movement //
        foreach (var wheel in m_suspension.wheels)
        {
            if (!wheel.grounded) { continue; }

            wheel.torque += m_revs * m_stats.acceleration;
        }
    }

    void EnableInput() 
    {
        m_physics.useGravity = true;
    }

    private void AccelInputHandling()
    {
        m_braking = m_revs > 0 && m_moveInput < 0;

        if (Mathf.Abs(m_revs) >= 0.01) { m_revs = Mathf.Lerp(m_revs, 0, m_stats.revDrag); }
        else { m_revs = 0; }

        m_revs += m_moveInput * m_stats.revScale;
        m_revs = m_stats.revLimits.Clamp(m_revs);
    }

    private void TurnInputHandling()
    {
        if (m_turnInput != 0)
        {
            m_turning += m_turnInput * m_stats.turnSpeed * Time.deltaTime;
            m_turning = Mathf.Clamp(m_turning, -1, 1);
        }
        else if (Mathf.Abs(m_turning) <= 0.1f && m_moveInput != 0.0f)
        {
            m_turning = 0;
        }
        else
        {
            m_turning = Mathf.Lerp(m_turning, 0, m_stats.turnResetRate * Time.deltaTime);
        }
    }

    private void Shoot()
    {
        var shot = Instantiate(
            m_shotPrefab,
            m_deliveryCannon.transform.position + (m_deliveryCannon.transform.forward * m_cannonOffset),
            Quaternion.identity
        );
        
        shot.GetComponent<Rigidbody>().AddForce(m_physics.linearVelocity + (m_deliveryCannon.transform.forward * m_cannonForce));
    }

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere( transform.position + m_physics.centerOfMass, 0.5f);
    }
}
