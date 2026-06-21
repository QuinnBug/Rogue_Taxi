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
            if (d_bApplyBaseSettings)
            {
                wheel.settings = baseSettings;
            }

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

    private void OnDrawGizmos()
    {
        if (wheels == null) { return; }

        foreach (Wheel wheel in wheels)
        {
            if (d_bApplyBaseSettings)
            {
                wheel.settings = baseSettings;
            }
        }

        if(m_rb != null)
        {
            Gizmos.color = Color.hotPink;
            Gizmos.DrawLine(transform.position, transform.position + m_rb.linearVelocity);
        }
    }

    //https://www.youtube.com/watch?v=x0LUiE0dxP0
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
