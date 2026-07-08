using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Minimap_Manager : Singleton<Minimap_Manager>
{
    public TruckController m_player;
    public Transform m_focus;
    public Transform m_camera;
    [Space]
    public float m_height;
    public float m_forwardOffset;
    [Space]
    public float m_moveSpeed;
    [Space]
    public bool m_followPlayer;
    public bool m_fixedNorth;

    private void Start()
    {
        m_player = FindFirstObjectByType<TruckController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (m_followPlayer)
        {
            m_focus.position = m_player.transform.position + (m_player.transform.forward * m_forwardOffset);
        }
        
        var cRot = m_camera.transform.rotation.eulerAngles;
        cRot.y = m_fixedNorth ? 0 : m_player.transform.rotation.eulerAngles.y;
        m_camera.transform.rotation = Quaternion.Euler(cRot);

        m_camera.position = m_focus.position + (Vector3.up * m_height);
    }
}
