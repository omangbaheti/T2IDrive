using System;
using System.Collections;
using System.Collections.Generic;
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
    public int currentSplineIndex;
    [FormerlySerializedAs("startDistance")] public float splineStartPoint;
    [FormerlySerializedAs("endDistance")] public float splineEndPoint;
    [SerializeField] private float splineTravelStep = 2f;
    [SerializeField] private SplineContainer splineContainer;
    private SplineRoad splineRoad;
    private bool isSplineDirectionPositive;
    private bool isFindingSpline;
    [SerializeField] private float splineLerpParam;
    [SerializeField] private float splineStep;

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

    [Header("Vehicle Inputs")]
    [SerializeField] private float steerInput;
    [SerializeField] private float brakeInput;
    [SerializeField] private float acceleratorInput;

    private Vector3 targetPoint;
    private Vector3 directionToTarget;
    private Vector3 splineTangent;
    private CarInputManager carInputManager;
    private VehicleController vehicleController;
    private float lastSteeringInput;
    [Header("Debug Variables")]
    [SerializeField] private float steerAngle;
    [SerializeField] private float steeringLerp = 0f;
    [SerializeField] private float angleToTarget;

    private OneEuroFilter oneEuroFilter;

    private void Start()
    {
        vehicleController = GetComponent<VehicleController>();
        splineLerpParam = splineStartPoint;
        splineRoad = splineContainer.transform.GetComponent<SplineRoad>();
        oneEuroFilter = new(freq:50, minCutoff:0.01f, beta:0.010f, dCutoff:1f);
        carInputManager = GetComponent<CarInputManager>();
        StartCoroutine(SetupNewSpline(currentSplineIndex, splineStartPoint, splineEndPoint, 0f));
        steeringPIDController = new PIDController();
    }

    private IEnumerator SetupNewSpline(int _splineIndex, float _startPoint, float _endPoint, float delay)
    {
        isFindingSpline = true;
        yield return new WaitForSeconds(delay);
        Debug.Log("Setting Up new Spline");
        currentSplineIndex = _splineIndex;
        splineStartPoint = _startPoint;
        splineEndPoint = _endPoint;
        isSplineDirectionPositive = Mathf.Approximately(splineStartPoint, 0);
        splineStep = splineTravelStep / splineContainer.Splines[currentSplineIndex].GetLength();
        splineLerpParam = splineStartPoint;
        isFindingSpline = false;
    }

    private void FixedUpdate()
    {
        if(!carInputManager.IsSelfDrivingActive) return;

        Vector3 carPosition = new(transform.position.x, 0f, transform.position.z);
        Vector3 carForward = transform.forward;
        float speed = vehicleController.CurrentSpeed;
        float splineDirection = isSplineDirectionPositive ? 1 : -1;

        splineContainer.Evaluate(currentSplineIndex, splineLerpParam, out float3 position, out float3 forward, out float3 upVector);
        float3 right = Vector3.Cross(forward, Vector3.up).normalized;
        targetPoint = position + (-right * splineRoad.RightWidth/2 * splineDirection );
        splineTangent = forward;

        if (Vector3.Distance(transform.position, targetPoint) < distanceThreshold) // TODO: Check if car is facing the right way
        {
            Debug.Log("Within Distance");
            splineLerpParam += splineStep * splineDirection;
            steeringLerp = 0;
        }

        if ( !(splineLerpParam >= Mathf.Min(splineStartPoint, splineEndPoint) && splineLerpParam <= Mathf.Max(splineStartPoint, splineEndPoint)) )
        {
            lastSteeringInput = steerInput;
            if (!isFindingSpline)
            {
                Debug.Log("Spline Complete, Finding new spline");
                ChangeSpline();
            }
            return;
        }

        directionToTarget = (targetPoint - carPosition).normalized;
        angleToTarget = Vector3.SignedAngle(carForward, directionToTarget, Vector3.up);
        angleToTarget = Mathf.Clamp(angleToTarget, -maxSteeringAngle, maxSteeringAngle);
        // Debug.Log($"Angle: {angleToTarget}");
        // // PID calculations
        // float error = angleToTarget;
        // float deltaTime = Time.fixedDeltaTime;
        // integral += error * deltaTime;
        // // Clamp integral to avoid windup
        // integral = Mathf.Clamp(integral, -integratorLimit, integratorLimit);
        // float derivative = (error - lastError) / deltaTime;
        // float pidOutput = Kp * error + Ki * integral + Kd * derivative;
        // lastError = error;
        float pidOutput = steeringPIDController.CalculatePIDStep(angleToTarget);
        // Normalize to [-1,1]
        float pidSteerInput = (pidOutput / maxSteeringAngle);
        steerInput = Mathf.Lerp(lastSteeringInput, pidSteerInput, steeringLerp);
        steeringLerp += Time.fixedDeltaTime/3;
        steerInput = Mathf.Clamp(steerInput, -1f, 1f);
        // acceleratorInput = 1 - Mathf.Abs(steerInput);
        // brakeInput = speed > maxSpeed /3 ? Mathf.Abs(steerInput) : 0;
        // acceleratorInput = speed < maxSpeed ? acceleratorInput : 0f;
        CalculateThrottleAndBrake(speed);
        lastSteeringInput = steerInput;
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
    
        splineContainer.Evaluate(currentSplineIndex, t2, out float3 p2, out _, out _);
        
        float curvature = splineContainer[currentSplineIndex].EvaluateCurvature(splineLerpParam);
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


    public void ChangeSpline()
    {
        int nextSplineIndex = -1;

        int currentKnotIndex = GetKnotIndexFromT(currentSplineIndex, splineEndPoint);
        Debug.Log($"Current Knot Index:{currentKnotIndex}");
        Intersection currentIntersection = null;

        //looking through intersection and then finding the current one
        //each intersection is made up of multiple spline terminals
        foreach (Intersection intersection in splineRoad.Intersections)
        {
            Debug.Log($"Current spline: {currentSplineIndex}, Ends at {currentKnotIndex}");

            //Look through all terminals to verify which spline ended right now
            foreach (SplineTerminalInfo terminal in intersection.Terminals)
            {
                Debug.Log($"----Possible spline: {terminal.splineIndex}, Ends at {terminal.knotIndex}");
                if (terminal.knotIndex == currentKnotIndex && terminal.splineIndex == currentSplineIndex)
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
            while (nextSplineIndex == currentSplineIndex)
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
                    splineStartPoint = nextKnotIndex == 0 ? 0 : 1;
                    splineEndPoint = nextKnotIndex == 0 ? 1 : 0;
                    StartCoroutine(SetupNewSpline(nextSplineIndex, splineStartPoint, splineEndPoint,1f));
                    break;
                }
            }
            break;
        }

        if (currentIntersection == null)
        {
            Debug.LogError("No intersection found Making a U-turn");
            StartCoroutine(SetupNewSpline(currentSplineIndex, _startPoint:splineEndPoint, _endPoint:splineStartPoint, 1f));
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