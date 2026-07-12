using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class TruckController : MonoBehaviour
{
    public Transform[] m_aWheelTransforms; //for turning the front wheels - can move this to the wheel I think
    public Rigidbody m_physics;
    public SuspensionSystem m_suspension;
    public InputHandler m_inputs;
    [Space]
    public CarSettingPresets m_driveSettings;
    [Space]
    [SerializeField]
    private EngineValues m_engine;
    [Space]
    public TruckStats m_stats;
    public float m_debugSpeed = 10;
    [Space]
    //I'd like this bundled up into a different script I think
    public GameObject m_shotPrefab;
    public GameObject m_deliveryCannon;
    public GameObject m_deliveryPointer;
    public float m_cannonOffset;
    public float m_cannonForce;

    private float m_revs = 0;
    private float m_turning = 0;

    //This is input stuff
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
        else { EnableInput(); }
    }

    // Update is called once per frame
    void Update()
    {
        if (m_inputs.fire) { Shoot(); }

        AccelInputHandling();
        TurnInputHandling();

        ModelUpdate();
    }

    private void FixedUpdate()
    {
        if (m_inputs.phoneNav.x != 0)
        {
            m_physics.AddForce(transform.right * m_debugSpeed * m_inputs.phoneNav.x);
        }

        PhysicsUpdate();
        m_suspension.WheelsUpdate(m_driveSettings, m_inputs.brake);
    }

    private void ModelUpdate()
    {
        float steeringDir = m_turning * m_stats.fWheelLimit;

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
        m_engine.ApplyChanges(m_driveSettings.engine);
        float speed = Vector3.Dot(transform.forward, m_physics.linearVelocity);
        m_engine.Update(speed);

        foreach (Wheel wheel in m_suspension.wheels)
        {
            if (wheel.grounded && wheel.drive) 
            {
                wheel.torque = m_revs * m_engine.currentValue; 
            }
        }
    }

    void EnableInput() 
    {
        m_physics.useGravity = true;
    }

    private void AccelInputHandling()
    {
        m_revs = m_inputs.throttle * m_stats.revScale;
    }

    private void TurnInputHandling()
    {
        if (m_inputs.steering != 0)
        {
            m_turning += m_inputs.steering * m_stats.turnSpeed * Time.deltaTime;
            m_turning = Mathf.Clamp(m_turning, -1, 1);
        }
        else if (Mathf.Abs(m_turning) <= 0.1f)
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

    //public void OnDrawGizmosSelected()
    //{
    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawSphere( transform.position + m_physics.centerOfMass, 0.5f);
    //}
}
