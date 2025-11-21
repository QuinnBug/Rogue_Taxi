using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Earclipping;
using UnityEditor;
using Utility;

public class NodeMeshConstructor : MonoBehaviour
{
    public LNode_Manager nodeManager;

    public float roadWidth;
    public float nodeRadius;
    public bool doubleSided;
    [Space]
    public bool extrude;
    public float extrusionDepth;
    [Space]
    public bool drawPoints;
    public bool drawPolygons;
    [Space]
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
        if (nodeManager.nodeGenDone && polygons == null && !meshCreated)
        {
            StartCoroutine(CreatePolygonFromNodes());
        }
    }

    IEnumerator CreatePolygonFromNodes() 
    {
        polygons = new List<Polygon>();

        foreach (Node node in nodeManager.AllNodes())
        {
            if (node.connections.Count == 0) continue;

            polygons.Add(PolyFromNode(node));

            if(timePerNode > 0) yield return new WaitForSeconds(timePerNode);
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

        //creates the line to each of the connections
        List<Line> nodeLines = new List<Line>();
        foreach (Node conn in node.connections)
        {
            //find midpoint from node to conn
            Vector3 farPoint = Vector3.Lerp(node.point, conn.point, 0.5f);
            Quaternion rotation = Quaternion.LookRotation(conn.point - node.point, Vector3.up);
            Vector3[] points = new Vector3[4];

            //close points
            points[0] = node.point + (rotation * (-Vector3.right * roadWidth));
            points[3] = node.point + (rotation * (Vector3.right * roadWidth));

            //middle points
            points[1] = farPoint + (rotation * (-Vector3.right * roadWidth));
            points[2] = farPoint + (rotation * (Vector3.right * roadWidth));

            Line[] lines = new Line[3];

            lines[0] = new Line(points[0], points[1]);
            lines[1] = new Line(points[1], points[2]);
            lines[2] = new Line(points[2], points[3]);

            if (lines[0].CircleIntersections(node.point, nodeRadius, out Vector3[] iOne))
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

            if (lines[2].CircleIntersections(node.point, nodeRadius, out Vector3[] iTwo))
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

                if (newLine.DoesIntersect(node.point, conn.point, out Vector3 iPoint))
                {
                    Vector3 direction = (node.point - Vector3.Lerp(newLine.a, newLine.b, 0.5f)).normalized;
                    Line otherLine = new Line(newLine.a, node.point + (direction * (roadWidth * 0.25f)));
                    newLine.a = otherLine.b;
                    nodeLines.Add(otherLine);
                    //Debug.Log("???");
                }

                //this draws a line from the start point to the last line
                nodeLines.Add(newLine);
            }

            nodeLines.AddRange(lines);
        }

        if (node.connections.Count == 1)
        {
            //this is a dead end node so we need to draw around the node a lil extra (and replace this line which just cuts through a node)
            Vector3[] points = new Vector3[4];
            Vector3 farPoint = node.point + ((node.point - node.connections[0].point).normalized * roadWidth);
            Quaternion rotation = Quaternion.LookRotation(node.connections[0].point - node.point, Vector3.up);

            //close points
            points[0] = nodeLines[0].a;
            points[3] = nodeLines[nodeLines.Count - 1].b;

            //middle points
            points[1] = farPoint + (rotation * (-Vector3.right * roadWidth));
            points[2] = farPoint + (rotation * (Vector3.right * roadWidth));

            nodeLines.Add(new Line(points[0], points[1]));
            nodeLines.Add(new Line(points[1], points[2]));
            nodeLines.Add(new Line(points[2], points[3]));
        }
        else
        {
            Line newLine = new Line(nodeLines[2].b, nodeLines[nodeLines.Count - 3].a);
            //This is one of 2 places where we can check if we are having issues with overlapping the corners

            foreach (Node conn in node.connections)
            {
                if (newLine.DoesIntersect(node.point, conn.point, out Vector3 iPoint))
                {
                    Vector3 direction = (node.point - Vector3.Lerp(newLine.a, newLine.b, 0.5f)).normalized;
                    Line otherLine = new Line(node.point + (direction * (roadWidth * 0.25f)), newLine.b);
                    newLine.b = otherLine.a;
                    nodeLines.Add(otherLine);
                    //Debug.Log("???");
                    break;
                }
            }

            //this draws a line from the start point to the last line
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
                    //Debug.Log("j - i = " + j + " - " + i + " : " + lineCount);

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

        if (extrude)
        {
            Vector3[] eVertices = new Vector3[poly.vertices.Length];

            for (int i = 0; i < eVertices.Length; i++)
            {
                eVertices[i] = poly.vertices[i].point + (Vector3.up * extrusionDepth);
            }

            Polygon ePoly = new Polygon(node.point, eVertices);

            Line[] connLines = new Line[node.connections.Count];
            for (int i = 0; i < node.connections.Count; i++)
            {
                connLines[i] = new Line(node.point, node.connections[i].point);
            }

            //each of these arrays are individual polygons
            List<Vector3[]> wallVertices = new List<Vector3[]>();

            int start = 0;
            int end = poly.vertices.Length;
            int next = 1;
            int current = 0;
            Line testLine = new Line(Vector3.zero, Vector3.forward);

            List<Vector3> verts = new List<Vector3>();
            while (current < poly.vertices.Length)
            {
                verts.Add(poly.vertices[current].point);

                next = Lists.ClampListIndex(current + 1, poly.vertices.Length);
                testLine.a = poly.vertices[current].point;
                testLine.b = poly.vertices[next].point;
                //testLine.DebugDraw(Color.green, 120);

                bool intersects = false;
                foreach (Line cl in connLines)
                {
                    //cl.DebugDraw(Color.blue, 120);
                    if (cl.DoesIntersect(testLine, out Vector3 intersect))
                    {
                        intersects = true;
                        break;
                    }
                }

                if (intersects)
                {
                    end = next;

                    //we need to loop back around to the start idx in the eVerts
                    while (current >= start)
                    {
                        verts.Add(eVertices[current]);
                        current--;
                    }

                    //then we add a connection to poly.vertices.start
                    verts.Add(poly.vertices[start].point);

                    //then we add verts to wallVertices and clear verts
                    wallVertices.Add(verts.ToArray());
                    verts.Clear();

                    //then we jump to the start of the next poly
                    current = start = end;
                    end = poly.vertices.Length;
                }
                else
                {
                    current++;
                }
            }

            //to exit the prev loop current needs to be out of index range, so we bring it back in here
            current--;
            //we need to add the last set of verts that didn't get added by an interception.
            if (verts.Count > 0)
            {
                //we need to loop back around to the start idx in the eVerts
                while (current >= start)
                {
                    verts.Add(eVertices[current]);
                    current--;
                }

                //then we add a connection to poly.vertices.start
                verts.Add(poly.vertices[start].point);

                //then we add verts to wallVertices and clear verts
                wallVertices.Add(verts.ToArray());
            }

            poly.AddConnectedPolygon(ePoly);
            foreach (Vector3[] vees in wallVertices)
            {
                Polygon temp = new Polygon(node.point, vees, true);
                //temp.DebugDraw(Color.red, 120);
                poly.AddConnectedPolygon(temp);
            }
            poly.isThreeD = true;
        }


        return poly;
    }

    private void OnValidate()
    {
        if(nodeManager != null) ValueClamps(nodeManager.clampValues);
    }

    public void ValueClamps(bool forceUpdate = false)
    {
        if (nodeRadius >= (nodeManager.m_nodeLimitRange.min / 2)*0.75f || forceUpdate)
        {
            //nodeRadius = (nodeManager.nodeLimitRange.min / 2) * 0.75f;
        }

        if (roadWidth > nodeRadius * 0.75f || forceUpdate)
        {
            //roadWidth = nodeRadius * 0.75f;
        }
    }

    private void OnDrawGizmos()
    {
        if (nodeManager != null && nodeManager.m_nodeMap != null && drawPoints)
        {
            foreach (Node node in nodeManager.AllNodes())
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawSphere(node.point, nodeRadius);
            }
        }

        if (polygons != null && drawPolygons)
        {
            for (int i = 0; i < polygons.Count; i++)
            {
                for (int j = 0; j < polygons[i].vertices.Length; j++)
                {
                    if (j > 0)
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawLine(polygons[i].vertices[j].point, polygons[i].vertices[j - 1].point);
                    }

                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(polygons[i].vertices[j].point, 1.5f);
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
