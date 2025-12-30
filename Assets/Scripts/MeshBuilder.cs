using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Earclipping;
using Utility;
using System;

public class MeshBuilder : MonoBehaviour
{
    public float d_SecondsPerLoop;
    public int d_PolygonsPerLoop;
    [Space]
    public GameObject prefabObj;
    public Transform roadHolder;
    public Material[] materialPallette;
    public Vector2[] textureScales;
    [Space]
    public Mesh[] meshes;
    public GameObject[] roads;

    private bool spawnMesh;
    private EarClipper clipper;
    private NodeMeshConstructor nmc;

    // Start is called before the first frame update
    void Start()
    {
        meshes = null;
        roads = null;
        clipper = GetComponent<EarClipper>();
        nmc = FindAnyObjectByType<NodeMeshConstructor>();
    }

    // Update is called once per frame
    void Update()
    {
        if (nmc.meshCreated && meshes == null)
        {
            StartCoroutine(CreateMeshes(nmc.polygons));
        }

        if (meshes != null && spawnMesh == true)
        {
            spawnMesh = false;
            CreateRoads();
            //BuildingPopulator.Instance.spawnBuildings = true;
        }
    }

    private void CreateRoads()
    {
        roads = new GameObject[meshes.Length];
        for (int i = 0; i < meshes.Length; i++)
        {
            roads[i] = Instantiate(prefabObj, roadHolder);
            //roads[i].transform.position = clipper.nmc.polygons[i].center;

            roads[i].GetComponent<MeshFilter>().mesh = meshes[i];

            List<Material> mats = new List<Material>() { materialPallette[0], materialPallette[0]};
            for (int j = 2; j < meshes[i].subMeshCount; j++)
            {
                mats.Add(materialPallette[1]);
            }
            roads[i].GetComponent<MeshRenderer>().sharedMaterials = mats.ToArray();

            roads[i].GetComponent<MeshCollider>().sharedMesh = meshes[i];
        }
    }

    private Vector3[] CalculateNormals(Vector3[] verts, int[] idxList) 
    {
        Vector3[] normals = new Vector3[verts.Length];
        int triCount = idxList.Length / 3;
        for (int i = 0; i < triCount; ++i)
        {
            int triIdx = i * 3;
            int vertIdxA = idxList[triIdx];
            int vertIdxB = idxList[triIdx+1];
            int vertIdxC = idxList[triIdx+2];

            if (vertIdxA >= verts.Length) 
            {
                Debug.Log("[MB] A:" + vertIdxA);
            }
            if (vertIdxB >= verts.Length)
            {
                Debug.Log("[MB] B:" + vertIdxB);
            }
            if (vertIdxC >= verts.Length)
            {
                Debug.Log("[MB] C:" + vertIdxC);
            }
            Vector3 n = Geometry.GetNormalOfPoints(verts[vertIdxA], verts[vertIdxB], verts[vertIdxC]);
            
            normals[vertIdxA] += n;
            normals[vertIdxB] += n;
            normals[vertIdxC] += n;
        }

        for (int j = 0; j < normals.Length; j++)
        {
            normals[j].Normalize();
        }

        return normals;
    }

    private IEnumerator CreateMeshes(List<Polygon> polygons)
    {
        Debug.Log("[MB] Creating Meshes");
        meshes = new Mesh[polygons.Count];
        int i = 0;
        foreach (Polygon poly in polygons)
        {
            //QWN:: Test this
            meshes[i] = BuildMeshFromPoly(poly);
            ++i;

            if (i % d_PolygonsPerLoop == 0) 
            {
                yield return new WaitForSeconds(d_SecondsPerLoop);
            }
        }

        Debug.Log("[MB] Meshes Created");
        spawnMesh = true;
    }

    private Mesh BuildMeshFromPoly(Polygon poly) 
    {
        Mesh mesh = new Mesh();
        Triangle[] polyTris = clipper.GetTriangles(poly);

        List<Vector3> verts = new List<Vector3>();
        List<int> idxList = new List<int>();

        foreach (Triangle tri in polyTris)
        {
            foreach (Vector3 vert in tri.vertices)
            {
                if (!verts.Contains(vert)) { verts.Add(vert); }
                idxList.Add(verts.IndexOf(vert));
            }
        }

        Debug.Log("[MB] verts:idxList:triCount || " + verts.Count + " : " + idxList.Count + " : " + polyTris.Length);

        List<Vector3> normalList = new List<Vector3>(CalculateNormals(verts.ToArray(), idxList.ToArray()));
        List<Vector2> uvList = new List<Vector2>(CalculateUVs(idxList, verts, textureScales[0]));


        mesh.vertices = verts.ToArray();
        mesh.SetUVs(0, uvList);
        mesh.triangles = idxList.ToArray();
        mesh.normals = normalList.ToArray();
        mesh.tangents = new Vector4[verts.Count];

        //if the polygon is going to be a 3d mesh we need to build the other linked polygon
        if (poly.isThreeD && poly.linkedPolygons != null)
        {
            List<Mesh> meshList = new List<Mesh>() { mesh };

            foreach (Polygon linkedPoly in poly.linkedPolygons)
            {
                verts.Clear();
                idxList.Clear();
                normalList.Clear();

                //Walls
                if (linkedPoly.isVert)
                {
                    //the list of vertices halved then -1 for 0 start
                    for (int top = 0; top < (linkedPoly.vertices.Length / 2); top++)
                    {
                        int bottom = (linkedPoly.vertices.Length - 1) - top;

                        verts.Add(linkedPoly.vertices[top].point);
                        idxList.Add(verts.Count - 1);
                        verts.Add(linkedPoly.vertices[bottom - 1].point);
                        idxList.Add(verts.Count - 1);
                        verts.Add(linkedPoly.vertices[top + 1].point);
                        idxList.Add(verts.Count - 1);

                        //--

                        verts.Add(linkedPoly.vertices[top].point);
                        idxList.Add(verts.Count - 1);
                        verts.Add(linkedPoly.vertices[bottom].point);
                        idxList.Add(verts.Count - 1);
                        verts.Add(linkedPoly.vertices[bottom - 1].point);
                        idxList.Add(verts.Count - 1);
                    }
                }
                else //Ceiling
                {
                    polyTris = clipper.GetTriangles(linkedPoly);

                    foreach (Triangle tri in polyTris)
                    {
                        foreach (Vector3 vert in tri.vertices)
                        {
                            if (!verts.Contains(vert)) { verts.Add(vert); }
                            idxList.Add(verts.IndexOf(vert));
                        }
                    }
                    idxList.Reverse();
                }
                normalList = new List<Vector3>(CalculateNormals(verts.ToArray(), idxList.ToArray()));
                uvList = new List<Vector2>(CalculateUVs(idxList, verts, textureScales[1]));

                Mesh subMesh = new Mesh();
                subMesh.vertices = verts.ToArray();
                subMesh.SetUVs(0, uvList);
                subMesh.triangles = idxList.ToArray();
                subMesh.normals = normalList.ToArray();
                subMesh.tangents = new Vector4[verts.Count];
                meshList.Add(subMesh);
            }

            CombineInstance[] combine = new CombineInstance[meshList.Count];
            for (int m = 0; m < meshList.Count; m++)
            {
                combine[m] = new CombineInstance();
                combine[m].mesh = meshList[m];
                combine[m].transform = transform.localToWorldMatrix;
            }

            mesh = new Mesh();
            mesh.CombineMeshes(combine, false);
        }

        return mesh;
    }

    private Vector2[] CalculateUVs(List<int> idxList, List<Vector3> points, Vector2 textureScale)
    {
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i < points.Count; i += 3)
        {
            Vector3[] vertices = new Vector3[3];
            vertices[0] = points[idxList[i]];
            vertices[1] = points[Lists.ClampListIndex(idxList[i]+1, points.Count)];
            vertices[2] = points[Lists.ClampListIndex(idxList[i]+2, points.Count)];

            Vector3 startingForward = Geometry.GetNormalOfPoints(vertices[0], vertices[1], vertices[2]);

            //Rotate each of the points so that the normal is (0,0,-1) - Facing the screen
            //That gives us the local x,y and we can then calculate the uv as before
            Quaternion quaternion = Quaternion.identity;
            //This is giving me the wrong rotation for some walls;
            quaternion.SetFromToRotation(startingForward, Vector3.back);

            Vector2 bottomLeft = Vector2.positiveInfinity;

            Triangle test = new Triangle(vertices[0], vertices[1], vertices[2]);
            //test.DebugDraw(Color.red, 120);

            for (int j = 0; j < 3; j++)
            {
                vertices[j] = (Vector2)(quaternion * vertices[j]);
                //Debug.Log(startingForward + " " + points[i + j] + " >> " + vertices[j]);

                if (vertices[j].x < bottomLeft.x) bottomLeft.x = vertices[j].x;
                if (vertices[j].y < bottomLeft.y) bottomLeft.y = vertices[j].y;
            }

            test.vertices = vertices;
            //test.DebugDraw(Color.green, 120);


            foreach (Vector2 v in vertices)
            {
                //Vector2 uv = (v - bottomLeft) / textureScale;
                Vector2 uv = v / textureScale;
                uvs.Add(uv);
            }
        }

        return uvs.ToArray();
    }

    //private Vector2[] CalculateUVs(Polygon poly, List<Vector3> points)
    //{
    //    List<Vector2> uvs = new List<Vector2>();
    //    List<Vertex> vertices = new List<Vertex>(poly.vertices);

    //    foreach (Vector3 point in points)
    //    {
    //        uvs.Add(vertices.Find(x => x.point == point).uv);
    //    }

    //    return uvs.ToArray();
    //}
}
