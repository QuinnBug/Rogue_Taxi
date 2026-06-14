using UnityEngine;
using Utility;

public class PhoneHandler : MonoBehaviour
{
    bool m_phoneOpen;

    public GameObject m_phone;
    public GameObject[] m_screens;
    public InputHandler m_inputs;

    private bool screenChangeLock = false;
    private int currentScreen = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ChangeScreen(currentScreen + 1);
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

        if (m_inputs.phoneNav.x != 0)
        {
            if (!screenChangeLock)
            {
                screenChangeLock = true;
                ChangeScreen(currentScreen + (m_inputs.phoneNav.x > 0 ? 1 : -1));
            }
        }
        else
        {
            screenChangeLock = false;
        }
    }

    void ChangeScreen(int newScreen)
    {
        if (newScreen == currentScreen) { return; }

        Debug.Log("Phone Change: " + newScreen);

        m_screens[currentScreen].SetActive(false);
        currentScreen = Lists.ClampListIndex(newScreen, m_screens.Length);
        m_screens[currentScreen].SetActive(true);
    }
}
