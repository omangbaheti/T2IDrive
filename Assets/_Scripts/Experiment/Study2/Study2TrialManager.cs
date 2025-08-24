using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using TMPro;
using ubco.ovilab.HPUI;
using ubco.ovilab.HPUI.Core;
using ubco.ovilab.HPUI.Interaction;
using ubco.ovilab.hpuiModel;
using UnityEditor.VersionControl;
using UnityEngine;
using UXF;
using XRUtils = Unity.XR.CoreUtils.Collections;
public class Study2TrialManager : MonoBehaviour, IHPUICanvasUIManager
{
    public List<float> XDivisions => xDivisions;
    public List<float> YDivisions => yDivisions;
    public List<MicrogestureAction> GestureActions => gestureLayout.microGestureActions;
    public HPUIMultiFingerCanvas HPUICanvas => hpuiCanvas;

    public XRUtils.SerializableDictionary<Vector2Int?, HPUICanvasRegion> HPUIRegions => hpuiRegions;
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
    private Vector2Int startRegion;
    private Vector2Int currentRegion;
    private Vector2Int endRegion;
    private HPUIMultiFingerCanvas hpuiCanvas;

    [SerializeField] private XRUtils.SerializableDictionary<Vector2Int?, HPUICanvasRegion> hpuiRegions = new();
    [SerializeField] private GameObject layer1Prefab;
    [SerializeField] private GameObject layer2Prefab;
    [SerializeField, ShowField(nameof(InteractionMapping), InteractionMapping.Indirect)] private Transform prompter;
    [SerializeField, ShowField(nameof(InteractionMapping), InteractionMapping.Indirect)] private XRUtils.SerializableDictionary<Vector2Int?, HPUICanvasRegion> indirectMappingTransforms;
    [SerializeField, ShowField(nameof(InteractionMapping), InteractionMapping.Indirect)] private Transform radialDistal;
    [SerializeField, ShowField(nameof(InteractionMapping), InteractionMapping.Indirect)] private Transform radialIntermediate;
    [SerializeField, ShowField(nameof(InteractionMapping), InteractionMapping.Indirect)] private Transform radialProximal;
    [SerializeField, ShowField(nameof(InteractionMapping), InteractionMapping.Indirect)] private Transform volarDistal;
    [SerializeField, ShowField(nameof(InteractionMapping), InteractionMapping.Indirect)] private Transform volarIntermediate;
    [SerializeField, ShowField(nameof(InteractionMapping), InteractionMapping.Indirect)] private Transform volarProximal;
    private Dictionary<Vector2Int, Transform> interactionMappingTransforms = new();

    private void OnEnable()
    {
        hpuiCanvas = GetComponent<HPUIMultiFingerCanvas>();
        canvasTracker = GetComponent<HPUIInteractableCanvasTracker>();
        HPUICanvas.OnCanvasInteractions.AddListener(HandleCanvasGesture);
        // experimentManager = FindAnyObjectByType<TypingExperimentManager>();
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
        interactionMappingTransforms = new()
        {
            {new Vector2Int(1,2), radialDistal},
            {new Vector2Int(1,1), radialIntermediate},
            {new Vector2Int(1,0), radialProximal},
            {new Vector2Int(0,2), volarDistal},
            {new Vector2Int(0,1), volarIntermediate},
            {new Vector2Int(0,0), volarProximal},
        };
        SpawnCanvasRegions();
    }


    public void SpawnCanvasRegions()
    {
        ResetCanvasRegions();
        foreach (float division in gestureLayout.xDivisions) {xDivisions.Add(division * HPUICanvas.MaxBounds.x);}
        foreach (float division in gestureLayout.yDivisions) {yDivisions.Add(division * HPUICanvas.MaxBounds.y);}
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
                hpuiRegion.UIVisual = layer2Prefab;
                hpuiRegion.pressedColor = selectedColor;
                hpuiRegion.defaultColor = defaultColor;
                hpuiRegion.canvasInteractable = HPUICanvas;
                hpuiRegion.parentTransform = layer2;
                hpuiRegion.canvasManager = this;
                List<MicrogestureAction> actions = GestureActions.FindAll(action => action.startRegion == hpuiRegion.ID);
                List<TextMeshPro> textFields = regionGameObject.GetComponentsInChildren<TextMeshPro>().ToList();
                Debug.Log($"Text Field Length: {textFields.Count}");
                textFields.Sort((x, y) => string.Compare(x.text, y.text, StringComparison.Ordinal));
                for (int k  = 0; k < actions.Count; k++)
                {
                    CharacterOutput charOutput = actions[k].SwipeActions.OfType<CharacterOutput>().First();
                    textFields[k].text = String.Empty;
                    Debug.Log($"{hpuiRegion.ID}: {charOutput.outputKey} {textFields[k].name}");
                    // charOutput.inputStreamTracker = keyboardInputStreamTracker;
                    textFields[k].text = charOutput.outputKey;
                }
                if (actions.Count != textFields.Count)
                {
                    Debug.LogError("Mismatched gesture action count");
                }
                SetFollowTransform(hpuiRegion);
                hpuiRegions.Add(new Vector2Int(i,j), hpuiRegion);
            }
        }

        //Setup Layer 2
        foreach (MicrogestureAction action in GestureActions)
        {
            hpuiRegions[action.startRegion].gestureActions.Add(action);
        }
        foreach ((Vector2Int? ID, HPUICanvasRegion region) in hpuiRegions)
        {
            region.InitialiseUI();
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
            TransformFollower transformFollower = region.gameObject.AddComponent<TransformFollower>();
            transformFollower.SetTarget(region.followTransform);
            transformFollower.SetRotationOffset(new Vector3(90,0,-90));
            transformFollower.SetScaleOffset(transform.lossyScale);
        }
        else
        {
            TransformFollower transformFollower = region.gameObject.AddComponent<TransformFollower>();
            transformFollower.SetTarget(interactionMappingTransforms[region.ID]);
            transformFollower.SetRotationOffset(new Vector3(90,0,0));
            Transform targetTransform = interactionMappingTransforms[region.ID].transform;
            Vector3 targetScale = new Vector3(targetTransform.lossyScale.x/targetTransform.parent.lossyScale.x, targetTransform.lossyScale.y/targetTransform.parent.lossyScale.y, targetTransform.lossyScale.z/targetTransform.parent.lossyScale.z); 
            transformFollower.SetScaleOffset(targetScale*2);
        }
    }

    public void ResetCanvasRegions()
    {
        // HPUICanvasRegion[] CanvasRegions = GetComponents<HPUICanvasRegion>();
        // foreach (HPUICanvasRegion region in CanvasRegions)
        // {
        //     Destroy(region);
        //     Debug.Log("Destroying regions");
        // }
        // xDivisions.Clear();
        // yDivisions.Clear();
        // hpuiRegions.Clear();
    }

    public void InitialiseRegions()
    {

    }

    public void HandleCanvasGesture(HPUIGestureEventArgs gestureArgs, HPUICanvasEventArgs canvasArgs)
    {
        if (!Session.instance.InTrial)
        {
            Debug.LogWarning("Interaction when not in trial");
            return;
        }
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
                //canvasTracker.RecordRow(gestureArgs, canvasArgs);
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
                foreach (GameObject uiButton in layer1)
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
                foreach (GameObject uiButton in layer1)
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
        foreach (KeyValuePair<Vector2Int, GameObject> uiElement in layer1)
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


public enum InteractionMapping
{
    Direct,
    Indirect
}