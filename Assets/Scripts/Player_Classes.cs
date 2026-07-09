using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct TruckStats 
{
    // Driving
    public float revScale;

    // Steering
    [SerializeField]
    public float turnSpeed;
    [SerializeField]
    public float turnResetRate;
    [SerializeField]
    public float fWheelLimit;

}