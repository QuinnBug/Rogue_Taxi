using UnityEngine;
using UnityEngine.AI;

public class EnemyBase : MonoBehaviour
{
    public EnemyStats m_stats;
    [Space]
    public bool m_dead;
    [SerializeField]
    private TruckController m_player;
    private NavMeshAgent m_agent;
    private Rigidbody m_physics;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_player = FindAnyObjectByType<TruckController>();
        m_agent = GetComponent<NavMeshAgent>();
        m_physics = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!m_dead)
        {
            m_agent.SetDestination(m_player.transform.position);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.gameObject.CompareTag("Player"))
        {
            m_agent.enabled = false;
            m_dead = true;
            m_physics.linearVelocity = m_player.m_physics.linearVelocity * 1.25f;
        }
    }
}

public struct EnemyStats 
{
    public int health;
}
