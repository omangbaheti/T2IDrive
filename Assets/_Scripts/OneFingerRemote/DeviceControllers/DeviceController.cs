using ubco.ovilab.HPUI;
using ubco.ovilab.HPUI.Core;
using UnityEngine;
using UnityEngine.Events;

public class DeviceController : MonoBehaviour, IHPUISwipeAction
{
    public UnityEvent<HPUICanvasEventArgs> OnSwipeStarted { get; }
    public UnityEvent<HPUICanvasEventArgs> OnSwipeCompleted { get; }
    public void GestureStarted(HPUICanvasEventArgs canvasArgs)
    {
        throw new System.NotImplementedException();
    }

    public void GestureCompleted(HPUICanvasEventArgs canvasArgs)
    {
        throw new System.NotImplementedException();
    }
}
