using System.Collections.Generic;
using ubco.ovilab.HPUI.Core;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;

public class HPUICanvasManager : MonoBehaviour, IHPUICanvasUIManager
{
    public List<float> XDivisions => xDivisions;
    public List<float> YDivisions => yDivisions;
    
    public HPUIMultiFingerCanvas HPUICanvas { get; set; }
    public SerializedDictionary<Vector2Int?, HPUICanvasRegion> HPUIRegions => hpuiRegions;

    private List<float> xDivisions;
    private List<float> yDivisions;
    [SerializeField] private SerializedDictionary<Vector2Int?, HPUICanvasRegion> hpuiRegions = new();

    public void SpawnCanvasRegions()
    {
        
    }

    public void InitialiseRegions()
    {
        
    }

    public void HandleCanvasGesture(HPUIGestureEventArgs gestureArgs, HPUICanvasEventArgs canvasArgs)
    {
        
    }
}
