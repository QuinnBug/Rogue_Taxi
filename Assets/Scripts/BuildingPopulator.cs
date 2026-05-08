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

    [Space]
    public MeshBuilder mb;
    public LNode_Manager lnm;
    public NodePolygonGenerator npg;

    private Dictionary<int, List<GameObject>> buildingsMap;
    private LayerMask layerMask;
    private int lastPrefab;

    internal int stepIterations = 0;

    private void Start()
    {
        lastPrefab = -1;
        layerMask = 1 << LayerMask.NameToLayer("Building");

        Event_Manager.Instance.AddListener(E_Event.RoadMeshes, E_Action.Finished, StartBuildingProcess);
    }


    private void StartBuildingProcess()
    {
        StartCoroutine(PlaceBuildings());
    }

    IEnumerator PlaceBuildings() 
    {
        List<GameObject> buildings = new List<GameObject>();

        // I need to get each node connection
        // Take each side of the line and for each
        // then calculate the length of the line
        // place a building, subtract it's size from the length
        // repeat until no more size.

        foreach (var node in lnm.AllNodes())
        {
            foreach (var conn in node.m_connections)
            {
                var length = (conn.m_point - node.m_point).magnitude;
                var direction = (conn.m_point - node.m_point).normalized;

                var point = node.m_point + (Quaternion.LookRotation(direction) * Vector3.left) * npg.m_roadWidth;
                var facingDir = Quaternion.LookRotation(Quaternion.LookRotation(direction) * Vector3.right);

                while (length > 0)
                {
                    GameObject prefab = RandomPrefab();
                    BuildingData data = prefab.GetComponent<BuildingData>();
                    Vector3 step = direction * (data.size.x / 2);

                    length -= data.size.x;

                    var pos = (point + step);
                    pos -= facingDir * (Vector3.forward * (data.size.z / 2));

                    if (!Physics.CheckBox(pos, data.size/2, facingDir, layerMask))
                    {
                        buildings.Add(Instantiate(prefab, pos, facingDir, transform));
                    }

                    point += step*2;

                    if (++stepIterations >= iterationsPerStep)
                    {
                        yield return new WaitForSeconds(timePerStep);
                        stepIterations = 0;
                    }
                }
            }
        }

        Event_Manager.Instance.InvokeEvent(E_Event.Buildings, E_Action.Finished);

        //for (int x = 0; x < gridSize.x; x++)
        //{
        //    buildings[x] = new GameObject[gridSize.y];
        //    for (int y = 0; y < gridSize.y; y++)
        //    {
        //        //idx - 
        //        position = startPoint;
        //        position.x += x * spaceSize.x + (spaceSize.x / 2);
        //        position.z += y * spaceSize.z + (spaceSize.z / 2);

        //        if (!Physics.CheckBox(position, spaceSize * 0.4f, Quaternion.identity, layerMask))
        //        {
        //            buildings[x][y] = null;
        //        }
        //        stepIterations++;

        //        if (stepIterations >= iterationsPerStep)
        //        {
        //            yield return new WaitForSeconds(timePerStep);
        //            stepIterations = 0;
        //        }
        //    }
        //}
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
