using System;
using System.Collections.Generic;
using Experiment;
using TMPro;
using ubco.ovilab.HPUI.Core;
using ubco.ovilab.HPUI.Interaction;
using ubco.ovilab.hpuiModel;
using UnityEngine;
using XRUtils = Unity.XR.CoreUtils.Collections;

using Unity.XR.CoreUtils.Collections;
using UnityEngine.Serialization;
using UXF;

[RequireComponent(typeof(HPUIInteractableCanvasTracker))]
public class TrialManager : MonoBehaviour, IHPUICanvasUIManager
{
    public XRUtils.SerializableDictionary<Vector2Int?, HPUICanvasRegion> HPUIRegions => hpuiRegions;

    public List<float> XDivisions => xDivisions;

    public List<float> YDivisions => yDivisions;


    public HPUIMultiFingerCanvas HPUICanvas { get; set; }
    public List<MicrogestureAction> gestureActions = new();
    public Dictionary<Vector2Int, GameObject> layer1UIElements = new();

    [SerializeField] public GameObject BlueButton;
    [SerializeField] public GameObject RedButton;
    [SerializeField] XRUtils.SerializableDictionary<Vector2Int?, HPUICanvasRegion> hpuiRegions = new();
    [SerializeField] private GestureLayoutSetup gestureLayout;
    [SerializeField] private TextMeshProUGUI debugText;
    private Vector2Int ID;
    private Vector2 centrePoint;
    private Vector2 area;
    private Vector2 basePoint;
    private Vector2 centreOffset;
    private List<float> xDivisions = new();
    private List<float> yDivisions = new();
    private HPUIInteractableCanvasTracker hpuiInteractableCanvasTracker;
    private Vector2Int startRegion;
    private Vector2Int currentRegion;
    private Vector2Int endRegion;
    private bool startedCorrectly = false;
    private FingerSwipeExperimentManager expManager;
    
    

    private void OnEnable()
    {
        HPUICanvas = GetComponent<HPUIMultiFingerCanvas>();
        hpuiInteractableCanvasTracker = GetComponent<HPUIInteractableCanvasTracker>();
        HPUICanvas.OnCanvasInteractions.AddListener(HandleCanvasGesture);
        expManager = FindAnyObjectByType<FingerSwipeExperimentManager>();

    }

    private void OnDisable()
    {
        HPUICanvas.OnCanvasInteractions.RemoveListener(HandleCanvasGesture);
    }

    private void Start()
    {
        SpawnCanvasRegions();
        InitialiseRegions();
    }

    public void SpawnCanvasRegions()
    {
        HPUICanvasRegion[] CanvasRegions = GetComponents<HPUICanvasRegion>();
        foreach (HPUICanvasRegion region in CanvasRegions)
        {
            DestroyImmediate(region);
            Debug.Log($"Added Region {region.ID}");
        }
        hpuiRegions.Clear();

        foreach (float division in gestureLayout.xDivisions)
        {
            xDivisions.Add(division * HPUICanvas.MaxBounds.x);
        }

        foreach (float division in gestureLayout.yDivisions)
        {
            yDivisions.Add(division * HPUICanvas.MaxBounds.y);
        }

        gestureActions = gestureLayout.microGestureActions;

        for (int i = 0; i < xDivisions.Count-1; i++)
        {
            for (int j = 0; j < yDivisions.Count-1; j++)
            {
                HPUICanvasRegion hpuiRegion = gameObject.AddComponent<HPUICanvasRegion>();
                hpuiRegion.ID = new Vector2Int(i, j);
                hpuiRegion.basePoint = new Vector2(xDivisions[i], yDivisions[j]);
                hpuiRegion.area = new Vector2(xDivisions[i+1] - xDivisions[i], yDivisions[j+1] - yDivisions[j]);
                hpuiRegion.UIVisual = RedButton;
                hpuiRegions.Add(new Vector2Int(i,j), hpuiRegion);
            }
        }
    }

    public void InitialiseRegions()
    {
        foreach (MicrogestureAction action in gestureActions)
        {
            hpuiRegions[action.startRegion].gestureActions.Add(action);
        }
        foreach ((Vector2Int? ID, HPUICanvasRegion region) in hpuiRegions)
        {
            region.InitialiseUI();
        }
        InitialiseUI();
    }

    public void InitialiseUI()
    {
        foreach (MicrogestureAction action in gestureActions)
        {
            if (layer1UIElements.ContainsKey(action.startRegion))
            {
                continue;
            }
            HPUICanvasRegion region = hpuiRegions[action.startRegion];
            Vector2 regionCenterPoint = region.basePoint + region.area/2f + region.centreOffset;
            Vector2Int centreIndex = HPUICanvasComponentUtils.CalculateColliderIndex(regionCenterPoint, HPUICanvas);
            Debug.Log("Centre Index;" + centreIndex);
            Transform regionCentre = HPUICanvas.coordsToCollider[centreIndex].transform;
            GameObject UI = Instantiate(BlueButton, regionCentre.position, Quaternion.identity, regionCentre);
            UI.transform.localPosition = new Vector3(0,50f,0);
            UI.transform.localRotation = Quaternion.Euler(90,90,0);
            layer1UIElements.Add(action.startRegion, UI);
            UI.SetActive(false);
        }
    }

    public void HandleCanvasGesture(HPUIGestureEventArgs gestureArgs, HPUICanvasEventArgs canvasArgs)
    {
        if (canvasArgs.GesturePositions.Count < 1)
        {
            Debug.LogWarning($"Gesture Positions are empty at {canvasArgs.State.ToString()}");
        }
        switch (canvasArgs.State)
        {
            case HPUICanvasState.INVALID or HPUICanvasState.NotStarted:
            {
                hpuiInteractableCanvasTracker.RecordRow(gestureArgs, canvasArgs);
                break;
            }
            case HPUICanvasState.Started:
            {
                startRegion = GetInteractionRegion(canvasArgs.GesturePositions[^1]);
                canvasArgs.SwipeStartRegion = startRegion;
                canvasArgs.CurrentSwipeRegion = startRegion;

                FingerRegions trialStartRegion = (FingerRegions) Session.instance.CurrentTrial.settings.GetObject(StudyLogs.StartRegion);
                Debug.Log($"Starting region {startRegion} : Target Region {trialStartRegion}");
                startedCorrectly = StudyLogs.RegionToVectorDict[trialStartRegion] == startRegion;
                SetUIActive(false);
                hpuiRegions[startRegion].OnGestureStarted(canvasArgs);
                Session.instance.CurrentTrial.settings.SetValue(StudyLogs.GestureStartRegion, StudyLogs.VectorToRegionDict[canvasArgs.SwipeStartRegion.Value]);
                hpuiInteractableCanvasTracker.RecordRow(gestureArgs, canvasArgs);
                break;
            }
            case HPUICanvasState.Processing:
            {
                currentRegion = GetInteractionRegion(canvasArgs.GesturePositions[^1]);
                canvasArgs.SwipeStartRegion = startRegion;
                canvasArgs.CurrentSwipeRegion = currentRegion;
                canvasArgs.SwipeEndRegion = endRegion;
                hpuiRegions[startRegion].OnGestureOnGoing(canvasArgs);
                hpuiInteractableCanvasTracker.RecordRow(gestureArgs, canvasArgs);
                break;
            }
            case HPUICanvasState.Cancelled:
            {
                endRegion = GetInteractionRegion(canvasArgs.GesturePositions[^1]);
                canvasArgs.SwipeStartRegion = startRegion;
                canvasArgs.CurrentSwipeRegion = endRegion;
                canvasArgs.SwipeEndRegion = endRegion;
                hpuiRegions[startRegion].OnGestureEnded(canvasArgs);
                foreach (KeyValuePair<Vector2Int?, HPUICanvasRegion> region in hpuiRegions)
                {
                    region.Value.DisableUI();
                }

                break;
            }
            case HPUICanvasState.Completed:
            {
                try
                {
                    endRegion = GetInteractionRegion(canvasArgs.GesturePositions[^1]);
                    canvasArgs.SwipeStartRegion = startRegion;
                    canvasArgs.CurrentSwipeRegion = endRegion;
                    canvasArgs.SwipeEndRegion = endRegion;
                    hpuiInteractableCanvasTracker.RecordRow(gestureArgs, canvasArgs);
                    Session.instance.CurrentTrial.settings.SetValue(StudyLogs.GestureEndRegion, StudyLogs.VectorToRegionDict[canvasArgs.SwipeEndRegion.Value]);
                    // Session.instance.CurrentTrial.settings.SetValue(StudyLogs.CumulativeDistance, thumbPositionTracker.CumulativeDistance);
                      // Session.instance.CurrentTrial.settings.SetValue(StudyLogs.NetThumbDisplacement, thumbPositionTracker.NetDisplacement);
                    // string currentFinger = Session.instance.CurrentTrial.settings.GetString(StudyLogs.FingerType);

                    // Session.instance.CurrentTrial.settings.SetValue(StudyLogs.CumulativeDistanceNormalised, thumbPositionTracker.CumulativeDistance / expManager.FingerLengths[currentFinger]);
                    // Session.instance.CurrentTrial.settings.SetValue(StudyLogs.NetDisplacementNormalised, thumbPositionTracker.NetDisplacement / expManager.FingerLengths[currentFinger]);
                    hpuiRegions[startRegion].OnGestureEnded(canvasArgs);
                    foreach (KeyValuePair<Vector2Int?, HPUICanvasRegion> region in hpuiRegions)
                    {
                        region.Value.DisableUI();
                    }

                }
                catch (ArgumentOutOfRangeException e)
                {
                    Debug.LogError($"{e}: {canvasArgs.GesturePositions.Count}");
                    throw;
                }

                break;
            }
            default:
                Debug.Log("Unhandled HPUICanvasState");
                throw new ArgumentOutOfRangeException();
        }

        if (canvasArgs.CurrentSwipeRegion != null) debugText.text = canvasArgs.GesturePositions[^1].ToString();
    }

    public void SetCurrentTrialActive(Trial trial)
    {
        FingerRegions currentTrialStartRegion = (FingerRegions) trial.settings.GetObject(StudyLogs.StartRegion);
        Vector2Int startRegionIndex = StudyLogs.RegionToVectorDict[currentTrialStartRegion];
        layer1UIElements[startRegionIndex].gameObject.SetActive(true);
    }

    public virtual void SetUIActive(bool active)
    {
        foreach (KeyValuePair<Vector2Int, GameObject> uiElement in layer1UIElements)
        {
            uiElement.Value.SetActive(active);
        }
    }

    private Vector2Int GetInteractionRegion(Vector2 touchPoint)
    {
        int regionX = -1, regionY = -1;
        for (int i = 0; i < xDivisions.Count-1; i++)
        {
            if (touchPoint.x > xDivisions[i] && touchPoint.x <= xDivisions[i + 1])
            {
                regionX = i;
                break;
            }
        }

        for (int j = 0; j < yDivisions.Count-1; j++)
        {
            if (touchPoint.y > yDivisions[j] && touchPoint.y <= yDivisions[j + 1])
            {
                regionY = j;
                break;
            }
        }

        return new Vector2Int(regionX, regionY);
    }


}




