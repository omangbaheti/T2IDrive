using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;using Unity.VisualScripting.FullSerializer;
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
    public float splineStartKnot;
    public float splineEndKnot;
    public int lookAheadRange;
    
    
    [SerializeField] private float splineTravelStep = 2f;
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private float splineLerpParam;
    [SerializeField] private float splineStep;
    
    private SplineRoad splineRoad;
    private Vector3 lookAheadPoint;

    private SplineInfo currentSpline;
    private List<SplineInfo> nextSplines = new();
    
    [Header("Car Controller Properties")]
    public float maxSpeed = 10f;
    public float cornerSpeedFactor = 0.3f;
    public float maxSteeringAngle = 30f;
    public float distanceThreshold = 0.2f;
    
    [Header("PID Steering Settings")]
    [SerializeField] private float Kp = 0.5f;
    [SerializeField] private float Ki = 0.1f;
    [SerializeField] private float Kd = 0.05f;
    [SerializeField] private float integratorLimit = 1f;
    private float integral;
    private float lastError;

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

    private void Awake()
    {
        currentSpline = new SplineInfo(currentSplineIndex,splineStartKnot, splineEndKnot, splineContainer, splineTravelStep);
        nextSplines = new List<SplineInfo>();
    }

    private void Start()
    {
        vehicleController = GetComponent<VehicleController>();
        splineLerpParam = splineStartKnot;
        splineRoad = splineContainer.transform.GetComponent<SplineRoad>();
        oneEuroFilter = new(freq:50, minCutoff:0.01f, beta:0.010f, dCutoff:1f);
        carInputManager = GetComponent<CarInputManager>();
    }

    private void FixedUpdate()
    {
        if(!carInputManager.IsSelfDrivingActive) return;
        
        
        lookAheadStep = lookAheadRange / splineContainer.Splines[currentSplineIndex].GetLength() * maxSpeed;
        
        Vector3 carPosition = new(transform.position.x, 0f, transform.position.z);
        Vector3 carForward = transform.forward;
        float speed = vehicleController.CurrentSpeed;
        float splineDirection = currentSpline.IsSplineDirectionPositive ? 1 : -1;

        splineContainer.Evaluate(currentSplineIndex, splineLerpParam, out float3 target, out float3 forward, out float3 upVector);
        float3 right = Vector3.Cross(forward, Vector3.up).normalized;
        targetPoint = target + (-right * splineRoad.RightWidth/2 * splineDirection );
        splineTangent = forward;

        float lookAheadLerpParam = splineLerpParam + lookAheadStep;
        if (lookAheadLerpParam >= Mathf.Min(splineStartKnot, splineEndKnot) && lookAheadLerpParam <= Mathf.Max(splineStartKnot, splineEndKnot))
        {
            FindNextSpline();
        }
        
        splineContainer.Evaluate(currentSplineIndex, lookAheadLerpParam, out float3 lookAheadPoint, out forward, out upVector);
        right = Vector3.Cross(forward, Vector3.up).normalized;
        this.lookAheadPoint = lookAheadPoint + (-right * splineRoad.RightWidth / 2 * splineDirection);
        

        if (Vector3.Distance(transform.position, targetPoint) < distanceThreshold) // TODO: Check if car is facing the right way
        {
            splineLerpParam += splineStep * splineDirection;
            steeringLerp = 0;
        }

        if ( !(splineLerpParam >= Mathf.Min(splineStartKnot, splineEndKnot) && splineLerpParam <= Mathf.Max(splineStartKnot, splineEndKnot)) )
        {
            lastSteeringInput = steerInput;
            currentSpline = 
            StartCoroutine(SetupNewSpline(nextSplineIndex, nextSplineStartKnot, nextSplineEndKnot,1f));
            return;
        }

        directionToTarget = (targetPoint - carPosition).normalized;
        angleToTarget = Vector3.SignedAngle(carForward, directionToTarget, Vector3.up);
        angleToTarget = Mathf.Clamp(angleToTarget, -maxSteeringAngle, maxSteeringAngle);
        Debug.Log($"Angle: {angleToTarget}");

        // PID calculations
        float error = angleToTarget;
        float deltaTime = Time.fixedDeltaTime;
        integral += error * deltaTime;
        // Clamp integral to avoid windup
        integral = Mathf.Clamp(integral, -integratorLimit, integratorLimit);
        float derivative = (error - lastError) / deltaTime;
        float pidOutput = Kp * error + Ki * integral + Kd * derivative;
        lastError = error;

        // Normalize to [-1,1]
        float pidSteerInput = (pidOutput / maxSteeringAngle);
        steerInput = Mathf.Lerp(lastSteeringInput, pidSteerInput, steeringLerp);
        steeringLerp += Time.fixedDeltaTime/3;
        steerInput = Mathf.Clamp(steerInput, -1f, 1f);
        acceleratorInput = 1 - Mathf.Abs(steerInput);
        brakeInput = speed > maxSpeed /3 ? Mathf.Abs(steerInput) : 0;
        acceleratorInput = speed < maxSpeed ? acceleratorInput : 0f;
        lastSteeringInput = steerInput;
    }

    public void FindNextSpline()
    {
        nextSplineIndex = -1;

        int currentEndKnotIndex = GetKnotIndexFromT(currentSplineIndex, splineEndKnot);
        Debug.Log($"Current Knot Index:{currentEndKnotIndex}");
        Intersection currentIntersection = null;

        //looking through intersection and then finding the current one
        //each intersection is made up of multiple spline terminals
        foreach (Intersection intersection in splineRoad.Intersections)
        {
            // Debug.Log($"Current spline: {currentSplineIndex}, Ends at {currentKnotIndex}");
            //Look through all terminals to verify which spline ended right now
            if (intersection.Terminals.Any(terminal => terminal.knotIndex == currentEndKnotIndex && 
                                                       terminal.splineIndex == currentSplineIndex))
            {
                currentIntersection = intersection;
            }
        }

        if (currentIntersection == null)
        {
            Debug.LogError("No intersection found");
            StartCoroutine(SetupNewSpline(currentSplineIndex, _startPoint:splineEndKnot, _endPoint:splineStartKnot, 1f));
            return;
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
            if (terminal.splineIndex != nextSplineIndex) continue;
            
            nextSplineStartKnot = terminal.knotIndex;
            nextSplineEndKnot = nextSplineStartKnot == 0 ? 1 : 0;
            break;
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
        Debug.DrawLine(transform.position, transform.position + transform.forward * 5f, Color.red);
        Debug.DrawLine(transform.position, transform.position + directionToTarget.normalized * 10f, Color.green);
        Handles.color = Color.yellow;
        Handles.SphereHandleCap(0, targetPoint, Quaternion.identity, 0.8f, EventType.Repaint);
        Handles.color = Color.cyan;
        Handles.SphereHandleCap(0, lookAheadPoint, Quaternion.identity, 0.8f, EventType.Repaint);
    }
}

[Serializable]
public class SplineInfo
{
    public int SplineIndex => splineIndex;
    public float SplineStep => splineStep;
    public int SplineStartKnot => splineStartKnot;
    public int SplineEndKnot => splineEndKnot;
    public bool IsSplineDirectionPositive => isSplineDirectionPositive;
    
    public float splineStep;
    public int splineLerpParam;
    
    [SerializeField] private int splineIndex;
    [SerializeField] private int splineStartKnot;
    [SerializeField] private int splineEndKnot;
    private bool isSplineDirectionPositive;
    public SplineInfo(int _splineIndex, float _startPoint, float _endPoint, SplineContainer splineContainer, float splineTravelStep)
    {
        splineIndex = _splineIndex;
        splineStartKnot = (int) _startPoint;
        splineEndKnot = (int) _endPoint;
        isSplineDirectionPositive = Mathf.Approximately(splineStartKnot, 0);
        splineStep = splineTravelStep / splineContainer.Splines[splineIndex].GetLength();
        splineLerpParam = splineStartKnot;
    }
}