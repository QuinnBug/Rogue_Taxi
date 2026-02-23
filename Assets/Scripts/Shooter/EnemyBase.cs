using UnityEngine;
using UnityEngine.AI;

public class EnemyBase : MonoBehaviour
{
    public EnemyStats m_stats;
    [Space]
    public bool m_dead;

    private GameObject m_player;
    private NavMeshAgent m_agent;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_player = FindAnyObjectByType<ShooterController>().gameObject;
        m_agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        m_agent.SetDestination(m_player.transform.position);
    }
}

public struct EnemyStats 
{
    public int health;
}
