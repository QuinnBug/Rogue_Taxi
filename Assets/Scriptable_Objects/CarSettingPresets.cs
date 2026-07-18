using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Suspension", menuName = "Vehicle/Suspension")]
public class CarSettingPresets : ScriptableObject
{
    public EngineValues engine;
    [Space]
    [Header("Forward Friction")]
    public FrictionValues frontFwdFriction;
    public FrictionValues rearFwdFriction;
    [Space]
    public FrictionValues frontFwdBrakeFriction;
    public FrictionValues rearFwdBrakeFriction;
    //Side Frictions
    [Header("Side Friction")]
    public FrictionValues frontSideFriction;
    public FrictionValues rearSideFriction;
    [Space]
    public FrictionValues frontSideBrakeFriction;
    public FrictionValues rearSideBrakeFriction;
    [Space]
    public float wheelMass;
}

[System.Serializable]
public class FrictionValues
{
    //Debug
    public float d_dirSpd = 0;
    public float d_fullSpd = 0;

    // Settings
    public AnimationCurve frictionCurve;
    public AnimationCurve velocityCurve;
    public float maxVelocity;

    //Variables
    [SerializeField]
    internal float frictionPercent;
    [SerializeField]
    internal float velocityPercent;
    [Range(0.0f, 1.0f)] internal float currentValue;

    public void ApplyChanges(FrictionValues other)
    {
        frictionCurve = other.frictionCurve;
        velocityCurve = other.velocityCurve;
        maxVelocity = other.maxVelocity;
    }

    public void Update(float directionalVelocity, float totalVelocity)
    {
        d_dirSpd = directionalVelocity;
        d_fullSpd = totalVelocity;

        if (totalVelocity <= 0.01f)
        {
            frictionPercent = 0.0f;
            velocityPercent = 0.0f;
        }
        else
        {
            frictionPercent = Mathf.Clamp01(Mathf.Abs(directionalVelocity) / totalVelocity);
            velocityPercent = Mathf.Clamp01(totalVelocity / maxVelocity);
        }

        currentValue = frictionCurve.Evaluate(frictionPercent) * velocityCurve.Evaluate(velocityPercent);
    }
}

[Serializable]
public class EngineValues
{
    // Settings
    public AnimationCurve torqueCurve;
    public float maxSpeed;

    //Variables
    [SerializeField]
    internal float currentPercent;
    [Range(0.0f, 1.0f)] internal float currentValue;

    public void ApplyChanges(EngineValues other)
    {
        torqueCurve = other.torqueCurve;
        maxSpeed = other.maxSpeed;
    }

    public void Update(float currentSpeed)
    {
        if (currentSpeed == 0)
        {
            currentPercent = 0.0f;
        }
        else
        {
            currentPercent = Mathf.Clamp(currentSpeed / maxSpeed, -1, 1);
        }

        currentValue = torqueCurve.Evaluate(currentPercent);
    }
}
