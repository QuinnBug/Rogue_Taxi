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
    // Settings
    public AnimationCurve frictionCurve;

    //Variables
    [SerializeField]
    internal float currentPercent;
    [Range(0.0f, 1.0f)] internal float currentValue;

    public void ApplyChanges(FrictionValues other)
    {
        frictionCurve = other.frictionCurve;
    }

    public void Update(float directionalVelocity, float totalVelocity)
    {
        if (totalVelocity <= 0.01f)
        {
            currentPercent = 0.0f;
        }
        else
        {
            currentPercent = Mathf.Clamp01(Mathf.Abs(directionalVelocity) / totalVelocity);
        }

        currentValue = frictionCurve.Evaluate(currentPercent);
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
