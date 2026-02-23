using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : Singleton<EnemySpawner>
{
    public bool d_doSpawning;
    [Space]
    public int m_MaxEnemyCount;
    public int m_SpawnRange;
    public Range<float> m_SpawnDelay;
    public Range<int> m_SpawnCount;
    [Space]
    public List<GameObject> m_enemyPrefabs;

    private LNode_Manager m_lnm;
    private GameObject m_player;
    private float m_SpawnTimer;

    private List<GameObject> m_enemies = new List<GameObject>();

    private void Start()
    {
        m_lnm = GameObject.FindAnyObjectByType<LNode_Manager>();
        m_player = GameObject.FindAnyObjectByType<ShooterController>().gameObject;
    }

    private void Update()
    {
        if (!d_doSpawning) { return; }

        if (m_SpawnTimer >= 0)
        {
            m_SpawnTimer -= Time.deltaTime;
        }
        else
        {
            SpawnWave();
            m_SpawnTimer += m_SpawnDelay.RandomValue();
        }
    }

    private void SpawnWave()
    {
        int count = m_SpawnCount.RandomValue();

        for (int i = 0; i < count; ++i)
        {
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        if (m_enemies.Count >= m_MaxEnemyCount) { return; }

        List<Node> validNodes = m_lnm.GetNodesInRange(m_player.transform.position, m_SpawnRange);
        int nodeIdx = UnityEngine.Random.Range(0, validNodes.Count);
        Vector3 position = validNodes[nodeIdx].m_point;

        int enemyIdx = UnityEngine.Random.Range(0, m_enemyPrefabs.Count);

        m_enemies.Add(Instantiate(m_enemyPrefabs[enemyIdx], position, Quaternion.identity, transform));
    }
}
