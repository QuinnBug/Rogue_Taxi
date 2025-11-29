using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;
using UnityEditor;


namespace Earclipping
{
	public class EarClipper : MonoBehaviour
	{
		public bool drawTris;
		[Space]
		public bool doClip;
		[Space]
		public float timePerTri;
		public float timePerPoly;
		[Space]
		public List<Triangle[]> triList = null;
		public bool clippingDone;

		internal NodeMeshConstructor nmc;
		List<Vertex> vertices;

		Vertex displayEarTip = null;

		public void Start()
		{
			displayEarTip = null;
			nmc = FindFirstObjectByType<NodeMeshConstructor>();
			triList = null;
			//clippingDone = false;
		}

		public void Update()
		{
			if (nmc.meshCreated && triList == null && !clippingDone && doClip)
			{
				doClip = false;
				//ClipAllPolygonCoRoutine();
			}
		}

		public void ClipAllPolygonCoRoutine()
		{
			triList = new List<Triangle[]>();
			StartCoroutine(ClipPolygons(nmc.polygons.ToArray()));
		}

		private void OnDrawGizmos()
		{
			if (triList != null && drawTris)
			{
				foreach (Triangle[] tris in triList)
				{
					foreach (Triangle triangle in tris)
					{
						for (int i = 0; i < 3; i++)
						{
							int j = Lists.ClampListIndex(i + 1, 3);

							Gizmos.color = Color.red;
							Gizmos.DrawLine(triangle.vertices[i], triangle.vertices[j]);
						}
					}
				}
			}

			if (displayEarTip != null && displayEarTip.prev != null && displayEarTip.next != null)
			{
				Handles.Label(displayEarTip.point, displayEarTip.isReflex.ToString() + " - 1");
				Handles.Label(displayEarTip.prev.point, displayEarTip.prev.isReflex.ToString() + " - 0");
				Handles.Label(displayEarTip.next.point, displayEarTip.next.isReflex.ToString() + " - 2");
			}
		}

		public Triangle[] GetTriangles(Polygon poly)
		{
			vertices = new List<Vertex>();
			List<Triangle> triangles = new List<Triangle>();
			List<Vertex> earVertices = new List<Vertex>();

			Vertex earVertex;
			Vertex earVertexPrev;
			Vertex earVertexNext;
			Triangle newTriangle;

			triangles.Clear();
			//If we just have three points, then we dont have to do all calculations
			if (poly.vertices.Length == 3)
			{
				triangles.Add(new Triangle(poly.vertices[0].point, poly.vertices[1].point, poly.vertices[2].point));
				return triangles.ToArray();
			}

			//Step 1. Store the vertices in a list and we also need to know the next and prev vertex
			vertices.Clear();
			for (int i = 0; i < poly.vertices.Length; i++)
			{
				vertices.Add(new Vertex(poly.vertices[i].point));
			}

			//Find the next and previous vertex
			for (int i = 0; i < vertices.Count; i++)
			{
				vertices[i].prev = vertices[Lists.ClampListIndex(i - 1, vertices.Count)];
				vertices[i].next = vertices[Lists.ClampListIndex(i + 1, vertices.Count)];
			}

			//Step 2. Find the reflex (concave) and convex vertices, and ear vertices
			for (int i = 0; i < vertices.Count; i++)
			{
				CheckIfReflexOrConvex(vertices[i], poly.center);
			}

			//Have to find the ears after we have found if the vertex is reflex or convex
			earVertices.Clear();
			for (int i = 0; i < vertices.Count; i++)
			{
				IsVertexEar(vertices[i], vertices, earVertices, poly);
			}

			int loopCount = 0;
			//Step 3. Triangulate!

			while (true)
			{
				loopCount++;

				//This means we have just one triangle left
				if (vertices.Count == 3)
				{
					//The final triangle
					triangles.Add(new Triangle(vertices[0].point, vertices[0].prev.point, vertices[0].next.point));
					break;
				}
				else if (vertices.Count < 3 || earVertices.Count == 0) break;

				//Make a triangle of the first ear
				earVertex = earVertices[0];
				earVertexPrev = earVertex.prev;
				earVertexNext = earVertex.next;

				newTriangle = new Triangle(earVertex.point, earVertexPrev.point, earVertexNext.point);

				triangles.Add(newTriangle);

				//Remove the vertex from the lists
				earVertices.Remove(earVertex);
				vertices.Remove(earVertex);

				//Update the previous vertex and next vertex
				earVertexPrev.next = earVertexNext;
				earVertexNext.prev = earVertexPrev;

				//...see if we have found a new ear by investigating the two vertices that were part of the ear
				CheckIfReflexOrConvex(earVertexPrev, poly.center);
				CheckIfReflexOrConvex(earVertexNext, poly.center);

				earVertices.Remove(earVertexPrev);
				earVertices.Remove(earVertexNext);

				IsVertexEar(earVertexPrev, vertices, earVertices, poly);
				IsVertexEar(earVertexNext, vertices, earVertices, poly);
			}

			return triangles.ToArray();
		}

		IEnumerator ClipPolygons(Polygon[] polygons)
		{
			foreach (Polygon poly in polygons)
			{
				triList.Add(GetTriangles(poly));
				if (timePerPoly > 0) yield return new WaitForSeconds(timePerPoly);
			}

			clippingDone = true;
		}

		//Check if a vertex if reflex or convex, and add to appropriate list
		private void CheckIfReflexOrConvex(Vertex v, Vector3 center)
		{
			v.isReflex = false;

            //This is a reflex vertex if its triangle is oriented clockwise
            Vector3 a = v.prev.point;
            Vector3 b = v.point;
            Vector3 c = v.next.point;

            v.isReflex = (new Triangle(b, a, c).IsTriangleOrientedClockwise());
		}
	
		//Check if a vertex is an ear
		private void IsVertexEar(Vertex v, List<Vertex> vertices, List<Vertex> earVertices, Polygon poly)
		{
			//A reflex vertex cant be an ear!
			if (v.isReflex)
			{
				return;
			}
	
			bool hasPointInside = false;
            Triangle tri = new Triangle(v.prev.point, v.point, v.next.point);
            for (int i = 0; i < vertices.Count; i++)
			{
				//We only need to check if a reflex vertex is inside of the triangle
				if (vertices[i].isReflex)
				{
					//This means inside and not on the hull
					if (tri.IsPointInside(vertices[i].point))
					{
						hasPointInside = true;
						break;
					}
				}
			}
	
			if (!hasPointInside)
			{
				earVertices.Add(v);
			}
            else
            {
				//tri.DebugDraw(Color.green, timePerTri * 0.95f);
			}
		}
	}
	
	[System.Serializable]
	public class Vertex
	{
		//position
		public Vector3 point;
		public Vector2 uv;
		public Vertex next;
		public Vertex prev;
	
		public bool isReflex = false;
	
	    public Vertex(Vector3 p)
	    {
			point = p;
	    }
	}
	
	[System.Serializable]
	public class Triangle 
	{
	    public Vector3[] vertices = new Vector3[3];
	
		public Triangle(Vector3 a, Vector3 b, Vector3 c) 
		{
			vertices[0] = a;
			vertices[1] = b;
			vertices[2] = c;
		}
	
	    //adjusted for x,z orientation
	    public bool IsTriangleOrientedClockwise()
	    {
	        Vector3 a, b, c;
	        a = vertices[0];
	        b = vertices[1];
	        c = vertices[2];

			float determinant = a.x * b.z + c.x * a.z + b.x * c.z - a.x * c.z - c.x * b.z - b.x * a.z;
			//float determinant = (b.x - a.x) * (c.z - a.z) - (c.x - a.x) * (b.z - a.z);
			//Debug.Log(determinant + " = " + a + " " + b + " " + c);
			return determinant < 0.0f;
		}
	
		//adjusted for x,z orientation
		public bool IsPointInside(Vector3 p)
		{
			Vector3 p1, p2, p3;
			p1 = vertices[0];
			p2 = vertices[1];
			p3 = vertices[2];
	
			//Based on Barycentric coordinates
			float denominator = ((p2.z - p3.z) * (p1.x - p3.x) + (p3.x - p2.x) * (p1.z - p3.z));
	
			float a = ((p2.z - p3.z) * (p.x - p3.x) + (p3.x - p2.x) * (p.z - p3.z)) / denominator;
			float b = ((p3.z - p1.z) * (p.x - p3.x) + (p1.x - p3.x) * (p.z - p3.z)) / denominator;
			float c = 1 - a - b;
	
			//The point is within the triangle or on the border if 0 <= a <= 1 and 0 <= b <= 1 and 0 <= c <= 1
			//if (a >= 0f && a <= 1f && b >= 0f && b <= 1f && c >= 0f && c <= 1f)
			//{
			//    isWithinTriangle = true;
			//}
	
			//The point is within the triangle (including on the border)
			//return (a >= 0f && a <= 1f && b >= 0f && b <= 1f && c >= 0f && c <= 1f);
			return (a > 0f && a < 1f && b > 0f && b < 1f && c > 0f && c < 1f);
		}

		public void DebugDraw(Color color, float time) 
		{
            for (int i = 0; i < 3; i++)
            {
				int j = Lists.ClampListIndex(i + 1, 3);
				Debug.DrawLine(vertices[i], vertices[j], color, time);
            }
		}
	}

	[System.Serializable]
	public class Polygon
	{
		public Vector3 center;
		public Vertex[] vertices;
		public Line[] lines;

		public bool isThreeD = false; //3d flag
		public bool isVert = false; // Vertical flag

		//This list of the polygons that form the walls and roof.
		public List<Polygon> linkedPolygons = null;

		public Polygon(List<Line> nodeLines, Vector3 _center)
		{
			lines = nodeLines.ToArray();

			List<Line> tempLines = new List<Line>(lines);
			List<Vertex> points = new List<Vertex>();

			//if(SortLines(tempLines, out List<Line> sortedLines)) 
			//{
			//	tempLines = sortedLines;
			//}

			//add the first line
			points.Add(new Vertex(tempLines[0].a));
			points.Add(new Vertex(tempLines[0].b));
			tempLines.RemoveAt(0);

            while (tempLines.Count > 0)
            {
				bool found = false;

				int closestIdx = 0;
				bool closeA = false;
				float closestDist = 9999999999;

                for (int i = 0; i < tempLines.Count; ++i)
                {
					if (tempLines[i].a == points[points.Count - 1].point)
					{
						points.Add(new Vertex(tempLines[i].b));
						tempLines.RemoveAt(i);
						found = true;
						break;
					}
					else if (tempLines[i].b == points[points.Count - 1].point)
					{
						points.Add(new Vertex(tempLines[i].a));
						tempLines.RemoveAt(i);
						found = true;
						break;
					}
                    else if (Vector3.Distance(tempLines[i].a, points[points.Count - 1].point) < closestDist)
					{
						closeA = true;
						closestIdx = i;
						closestDist = Vector3.Distance(tempLines[i].a, points[points.Count - 1].point);
					}
                    else if (Vector3.Distance(tempLines[i].b, points[points.Count - 1].point) < closestDist)
                    {
						closeA = false;
						closestIdx = i;
						closestDist = Vector3.Distance(tempLines[i].b, points[points.Count - 1].point);
					}
				}

                if (!found)
                {
					points.Add(new Vertex(closeA ? tempLines[closestIdx].a : tempLines[closestIdx].b));
					points.Add(new Vertex(!closeA ? tempLines[closestIdx].a : tempLines[closestIdx].b));

					tempLines.RemoveAt(closestIdx);
					//Debug.Log("no identical line in tLine " + center);
                }
            }
            
			vertices = points.ToArray();

			center = _center;
		}

		public Polygon(Vector3 _center, Vector3[] _points, bool _isVert = false) 
		{
			center = _center;

			vertices = new Vertex[_points.Length];
            for (int i = 0; i < _points.Length; i++)
            {
				vertices[i] = new Vertex(_points[i]);
            }

			isVert = _isVert;

			List<Line> tempLines = new List<Line>();
            for (int i = 0; i < _points.Length; i++)
            {
				int j = Lists.ClampListIndex(i + 1, _points.Length);

				tempLines.Add(new Line(_points[i], _points[j]));
            }
			lines = tempLines.ToArray();
		}

		public void Flip() 
		{
			//flipping the vertices is easy, just remake the line list rather than flip them all? I think there's a line flip func anyway tho
		}

		public void AddConnectedPolygon(Polygon other)
		{
			if (linkedPolygons == null) linkedPolygons = new List<Polygon>() { other };
			else linkedPolygons.Add(other);
		}

        internal bool TriInBounds(Triangle newTriangle)
        {
			Line testLine = new Line(center, center + (Vector3.up + Vector3.right) * 99999999);

			//loop through the 3 midpoints of the lines
            for (int i = 0; i < 3; i++)
            {
				int j = Lists.ClampListIndex(i + 1, 3);
				testLine.a = Vector3.Lerp(newTriangle.vertices[i], newTriangle.vertices[j], 0.5f);

                foreach (Line line in lines)
                {
					if (line.DoesIntersect(testLine, out Vector3 iPoint))
                    {
						return false;
                    }
                }
            }

			return true;
        }

		internal bool SortLines(List<Line> _lines, out List<Line> _sortedLines) 
		{
			_sortedLines = new List<Line>();
			_sortedLines.Add(_lines[0]);
            for (int i = 0; i < _sortedLines.Count; ++i)
            {
				bool found = false;
				foreach (Line secondLine in _lines) 
				{
					if (_sortedLines.Contains(secondLine)) { continue; }

					if (_sortedLines[i].b == secondLine.a) 
					{
						_sortedLines.Add(secondLine);
						found = true;
						break;
					}
					else if (_sortedLines[i].b == secondLine.b) 
					{
						secondLine.Flip();
						_sortedLines.Add(secondLine);
						found = true;
						break;
					}
				}

				if (!found) 
				{
					Debug.LogError("[Polygon.SortLines] There was no matching points between lines");
					return false;
				}
			}

			return true;
		}

		public void DebugDraw(Color color, float duration, bool connecteds = false) 
		{
            foreach (Line line in lines)
            {
				Debug.DrawLine(line.a, line.b, color, duration);
            }

            if (connecteds && linkedPolygons != null)
            {
                foreach (Polygon polygon in linkedPolygons)
                {
					polygon.DebugDraw(Color.green, duration);
                }
            }
		}
    }
}

namespace Utility 
{
	public static class Lists 
	{
		public static int ClampListIndex(int index, int listSize)
		{
			return ((index % listSize) + listSize) % listSize;
		}
	}

	public static class Geometry 
	{
		public static Vector3 GetNormalOfPoints(Vector3 a, Vector3 b, Vector3 c) 
		{
			Vector3 result = Vector3.zero;

			Vector3 U, V;
			U = b - a;
			V = c - a;
			result.x = Mathf.Abs((U.y * V.z) - (U.z * V.y));
			result.y = Mathf.Abs((U.z * V.x) - (U.x * V.z));
			result.z = Mathf.Abs((U.x * V.y) - (U.y * V.x));

			return result.normalized;
		}
	}
}

