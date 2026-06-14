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
    [SerializeField] private GameObject debugCursor;
    
    private EventSystem eventSystem;
    private GraphicRaycaster UIraycaster;
    private RectTransform canvasRectTransform;
    private Button initialButton;
    private Button currentButton;
    private Camera interfaceEventCamera;
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
        interfaceEventCamera = canvasRectTransform.GetComponent<Canvas>().worldCamera;
    }
    
    
    // ReSharper disable Unity.PerformanceAnalysis
    private void HandleTapEvent(HPUIGestureEventArgs arg)
    {
        // Debug.Log($"Interactable Point = {arg.CurrentTrackingInteractablePoint} Position = {arg.Position}");
        Vector3 screenPosition = debugCursor.transform.position;
        Debug.Log(screenPosition);
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

    private void HandleGestureStart(Vector3 screenPosition, HPUIGestureEventArgs arg)
    {
        Button button = GetButtonAtPosition(screenPosition);
        if (!button)
        {
            return;
        }
        initialButton = button;
        currentButton = button;
        Debug.Log($"===1111 {arg.interactableObject.transform.name} {screenPosition:F4} {initialButton} {currentButton?.name}");
        SendPointerEvents(screenPosition, button, pointerDown: true, pointerEnter: true);
    }

    private void HandleGestureUpdate(Vector3 screenPosition, HPUIGestureEventArgs arg)
    {
        if (!initialButton) return;
        Button button = GetButtonAtPosition(screenPosition);
        Debug.Log($"---== {button}");
        if (currentButton == button) return;
        currentButton = button;
        if (currentButton && currentButton == initialButton)
        {
            SendPointerEvents(screenPosition, currentButton, pointerDown: true, pointerEnter: true);
        }
    }

    private void HandleGestureEnd(Vector3 screenPosition, HPUIGestureEventArgs arg)
    {
        if (!initialButton) return;
        
        Button button = GetButtonAtPosition(screenPosition);
        SendPointerEvents(screenPosition, initialButton, pointerUp: true, pointerExit: true);
        
        if (button == initialButton)
        {
            SendPointerEvents(screenPosition, initialButton, pointerClick: true);
        }
        ResetInteraction();
    }

    private void CancelGesture()
    {
        if (initialButton)
        {
            SendPointerEvents(Vector2.negativeInfinity, initialButton, pointerUp: true, pointerExit: true);
        }
        ResetInteraction();
    }

    private Button GetButtonAtPosition(Vector2 screenPosition)
    {
       Vector3 worldPosition = debugCursor.transform.position;
       Debug.Log($"{worldPosition}:{interfaceEventCamera.WorldToScreenPoint(worldPosition)}");
        PointerEventData pointerData = new(eventSystem)
        {
            position = interfaceEventCamera.WorldToScreenPoint(worldPosition),
        };
        List<RaycastResult> results = new();
        UIraycaster.Raycast(pointerData, results);
        
        foreach (RaycastResult result in results)
        {
            Button button = result.gameObject.GetComponent<Button>();
            if (button)
            {
                Debug.Log(button.name);
                return button;
            }

        }
        
        return null;
    }

    private void SendPointerEvents(Vector2 screenPoint, Button button, bool pointerEnter = false, bool pointerDown = false, 
                                  bool pointerUp = false, bool pointerExit = false, bool pointerClick = false)
    {
        if (!button)
        {
            Debug.Log("No button ");
            return;
        }
        
        Vector3 worldPosition = button.transform.position;
        PointerEventData pointerData = new(eventSystem)
        {
            position = interfaceEventCamera.WorldToScreenPoint(worldPosition),
        };
        Debug.Log($">>>> {worldPosition}");
        
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
    
    private Vector2 ConvertHitPointToCanvasCoordinates(Vector3 hitPoint, IHPUIInteractable interactable)
    {
        Vector3 interactableExtents = interactable.colliders[0].bounds.size/2;
        Debug.Log($"Extents = {interactableExtents}");
        Vector3 normalizedPoint = new(hitPoint.x / interactableExtents.x * 0.5f, hitPoint.y / interactableExtents.y * 0.5f, hitPoint.z / interactableExtents.z * 0.5f);
        Vector2 canvasPosition = new(normalizedPoint.x * canvasRectTransform.sizeDelta.x, normalizedPoint.y * canvasRectTransform.sizeDelta.y);
        return canvasPosition;
    }
    
    
}
