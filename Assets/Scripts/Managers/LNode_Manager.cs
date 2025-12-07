using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;
using NodeList = System.Collections.Generic.List<Node>;
using NodeMap = System.Collections.Generic.Dictionary<UnityEngine.Vector2Int, System.Collections.Generic.List<Node>>;
using UnityEditor.Experimental.GraphView;
using UnityEditor;

/// <summary>
/// Uses an L system to generate a sequence of roads, and then creates a mesh for each of them
/// </summary>
public class LNode_Manager : Singleton<LNode_Manager>
{
    public bool showConnections = false;
    public bool showNodes = false;
    public bool showLabels = false;
    [Space]
    public LSystem lSys = new LSystem();
    public int count = 5;
    [Space]
    public int angle;
    public int m_length;
    //below the minimum the nodes combine, above the maximum connections are broken
    public Range m_nodeLimitRange;
    public bool clampValues = false;
    [Space]
    public int nodesPerStep = 50;
    public float timePerStep;
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
        m_nodeMap.TryAdd(startingMapKey, new NodeList());
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

            if (counter % nodesPerStep == 0)
            {
                if (timePerStep > 0) { yield return new WaitForSeconds(timePerStep);}
            }
        }

        Debug.Log("[LNM] Sequence Plotted - Starting Untangle");

        //sort each nodes connections
        bool stillTangled = false;
        do
        {
            if (stillTangled) { Debug.Log("[LNM] Rechecking Tangles"); }
            stillTangled = false;
            counter = 0;
            foreach (NodeList nodeList in m_nodeMap.Values)
            {
                foreach (Node item in nodeList)
                {
                    stillTangled |= UntangleNode(item);

                    if (++counter % nodesPerStep == 0)
                    {
                        if (timePerStep > 0) { yield return new WaitForSeconds(timePerStep); }
                    }
                }
            }
        } while (stillTangled);

        Debug.Log("[LNM] Nodes Untangled - Generation complete (Node Count = " + counter + ")");

        nodeGenDone = true;
    }

    private Node AddNode(Vector3 _position, Node _parent)
    {
        Node nodeAtPosition = new Node(_position);

        bool isNew = true;

        NodeList nodesInRange = GetNodesInRange(_position, (int)Mathf.Ceil(m_nodeLimitRange.min));
        foreach (Node item in nodesInRange)
        {
            if (_position == item.point || Vector3.Distance(_position, item.point) <= m_nodeLimitRange.min)
            {
                nodeAtPosition = item;
                isNew = false;
                break;
            }
        }

        nodeAtPosition.AddConnection(_parent);

        Vector2Int mapKey = WorldPosToMapKey(nodeAtPosition.point);

        if (isNew)
        { 
            m_nodeMap.TryAdd(mapKey, new NodeList());
            m_nodeMap[mapKey].Add(nodeAtPosition);
        }

        return nodeAtPosition;
    }

    private bool UntangleNode(Node focusNode)
    {
        bool didUntangle = false;

        //Untangle any connection crossovers

        NodeList nodesInRange = GetNodesInRange(focusNode.point, 2);

        for (int c = 0; c < focusNode.connections.Count; ++c)
        {
            Node connectedNode = focusNode.connections[c];

            for (int i = 0; i < nodesInRange.Count; ++i)
            {
                if (connectedNode == nodesInRange[i] || focusNode == nodesInRange[i]) { continue; }
                Node checkingNode = nodesInRange[i];

                if (TestConnectionIntersectionsWithLine(focusNode, connectedNode, checkingNode, out Node intersectedNode))
                {
                    didUntangle = true;

                    focusNode.RemoveConnection(connectedNode);
                    checkingNode.RemoveConnection(intersectedNode);

                    focusNode.AddConnection(checkingNode);
                    focusNode.AddConnection(intersectedNode);

                    connectedNode.AddConnection(checkingNode);
                    connectedNode.AddConnection(intersectedNode);

                    //new Line(focusNode.point,   checkingNode.point   ).DebugDraw(Color.green    , 1200, Vector3.up * 2, true);
                    //new Line(focusNode.point,   intersectedNode.point).DebugDraw(Color.purple   , 1200, Vector3.up * 2, true);
                    //new Line(checkingNode.point,intersectedNode.point).DebugDraw(Color.red      , 1200, Vector3.up);
                    //new Line(focusNode.point,   connectedNode.point  ).DebugDraw(Color.orangeRed, 1200, Vector3.up);

                    c = 0;
                    break;
                }
            }
        }

        return didUntangle;
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

        lineToParent.DebugDraw(Color.pink, 1200, Vector3.up);

        _intersectedNode = _checkNode.connections[0];
        for (int i = 0; i < _checkNode.connections.Count; ++i)
        {
            if (_intersectedNode == _endNode || _intersectedNode == _startNode) { continue; }
            _intersectedNode = _checkNode.connections[i];

            lineBetweenConnections.b = _intersectedNode.point;

            if (lineToParent.DoesIntersect(lineBetweenConnections, out Vector3 intersection))
            {
                lineToParent.DebugDraw(Color.red, 500, Vector3.up);
                lineBetweenConnections.DebugDraw(Color.navyBlue, 500, Vector3.up);
                return true;
            }
        }

        return false;
    }

    public Vector2Int WorldPosToMapKey(Vector3 _position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(_position.x / m_nodeLimitRange.max),
            Mathf.FloorToInt(_position.z / m_nodeLimitRange.max)
            );
    }

    public NodeList GetNodesInRange(Vector3 _position, int _range) 
    {
        NodeList nodeList = new NodeList();

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

    public NodeList AllNodes()
    {
        NodeList allNodes = new NodeList();

        foreach (NodeList nodeList in m_nodeMap.Values)
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
            foreach (NodeList nodeList in m_nodeMap.Values)
            {
                foreach (Node item in nodeList)
                {
                    if (showNodes)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawSphere(item.point, m_length / 15.0f);
                    }

                    if (showLabels)
                    {
                        Handles.Label(item.point + (Vector3.forward * m_length/10.0f), WorldPosToMapKey(item.point).ToString());
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
            //if (Vector3.Distance(point, node.point) >= LNode_Manager.Instance.m_nodeLimitRange.max)
            //{
            //    Debug.DrawLine(point, node.point, Color.purple, 300);
            //}
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
        ClockwiseComparer cs = new ClockwiseComparer();
        cs.current = this;
        cs.start = this.connections[0];
        connections.Sort(cs);
    }
    public class ClockwiseComparer : IComparer<Node>
    {
        public Node start, current;
    
        //returns which line starts most to the left
    
        public int Compare(Node x, Node y)
        {
            Vector3 incomingDir = Vector3.Normalize(current.point - start.point);
    
            float xRot = Vector3.SignedAngle(incomingDir, Vector3.Normalize(current.point - x.point), Vector3.up);
            float yRot = Vector3.SignedAngle(incomingDir, Vector3.Normalize(current.point - y.point), Vector3.up);
    
            if (xRot == yRot) return 0;
    
            return xRot < yRot ? -1 : 1;
        }
    }
}