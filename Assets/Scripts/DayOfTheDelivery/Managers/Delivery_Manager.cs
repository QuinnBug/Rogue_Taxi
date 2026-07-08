using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Utility;

public class Delivery_Manager : Singleton<Delivery_Manager>
{
    public Range<int> m_nextDeliveryNodeRange;

    public Range<float> m_timeRange;

    public int m_currentDeliveryId = -1;

    private TruckController m_player;

    public GameObject pointer;

    private float m_newDeliveryTimer = 0;
    [SerializeField] private bool m_active = false;
    private Dictionary<int, Delivery> m_availableDeliveries = new Dictionary<int, Delivery>();

    private void Start()
    {
        Event_Manager.Instance.AddListener(E_Event.Buildings, E_Action.Finished, StartDeliveryClock);
        m_player = FindAnyObjectByType<TruckController>();
        pointer = m_player.m_deliveryPointer;
    }

    void StartDeliveryClock()
    {
        m_active = true;
    }

    void UpdateDeliveryClock()
    {
        if (!m_active) { return; }

        m_newDeliveryTimer -= Time.deltaTime;
        if (m_newDeliveryTimer < 0 && m_availableDeliveries.Count < 5)
        {
            RandomNewDelivery();
            m_newDeliveryTimer = m_timeRange.RandomValue();
        }
    }

    // Update is called once per frame
    public void RandomNewDelivery()
    {
        var nodes = LNode_Manager.Instance.GetNodesWithinRange(m_player.transform.position, m_nextDeliveryNodeRange.min, m_nextDeliveryNodeRange.max);
        var currentBuilding = BuildingPopulator.Instance.GetRandomBuildingForNode(nodes[Lists.RandomIndex(nodes.Count)]);
        if (!m_availableDeliveries.ContainsKey(currentBuilding.m_id))
        {
            m_availableDeliveries[currentBuilding.m_id] = new Delivery(currentBuilding);
        }

        if (m_currentDeliveryId == -1)
        {
            m_currentDeliveryId = currentBuilding.m_id;
        }
    }

    public void MarkDeliveryComplete(int id)
    {
        if (m_availableDeliveries.TryGetValue(id, out Delivery value))
        {
            if (m_availableDeliveries.Count == 0)
            {
                RandomNewDelivery();
            }

            value.building.SetHighlight(false);
            m_availableDeliveries.Remove(id);

            SetDeliveryId(m_availableDeliveries.Keys.First());
        }
    }

    private void Update()
    {
        if (m_currentDeliveryId != -1) {
            if (m_availableDeliveries.TryGetValue(m_currentDeliveryId, out Delivery value))
            { 
                var deliveryTarget = value.building.transform.position;
                var dir = deliveryTarget - pointer.transform.position;
                dir.y = 0;
                pointer.transform.rotation = Quaternion.LookRotation(dir, m_player.transform.up);
            }
            else
            {
                m_currentDeliveryId = -1;
            }
        }

        UpdateDeliveryClock();
    }

    internal void ChangeCurrentDelivery(int _input)
    {
        var keyList = m_availableDeliveries.Keys.ToList();
        var currentIdx = keyList.IndexOf(m_currentDeliveryId);
        currentIdx = Lists.ClampListIndex(currentIdx + _input, keyList.Count);
        SetDeliveryId(keyList[currentIdx]);
    }

    void SetDeliveryId(int _newId)
    {
        if (m_availableDeliveries.TryGetValue(m_currentDeliveryId, out Delivery oldDelivery))
        {
            oldDelivery.building.SetHighlight(false);
        }
        
        m_currentDeliveryId = _newId;

        if (m_availableDeliveries.TryGetValue(m_currentDeliveryId, out Delivery newDelivery))
        {
            newDelivery.building.SetHighlight(true);
        }
    }
}

public struct Delivery
{
    public Delivery(BuildingData _building)
    {
        building = _building;
        timeStamp = Time.time;
    }

    public BuildingData building;
    public float timeStamp;
}
