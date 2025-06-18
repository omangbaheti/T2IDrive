using System.Collections.Generic;
using UnityEngine;

public enum CoverageStrategy
{
    SingleSeed,
    MultipleSeeds,
    GridBasedSeeding,
    HybridApproach
}

public class EllipsoidPoissonSampling : MonoBehaviour
{
    [Header("Ellipsoid Parameters")]
    public Vector3 ellipsoidRadii = new Vector3(2f, 1f, 1.5f);

    [Header("Poisson Disk Sampling")]
    [Range(0.01f, 2f)]
    public float minDistance = 0.3f;
    [Range(10, 1000)]
    public int maxAttempts = 100;
    [Range(100, 9000)]
    public int maxSamples = 9000;

    [Header("Visualization")]
    public Color pointColor = Color.red;
    public float pointSize = 0.05f;
    public bool showEllipsoidWireframe = true;
    public Color wireframeColor = Color.white;

    [Header("Generation")]
    public bool autoGenerate = true;
    public bool regenerateOnValidate = true;
    [Header("Coverage Strategy")]
    public CoverageStrategy coverageStrategy = CoverageStrategy.MultipleSeeds;
    [Range(1, 20)]
    public int numberOfSeeds = 6;
    public bool useStratifiedSeeding = true;

    private List<Vector3> samplePoints = new List<Vector3>();
    private List<Vector3> activeList = new List<Vector3>();

    void Start()
    {
        if (autoGenerate)
        {
            GenerateSamples();
        }
    }

    void OnValidate()
    {
        if (regenerateOnValidate && Application.isPlaying)
        {
            GenerateSamples();
        }
    }

    [ContextMenu("Generate Samples")]
    public void GenerateSamples()
    {
        samplePoints.Clear();
        activeList.Clear();

        switch (coverageStrategy)
        {
            case CoverageStrategy.SingleSeed:
                GenerateWithSingleSeed();
                break;
            case CoverageStrategy.MultipleSeeds:
                GenerateWithMultipleSeeds();
                break;
            case CoverageStrategy.GridBasedSeeding:
                GenerateWithGridSeeding();
                break;
            case CoverageStrategy.HybridApproach:
                GenerateWithHybridApproach();
                break;
        }

        Debug.Log($"Generated {samplePoints.Count} points on ellipsoid surface using {coverageStrategy}");
    }

    void GenerateWithSingleSeed()
    {
        // Original single seed approach
        Vector3 initialPoint = GetRandomPointOnEllipsoid();
        samplePoints.Add(initialPoint);
        activeList.Add(initialPoint);

        ProcessActiveList();
    }

    void GenerateWithMultipleSeeds()
    {
        // Generate multiple well-distributed seed points
        List<Vector3> seedPoints = useStratifiedSeeding ?
            GenerateStratifiedSeeds(numberOfSeeds) :
            GenerateRandomSeeds(numberOfSeeds);

        // Add all seeds first
        foreach (Vector3 seed in seedPoints)
        {
            if (IsValidSeed(seed))
            {
                samplePoints.Add(seed);
                activeList.Add(seed);
            }
        }

        ProcessActiveList();
    }

    void GenerateWithGridSeeding()
    {
        // Create a spherical grid and project to ellipsoid
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(numberOfSeeds * 4)); // Approximate grid resolution

        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                float u = (float)i / (gridSize - 1);
                float v = (float)j / (gridSize - 1);

                // Convert to spherical coordinates
                float theta = 2f * Mathf.PI * u;
                float phi = Mathf.PI * v;

                Vector3 seedPoint = SphericalToEllipsoid(theta, phi);

                if (IsValidSeed(seedPoint))
                {
                    samplePoints.Add(seedPoint);
                    activeList.Add(seedPoint);
                }

                if (samplePoints.Count >= numberOfSeeds) break;
            }
            if (samplePoints.Count >= numberOfSeeds) break;
        }

        ProcessActiveList();
    }

    void GenerateWithHybridApproach()
    {
        // Combine grid seeding with gap filling
        GenerateWithGridSeeding();

        // Fill remaining gaps with random seeds
        int attempts = 0;
        int maxGapFillAttempts = 1000;

        while (attempts < maxGapFillAttempts && samplePoints.Count < maxSamples)
        {
            Vector3 candidatePoint = GetRandomPointOnEllipsoid();

            if (IsInGap(candidatePoint) && IsValidPoint(candidatePoint))
            {
                samplePoints.Add(candidatePoint);
                activeList.Add(candidatePoint);
            }

            attempts++;
        }

        ProcessActiveList();
    }

    List<Vector3> GenerateStratifiedSeeds(int numSeeds)
    {
        List<Vector3> seeds = new List<Vector3>();

        // Use Fibonacci sphere distribution for even coverage
        float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f)); // Golden angle in radians

        for (int i = 0; i < numSeeds; i++)
        {
            float y = 1f - (i / (float)(numSeeds - 1)) * 2f; // y goes from 1 to -1
            float radius = Mathf.Sqrt(1f - y * y);

            float theta = goldenAngle * i;

            float x = Mathf.Cos(theta) * radius;
            float z = Mathf.Sin(theta) * radius;

            // Scale by ellipsoid radii
            Vector3 seed = new Vector3(
                x * ellipsoidRadii.x,
                y * ellipsoidRadii.y,
                z * ellipsoidRadii.z
            ) + transform.position;

            seeds.Add(seed);
        }

        return seeds;
    }

    List<Vector3> GenerateRandomSeeds(int numSeeds)
    {
        List<Vector3> seeds = new List<Vector3>();

        for (int i = 0; i < numSeeds; i++)
        {
            seeds.Add(GetRandomPointOnEllipsoid());
        }

        return seeds;
    }

    Vector3 SphericalToEllipsoid(float theta, float phi)
    {
        float x = Mathf.Sin(phi) * Mathf.Cos(theta);
        float y = Mathf.Sin(phi) * Mathf.Sin(theta);
        float z = Mathf.Cos(phi);

        return new Vector3(
            x * ellipsoidRadii.x,
            y * ellipsoidRadii.y,
            z * ellipsoidRadii.z
        ) + transform.position;
    }

    bool IsValidSeed(Vector3 seed)
    {
        // Check if seed is far enough from existing seeds
        foreach (Vector3 existingPoint in samplePoints)
        {
            if (Vector3.Distance(seed, existingPoint) < minDistance)
            {
                return false;
            }
        }
        return true;
    }

    bool IsInGap(Vector3 point)
    {
        // Check if point is in a region with low density
        float searchRadius = minDistance * 3f;
        int nearbyCount = 0;

        foreach (Vector3 existingPoint in samplePoints)
        {
            if (Vector3.Distance(point, existingPoint) < searchRadius)
            {
                nearbyCount++;
                if (nearbyCount >= 3) return false; // Not in a gap
            }
        }

        return true; // In a gap
    }

    void ProcessActiveList()
    {
        while (activeList.Count > 0 && samplePoints.Count < maxSamples)
        {
            int randomIndex = Random.Range(0, activeList.Count);
            Vector3 activePoint = activeList[randomIndex];

            bool foundValidPoint = false;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Vector3 newPoint = GeneratePointAroundActive(activePoint);

                if (IsValidPoint(newPoint))
                {
                    samplePoints.Add(newPoint);
                    activeList.Add(newPoint);
                    foundValidPoint = true;
                    break;
                }
            }

            if (!foundValidPoint)
            {
                activeList.RemoveAt(randomIndex);
            }
        }
    }

    Vector3 GetRandomPointOnEllipsoid()
    {
        // Generate random spherical coordinates
        float u = Random.Range(0f, 1f);
        float v = Random.Range(0f, 1f);

        float theta = 2f * Mathf.PI * u; // Azimuthal angle
        float phi = Mathf.Acos(2f * v - 1f); // Polar angle

        // Convert to Cartesian coordinates on unit sphere
        float x = Mathf.Sin(phi) * Mathf.Cos(theta);
        float y = Mathf.Sin(phi) * Mathf.Sin(theta);
        float z = Mathf.Cos(phi);

        // Scale by ellipsoid radii
        return new Vector3(
            x * ellipsoidRadii.x,
            y * ellipsoidRadii.y,
            z * ellipsoidRadii.z
        ) + transform.position;
    }

    Vector3 GeneratePointAroundActive(Vector3 activePoint)
    {
        // Generate a random point in annulus around the active point
        float angle = Random.Range(0f, 2f * Mathf.PI);
        float distance = Random.Range(minDistance, 2f * minDistance);

        // Get local surface normal at active point
        Vector3 localActivePoint = activePoint - transform.position;
        Vector3 surfaceNormal = GetEllipsoidNormal(localActivePoint);

        // Create two orthogonal vectors to the surface normal
        Vector3 tangent1 = Vector3.Cross(surfaceNormal, Vector3.up);
        if (tangent1.magnitude < 0.1f)
            tangent1 = Vector3.Cross(surfaceNormal, Vector3.right);
        tangent1.Normalize();

        Vector3 tangent2 = Vector3.Cross(surfaceNormal, tangent1).normalized;

        // Generate point in tangent plane
        Vector3 offset = (tangent1 * Mathf.Cos(angle) + tangent2 * Mathf.Sin(angle)) * distance;
        Vector3 candidatePoint = activePoint + offset;

        // Project back onto ellipsoid surface
        return ProjectToEllipsoidSurface(candidatePoint);
    }

    Vector3 GetEllipsoidNormal(Vector3 localPoint)
    {
        // Normal to ellipsoid at point (x,y,z) is (2x/a², 2y/b², 2z/c²)
        Vector3 normal = new Vector3(
            2f * localPoint.x / (ellipsoidRadii.x * ellipsoidRadii.x),
            2f * localPoint.y / (ellipsoidRadii.y * ellipsoidRadii.y),
            2f * localPoint.z / (ellipsoidRadii.z * ellipsoidRadii.z)
        );
        return normal.normalized;
    }

    Vector3 ProjectToEllipsoidSurface(Vector3 point)
    {
        Vector3 localPoint = point - transform.position;

        // Iterative method to project point onto ellipsoid surface
        Vector3 projected = localPoint.normalized;

        for (int i = 0; i < 10; i++)
        {
            Vector3 gradient = GetEllipsoidNormal(projected);
            float f = (projected.x * projected.x) / (ellipsoidRadii.x * ellipsoidRadii.x) +
                     (projected.y * projected.y) / (ellipsoidRadii.y * ellipsoidRadii.y) +
                     (projected.z * projected.z) / (ellipsoidRadii.z * ellipsoidRadii.z) - 1f;

            if (Mathf.Abs(f) < 0.001f) break;

            float gradientMagnitude = gradient.sqrMagnitude;
            if (gradientMagnitude > 0)
            {
                projected = projected - gradient * (f / gradientMagnitude);
            }
        }

        return projected + transform.position;
    }

    bool IsValidPoint(Vector3 point)
    {
        // Check if point is too close to existing points
        foreach (Vector3 existingPoint in samplePoints)
        {
            if (Vector3.Distance(point, existingPoint) < minDistance)
            {
                return false;
            }
        }

        return true;
    }

    void OnDrawGizmos()
    {
        // Draw ellipsoid wireframe
        if (showEllipsoidWireframe)
        {
            Gizmos.color = wireframeColor;
            DrawEllipsoidWireframe();
        }

        // Draw sample points
        Gizmos.color = pointColor;
        foreach (Vector3 point in samplePoints)
        {
            Gizmos.DrawSphere(point, pointSize);
        }
    }

    void DrawEllipsoidWireframe()
    {
        int segments = 32;

        // Draw longitude lines
        for (int i = 0; i < segments; i++)
        {
            float theta = (float)i / segments * 2f * Mathf.PI;

            Vector3 prevPoint = Vector3.zero;
            for (int j = 0; j <= segments; j++)
            {
                float phi = (float)j / segments * Mathf.PI;

                Vector3 point = new Vector3(
                    ellipsoidRadii.x * Mathf.Sin(phi) * Mathf.Cos(theta),
                    ellipsoidRadii.y * Mathf.Sin(phi) * Mathf.Sin(theta),
                    ellipsoidRadii.z * Mathf.Cos(phi)
                ) + transform.position;

                if (j > 0)
                {
                    Gizmos.DrawLine(prevPoint, point);
                }
                prevPoint = point;
            }
        }

        // Draw latitude lines
        for (int i = 1; i < segments; i++)
        {
            float phi = (float)i / segments * Mathf.PI;

            Vector3 prevPoint = Vector3.zero;
            for (int j = 0; j <= segments; j++)
            {
                float theta = (float)j / segments * 2f * Mathf.PI;

                Vector3 point = new Vector3(
                    ellipsoidRadii.x * Mathf.Sin(phi) * Mathf.Cos(theta),
                    ellipsoidRadii.y * Mathf.Sin(phi) * Mathf.Sin(theta),
                    ellipsoidRadii.z * Mathf.Cos(phi)
                ) + transform.position;

                if (j > 0)
                {
                    Gizmos.DrawLine(prevPoint, point);
                }
                prevPoint = point;
            }
        }
    }

    [ContextMenu("Clear Samples")]
    public void ClearSamples()
    {
        samplePoints.Clear();
        activeList.Clear();
    }

    public List<Vector3> GetSamplePoints()
    {
        return new List<Vector3>(samplePoints);
    }
}