using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;
using NodeMap = System.Collections.Generic.Dictionary<UnityEngine.Vector2Int, System.Collections.Generic.List<Node>>;
using NUnit.Framework;

/// <summary>
/// Uses an L system to generate a sequence of roads, and then creates a mesh for each of them
/// </summary>
public class LNode_Manager : Singleton<LNode_Manager>
{

    [Space]
    public bool showConnections = false;
    public bool showNodes = false;
    [Space]
    public LSystem lSys = new LSystem();
    public int count = 5;
    [Space]
    public bool clampValues = false;
    public int m_length;
    [Space]
    public int angle;
    //below the minimum the nodes combine, above the maximum connections are broken
    public Range m_nodeLimitRange;
    public int nodesPerStep = 50;
    public float timePerStep;
    //internal List<Node> nodes = new List<Node>();
    internal NodeMap m_nodeMap = new NodeMap();

    internal bool nodeGenDone = false;

    public void Start()
    {
        VisualizeSequence();
    }

    //This is the start point of generating a route
    public void VisualizeSequence() 
    {
        lSys.GenerateSequence(count);
        StartCoroutine(CreateRouteCoroutine(lSys.finalString));
    }

    public IEnumerator CreateRouteCoroutine(string _sequence)
    {
        int counter = 0;
        Stack<LAgent> savePoints = new Stack<LAgent>();
        Vector3 currentPos = transform.position;
        Vector3 tempPos = currentPos;
        Vector3 direction = Vector3.forward;

        m_nodeMap = new NodeMap();
        Vector2Int startingMapKey = WorldPosToMapKey(currentPos);
        m_nodeMap.TryAdd(startingMapKey, new List<Node>());
        m_nodeMap[startingMapKey].Add(new Node(currentPos));
        Node prevNode = m_nodeMap[startingMapKey][0];

        foreach (char letter in _sequence)
        {
            Instructions _instruction = (Instructions)letter;
            switch (_instruction)
            {
                case Instructions.DRAW:
                    currentPos += direction * m_length;
                    prevNode = AddNode(currentPos, prevNode);
                    counter++;
                    tempPos = currentPos;
                    break;

                case Instructions.LEFT_TURN:
                    direction = Quaternion.Euler(0, angle * -1, 0) * direction;
                    break;

                case Instructions.RIGHT_TURN:
                    direction = Quaternion.Euler(0, angle, 0) * direction;
                    break;

                case Instructions.SAVE:
                    savePoints.Push(new LAgent(currentPos, tempPos, direction, m_length));
                    break;

                case Instructions.LOAD:
                    if (savePoints.Count > 0)
                    {
                        LAgent ag = savePoints.Pop();
                        currentPos = ag.position;
                        tempPos = ag.tempPos;
                        direction = ag.direction;
                        m_length = ag.length;
                        List<Node> localNodes = GetNodesInRange(currentPos, (int)Math.Ceiling(m_nodeLimitRange.min));

                        if (localNodes.Count == 0) { Debug.LogError("[LNM] No nodes found at loaded position"); }
                        foreach (Node item in localNodes)
                        {
                            if (Vector3.Distance(currentPos, item.point) <= m_nodeLimitRange.min)
                            {
                                prevNode = item;
                                break;
                            }
                        }

                        //Debug.DrawLine(currentPos + (Vector3.up * 2), currentPos + (-direction * length), Color.orange, 300);
                        //Debug.LogError("[LNM] Didn't find a current node");
                    }
                    break;

                default:
                    break;
            }

            //Debug.Log("Counter = " + counter);
            if (counter % nodesPerStep == 0)
            {
                //Debug.Log("Step");
                yield return new WaitForSeconds(timePerStep);
            }
        }

        Debug.Log("[LNM] Sequence Plotted - Starting Untangling of intersections");

        //sort each nodes connections
        counter = 0;
        foreach (List<Node> nodeList in m_nodeMap.Values) 
        {
            foreach (Node item in nodeList)
            {
                ++counter;

                UntangleNode(item);
                item.SortConnections();

                if (counter % nodesPerStep == 0)
                {
                    yield return new WaitForSeconds(timePerStep);
                }
            }
        }

        Debug.Log("[LNM] Nodes Untangled - Generation complete (Node Count = " + counter + ")");

        nodeGenDone = true;
    }

    private Node AddNode(Vector3 _position, Node _parent)
    {
        Node nodeAtPosition = new Node(_position);

        List<Node> nodesInRange = GetNodesInRange(_position, (int)Math.Ceiling(m_nodeLimitRange.min));
        foreach (Node item in nodesInRange)
        {
            if (_position == item.point || Vector3.Distance(_position, item.point) <= m_nodeLimitRange.min)
            {
                nodeAtPosition = item;
                break;
            }
        }

        nodeAtPosition.AddConnection(_parent);

        Vector2Int mapKey = WorldPosToMapKey(nodeAtPosition.point);
        m_nodeMap.TryAdd(mapKey, new List<Node>());
        m_nodeMap[mapKey].Add(nodeAtPosition);

        return nodeAtPosition;
    }

    private void UntangleNode(Node focusNode)
    {
        //Untangle any connection crossovers
        List<Node> nodesInRange = GetNodesInRange(focusNode.point, (int)Math.Ceiling(m_nodeLimitRange.max) + 1);

        for (int c = 0; c < focusNode.connections.Count; ++c)
        {
            Node connectedNode = focusNode.connections[c];

            for (int i = 0; i < nodesInRange.Count; ++i)
            {
                if (connectedNode == nodesInRange[i] || focusNode == nodesInRange[i]) { continue; }
                Node checkingNode = nodesInRange[i];

                if (TestConnectionIntersectionsWithLine(focusNode, connectedNode, checkingNode, out Node intersectedNode))
                {
                    focusNode.RemoveConnection(connectedNode);
                    checkingNode.RemoveConnection(intersectedNode);

                    focusNode.AddConnection(checkingNode);
                    focusNode.AddConnection(intersectedNode);

                    connectedNode.AddConnection(checkingNode);
                    connectedNode.AddConnection(intersectedNode);

                    --c;
                    break;
                }
            }
        }

        
    }

    public void ValueClamps(bool _forceUpdate = false) 
    {
        if (m_nodeLimitRange.min >= m_length / 2.0f || _forceUpdate)
        {
            m_nodeLimitRange.min = m_length / 2.0f;
        }

        if (m_nodeLimitRange.max <= m_length * 2.0f || _forceUpdate)
        {
            m_nodeLimitRange.max = m_length * 2.0f;
        }
    }

    bool TestConnectionIntersectionsWithLine(Node _startNode, Node _endNode, Node _checkNode, out Node _intersectedNode) 
    {
        Line lineToParent = new Line(_startNode.point, _endNode.point);
        Line lineBetweenConnections = new Line(_checkNode.point, Vector3.zero);

        _intersectedNode = _checkNode.connections[0];
        for (int i = 0; i < _checkNode.connections.Count; ++i)
        {
            if (_intersectedNode == _endNode || _intersectedNode == _startNode) { continue; }
            _intersectedNode = _checkNode.connections[i];

            lineBetweenConnections.b = _intersectedNode.point;

            if (lineToParent.DoesIntersect(lineBetweenConnections, out Vector3 intersection))
            {
                return true;
            }
        }

        return false;
    }

    public Vector2Int WorldPosToMapKey(Vector3 _position)
    {
        return new Vector2Int((int)Math.Floor(_position.x), (int)Math.Floor(_position.z));
    }

    public List<Node> GetNodesInRange(Vector3 _position, int _range) 
    {
        List<Node> nodeList = new List<Node>();

        Vector2Int centralKey = WorldPosToMapKey(_position);
        Vector2Int rangeKey = new Vector2Int(0,0);

        for (int x = -_range; x < _range; ++x)
        {
            for (int y = -_range; y < _range; ++y)
            {
                rangeKey.x = centralKey.x + x;
                rangeKey.y = centralKey.y + y;

                if (m_nodeMap.ContainsKey(rangeKey)) 
                {
                    nodeList.AddRange(m_nodeMap[rangeKey]);
                }
            }
        }

        return nodeList;
    }

    public List<Node> AllNodes()
    {
        List<Node> allNodes = new List<Node>();

        foreach (List<Node> nodeList in m_nodeMap.Values)
        {
            allNodes.AddRange(nodeList);
        }

        return allNodes;
    }

    private void OnValidate()
    {
        //ValueClamps(clampValues);
    }

    private void OnDrawGizmos()
    {
        if (m_nodeMap != null)
        {
            foreach (List<Node> nodeList in m_nodeMap.Values)
            {
                foreach (Node item in nodeList)
                {
                    if (showNodes)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawSphere(item.point, m_length / 10.0f);
                    }

                    if (showConnections)
                    {
                        foreach (Node node in item.connections)
                        {
                            Gizmos.color = Color.blue;
                            Gizmos.DrawLine(item.point, item.point + ((node.point - item.point) * 0.5f));
                        }
                    }
                }
            }
        }
    }
}

//[System.Serializable]
public class Node
{
    public Vector3 point;
    internal Vector3 forward;
    internal List<Node> connections;

    public Node(Vector3 m_point, Node parent = null) 
    {
        point = m_point;
        connections = new List<Node>();
        if (parent != null)
        {
            AddConnection(parent);
            forward = (point - parent.point).normalized;
        }
        else
        {
            forward = Vector3.forward;
        }
    }

    public void AddConnection(Node node) 
    {

        if (node == this || connections.Contains(node) || Vector3.Distance(point, node.point) >= LNode_Manager.Instance.m_nodeLimitRange.max)
        {
            if (Vector3.Distance(point, node.point) >= LNode_Manager.Instance.m_nodeLimitRange.max)
            {
                Debug.DrawLine(point, node.point, Color.purple, 300);
            }
            return;
        }
        else
        {
            connections.Add(node);
            node.connections.Add(this);
        }

    }

    public void RemoveConnection(Node node) 
    {
        if (node == this || !connections.Contains(node))
        { return; }
        else
        {
            node.connections.Remove(this);
            connections.Remove(node);
        }
    }

    public void RemoveConnection(int i) 
    {
        RemoveConnection(connections[i]);
    }

    public void SortConnections()
    {
        connections.Sort(new ClockwiseComparer(Vector2.right));
    }
}

public class ClockwiseComparer : IComparer<Node>
{
    private Vector2 m_Origin;

    #region Properties

    public Vector2 origin { get { return m_Origin; } set { m_Origin = value; } }

    #endregion

    /// <summary>
    ///     Initializes a new instance of the ClockwiseComparer class.
    /// </summary>
    /// <param name="origin">Origin.</param>
    public ClockwiseComparer(Vector2 origin)
    {
        m_Origin = origin;
    }

    #region IComparer Methods

    /// <summary>
    ///     Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
    /// </summary>
    /// <param name="first">First.</param>
    /// <param name="second">Second.</param>
    public int Compare(Node first, Node second)
    {
        return IsClockwise(first.point, second.point, m_Origin);
    }

    #endregion

    /// <summary>
    ///     Returns 1 if first comes before second in clockwise order.
    ///     Returns -1 if second comes before first.
    ///     Returns 0 if the points are identical.
    /// </summary>
    /// <param name="first">First.</param>
    /// <param name="second">Second.</param>
    /// <param name="origin">Origin.</param>
    public static int IsClockwise(Vector2 first, Vector2 second, Vector2 origin)
    {
        if (first == second)
            return 0;

        Vector2 firstOffset = first - origin;
        Vector2 secondOffset = second - origin;

        float angle1 = Mathf.Atan2(firstOffset.x, firstOffset.y);
        float angle2 = Mathf.Atan2(secondOffset.x, secondOffset.y);

        if (angle1 < angle2)
            return -1;

        if (angle1 > angle2)
            return 1;

        // Check to see which point is closest
        //return (firstOffset.sqrMagnitude < secondOffset.sqrMagnitude) ? -1 : 1;

        return (firstOffset.sqrMagnitude < secondOffset.sqrMagnitude) ? -1 : 1;
    }
}