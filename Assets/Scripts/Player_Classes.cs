using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PlayerStats 
{
    public float moveSpeed;
    public float turnSpeed;

    public int currentHealth;
    public int maxHealth;

    public int currentAmmo;
    public int maxAmmo;
    public float shotsPerSecond;

    public float currentFuel;
    public float maxFuel;

    public float fuelDrain;

    public void Init() 
    {
        currentHealth = maxHealth;
        currentAmmo = maxAmmo;
        currentFuel = maxFuel;
    }
}