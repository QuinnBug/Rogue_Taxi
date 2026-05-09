using UnityEngine;

public class BuildingData : MonoBehaviour
{
    public Vector3 size;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + (Vector3.up * size.y / 2), size);
    }
}
