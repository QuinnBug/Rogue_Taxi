using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct TruckStats 
{
    // Driving
    [SerializeField]
    private float speedScaling;
    [SerializeField]
    private float baseAcceleration;
    [SerializeField]
    private float baseDeceleration;
    [SerializeField]
    private Range<float> baseSpeedLimits;

    // Steering
    [SerializeField]
    public float turnSpeed;
    [SerializeField]
    public float turnResetRate;
    [SerializeField]
    public float fTurnLimit;
    [SerializeField]
    public float fWheelLimit;

    internal float acceleration;
    internal float deceleration;
    internal Range<float> speedLimits;

    public void Init() 
    {
        Update();
    }

    public void Update()
    {
        acceleration = baseAcceleration * speedScaling;
        deceleration = baseDeceleration * speedScaling;
        speedLimits.min = baseSpeedLimits.min * speedScaling;
        speedLimits.max = baseSpeedLimits.max * speedScaling;
    }
}