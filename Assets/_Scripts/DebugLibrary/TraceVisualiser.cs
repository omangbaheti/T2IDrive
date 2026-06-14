using System;
using System.Collections.Generic;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;
using EditorAttributes;
using ubco.ovilab.HPUI.Core;


public class TraceVisualiser : MonoBehaviour
{
    [SerializeField] private bool debugMode;
    [SerializeField] private GameObject debugPointPrefab;
    [SerializeField] private List<GameObject> sphereObjects;
    [SerializeField] private List<GameObject> tempCache;

    [SerializeField] private bool enableColor1;
    [SerializeField] private List<GameObject> color1;

    [SerializeField] private bool enableColor2;
    [SerializeField] private List<GameObject> color2;

    [SerializeField] private bool enableColor3;
    [SerializeField] private List<GameObject> color3;

    [SerializeField] private bool enableColor4;
    [SerializeField] private List<GameObject> color4;


    private Quaternion directionAdjustment = Quaternion.Euler(0,0,90f);
    private HPUIMultiFingerCanvas target;
    private int gestureIndex = 0;
    [SerializeField] List<Vector2> currentPoints = new List<Vector2>();
    public Vector2 currentPosition { get; set; }

    private void Awake()
    {
        if (target == null)
        {
            target = GetComponent<HPUIMultiFingerCanvas>();
        }

        target.OnCanvasInteractions.AddListener(HandleGesture);
    }

    private void OnDestroy()
    {
        target.OnCanvasInteractions.RemoveListener(HandleGesture);
    }

    public void HandleGesture(HPUIGestureEventArgs args, HPUICanvasEventArgs hpuiCanvasEventArgs)
    {
        if (!debugMode || hpuiCanvasEventArgs.State == HPUICanvasState.NotStarted)
        {
            return;
        }
        Color debugColor = (gestureIndex % 4) switch
        {
            0 => Color.cyan,
            1 => Color.green,
            2 => Color.red,
            3 => Color.yellow,
            _ => Color.magenta
        };
        if (hpuiCanvasEventArgs.State == HPUICanvasState.Started)
        {
            ClearPoints();
        }
        if (hpuiCanvasEventArgs.State == HPUICanvasState.Cancelled)
        {
            foreach (GameObject sphere in tempCache)
            {
                Destroy(sphere);
            }
            tempCache.Clear();
            return;
        }
        Vector2 processedTouchPos = hpuiCanvasEventArgs.GesturePositions[^1];
        Vector2Int colliderVal = ComputeDebugPointPosition(processedTouchPos,target, out Vector2 localPos);
        currentPosition = colliderVal + localPos;
        GameObject tracePoint;
        if (target.coordsToCollider.ContainsKey(colliderVal))
        {
            tracePoint = Instantiate(debugPointPrefab, target.coordsToCollider[colliderVal].transform, true);
            tempCache.Add(tracePoint);

            tracePoint.transform.position = transform.parent.position;
            tracePoint.transform.localScale = Vector3.one;
            tracePoint.transform.localRotation = Quaternion.Euler(90, 0, 0);
            tracePoint.transform.localPosition = new Vector3(localPos.x, 75f, localPos.y);
            tracePoint.transform.name = "Green" + sphereObjects.Count;
        }


        if (hpuiCanvasEventArgs.State == HPUICanvasState.Completed)
        {
            foreach (GameObject sphere in tempCache)
            {
                Destroy(sphere);
            }
            tempCache.Clear();
            foreach (Vector2 point in hpuiCanvasEventArgs.GesturePositions)
            {
                colliderVal = ComputeDebugPointPosition(point, target, out localPos);
                tracePoint = Instantiate(debugPointPrefab);
                sphereObjects.Add(tracePoint);
                tempCache.Add(tracePoint);

                if (target.coordsToCollider.ContainsKey(colliderVal))
                {

                    tracePoint.transform.parent = target.coordsToCollider[colliderVal].transform;
                    tracePoint.transform.position = transform.parent.position;
                    tracePoint.transform.localScale = Vector3.one;
                    tracePoint.transform.localPosition = new Vector3(localPos.x, 50f, localPos.y);
                    tracePoint.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    tracePoint.transform.name = "DebugPoint" + sphereObjects.Count;

                    switch (gestureIndex)
                    {
                        case 0:
                            color1.Add(tracePoint);
                            break;
                        case 1:
                            color2.Add(tracePoint);
                            break;
                        case 2:
                            color3.Add(tracePoint);
                            break;
                        case 3:
                            color4.Add(tracePoint);
                            break;
                    }
                }
            }

            int index = 0;
            foreach (GameObject tempSphere in tempCache)
            {
                Color setColor = Color.Lerp(debugColor, Color.black, index / (float)tempCache.Count);
                tempSphere.GetComponent<HotSwapColor>().SetColor(setColor);
                index++;
            }
            ClearPoints();
            gestureIndex++;
            tempCache.Clear();
        }
    }

        public Vector2Int   ComputeDebugPointPosition(Vector2 coords, HPUIMultiFingerCanvas multiFinger, out Vector2 localPos)
        {
            int xVal = Mathf.FloorToInt(coords.x / multiFinger.MaxBounds.x * multiFinger.MeshXResolution );
            int yVal = Mathf.FloorToInt(coords.y / multiFinger.MaxBounds.y *  multiFinger.MeshYResolution );

            xVal = Mathf.Clamp(xVal, 0, multiFinger.MeshXResolution - 2);
            yVal = Mathf.Clamp(yVal, 0, multiFinger.MeshYResolution - 2);

            float xRemainder = (coords.x * multiFinger.MeshXResolution / multiFinger.MaxBounds.x) - xVal;
            float yRemainder = (coords.y * multiFinger.MeshYResolution / multiFinger.MaxBounds.y) - yVal;

            localPos = new Vector2(xRemainder, yRemainder);
            return new Vector2Int(xVal, yVal);
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                foreach (GameObject sphere in color1)
                {
                    sphere.SetActive(enableColor1);
                }

                foreach (GameObject sphere in color2)
                {
                    sphere.SetActive(enableColor2);
                }

                foreach (GameObject sphere in color3)
                {
                    sphere.SetActive(enableColor3);
                }
            }
        }

        [Button]
        public void ClearPoints()
        {
            foreach (GameObject sphere in sphereObjects)
            {
                Destroy(sphere);
            }


            foreach (GameObject sphere in tempCache)
            {
                Destroy(sphere);
            }

            tempCache.Clear();
            sphereObjects.Clear();
            color1.Clear();
            color2.Clear();
            color3.Clear();
            color4.Clear();
        }


    }