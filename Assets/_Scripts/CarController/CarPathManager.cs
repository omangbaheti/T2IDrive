using System;
using System.Collections;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Random = UnityEngine.Random;

public class SplinePathData
{
    public int index;
    public float startPoint;
    public float endPoint;
    public bool isSplineDirectionPositive;
    public float splineSamplingResolution;
    public float splineStep;
    public float lerpParam;
}


[Serializable]
public class CarPathManager
{
    [SerializeField] private SplineContainer splineContainer;
    private SplineRoad splineRoad;


    public CarPathManager(SplineContainer splineContainer)
    {
        this.splineContainer = splineContainer;
        splineRoad = splineContainer.transform.GetComponent<SplineRoad>();
    }

    public SplinePathData SetupNewPath(int _splineIndex, float _startPoint, float _endPoint, float samplingRes)
    {
        Debug.Log("Setting Up new Spline");
        SplinePathData splineData = new()
        {
            index = _splineIndex,
            startPoint = _startPoint,
            endPoint = _endPoint,
            isSplineDirectionPositive = Mathf.Approximately(_startPoint, 0),
            splineSamplingResolution = samplingRes,
            splineStep = samplingRes / splineContainer.Splines[_splineIndex].GetLength(),
            lerpParam = _startPoint
        };
        return splineData;
    }

    public Vector3 GetPointOnSpline(SplinePathData _splineData, out float3 tangent)
    {
        float3 position, forward, upVector;
        float splineDirection = _splineData.isSplineDirectionPositive ? 1 : -1;
        splineContainer.Evaluate(_splineData.index, _splineData.lerpParam, out position, out forward, out upVector);
        float3 right = Vector3.Cross(forward, Vector3.up).normalized;
        tangent = forward;
        Vector3 targetPoint = position + (-right * splineRoad.RightWidth/2 * splineDirection);
        return targetPoint;
    }

    public SplinePathData ChangeSpline(SplinePathData _splineData)
    {
        int nextSplineIndex = -1;

        int currentKnotIndex = GetKnotIndexFromT(_splineData.index, _splineData.endPoint);
        Debug.Log($"Current Knot Index:{currentKnotIndex}");
        Intersection currentIntersection = null;

        //looking through intersection and then finding the current one
        //each intersection is made up of multiple spline terminals
        foreach (Intersection intersection in splineRoad.Intersections)
        {
            Debug.Log($"Current spline: {_splineData.index}, Ends at {currentKnotIndex}");

            //Look through all terminals to verify which spline ended right now
            foreach (SplineTerminalInfo terminal in intersection.Terminals)
            {
                Debug.Log($"----Possible spline: {terminal.splineIndex}, Ends at {terminal.knotIndex}");
                if (terminal.knotIndex == currentKnotIndex && terminal.splineIndex == _splineData.index)
                {
                    Debug.Log("Found Intersection");
                    currentIntersection = intersection;
                    break;
                }
            }

            if (currentIntersection == null)
            {
                // Debug.LogError("No intersection found");
                continue;
            }

            Debug.Log("-------------------Assigning new spline");
            // brute force to make sure the new spline is not the same as where it ended
            int randomInt = Random.Range(0, currentIntersection.Terminals.Count);
            nextSplineIndex = currentIntersection.Terminals[randomInt].splineIndex;
            while (nextSplineIndex == _splineData.index)
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
                    float startPoint = nextKnotIndex == 0 ? 0 : 1;
                    float endPoint = nextKnotIndex == 0 ? 1 : 0;
                    return SetupNewPath(nextSplineIndex, startPoint, endPoint, _splineData.splineSamplingResolution);
                }
            }
        }

        Debug.LogWarning("No intersection found Making a U-turn");
        return SetupNewPath(_splineData.index, _startPoint: _splineData.endPoint, _endPoint: _splineData.startPoint, _splineData.splineSamplingResolution);
    }

    public int GetKnotIndexFromT(int splineIndex, float T)
    {
        Spline currentSpline = splineContainer.Splines[splineIndex];
        int segmentCount = currentSpline.Count;
        float rawSegmentIndex = T * segmentCount;
        int knotIndex = Mathf.FloorToInt(rawSegmentIndex);
        return Mathf.Clamp(knotIndex, 0, segmentCount-1);
    }
}