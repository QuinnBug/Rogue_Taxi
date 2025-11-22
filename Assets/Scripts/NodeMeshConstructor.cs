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
    public LNode_Manager s_nodeManager;

    public float m_roadWidth;
    public float m_nodeRadius;
    public bool m_doubleSided;
    [Space]
    public bool m_extrude;
    public float m_extrusionDepth;
    [Space]
    public bool drawPoints;
    public bool drawPolygons;
    [Space]
    public int nodesPerStep = 50;
    public float timePerNode;

    internal bool meshCreated;
    internal List<Polygon> polygons = null;

    // Start is called before the first frame update
    void Start()
    {
        polygons = null;

        meshCreated = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (s_nodeManager.nodeGenDone && polygons == null && !meshCreated)
        {
            StartCoroutine(CreatePolygonFromNodes());
        }
    }

    IEnumerator CreatePolygonFromNodes() 
    {
        polygons = new List<Polygon>();

        int counter = 0;

        foreach (Node node in s_nodeManager.AllNodes())
        {
            if (node.connections.Count == 0) continue;

            polygons.Add(PolyFromNode(node));
            if (++counter % nodesPerStep == 0)
            {
                if (timePerNode > 0) { yield return new WaitForSeconds(timePerNode); }
            }
        }

        Debug.Log("Mesh Created");
        meshCreated = true;
    }

    public Polygon PolyFromNode(Node node) 
    {
        //this guarantees that the connections are in a clockwise order
        ConnectionSort cs = new ConnectionSort();
        cs.current = node;
        cs.start = node.connections[0];
        node.connections.Sort(cs);

        //creates the intial set of node lines
        List<Line> nodeLines = GetInitialNodeLines(node);

        if (node.connections.Count == 1)
        {
            //this is a dead end node so we need to draw around the node a lil extra
            Vector3[] points = new Vector3[4];
            Vector3 farPoint = node.point + ((node.point - node.connections[0].point).normalized * m_roadWidth);
            Quaternion rotation = Quaternion.LookRotation(node.connections[0].point - node.point, Vector3.up);

            //close points
            points[0] = nodeLines[0].a;
            points[3] = nodeLines[nodeLines.Count - 1].b;

            //middle points
            points[1] = farPoint + (rotation * (-Vector3.right * m_roadWidth));
            points[2] = farPoint + (rotation * (Vector3.right * m_roadWidth));

            nodeLines.Add(new Line(points[0], points[1]));
            nodeLines.Add(new Line(points[1], points[2]));
            nodeLines.Add(new Line(points[2], points[3]));
        }
        else
        {
            //this draws a line from the start point of the the latest line to the last point of the last line
            Line newLine = new Line(nodeLines[2].b, nodeLines[nodeLines.Count - 3].a);

            foreach (Node conn in node.connections)
            {
                if (newLine.DoesIntersect(node.point, conn.point, out Vector3 iPoint))
                {
                    Vector3 direction = (node.point - Vector3.Lerp(newLine.a, newLine.b, 0.5f)).normalized;
                    Line otherLine = new Line(node.point + (direction * (m_roadWidth * 0.25f)), newLine.b);
                    newLine.b = otherLine.a;
                    nodeLines.Add(otherLine);
                    break;
                }
            }

            nodeLines.Add(newLine);
        }

        //untangling any overlapping lines in the node before adding the final connection line in
        int lineCount = nodeLines.Count;
        for (int i = 0; i < lineCount; i++)
        {
            for (int j = i; j < lineCount; j++)
            {
                if (j == i) continue;

                if (nodeLines[i].DoesIntersect(nodeLines[j], out Vector3 intersection))
                {
                    int m = -1;
                    for (int k = i; k < lineCount; k++)
                    {
                        if (nodeLines[k] == nodeLines[i] || nodeLines[k] == nodeLines[j]) continue;
                        if (nodeLines[k].SharesPoints(nodeLines[i]) && nodeLines[k].SharesPoints(nodeLines[j]))
                        {
                            m = k;
                            break;
                        }
                    }

                    if (m == -1)
                    {
                        Debug.Log("There's an issue here");
                    }
                    else
                    {
                        if (nodeLines[i].CloserToA(node.point)) nodeLines[i].a = intersection;
                        else nodeLines[i].b = intersection;

                        if (nodeLines[j].CloserToA(node.point)) nodeLines[j].a = intersection;
                        else nodeLines[j].b = intersection;

                        nodeLines.RemoveAt(m);
                        lineCount--;
                        j--;
                    }
                }
            }
        }

        Polygon poly = new Polygon(nodeLines, node.point);

        return m_extrude ? ExtrudeNodePolygon(poly, node) : poly;
    }

    private List<Line> GetInitialNodeLines(Node _node) 
    {
        List<Line> nodeLines = new List<Line>();

        foreach (Node conn in _node.connections)
        {
            //find midpoint from node to conn
            Vector3 farPoint = Vector3.Lerp(_node.point, conn.point, 0.5f);
            Quaternion rotation = Quaternion.LookRotation(conn.point - _node.point, Vector3.up);
            Vector3[] points = new Vector3[4];

            //close points
            points[0] = _node.point + (rotation * (-Vector3.right * m_roadWidth));
            points[3] = _node.point + (rotation * (Vector3.right * m_roadWidth));

            //middle points
            points[1] = farPoint + (rotation * (-Vector3.right * m_roadWidth));
            points[2] = farPoint + (rotation * (Vector3.right * m_roadWidth));

            Line[] lines = new Line[3];

            lines[0] = new Line(points[0], points[1]);
            lines[1] = new Line(points[1], points[2]);
            lines[2] = new Line(points[2], points[3]);

            if (lines[0].CircleIntersections(_node.point, m_nodeRadius, out Vector3[] iOne))
            {
                Vector3 point = lines[0].a;
                if (iOne.Length == 2)
                {
                    if (Vector3.Distance(iOne[0], lines[0].b) < Vector3.Distance(iOne[1], lines[0].b))
                    {
                        point = iOne[0];
                    }
                    else
                    {
                        point = iOne[1];
                    }
                }
                else point = iOne[0];

                //Debug.DrawLine(lines[0].a, point, Color.red, 60);
                lines[0].a = point;
            }
            else
            {
                Debug.Log("How come line 0 doesn't intersect the node?");
            }

            if (lines[2].CircleIntersections(_node.point, m_nodeRadius, out Vector3[] iTwo))
            {
                Vector3 point = lines[2].a;
                if (iTwo.Length == 2)
                {
                    if (Vector3.Distance(iTwo[0], lines[1].a) < Vector3.Distance(iTwo[1], lines[1].a))
                    {
                        point = iTwo[0];
                    }
                    else
                    {
                        point = iTwo[1];
                    }
                }
                else point = iTwo[0];

                //Debug.DrawLine(point, lines[2].b, Color.magenta, 60);
                lines[2].b = point;
            }
            else
            {
                Debug.Log("How come line 2 doesn't intersect the node?");
            }

            //this connects the last point from the previous line to the start point of this section - we add it before we add the next lines
            if (nodeLines.Count > 0)
            {
                Line newLine = new Line(lines[2].b, nodeLines[nodeLines.Count - 3].a);
                //This is one of 2 places where we can check if we are having issues with overlapping the corners

                if (newLine.DoesIntersect(_node.point, conn.point, out Vector3 iPoint))
                {
                    Vector3 direction = (_node.point - Vector3.Lerp(newLine.a, newLine.b, 0.5f)).normalized;
                    Line otherLine = new Line(newLine.a, _node.point + (direction * (m_roadWidth * 0.25f)));
                    newLine.a = otherLine.b;
                    nodeLines.Add(otherLine);
                    //Debug.Log("???");
                }

                //this draws a line from the start point to the last line
                nodeLines.Add(newLine);
            }

            nodeLines.AddRange(lines);
        }

        return nodeLines;
    }

    private Polygon ExtrudeNodePolygon(Polygon _poly, Node _node) 
    {
        Vector3[] extrudedVertices = new Vector3[_poly.vertices.Length];

        for (int i = 0; i < extrudedVertices.Length; i++)
        {
            extrudedVertices[i] = _poly.vertices[i].point + (Vector3.up * m_extrusionDepth);
        }

        Polygon extrudedPoly = new Polygon(_node.point, extrudedVertices);

        Line[] connectionLines = new Line[_node.connections.Count];
        for (int i = 0; i < _node.connections.Count; i++)
        {
            connectionLines[i] = new Line(_node.point, _node.connections[i].point);
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
            _poly.AddConnectedPolygon(new Polygon(_node.point, vertexArray, true));
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
        if (polygons != null && drawPolygons || drawPoints)
        {
            for (int i = 0; i < polygons.Count; i++)
            {
                for (int j = 0; j < polygons[i].vertices.Length; j++)
                {
                    if (j > 0 && drawPolygons)
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawLine(polygons[i].vertices[j].point, polygons[i].vertices[j - 1].point);
                    }

                    if (drawPoints) 
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawSphere(polygons[i].vertices[j].point, 0.5f);
                    }
                    //Handles.Label(polygons[i].vertices[j] + (Vector3.up * j), j.ToString());
                }
            }
        }
    }

    public class ConnectionSort : IComparer<Node>
    {
        public Node start, current;

        //returns which line starts most to the left

        public int Compare(Node x, Node y)
        {
            Vector3 incomingDir = Vector3.Normalize(current.point - start.point);

            float xRot = Vector3.SignedAngle(incomingDir, Vector3.Normalize(current.point - x.point), Vector3.up);
            float yRot = Vector3.SignedAngle(incomingDir, Vector3.Normalize(current.point - y.point), Vector3.up);

            if (xRot == yRot) return 0;

            return  xRot < yRot ? 1 : -1;
        }
    }
}
