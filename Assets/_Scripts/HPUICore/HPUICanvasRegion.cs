
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using ubco.ovilab.HPUI;
using ubco.ovilab.HPUI.Core;
using Unity.VisualScripting;
using UnityEngine;
using UXF;


[RequireComponent(typeof(SyncTransform))]
public class HPUICanvasRegion : MonoBehaviour
{
    public Vector2Int ID;
    public Vector2 area;
    public List<MicrogestureAction> gestureActions = new();
    public Vector2 basePoint;
    public Vector2 centreOffset;
    public Color pressedColor;
    public Color defaultColor;

    [SerializeField] public IHPUICanvasUIManager canvasManager;
    [SerializeField] public GameObject UIVisual;
    [SerializeField] public Dictionary<Vector2Int, GameObject> layer2UIElements = new();

    private Vector2 centrePoint;
    private HPUIMultiFingerCanvas canvasMultiFinger;

    public void InitialiseUI()
    {
        Debug.Log("InitialiseUI");
        centrePoint = basePoint + new Vector2(area.x / 2f, area.y / 2f);
        foreach (KeyValuePair<Vector2Int, GameObject> uiElement in layer2UIElements)
        {
            Destroy(uiElement.Value);
        }
        layer2UIElements.Clear();
        foreach (MicrogestureAction action in gestureActions)
        {
            HPUICanvasRegion endRegion = canvasManager.HPUIRegions[action.endRegion];
            Vector2 spawnPoint = endRegion.basePoint + endRegion.area/2 + endRegion.centreOffset;
            Vector2Int spawnColliderIndex = HPUICanvasComponentUtils.CalculateColliderIndex(spawnPoint, canvasMultiFinger);
            Transform spawnCollider = canvasMultiFinger.coordsToCollider[spawnColliderIndex].transform;
            GameObject key = Instantiate(UIVisual, spawnCollider.position, Quaternion.identity, spawnCollider.transform);
            key.transform.localPosition = new(0,50f,0);
            key.transform.localRotation = Quaternion.Euler(90,90,0);
        }
    }

    private void OnDestroy()
    {
        gestureActions.Clear();
    }

    void OnDisable()
    {
        gestureActions.Clear();
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
        Debug.Log("OnGestureEnded");
        foreach (MicrogestureAction gesture in gestureActions.Where
                 (gesture => canvasArgs.SwipeStartRegion == gesture.startRegion &&
                             canvasArgs.SwipeEndRegion == gesture.endRegion))
        {
            foreach (IHPUISwipeAction action in gesture.SwipeActions)
            {
                action.GestureCompleted(canvasArgs);
            }
        }
        foreach (KeyValuePair<Vector2Int, GameObject> uiElement in layer2UIElements)
        {
            HotSwapColor hotSwapColor = uiElement.Value.transform.GetChild(0).gameObject.GetComponent<HotSwapColor>();
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
        foreach (KeyValuePair<Vector2Int, GameObject> uiElement in layer2UIElements)
        {
            uiElement.Value.SetActive(active);
        }
    }



}




