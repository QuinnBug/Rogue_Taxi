using System.IO;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Wheel : MonoBehaviour
{
    internal Vector3 hingePosition;
    internal bool grounded = false;
    internal Vector3 groundPos;
    internal Vector3 forward = Vector3.zero;
    internal Vector3 right = Vector3.zero;

    public Vector3 offset;
    public SuspensionSettings settings;
    public LayerMask groundMask;
    
    //Drive
    public FrictionValues fwdDrag;
    public FrictionValues sideFriction;

    internal float torque;
    [Range(0.0f, 1.0f)] public float torqueDrag;
    public float wheelMass;

    Vector3 fwdForce = Vector3.zero;
    Vector3 sideForce = Vector3.zero;

    //Suspension
    private float maxLength;
    public float springMoveSpeed;

    public void PhysicsUpdate(Rigidbody _rb)
    {
        UpdatePosition(_rb.transform);
        if (UpdateSpringLength(-transform.up))
        {
            _rb.AddForceAtPosition(GetSuspensionForce(_rb, transform.up), hingePosition);
            UpdateForces(_rb);
        }
    }

    public void UpdateForces(Rigidbody _rb)
    {
        // Acceleration
        var fwdForce = (forward * torque) / Time.fixedDeltaTime;
        _rb.AddForce(fwdForce);

        // Steering
        Vector3 steerDir = transform.right;
        Vector3 tireWorldVel = _rb.GetPointVelocity(transform.position);
        float steeringVel = Vector3.Dot(steerDir, tireWorldVel);
        sideFriction.Update(steeringVel);
        float desiredVelChange = -steeringVel * sideFriction.currentValue;
        float desiredAccel = desiredVelChange / Time.fixedDeltaTime;
        _rb.AddForceAtPosition(steerDir * wheelMass * desiredAccel, transform.position);
    }

    public void UpdatePosition(Transform _parent)
    {
        hingePosition = _parent.position + (_parent.rotation * offset);
    }

    public bool UpdateSpringLength(Vector3 _dir)
    {
        maxLength = settings.restLength + settings.springTravel;

        RaycastHit hit;
        if (Physics.Raycast(hingePosition, _dir, out hit, maxLength, groundMask))
        {
            grounded = true;
            groundPos = hit.point;

            Vector3 wheelOut = transform.right;
            var rot = Quaternion.AngleAxis(90, wheelOut);
            forward = rot * hit.normal;
        }
        else
        {
            grounded = false;
            groundPos = hingePosition + (_dir * maxLength);
            forward = transform.forward;
        }

        right = Quaternion.Euler(0, 90, 0) * forward;
        transform.position = groundPos + (transform.up * settings.wheelRadius);
        Debug.DrawLine(transform.position, transform.position + forward, Color.yellow);
        Debug.DrawLine(transform.position, transform.position + right, Color.cyan);

        return grounded;
    }

    public Vector3 GetSuspensionForce(Rigidbody _rb, Vector3 _dir)
    {
        //force = (offset * strength) - (velocity * damping)

        Vector3 springDir = transform.up;
        Vector3 tireWorldVel = _rb.GetPointVelocity(transform.position);
        float offset = settings.restLength - Vector3.Distance(hingePosition, groundPos);
        float vel = Vector3.Dot(springDir, tireWorldVel);
        float force = (offset * settings.springStiffness) - (vel * settings.damperStiffness);
        return springDir * force;
    }

    private void OnDrawGizmosSelected()
    {
        if (fwdForce.magnitude > 0)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + fwdForce);
        }

        if (sideForce.magnitude > 0) 
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(hingePosition, hingePosition + sideForce);
        }

        if (!Application.isPlaying)
        {
            maxLength = settings.restLength + settings.springTravel;

            UpdatePosition(transform.parent);
            UpdateSpringLength(-transform.up);

            Gizmos.color = Color.purple;
            Gizmos.DrawSphere(hingePosition, 0.1f);

            if (grounded)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(groundPos - (Vector3.up * settings.wheelRadius), settings.wheelRadius);
            }
        }
    }
}

[System.Serializable]
public struct SuspensionSettings
{
    public float restLength;
    public float springTravel;
    public float springStiffness;
    public float damperStiffness;
    public float wheelRadius;
}

[System.Serializable]
public class FrictionValues
{
    // Settings
    public AnimationCurve frictionCurve;
    public float maxVelocity;

    //Variables
    private float currentPercent;
    [Range(0.0f, 1.0f)] internal float currentValue;

    public void Update(float steeringVel)
    {
        currentPercent = Mathf.Clamp(Mathf.Abs(steeringVel), 0, maxVelocity) / maxVelocity;
        currentValue = frictionCurve.Evaluate(currentPercent);
    }
}


