using QuinnMeshes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Building : MonoBehaviour
{
    public Material material;
    public Renderer rend;
    bool isTarget = false;
    internal Road road;
    public Vector3[] pointArray;

    public Range xLimits = new Range();
    public Range zLimits = new Range();

    Vector3 deliveryCenter = Vector3.zero;
    public Vector3 meshCenter;
    Vector3 extents = Vector3.zero;

    public void Init(List<Vector3> points, Road parent, Material _mat, Range _xLim, Range _zLim)
    {
        //Debug.Log("Init start for " + name);

        xLimits = _xLim;
        zLimits = _zLim;

        pointArray = points.ToArray();
        road = parent;

        float minDistance = 0.1f;
        float height = Random.Range(3, 10);

        Vector3 up = Vector3.up * height;

        Vector3 centerpoint = GetCenter(points);

        List<int> indexes = new List<int>();
        for (int p = 0; p < pointArray.Length; p++)
        {
            indexes.Add(p);
        }

        while (indexes.Count > 0)
        {
            int i = indexes[0];
            indexes.RemoveAt(0);

            int j = i + 1;
            if (j == points.Count)
            {
                j = 0;
            }

            int k = i - 1;
            if (k == -1)
            {
                k = points.Count - 1;
            }

            //Vector3 adjCenter = Vector3.Lerp(Vector3.Lerp(points[j], centerpoint, 1 - minDistance), Vector3.Lerp(points[k], centerpoint, 1 - minDistance), 0.5f);
            //need to find a way to get a proper center, possibly also move points k and j and readd those to the index list if they aren't in there
            Vector3 adjCenter = centerpoint;

            #region old adjCenter
            //Vector3 adjCenter = _points[i];
            //if (adjCenter.z == zLimits.min || adjCenter.z == zLimits.max)
            //{
            //    adjCenter.z = centerpoint.z;
            //}

            //if (adjCenter.x == xLimits.min || adjCenter.x == xLimits.max)
            //{
            //    adjCenter.x = centerpoint.x;
            //}
            #endregion

            Vector3 hitPoint = TestCollision(points[i]);

            while (hitPoint != Vector3.zero && Vector3.Distance(points[i], adjCenter) > minDistance)
            {
                points[i] = Vector3.Lerp(points[i], adjCenter, 0.001f);
                hitPoint = TestCollision(points[i]);
            }

            if (hitPoint != Vector3.zero)
            {
                Destroy(gameObject);
                return;
            }

            centerpoint = GetCenter(points);

        }

        pointArray = points.ToArray();

        #region Mesh Creation
        QMesh _qMesh = new QMesh();

        //side walls
        for (int i = 0; i < points.Count; i++)
        {
            int j = i + 1;
            if (j >= points.Count)
            {
                j = 0;
            }

            Vertex[] _v = new Vertex[] { 
                new Vertex(points[i], new Vector2(0, 0)),
                new Vertex(points[j], new Vector2(1, 0)),
                new Vertex(points[i] + up, new Vector2(0, 1)),
                new Vertex(points[j] + up, new Vector2(1, 1)),
            };

            Triangle tri1 = new Triangle();
            tri1.vertices = new Vertex[3] { _v[0], _v[2], _v[3] };
            _qMesh.triangles.Add(tri1);

            Triangle tri2 = new Triangle();
            tri2.vertices = new Vertex[3] { _v[0], _v[3], _v[1] };
            _qMesh.triangles.Add(tri2);
        }

        //top and bottom
        for (int i = 0; i < points.Count; i++)
        {
            int j = i + 1;
            if (j >= points.Count)
            {
                j = 0;
            }

            Vertex[] _v = new Vertex[] {
                new Vertex(points[i], new Vector2(0, 0)),
                new Vertex(points[j], new Vector2(0, 0)),
                new Vertex(centerpoint, new Vector2(0, 0)),
                new Vertex(points[i] + up, new Vector2(0, 0)),
                new Vertex(points[j] + up, new Vector2(0, 0)),
                new Vertex(centerpoint + up, new Vector2(0, 0))
            };

            Triangle tri1 = new Triangle();
            tri1.vertices = new Vertex[3] { _v[0], _v[1], _v[2] };
            _qMesh.triangles.Add(tri1);

            Triangle tri2 = new Triangle();
            tri2.vertices = new Vertex[3] { _v[5], _v[4], _v[3] };
            _qMesh.triangles.Add(tri2);
        }

        MeshFilter filter = gameObject.AddComponent<MeshFilter>();
        MeshCollider collider = gameObject.AddComponent<MeshCollider>();
        rend = gameObject.AddComponent<MeshRenderer>();

        Mesh mesh = _qMesh.ConvertToMesh();

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        filter.sharedMesh = collider.sharedMesh = mesh;
        rend.material = _mat;
        material = _mat;

        meshCenter = mesh.bounds.center;
        #endregion

        //deliveryCenter = meshCenter + (transform.right * (roadSide == Direction.RIGHT ? -1 : 1) * Building_Manager.Instance.buildingDepth);
        //extents = topRight - bottomLeft;
    }

    Vector3 GetCenter(List<Vector3> points) 
    {
        Vector3 centerpoint = Vector3.zero;
        foreach (Vector3 point in points)
        {
            centerpoint += point;
        }
        centerpoint /= points.Count;
        return centerpoint;
    }

    private Vector3 TestCollision(Vector3 point) 
    {
        LayerMask mask = 1 << LayerMask.NameToLayer("Road");
        mask |= 1 << LayerMask.NameToLayer("Building");

        point += Vector3.up * 20;

        RaycastHit hit;

        //if (Physics.Raycast(transform.TransformPoint(point), Vector3.down, out hit, 60, mask))
        if (Physics.BoxCast(transform.TransformPoint(point), new Vector3(0.01f, 0.01f, 0.01f), Vector3.down, out hit, transform.rotation, 60, mask))
        {
            //Debug.DrawLine(transform.TransformPoint(point), hit.point, Color.red, 20);
            return transform.InverseTransformPoint(hit.point);
        }
        else
        {
            //Debug.DrawRay(transform.TransformPoint(point), Vector3.down * 50, Color.blue, 10);
            return Vector3.zero;
        }
    }

    public void Update()
    {
        if (isTarget) 
        {
            CheckDeliveryZone();
        }
    }

    private void CheckDeliveryZone()
    {
        Car_Movement player;
        Collider[] colliders = Physics.OverlapBox(deliveryCenter, new Vector3(1,20,1), transform.rotation);
        foreach (Collider collider in colliders) 
        {
            if (collider.TryGetComponent(out player))
            {
                CompleteDelivery();
                return;
            }
        }
    }

    public void SetAsTarget() 
    {
        if (isTarget) return;

        // this probably needs to be more sophisticated, some sort of actual highlighting system;

        rend.material = Delivery_Manager.Instance.targetMaterial;
        isTarget = true;
    }

    public void CompleteDelivery() 
    {
        if (!isTarget) return;

        rend.material = material;
        isTarget = false;
        Event_Manager.Instance._DeliveryMade.Invoke(this);
    }

    internal bool IsSameRoad(Building other)
    {
        return road == other.road;
    }

    private void OnDrawGizmosSelected()
    {
        if (pointArray != null)
        {
            foreach (Vector3 item in pointArray)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(transform.TransformPoint(item), 0.1f);
            }
        }

        if (deliveryCenter != null && isTarget)
        {
            //Gizmos.color = Color.blue;
            //Gizmos.DrawLine(bottomLeft, topRight);

            

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(deliveryCenter, deliveryCenter + extents);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(deliveryCenter, deliveryCenter - extents);
        }
    }
}
