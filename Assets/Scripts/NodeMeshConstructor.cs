using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Earclipping;
using UnityEditor;

public class NodeMeshConstructor : MonoBehaviour
{
    public LNode_Manager nodeManager;

    public float roadWidth;
    public float nodeRadius;
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

        ConnectionSort cs = new ConnectionSort();

        foreach (Node node in nodeManager.nodes)
        {
            if (node.connections.Count == 0) continue;

            cs.current = node;
            cs.start = node.connections[0];

            node.connections.Sort(cs);
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

                if (nodeLines.Count > 0)
                {
                    nodeLines.Add(new Line(lines[2].b, nodeLines[nodeLines.Count - 3].a));
                }

                nodeLines.AddRange(lines);
            }

            if (node.connections.Count == 1)
            {
                //this is a dead end node so we need to draw around the node a lil extra (and replace this line which just cuts through a node)
                nodeLines.Add(new Line(nodeLines[nodeLines.Count - 1].b, nodeLines[0].a));
            }
            else
            {
                nodeLines.Add(new Line(nodeLines[2].b, nodeLines[nodeLines.Count - 3].a));
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

            polygons.Add(new Polygon(nodeLines, node.point));

            if(timePerNode > 0) yield return new WaitForSeconds(timePerNode);
        }

        Debug.Log("Mesh Created");
        meshCreated = true;
    }

    private void OnValidate()
    {
        if(nodeManager != null) ValueClamps(nodeManager.clampValues);
    }

    public void ValueClamps(bool forceUpdate = false)
    {
        if (nodeRadius >= (nodeManager.nodeLimitRange.min / 2)*0.75f || forceUpdate)
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
        if (nodeManager != null && nodeManager.nodes != null && drawPoints)
        {
            foreach (Node node in nodeManager.nodes)
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
                        Gizmos.DrawLine(polygons[i].vertices[j], polygons[i].vertices[j - 1]);
                    }
                    //Handles.Label(polygons[i].vertices[j], j.ToString());
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
