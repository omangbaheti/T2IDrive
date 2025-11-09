using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using System.IO;
using EditorAttributes;

[ExecuteInEditMode]
public class SplineToCSV : MonoBehaviour
{
    [Header("Spline Settings")]
    public SplineContainer splineContainer;
    [Range(0.001f, 0.1f)] public float stepSize = 0.01f;
    public List<int> splineIndices = new List<int>(); // splines to export
    public bool worldSpace = true;

    [System.Serializable]
    public struct KnotConnection
    {
        [Tooltip("Spline index of the first spline")]
        public int fromSpline;

        [Tooltip("Knot index on the first spline (0-based)")]
        public int fromKnot;

        [Tooltip("Spline index of the second spline")]
        public int toSpline;

        [Tooltip("Knot index on the second spline (0-based)")]
        public int toKnot;

        [Tooltip("How many points to generate along this connection line")]
        public int resolution;
    }

    [Header("Lerp Connections (Spline-Knot to Spline-Knot)")]
    public List<KnotConnection> knotConnections = new List<KnotConnection>();

    [Header("Output Settings")]
    public string fileName = "SplinePoints.csv";

    private Vector3 ApplyRightOffset(Vector3 position, Vector3 tangent, Transform containerTransform, bool worldSpace, float offset = 5f)
    {
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(up, tangent).normalized;
        Vector3 offsetPos = position + right * offset;
        return worldSpace ? containerTransform.TransformPoint(offsetPos) : offsetPos;
    }
    [Button]
    public void ExportSplines()
    {
        if (splineContainer == null)
        {
            Debug.LogError("No SplineContainer assigned.");
            return;
        }

        List<Vector3> allPoints = new();

        // --- Sample the chosen splines ---
        foreach (int index in splineIndices)
        {
            if (index < 0 || index >= splineContainer.Splines.Count)
            {
                Debug.LogWarning($"Invalid spline index {index}");
                continue;
            }

            var spline = splineContainer.Splines[index];
            for (float t = 0; t <= 1f; t += stepSize)
            {
                // Vector3 p = spline.EvaluatePosition(t);
                // if (worldSpace)
                //     p = splineContainer.transform.TransformPoint(p);
                // allPoints.Add(p);
                
                Vector3 p = spline.EvaluatePosition(t);
                Vector3 tangent = spline.EvaluateTangent(t);
                tangent.Normalize();
                // Define "up" (assumed world up)
                Vector3 up = Vector3.up;
                
                // Compute right direction
                Vector3 right = Vector3.Cross(up, tangent).normalized;

                // Offset by half the lane width (5 units to the right)
                float laneOffset = index != 6 ? 5f : -5f;
                p += right * laneOffset;

                if (worldSpace)
                    p = splineContainer.transform.TransformPoint(p);

                allPoints.Add(p);
            }
        }

        // --- Connect specified spline knots ---
        foreach (var connection in knotConnections)
        {
            if (connection.fromSpline < 0 || connection.fromSpline >= splineContainer.Splines.Count ||
                connection.toSpline < 0 || connection.toSpline >= splineContainer.Splines.Count)
            {
                Debug.LogWarning($"Invalid spline index in connection ({connection.fromSpline}→{connection.toSpline})");
                continue;
            }

            var splineA = splineContainer.Splines[connection.fromSpline];
            var splineB = splineContainer.Splines[connection.toSpline];

            if (connection.fromKnot < 0 || connection.fromKnot >= splineA.Count ||
                connection.toKnot < 0 || connection.toKnot >= splineB.Count)
            {
                Debug.LogWarning($"Invalid knot index in connection ({connection.fromKnot}→{connection.toKnot})");
                continue;
            }

            Vector3 start = splineA[connection.fromKnot].Position;
            Vector3 end = splineB[connection.toKnot].Position;
            if (worldSpace)
            {
                start = splineContainer.transform.TransformPoint(start);
                end = splineContainer.transform.TransformPoint(end);
            }

            int resolution = Mathf.Max(2, connection.resolution);
            for (int i = 1; i <= resolution; i++)
            {
                float t = i / (float)resolution;
                Vector3 tangent = (end - start).normalized;
                Vector3 lerpPoint = Vector3.Lerp(start, end, t);
                // Apply same right-lane offset
                Vector3 offsetPos = ApplyRightOffset(lerpPoint, tangent, splineContainer.transform, false);
                allPoints.Add(offsetPos);
            }
        }

        // --- Write everything to CSV ---
        string path = Path.Combine(Application.dataPath, fileName);
        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("x,y,z");
            foreach (var p in allPoints)
                writer.WriteLine($"{p.x},{p.y},{p.z}");
        }

        Debug.Log($"Exported {allPoints.Count} points to {path}");
        
    }

}