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

    private Vector2 centrePoint;
    private Vector2Int endRegion;
    public HPUIMultiFingerCanvas canvasInteractable;

    public void InitialiseUI()
    {
        centrePoint = basePoint + new Vector2(area.x / 2f, area.y / 2f);
        canvasInteractable = canvasManager.HPUICanvas;
        GameObject regionParent = Instantiate(new  GameObject(), parentTransform);
        regionParent.name = $"HPUIRegion ({ID.x},{ID.y})";
        foreach (MicrogestureAction action in gestureActions)
        {
            HPUICanvasRegion startRegion = canvasManager.HPUIRegions[action.startRegion];
            Vector2 startRegionSpawnPoint = startRegion.basePoint + startRegion.area/2 + startRegion.centreOffset;
            Vector2Int startRegionSpawnColliderIndex = HPUICanvasComponentUtils.CalculateColliderIndex(startRegionSpawnPoint , canvasInteractable);
            followTransform = canvasInteractable.coordsToCollider[startRegionSpawnColliderIndex].transform;
            
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

            // if (outputHandler == null)
            // {
            //     Debug.LogError("Swipe Actions does not have a character output action. Apply the right layout on Study2TrialManager");
            //     return;
            // }
            //
            // key.GetComponent<TextMeshPro>().text = outputHandler.outputKey;
            key.transform.localPosition = new(0,50f,0);
            key.transform.localRotation = Quaternion.Euler(90,90,0);
            key.SetActive(false);
        }
    }

    public virtual void OnGestureStarted(HPUICanvasEventArgs canvasArgs)
    {
        
    }
    public virtual void OnGestureOnGoing(HPUICanvasEventArgs canvasArgs)
    {
        
    }
    public virtual void OnGestureEnded(HPUICanvasEventArgs canvasArgs)
    {
       
    }

    public virtual void DisableUI()
    {
        ActivateUIElements(false);
    }

    public virtual void ActivateUIElements(bool active)
    {
        foreach (KeyValuePair<Vector2Int, GameObject> uiElement in layer2UIEndRegions)
        {
            uiElement.Value.SetActive(false);
        }
    }  
}