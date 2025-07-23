using System.Collections.Generic;
using ArtificeToolkit.Runtime.SerializedDictionary;
using ubco.ovilab.HPUI.Core;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;
using XRUtils = Unity.XR.CoreUtils.Collections;


public class HPUICanvasManager : MonoBehaviour, IHPUICanvasUIManager
{
    public List<float> XDivisions => xDivisions;
    public List<float> YDivisions => yDivisions;

    public HPUIMultiFingerCanvas HPUICanvas { get; set; }
    public XRUtils.SerializableDictionary<Vector2Int?, HPUICanvasRegion> HPUIRegions => hpuiRegions;

    private List<float> xDivisions;
    private List<float> yDivisions;
    [SerializeField] private XRUtils.SerializableDictionary<Vector2Int?, HPUICanvasRegion> hpuiRegions = new();

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
