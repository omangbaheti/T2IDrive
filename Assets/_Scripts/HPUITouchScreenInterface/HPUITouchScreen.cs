using System;
using System.Collections.Generic;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HPUITouchScreen : MonoBehaviour//, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private HPUIBaseInteractable hpuiInteractable;
    [SerializeField] private Canvas targetCanvas;
    
    private EventSystem eventSystem;
    private GraphicRaycaster UIraycaster;
    private RectTransform canvasRectTransform;
    private Button initialButton;
    private Button currentButton;

    private void OnEnable()
    {
        hpuiInteractable.GestureEvent.AddListener(HandleTapEvent);
        SetupCanvas();
    }
    
    private void OnDisable()
    {
        hpuiInteractable.GestureEvent.RemoveListener(HandleTapEvent);
    }

    private void SetupCanvas()
    {
        
        if (targetCanvas == null)
        {
            Debug.LogError($"Canvas and event system are required on :{gameObject.name}");
        }
        if (!targetCanvas.TryGetComponent(out UIraycaster))
        {
            UIraycaster = targetCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }
        
        eventSystem = EventSystem.current;
        canvasRectTransform = targetCanvas.transform as RectTransform;
    }
    
    
    // ReSharper disable Unity.PerformanceAnalysis
    private void HandleTapEvent(HPUIGestureEventArgs arg)
    {
        // Debug.Log($"Interactable Point = {arg.CurrentTrackingInteractablePoint} Position = {arg.Position}");
        Vector2 screenPosition = ConvertHitPointToCanvasCoordinates(arg.CurrentTrackingInteractablePoint, arg.CurrentTrackingInteractable);
        
        switch (arg.State)
        {
            case HPUIGestureState.Started:
                HandleGestureStart(screenPosition, arg);
                break;
            case HPUIGestureState.Updated:
                HandleGestureUpdate(screenPosition, arg);
                break;
            case HPUIGestureState.Stopped:
                HandleGestureEnd(screenPosition, arg);
                break;
            case HPUIGestureState.Canceled or HPUIGestureState.Invalid :
                CancelGesture();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void HandleGestureStart(Vector2 screenPosition, HPUIGestureEventArgs arg)
    {
        Button button = GetButtonAtPosition(screenPosition);
        if (!button) return;
        initialButton = button;
        currentButton = button;
        SendPointerEvents(button, pointerDown: true, pointerEnter: true);
    }

    private void HandleGestureUpdate(Vector2 screenPosition, HPUIGestureEventArgs arg)
    {
        if (!initialButton) return;
        Button button = GetButtonAtPosition(screenPosition);
        if (currentButton == button) return;
        currentButton = button;
        if (currentButton && currentButton == initialButton)
        {
            SendPointerEvents(currentButton, pointerDown: true, pointerEnter: true);
        }
    }

    private void HandleGestureEnd(Vector2 screenPosition, HPUIGestureEventArgs arg)
    {
        if (!initialButton) return;
        
        Button button = GetButtonAtPosition(screenPosition);
        SendPointerEvents(initialButton, pointerUp: true, pointerExit: true);
        
        if (button == initialButton)
        {
            SendPointerEvents(initialButton, pointerClick: true);
        }
        ResetInteraction();
    }

    private void CancelGesture()
    {
        if (initialButton)
        {
            SendPointerEvents(initialButton, pointerUp: true, pointerExit: true);
        }
        ResetInteraction();
    }

    private Button GetButtonAtPosition(Vector2 screenPosition)
    {
        Vector3 worldPosition = canvasRectTransform.TransformPoint(screenPosition);
        PointerEventData pointerData = new(eventSystem)
        {
            position = Camera.main.WorldToScreenPoint(worldPosition),
        };
        
        List<RaycastResult> results = new();
        UIraycaster.Raycast(pointerData, results);
        
        foreach (RaycastResult result in results)
        {
            Button button = result.gameObject.GetComponent<Button>();
            if (button)
                return button;
        }
        
        return null;
    }

    private void SendPointerEvents(Button button, bool pointerEnter = false, bool pointerDown = false, 
                                  bool pointerUp = false, bool pointerExit = false, bool pointerClick = false)
    {
        if (!button) return;
        
        Vector3 worldPosition = button.transform.position;
        PointerEventData pointerData = new(eventSystem)
        {
            position = Camera.main.WorldToScreenPoint(worldPosition),
        };
        
        if (pointerEnter)
            ExecuteEvents.Execute(button.gameObject, pointerData, ExecuteEvents.pointerEnterHandler);
        if (pointerDown)
            ExecuteEvents.Execute(button.gameObject, pointerData, ExecuteEvents.pointerDownHandler);
        if (pointerUp)
            ExecuteEvents.Execute(button.gameObject, pointerData, ExecuteEvents.pointerUpHandler);
        if (pointerClick)
            ExecuteEvents.Execute(button.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
        if (pointerExit)
            ExecuteEvents.Execute(button.gameObject, pointerData, ExecuteEvents.pointerExitHandler);
    }

    private void ResetInteraction()
    {
        initialButton = null;
        currentButton = null;
    }
    
    private Vector2 ConvertHitPointToCanvasCoordinates(Vector2 hitPoint, IHPUIInteractable interactable)
    {
        Vector3 interactableExtents = interactable.colliders[0].bounds.extents;
        Vector2 normalizedPoint = new(hitPoint.x / interactableExtents.x * 0.5f, hitPoint.y / interactableExtents.y * 0.5f);
        Vector2 canvasPosition = new(normalizedPoint.x * canvasRectTransform.sizeDelta.x, normalizedPoint.y * canvasRectTransform.sizeDelta.y);
        return canvasPosition;
    }
    
}
