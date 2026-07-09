using TMPro;
using UnityEngine;

public class DeliveryUILabel : MonoBehaviour
{
    public int deliveryId;
    public TextMeshProUGUI numberTMP;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Init()
    {
        numberTMP.text = deliveryId.ToString();
    }
}
