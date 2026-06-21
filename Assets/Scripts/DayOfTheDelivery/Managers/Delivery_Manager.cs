using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Delivery_Manager : Singleton<Delivery_Manager>
{
    public Range<float> m_nodeRange;

    public Range<float> m_timeRange;

    public int m_currentDeliveryId = -1;

    private GameObject m_player;

    public GameObject pointer;

    private float m_newDeliveryTimer = 0;
    [SerializeField] private bool m_active = false;
    private Dictionary<int, Delivery> m_activeDeliveries = new Dictionary<int, Delivery>();

    private void Start()
    {
        Event_Manager.Instance.AddListener(E_Event.Buildings, E_Action.Finished, StartDeliveryClock);
        m_player = FindAnyObjectByType<TruckController>().gameObject;
    }

    void StartDeliveryClock()
    {
        m_active = true;
    }

    void UpdateDeliveryClock()
    {
        if (!m_active) { return; }

        m_newDeliveryTimer -= Time.deltaTime;
        if (m_newDeliveryTimer < 0)
        {
            RandomNewDelivery();
            m_newDeliveryTimer = m_timeRange.RandomValue();
        }
    }

    // Update is called once per frame
    public void RandomNewDelivery()
    {
        var nodes = LNode_Manager.Instance.GetNodesInRange(m_player.transform.position, 2, false);
        var currentBuilding = BuildingPopulator.Instance.GetRandomBuildingForNode(nodes[Utility.Lists.RandomIndex(nodes.Count)]);
        if (!m_activeDeliveries.ContainsKey(currentBuilding.m_id))
        {
            m_activeDeliveries[currentBuilding.m_id] = new Delivery(currentBuilding);
        }

        if (m_currentDeliveryId == -1)
        {
            m_currentDeliveryId = currentBuilding.m_id;
        }
    }

    public void MarkDeliveryComplete(int id)
    {
        if (m_activeDeliveries.TryGetValue(id, out Delivery value))
        {
            value.building.SetHighlight(false);
            m_activeDeliveries.Remove(id);
            m_currentDeliveryId = m_activeDeliveries.Count > 0 ? m_activeDeliveries.Keys.First() : -1;
        }
    }

    private void Update()
    {
        if (m_currentDeliveryId != -1) {
            if (m_activeDeliveries.TryGetValue(m_currentDeliveryId, out Delivery value))
            { 
                var deliveryTarget = value.building.transform.position;
                var dir = deliveryTarget - pointer.transform.position;
                dir.y = 0;
                pointer.transform.rotation = Quaternion.LookRotation(dir, m_player.transform.up);
            }
        }

        UpdateDeliveryClock();
    }
}

public struct Delivery
{
    public Delivery(BuildingData _building)
    {
        building = _building;
        
        building.SetHighlight(true);
        timeStamp = Time.time;
    }

    public BuildingData building;
    public float timeStamp;
}
