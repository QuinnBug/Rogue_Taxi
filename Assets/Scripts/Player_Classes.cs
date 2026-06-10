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
    public float revScale;
    public float revDrag;
    public Range<float> revLimits;


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

    public void Init() 
    {
        Update();
    }

    public void Update()
    {
        acceleration = baseAcceleration * speedScaling;
    }
}