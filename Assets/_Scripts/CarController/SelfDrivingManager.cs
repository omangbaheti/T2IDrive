using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using UnityEngine.Splines;
using Random = UnityEngine.Random;

[RequireComponent(typeof(VehicleController))]
public class SelfDrivingManager : MonoBehaviour
{
    public float SteerInput => steerInput;
    public float BrakeInput => brakeInput;
    public float AcceleratorInput => acceleratorInput;

    [Header("Spline Properties")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private SplineInfo currentSpline;
    [SerializeField] private SplineInfo nextSpline = null;
    
    [SerializeField] private float splineTravelStep = 2f;
    
    [SerializeField] private float splineLerpParam;
    
    private SplineRoad splineRoad;
    private bool isSplineDirectionPositive;
    private bool isFindingSpline;

    [Header("Car Controller Properties")]
    public float maxSpeed = 10f;
    public float maxSteeringAngle = 30f;
    public float distanceThreshold = 0.2f;

    [Header("PID Controllers")] 
    [SerializeField] private PIDController steeringPIDController;
    
    [Header("Stanley Controller Settings")]
    [SerializeField] private float stanleyGain = 1.0f;
    [SerializeField] private float softeningGain = 0.1f;
    [SerializeField] private float headingGain = 0.5f;
    [SerializeField] private float lookaheadDistance = 5f;
    
    [Header("Adaptive Control")]
    [SerializeField] private float minLookahead = 3f;
    [SerializeField] private float maxLookahead = 15f;
    [SerializeField] private float lookaheadSpeedFactor = 0.3f;

    [Header("Vehicle Inputs")]
    [SerializeField] private float steerInput;
    [SerializeField] private float brakeInput;
    [SerializeField] private float acceleratorInput;

    private Vector3 targetPoint;
    private Vector3 directionToTarget;
    private CarInputManager carInputManager;
    private VehicleController vehicleController;
    private float lastSteeringInput;
    
    [Header("Debug Variables")]
    [SerializeField] private float steerAngle;
    [SerializeField] private float steeringLerp = 0f;
    [SerializeField] private float angleToTarget;
    
    private void Start()
    {
        currentSpline = new SplineInfo(splineContainer, currentSpline.index, currentSpline.StartPoint, currentSpline.EndPoint, splineTravelStep);
        vehicleController = GetComponent<VehicleController>();
        splineLerpParam = currentSpline.StartPoint;
        splineRoad = splineContainer.transform.GetComponent<SplineRoad>();
        carInputManager = GetComponent<CarInputManager>();
        steeringPIDController = new PIDController();
        FindNextSpline();
    }
    
    private void UpdateAdaptiveParameters(float speed)
    {
        // Adaptive lookahead distance
        lookaheadDistance = Mathf.Lerp(minLookahead, maxLookahead, speed / maxSpeed * lookaheadSpeedFactor);
    
        // Adaptive Stanley gain (lower at high speeds for stability)
        float adaptiveGain = stanleyGain * (1f - (speed / maxSpeed) * 0.3f);
        stanleyGain = Mathf.Max(adaptiveGain, 0.3f);
    }

    private void FixedUpdate()
    {
        if(!carInputManager.IsSelfDrivingActive) return;
        Debug.Log(currentSpline.index + "----");
        
        Vector3 carPosition = new(transform.position.x, 0f, transform.position.z);
        Vector3 carForward = transform.forward;
        float speed = vehicleController.CurrentSpeed;

        splineContainer.Evaluate(currentSpline.index, splineLerpParam, out float3 position, out float3 forward, out float3 upVector);
        float3 right = Vector3.Cross(forward, Vector3.up).normalized;
        targetPoint = UpdateSplineProgress(transform.position, position, speed);

        if (Vector3.Distance(transform.position, targetPoint) < distanceThreshold) // TODO: Check if car is facing the right way
        {
            Debug.Log("Within Distance");
            // splineLerpParam += splineStep * splineDirection;
            steeringLerp = 0;
        }

        // if ( !(splineLerpParam >= Mathf.Min(splineStartPoint, splineEndPoint) && splineLerpParam <= Mathf.Max(splineStartPoint, splineEndPoint)) )
        // {
        //     lastSteeringInput = steerInput;
        //     if (!isFindingSpline)
        //     {
        //         Debug.Log("Spline Complete, Finding new spline");
        //         FindNextSpline();
        //     }
        //     return;
        // }

        directionToTarget = (targetPoint - carPosition).normalized;
        angleToTarget = Vector3.SignedAngle(carForward, directionToTarget, Vector3.up);
        angleToTarget = Mathf.Clamp(angleToTarget, -maxSteeringAngle, maxSteeringAngle);
        float pidOutput = steeringPIDController.CalculatePIDStep(angleToTarget);
        // Normalize to [-1,1]
        float pidSteerInput = (pidOutput / maxSteeringAngle);
        steerInput = Mathf.Lerp(lastSteeringInput, pidSteerInput, steeringLerp);
        steeringLerp += Time.fixedDeltaTime/3;
        steerInput = Mathf.Clamp(steerInput, -1f, 1f);
        CalculateThrottleAndBrake(speed);
        lastSteeringInput = steerInput;
    }
    
    private float CalculateStanleySteeringAngle(Vector3 carPos, Vector3 carForward, Vector3 pathPoint, Vector3 pathTangent, float velocity)
    {
        // Calculate cross-track error
        Vector3 pathToVehicle = carPos - pathPoint;
        Vector3 pathRight = Vector3.Cross(pathTangent, Vector3.up).normalized;
        float crossTrackError = Vector3.Dot(pathToVehicle, pathRight);
    
        // Calculate heading error
        float desiredHeading = Mathf.Atan2(pathTangent.x, pathTangent.z);
        float currentHeading = Mathf.Atan2(carForward.x, carForward.z);
        float headingError = Mathf.DeltaAngle(currentHeading * Mathf.Rad2Deg, desiredHeading * Mathf.Rad2Deg) * Mathf.Deg2Rad;
    
        // Stanley controller equation: δ = ψ + arctan(k * e / (v + ks))
        velocity = Mathf.Max(velocity, 0.1f); // Prevent division by zero
        float steeringAngle = headingError * headingGain + Mathf.Atan(stanleyGain * crossTrackError / (velocity + softeningGain));
    
        // Convert to degrees and clamp
        steeringAngle *= Mathf.Rad2Deg;
        return Mathf.Clamp(steeringAngle, -maxSteeringAngle, maxSteeringAngle);
    }
    
    
    private void CalculateThrottleAndBrake(float currentSpeed)
    {
        // Calculate desired speed based on path curvature
        float pathCurvature = CalculatePathCurvature();
        float desiredSpeed = CalculateDesiredSpeedForCurvature(pathCurvature);
        Debug.Log($"Desired Speed {desiredSpeed}");
    
        float speedError = desiredSpeed - currentSpeed;
    
        // Separate throttle and brake logic
        if (speedError > 0.5f) // Need to accelerate
        {
            acceleratorInput = Mathf.Clamp01(speedError * 0.2f) * (1f - Mathf.Abs(steerInput) * 0.3f);
            brakeInput = 0f;
        }
        else if (speedError < -1f) // Need to brake
        {
            acceleratorInput = 0f;
            brakeInput = Mathf.Clamp01(-speedError * 0.3f);
        }
        else // Maintain speed
        {
            float throttleReduction = Mathf.Abs(steerInput) * 0.4f;
            acceleratorInput = Mathf.Clamp01(0.3f - throttleReduction);
            brakeInput = 0f;
        }
    }

    private float CalculatePathCurvature()
    {
        // Sample three points to estimate curvature
        float t2 = splineLerpParam;
    
        splineContainer.Evaluate(currentSpline.index, t2, out float3 p2, out _, out _);
        
        float curvature = splineContainer[currentSpline.index].EvaluateCurvature(splineLerpParam);
        Debug.Log("Curvature: " + curvature);
        return curvature;
    }

    private float CalculateDesiredSpeedForCurvature(float curvature)
    {
        // Reduce speed in curves
        float curvatureSpeedFactor = 1f / (1f + curvature * 50f);
        Debug.Log($"curvatureSpeedFactor {curvatureSpeedFactor}");
        float speed = Mathf.Clamp(maxSpeed * curvatureSpeedFactor, 0, maxSpeed);
        return speed;
    }

    private Vector3 UpdateSplineProgress(Vector3 carPosition, Vector3 currentPathPoint, float speed)
    {
        float bestT = splineLerpParam;
        float bestDistance = Vector3.Distance(carPosition, currentPathPoint);
        float splineDirection = isSplineDirectionPositive ? 1 : -1;
        int searchSteps = 20;
        float searchStart = Mathf.Max(currentSpline.StartPoint, splineLerpParam - (currentSpline.splineStep * splineDirection));
        float searchEnd = splineLerpParam + (currentSpline.splineStep * splineDirection);
        Vector3 testPoint = Vector3.zero;
        if (searchEnd < currentSpline.EndPoint || searchEnd > currentSpline.EndPoint)
        {
            if (nextSpline == null || nextSpline == currentSpline)
            {
                FindNextSpline();
            }
            splineContainer.Evaluate(currentSpline.index, currentSpline.EndPoint, out float3 currentEndPos, out float3 fwd, out float3 up);
            splineContainer.Evaluate(nextSpline.index, nextSpline.StartPoint, out float3 newStartPos, out float3 fwd2, out float3 up2);
            
            Vector3 p1 = currentEndPos;
            Vector3 p2 = newStartPos;
            Vector3 mid = (p1 + p2) / 2;
            FindRayIntersection(currentEndPos, fwd, newStartPos, fwd2, out float3 intersection);
            Vector3 p3 = Vector3.Lerp(mid, intersection, 0.3f);
            BezierCurve ad_hoc_bezier = new(p1, p3, p2);
            for (int i = 0; i <= searchSteps; i++)
            {
                float t = Mathf.Lerp(searchStart, searchEnd, (float)i / searchSteps);
                testPoint = CurveUtility.EvaluatePosition(ad_hoc_bezier, t);
                float distance = Vector3.Distance(carPosition, testPoint);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestT = t;
                }
            }

            // splineLerpParam = 1;

        }
        else
        {
            for (int i = 0; i <= searchSteps; i++)
            {
                float t = Mathf.Lerp(searchStart, searchEnd, (float)i / searchSteps);
                splineContainer.Evaluate(currentSpline.index, t, out float3 pos, out float3 fwd, out float3 up);
                float3 right = Vector3.Cross(fwd, Vector3.up).normalized;
                testPoint = pos + (-right * splineRoad.RightWidth/2 * (isSplineDirectionPositive ? 1 : -1));
                float distance = Vector3.Distance(carPosition, testPoint);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestT = t;
                }
            }
            float lookaheadT = lookaheadDistance / splineContainer.Splines[currentSpline.index].GetLength() * splineDirection;
            splineLerpParam = bestT + lookaheadT;
            splineLerpParam = Mathf.Clamp(splineLerpParam, Mathf.Min(currentSpline.StartPoint, currentSpline.EndPoint), Mathf.Max(currentSpline.StartPoint, currentSpline.EndPoint));
        }
        return testPoint;
    }
    
    public static bool FindRayIntersection(float3 currentEndPos, float3 fwd, 
        float3 newStartPos, float3 fwd2, 
        out float3 intersectionPoint)
    {
        // Normalize direction vectors
        float3 d1 = math.normalize(fwd);
        float3 d2 = math.normalize(fwd2);
    
        // Vector between the two starting points
        float3 p21 = newStartPos - currentEndPos;
    
        // Cross product of direction vectors
        float3 d1xd2 = math.cross(d1, d2);
        float crossMagnitude = math.lengthsq(d1xd2);
    
        // Check if rays are parallel
        if (crossMagnitude < 1e-6f)
        {
            intersectionPoint = float3.zero;
            return false; // Rays are parallel, no intersection
        }
    
        // Calculate parameters for intersection
        float t1 = math.dot(math.cross(p21, d2), d1xd2) / crossMagnitude;
        float t2 = math.dot(math.cross(p21, d1), d1xd2) / crossMagnitude;
    
        // Check if intersection is in forward direction for both rays
        if (t1 >= 0 && t2 >= 0)
        {
            // Calculate intersection points on both rays
            float3 point1 = currentEndPos + t1 * d1;
            float3 point2 = newStartPos + t2 * d2;
        
            // Check if points are close enough (for numerical stability)
            if (math.distance(point1, point2) < 1e-4f)
            {
                intersectionPoint = (point1 + point2) * 0.5f;
                return true;
            }
        }
    
        intersectionPoint = float3.zero;
        return false; // No valid intersection in forward directions
    }


    public void FindNextSpline()
    {
        int nextSplineIndex = -1;

        int currentKnotIndex = GetKnotIndexFromT(currentSpline.index, currentSpline.EndPoint);
        Debug.Log($"Current Knot Index:{currentKnotIndex}");
        Intersection currentIntersection = null;

        //looking through intersection and then finding the current one
        //each intersection is made up of multiple spline terminals
        foreach (Intersection intersection in splineRoad.Intersections)
        {
            Debug.Log($"Current spline: {currentSpline.index}, Ends at {currentKnotIndex}");

            //Look through all terminals to verify which spline ended right now
            foreach (SplineTerminalInfo terminal in intersection.Terminals)
            {
                Debug.Log($"----Possible spline: {terminal.splineIndex}, Ends at {terminal.knotIndex}");
                if (terminal.knotIndex == currentKnotIndex && terminal.splineIndex == currentSpline.index)
                {
                    Debug.Log("Found Intersection");
                    currentIntersection = intersection;
                    break;
                }
            }

            if (currentIntersection == null)
            {
                Debug.LogError("No intersection found");
                continue;
            }

            Debug.Log("-------------------Assigning new spline");
            // brute force to make sure the new spline is not the same as where it ended
            int randomInt = Random.Range(0, currentIntersection.Terminals.Count);
            nextSplineIndex = currentIntersection.Terminals[randomInt].splineIndex;
            while (nextSplineIndex == currentSpline.index)
            {
                randomInt = Random.Range(0, currentIntersection.Terminals.Count);
                nextSplineIndex = currentIntersection.Terminals[randomInt].splineIndex;
            }
            Debug.Log($"-----------------Next Spline Index {nextSplineIndex}");
            Assert.AreNotEqual(nextSplineIndex, -1, "The spline was not found");

            //Loop through all terminals to find the spline corresponding to the index above
            foreach (SplineTerminalInfo terminal in currentIntersection.Terminals)
            {
                Debug.Log($"In the for loop {terminal.splineIndex} - {nextSplineIndex}");
                if (terminal.splineIndex == nextSplineIndex)
                {
                    int nextKnotIndex = terminal.knotIndex;
                    int splineStartPoint = nextKnotIndex == 0 ? 0 : 1;
                    int splineEndPoint = nextKnotIndex == 0 ? 1 : 0;
                    nextSpline = new SplineInfo(splineContainer, nextSplineIndex, splineStartPoint, splineEndPoint, splineTravelStep);
                    break;
                }
            }
            break;
        }

        if (currentIntersection == null)
        {
            Debug.LogError("No intersection found Making a U-turn");
            nextSpline = new SplineInfo(splineContainer, nextSplineIndex, startPoint: currentSpline.EndPoint, endPoint: currentSpline.StartPoint, splineTravelStep);
        }
    }

    public int GetKnotIndexFromT(int splineIndex, float T)
    {
        Spline currentSpline = splineContainer.Splines[splineIndex];
        int segmentCount = currentSpline.Count;
        float rawSegmentIndex = T * segmentCount;
        int knotIndex = Mathf.FloorToInt(rawSegmentIndex);
        return Mathf.Clamp(knotIndex, 0, segmentCount-1);
    }

    private void OnDrawGizmos()
    {
        Handles.color = Color.red;
        Handles.SphereHandleCap(0,transform.position, transform.rotation, 0.1f, EventType.Repaint);

        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        // Define direction vectors
        Vector3 forward = rot * Vector3.forward;
        Vector3 up = rot * Vector3.up;
        Vector3 right = rot * Vector3.right;

        float lineLength = 0.3f;

        // Draw forward (blue)
        Handles.color = Color.blue;
        Handles.DrawLine(pos, pos + forward * lineLength);

        // Draw up (green)
        Handles.color = Color.green;
        Handles.DrawLine(pos, pos + up * lineLength);

        // Draw right (red)
        Handles.color = Color.red;
        Handles.DrawLine(pos, pos + right * lineLength);

        Debug.DrawLine(transform.position, transform.position + transform.forward * 5f, Color.red);
        Debug.DrawLine(transform.position, transform.position + directionToTarget.normalized * 10f, Color.green);
        Handles.color = Color.yellow;
        Handles.SphereHandleCap(0, targetPoint, Quaternion.identity, 0.8f, EventType.Repaint);
    }
}


[Serializable]
public class SplineInfo
{
    public int index;
    public float StartPoint;
    public float EndPoint;
    public float splineStep;
    public bool isSplineDirectionPositive;
    public SplineContainer splineContainer;
    public SplineInfo(SplineContainer splineContainer, int index, float startPoint, float endPoint, float splineTravelStep)
    {
        this.splineContainer = splineContainer;
        this.index = index;
        this.StartPoint = startPoint;
        this.EndPoint = endPoint;
        this.splineStep = splineTravelStep / splineContainer.Splines[index].GetLength();;
        this.isSplineDirectionPositive = Mathf.Approximately(startPoint, 0);
    }
}

