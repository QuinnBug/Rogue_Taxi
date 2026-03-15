using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct TileSettings 
{
    public Vector2Int size;
    public float perlinZoom;
    public float perlinHeight;
    public float perlinHeightLimit;
    public Vector2 vSize;
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TerrainTile : MonoBehaviour
{
    public Vector2Int tilePosition;
    [Space]
    public bool m_ShowGrid = true;
    public TileSettings settings;

    private Vector3[] vertices;
    private Mesh mesh;
    internal Vector2 perlinOffset;

    public void GenerateMesh() 
    {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Procedural Grid";

        #region vertices generation
        vertices = new Vector3[(settings.size.x + 1) * (settings.size.y + 1)];
        Vector4[] tangents = new Vector4[vertices.Length];
        Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);
        Vector2[] uv = new Vector2[vertices.Length];
        for (int i = 0, z = 0; z <= settings.size.y; z++)
        {
            for (int x = 0; x <= settings.size.x; x++, i++)
            {
                float y = (Mathf.PerlinNoise(
                    (perlinOffset.x + x + (tilePosition.x * settings.size.x)) * settings.perlinZoom,
                    (perlinOffset.y + z + (tilePosition.y * settings.size.y)) * settings.perlinZoom) - 0.5f)
                    * settings.perlinHeight;

                if (y > settings.perlinHeightLimit) y = settings.perlinHeightLimit;
                else if (y < -settings.perlinHeightLimit) y = -settings.perlinHeightLimit;

                vertices[i] = new Vector3(x * settings.vSize.x, y, z * settings.vSize.y);
                uv[i] = new Vector2((float)x / settings.size.x, (float)z / settings.size.y);
                tangents[i] = tangent;
            }
        }
        #endregion

        mesh.vertices = vertices;

        #region tri generation
        int[] triangles = new int[settings.size.x * settings.size.y * 6];
        for (int ti = 0, vi = 0, y = 0; y < settings.size.y; y++, vi++)
        {
            for (int x = 0; x < settings.size.x; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + settings.size.x + 1;
                triangles[ti + 5] = vi + settings.size.x + 2;
            }
        }
        #endregion

        mesh.triangles = triangles;

        mesh.uv = uv;
        mesh.tangents = tangents;

        mesh.RecalculateNormals();

        mesh.RecalculateBounds();
        MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }

    //private void OnDrawGizmosSelected()
    //{
    //    if (m_ShowGrid)
    //    {
    //        m_ShowGrid = false;
    //        GenerateMesh();
    //    }

    //    //if (vertices != null)
    //    //{
    //    //    Gizmos.color = Color.black;
    //    //    for (int i = 0; i < vertices.Length; i++)
    //    //    {
    //    //        Gizmos.DrawSphere(transform.TransformPoint(vertices[i]), 0.1f);
    //    //    }
    //    //}
    //}
}
