using System;
using System.Collections.Generic;
using ubco.ovilab.HPUI.Core;
using ubco.ovilab.HPUI.Interaction;
using ubco.ovilab.hpuiModel;
using UnityEngine;
using XRUtils = Unity.XR.CoreUtils.Collections;
public class Study2TrialManager : MonoBehaviour, IHPUICanvasUIManager
{
    public List<float> XDivisions => xDivisions;
    public List<float> YDivisions => yDivisions;
    public List<MicrogestureAction> GestureActions => gestureLayout.microGestureActions;
    public HPUIMultiFingerCanvas HPUICanvas { get; set; }

    public XRUtils.SerializableDictionary<Vector2Int?, HPUICanvasRegion> HPUIRegions { get; }
    public InteractionMapping InteractionMapping;
    public GestureLayoutSetup gestureLayout;
    public Transform UIParent;
    public Color defaultColor;
    public Color selectedColor;

    private List<float> xDivisions = new();
    private List<float> yDivisions = new();
    private Transform layer1;
    private Transform layer2;
    private HPUIInteractableCanvasTracker canvasTracker;

    [SerializeField] private XRUtils.SerializableDictionary<Vector2Int?, HPUICanvasRegion> hpuiRegions = new();
    [SerializeField] private GameObject layer1Prefab;
    [SerializeField] private GameObject layer2Prefab;

    private void OnEnable()
    {
        HPUICanvas = GetComponent<HPUIMultiFingerCanvas>();
        canvasTracker = GetComponent<HPUIInteractableCanvasTracker>();
        HPUICanvas.OnCanvasInteractions.AddListener(HandleCanvasGesture);
        // experimentManager = FindAnyObjectByType<TypingExperimentManager>();
        HPUICanvas.OnCanvasInteractions.AddListener(HandleCanvasGesture);

        if (UIParent == null)
        {
            UIParent = transform;
        }

        layer1 = Instantiate(new GameObject("Layer1").transform, UIParent);
        layer2 = Instantiate(new GameObject("Layer2").transform, UIParent);
    }

    private void OnDisable()
    {
        HPUICanvas.OnCanvasInteractions.RemoveListener(HandleCanvasGesture);
    }

    private void Start()
    {
        SpawnCanvasRegions();
    }


    public void SpawnCanvasRegions()
    {
        ResetCanvasRegions();
        foreach (float division in gestureLayout.xDivisions) xDivisions.Add(division * HPUICanvas.MaxBounds.x);
        foreach (float division in gestureLayout.yDivisions) yDivisions.Add(division * HPUICanvas.MaxBounds.y);
        //Setup Layer 1
        for (int i = 0; i < xDivisions.Count-1; i++)
        {
            for (int j = 0; j < yDivisions.Count-1; j++)
            {
                GameObject regionGameObject = Instantiate(layer1Prefab, layer1);
                HPUICanvasRegion hpuiRegion =  regionGameObject.AddComponent<HPUICanvasRegion>();
                regionGameObject.name = $"HPUIRegion ({i},{j})";
                hpuiRegion.ID = new Vector2Int(i, j);
                hpuiRegion.basePoint = new Vector2(xDivisions[i], yDivisions[j]);
                hpuiRegion.area = new Vector2(xDivisions[i+1] - xDivisions[i], yDivisions[j+1] - yDivisions[j]);
                hpuiRegion.EndRegionVisual = layer2Prefab;
                hpuiRegion.pressedColor = selectedColor;
                hpuiRegion.defaultColor = defaultColor;
                hpuiRegion.canvasInteractable = HPUICanvas;
                SetFollowTransform(hpuiRegion);
                hpuiRegions.Add(new Vector2Int(i,j), hpuiRegion);
            }
        }

        //Setup Layer 2
        foreach (MicrogestureAction action in GestureActions)
        {
            hpuiRegions[action.startRegion].gestureActions.Add(action);
        }

    }

    private void SetFollowTransform(HPUICanvasRegion region)
    {
        if (InteractionMapping == InteractionMapping.Direct)
        {
            Vector2 regionCenterPoint = region.basePoint + region.area/2f + region.centreOffset;
            Vector2Int centreIndex = HPUICanvasComponentUtils.CalculateColliderIndex(regionCenterPoint, HPUICanvas);
            Transform regionCentre = HPUICanvas.coordsToCollider[centreIndex].transform;
            region.followTransform = regionCentre;
        }
        else
        {

        }
    }

    public void ResetCanvasRegions()
    {
        HPUICanvasRegion[] CanvasRegions = GetComponents<HPUICanvasRegion>();
        foreach (HPUICanvasRegion region in CanvasRegions)
        {
            Destroy(region);
            Debug.Log("Destroying regions");
        }
        xDivisions.Clear();
        yDivisions.Clear();
        hpuiRegions.Clear();
    }

    public void InitialiseRegions()
    {

    }

    public void HandleCanvasGesture(HPUIGestureEventArgs gestureArgs, HPUICanvasEventArgs canvasArgs)
    {

    }
}


public enum InteractionMapping
{
    Direct,
    Indirect
}