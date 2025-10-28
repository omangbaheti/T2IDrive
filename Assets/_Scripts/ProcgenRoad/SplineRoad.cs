using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting.FullSerializer;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using UnityEngine.Splines;

[ExecuteInEditMode]
[RequireComponent(typeof(SplineContainer))]
public class SplineRoad : MonoBehaviour
{
    public List<Intersection> Intersections => intersections;
    public float RightWidth => rightWidth;
    public float LeftWidth => leftWidth;
    [SerializeField] private bool showGizmos = false;

    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private int resolution;

    private float3 position;
    private float3 forward;
    private float3 upVector;

    [SerializeField] private List<SerializableList<Vector3>> p1_vertices = new();
    [SerializeField] private List<SerializableList<Vector3>> p2_vertices = new();
    [SerializeField] private float leftWidth;
    [SerializeField] private float rightWidth;
    [SerializeField] private Material roadMaterial;
    [SerializeField] private List<GameObject> roadObjects = new();

    [SerializeField] private List<Intersection> intersections = new();
    private void OnEnable()
    {
        foreach (Spline spline in splineContainer.Splines)
        {
            spline.changed += SplineChanged;
        }
    }

    private void OnDisable()
    {
        foreach (Spline spline in splineContainer.Splines)
        {
            spline.changed -= SplineChanged;
        }
    }

    private void Awake()
    {
        BuildMesh();
    }

    // Update is called once per frame
    private void GetVertices()
    {
        p1_vertices = new();
        p2_vertices = new();
        float step = 1f/resolution;
        for (int i = 0; i < splineContainer.Splines.Count; i++)
        {
            List<Vector3> p1Vertices = new();
            List<Vector3> p2Vertices = new();
            float t = 0;
            for (int j = 0; j < resolution; j++)
            {
                t = j * step;
                SampleAlongSplineWidth(i, t, leftWidth, out float3 p1, out float3 p2);
                p1Vertices.Add(p1);
                p2Vertices.Add(p2);
            }
            t = resolution * step;
            SampleAlongSplineWidth(i, resolution, leftWidth, out float3 lastp1, out float3 lastp2);
            p1Vertices.Add(lastp1);
            p2Vertices.Add(lastp2);

            p1_vertices.Add(new()
            {
                points = p1Vertices,
            });
            p2_vertices.Add(new()
            {
                points = p2Vertices,
            });
        }
    }

    private void SampleAlongSplineWidth(int splineIndex, float step, float width, out float3 p1, out float3 p2)
    {
        splineContainer.Evaluate(splineIndex, step, out position, out forward, out upVector);
        //we can use either upVector or Vector3.up
        //Vector3.up ensure road is flat, while upVector builds the road along the bezier curve's local up
        float3 right = Vector3.Cross(forward, Vector3.up).normalized;
        p1 = position + (right * rightWidth);
        p2 = position + (-right * leftWidth);
    }

    private void SplineChanged()
    {
        BuildMesh();
    }

    private void BuildMesh()
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
            float uvOffset = 0;
            List<Vector2> uvs = new();
            
            for (int currSplinePoint = 1; currSplinePoint <= resolution; currSplinePoint++)
            {
                Vector3 p1 = p1_vertices[currSplineIndex].points[currSplinePoint-1];
                Vector3 p2 = p2_vertices[currSplineIndex].points[currSplinePoint-1];
                Vector3 p3 = p1_vertices[currSplineIndex].points[currSplinePoint];
                Vector3 p4 = p2_vertices[currSplineIndex].points[currSplinePoint];
                
                int baseIndex = verts.Count;

                int t1 = baseIndex + 0;
                int t2 = baseIndex + 2;
                int t3 = baseIndex + 3;
                int t4 = baseIndex + 1;
                verts.AddRange(new List<Vector3>{p1, p2, p3, p4});
                tris.AddRange(new List<int>{t1, t2, t3, t3, t4, t1});
                
                float normalizedDistance = Vector3.Distance(p1, p3) / 4f;
                float uvDistance = uvOffset + normalizedDistance;
                uvs.AddRange(new List<Vector2>()
                {
                    new(uvOffset, 0),
                    new(uvOffset,1),      
                    new(uvDistance,0),    
                    new(uvDistance,1)     
                });
                uvOffset += normalizedDistance;
            }
            Mesh mesh = new();
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject splineGO = new($"SplineMesh_{currSplineIndex}");
            splineGO.transform.parent = transform;
            roadObjects.Add(splineGO);  
            MeshFilter mf = splineGO.AddComponent<MeshFilter>();
            MeshRenderer mr = splineGO.AddComponent<MeshRenderer>();
            mf.mesh = mesh;
            mr.material = roadMaterial;
            mr.sharedMaterial.mainTexture.wrapMode = TextureWrapMode.Repeat;
        }

        BuildAllJunctions();
    }

    public void AddJunction(Intersection intersectionToAdd)
    {
        HashSet<int> newTerminals = new();
        HashSet<int> newKnots = new();
        foreach (SplineTerminalInfo terminal in intersectionToAdd.Terminals)
        {
            newTerminals.Add(terminal.splineIndex);
            newKnots.Add(terminal.knotIndex);
        }
        bool intersectionExists = false;
        foreach (Intersection _intersection in intersections)
        {
            HashSet<int> existingTerminals = new();
            HashSet<int> existingKnots = new();
            foreach (SplineTerminalInfo terminal in _intersection.Terminals)
            {
                existingTerminals.Add(terminal.splineIndex);
                existingKnots.Add(terminal.knotIndex);
            }
            if (existingTerminals.SetEquals(newTerminals) && existingKnots.SetEquals(newKnots))
            {
                intersectionExists = true;
            }
        }

        if (!intersectionExists)
        {
            intersections.Add(intersectionToAdd);
        }
        else
        {
            Debug.LogWarning("Duplicate intersection");
        }
    }

    public void BuildAllJunctions()
    {
        foreach (Intersection intersection in intersections)
        {
            string intersectionID = intersection.Terminals.Aggregate("", (current, terminal) => current + $"{terminal.splineIndex}_");

            int terminalCount = 0;
            List<Vector3> points = new();
            Vector3 center = Vector3.zero;

            //Calculating Centre
            foreach (SplineTerminalInfo terminal in intersection.Terminals)
            {
                int terminalSplineIndex = terminal.splineIndex;
                float t = terminal.knotIndex == 0 ? 0f : 1f;
                // Debug.Log($"{intersectionID}:{t}");
                SampleAlongSplineWidth(terminalSplineIndex, t, leftWidth, out float3 p1, out float3 p2);
                // Debug.Log($"{intersectionID}:{terminal.splineIndex}:{p1},{p2}");
                points.Add(p1);
                points.Add(p2);
                center +=  (Vector3) p1;
                center +=  (Vector3) p2;
                terminalCount++;
            }
            Assert.IsTrue(terminalCount > 0, "No Intersections found");
            //multiplying by 2 as each terminal has 2 points along the width
            center /= terminalCount * 2;

            //Sorting points according to angle from centre
            points.Sort((x, y) =>
            {
                Vector3 xDir = x - center;
                Vector3 yDir = y - center;
                float angleA = Vector3.SignedAngle(center.normalized, xDir.normalized, Vector3.up);
                float angleB = Vector3.SignedAngle(center.normalized, yDir.normalized, Vector3.up);
                if (angleA > angleB)
                {
                    return 1;
                }
                if (angleA < angleB)
                {
                    return -1;
                }
                return 0;
            });

            //Finally making the mesh based on sorted points and centre
            List<Vector3> verts = new();
            List<int> tris = new();
            int pointOffset = verts.Count;

            for (int i = 1; i <= points.Count; i++)
            {
                verts.Add(center);
                verts.Add(points[i -1]);
                if (i == points.Count)
                {
                    verts.Add(points[0]);
                }
                else
                {
                    verts.Add(points[i]);
                }

                tris.Add(pointOffset + ((i - 1) * 3) + 0);
                tris.Add(pointOffset + ((i - 1) * 3) + 1);
                tris.Add(pointOffset + ((i - 1) * 3) + 2);
            }

            Mesh mesh = new();
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject intersectionGO = new($"SplineIntersection_{intersectionID}");
            intersectionGO.transform.parent = transform;
            roadObjects.Add(intersectionGO);
            MeshFilter mf = intersectionGO.AddComponent<MeshFilter>();
            MeshRenderer mr = intersectionGO.AddComponent<MeshRenderer>();
            mf.mesh = mesh;
            mr.material = roadMaterial;
        }
    }

    private void OnDrawGizmos()
    {
        // Handles.matrix = transform.localToWorldMatrix;
        if (Application.isPlaying || !showGizmos)
        {
            return;
        }

        Handles.color = Color.red;
        for (int i = 0; i < p1_vertices.Count; i++)
        {
            for (int j = 0; j < p1_vertices[i].points.Count; j++)
            {
                Handles.SphereHandleCap(0, p1_vertices[i].points[j], Quaternion.identity, 0.8f, EventType.Repaint);
            }

        }
        Handles.color = Color.blue;
        for (int i = 0; i < p2_vertices.Count; i++)
        {
            for (int j = 0; j < p2_vertices[i].points.Count; j++)
            {
                Handles.SphereHandleCap(0, p2_vertices[i].points[j], Quaternion.identity, 0.8f, EventType.Repaint);
            }
        }
    }
}

[Serializable]
public class SplineTerminalInfo
{
    public int splineIndex;
    public int knotIndex;
    public Spline spline;
    public BezierKnot bezierKnot;

    public SplineTerminalInfo(int splineIndex, int knotIndex, Spline spline, BezierKnot knot)
    {
        this.splineIndex = splineIndex;
        this.knotIndex = knotIndex;
        this.spline = spline;
        this.bezierKnot = knot;
    }
}

[Serializable]
public class Intersection
{
    public List<SplineTerminalInfo> Terminals => terminals;

    [SerializeField] private List<SplineTerminalInfo> terminals;

    public void AddTerminal(SplineTerminalInfo terminal)
    {
        terminals ??= new();
        terminals.Add(terminal);
    }
}

//Shitty way to make a list of lists serializable
//Hacky af but we ball
[Serializable]
public class SerializableList<T>
{
    public List<T> points;
}