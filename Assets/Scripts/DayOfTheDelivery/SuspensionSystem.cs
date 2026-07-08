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
    public void WheelsUpdate(CarSettingPresets settings)
    {
        foreach (Wheel wheel in wheels)
        {
            if (d_bApplyBaseSettings) { UpdateWheelSettings(wheel, settings); }

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

    void UpdateWheelSettings(Wheel wheel, CarSettingPresets carSettings)
    {
        wheel.settings = baseSettings;
        wheel.wheelMass = carSettings.wheelMass;
        wheel.fwdFriction.ApplyChanges(wheel.offset.z > 0 ? carSettings.frontFwdFriction : carSettings.rearFwdFriction);
        wheel.sideFriction.ApplyChanges(wheel.offset.z > 0 ? carSettings.frontSideFriction : carSettings.rearSideFriction);
    }

    //https://www.youtube.com/watch?v=x0LUiE0dxP0
}
