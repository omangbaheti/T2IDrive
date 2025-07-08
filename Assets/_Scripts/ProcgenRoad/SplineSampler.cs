using System;
using System.Collections.Generic;
using EditorAttributes;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Splines;

[ExecuteInEditMode]
public class SplineSampler : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private int splineIndex;
    [Range(1f, 200f)]
    [SerializeField] private int resolution;

    [SerializeField] [Range(0f, 1f)]
    private float time;

    private float3 position;
    private float3 forward;
    private float3 upVector;

    [SerializeField] private List<Vector3> p1_vertices = new();
    [SerializeField] private List<Vector3> p2_vertices = new();
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private float width;

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    private void OnEnable()
    {
        Spline.Changed += BuildMesh;
    }

    private void OnDisable()
    {
        Spline.Changed -= BuildMesh;
    }

    private void Update()
    {
        GetVertices();
    }

    // Update is called once per frame
    private void GetVertices()
    {
        p1_vertices = new();
        p2_vertices = new();
        float step = 1f/resolution;
        for (int i = 0; i < resolution; i++)
        {
            float t = i * step;
            splineContainer.Evaluate(splineIndex, t, out position, out forward, out upVector);
            float3 right = Vector3.Cross(forward, upVector).normalized;
            position.y = 0;
            var p1 = position + (right * width);
            var p2 = position + (-right * width);
            p1_vertices.Add(p1);
            p2_vertices.Add(p2);
        }
    }

    private void BuildMesh(Spline spline, int i1, SplineModification arg3)
    {
        Mesh roadMesh = new();
        List<Vector3> verts = new();
        List<int> tris = new();

        int length = p1_vertices.Count;

        for (int i = 1; i <= length; i++)
        {
            Vector3 p1 = p1_vertices[i-1];
            Vector3 p2 = p2_vertices[i-1];
            Vector3 p3;
            Vector3 p4;

            if (i == length)
            {
                p3 = p1_vertices[0];
                p4 = p2_vertices[0];
            }
            else
            {
                p3 = p1_vertices[i];
                p4 = p2_vertices[i];
            }

            int offset = 4 * (i - 1);

            int t1 = offset + 0;
            int t2 = offset + 2;
            int t3 = offset + 3;

            int t4 = offset + 3;
            int t5 = offset + 1;
            verts.AddRange(new List<Vector3>{p1, p2, p3 ,p4});
            tris.AddRange(new List<int>{t1, t2, t3, t4, t5, t1});
        }

        roadMesh.SetVertices(verts);
        roadMesh.SetTriangles(tris, 0);
        roadMesh.RecalculateNormals();
        roadMesh.RecalculateBounds();
        meshFilter.sharedMesh = roadMesh;
    }

    private void OnDrawGizmos()
    {

        Handles.matrix = transform.localToWorldMatrix;
        Handles.color = Color.red;
        for (int i = 0; i < p1_vertices.Count; i++)
        {
            Handles.SphereHandleCap(0, p1_vertices[i], Quaternion.identity, 0.8f, EventType.Repaint);
        }
        Handles.color = Color.blue;
        for (int i = 0; i < p2_vertices.Count; i++)
        {
            Handles.SphereHandleCap(0, p2_vertices[i], Quaternion.identity, 0.8f, EventType.Repaint);
        }

    }
}
