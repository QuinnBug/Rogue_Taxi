using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Manager : Singleton<Camera_Manager>
{
    public Transform target;
    public Transform focus;
    public float moveSpeed;
    public Vector3 offset;

    // Update is called once per frame
    void Update()
    {
        focus.position = Vector3.Lerp(focus.position,
            target.position + offset,
            moveSpeed * Time.deltaTime);
    }
}
