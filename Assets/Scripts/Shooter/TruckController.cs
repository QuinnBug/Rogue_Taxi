using UnityEngine;
using UnityEngine.InputSystem;

public class TruckController : MonoBehaviour
{
    public Transform m_bodyTransform;
    public Transform m_noseTransform;
    public Rigidbody m_physics;
    [Space]
    public float d_x;
    public PlayerStats m_stats;

    private float m_acceleration = 0;

    private Vector2 m_turnInput;
    private Vector2 m_moveInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (m_physics == null) { Debug.LogError("[SC] Rigidbody not assigned"); }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        //Input//
        if (m_moveInput.y != 0)
        {
            m_acceleration += m_moveInput.y * m_stats.accelerationRate * Time.deltaTime;
            m_acceleration = m_stats.speedLimits.Clamp(m_acceleration);
        }
        else
        {
            m_acceleration = Mathf.Lerp(m_acceleration, 0, m_stats.decelerationRate * Time.deltaTime);
        }

        // Steering //
        Vector3 steeringDir = new Vector3(0, m_turnInput.x * m_stats.turnSpeed, 0);
        m_noseTransform.transform.localRotation = Quaternion.Euler(steeringDir);

        // Movement //
        if (Mathf.Abs(m_acceleration) >= 0)
        {
            Vector3 movement = m_noseTransform.transform.forward * m_acceleration * Time.deltaTime;
            m_physics.AddForce(movement);
        }

        // Turning //
        if (m_physics.linearVelocity.magnitude > 0)
        {
            var targetRot = Quaternion.LookRotation(m_physics.linearVelocity, Vector3.up);
            //var targetRot = m_noseTransform.rotation;
            m_bodyTransform.rotation = Quaternion.Lerp(m_bodyTransform.rotation, targetRot, d_x * Time.deltaTime);
        }

    }

    public void MovementInput(InputAction.CallbackContext context)
    {
        Vector2 _input = context.ReadValue<Vector2>();

        m_moveInput.x = _input.x;
        m_moveInput.y = _input.y;
    }

    public void TurningInput(InputAction.CallbackContext context)
    {
        Vector2 _input = context.ReadValue<Vector2>();

        m_turnInput.x = _input.x;
        //m_turnInput.y = _input.y;
    }
}
