using System.Collections.Generic;
using System.Linq;
using TMPro;
using ubco.ovilab.HPUI;
using ubco.ovilab.HPUI.Core;
using UnityEngine;
using UnityEngine.Rendering;

public class HPUICanvasRegionTextInput : HPUICanvasRegion
{
    [SerializeField] public GameObject UIVisual;
    [SerializeField] public SerializedDictionary<Vector2Int, GameObject> layer2UIStartRegions = new();
    private Transform regionParent;
    public override void InitialiseUI(float targetSize)
    {
        base.InitialiseUI(targetSize);
        foreach (MicrogestureAction action in gestureActions)
        {
            HPUICanvasRegion startRegionTextInput = canvasManager.HPUIRegions[action.startRegion];
            HPUICanvasRegion endRegionTextInput = canvasManager.HPUIRegions[action.endRegion];
            Vector2 startRegionSpawnPoint = startRegionTextInput.basePoint + startRegionTextInput.area/2 + startRegionTextInput.centreOffset;
            Vector2 endRegionSpawnPoint = endRegionTextInput.basePoint + endRegionTextInput.area/2 + endRegionTextInput.centreOffset;
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

    public override void OnGestureStarted(HPUICanvasEventArgs canvasArgs)
    {
        ActivateUIElements(true);
    }
    public override void OnGestureOnGoing(HPUICanvasEventArgs canvasArgs)
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
    
    public override void OnGestureEnded(HPUICanvasEventArgs canvasArgs)
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
    public override void DisableUI()
    {
        ActivateUIElements(false);
    }

    public override void ActivateUIElements(bool active)
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