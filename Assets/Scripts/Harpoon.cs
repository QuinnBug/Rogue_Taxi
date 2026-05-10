using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Harpoon : MonoBehaviour
{
    Rigidbody rb;
    Rigidbody playerRb;
    bool stuck = false;
    public float ropeSlack = 1;
    public float lifetime = 0.5f;
    public float hitLifetime = 2.5f;
    internal float hitDistance = 0;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        //playerRb = PlayerManager.Instance.rb;
    }

    private void Update()
    {
        if (stuck && Vector3.Distance(playerRb.transform.position, transform.position) >= hitDistance + ropeSlack)
        {
            playerRb.AddForce((transform.position - playerRb.transform.position) * (playerRb.linearVelocity.magnitude));
        }

        lifetime -= Time.deltaTime;

        if (lifetime <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Building") 
        {
            Debug.Log("hit building");

            stuck = true;
            lifetime = hitLifetime;
            hitDistance = Vector3.Distance(playerRb.transform.position, transform.position);

            rb.useGravity = false;
            rb.mass = Mathf.Infinity;
            rb.linearDamping = Mathf.Infinity;
            rb.linearVelocity = Vector3.zero;
        }
    }
}
