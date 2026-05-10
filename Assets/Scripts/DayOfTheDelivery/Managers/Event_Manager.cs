using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;

public enum E_Action
{
    Start,
    Finished
}
public enum E_Event
{
    Game,
    Nodes,
    Terrain,
    RoadPolygons,
    RoadMeshes,
    Buildings,
    NavMesh,
    GamePlay
}

public class Event_Manager : Singleton<Event_Manager>
{
    private Dictionary<E_Event, Dictionary<E_Action, UnityEvent>> m_nEvents = new Dictionary<E_Event, Dictionary<E_Action, UnityEvent>>();

    public void Start()
    {
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart() 
    {
        yield return new WaitForSeconds(0.1f);
        InvokeEvent(E_Event.Game, E_Action.Start);
    }

    public void AddListener(E_Event eventKey, E_Action actionKey, UnityAction function) 
    {
        if (!m_nEvents.ContainsKey(eventKey)) 
        {
            m_nEvents.Add(eventKey, new Dictionary<E_Action, UnityEvent>());
        }

        if (!m_nEvents[eventKey].ContainsKey(actionKey))
        {
            m_nEvents[eventKey].Add(actionKey, new UnityEvent());
        }

        m_nEvents[eventKey][actionKey].AddListener(function);
    }

    public void InvokeEvent(E_Event eventKey, E_Action actionKey) 
    {
        if (m_nEvents.ContainsKey(eventKey) && m_nEvents[eventKey].ContainsKey(actionKey)) 
        {
            m_nEvents[eventKey][actionKey].Invoke();
        }
        else
        {
            Debug.Log("[Event Manager] No Event of type " + eventKey.ToString() + " > " + actionKey.ToString());
        }
    }
}

public class BoolEvent : UnityEvent<bool> { }
