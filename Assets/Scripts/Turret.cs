//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.InputSystem;

//public class Turret : MonoBehaviour
//{
//    public GameObject projectilePrefab;
//    public Vector3 projectileSpawnpoint;
//    public Vector3 projectileForceDirection;
//    public float projectileForcePower;

//    Transform parent;
//    Rigidbody rb;

//    Vector3 aimingInput;

//    public float turnSpeed;

//    private GameObject projectile;

//    internal bool active;

//    internal int ammo;
//    internal float fireDelay;
//    private float fireTimer;
//    private bool firing;

//    // Start is called before the first frame update
//    void Start()
//    {
//        parent = transform.parent;
//        rb = GetComponentInParent<Rigidbody>();
//    }

//    // Update is called once per frame
//    void Update()
//    {
//        Rotation();

//        if (!active) return;
        
//        Shoot();
//    }

//    public void Rotation() 
//    {
//        //Vector3 targetPos = transform.position + parent.InverseTransformDirection(aimingInput);
//        Vector3 targetPos = transform.position + Vector3.forward + aimingInput;

//        Vector3 diff = targetPos - transform.position;
//        diff.Normalize();
//        float rot_y = Mathf.Atan2(diff.x, diff.z) * Mathf.Rad2Deg;
//        Quaternion desiredRot = Quaternion.Euler(0f, rot_y, 0);
//        transform.localRotation = Quaternion.Lerp(transform.localRotation, desiredRot, turnSpeed * Time.deltaTime);
//    }

//    public void Shoot() 
//    {
//        if (ammo <= 0 || !active)
//        {
//            return;
//        }

//        if (fireTimer > 0)
//        {
//            fireTimer -= Time.deltaTime;
//            return;
//        }

//        if (firing && fireTimer <= 0)
//        {
//            fireTimer = fireDelay;

//            PlayerManager.Instance.stats.currentAmmo--;
//            projectile = Instantiate(projectilePrefab, transform.TransformPoint(projectileSpawnpoint), Quaternion.identity);

//            Rigidbody proj_rb;
//            if (projectile.TryGetComponent(out proj_rb))
//            {
//                //Vector3 force = transform.TransformDirection(projectileForceDirection) * projectileForcePower;
//                Vector3 force = transform.forward * projectileForcePower;
//                force.y = projectileForceDirection.y;

//                //Adjustmest for velocity
//                //Vector3 carVel = rb.velocity;
//                //carVel = transform.InverseTransformDirection(carVel);
//                //carVel.y = 0;
//                //carVel.x = Mathf.Abs(carVel.x);
//                //carVel.z = Mathf.Abs(carVel.z);

//                //if (carVel.magnitude > 1)
//                //{
//                //    carVel.Normalize();
//                //    carVel += Vector3.one;
//                //    force.Scale(carVel);
//                //}
//                //end of adjustment

//                proj_rb.linearVelocity = rb.linearVelocity;
//                proj_rb.AddForce(force);
//            }
//        }
//    }

//    public void Fire(InputAction.CallbackContext context) 
//    {
//        if (context.phase == InputActionPhase.Started) 
//        {
//            firing = true;
//        }

//        if (context.phase == InputActionPhase.Canceled)
//        {
//            firing = false;
//        }
//    }

//    public void AimingInput(InputAction.CallbackContext context) 
//    {
//        Vector2 input = context.ReadValue<Vector2>();
        
//        if (input != Vector2.zero)
//        {
//            aimingInput.x = input.x;
//            aimingInput.z = input.y;
//        }
//    }

//    private void OnDrawGizmosSelected()
//    {
//        Gizmos.color = Color.grey;
//        Gizmos.DrawSphere(transform.TransformPoint(projectileSpawnpoint), 0.1f);
//    }
//}
