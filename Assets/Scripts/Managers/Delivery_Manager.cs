using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Delivery_Manager : Singleton<Delivery_Manager>
{
    bool active = false;
    List<Building> targets = new List<Building>();

    public int deliveryCounts;
    public int deliveriesCompleted;
    public Material targetMaterial;

    void Start()
    {
        Event_Manager.Instance._OnBuildingsGenerated.AddListener(AssignDeliveries);
        Event_Manager.Instance._DeliveryMade.AddListener(ConfirmDelivery);
    }

    void Update()
    {
        if (deliveriesCompleted >= deliveryCounts)
        {
            ChangeActiveState(false);
        }
    }

    void ConfirmDelivery(Building building) 
    {
        if (targets.Contains(building))
        {
            deliveriesCompleted++;
            targets.Remove(building);
        }
    }

    void ChangeActiveState(bool state) 
    {
        if (state && !active)
        {
            AssignDeliveries();
        }
        else if (!state && active)
        {
            targets = null;
        }
        active = state;
    }

    void AssignDeliveries() 
    {
        for (int i = 0; i < deliveryCounts; i++)
        {
            bool loop = true;
            Building newTarget = null;
            while (loop)
            {
                loop = false;
                newTarget = Building_Manager.Instance.GetRandomBuilding();

                foreach (Building item in targets)
                {
                    if (item.IsSameRoad(newTarget))
                    {
                        loop = true;
                        break;
                    }
                }
            }
            newTarget.SetAsTarget();
            targets.Add(newTarget);
        }
    }
}
