using System.Collections.Generic;
using ubco.ovilab.HPUI.Core;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;

public interface IHPUICanvasUIManager
{
    public List<float> XDivisions { get;}
    public List<float> YDivisions { get;}
    public HPUIMultiFingerCanvas HPUICanvas {get;}
    public SerializedDictionary<Vector2Int?, HPUICanvasRegion> HPUIRegions { get;}
    public void SpawnCanvasRegions();
    public void InitialiseRegions();
    public void HandleCanvasGesture(HPUIGestureEventArgs gestureArgs, HPUICanvasEventArgs canvasArgs);
}
