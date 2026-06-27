using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class SuspensionSystem : MonoBehaviour
{
    public bool d_bShowPoints;
    public bool d_bApplyBaseSettings;
    [Space]
    public Wheel[] wheels;
    public SuspensionSettings baseSettings;
    //Side Frictions
    public FrictionValues frontSideFriction;
    public FrictionValues rearSideFriction;

    public LayerMask groundMask = new LayerMask();

    private Rigidbody m_rb;

    // Start is called before the first frame update
    void Start()
    {
        m_rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        foreach (Wheel wheel in wheels)
        {
            if (d_bApplyBaseSettings) { UpdateWheelSettings(wheel); }

            wheel.PhysicsUpdate(m_rb);
        }
    }

    public float GroundedPercent() 
    {
        int groundedI = 0;
        foreach (Wheel wheel in wheels)
        {
            if (wheel.grounded)
            {
                groundedI++;
            }
        }

        return groundedI / wheels.Length;
    }

    void UpdateWheelSettings(Wheel wheel)
    {
        wheel.settings = baseSettings;

        if (wheel.offset.z > 0)
        {
            wheel.sideFriction = frontSideFriction;
        }
        else
        {
            wheel.sideFriction = rearSideFriction;
        }
    }

    private void OnDrawGizmos()
    {
        if (wheels == null) { return; }

        foreach (Wheel wheel in wheels)
        {
            if (d_bApplyBaseSettings) { UpdateWheelSettings(wheel); }
        }

        if (m_rb != null)
        {
            Gizmos.color = Color.hotPink;
            Gizmos.DrawLine(transform.position, transform.position + m_rb.linearVelocity);
        }
    }

    //https://www.youtube.com/watch?v=x0LUiE0dxP0
}
