using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class WheelAxle
{
    public WheelCollider leftWheel;
    public WheelCollider rightWheel;
    public bool isMotor = false;
    public bool isSteering = false;
}

public class StanleyCarController : MonoBehaviour
{
    [Header("Vehicle Setup")]
    public List<WheelAxle> wheelAxles = new List<WheelAxle>();
    public Transform frontAxleCenter; // Reference point for Stanley controller
    
    [Header("Vehicle Parameters")]
    public float maxMotorTorque = 1500f;
    public float maxSteeringAngle = 30f;
    public float wheelbase = 2.5f; // Distance between front and rear axle
    public float maxSpeed = 80f; // km/h
    
    [Header("Stanley Controller Parameters")]
    [Range(0.1f, 5.0f)]
    public float controlGain = 1.0f; // k parameter in Stanley equation
    [Range(0.01f, 1.0f)]
    public float softeningGain = 0.1f; // ks parameter to prevent division by zero
    [Range(0.1f, 2.0f)]
    public float yawRateGain = 0.5f; // Heading error gain
    [Range(0.0f, 1.0f)]
    public float steeringDampGain = 0.3f; // Steering smoothing
    
    [Header("Path Following")]
    public Transform[] pathWaypoints;
    public float lookaheadDistance = 10f;
    public float waypointReachedDistance = 5f;
    
    // Private variables
    private Rigidbody carRigidbody;
    private int currentWaypointIndex = 0;
    private Vector3 currentTarget;
    private float currentSteeringAngle = 0f;
    private float previousSteeringAngle = 0f;
    
    // Spline interpolation variables
    private List<Vector3> splinePoints = new List<Vector3>();
    private int currentSplineIndex = 0;
    
    void Start()
    {
        carRigidbody = GetComponent<Rigidbody>();
        
        // Set center of mass lower for better stability
        carRigidbody.centerOfMass = new Vector3(0, -0.5f, 0.3f);
        
        // Generate spline points from waypoints
        GenerateSplineFromWaypoints();
        
        // Initialize target
        if (splinePoints.Count > 0)
        {
            currentTarget = splinePoints[0];
        }
    }
    
    void FixedUpdate()
    {
        if (splinePoints.Count == 0) return;
        
        // Update target point
        UpdateTargetPoint();
        
        // Calculate Stanley steering
        float steeringCommand = CalculateStanleySteeringAngle();
        
        // Apply smooth steering with damping
        ApplySteeringWithDamping(steeringCommand);
        
        // Calculate and apply motor torque
        ApplyMotorControl();
        
        // Update wheel visuals if needed
        UpdateWheelVisuals();
    }
    
    void GenerateSplineFromWaypoints()
    {
        splinePoints.Clear();
        
        if (pathWaypoints.Length < 2) return;
        
        // Simple cubic spline interpolation
        for (int i = 0; i < pathWaypoints.Length - 1; i++)
        {
            Vector3 p0 = pathWaypoints[Mathf.Max(0, i - 1)].position;
            Vector3 p1 = pathWaypoints[i].position;
            Vector3 p2 = pathWaypoints[i + 1].position;
            Vector3 p3 = pathWaypoints[Mathf.Min(pathWaypoints.Length - 1, i + 2)].position;
            
            // Generate interpolated points
            int segments = Mathf.RoundToInt(Vector3.Distance(p1, p2) / 2f); // 2m resolution
            segments = Mathf.Max(segments, 5); // Minimum 5 points per segment
            
            for (int j = 0; j < segments; j++)
            {
                float t = (float)j / segments;
                Vector3 splinePoint = CatmullRomInterpolation(p0, p1, p2, p3, t);
                splinePoints.Add(splinePoint);
            }
        }
        
        // Add final waypoint
        splinePoints.Add(pathWaypoints[pathWaypoints.Length - 1].position);
    }
    
    Vector3 CatmullRomInterpolation(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        
        Vector3 result = 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
        
        return result;
    }
    
    void UpdateTargetPoint()
    {
        if (frontAxleCenter == null) frontAxleCenter = transform;
        
        Vector3 frontAxlePosition = frontAxleCenter.position;
        
        // Find the closest point on the path
        float closestDistance = float.MaxValue;
        int closestIndex = currentSplineIndex;
        
        // Search in a reasonable range around current index
        int searchRange = Mathf.Min(50, splinePoints.Count);
        int startIndex = Mathf.Max(0, currentSplineIndex - 10);
        int endIndex = Mathf.Min(splinePoints.Count - 1, currentSplineIndex + searchRange);
        
        for (int i = startIndex; i <= endIndex; i++)
        {
            float distance = Vector3.Distance(frontAxlePosition, splinePoints[i]);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        
        currentSplineIndex = closestIndex;
        
        // Find lookahead point
        float accumulatedDistance = 0f;
        int lookaheadIndex = currentSplineIndex;
        
        for (int i = currentSplineIndex; i < splinePoints.Count - 1; i++)
        {
            accumulatedDistance += Vector3.Distance(splinePoints[i], splinePoints[i + 1]);
            if (accumulatedDistance >= lookaheadDistance)
            {
                lookaheadIndex = i + 1;
                break;
            }
        }
        
        lookaheadIndex = Mathf.Min(lookaheadIndex, splinePoints.Count - 1);
        currentTarget = splinePoints[lookaheadIndex];
    }
    
    float CalculateStanleySteeringAngle()
    {
        if (frontAxleCenter == null) return 0f;
        
        Vector3 frontAxlePosition = frontAxleCenter.position;
        Vector3 vehicleForward = transform.forward;
        
        // Calculate cross-track error (distance from front axle to closest point on path)
        float crossTrackError = CalculateCrossTrackError(frontAxlePosition);
        
        // Calculate heading error
        float headingError = CalculateHeadingError();
        
        // Get current velocity
        float velocity = carRigidbody.linearVelocity.magnitude;
        velocity = Mathf.Max(velocity, 0.1f); // Prevent division by zero
        
        // Stanley controller equation: δ = ψ + arctan(k * e / (v + ks))
        float steeringAngle = headingError + Mathf.Atan(controlGain * crossTrackError / (velocity + softeningGain));
        
        // Convert to degrees and clamp
        steeringAngle *= Mathf.Rad2Deg;
        steeringAngle = Mathf.Clamp(steeringAngle, -maxSteeringAngle, maxSteeringAngle);
        
        return steeringAngle;
    }
    
    float CalculateCrossTrackError(Vector3 frontAxlePosition)
    {
        // Find the closest point on the current path segment
        Vector3 closestPoint = splinePoints[currentSplineIndex];
        
        if (currentSplineIndex < splinePoints.Count - 1)
        {
            Vector3 segmentStart = splinePoints[currentSplineIndex];
            Vector3 segmentEnd = splinePoints[currentSplineIndex + 1];
            closestPoint = GetClosestPointOnLine(frontAxlePosition, segmentStart, segmentEnd);
        }
        
        // Calculate signed cross-track error
        Vector3 pathDirection = (currentTarget - closestPoint).normalized;
        Vector3 toVehicle = frontAxlePosition - closestPoint;
        
        // Use cross product to determine sign
        float crossTrackError = Vector3.Distance(frontAxlePosition, closestPoint);
        Vector3 cross = Vector3.Cross(pathDirection, toVehicle.normalized);
        
        if (cross.y < 0) // Vehicle is to the right of the path
        {
            crossTrackError = -crossTrackError;
        }
        
        return crossTrackError;
    }
    
    float CalculateHeadingError()
    {
        // Calculate desired heading (path direction)
        Vector3 pathDirection = Vector3.zero;
        
        if (currentSplineIndex < splinePoints.Count - 1)
        {
            pathDirection = (splinePoints[currentSplineIndex + 1] - splinePoints[currentSplineIndex]).normalized;
        }
        else if (currentSplineIndex > 0)
        {
            pathDirection = (splinePoints[currentSplineIndex] - splinePoints[currentSplineIndex - 1]).normalized;
        }
        else
        {
            pathDirection = (currentTarget - transform.position).normalized;
        }
        
        // Calculate heading error
        float desiredHeading = Mathf.Atan2(pathDirection.x, pathDirection.z) * Mathf.Rad2Deg;
        float currentHeading = transform.eulerAngles.y;
        
        float headingError = Mathf.DeltaAngle(currentHeading, desiredHeading);
        
        // Apply yaw rate gain
        return headingError * yawRateGain * Mathf.Deg2Rad;
    }
    
    Vector3 GetClosestPointOnLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 lineDirection = lineEnd - lineStart;
        float lineLength = lineDirection.magnitude;
        
        if (lineLength < 0.001f)
            return lineStart;
        
        lineDirection.Normalize();
        
        Vector3 toPoint = point - lineStart;
        float dotProduct = Vector3.Dot(toPoint, lineDirection);
        
        dotProduct = Mathf.Clamp(dotProduct, 0f, lineLength);
        
        return lineStart + lineDirection * dotProduct;
    }
    
    void ApplySteeringWithDamping(float targetSteeringAngle)
    {
        // Apply steering damping to smooth out rapid changes
        float dampedSteering = Mathf.Lerp(previousSteeringAngle, targetSteeringAngle, 
                                        1f - steeringDampGain);
        
        currentSteeringAngle = dampedSteering;
        previousSteeringAngle = currentSteeringAngle;
        
        // Apply to steering wheels
        foreach (var axle in wheelAxles)
        {
            if (axle.isSteering)
            {
                axle.leftWheel.steerAngle = currentSteeringAngle;
                axle.rightWheel.steerAngle = currentSteeringAngle;
            }
        }
    }
    
    void ApplyMotorControl()
    {
        // Simple speed control based on path curvature
        float desiredSpeed = CalculateDesiredSpeed();
        float currentSpeed = carRigidbody.linearVelocity.magnitude * 3.6f; // Convert to km/h
        
        float speedError = desiredSpeed - currentSpeed;
        float motorInput = Mathf.Clamp(speedError * 0.1f, -1f, 1f);
        
        // Apply motor torque
        foreach (var axle in wheelAxles)
        {
            if (axle.isMotor)
            {
                float torque = motorInput * maxMotorTorque;
                axle.leftWheel.motorTorque = torque;
                axle.rightWheel.motorTorque = torque;
            }
        }
    }
    
    float CalculateDesiredSpeed()
    {
        // Reduce speed based on path curvature and steering angle
        float steeringFactor = 1f - (Mathf.Abs(currentSteeringAngle) / maxSteeringAngle) * 0.5f;
        return maxSpeed * steeringFactor;
    }
    
    void UpdateWheelVisuals()
    {
        foreach (var axle in wheelAxles)
        {
            UpdateWheelVisual(axle.leftWheel);
            UpdateWheelVisual(axle.rightWheel);
        }
    }
    
    void UpdateWheelVisual(WheelCollider wheel)
    {
        if (wheel.transform.childCount == 0) return;
        
        Transform visualWheel = wheel.transform.GetChild(0);
        Vector3 position;
        Quaternion rotation;
        
        wheel.GetWorldPose(out position, out rotation);
        
        visualWheel.transform.position = position;
        visualWheel.transform.rotation = rotation;
    }
    
    void OnDrawGizmos()
    {
        // Draw spline path
        if (splinePoints.Count > 1)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < splinePoints.Count - 1; i++)
            {
                Gizmos.DrawLine(splinePoints[i], splinePoints[i + 1]);
            }
        }
        
        // Draw current target
        if (currentTarget != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(currentTarget, 2f);
        }
        
        // Draw front axle position
        if (frontAxleCenter != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(frontAxleCenter.position, 1f);
        }
        
        // Draw cross-track error line
        if (Application.isPlaying && splinePoints.Count > 0 && frontAxleCenter != null)
        {
            Vector3 closestPoint = splinePoints[currentSplineIndex];
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(frontAxleCenter.position, closestPoint);
        }
    }
}
