using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class BuildingPopulator : Singleton<BuildingPopulator>
{
    public float timePerStep;
    public float iterationsPerStep;
    [Space]
    public GameObject[] prefabs;
    [Space]
    public GameObject m_buildingHighlighter;
    public LayerMask layerMask;

    [Space]
    public MeshBuilder mb;
    public LNode_Manager lnm;
    public NodePolygonGenerator npg;

    public Dictionary<Node, List<GameObject>> m_buildingsMap = new Dictionary<Node, List<GameObject>>();
    private int lastPrefab;

    internal int stepIterations = 0;

    private void Start()
    {
        lastPrefab = -1;
        //layerMask = 1 << LayerMask.NameToLayer("Road") << LayerMask.NameToLayer("Building");

        Event_Manager.Instance.AddListener(E_Event.RoadMeshes, E_Action.Finished, StartBuildingProcess);
    }


    private void StartBuildingProcess()
    {
        StartCoroutine(PlaceBuildings());
    }

    IEnumerator PlaceBuildings() 
    {
        int i = 0;
        foreach (var node in lnm.AllNodes())
        {
            List<GameObject> buildings = new List<GameObject>();
            //foreach (var conn in node.m_connections)
            var polygon = npg.m_nodePolygons[node];
            foreach (var line in polygon.m_lines)
            {
                //var length = (conn.m_point - node.m_point).magnitude;
                var length = line.Length();
                var direction = line.Direction();

                var point = line.a;
                Vector3 dirFromRoad = Quaternion.LookRotation(direction) * Vector3.left;
                //Kenney assets are currently flipped the wrong way around. This is flipped for now to compensate
                //Quaternion rotToRoad = Quaternion.LookRotation(Quaternion.LookRotation(direction) * Vector3.right);
                Quaternion rotToRoad = Quaternion.LookRotation(Quaternion.LookRotation(direction) * Vector3.left);

                while (length > 0)
                {
                    GameObject prefab = RandomPrefab();
                    BuildingData data = prefab.GetComponent<BuildingData>();
                    Vector3 step = direction * (data.m_size.x / 2);

                    // Reduce the length
                    length -= data.m_size.x;

                    // Step along the road, then move away from the road
                    var pos = (point + step) + (dirFromRoad * (data.m_size.z * 0.5f));

                    // Check for collisions (smaller hit box to allow for slight overlap of blank spaces)
                    if (!Physics.CheckBox(pos, data.m_size * 0.4f, rotToRoad, layerMask))
                    {
                        var building = Instantiate(prefab, pos, rotToRoad, transform);

                        var particles = Instantiate(m_buildingHighlighter, building.transform);
                        var ps = particles.GetComponent<ParticleSystem>().shape;
                        ps.meshRenderer = building.GetComponent<MeshRenderer>();

                        var bd = building.GetComponent<BuildingData>();
                        bd.m_id = ++i;
                        bd.m_highlighter = particles;

                        buildings.Add(building);
                    }
                    //else 
                    //{
                    //    var size = rotToRoad * data.size;
                    //    var un = new Vector3(size.x/2, 0, size.z/2);
                    //    var deux = new Vector3(-size.x/2, 0, size.z/2);

                    //    Debug.DrawLine(pos + un, pos - un, Color.red, 9999);
                    //    Debug.DrawLine(pos + deux, pos - deux, Color.red, 9999);
                    //    Debug.DrawLine(pos + un, pos - deux, Color.blue, 9999);
                    //    Debug.DrawLine(pos + deux, pos - un, Color.blue, 9999);
                    //}

                    // Update the point to the edge of the building
                    point += step * 2;

                    if (++stepIterations >= iterationsPerStep)
                    {
                        yield return new WaitForSeconds(timePerStep);
                        stepIterations = 0;
                    }
                }
            }

            m_buildingsMap[node] = buildings;
        }

        Event_Manager.Instance.InvokeEvent(E_Event.Buildings, E_Action.Finished);
    }

    private GameObject RandomPrefab()
    {
        int rndNum = lastPrefab;

        while (rndNum == lastPrefab)
        {
            rndNum = Random.Range(0, prefabs.Length);
        }

        lastPrefab = rndNum;
        return prefabs[rndNum];
    }

    public BuildingData GetRandomBuildingForNode(Node node)
    {
        var buildings = m_buildingsMap[node];
        return buildings[Utility.Lists.RandomIndex(buildings.Count)].GetComponent<BuildingData>();
    }
}
