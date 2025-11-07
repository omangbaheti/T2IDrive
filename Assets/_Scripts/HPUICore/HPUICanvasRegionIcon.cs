using System;
using System.Collections.Generic;
using System.Linq;
using Experiment;
using ubco.ovilab.HPUI;
using ubco.ovilab.HPUI.Core;
using UnityEngine;
using UnityEngine.Rendering;

public class HPUICanvasRegionIcon : HPUICanvasRegion
{
    [SerializeField] public SerializedDictionary<Vector2Int, GameObject> layer2UIStartRegions = new();
    [SerializeField] public ActionInputStreamTracker inputStreamTracker;
    
    public override void InitialiseUI()
    {
        base.InitialiseUI();
        foreach (MicrogestureAction action in gestureActions)
        {
            HPUICanvasRegion startRegionIcon = canvasManager.HPUIRegions[action.startRegion]; 
            HPUICanvasRegion endRegionIcon = canvasManager.HPUIRegions[action.endRegion];
            Vector2 startRegionSpawnPoint = startRegionIcon.basePoint + startRegionIcon.area/2 + startRegionIcon.centreOffset;
            Vector2 endRegionSpawnPoint = endRegionIcon.basePoint + endRegionIcon.area/2 + startRegionIcon.centreOffset;
            Vector2Int startRegionSpawnColliderIndex = HPUICanvasComponentUtils.CalculateColliderIndex(startRegionSpawnPoint , canvasInteractable);
            Vector2Int endRegionSpawnColliderIndex = HPUICanvasComponentUtils.CalculateColliderIndex(endRegionSpawnPoint, canvasInteractable);
            // Debug.Log($"Follow Transform: >>>> {followTransform.name}");
            GameObject key = Instantiate(UIVisual, followTransform.position, Quaternion.identity, regionParent.transform);
            key.transform.localPosition = new(0,50f,0);
            key.transform.localRotation = Quaternion.Euler(90,90,0);
            layer2UIStartRegions.Add(action.endRegion, key);
            IconAction outputHandler = null;
            foreach (IHPUISwipeAction actionHandler in action.SwipeActions)
            {
                if (actionHandler is IconAction output)
                {
                    outputHandler = output;
                    outputHandler.inputStreamTracker = inputStreamTracker;
                }
            }
            
            if (outputHandler == null)
            {
                Debug.LogError("Swipe Actions do not have an Icon Layout. Please apply the right layout on Study2TrialManager");
                return;
            }

            key.GetComponentInChildren<SpriteRenderer>().sprite = action.startRegion == action.endRegion ? null : outputHandler.displayImage;
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
                
                transformFollower.SetRotationOffset(new Vector3(0,-90,0));
                Transform targetTransform = interactionMappingTransforms[action.endRegion].transform;
                Vector3 targetScale = new Vector3(targetTransform.lossyScale.x*targetTransform.parent.lossyScale.x, targetTransform.lossyScale.y*targetTransform.parent.lossyScale.y, targetTransform.lossyScale.z*targetTransform.parent.lossyScale.z); 
                transformFollower.SetScaleOffset(targetScale);
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
            HotSwapColor hotSwapColor = uiElement.transform.gameObject.GetComponent<HotSwapColor>();
            hotSwapColor.SetColor(defaultColor);
        }
        ActivateUIElements(false);
    }
    
    public override void DisableUI()
    {
        ActivateUIElements(false);
    }
}