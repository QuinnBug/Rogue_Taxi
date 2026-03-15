using UnityEngine;
using UnityEngine.InputSystem;

public class ShooterController : MonoBehaviour
{
    public GameObject m_head;
    public GameObject m_body;
    public Rigidbody m_physics;
    [Space]
    public Range<float> m_headTurnRange;
    public PlayerStats m_stats;

    private Vector2 m_turnInput;
    private Vector2 m_moveInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (m_head == null) { Debug.LogError("[SC] Head not assigned"); }
        if (m_body == null) { Debug.LogError("[SC] Body not assigned"); }
        if (m_physics == null) { Debug.LogError("[SC] Rigidbody not assigned"); }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        //Body movement and rotation
        Vector3 movement = new Vector3(m_moveInput.x, 0, m_moveInput.y) * m_stats.accelerationRate * Time.deltaTime;
        movement = m_body.transform.rotation * movement;
        m_physics.AddForce(movement);

        Vector3 rotation = m_body.transform.rotation.eulerAngles;
        m_body.transform.Rotate(new Vector3(0, m_turnInput.x, 0) * m_stats.turnSpeed * Time.deltaTime);

        //Head rotation
        rotation = m_head.transform.rotation.eulerAngles;
        rotation.x -= m_turnInput.y * m_stats.turnSpeed * Time.deltaTime;
        rotation.x = MathUtil.ClampRotation(rotation.x, m_headTurnRange.min, m_headTurnRange.max);

        m_head.transform.rotation = Quaternion.Euler(rotation);
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
        m_turnInput.y = _input.y;
    }
}
