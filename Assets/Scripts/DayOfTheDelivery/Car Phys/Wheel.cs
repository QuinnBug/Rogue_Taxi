using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Wheel : MonoBehaviour
{
    internal Vector3 hingePosition;
    internal bool grounded = false;
    internal Vector3 groundPos;
    internal Vector3 groundNormal = Vector3.zero;

    public Vector3 offset;
    public SuspensionSettings settings;
    public LayerMask groundMask;
    [Space]
    public bool drive;
    [Space]
    //Drive
    public FrictionValues fwdFriction;
    public FrictionValues sideFriction;
    public float wheelMass;

    internal float torque;
    [Range(0.0f, 1.0f)] public float torqueDrag;

    Vector3 fwdForce = Vector3.zero;
    Vector3 sideForce = Vector3.zero;
    Vector3 driveForce = Vector3.zero;

    //Suspension
    private float maxLength;
    public float springMoveSpeed;

    public void PhysicsUpdate(Rigidbody _rb, bool _braking)
    {
        UpdatePosition(_rb.transform);
        if (UpdateSpringLength(-transform.up))
        {
            _rb.AddForceAtPosition(GetSuspensionForce(_rb, groundNormal), hingePosition);
            UpdateForces(_rb, _braking);
        }
    }

    public void UpdateForces(Rigidbody _rb, bool _braking)
    {
        // Acceleration
        if (drive) 
        { 
            driveForce = (transform.forward * torque) / Time.fixedDeltaTime;
            _rb.AddForceAtPosition(driveForce, transform.position);
        }

        fwdForce = GetFrictionForce(_rb, transform.forward, fwdFriction);
        _rb.AddForceAtPosition(fwdForce, transform.position);

        sideForce = GetFrictionForce(_rb, transform.right, sideFriction);
        _rb.AddForceAtPosition(sideForce, transform.position);
    }

    private Vector3 GetFrictionForce(Rigidbody _rb, Vector3 direction, FrictionValues friction)
    {
        Vector3 tireWorldVel = _rb.GetPointVelocity(hingePosition);
        float velInDirection = Vector3.Dot(direction, tireWorldVel);
        friction.Update(velInDirection, tireWorldVel.magnitude);

        float desiredVelChange = -velInDirection * friction.currentValue;
        float desiredAccel = desiredVelChange / Time.fixedDeltaTime;
        Vector3 force = direction * wheelMass * desiredAccel;

        return force;
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
            groundNormal = hit.normal;
        }
        else
        {
            grounded = false;
            groundPos = hingePosition + (_dir * maxLength);
            groundNormal = transform.up;
        }

        transform.position = groundPos + (transform.up * settings.wheelRadius);

        return grounded;
    }

    public Vector3 GetSuspensionForce(Rigidbody _rb, Vector3 _dir)
    {
        //force = (offset * strength) - (velocity * damping)

        Vector3 springDir = _dir;
        Vector3 tireWorldVel = _rb.GetPointVelocity(transform.position);
        float offset = settings.restLength - Vector3.Distance(hingePosition, groundPos);
        float vel = Vector3.Dot(springDir, tireWorldVel);
        float force = (offset * settings.springStiffness) - (vel * settings.damperStiffness);
        return springDir * force;
    }

    private void OnDrawGizmos()
    {
        //if (driveForce.magnitude > 0)
        //{
        //    Gizmos.color = Color.lavender;
        //    Gizmos.DrawLine(hingePosition, hingePosition + driveForce);
        //}

        if (fwdForce.magnitude > 0)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(hingePosition, hingePosition + (-transform.forward * fwdFriction.currentPercent));
        }

        if (sideForce.magnitude > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(hingePosition, hingePosition + (transform.right * sideFriction.currentPercent));
        }

        //Handles.Label(
        //    hingePosition + Vector3.up * 2 + Vector3.back * 0.5f,
        //    ((int)(fwdFriction.currentPercent * 100)).ToString()
        //);
        //Handles.Label(
        //    hingePosition + Vector3.up * 2 + Vector3.back * 1.0f,
        //    ((int)(sideFriction.currentPercent * 100)).ToString()
        //    );
        Handles.Label(
            hingePosition + Vector3.up * 2 + Vector3.back * 0.5f,
            fwdFriction.currentValue.ToString()
        );
        Handles.Label(
            hingePosition + Vector3.up * 2 + Vector3.back * 1.0f,
            sideFriction.currentValue.ToString()
            );

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


