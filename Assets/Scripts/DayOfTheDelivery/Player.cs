using UnityEngine;

public class Player : MonoBehaviour
{
    public Transform checkpointIndicator;

    private void Start()
    {
        Event_Manager.Instance.AddListener(E_Event.Race, E_Action.Finished, MoveToStart);
    }

    // Update is called once per frame
    void Update()
    {
        if (Race_Manager.Instance.raceStarted)
        {
            checkpointIndicator.transform.position = Race_Manager.Instance.GetCheckpoint(transform).m_point;
        }
    }

    void MoveToStart() 
    {
        transform.position = Race_Manager.Instance.GetCheckpoint(transform).m_point + (Vector3.up * 3);
    }
}
