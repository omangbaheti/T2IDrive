using System;
using System.Collections.Generic;
using TMPro;
using XRUtils = Unity.XR.CoreUtils.Collections;
using ubco.ovilab.HPUI.Core;
using ubco.ovilab.HPUI.Interaction;
using ubco.ovilab.hpuiModel;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public class ComfortStudyTrialManager : MonoBehaviour, IHPUICanvasUIManager
{

    public List<float> XDivisions => xDivisions;

    public List<float> YDivisions => yDivisions;

    public XRUtils.SerializableDictionary<Vector2Int?, HPUICanvasRegion> HPUIRegions => hpuiRegions;

    public HPUIMultiFingerCanvas HPUICanvas { get; set; }

    public List<MicrogestureAction> gestureActions = new();
    public SerializedDictionary<Vector2Int, GameObject> layer1StartRegions = new();
    private SerializedDictionary<Vector2Int, GameObject> layer1EndRegions = new();
    // [SerializeField] public GameObject RedButton;
    // [SerializeField] public GameObject GreenButton;

    [SerializeField] private GameObject BlueButton;
    [SerializeField] private XRUtils.SerializableDictionary<Vector2Int?, HPUICanvasRegion> hpuiRegions = new();
    [SerializeField] private GestureLayoutSetup gestureLayout;
    [SerializeField] public Color startRegionColor = Color.blue;
    [SerializeField] public Color endRegionColor = Color.red;

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

    private void OnEnable()
    {
        HPUICanvas = GetComponent<HPUIMultiFingerCanvas>();
        hpuiInteractableCanvasTracker = GetComponent<HPUIInteractableCanvasTracker>();
        HPUICanvas.OnCanvasInteractions.AddListener(HandleCanvasGesture);
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
                hpuiRegion.UIVisual = BlueButton;
                // hpuiRegion.EndRegionVisual = BlueButton;
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
            region.InitialiseUI(0);
        }
        InitialiseUI();
    }

    public void InitialiseUI()
    {
        // foreach (MicrogestureAction action in gestureActions)
        // {
        //     if (layer1StartRegions.ContainsKey(action.startRegion))
        //     {
        //         continue;
        //     }
        //     HPUICanvasRegion region = hpuiRegions[action.startRegion];
        //     Vector2 regionCenterPoint = region.basePoint + region.area/2f + region.centreOffset;
        //     Vector2Int centreIndex = HPUICanvasComponentUtils.CalculateColliderIndex(regionCenterPoint, HPUICanvas);
        //     Transform regionCentre = HPUICanvas.coordsToCollider[centreIndex].transform;
        //     GameObject UI = Instantiate(BlueButton, regionCentre.position, Quaternion.identity, regionCentre);
        //     UI.transform.localPosition = new Vector3(0,50f,0);
        //     UI.transform.localRotation = Quaternion.Euler(90,90,0);
        //     layer1StartRegions.Add(action.startRegion, UI);
        //     UI.SetActive(false);
        // }
       Debug.Log("InitialiseUI");

        foreach (MicrogestureAction action in gestureActions)
        {
            // Handle start region
            if (!layer1StartRegions.ContainsKey(action.startRegion))
            {
                HPUICanvasRegion startRegion = hpuiRegions[action.startRegion];
                Vector2 startCenterPoint = startRegion.basePoint + startRegion.area / 2f + startRegion.centreOffset;
                Vector2Int startCentreIndex = HPUICanvasComponentUtils.CalculateColliderIndex(startCenterPoint, HPUICanvas);
                Debug.Log("Start Centre Index: " + startCentreIndex);
                Transform startRegionCentre = HPUICanvas.coordsToCollider[startCentreIndex].transform;
                GameObject startUI = Instantiate(BlueButton, startRegionCentre.position, Quaternion.identity, startRegionCentre);
                startUI.name = "BlueButtonStartRegion";
                startUI.transform.localPosition = new Vector3(0, 50f, 0);
                startUI.transform.localRotation = Quaternion.Euler(90, 90, 0);
                layer1StartRegions.Add(action.startRegion, startUI);
                startUI.SetActive(false);
            }

            // Handle end region
            if (!layer1EndRegions.ContainsKey(action.endRegion))
            {
                HPUICanvasRegion endRegion = hpuiRegions[action.endRegion];
                Vector2 endCenterPoint = endRegion.basePoint + endRegion.area / 2f + endRegion.centreOffset;
                Vector2Int endCentreIndex = HPUICanvasComponentUtils.CalculateColliderIndex(endCenterPoint, HPUICanvas);
                Debug.Log("End Centre Index: " + endCentreIndex);
                Transform endRegionCentre = HPUICanvas.coordsToCollider[endCentreIndex].transform;
                GameObject endUI = Instantiate(BlueButton, endRegionCentre.position, Quaternion.identity, endRegionCentre);
                endUI.name = "EndRegion";
                endUI.transform.localPosition = new Vector3(0, 50f, 0);
                endUI.transform.localRotation = Quaternion.Euler(90, 90, 0);
                layer1EndRegions.Add(action.endRegion, endUI);
                endUI.SetActive(false);
            }
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
                break;
            }
            case HPUICanvasState.Started:
            {
                startRegion = GetInteractionRegion(canvasArgs.GesturePositions[^1]);
                canvasArgs.SwipeStartRegion = startRegion;
                canvasArgs.CurrentSwipeRegion = startRegion;
                FingerRegions trialStartRegion = ComfortStudyExperimentManager.Instance.startRegion;

                startedCorrectly = StudyLogs.RegionToVectorDict[trialStartRegion] == startRegion;

                if (startedCorrectly)
                {
                    SetUIActive(false);
                    hpuiRegions[startRegion].OnGestureStarted(canvasArgs);
                }
                else
                {
                    Debug.Log("Failed Trial?");
                }
                break;
            }
            case HPUICanvasState.Processing:
            {
                currentRegion = GetInteractionRegion(canvasArgs.GesturePositions[^1]);
                canvasArgs.SwipeStartRegion = startRegion;
                canvasArgs.CurrentSwipeRegion = currentRegion;
                canvasArgs.SwipeEndRegion = endRegion;
                if (startedCorrectly)
                {
                    hpuiRegions[startRegion].OnGestureOnGoing(canvasArgs);
                }
                else
                {
                    Debug.Log("Failed Trial?");
                }
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
                    if (startedCorrectly)
                    {
                        hpuiRegions[startRegion].OnGestureEnded(canvasArgs);
                    }
                    else
                    {
                        Debug.Log("Failed Trial?");
                    }
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

    }

    public void SetCurrentTrialActive()
    {
        foreach (KeyValuePair<Vector2Int, GameObject> uiElement in layer1StartRegions)
        {
            uiElement.Value.SetActive(false);
        }
        foreach (KeyValuePair<Vector2Int, GameObject> uiElement in layer1EndRegions)
        {
            uiElement.Value.SetActive(false);
        }
        FingerRegions currentTrialStartRegion = ComfortStudyExperimentManager.Instance.startRegion;
        Vector2Int startRegionIndex = StudyLogs.RegionToVectorDict[currentTrialStartRegion];
        GameObject startRegionButton = layer1StartRegions[startRegionIndex].transform.GetChild(0).gameObject;
        startRegionButton.transform.parent.gameObject.SetActive(true);
        startRegionButton.transform.parent.GetComponent<TextMeshPro>().text = "1";
        startRegionButton.GetComponent<HotSwapColor>().SetColor(startRegionColor);

        FingerRegions currentTrialEndRegion = ComfortStudyExperimentManager.Instance.endRegion;
        Vector2Int endRegionIndex = StudyLogs.RegionToVectorDict[currentTrialEndRegion];
        GameObject endRegionButton = layer1EndRegions[endRegionIndex].transform.GetChild(0).gameObject;
        endRegionButton.transform.parent.gameObject.SetActive(true);
        // layer1EndRegions[endRegionIndex].gameObject.SetActive(true);
        if (currentTrialEndRegion == currentTrialStartRegion)
        {

            endRegionButton.transform.parent.GetComponent<TextMeshPro>().text = "1";
            endRegionButton.GetComponent<HotSwapColor>().SetColor(startRegionColor);
        }
        else
        {

            endRegionButton.transform.parent.GetComponent<TextMeshPro>().text = "2";
            endRegionButton.GetComponent<HotSwapColor>().SetColor(endRegionColor);
        }
    }

    public virtual void SetUIActive(bool active)
    {
        foreach (KeyValuePair<Vector2Int, GameObject> uiElement in layer1StartRegions)
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




