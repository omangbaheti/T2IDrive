using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Splines;

[ExecuteInEditMode]
public class SplineRoad : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private int resolution;

    private float3 position;
    private float3 forward;
    private float3 upVector;

    [SerializeField] private List<Vector3> p1_vertices = new();
    [SerializeField] private List<Vector3> p2_vertices = new();
    [SerializeField] private float width;
    [SerializeField] private Material roadMaterial;
    [SerializeField] private List<GameObject> roadObjects = new();

    private void OnEnable()
    {
        Spline.Changed += BuildMesh;
    }

    private void OnDisable()
    {
        Spline.Changed -= BuildMesh;
    }
    
    // Update is called once per frame
    private void GetVertices()
    {
        p1_vertices = new();
        p2_vertices = new();
        float step = 1f/resolution;
        for (int i = 0; i < splineContainer.Splines.Count; i++)
        {
            for (int j = 0; j < resolution; j++)
            {
                float t = j * step;
                splineContainer.Evaluate(i, t, out position, out forward, out upVector);
                //we can use either upVector or Vector3.up
                //Vector3.up ensure road is flat, while upVector builds the road along the bezier curve's local up
                float3 right = Vector3.Cross(forward, Vector3.up).normalized;
                float3 p1 = position + (right * width/2);
                float3 p2 = position + (-right * width/2);
                p1_vertices.Add(p1);
                p2_vertices.Add(p2);
            }
        }
        
    }
    

    private void BuildMesh(Spline spline, int i1, SplineModification arg3)
    {
        GetVertices();
        int offset = 0;

        foreach (GameObject roadMesh in roadObjects)       
        {
            DestroyImmediate(roadMesh);
        }
        roadObjects.Clear();
        
        for (int currSplineIndex = 0; currSplineIndex < splineContainer.Splines.Count; currSplineIndex++)
        {
            List<Vector3> verts = new();
            List<int> tris = new();
            int splineOffset = resolution * currSplineIndex;
            for (int currSplinePoint = 1; currSplinePoint < resolution; currSplinePoint++)
            {
                int vertexIndex = splineOffset + currSplinePoint;
                Vector3 p1 = p1_vertices[vertexIndex-1];
                Vector3 p2 = p2_vertices[vertexIndex-1];
                Vector3 p3 = p1_vertices[vertexIndex];
                Vector3 p4 = p2_vertices[vertexIndex];
                
                int baseIndex = verts.Count;

                int t1 = baseIndex + 0;
                int t2 = baseIndex + 2;
                int t3 = baseIndex + 3;
                int t4 = baseIndex + 1;
                verts.AddRange(new List<Vector3>{p1, p2, p3, p4});
                tris.AddRange(new List<int>{t1, t2, t3, t3, t4, t1});
            }
            Mesh mesh = new();
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            GameObject splineGO = new($"SplineMesh_{currSplineIndex}");
            splineGO.transform.parent = transform;
            roadObjects.Add(splineGO);
            MeshFilter mf = splineGO.AddComponent<MeshFilter>();
            MeshRenderer mr = splineGO.AddComponent<MeshRenderer>();
            mf.mesh = mesh;
            mr.material = roadMaterial;
        }
        
    }

    private void OnDrawGizmos()
    {

        // Handles.matrix = transform.localToWorldMatrix;
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
