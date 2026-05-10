using UnityEngine;

public class DropZone : MonoBehaviour
{
    TruckController m_player;

    private void Start()
    {
        m_player = FindAnyObjectByType<TruckController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == m_player.m_bodyTransform.gameObject)
        {
            Delivery_Manager.Instance.SelectRandomDelivery();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.gameObject == m_player.m_bodyTransform.gameObject)
        {
            Delivery_Manager.Instance.SelectRandomDelivery();
        }
    }
}
