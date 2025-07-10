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
    public int currentSplineIndex;
    [FormerlySerializedAs("startDistance")] public float splineStartPoint;
    [FormerlySerializedAs("endDistance")] public float splineEndPoint;
    [SerializeField] private float splineTravelStep = 2f;
    [SerializeField] private SplineContainer splineContainer;
    private SplineRoad splineRoad;
    private bool isSplineDirectionPositive;
    [SerializeField] private float splineLerpParam;
    [SerializeField] private float splineStep;

    [Header("Car Controller Properties")]
    public float maxSpeed = 10f;
    public float brakeThreshold = 1.1f;
    public float maxSteeringAngle = 30f;
    public float distanceThreshold = 0.2f;
    public float steeringThreshold = 4f;

    [Header("Vehicle Inputs")]
    [SerializeField] private float steerInput;
    [SerializeField] private float brakeInput;
    [SerializeField] private float acceleratorInput;

    private Vector3 targetPoint;
    private Vector3 directionToTarget;
    private Vector3 splineTangent;
    private CarInputManager carInputManager;
    private VehicleController vehicleController;

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
        SetupNewSpline(currentSplineIndex, splineStartPoint, splineEndPoint);
    }

    private void SetupNewSpline(int _splineIndex, float _startPoint, float _endPoint)
    {
        Debug.Log("Setting Up new Spline");
        currentSplineIndex = _splineIndex;
        splineStartPoint = _startPoint;
        splineEndPoint = _endPoint;
        isSplineDirectionPositive = Mathf.Approximately(splineStartPoint, 0);
        splineStep = splineTravelStep / splineContainer.Splines[currentSplineIndex].GetLength();
        splineLerpParam = splineStartPoint;
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
            Debug.Log("Spline Complete, Finding new spline");
            ChangeSpline();
            return;
        }

        directionToTarget = (targetPoint - carPosition).normalized;
        angleToTarget = Vector3.SignedAngle(carForward, directionToTarget, Vector3.up);
        angleToTarget = Mathf.Clamp(angleToTarget, -maxSteeringAngle, maxSteeringAngle);
        Debug.Log($"Angle: {angleToTarget}");

        //If angle to target is below threshold, stop steering
        angleToTarget = Mathf.Abs(steerAngle - angleToTarget) < steeringThreshold ? 0 : angleToTarget;
        steerAngle = Mathf.Lerp(steerAngle, angleToTarget, steeringLerp);
        steerAngle = oneEuroFilter.Filter(steerAngle);

        steeringLerp += Time.fixedDeltaTime/3;
        steerInput = steerAngle / (maxSteeringAngle);

        acceleratorInput = 1 - Mathf.Abs(steerInput);
        brakeInput = speed > maxSpeed /3 ? Mathf.Abs(steerInput) : 0;
        acceleratorInput = speed < maxSpeed ? acceleratorInput : 0f;
    }

    public void ChangeSpline()
    {
        int nextSplineIndex = -1;

        int currentKnotIndex = GetKnotIndexFromT(currentSplineIndex, splineEndPoint);
        Debug.Log($"Current Knot Index:{currentKnotIndex}");

        //looking through intersection and then finding the current one
        //each intersection is made up of multiple spline terminals
        foreach (Intersection intersection in splineRoad.Intersections)
        {
            Intersection currentIntersection = null;
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
            nextSplineIndex = Random.Range(0, currentIntersection.Terminals.Count);
            while (nextSplineIndex == currentSplineIndex)
            {
                nextSplineIndex = Random.Range(0, currentIntersection.Terminals.Count);
            }
            Debug.Log($"-----------------Next Spline Index {nextSplineIndex}");
            Assert.AreNotEqual(nextSplineIndex, -1, "The spline was not found");
            //Loop through all terminals to find the spline corresponding to the index above
            foreach (SplineTerminalInfo terminal in currentIntersection.Terminals)
            {
                if (terminal.splineIndex == nextSplineIndex)
                {
                    int nextKnotIndex = terminal.knotIndex;
                    splineStartPoint = nextKnotIndex == 0 ? 0 : 1;
                    splineEndPoint = nextKnotIndex == 0 ? 1 : 0;
                    SetupNewSpline(nextSplineIndex, splineStartPoint, splineEndPoint);
                }
            }
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
    }
}