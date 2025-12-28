using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Earclipping;
using Utility;
using System;

public class MeshBuilder : MonoBehaviour
{
    public bool spawnMesh;
    public GameObject prefabObj;
    public Transform roadHolder;
    public Material[] materialPallette;
    public Vector2[] textureScales;
    [Space]
    EarClipper clipper;
    NodeMeshConstructor nmc;
    public Mesh[] meshes;
    public GameObject[] roads;

    // Start is called before the first frame update
    void Start()
    {
        meshes = null;
        roads = null;
        clipper = GetComponent<EarClipper>();
        nmc = clipper.nmc;
    }

    // Update is called once per frame
    void Update()
    {
        if (nmc.meshCreated && meshes == null)
        {
            CreateMeshes(nmc.polygons);
            spawnMesh = true;
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

            List<Material> mats = new List<Material>() { materialPallette[0] };
            for (int j = 1; j < meshes[i].subMeshCount; j++)
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

        for (int k = 0; k < idxList.Length; k += 3)
        {
            int v1 = idxList[k];
            int v2 = idxList[Lists.ClampListIndex(k + 1, idxList.Length)];
            int v3 = idxList[Lists.ClampListIndex(k + 2, idxList.Length)];

            Vector3 n = Geometry.GetNormalOfPoints(verts[v1], verts[v2], verts[v3]);
            normals[v1] += n;
            normals[v2] += n;
            normals[v3] += n;
        }

        for (int k = 0; k < normals.Length; k++)
        {
            normals[k].Normalize();
        }

        return normals;
    }

    private void CreateMeshes(List<Polygon> polygons)
    {
        meshes = new Mesh[polygons.Count];
        int i = 0;
        foreach (Polygon poly in polygons)
        {
            meshes[i] = new Mesh();

            Triangle[] polyTris = clipper.GetTriangles(poly);

            List<Vector3> verts = new List<Vector3>();
            List<int> idxList = new List<int>();

            foreach (Triangle tri in polyTris)
            {
                for (int v = 0; v < tri.vertices.Length; v++)
                {
                    verts.Add(tri.vertices[v]);
                    idxList.Add(verts.Count-1);
                }
            }

            //need to go through each vert and find the correct normal (possibly need to check the tri formed by the normals)
            List<Vector3> normalList = new List<Vector3>(CalculateNormals(verts.ToArray(), idxList.ToArray()));
            List<Vector2> uvList = new List<Vector2>(CalculateUVs(idxList, verts, textureScales[0]));

            List<int> idxRev = new List<int>(idxList);
            idxRev.Reverse();
            idxList.AddRange(idxRev);

            meshes[i].vertices = verts.ToArray();
            meshes[i].SetUVs(0, uvList);
            meshes[i].triangles = idxList.ToArray();
            meshes[i].normals = normalList.ToArray();
            meshes[i].tangents = new Vector4[verts.Count];

            //if the polygon is going to be a 3d mesh we need to build the other linked polygon
            if (poly.isThreeD && poly.linkedPolygons != null)
            {
                List<Mesh> meshList = new List<Mesh>() { meshes[i] };
                
                foreach (Polygon linkedPoly in poly.linkedPolygons)
                {
                    verts.Clear();
                    idxList.Clear();
                    normalList.Clear();
                    if (linkedPoly.isVert)
                    {
                        verts.Clear();
                        //foreach (Vertex vertex in linkedPoly.vertices)
                        //{
                        //    verts.Add(vertex.point);
                        //}

                        //the list of vertices halved then -1 for 0 start
                        for (int top = 0; top < (linkedPoly.vertices.Length /2); top++)
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
                    else
                    {
                        polyTris = clipper.GetTriangles(linkedPoly);
                        
                        foreach (Triangle tri in polyTris)
                        {
                            foreach (Vector3 v in tri.vertices)
                            {
                                verts.Add(v);
                                idxList.Add(verts.Count - 1);
                            }
                        }
                    }

                    normalList = new List<Vector3>(CalculateNormals(verts.ToArray(), idxList.ToArray()));
                    uvList = new List<Vector2>(CalculateUVs(idxList, verts, textureScales[1]));

                    idxRev = new List<int>(idxList);
                    idxRev.Reverse();
                    idxList.AddRange(idxRev);

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

                meshes[i] = new Mesh();
                meshes[i].CombineMeshes(combine, false);
            }

            i++;
        }
    }

    private Vector2[] CalculateUVs(List<int> idxList, List<Vector3> points, Vector2 textureScale)
    {
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i < idxList.Count; i += 3)
        {
            Vector3[] vertices = new Vector3[3];
            vertices[0] = points[idxList[i]];
            vertices[1] = points[idxList[i]+1];
            vertices[2] = points[idxList[i]+2];

            Vector3 startingForward = Geometry.GetNormalOfPoints(vertices[0], vertices[1], vertices[2]);

            //Rotate each of the points so that the normal is (0,0,-1) - Facing the screen
            //That gives us the local x,y and we can then calculate the uv as before
            Quaternion quaternion = Quaternion.identity;
            //This is giving me the wrong rotation for some walls;
            quaternion.SetFromToRotation(startingForward, Vector3.back);

            Vector2 bottomLeft = Vector2.positiveInfinity;

            Triangle test = new Triangle(vertices[0], vertices[1], vertices[2]);
            test.DebugDraw(Color.red, 120);

            for (int j = 0; j < 3; j++)
            {
                vertices[j] = (Vector2)(quaternion * vertices[j]);
                //Debug.Log(startingForward + " " + points[i + j] + " >> " + vertices[j]);

                if (vertices[j].x < bottomLeft.x) bottomLeft.x = vertices[j].x;
                if (vertices[j].y < bottomLeft.y) bottomLeft.y = vertices[j].y;
            }

            test.vertices = vertices;
            test.DebugDraw(Color.green, 120);


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
