using System;
using UnityEngine;
using Utility;

enum Screens
{
    MAP,
    DELIVERIES,
    UPGRADES,
}

public class PhoneHandler : MonoBehaviour
{
    bool m_phoneOpen;

    public GameObject m_phone;
    public GameObject[] m_screens;
    public InputHandler m_inputs;

    private bool navLock = false;
    private int currentScreen = 1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ChangeScreen(currentScreen);
    }

    // Update is called once per frame
    void Update()
    {
        if (m_inputs.phoneToggle)
        {
            m_phoneOpen = !m_phoneOpen;
            m_phone.SetActive(m_phoneOpen);
        }

        if (!m_phoneOpen) { return; }

        if (m_inputs.phoneNav.x != 0 && !navLock)
        {
            navLock = true;
            ChangeScreen(currentScreen + (m_inputs.phoneNav.x > 0 ? 1 : -1));
        }

        if (m_inputs.phoneNav.magnitude == 0)
        {
            navLock = false;
        }

        ScreenUpdate();
    }

    void ChangeScreen(int newScreen)
    {
        if (newScreen == currentScreen) { return; }

        Debug.Log("Phone Change: " + newScreen);

        m_screens[currentScreen].SetActive(false);
        currentScreen = Lists.ClampListIndex(newScreen, m_screens.Length);
        m_screens[currentScreen].SetActive(true);
    }

    private void ScreenUpdate()
    {
        switch ((Screens)currentScreen)
        {
            case Screens.MAP:
                break;
            case Screens.DELIVERIES:
                DeliveriesUpdate();
                break;
            case Screens.UPGRADES:
                break;
        }
    }

    private void DeliveriesUpdate()
    {
        if (m_inputs.phoneNav.y != 0 && !navLock)
        {
            navLock = true;
            Delivery_Manager.Instance.ChangeCurrentDelivery(m_inputs.phoneNav.y > 0 ? 1 : -1);
        }
    }
}
