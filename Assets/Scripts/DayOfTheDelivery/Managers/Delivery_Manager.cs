using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Delivery_Manager : Singleton<Delivery_Manager>
{
    public float m_zoneDistFromCurb = 3.5f;
    [Space]
    public Range<float> m_deliveryRange;

    public BuildingData m_currentBuilding;

    private GameObject m_player;

    public GameObject dropZone;
    public GameObject pointer;

    private void Start()
    {
        Event_Manager.Instance.AddListener(E_Event.Buildings, E_Action.Finished, SelectRandomDelivery);
        m_player = FindAnyObjectByType<TruckController>().gameObject;
    }

    // Update is called once per frame
    public void SelectRandomDelivery()
    {
        var nodes = LNode_Manager.Instance.GetNodesInRange(m_player.transform.position, 2, false);
        m_currentBuilding = BuildingPopulator.Instance.GetRandomBuildingForNode(nodes[Utility.Lists.RandomIndex(nodes.Count)]);
        dropZone.transform.position = m_currentBuilding.transform.position +
            m_currentBuilding.transform.forward * -(m_currentBuilding.m_size.z + m_zoneDistFromCurb);
        //The -on the size is because kenney buildings are reversed on the Z axis
    }

    private void Update()
    {
        if (m_currentBuilding != null) {
            var dir = m_currentBuilding.transform.position - pointer.transform.position;
            dir.y = 0;
            pointer.transform.rotation = Quaternion.LookRotation(dir, m_player.transform.up);
        }
    }
}
