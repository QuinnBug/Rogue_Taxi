using System;
using UnityEngine;

public class BuildingData : MonoBehaviour
{
    public int m_id;
    public Vector3 m_size;

    public bool m_highlighted = false;
    public GameObject m_highlighter;

    private void Update()
    {
        if (m_highlighter)
        {
            m_highlighter.SetActive(m_highlighted);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Delivery"))
        {
            Delivery_Manager.Instance.MarkDeliveryComplete(m_id);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + (Vector3.up * m_size.y / 2), m_size);
    }
}
