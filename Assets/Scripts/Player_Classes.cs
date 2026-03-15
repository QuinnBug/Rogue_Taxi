using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PlayerStats 
{
    public float accelerationRate;
    public float decelerationRate;
    public Range<float> speedLimits;
    public float turnSpeed;
    public float turnResetRate;

    public float fTurnLimit;
    public float fWheelLimit;

    public float currentFuel;
    public float maxFuel;

    public float fuelDrain;

    public void Init() 
    {
        currentFuel = maxFuel;
    }
}