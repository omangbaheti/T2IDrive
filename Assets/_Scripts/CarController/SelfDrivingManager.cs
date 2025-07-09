using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Splines;
using Random = UnityEngine.Random;

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
    [SerializeField] private SplineRoad splineRoad;

    
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
    private CarInputManager carInputManager;
    
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
        splineRoad = splineContainer.transform.GetComponent<SplineRoad>();
        oneEuroFilter = new(freq:50, minCutoff:0.01f, beta:0.010f, dCutoff:1f);
        carInputManager = GetComponent<CarInputManager>();
    }

    

    private void FixedUpdate()
    {
        if(!carInputManager.IsSelfDrivingActive) return;
        Vector3 carPosition = transform.position;
        carPosition.y = 0f;
        Vector3 carForward = transform.forward;
        
        float speed = vehicleController.CurrentSpeed;
        float lookahead = lookAheadDistance;
        float targetSplinePoint = currentSplineT;
        
        splineContainer.Evaluate(currentSplineIndex, targetSplinePoint, out float3 position, out float3 forward, out float3 upVector);
        targetPoint = position;
        splineTangent = forward;
        
        if (Vector3.Distance(transform.position, targetPoint) < distanceThreshold) // Check if car is facing the right way
        {
            Debug.Log("Within Distance");
            currentSplineT += splineStep;
            steeringLerp = 0;
        }
        
        if ( !(currentSplineT >= Mathf.Min(startDistance, endDistance) && currentSplineT <= Mathf.Max(startDistance, endDistance)) )
        {
            Debug.Log("Spline Complete, Finding new spline");
            acceleratorInput = 0;
            brakeInput = 0.25f;
            ChangeSpline();
            return;
            //Trigger some spline change mechanism
        }

        Vector3 directionToTarget = (targetPoint - carPosition).normalized;
        angleToTarget = Vector3.SignedAngle(carForward, directionToTarget, Vector3.up);
        angleToTarget = Mathf.Clamp(angleToTarget, -maxSteeringAngle, maxSteeringAngle);
        Debug.Log($"Angle: {angleToTarget}");
        if (Mathf.Abs(steerAngle - angleToTarget) < steeringThreshold)
        {
            steerAngle = Mathf.Lerp(steerAngle, 0, steeringLerp);
            steeringLerp += Time.fixedDeltaTime;
            // steerAngle = 0;
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
    
    public void ChangeSpline()
    {
        int nextSplineIndex = -1;
        int nextKnotIndex = -1;

        int currentKnotIndex = GetKnotIndexFromT(currentSplineIndex, endDistance);
        Debug.Log($"Current Knot Index:{currentKnotIndex}");
        foreach (Intersection intersection in splineRoad.Intersections)
        {
            Intersection currentIntersection = null;
            foreach (SplineTerminalInfo terminal in intersection.Terminals)
            {
                Debug.Log($"Current spline: {currentSplineIndex}, Ends at {currentKnotIndex}");
                Debug.Log($"----New spline: {terminal.splineIndex}, Ends at {terminal.knotIndex}");
                if (terminal.knotIndex == currentKnotIndex && terminal.splineIndex == currentSplineIndex)
                {
                    Debug.Log("Found Intersection");
                    currentIntersection = intersection;
                    break;
                }
            }

            if (currentIntersection != null)
            {
                Debug.Log("-------------------Assigning new spline");
                nextSplineIndex = Random.Range(0, currentIntersection.Terminals.Count);
                while (nextSplineIndex == currentSplineIndex)
                {
                    Debug.Log($"{currentSplineIndex} - {nextSplineIndex}");
                    nextSplineIndex = Random.Range(0, currentIntersection.Terminals.Count);
                }
                
                foreach (SplineTerminalInfo terminal in currentIntersection.Terminals)
                {
                    if (terminal.splineIndex == nextSplineIndex)
                    {
                        currentSplineIndex = nextSplineIndex;
                        nextKnotIndex = terminal.knotIndex;
                    }
                }
                break;
            }
        }
        
        startDistance = nextKnotIndex == 0 ? 0 : 1;
        endDistance = nextKnotIndex == 0 ? 1 : 0;

        if (Mathf.Approximately(startDistance, 1))
        {
            splineStep = -Mathf.Abs(splineStep);
        }
        else
        {
            splineStep = Mathf.Abs(splineStep);
        }
        
        currentSplineT = startDistance;
        currentSplineIndex = nextSplineIndex;
        brakeInput = 0;
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
    }
}