using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using ubco.ovilab.HPUI;
using ubco.ovilab.HPUI.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UXF;

public class HPUICanvasRegion : MonoBehaviour
{
    public Vector2Int ID;
    public Vector2 area;
    public List<MicrogestureAction> gestureActions = new();
    public Vector2 basePoint;
    public Vector2 centreOffset;
    public Color pressedColor;
    public Color defaultColor;
    public Transform followTransform;
    public Transform parentTransform;
    public IHPUICanvasUIManager canvasManager;
    [SerializeField] public GameObject UIVisual;
    [SerializeField] public Color startColor;
    [SerializeField] public Color endColor;
    [SerializeField] public SerializedDictionary<Vector2Int, GameObject> layer2UIEndRegions = new();
    [SerializeField] public SerializedDictionary<Vector2Int, GameObject> layer2UIStartRegions = new();
    public Dictionary<Vector2Int, GameObject> layer2UIElements = new();
    private Vector2 centrePoint;
    private Vector2Int endRegion;
    public HPUIMultiFingerCanvas canvasInteractable;
    private Transform regionParent;
    public InteractionMapping interactionMapping;
    public Dictionary<Vector2Int, Transform> interactionMappingTransforms = new();
    public void InitialiseUI()
    {
        layer2UIElements.Clear();
        centrePoint = basePoint + new Vector2(area.x / 2f, area.y / 2f);
        canvasInteractable = canvasManager.HPUICanvas;
        regionParent = new GameObject().transform;
        regionParent.parent = parentTransform;
        regionParent.name = $"HPUIRegion ({ID.x},{ID.y})";
        foreach (MicrogestureAction action in gestureActions)
        {
            HPUICanvasRegion startRegion = canvasManager.HPUIRegions[action.startRegion];
            HPUICanvasRegion endRegion = canvasManager.HPUIRegions[action.endRegion];
            Vector2 startRegionSpawnPoint = startRegion.basePoint + startRegion.area/2 + startRegion.centreOffset;
            Vector2 endRegionSpawnPoint = endRegion.basePoint + endRegion.area/2 + endRegion.centreOffset;
            Vector2Int startRegionSpawnColliderIndex = HPUICanvasComponentUtils.CalculateColliderIndex(startRegionSpawnPoint , canvasInteractable);
            Vector2Int endRegionSpawnColliderIndex = HPUICanvasComponentUtils.CalculateColliderIndex(endRegionSpawnPoint, canvasInteractable);
            Debug.Log($"Follow Transform: >>>> {followTransform.name}");
            GameObject key = Instantiate(UIVisual, followTransform.position, Quaternion.identity, regionParent.transform);
            key.transform.localPosition = new(0,50f,0);
            key.transform.localRotation = Quaternion.Euler(90,90,0);
            layer2UIStartRegions.Add(action.endRegion, key);
            key.SetActive(false);
            
            CharacterOutput outputHandler = null;
            foreach (IHPUISwipeAction actionHandler in action.SwipeActions)
            {
                if (actionHandler is CharacterOutput output)
                {
                    outputHandler = output;
                }
            }

            if (outputHandler == null)
            {
                Debug.LogError("Swipe Actions does not have a character output action. Apply the right layout on Study2TrialManager");
                return;
            }
            
            key.GetComponentInChildren<TextMeshPro>().text = outputHandler.outputKey;
            if (interactionMapping == InteractionMapping.Direct)
            {
                followTransform = canvasInteractable.coordsToCollider[endRegionSpawnColliderIndex].transform;
                TransformFollower transformFollower = key.gameObject.AddComponent<TransformFollower>();
                transformFollower.SetTarget(followTransform);
                transformFollower.SetRotationOffset(new Vector3(90,0,-90));
                transformFollower.SetScaleOffset(canvasInteractable.transform.lossyScale);
            }
            else
            {
                followTransform = interactionMappingTransforms[action.endRegion];
                TransformFollower transformFollower = key.gameObject.AddComponent<TransformFollower>();
                transformFollower.SetTarget(interactionMappingTransforms[action.endRegion]);
                transformFollower.SetRotationOffset(new Vector3(90,0,0));
                Transform targetTransform = interactionMappingTransforms[action.endRegion].transform;
                Vector3 targetScale = new Vector3(targetTransform.lossyScale.x/targetTransform.parent.lossyScale.x, targetTransform.lossyScale.y/targetTransform.parent.lossyScale.y, targetTransform.lossyScale.z/targetTransform.parent.lossyScale.z); 
                transformFollower.SetScaleOffset(targetScale*2);
            }
            layer2UIElements.Add(action.endRegion, key);
            key.SetActive(false);
        }
    }

    public virtual void OnGestureStarted(HPUICanvasEventArgs canvasArgs)
    {
        ActivateUIElements(true);
    }
    public virtual void OnGestureOnGoing(HPUICanvasEventArgs canvasArgs)
    {
        foreach (KeyValuePair<Vector2Int, GameObject> uiElement in layer2UIElements)
        {
            HotSwapColor hotSwapColor = uiElement.Value.GetComponentInChildren<HotSwapColor>();
            if (canvasArgs.CurrentSwipeRegion == uiElement.Key)
            {
                hotSwapColor.SetColor(pressedColor);
            }
            else
            {
                hotSwapColor.SetColor(defaultColor);
            }
        }
    }
    
    public virtual void OnGestureEnded(HPUICanvasEventArgs canvasArgs)
    {
        
        foreach (MicrogestureAction gesture in gestureActions.Where
                 (gesture => canvasArgs.SwipeStartRegion == gesture.startRegion &&
                             canvasArgs.SwipeEndRegion == gesture.endRegion))
        {
            foreach (IHPUISwipeAction action in gesture.SwipeActions)
            {
                action.GestureCompleted(canvasArgs);
            }
        }
        foreach (Transform uiElement in regionParent)
        {
            HotSwapColor hotSwapColor = uiElement.transform.GetChild(0).gameObject.GetComponent<HotSwapColor>();
            hotSwapColor.SetColor(defaultColor);
        }
        ActivateUIElements(false);
    }

    public virtual void DisableUI()
    {
        ActivateUIElements(false);
    }

    public virtual void ActivateUIElements(bool active)
    {
        foreach (Transform uiElement in regionParent)
        {
            uiElement.gameObject.SetActive(active);
        }
    }

    private void OnDestroy()
    {
        foreach (Transform region in regionParent)
        {
            Destroy(region.gameObject);
        }
        Destroy(regionParent);
    }
}