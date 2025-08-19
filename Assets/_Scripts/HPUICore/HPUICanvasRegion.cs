using System;
using System.Collections.Generic;
using System.Linq;
using ubco.ovilab.HPUI;
using ubco.ovilab.HPUI.Core;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UXF;

public class HPUICanvasRegion : MonoBehaviour
{
    public Vector2Int ID;
    public Vector2 area;
    public List<MicrogestureAction> gestureActions = new();
    public Vector2 basePoint;
    public Vector2 centreOffset;

    [SerializeField] private IHPUICanvasUIManager canvasManager;
    [SerializeField] public GameObject StartRegionVisual;
    [FormerlySerializedAs("UIVisual")] [SerializeField] public GameObject EndRegionVisual;
    [SerializeField] public Color startColor;
    [SerializeField] public Color endColor;
    [SerializeField] public SerializedDictionary<Vector2Int, GameObject> layer2UIEndRegions = new();
    [SerializeField] public SerializedDictionary<Vector2Int, GameObject> layer2UIStartRegions = new();

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
        centrePoint = basePoint + new Vector2(area.x / 2f, area.y / 2f);

        foreach (MicrogestureAction action in gestureActions)
        {
            HPUICanvasRegion startRegion = canvasManager.HPUIRegions[action.startRegion];
            Vector2 startRegionSpawnPoint = startRegion.basePoint + startRegion.area/2 + startRegion.centreOffset;
            Vector2Int startRegionSpawnColliderIndex = HPUICanvasComponentUtils.CalculateColliderIndex(startRegionSpawnPoint , canvasInteractable);
            Transform startRegionSpawnCollider = canvasInteractable.coordsToCollider[startRegionSpawnColliderIndex].transform;
            GameObject key = Instantiate(StartRegionVisual, startRegionSpawnCollider.position, Quaternion.identity, startRegionSpawnCollider.transform);
            key.transform.localPosition = new(0,50f,0);
            key.transform.localRotation = Quaternion.Euler(90,90,0);
            layer2UIStartRegions.Add(action.endRegion, key);
            key.SetActive(false);

            HPUICanvasRegion endRegion = canvasManager.HPUIRegions[action.endRegion];
            Vector2 spawnPoint = endRegion.basePoint + endRegion.area/2 + endRegion.centreOffset;
            Vector2Int spawnColliderIndex = HPUICanvasComponentUtils.CalculateColliderIndex(spawnPoint, canvasInteractable);
            Transform spawnCollider = canvasInteractable.coordsToCollider[spawnColliderIndex].transform;
            if (action.startRegion == action.endRegion)
            {
                key = Instantiate(StartRegionVisual, spawnCollider.position, Quaternion.identity, spawnCollider.transform);
            }
            else
            {

                key = Instantiate(EndRegionVisual, spawnCollider.position, Quaternion.identity, spawnCollider.transform);
            }
            key.transform.localPosition = new(0,50f,0);
            key.transform.localRotation = Quaternion.Euler(90,90,0);
            layer2UIEndRegions.Add(action.endRegion, key);
            key.SetActive(false);
        }
    }

    public virtual void OnGestureStarted(HPUICanvasEventArgs canvasArgs)
    {
        // Debug.Log("Getting Here???????");
        foreach (KeyValuePair<Vector2Int, GameObject> uiElement in layer2UIEndRegions)
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
            Debug.Log($"Getting Here ig {e}");
        }
        endRegion = StudyLogs.RegionToVectorDict[endRegionName];
        layer2UIEndRegions[endRegion].SetActive(true);
        // Debug.Log("========== " + endRegionName.ToString());
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
        foreach (KeyValuePair<Vector2Int, GameObject> uiElement in layer2UIEndRegions)
        {
            uiElement.Value.SetActive(false);
        }
    }

    public virtual void OnGestureOnGoing(HPUICanvasEventArgs canvasArgs)
    {
        // Debug.Log("OnGestureOnGoing");
        if (canvasArgs.CurrentSwipeRegion == endRegion)
        {
            HotSwapColor hotSwapColor = layer2UIEndRegions[endRegion].transform.GetChild(0).GetComponent<HotSwapColor>();
            if (layer2UIEndRegions[endRegion].gameObject.activeSelf == false)
            {
                // layer2UIElements[endRegion].gameObject.SetActive(true);
                Debug.LogWarning("This shouldnt happen");

            }
            // Debug.Log("Chanigng Color to greeen");
            Debug.Log($" >>> Within HPUI Region: {canvasArgs.CurrentSwipeRegion}");
            hotSwapColor.SetColor(Color.green);
        }
        else
        {
            HotSwapColor hotSwapColor = layer2UIEndRegions[endRegion].transform.GetChild(0).GetComponent<HotSwapColor>();
            if (layer2UIEndRegions[endRegion].gameObject.activeSelf == false)
            {
                // layer2UIElements[endRegion].gameObject.SetActive(true);
                Debug.LogWarning("This shouldnt happen");
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
        foreach (KeyValuePair<Vector2Int, GameObject> gameObject in layer2UIEndRegions)
        {
            HotSwapColor hotSwapColor = gameObject.Value.transform.GetChild(0).gameObject.GetComponent<HotSwapColor>();
            hotSwapColor.SetColor(Color.red);
        }
        ActivateUIElements(false);
    }
}