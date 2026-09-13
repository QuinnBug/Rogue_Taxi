using TMPro;
using UnityEngine;

public class DeliveryUILabel : MonoBehaviour
{
    public TextMeshProUGUI numberTMP;
    public TextMeshProUGUI destinationTMP;
    public TextMeshProUGUI timeTMP;

    private TruckController player;
    private void Start()
    {
        player = FindFirstObjectByType<TruckController>();
    }

    public void Refresh(Delivery _delivery)
    {
        numberTMP.text = "#" + _delivery.building.m_id.ToString();
        destinationTMP.text = Mathf.RoundToInt((player.transform.position - _delivery.building.transform.position).magnitude).ToString() + "m";
        timeTMP.text = _delivery.TimePassed().ToString() + "s";
    }
}
