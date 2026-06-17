using UnityEngine;

public class Wheel : MonoBehaviour
{
    internal Vector3 position;
    internal bool grounded = false;
    internal Vector3 groundPos;
    internal Vector3 forward = Vector3.zero;

    public Vector3 offset;
    public SuspensionSettings settings;
    public LayerMask groundMask;
    
    //Drive
    [Range(0.0f, 1.0f)] public float fwdFriction;
    [Range(0.0f, 1.0f)] public float sideFriction;
    internal float torque;
    [Range(0.0f, 1.0f)] public float torqueDrag;

    Vector3 fwdDrag = Vector3.zero;
    Vector3 sideDrag = Vector3.zero;
    Vector3 overallForce = Vector3.zero;


    //Suspension
    private float minLength;
    private float maxLength;
    private float lastLength;
    private float springLength;
    private float springVelocity;
    private float springForce;
    private float damperForce;

    public void PhysicsUpdate(Rigidbody _rb)
    {
        UpdatePosition(_rb.transform);
        if (UpdateSpringLength(-transform.up))
        {
            _rb.AddForceAtPosition(GetSuspensionForce(transform.up), position);
        }

        UpdateForces(_rb);
    }

    public void UpdateForces(Rigidbody _rb)
    {
        //Acceleration
        torque = Mathf.Lerp(torque, 0, torqueDrag);
        if (Mathf.Abs(torque) < 0.1f) { torque = 0; }
        var accel = (transform.forward * torque) / Time.fixedDeltaTime;

        //Drag
        var velocity = _rb.GetPointVelocity(position);

        var fwd = Vector3.Dot(velocity, transform.forward);
        var forwardResistance = -fwd * fwdFriction;
        fwdDrag = (transform.forward * forwardResistance) / Time.fixedDeltaTime;

        var side = Vector3.Dot(velocity, transform.right);
        var sideResistance = -side * sideFriction;
        sideDrag = (transform.right * sideResistance) / Time.fixedDeltaTime;

        overallForce = accel + fwdDrag + sideDrag;
        _rb.AddForceAtPosition(overallForce, position);
    }

    public void UpdatePosition(Transform _tf)
    {
        position = _tf.position + (_tf.rotation * offset);
    }

    public bool UpdateSpringLength(Vector3 _dir)
    {
        minLength = settings.restLength - settings.springTravel;
        maxLength = settings.restLength + settings.springTravel;

        RaycastHit hit;
        if (Physics.Raycast(position, _dir, out hit, maxLength + settings.wheelRadius, groundMask))
        {
            grounded = true;
            groundPos = hit.point;

            Vector3 wheelOut = transform.right;
            var rot = Quaternion.AngleAxis(90, wheelOut);
            forward = rot * hit.normal;
            Debug.DrawLine(position, position + forward, Color.yellow);
        }
        else
        {
            grounded = false;
            groundPos = position + (_dir * maxLength);
            forward = transform.forward;
        }

        transform.position = Vector3.Lerp(
                transform.position,
                groundPos - (_dir * settings.wheelRadius),
                settings.springStiffness * Time.deltaTime
        );

        return grounded;
    }

    public Vector3 GetSuspensionForce(Vector3 _dir)
    {
        minLength = settings.restLength - settings.springTravel;
        maxLength = settings.restLength + settings.springTravel;
        lastLength = springLength;

        springLength = Vector3.Distance(position, groundPos) - settings.wheelRadius;
        springLength = Mathf.Clamp(springLength, minLength, maxLength);
        springVelocity = (lastLength - springLength) / Time.fixedDeltaTime;

        springForce = settings.springStiffness * (settings.restLength - springLength);
        damperForce = settings.damperStiffness * springVelocity;

        return (springForce + damperForce) * transform.up;
    }

    private void OnDrawGizmos()
    {
        if (fwdDrag.magnitude > 0)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(position, position + fwdDrag * 10);
        }

        if (sideDrag.magnitude > 0) 
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(position, position + sideDrag * 10);
        }

        if (overallForce.magnitude > 0)
        {
            Gizmos.color = Color.purple;
            Gizmos.DrawLine(position, position + overallForce * 10);
        }

        if (!Application.isPlaying)
        {
            maxLength = settings.restLength + settings.springTravel;

            UpdatePosition(transform.parent);
            UpdateSpringLength(-transform.up);

            Gizmos.color = Color.purple;
            Gizmos.DrawSphere(position, 0.1f);

            if (grounded)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(groundPos - (Vector3.up * settings.wheelRadius), settings.wheelRadius);
            }
        }
    }
}
