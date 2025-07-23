
using System;
using System.Collections.Generic;
using System.Linq;
using ubco.ovilab.HPUI;
using ubco.ovilab.HPUI.Core;
using Unity.VisualScripting;
using UnityEngine;
using UXF;

public class HPUICanvasRegion : MonoBehaviour
{
    public Vector2Int ID;
    public Vector2 area;
    public List<MicrogestureAction> gestureActions = new();
    public Vector2 basePoint;
    public Vector2 centreOffset;

    [SerializeField] private IHPUICanvasUIManager canvasManager;
    [SerializeField] public GameObject UIVisual;
    [SerializeField] public Dictionary<Vector2Int, GameObject> layer2UIElements = new();

    private Vector2 centrePoint;
    private Vector2Int endRegion;
    private HPUIMultiFingerCanvas canvasInteractable;

    private void Awake()
    {
        canvasManager = GetComponent<IHPUICanvasUIManager>();
        canvasInteractable = canvasManager.HPUICanvas;
    }


    public void InitialiseUI()
    {
        canvasManager = GetComponent<IHPUICanvasUIManager>();
        canvasInteractable = canvasManager.HPUICanvas;
        centrePoint = basePoint + new Vector2(area.x / 2f, area.y / 2f);

        foreach (MicrogestureAction action in gestureActions)
        {
            HPUICanvasRegion endRegion = canvasManager.HPUIRegions[action.endRegion];
            Vector2 spawnPoint = endRegion.basePoint + endRegion.area/2 + endRegion.centreOffset;
            Vector2Int spawnColliderIndex = HPUICanvasComponentUtils.CalculateColliderIndex(spawnPoint, canvasInteractable);
            Transform spawnCollider = canvasInteractable.coordsToCollider[spawnColliderIndex].transform;
            GameObject key = Instantiate(UIVisual, spawnCollider.position, Quaternion.identity, spawnCollider.transform);
            key.transform.localPosition = new(0,50f,0);
            key.transform.localRotation = Quaternion.Euler(90,90,0);
            layer2UIElements.Add(action.endRegion, key);
            key.SetActive(false);
        }
    }

    public virtual void OnGestureStarted(HPUICanvasEventArgs canvasArgs)
    {
        foreach (KeyValuePair<Vector2Int, GameObject> uiElement in layer2UIElements)
        {
            HotSwapColor hotSwapColor = uiElement.Value.transform.GetChild(0).gameObject.GetComponent<HotSwapColor>();
            hotSwapColor.SetColor(Color.red);
        }

        FingerRegions endRegionName;
        try
        {
            endRegionName = (FingerRegions)Session.instance.CurrentTrial.settings.GetObject(StudyLogs.EndRegion);
        }
        catch (NoSuchTrialException e)
        {
            endRegionName = ComfortStudyExperimentManager.Instance.endRegion;
        }
        endRegion = StudyLogs.RegionToVectorDict[endRegionName];
        layer2UIElements[endRegion].SetActive(true);
        foreach (MicrogestureAction gesture in gestureActions)
        {
            if (canvasArgs.SwipeStartRegion == gesture.startRegion)
            {
                Debug.Log($"Starting region {canvasArgs.SwipeStartRegion} MicroGestureStartRegion {gesture.startRegion}");
                Debug.Log($"Ending region {canvasArgs.SwipeEndRegion} MicroGestureEndRegion {gesture.endRegion}");
                foreach (IHPUISwipeAction action in gesture.SwipeActions)
                {
                    action.GestureStarted(canvasArgs);
                }
            }
        }
    }

    public virtual void DisableUI()
    {
        ActivateUIElements(false);
    }

    public virtual void ActivateUIElements(bool active)
    {
        foreach (KeyValuePair<Vector2Int, GameObject> uiElement in layer2UIElements)
        {
            uiElement.Value.SetActive(false);
        }
    }

    public virtual void OnGestureOnGoing(HPUICanvasEventArgs canvasArgs)
    {
        Debug.Log("OnGestureOnGoing");
        if (canvasArgs.CurrentSwipeRegion == endRegion)
        {
            HotSwapColor hotSwapColor = layer2UIElements[endRegion].transform.GetChild(0).GetComponent<HotSwapColor>();
            if (layer2UIElements[endRegion].gameObject.activeSelf == false)
            {
                Debug.LogError("HUHHHHHH");
            }
            Debug.Log("Chanigng Color to greeen");
            hotSwapColor.SetColor(Color.green);
        }
        else
        {
            HotSwapColor hotSwapColor = layer2UIElements[endRegion].transform.GetChild(0).GetComponent<HotSwapColor>();
            if (layer2UIElements[endRegion].gameObject.activeSelf == false)
            {
                Debug.LogError("HUHHHHHH");
            }
            hotSwapColor.SetColor(Color.red);

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
        foreach (KeyValuePair<Vector2Int, GameObject> gameObject in layer2UIElements)
        {
            HotSwapColor hotSwapColor = gameObject.Value.transform.GetChild(0).gameObject.GetComponent<HotSwapColor>();
            hotSwapColor.SetColor(Color.red);
        }
        ActivateUIElements(false);
    }
}




