using UnityEngine;
using UnityEngine.UI;

public class TruckUIHandler : MonoBehaviour
{
    public Image speedometer;

    TruckController player;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = FindFirstObjectByType<TruckController>();
    }

    // Update is called once per frame
    void Update()
    {
        speedometer.fillAmount = (player.m_engine.currentPercent * 0.5f); //We're only using half a circle so half the percent
    }
}
