
using System;
using System.Collections.Generic;
using Experiment;
using ubco.ovilab.HPUI.Core;
using UnityEngine;

public abstract class HPUICanvasRegion : MonoBehaviour
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
    public ActionInputStreamTracker actionInputStreamTracker;
    public Dictionary<Vector2Int, GameObject> layer2UIElements = new();
    private Vector2 centrePoint;
    private Vector2Int endRegion;
    public HPUIMultiFingerCanvas canvasInteractable;
    protected Transform regionParent;
    public InteractionMapping interactionMapping;
    public Dictionary<Vector2Int, Transform> interactionMappingTransforms = new();
    public virtual void InitialiseUI()
    {
        layer2UIElements.Clear();
        centrePoint = basePoint + new Vector2(area.x / 2f, area.y / 2f);
        canvasInteractable = canvasManager.HPUICanvas;
        regionParent = new GameObject().transform;
        regionParent.parent = parentTransform;
        regionParent.name = $"HPUIRegion ({ID.x},{ID.y})";
    }

    public virtual void ActivateUIElements(bool active)
    {
        foreach (Transform uiElement in regionParent)
        {
            uiElement.gameObject.SetActive(active);
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
        
    }


}