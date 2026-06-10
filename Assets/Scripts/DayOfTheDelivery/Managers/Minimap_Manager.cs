using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Minimap_Manager : Singleton<Minimap_Manager>
{
    public TruckController m_player;
    public Transform m_focus;
    public Transform m_camera;
    [Space]
    public float m_moveSpeed;
    [Space]
    public bool m_followPlayer;

    // Update is called once per frame
    void Update()
    {
        if (m_followPlayer)
        {
            m_focus.position = m_player.transform.position;
        }

        m_camera.position = m_focus.position + (Vector3.up * 10);
    }
}
