using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Splines;

[RequireComponent(typeof(VehicleController))]
public class SelfDrivingManager : MonoBehaviour
{
    public float SteerInput => steerInput;
    public float BrakeInput => brakeInput;
    public float AcceleratorInput => acceleratorInput;
   
    [Header("Splines")]
    public int currentSplineIndex;
    public float startDistance;
    public float endDistance;
    [SerializeField] private SplineContainer splineContainer;

    
    [Header("Car Controller Properties")]
    public float maxSpeed = 10f;
    public float brakeThreshold = 1.1f;
    public float maxSteeringAngle = 30f;
    

    public float distanceThreshold = 0.2f;
    public float steeringThreshold = 4f;
    
    [SerializeField] private float steerInput;
    [SerializeField] private float brakeInput;
    [SerializeField] private float acceleratorInput;
    [SerializeField] private float splineStep = 0.05f;
    [SerializeField] private float currentSplineT;

    private VehicleController vehicleController;
    private Vector3 targetPoint;
    private Vector3 directionToTarget;
    private Vector3 splineTangent;
    
    [Header("Debug Variables")] [SerializeField]
    private float steerAngle;
    [SerializeField] private float steeringLerp = 0f;
    [SerializeField] private float angleToTarget;
    
    private OneEuroFilter oneEuroFilter;
    [SerializeField] private float lookAheadDistance;

    private void Start()
    {
        vehicleController = GetComponent<VehicleController>();
        currentSplineT = startDistance;
        oneEuroFilter = new(freq:50, minCutoff:0.01f, beta:0.010f, dCutoff:1f);
    }

    public void ChangeSpline(int index)
    {
        currentSplineIndex = index;
    }

    private void FixedUpdate()
    {
        Vector3 carPosition = transform.position;
        Vector3 carForward = transform.forward;
        
        float speed = vehicleController.CurrentSpeed;
        float lookahead = lookAheadDistance;
        float targetSplinePoint = currentSplineT + lookahead;
        
        splineContainer.Evaluate(currentSplineIndex, targetSplinePoint, out float3 position, out float3 forward, out float3 upVector);
        targetPoint = position;
        splineTangent = forward;
        
        if (Vector3.Distance(transform.position, targetPoint) < distanceThreshold) // Check if car is facing the right way
        {
            Debug.Log("Within Distance");
            currentSplineT += splineStep;
            steeringLerp = 0;
            
            if (currentSplineT >= 1)
            {
                Debug.Log("Spline Complete, Finding new spline");
                acceleratorInput = 0;
                return;
                //Trigger some spline change mechanism
            }
        }

        Vector3 directionToTarget = (targetPoint - transform.position).normalized;
        angleToTarget = Vector3.SignedAngle(carForward, directionToTarget, Vector3.up);
        angleToTarget = Mathf.Clamp(angleToTarget, -maxSteeringAngle, maxSteeringAngle);
        if (Mathf.Abs(steerAngle - angleToTarget) < steeringThreshold)
        {
            steerAngle = Mathf.Lerp(steerAngle, 0, steeringLerp);
            steeringLerp += Time.fixedDeltaTime;
        }
        else
        {
            if (angleToTarget > (maxSteeringAngle - maxSteeringAngle/3))
            {
                steerAngle = Mathf.Lerp(steerAngle, angleToTarget, steeringLerp);
                steeringLerp += Time.fixedDeltaTime * 2;
            }
            else
            {
                steerAngle = Mathf.Lerp(steerAngle, angleToTarget, steeringLerp);
                steeringLerp += Time.fixedDeltaTime;
            }
        }

        steerAngle = oneEuroFilter.Filter(steerAngle);
        steerInput = steerAngle / maxSteeringAngle;
        // Debug.Log($"Before:{steerAngle / maxSteeringAngle} After:{steerInput}");
        acceleratorInput = speed < maxSpeed ? 1f : 0f;
    }

    // private void FixedUpdate()
    // {
    //     Vector3 carPosition = transform.position;
    //     Vector3 carForward = transform.forward;
    //     float speed = vehicleController.CurrentSpeed;
    //     
    //     float lookahead = lookAheadDistance + speed * speedLookAheadFactor;
    //     float targetSplinePoint = currentSplineDistance + lookahead;
    //     splineContainer.Evaluate(currentSpline, targetSplinePoint, out float3 position, out float3 forward, out float3 upVector);
    //     
    //     targetPoint = position;
    //     splineTangent = forward;
    //     directionToTarget = (targetPoint - carPosition).normalized;
    //     
    //     steerAngle = Vector3.SignedAngle(carForward, directionToTarget, Vector3.up) / maxSteeringAngle; // Normalize to -1..1
    //     steerInput = Mathf.Clamp(steerAngle, -1f, 1f) * 0.5f;
    //     steerInput += 1f;
    //     float desiredSpeed = maxSpeed;
    //     acceleratorInput = speed < desiredSpeed ? 1f : 0f;
    //     brakeInput = speed > desiredSpeed * brakeThreshold ? 1f : 0f;
    //     
    //     float forwardDot = Vector3.Dot(carForward, splineTangent.normalized);
    //     if ((targetPoint - carPosition).magnitude < 0.1f && forwardDot > 0.8f)
    //     {
    //         currentSplineDistance += 0.05f;
    //     }
    //     
    // }

    private void OnDrawGizmos()
    {
        Debug.DrawLine(transform.position, transform.position + transform.forward * 5f, Color.red);
        Debug.DrawLine(transform.position, transform.position + directionToTarget.normalized * 5f, Color.green);
        Handles.color = Color.yellow;
        Handles.SphereHandleCap(0, targetPoint, Quaternion.identity, 0.8f, EventType.Repaint);
    }
}