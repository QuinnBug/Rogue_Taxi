using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
public class SuspensionSystem : MonoBehaviour
{
    public bool d_bShowPoints;
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
    public void WheelsUpdate(CarSettingPresets settings, bool braking)
    {
        foreach (Wheel wheel in wheels)
        {
            UpdateWheelSettings(wheel, settings, braking);

            wheel.PhysicsUpdate(m_rb, braking);
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

    void UpdateWheelSettings(Wheel wheel, CarSettingPresets carSettings, bool _braking)
    {
        wheel.settings = baseSettings;
        wheel.wheelMass = carSettings.wheelMass;

        if (_braking)
        {
            wheel.fwdFriction.ApplyChanges(wheel.offset.z > 0 ? carSettings.frontFwdBrakeFriction : carSettings.rearFwdBrakeFriction);
            wheel.sideFriction.ApplyChanges(wheel.offset.z > 0 ? carSettings.frontSideBrakeFriction : carSettings.rearSideBrakeFriction);
        }
        else
        {
            wheel.fwdFriction.ApplyChanges(wheel.offset.z > 0 ? carSettings.frontFwdFriction : carSettings.rearFwdFriction);
            wheel.sideFriction.ApplyChanges(wheel.offset.z > 0 ? carSettings.frontSideFriction : carSettings.rearSideFriction);
        }
    }

    //https://www.youtube.com/watch?v=x0LUiE0dxP0
}
