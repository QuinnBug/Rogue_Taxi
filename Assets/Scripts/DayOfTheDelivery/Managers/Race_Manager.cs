
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Race_Manager : Singleton<Race_Manager>
{
    public float m_checkpointRadius = 5;
    public float m_timePerCycle = 0.01f;
    private Dictionary<Transform, int> m_checkpointTracker = new Dictionary<Transform, int>();

    private List<Node> m_checkpoints = new List<Node>();
    internal bool raceStarted = false;

    private void Start()
    {
        Event_Manager.Instance.AddListener(E_Event.Buildings, E_Action.Finished, Init);
    }

    void Init()
    {
        GenerateRaceCourse();
        m_checkpointTracker[FindAnyObjectByType<Player>().transform] = 0;
    }

    void Update()
    {
        if (!raceStarted) { return; }

        foreach (Transform tf in m_checkpointTracker.Keys)
        {
            if (Vector3.Distance(tf.position, m_checkpoints[m_checkpointTracker[tf]].m_point) < m_checkpointRadius) 
            {
                ++m_checkpointTracker[tf];

                if (m_checkpointTracker[tf] >= m_checkpoints.Count)
                {
                    Debug.Log("Race Complete");
                    m_checkpointTracker[tf] = 0;
                }
                break;
            }
        }
    }

    private void GenerateRaceCourse()
    {
        var nodeMgr = LNode_Manager.Instance;

        var areas = nodeMgr.m_nodeMap.Keys;

        Vector2Int lowestZone = Vector2Int.one * 99999;
        Vector2Int highestZone = Vector2Int.one * -99999;

        foreach (Vector2Int area in areas) 
        {
            if (area.x + area.y < lowestZone.x + lowestZone.y)
            {
                lowestZone = area;
            }
            else if (area.x + area.y > highestZone.x + highestZone.y)
            {
                highestZone = area;
            }
        }

        Debug.Log(lowestZone + " -- " + highestZone);

        Node startNode = nodeMgr.GetRandomNode(lowestZone);
        Node endNode = nodeMgr.GetRandomNode(highestZone);

        StartCoroutine(CalculatePathBetweenNodes(startNode, endNode));
    }

    private IEnumerator CalculatePathBetweenNodes(Node start, Node end)
    {
        Dictionary<Node, List<Node>> openNodes = new Dictionary<Node, List<Node>>();
        List<Node> closedNodes = new List<Node>();

        Node currentNode = start;
        openNodes.Add(start, new List<Node>());

        while (openNodes.Count > 0) 
        {
            Debug.Log("Open Nodes count = " + openNodes.Count);
            yield return new WaitForSeconds(m_timePerCycle);

            var currentPath = openNodes[currentNode];

            openNodes.Remove(currentNode);
            closedNodes.Add(currentNode);

            foreach (Node neighbour in currentNode.m_connections)
            {
                if (closedNodes.Contains(neighbour) || openNodes.ContainsKey(neighbour))
                {
                    continue;
                }

                var pathToNeighbour = new List<Node>(currentPath);
                pathToNeighbour.Add(neighbour);

                //We've reached the neighbour, exploring only the closest options
                if (neighbour == end)
                {
                    m_checkpoints = pathToNeighbour;
                    raceStarted = true;
                    Event_Manager.Instance.InvokeEvent(E_Event.Race, E_Action.Finished);
                    break;
                }

                openNodes.Add(neighbour, pathToNeighbour);
            }


            Node closestNode = currentNode;
            float shortestDist = Mathf.Infinity;
            foreach (Node node in openNodes.Keys)
            {
                float nextDist = Vector3.Distance(node.m_point, end.m_point);
                if (nextDist < shortestDist)
                {
                    closestNode = node;
                    shortestDist = nextDist;
                }
            }

            currentNode = closestNode;
        }

        if (raceStarted)
        {
            Debug.Log("RACE STARTED");

            for (int i = 1; i < m_checkpoints.Count; ++i)
            {
                int j = i - 1;

                Debug.DrawLine(m_checkpoints[j].m_point, m_checkpoints[i].m_point, Color.aquamarine, 500);
            }
        }
        else
        {
            //There was no route between the nodes
            Debug.Log("FAILED TO FIND PATH");
        }
    }

    public Node GetCheckpoint(Transform tf) 
    {
        return m_checkpoints[m_checkpointTracker[tf]];
    }
}

struct PathNode
{
    public Node node;
    public List<Node> path;

    public PathNode(Node node, List<Node> path) : this()
    {
        this.node = node;
        this.path = path;
    }
}
