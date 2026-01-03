using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Earclipping;
using UnityEditor;
using Utility;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;

public class NodeMeshConstructor : MonoBehaviour
{
    [Header("Debug")]
    public bool db_run = false;
    [Space]
    public int db_nodesPerStep = 50;
    public float db_timePerNode;
    [Space]
    public bool db_drawPolygons;
    [UnityEngine.Range(0,200)]
    public int db_focusedPolyIdx = 0;
    public bool db_drawPoints;

    [Header("Values")]
    public LNode_Manager s_nodeManager;
    [Space]
    public float m_roadWidth;
    public float m_nodeRadius;
    public bool m_doubleSided;
    [Space]
    public bool m_extrude;
    public float m_extrusionDepth;

    internal bool meshCreated;
    internal Dictionary<uint, List<Polygon>> polygons = null;

    private int m_nodeCounter = 0;

    // Start is called before the first frame update
    void Start()
    {
        polygons = null;

        meshCreated = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (db_run) 
        { 
            if (s_nodeManager.nodeGenDone && polygons == null && !meshCreated)
            {
                db_run = false;
                StartCoroutine(CreatePolygonFromNodes());
            }
        }
    }

    IEnumerator CreatePolygonFromNodes() 
    {
        polygons = new Dictionary<uint, List<Polygon>>();

        m_nodeCounter = 0;

        foreach (Node node in s_nodeManager.AllNodes())
        {
            if (node.m_connections.Count == 0) continue;

            polygons.TryAdd(node.m_id, new List<Polygon>());
            polygons[node.m_id].Add(PolyFromNode(node));

            if (++m_nodeCounter % db_nodesPerStep == 0)
            {
                if (db_timePerNode > 0) { yield return new WaitForSeconds(db_timePerNode); }
            }
        }

        Debug.Log("Polygons Created");
        meshCreated = true;
    }

    public Polygon PolyFromNode(Node node) 
    {
        //this guarantees that the connections are in a clockwise order
        node.SortConnections();

        //creates the intial set of node lines
        List<Line> nodeLines = GetNodePolygonLines(node);

        if (node.m_connections.Count == 1)
        {
            //this is a dead end node so we need to draw around the node a lil extra
            Vector3[] points = new Vector3[4];
            Vector3 farPoint = node.m_point + ((node.m_point - node.m_connections[0].m_point).normalized * m_roadWidth);
            Quaternion rotation = Quaternion.LookRotation(node.m_connections[0].m_point - node.m_point, Vector3.up);

            //close points
            points[0] = nodeLines[^1].b;
            points[3] = nodeLines[0].a;

            //middle points
            points[1] = farPoint + (rotation * (Vector3.right * m_roadWidth * 0.75f));
            points[2] = farPoint + (rotation * (-Vector3.right * m_roadWidth * 0.75f));

            nodeLines.Add(new Line(points[0], points[1]));
            nodeLines.Add(new Line(points[1], points[2]));
            nodeLines.Add(new Line(points[2], points[3]));
        }
        else
        {
            Line newLine = new Line(nodeLines[0].a, nodeLines[^1].b);

            foreach (Node conn in node.m_connections)
            {
                if (newLine.DoesIntersect(node.m_point, conn.m_point, out Vector3 iPoint))
                {
                    Vector3 direction = (node.m_point - Vector3.Lerp(newLine.a, newLine.b, 0.5f)).normalized;
                    Line otherLine = new Line(node.m_point + (direction * (m_roadWidth * 0.25f)), newLine.b);
                    newLine.b = otherLine.a;
                    nodeLines.Add(otherLine);
                    break;
                }
            }

            nodeLines.Add(newLine);
        }
        
        
        //untangling any overlapping lines in the node before adding the final connection line in
        int lineCount = nodeLines.Count;
        //For each line in nodeLines
        for (int mainIdx = 0; mainIdx < lineCount; ++mainIdx)
        {
            //Check against all other node lines after the mainIdx
            for (int comparisonIdx = mainIdx; comparisonIdx < lineCount; ++comparisonIdx)
            {
                if (comparisonIdx == mainIdx) continue;

                //If the 2 lines intersect
                if (nodeLines[mainIdx].DoesIntersect(nodeLines[comparisonIdx], out Vector3 intersection))
                {
                    //For each line between mainIdx and comparisonIdx

                    int removedCount = 0;

                    //if the first line has overlapped the last line
                    int startIdx = mainIdx == 0 ? comparisonIdx : mainIdx;
                    int endIdx = mainIdx == 0 ? mainIdx : comparisonIdx;
                    int removalIdx = startIdx + 1;
                    int totalLoops = Mathf.Abs(startIdx - endIdx) - 1;

                    if (removalIdx < nodeLines.Count)
                    {
                        for (int loop = 0; loop < totalLoops; ++loop)
                        {
                            nodeLines.RemoveAt(removalIdx);
                            --lineCount;
                            ++removedCount;
                            if (mainIdx != 0) { --comparisonIdx;}

                            if (removalIdx >= nodeLines.Count || removalIdx == comparisonIdx)
                            {   
                                break;
                            }
                        }
                    }

                    //replace the point closer to the node center, with the intersection point
                    if (nodeLines[mainIdx].CloserToA(node.m_point)) { nodeLines[mainIdx].a = intersection; }
                    else { nodeLines[mainIdx].b = intersection; }
                    //nodeLines[mainIdx].DebugDraw(Color.red, 100, Vector3.up, true);

                    if (nodeLines[comparisonIdx].CloserToA(node.m_point)) { nodeLines[comparisonIdx].a = intersection; }
                    else { nodeLines[comparisonIdx].b = intersection; }
                    //nodeLines[comparisonIdx].DebugDraw(Color.purple, 100, Vector3.up, true);

                    //Debug checks
                    if (nodeLines[mainIdx].DoesIntersect(nodeLines[comparisonIdx], out Vector3 _)) 
                    {
                        Debug.DrawLine(intersection + Vector3.down, intersection + Vector3.up, Color.red, 300);
                        nodeLines[mainIdx].DebugDraw(Color.green, 300, Vector3.up * 0.5f);
                        nodeLines[comparisonIdx].DebugDraw(Color.yellow, 300, Vector3.up * 0.5f);
                    }
                }
            }
        }
        

        Polygon poly = new Polygon(nodeLines, node.m_point);

        return m_extrude ? ExtrudeNodePolygon(poly, node) : poly;
    }

    private List<Line> GetNodePolygonLines(Node _node) 
    {
        List<Line> nodeLines = new List<Line>();

        foreach (Node conn in _node.m_connections)
        {
            Vector3[] corners = GetNodeToConnectionPolygonCorners(_node, conn);

            Line[] lines = new Line[3];

            lines[0] = new Line(corners[0], corners[1]);
            lines[1] = new Line(corners[1], corners[2]);
            lines[2] = new Line(corners[2], corners[3]);

            UpdateNodeToConnectionLine(_node, lines[0], true);
            UpdateNodeToConnectionLine(_node, lines[2], false);

            if (nodeLines.Count > 0)
            {
                //this connects the last point from the previous segment to the start point of this section
                Line linkingLine = new Line(nodeLines[^1].b, lines[0].a);

                //Does the linkingLine overlap the centre 
                if (linkingLine.DoesIntersect(_node.m_point, conn.m_point, out Vector3 intersectionPoint))
                {
                    Vector3 direction = (_node.m_point - Vector3.Lerp(linkingLine.a, linkingLine.b, 0.5f)).normalized;
                    Line overlapFixLine = new Line(linkingLine.a, _node.m_point + (direction * (m_roadWidth * 0.5f)));
                    linkingLine.a = overlapFixLine.b;
                    nodeLines.Add(overlapFixLine);
                }

                nodeLines.Add(linkingLine);
            }

            nodeLines.AddRange(lines);
        }

        return nodeLines;
    }

    private Vector3[] GetNodeToConnectionPolygonCorners(Node _node, Node _conn) 
    {
        Vector3[] corners = new Vector3[4];

        //find midpoint from node to conn
        Vector3 lineEnd = Vector3.Lerp(_node.m_point, _conn.m_point, 0.5f);
        //get the forward rotation Node>>Point
        Quaternion forwardRotation = Quaternion.LookRotation(_conn.m_point - _node.m_point, Vector3.up);

        //Bottom Left
        corners[0] = _node.m_point + (forwardRotation * (-Vector3.right * m_roadWidth));
        //Top Left
        corners[1] = lineEnd + (forwardRotation * (-Vector3.right * m_roadWidth));
        //Top Right
        corners[2] = lineEnd + (forwardRotation * (Vector3.right * m_roadWidth));
        //Bottom Right
        corners[3] = _node.m_point + (forwardRotation * (Vector3.right * m_roadWidth));

        return corners;
    }

    private void UpdateNodeToConnectionLine(Node _node, Line _line, bool _updateStart = true) 
    {
        if (_line.CircleIntersections(_node.m_point, m_nodeRadius, out Vector3[] intersections))
        {
            Vector3 point = intersections[0];

            if (intersections.Length == 2)
            {
                //line goes b->a = 2nd->1st intersection
                Line intersectionLine = new Line(intersections[1], intersections[0]);
                //is the point of the line furthest from to the center of the node, closer to the second intersection point 
                if (intersectionLine.CloserToA(
                    _line.CloserToA(_node.m_point) ? _line.b : _line.a)
                    )
                {
                    point = intersections[1];
                }
            }

            if (_updateStart)
            {
                _line.a = point;
            }
            else
            {
                _line.b = point;
            }
        }
        else
        {
            Debug.Log("[NMC] No intersections found - line not updated");
        }
    }

    private Polygon ExtrudeNodePolygon(Polygon _poly, Node _node) 
    {
        Vector3[] extrudedVertices = new Vector3[_poly.vertices.Length];

        for (int i = 0; i < extrudedVertices.Length; i++)
        {
            extrudedVertices[i] = _poly.vertices[i].point + (Vector3.up * m_extrusionDepth);
        }

        Polygon extrudedPoly = new Polygon(_node.m_point, extrudedVertices);

        Line[] connectionLines = new Line[_node.m_connections.Count];
        for (int i = 0; i < _node.m_connections.Count; i++)
        {
            connectionLines[i] = new Line(_node.m_point, _node.m_connections[i].m_point);
        }

        //each of these arrays are individual polygons
        List<Vector3[]> wallVertices = new List<Vector3[]>();

        int start = 0;
        int end = _poly.vertices.Length;
        int next = 1;
        int current = 0;
        Line testLine = new Line(Vector3.zero, Vector3.forward);

        List<Vector3> verts = new List<Vector3>();
        while (current < _poly.vertices.Length)
        {
            verts.Add(_poly.vertices[current].point);

            next = Lists.ClampListIndex(current + 1, _poly.vertices.Length);
            testLine.a = _poly.vertices[current].point;
            testLine.b = _poly.vertices[next].point;
            //testLine.DebugDraw(Color.green, 120);

            bool intersects = false;
            foreach (Line line in connectionLines)
            {
                if (line.DoesIntersect(testLine, out Vector3 intersect))
                {
                    intersects = true;
                    break;
                }
            }

            if (intersects)
            {
                end = next;

                //we need to loop back around to the start idx in the extrudedVerts
                while (current >= start)
                {
                    verts.Add(extrudedVertices[current]);
                    current--;
                }

                //then we add a connection to poly.vertices.start
                verts.Add(_poly.vertices[start].point);

                //then we add verts to wallVertices and clear verts
                wallVertices.Add(verts.ToArray());
                verts.Clear();

                //then we jump to the start of the next poly
                current = start = end;
                end = _poly.vertices.Length;
            }
            else
            {
                ++current;
            }
        }

        //to exit the prev loop current needs to be out of index range, so we bring it back in here
        --current;
        //we need to add the last set of verts that didn't get added by an interception.
        if (verts.Count > 0)
        {
            //we need to loop back around to the start idx in the eVerts
            while (current >= start)
            {
                verts.Add(extrudedVertices[current]);
                --current;
            }

            //then we add a connection to poly.vertices.start
            verts.Add(_poly.vertices[start].point);

            //then we add verts to wallVertices and clear verts
            wallVertices.Add(verts.ToArray());
        }

        _poly.AddConnectedPolygon(extrudedPoly);
        foreach (Vector3[] vertexArray in wallVertices)
        {
            _poly.AddConnectedPolygon(new Polygon(_node.m_point, vertexArray, true));
        }
        _poly.isThreeD = true;

        return _poly;
    }

    private void OnValidate()
    {
        if(s_nodeManager != null) ValueClamps(s_nodeManager.clampValues);
    }

    public void ValueClamps(bool forceUpdate = false)
    {
        if (m_nodeRadius >= (s_nodeManager.m_nodeLimitRange.min / 2)*0.75f || forceUpdate)
        {
            //nodeRadius = (nodeManager.nodeLimitRange.min / 2) * 0.75f;
        }

        if (m_roadWidth > m_nodeRadius * 0.75f || forceUpdate)
        {
            //roadWidth = nodeRadius * 0.75f;
        }
    }

    private void OnDrawGizmos()
    {
        if (polygons != null && (db_drawPolygons || db_drawPoints))
        {
            foreach (List<Polygon> polyList in polygons.Values)
            {
                for (int i = 0; i < polyList.Count; i++)
                {
                    for (int j = 0; j < polyList[i].vertices.Length; j++)
                    {
                        if (j > 0 && db_drawPolygons)
                        {
                            Gizmos.color = Color.cyan;
                            Gizmos.DrawLine(polyList[i].vertices[j].point, polyList[i].vertices[j - 1].point);
                        }

                        if (db_drawPoints)
                        {
                            Gizmos.color = new Color(1, 0, 0, 0.1f);
                            Gizmos.DrawSphere(polyList[i].vertices[j].point, 0.2f);

                            if (db_focusedPolyIdx == i)
                            {
                                Handles.Label(polyList[i].vertices[j].point + (Vector3.up * 0.1f * (j + 1)), j.ToString());
                            }
                        }
                    }
                }
            }
           
        }
    }
}
