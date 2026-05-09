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
    public LayerMask layerMask;

    [Space]
    public MeshBuilder mb;
    public LNode_Manager lnm;
    public NodePolygonGenerator npg;

    private Dictionary<Node, List<GameObject>> m_buildingsMap = new Dictionary<Node, List<GameObject>>();
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

        // I need to get each node connection
        // Take each side of the line and for each
        // then calculate the length of the line
        // place a building, subtract it's size from the length
        // repeat until no more size.

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
                    Vector3 step = direction * (data.size.x / 2);

                    // Reduce the length
                    length -= data.size.x;

                    // Step along the road, then move away from the road
                    var pos = (point + step) + (dirFromRoad * (data.size.z * 0.5f));

                    // Check for collisions (smaller hit box to allow for slight overlap of blank spaces)
                    if (!Physics.CheckBox(pos, data.size * 0.4f, rotToRoad, layerMask))
                    {
                        buildings.Add(Instantiate(prefab, pos, rotToRoad, transform));
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
}
