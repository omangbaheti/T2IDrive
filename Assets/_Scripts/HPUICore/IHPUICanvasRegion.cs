using ubco.ovilab.HPUI.Core;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;

public interface IHPUICanvasRegion
{
    public void OnCanvasGesture(HPUIGestureEventArgs gestureEvent, HPUICanvasEventArgs canvasEventArgs);
}
