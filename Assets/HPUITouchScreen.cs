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
    [SerializeField] private EventSystem eventSystem;
    
    private GraphicRaycaster UIraycaster;
    private void OnEnable()
    {
        hpuiInteractable = GetComponent<HPUIBaseInteractable>();
        hpuiInteractable.GestureEvent.AddListener(HandleTapEvent);
        SetupCanvas();
    }
    
    private void OnDisable()
    {
        hpuiInteractable.GestureEvent.RemoveListener(HandleTapEvent);
    }

    private void SetupCanvas()
    {
        if (targetCanvas == null || eventSystem == null)
        {
            Debug.LogError($"Canvas and event system are required on :{gameObject.name}");
        }
        if (!targetCanvas.TryGetComponent(out UIraycaster))
        {
            targetCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void HandleTapEvent(HPUIGestureEventArgs arg0)
    {
        Vector2 worldPosition = arg0.Position;
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetCanvas.transform as RectTransform,
            screenPosition,
            Camera.main,
            out Vector2 localPos
        );
        
        switch (arg0.State)
        {
            case HPUIGestureState.Started:
                TryPressButton(screenPosition, true);
                break;
            case HPUIGestureState.Updated:
                TryPressButton(screenPosition, true);
                break;
            case HPUIGestureState.Stopped:
                TryPressButton(screenPosition, false);
                break;
            case HPUIGestureState.Invalid or HPUIGestureState.Canceled:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void TryPressButton(Vector2 screenPosition, bool isDown)
    {
        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = screenPosition
        };
        
        Debug.Log(pointerData.position);
        List<RaycastResult> results = new List<RaycastResult>();
        eventSystem.RaycastAll(pointerData, results);


        foreach (RaycastResult result in results)
        {
            Button button = result.gameObject.GetComponent<Button>();
            if (button != null)
            {
                if (isDown)
                {
                    // Simulate hover + press
                    ExecuteEvents.Execute(button.gameObject, pointerData, ExecuteEvents.pointerEnterHandler);
                    ExecuteEvents.Execute(button.gameObject, pointerData, ExecuteEvents.pointerDownHandler);
                }
                else
                {
                    // Simulate release + click
                    ExecuteEvents.Execute(button.gameObject, pointerData, ExecuteEvents.pointerUpHandler);
                    ExecuteEvents.Execute(button.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
                    ExecuteEvents.Execute(button.gameObject, pointerData, ExecuteEvents.pointerExitHandler);
                }

                break;
            }
        }
    }

   
}
