using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ubco.ovilab.ViconUnityStream;
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
    public int initialSplineIndex;
    public float splineStartPoint;
    public float splineEndPoint;
    [SerializeField] private float splineTravelStep = 2f;
    [SerializeField] private SplineContainer splineContainer;
    private SplineRoad splineRoad;
    private CarPathManager pathManager;
    private SplinePathData currentSpline;
    private SplinePathData nextSpline;

    //using queue to access previous, current and next point
    private Queue<Vector3> pathPoints = new();

    [Header("Car Controller Properties")]
    public float maxSpeed = 10f;
    public float maxSteeringAngle = 30f;
    public float distanceThreshold = 2f;
    
    [Header("PID Settings")]
    [SerializeField] private float Kp = 0.5f;
    [SerializeField] private float Ki = 0.1f;
    [SerializeField] private float Kd = 0.05f;
    [SerializeField] private float integratorLimit = 1f;
    private PIDController steeringPIDController;
    
    [Header("Stanley Controller Settings")]
    [SerializeField] private float stanleyGain = 1.0f;
    [SerializeField] private float softeningGain = 0.1f;
    [SerializeField] private float headingGain = 0.5f;
    [SerializeField] private float lookaheadDistance = 5f;

    [Header("Vehicle Inputs")]
    [SerializeField] private float steerInput;
    [SerializeField] private float brakeInput;
    [SerializeField] private float acceleratorInput;

    private Vector3 directionToTarget;
    private Vector3 splineTangent;
    private CarInputManager carInputManager;
    private VehicleController vehicleController;
    private float lastSteeringInput;
    
    private  OneEuroFilter filter;
    
    [Header("Debug Variables")]
    [SerializeField] private float steerAngle;
    [SerializeField] private float steeringLerp = 0f;
    [SerializeField] private float angleToTarget;
    
    private void Start()
    {
        vehicleController = GetComponent<VehicleController>();
        pathManager = new CarPathManager(splineContainer);
        splineRoad = splineContainer.GetComponent<SplineRoad>();
        currentSpline = pathManager.SetupNewPath(initialSplineIndex, splineStartPoint, splineEndPoint, splineTravelStep);
        nextSpline = pathManager.SetupNewPath(initialSplineIndex, splineStartPoint, splineEndPoint, splineTravelStep);
        nextSpline.lerpParam += nextSpline.splineStep;
        carInputManager = GetComponent<CarInputManager>();
        steeringPIDController = new PIDController(Kp, Ki, Kd);
        pathPoints.Enqueue(transform.position);
        pathPoints.Enqueue(pathManager.GetPointOnSpline(currentSpline, out float3 _));
        pathPoints.Enqueue(pathManager.GetPointOnSpline(nextSpline, out float3 _));
        filter = new OneEuroFilter(freq:50f, minCutoff: 0.1f, beta: 0.01f);
    }
    private void FixedUpdate()
    {
        if(!carInputManager.IsSelfDrivingActive) return;

        Vector3 carPosition = new(transform.position.x, 0f, transform.position.z);
        Vector3 carForward = transform.forward;
        float speed = vehicleController.CurrentSpeed;
        float splineDirection = currentSpline.isSplineDirectionPositive ? 1 : -1;
        Vector3 targetPoint = pathPoints.ElementAt(1);
        
        if (Vector3.Distance(transform.position, targetPoint) < distanceThreshold) // TODO: Check if car is facing the right way
        {
            Debug.Log("Within Distance");
            currentSpline.lerpParam += currentSpline.splineStep * splineDirection;
            nextSpline.lerpParam += currentSpline.splineStep * splineDirection;
            pathPoints.Dequeue();
            pathPoints.Enqueue(pathManager.GetPointOnSpline(nextSpline, out float3 _));
            steeringLerp = 0;
            Assert.IsFalse(pathPoints.Count == 3, $"Maybe too many points {pathPoints.Count}");
        }
        
        if ( !(nextSpline.lerpParam >= Mathf.Min(nextSpline.startPoint, nextSpline.endPoint) && 
               nextSpline.lerpParam <= Mathf.Max(nextSpline.startPoint, nextSpline.endPoint)))
        {
            nextSpline = pathManager.ChangeSpline(nextSpline);
            Debug.Log("Lookahead Spline Complete, Finding new spline");
        }
        

        if ( !(currentSpline.lerpParam >= Mathf.Min(splineStartPoint, splineEndPoint) && 
               currentSpline.lerpParam <= Mathf.Max(splineStartPoint, splineEndPoint)))
        {
            lastSteeringInput = steerInput;
            currentSpline = pathManager.SetupNewPath(nextSpline.index, nextSpline.startPoint, nextSpline.endPoint, splineTravelStep);
            Debug.Log("Spline Complete, Finding new spline");
            return;
        }

        directionToTarget = (targetPoint - carPosition).normalized;
        angleToTarget = Vector3.SignedAngle(carForward, directionToTarget, Vector3.up);
        angleToTarget = Mathf.Clamp(angleToTarget, -maxSteeringAngle, maxSteeringAngle);
        float pidOutput = steeringPIDController.CalculatePIDStep(angleToTarget);
        // Normalize to [-1,1]
        float pidSteerInput = (pidOutput / maxSteeringAngle);
        pidSteerInput = filter.Filter(pidSteerInput);
        steerInput = Mathf.Lerp(lastSteeringInput, pidSteerInput, steeringLerp);
        steeringLerp += Time.fixedDeltaTime/3;
        steeringLerp %= 1f;
        steerInput = Mathf.Clamp(steerInput, -1f, 1f);
        // acceleratorInput = 1 - Mathf.Abs(steerInput);
        // brakeInput = speed > maxSpeed /3 ? Mathf.Abs(steerInput) : 0;
        // acceleratorInput = speed < maxSpeed ? acceleratorInput : 0f;
        CalculateThrottleAndBrake(speed);
        lastSteeringInput = steerInput;
    }

    public void ToggleDrivingAgent()
    {
        GetComponent<Rigidbody>().isKinematic = carInputManager.IsSelfDrivingActive;
        carInputManager.IsSelfDrivingActive = !carInputManager.IsSelfDrivingActive;
    }
    
    private void CalculateThrottleAndBrake(float currentSpeed)
    {
        // Calculate desired speed based on path curvature
        float pathCurvature = CalculatePathCurvature(transform.position, pathPoints.ElementAt(1), pathPoints.ElementAt(2));
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

    private float CalculatePathCurvature(Vector3 previousPathPoint, Vector3 currentPathPoint, Vector3 nextPathPoint)
    {
        // Calculate curvature using three points
        Vector3 a = currentPathPoint - previousPathPoint;
        Vector3 b = nextPathPoint - currentPathPoint;
        float curvature = Vector3.Cross(a, b).magnitude / (a.magnitude * b.magnitude * (a + b).magnitude);
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

    private void OnDrawGizmos()
    {
        
        if(!Application.isPlaying) return;
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
        
        Handles.color = Color.red;
        Handles.SphereHandleCap(0, pathPoints.ElementAt(0), Quaternion.identity, 0.8f, EventType.Repaint);
        
        Handles.color = Color.green;
        Handles.SphereHandleCap(0, pathPoints.ElementAt(1), Quaternion.identity, 0.8f, EventType.Repaint);
        
        Handles.color = Color.blue;
        Handles.SphereHandleCap(0, pathPoints.ElementAt(2), Quaternion.identity, 0.8f, EventType.Repaint);
    }
}