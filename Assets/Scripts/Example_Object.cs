using UnityEngine;

public class Example_Object : MonoBehaviour
{
    public float startTime = 60000;
    [SerializeField]
    private float timer = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timer = startTime;
    }

    // Update is called once per frame
    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            Destroy(this);
        }
    }
}


