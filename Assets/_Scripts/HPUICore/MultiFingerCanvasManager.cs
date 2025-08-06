using System;
using System.Collections.Generic;
using XRUtils = Unity.XR.CoreUtils.Collections;
using EditorAttributes;
using ubco.ovilab.HPUI.Core;
using ubco.ovilab.HPUI.Interaction;
using ubco.ovilab.hpuiModel;
using UnityEngine;
using UXF;

public class MultiFingerCanvasManager : MonoBehaviour, IHPUICanvasUIManager
{
    public List<float> XDivisions => xDivisions;
    public List<float> YDivisions => yDivisions;
    public HPUIMultiFingerCanvas HPUICanvas { get; set; }
    public XRUtils.SerializableDictionary<Vector2Int?, HPUICanvasRegion> HPUIRegions => hpuiRegions;

    public List<MicrogestureAction> gestureActions = new();
    public Transform canvasInterfaceContainer;
    private Transform layer1CanvasContainer;
    private Transform layer2CanvasContainer;

    [SerializeField] private GestureLayoutSetup layoutSetup;
    [SerializeField] private XRUtils.SerializableDictionary<Vector2Int?, HPUICanvasRegion> hpuiRegions = new();

    private List<float> xDivisions = new();
    private List<float> yDivisions = new();
    private Vector2Int startRegion;
    private Vector2Int currentRegion;
    private Vector2Int endRegion;

    [SerializeField] private GameObject layer1Prefab;
    [SerializeField] private GameObject layer2Prefab;

    [SerializeField] private Color selectedColor;
    [SerializeField] private Color defaultColor;
    [SerializeField] private HPUIInteractableCanvasTracker canvasTracker;

    private void OnEnable()
    {
        HPUICanvas = GetComponent<HPUIMultiFingerCanvas>();
        canvasTracker = GetComponent<HPUIInteractableCanvasTracker>();
        HPUICanvas.OnCanvasInteractions.AddListener(HandleCanvasGesture);
        if (canvasInterfaceContainer == null)
        {
            canvasInterfaceContainer = new GameObject("CanvasInterfaceContainer").transform;
            canvasInterfaceContainer.parent = transform;
            canvasInterfaceContainer.localScale = Vector3.one;
        }
        else
        {
            canvasInterfaceContainer.localScale = Vector3.one;
        }
        layer1CanvasContainer = new GameObject("Layer1CanvasContainer").transform;
        layer1CanvasContainer.parent = canvasInterfaceContainer;
        layer1CanvasContainer.localScale = Vector3.one;
        layer2CanvasContainer = new GameObject("Layer2CanvasContainer").transform;
        layer2CanvasContainer.parent = canvasInterfaceContainer;
        layer2CanvasContainer.localScale = Vector3.one;
    }

    private void OnDisable()
    {
        HPUICanvas.OnCanvasInteractions.RemoveListener(HandleCanvasGesture);
    }

    [Button]
    public void ResetKeyboard()
    {
        foreach (KeyValuePair<Vector2Int?, HPUICanvasRegion> uiElement in hpuiRegions)
        {
            Destroy(uiElement.Value.gameObject);
        }
        hpuiRegions.Clear();
        SpawnCanvasRegions();
    }

    public void SpawnCanvasRegions()
    {
        foreach (float division in layoutSetup.xDivisions)
        {
            xDivisions.Add(division * HPUICanvas.MaxBounds.x);
        }

        foreach (float division in layoutSetup.yDivisions)
        {
            yDivisions.Add(division * HPUICanvas.MaxBounds.y);
        }

        gestureActions = layoutSetup.microGestureActions;

        for (int i = 0; i < xDivisions.Count-1; i++)
        {
            for (int j = 0; j < yDivisions.Count-1; j++)
            {
                GameObject newRegion = Instantiate(layer1Prefab, layer1CanvasContainer, false);
                newRegion.name = "Layer 1:"+" " + i + "-" + j;
                HPUICanvasRegion hpuiRegion = newRegion.AddComponent<HPUICanvasRegion>();
                hpuiRegion.ID = new Vector2Int(i, j);
                hpuiRegion.basePoint = new Vector2(xDivisions[i], yDivisions[j]);
                hpuiRegion.area = new Vector2(xDivisions[i+1] - xDivisions[i], yDivisions[j+1] - yDivisions[j]);
                hpuiRegion.EndRegionVisual = layer2Prefab;
                // hpuiRegion.pressedColor = selectedColor;
                // hpuiRegion.defaultColor = defaultColor;
                // hpuiRegion.canvasManager = this;
                Vector2 regionCenterPoint = hpuiRegion.basePoint + hpuiRegion.area/2f + hpuiRegion.centreOffset;
                Vector2Int centreIndex = HPUICanvasComponentUtils.CalculateColliderIndex(regionCenterPoint, HPUICanvas);
                Transform uiAnchor = HPUICanvas.coordsToCollider[centreIndex].transform;
            }
        }

        foreach (MicrogestureAction action in gestureActions)
        {
            hpuiRegions[action.startRegion].gestureActions.Add(action);
        }
    }

    public void InitialiseRegions()
    {

        foreach ((Vector2Int? ID, HPUICanvasRegion region) in hpuiRegions)
        {
            region.InitialiseUI();
        }
    }
    public void HandleCanvasGesture(HPUIGestureEventArgs gestureArgs, HPUICanvasEventArgs canvasArgs)
    {

        if (canvasArgs.GesturePositions.Count <= 0)
        {
            Debug.Log("Not enough points, cancelling gesture");
            return;
        }
        switch (canvasArgs.State)
        {
            case HPUICanvasState.INVALID:
                break;
            case HPUICanvasState.NotStarted:
                canvasTracker.RecordRow(gestureArgs, canvasArgs);
                break;
            case HPUICanvasState.Started:
                startRegion = GetInteractionRegion(canvasArgs.GesturePositions[^1]);
                canvasArgs.SwipeStartRegion = startRegion;
                canvasArgs.CurrentSwipeRegion = startRegion;
                SetUIActive(false);
                hpuiRegions[startRegion].OnGestureStarted(canvasArgs);
                Session.instance.CurrentTrial.settings.SetValue(StudyLogs.GestureStartRegion, StudyLogs.VectorToRegionDict[canvasArgs.SwipeStartRegion.Value]);
                canvasTracker.RecordRow(gestureArgs, canvasArgs);
                break;
            case HPUICanvasState.Processing:
                currentRegion = GetInteractionRegion(canvasArgs.GesturePositions[^1]);
                canvasArgs.SwipeStartRegion = startRegion;
                canvasArgs.CurrentSwipeRegion = currentRegion;
                canvasArgs.SwipeEndRegion = endRegion;
                hpuiRegions[startRegion].OnGestureOnGoing(canvasArgs);
                canvasTracker.RecordRow(gestureArgs, canvasArgs);
                break;
            case HPUICanvasState.Cancelled:
                foreach (KeyValuePair<Vector2Int?, HPUICanvasRegion> region in hpuiRegions)
                {
                    region.Value.DisableUI();
                }
                foreach (HPUICanvasRegion uiButton in hpuiRegions.Values)
                {
                    uiButton.gameObject.SetActive(true);
                }
                canvasTracker.RecordRow(gestureArgs, canvasArgs);
                break;
            case HPUICanvasState.Completed:
                endRegion = GetInteractionRegion(canvasArgs.GesturePositions[^1]);
                canvasArgs.SwipeStartRegion = startRegion;
                canvasArgs.CurrentSwipeRegion = endRegion;
                canvasArgs.SwipeEndRegion = endRegion;
                canvasTracker.RecordRow(gestureArgs, canvasArgs);
                hpuiRegions[startRegion].OnGestureEnded(canvasArgs);
                foreach (KeyValuePair<Vector2Int?, HPUICanvasRegion> region in hpuiRegions)
                {
                    region.Value.DisableUI();
                }
                foreach (HPUICanvasRegion uiButton in hpuiRegions.Values)
                {
                    uiButton.gameObject.SetActive(true);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public virtual void SetUIActive(bool active)
    {
        foreach (KeyValuePair<Vector2Int?, HPUICanvasRegion> uiElement in hpuiRegions)
        {
            uiElement.Value.gameObject.SetActive(active);
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
