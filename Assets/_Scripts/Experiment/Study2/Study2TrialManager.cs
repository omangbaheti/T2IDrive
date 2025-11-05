using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using Experiment;
using TMPro;
using ubco.ovilab.HPUI;
using ubco.ovilab.HPUI.Core;
using ubco.ovilab.HPUI.Interaction;
using ubco.ovilab.hpuiModel;
using UnityEngine;
using UXF;
using XRUtils = Unity.XR.CoreUtils.Collections;
public class Study2TrialManager : MonoBehaviour, IHPUICanvasUIManager
{
    [SerializeField] private Transform DirectAnchor;
    [SerializeField] private Vector3 offset;
    [SerializeField] private Vector3 rotOffset;
    [SerializeField] private Vector3 scaleOffset;
    
    public List<float> XDivisions => xDivisions;
    public List<float> YDivisions => yDivisions;
    [SerializeField] public List<MicrogestureAction> gestureActions; 
    public HPUIMultiFingerCanvas HPUICanvas => hpuiCanvas;

    public XRUtils.SerializableDictionary<Vector2Int?, HPUICanvasRegion> HPUIRegions => hpuiRegions;
    public InteractionMapping InteractionMapping;
    public GestureLayoutSetup gestureLayout;
    public Transform UIParent;
    // public Color defaultColor;
    // public Color selectedColor;

    public AudioClip clickOpen;
    public AudioClip clickClose;
    public int counter;
    private List<float> xDivisions = new();
    private List<float> yDivisions = new();
    private Transform layer1;
    private Transform layer2;
    private HPUIInteractableCanvasTracker canvasTracker;
    private Vector2Int startRegion;
    private Vector2Int prevSwipeRegion;
    private Vector2Int currentRegion;
    private Vector2Int endRegion;
    private HPUIMultiFingerCanvas hpuiCanvas;

    private ActionInputStreamTracker actionInputStreamTracker;
    [SerializeField] private XRUtils.SerializableDictionary<Vector2Int?, HPUICanvasRegion> hpuiRegions = new();
    [SerializeField] private GameObject layer1Prefab;
    [SerializeField] private GameObject layer2Prefab;
    [SerializeField, ShowField(nameof(InteractionMapping), InteractionMapping.Indirect)] private XRUtils.SerializableDictionary<Vector2Int?, HPUICanvasRegion> indirectMappingTransforms;
    [SerializeField, ShowField(nameof(InteractionMapping), InteractionMapping.Indirect)] private Transform prompterAnchor;
    [SerializeField, ShowField(nameof(InteractionMapping), InteractionMapping.Indirect)] private Vector3 offset2;
    [SerializeField, ShowField(nameof(InteractionMapping), InteractionMapping.Indirect)] private Vector3 rotOffset2;
    [SerializeField, ShowField(nameof(InteractionMapping), InteractionMapping.Indirect)] private Vector3 scaleOffset2;
    [SerializeField, ShowField(nameof(InteractionMapping), InteractionMapping.Indirect)] private Transform radialDistal;
    [SerializeField, ShowField(nameof(InteractionMapping), InteractionMapping.Indirect)] private Transform radialIntermediate;
    [SerializeField, ShowField(nameof(InteractionMapping), InteractionMapping.Indirect)] private Transform radialProximal;
    [SerializeField, ShowField(nameof(InteractionMapping), InteractionMapping.Indirect)] private Transform volarDistal;
    [SerializeField, ShowField(nameof(InteractionMapping), InteractionMapping.Indirect)] private Transform volarIntermediate;
    [SerializeField, ShowField(nameof(InteractionMapping), InteractionMapping.Indirect)] private Transform volarProximal;
    private Dictionary<Vector2Int, Transform> interactionMappingTransforms = new();
    public Dictionary<Vector2Int, Color> interactionMappingColor;
    private Study2ExperimentManager experimentManager;

    private void OnEnable()
    {
        hpuiCanvas = GetComponent<HPUIMultiFingerCanvas>();
        canvasTracker = GetComponent<HPUIInteractableCanvasTracker>();
        HPUICanvas.OnCanvasInteractions.AddListener(HandleCanvasGesture);
        experimentManager = FindAnyObjectByType<Study2ExperimentManager>();
        actionInputStreamTracker = GetComponent<ActionInputStreamTracker>();
        foreach (Transform child in UIParent)
        {
            Destroy(child.gameObject);
        }
        if (UIParent == null)
        {
            UIParent = transform;
        }
        UIParent.gameObject.SetActive(true);
        layer1 = new GameObject("Layer1").transform;
        layer2 = new GameObject("Layer2").transform;
        layer1.parent = UIParent;
        layer2.parent = UIParent;
        gestureActions = gestureLayout.microGestureActions;
    }

    private void OnDisable()
    {
        HPUICanvas.OnCanvasInteractions.RemoveListener(HandleCanvasGesture);
        UIParent.gameObject.SetActive(false);
    }

    private void OnDestroy()
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

    [Button]
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
                regionGameObject.GetComponent<HotSwapColor>().SetColor(interactionMappingColor[new (i, j)]);
                HPUICanvasRegionIcon hpuiRegion =  regionGameObject.AddComponent<HPUICanvasRegionIcon>();
                
                Color.RGBToHSV(hpuiRegion.defaultColor, out float h, out float s, out float v);
                float darkerV = Mathf.Clamp01(v * 0.8f); // 80% brightness, adjust as needed
                Color pressedColor = Color.HSVToRGB(h, s, darkerV);
                
                regionGameObject.name = $"HPUIRegion ({i},{j})";
                hpuiRegion.ID = new Vector2Int(i, j);
                hpuiRegion.basePoint = new Vector2(xDivisions[i], yDivisions[j]);
                hpuiRegion.area = new Vector2(xDivisions[i+1] - xDivisions[i], yDivisions[j+1] - yDivisions[j]);
                hpuiRegion.UIVisual = layer2Prefab;
                hpuiRegion.defaultColor =  interactionMappingColor[new Vector2Int(i, j)];
                hpuiRegion.pressedColor = pressedColor;
                hpuiRegion.canvasInteractable = HPUICanvas;
                hpuiRegion.parentTransform = layer2;
                hpuiRegion.canvasManager = this;
                hpuiRegion.interactionMapping = InteractionMapping;
                hpuiRegion.interactionMappingTransforms = interactionMappingTransforms;
                hpuiRegion.inputStreamTracker = actionInputStreamTracker;

                MicrogestureAction action = gestureActions.Find(action => action.startRegion == hpuiRegion.ID
                                                                          && action.endRegion == hpuiRegion.ID);
                IconAction iconAction = action.SwipeActions.OfType<IconAction>().FirstOrDefault();
                if (iconAction != null)
                {
                    iconAction.inputStreamTracker =  actionInputStreamTracker;
                    regionGameObject.GetComponentInChildren<SpriteRenderer>().sprite = iconAction.displayImage;
                }
                SetFollowTransform(hpuiRegion);
                hpuiRegions.Add(new Vector2Int(i,j), hpuiRegion);
            }
        }

        //Setup Layer 2
        
        foreach (MicrogestureAction action in gestureActions)
        {
            hpuiRegions[action.startRegion].gestureActions.Add(action);
        }
        foreach ((Vector2Int? ID, HPUICanvasRegion region) in hpuiRegions)
        {
            region.InitialiseUI();
        }
    }

    public void InitialiseRegions()
    {
        
    }

    
    public void SetPrompterLocation(Transform prompter)
    {
        TransformFollower transformFollower = prompter.GetComponent<TransformFollower>();
        if (InteractionMapping == InteractionMapping.Direct)
        {
            transformFollower.SetTarget(DirectAnchor);
            transformFollower.SetPositionOffset(offset);
            transformFollower.SetRotationOffset(rotOffset);
            transformFollower.SetScaleOffset(scaleOffset);
            // transformFollower.SetScaleOffset(Vector3.one * 0.0001f);
        }
        else
        {
            transformFollower.SetTarget(prompterAnchor);
            transformFollower.SetRotationOffset(offset2);
            transformFollower.SetRotationOffset(rotOffset2);
            transformFollower.SetScaleOffset(scaleOffset2);
        }
        
    }
    

    private void SetFollowTransform(HPUICanvasRegion canvasRegion)
    {
        if (InteractionMapping == InteractionMapping.Direct)
        {
            Vector2 regionCenterPoint = canvasRegion.basePoint + canvasRegion.area/2f + canvasRegion.centreOffset;
            Vector2Int centreIndex = HPUICanvasComponentUtils.CalculateColliderIndex(regionCenterPoint, HPUICanvas);
            Transform regionCentre = HPUICanvas.coordsToCollider[centreIndex].transform;
            canvasRegion.followTransform = regionCentre;
            TransformFollower transformFollower = canvasRegion.gameObject.AddComponent<TransformFollower>();
            transformFollower.SetTarget(canvasRegion.followTransform);
            transformFollower.SetRotationOffset(new Vector3(90,0,-90));
            transformFollower.SetScaleOffset(transform.lossyScale);
        }
        else
        {
            TransformFollower transformFollower = canvasRegion.gameObject.AddComponent<TransformFollower>();
            transformFollower.SetTarget(interactionMappingTransforms[canvasRegion.ID]);
            transformFollower.SetRotationOffset(new Vector3(0,-90,0));
            Transform targetTransform = interactionMappingTransforms[canvasRegion.ID].transform;
            canvasRegion.followTransform = targetTransform;
            Vector3 targetScale = new Vector3(targetTransform.lossyScale.x*targetTransform.parent.lossyScale.x, targetTransform.lossyScale.y*targetTransform.parent.lossyScale.y, targetTransform.lossyScale.z*targetTransform.parent.lossyScale.z); 
            transformFollower.SetScaleOffset(targetScale);
        }
    }

    public void ResetCanvasRegions()
    {
        Debug.Log($">>>>>>>>>>>{gameObject.name}");
        HPUICanvasRegion[] CanvasRegions = layer1.GetComponentsInChildren<HPUICanvasRegion>();
        
        foreach (HPUICanvasRegion region in CanvasRegions)
        {
            Destroy(region.gameObject);
            Debug.Log("Destroying regions");
        }
        xDivisions.Clear();
        yDivisions.Clear();
        hpuiRegions.Clear();
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
                TriggerStartSoundEffect();
                SetLayer1Active(false);
                HPUICanvasRegionIcon canvasIcon = (HPUICanvasRegionIcon) hpuiRegions[startRegion];
                canvasIcon.OnGestureStarted(canvasArgs);
                experimentManager.GestureStarted(canvasArgs);
                Session.instance.CurrentTrial.settings.SetValue(StudyLogs.GestureStartRegion, StudyLogs.VectorToRegionDict[canvasArgs.SwipeStartRegion.Value]);
                canvasTracker.RecordRow(gestureArgs, canvasArgs);
                break;
            case HPUICanvasState.Processing:
                currentRegion = GetInteractionRegion(canvasArgs.GesturePositions[^1]);
                canvasArgs.SwipeStartRegion = startRegion;
                canvasArgs.CurrentSwipeRegion = currentRegion;
                canvasArgs.SwipeEndRegion = endRegion;
                TriggerSoundEffect(currentRegion);
                hpuiRegions[startRegion].OnGestureOnGoing(canvasArgs);
                canvasTracker.RecordRow(gestureArgs, canvasArgs);
                break;
            case HPUICanvasState.Cancelled:
                foreach (KeyValuePair<Vector2Int?, HPUICanvasRegion> region in hpuiRegions)
                {
                    HPUICanvasRegionIcon regionIcon = (HPUICanvasRegionIcon) region.Value;
                    regionIcon.DisableUI();
                }
                SetLayer1Active(true);
                canvasTracker.RecordRow(gestureArgs, canvasArgs);
                break;
            case HPUICanvasState.Completed:
                endRegion = GetInteractionRegion(canvasArgs.GesturePositions[^1]);
                canvasArgs.SwipeStartRegion = startRegion;
                canvasArgs.CurrentSwipeRegion = endRegion;
                canvasArgs.SwipeEndRegion = endRegion;
                canvasTracker.RecordRow(gestureArgs, canvasArgs);
                Session.instance.CurrentTrial.settings.SetValue(StudyLogs.GestureEndRegion,
                    StudyLogs.VectorToRegionDict[canvasArgs.SwipeEndRegion.Value]);
                hpuiRegions[startRegion].OnGestureEnded(canvasArgs);
                TriggerSoundEffect(currentRegion);
                foreach (KeyValuePair<Vector2Int?, HPUICanvasRegion> region in hpuiRegions)
                {
                    HPUICanvasRegionIcon regionIcon = (HPUICanvasRegionIcon) region.Value;
                    regionIcon.DisableUI();
                }
                SetLayer1Active(true);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void TriggerStartSoundEffect()
    {
        SoundManager.Instance.PlaySound(clickOpen);
        SoundManager.Instance.EffectsSource.pitch = 1f;
    }

    public void TriggerSoundEffect(Vector2Int currentSwipeRegion)
    {
        if (prevSwipeRegion == currentSwipeRegion) return;
        counter = counter++ % 5;
        float pitch = Mathf.Lerp(1,3, counter/5f);
        SoundManager.Instance.EffectsSource.pitch = pitch;
        SoundManager.Instance.PlaySound(clickClose);
        SoundManager.Instance.ResetPitch(0.1f);
    }
    
    
    public virtual void SetLayer1Active(bool active)
    {
        foreach (Transform uiElement in layer1)
        {
            uiElement.gameObject.SetActive(active);
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