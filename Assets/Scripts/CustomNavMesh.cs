using Earclipping;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Splines.Interpolators;

public class CustomNavMesh : Singleton<CustomNavMesh>
{
    public bool d_testGeneration;

    internal Dictionary<uint, NavGridPoint> m_navGrid;

    private void Update()
    {
        if (d_testGeneration)
        {
            //var nmc = GameObject.FindFirstObjectByType<NodeMeshConstructor>();
            //GenerateNavMesh(nmc.m_nodePolygons);
            GetComponent<NavMeshSurface>().BuildNavMesh();
            d_testGeneration = false;
        }
    }

    public void GenerateNavMesh(Dictionary<Node, Polygon> _nodePolygons) 
    {
        m_navGrid = new Dictionary<uint, NavGridPoint>();
        uint gridIndex = 0;
        Dictionary<Node, Triangle[]> trianglesPerNode = new Dictionary<Node, Triangle[]>();
        Dictionary<Triangle, uint> gridIds = new Dictionary<Triangle, uint>();

        foreach (var nodePoly in _nodePolygons)
        {
            Node node = nodePoly.Key;
            Triangle[] triangles = EarClipper.GetTriangles(nodePoly.Value);
            trianglesPerNode.Add(node, triangles);

            foreach (var tri in triangles)
            {
                tri.DebugDraw(new Color(0, 1, 0, 0.5f), 3000);

                m_navGrid.Add(gridIndex, new NavGridPoint(gridIndex, tri.Center()));
                gridIds.Add(tri, gridIndex);
                ++gridIndex;
            }
        }

        foreach (var nodeTriangles in trianglesPerNode)
        {
            Node node = nodeTriangles.Key;
            Triangle[] triangles = nodeTriangles.Value;

            List<Triangle> extTriangles = new List<Triangle>(triangles);
            foreach (Node connection in node.m_connections)
            {
                extTriangles.AddRange(trianglesPerNode[connection]);
            }

            foreach (var tri in triangles)
            {
                foreach (var otherTri in extTriangles)
                {
                    if (tri.AdjacentTo(otherTri))
                    {
                        m_navGrid[gridIds[tri]].m_neighbourIds.Add(gridIds[otherTri]);
                    }
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (m_navGrid != null)
        {
            foreach (var item in m_navGrid)
            {
                Handles.Label(item.Value.m_position, item.Key.ToString());
                foreach (var connection in item.Value.m_neighbourIds)
                {
                    Vector3 lineEnd = Vector3.Lerp(item.Value.m_position, m_navGrid[connection].m_position, 0.5f);
                    Gizmos.color = Color.purple;
                    Gizmos.DrawLine(item.Value.m_position + Vector3.up, lineEnd + Vector3.up);
                }
            }
        }
    }
}

public struct NavGridPoint 
{
    public uint m_id;
    public List<uint> m_neighbourIds;

    public Vector3 m_position;

    public NavGridPoint(uint _id, Vector3 _position) 
    {
        m_id = _id;
        m_position = _position;
        m_neighbourIds = new List<uint>();
    }
}
