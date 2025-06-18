using System;
using System.Collections.Generic;
using TMPro;
using ubco.ovilab.HPUI.Core;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;
using UnityEngine.Serialization;

public class HPUICanvasRegionManager : MonoBehaviour, IHPUICanvasUIManager
{
    public List<float> XDivisions
    {
        get => xDivisions;
        set => xDivisions = value;
    }

    public List<float> YDivisions
    {
        get => yDivisions;
        set => yDivisions = value;
    }

    public SerializedDictionary<Vector2Int?, HPUICanvasRegion> HPUIRegions
    {
        get => hpuiRegions;
        set => hpuiRegions = value;
    }

    public HPUIMultiFingerCanvas HPUICanvas
    {
        get => targetCanvas;
        set => targetCanvas = value;
    }

    public List<MicrogestureAction> gestureActions = new();

    [SerializeField] private SerializedDictionary<Vector2Int?, HPUICanvasRegion> hpuiRegions = new();
    [SerializeField] private GestureLayoutSetup gestureLayout;
    [SerializeField] public GameObject textBox;
    [SerializeField] public TextMeshProUGUI textMesh;
    [SerializeField, HideInInspector] public SerializedDictionary<Vector2Int,GameObject> layer1UIElements = new();

    private List<float> xDivisions = new();
    private List<float> yDivisions = new() ;
    private HPUIMultiFingerCanvas targetCanvas;

    private void OnEnable()
    {
        targetCanvas = GetComponent<HPUIMultiFingerCanvas>();
        targetCanvas.OnCanvasInteractions.AddListener(HandleCanvasGesture);
    }

    private void OnDisable()
    {
        targetCanvas.OnCanvasInteractions.RemoveListener(HandleCanvasGesture);
    }

    private void Start()
    {
        SpawnCanvasRegions();
    }

    public void SpawnCanvasRegions()
    {
        HPUICanvasRegion[] CanvasRegions = GetComponents<HPUICanvasRegion>();
        foreach (HPUICanvasRegion region in CanvasRegions)
        {
            DestroyImmediate(region);
            Debug.Log($"Added Region {region.ID}");
        }
        hpuiRegions.Clear();

        foreach (float division in gestureLayout.xDivisions)
        {
            xDivisions.Add(division * targetCanvas.MaxBounds.x);
        }

        foreach (float division in gestureLayout.yDivisions)
        {
            yDivisions.Add(division * targetCanvas.MaxBounds.y);
        }

        gestureActions = gestureLayout.microGestureActions;

        for (int i = 0; i < xDivisions.Count-1; i++)
        {
            for (int j = 0; j < yDivisions.Count-1; j++)
            {
                HPUICanvasRegion hpuiRegion = gameObject.AddComponent<HPUICanvasRegion>();
                hpuiRegion.ID = new Vector2Int(i, j);
                hpuiRegion.basePoint = new Vector2(xDivisions[i], yDivisions[j]);
                hpuiRegion.area = new Vector2(xDivisions[i+1] - xDivisions[i], yDivisions[j+1] - yDivisions[j]);
                //hpuiRegion.textBox = textBox;
                hpuiRegions.Add(new Vector2Int(i,j), hpuiRegion);
            }
        }
        InitialiseRegions();
    }

    public void InitialiseRegions()
    {
        foreach (MicrogestureAction action in gestureActions)
        {
            hpuiRegions[action.startRegion].gestureActions.Add(action);
        }
        foreach ((Vector2Int? ID, HPUICanvasRegion region) in hpuiRegions)
        {
            region.InitialiseUI();
        }
        InitialiseUI();
    }

    public void InitialiseUI()
    {
        foreach (MicrogestureAction action in gestureActions)
        {
            if (layer1UIElements.ContainsKey(action.startRegion))
            {
                TextMeshPro keyText = layer1UIElements[action.startRegion].transform.GetComponent<TextMeshPro>();
                //keyText.text += action.outputKey;
                if (keyText.text.Length == 3)
                {
                    keyText.text += "\n";
                }
                continue;
            }
            HPUICanvasRegion region = hpuiRegions[action.startRegion];
            Vector2 regionBasePoint = region.basePoint;
            Vector2 regionCenterPoint = regionBasePoint + region.area/2f + region.centreOffset;
            Vector2Int centreIndex = HPUICanvasComponentUtils.CalculateColliderIndex(regionCenterPoint, targetCanvas);
            Transform regionCentre = targetCanvas.coordsToCollider[centreIndex].transform;
            GameObject textKey = Instantiate(textBox, regionCentre.position, Quaternion.identity, regionCentre);
            textKey.transform.localPosition = new Vector3(0,50f,0);
            textKey.transform.localRotation = Quaternion.Euler(90,90,0);
            //textKey.GetComponent<TextMeshPro>().text = action.outputKey;
            //textKey.name = "Key: " + action.outputKey;
            layer1UIElements.Add(action.startRegion, textKey);
        }
    }

    public void HandleCanvasGesture(HPUIGestureEventArgs gestureArgs, HPUICanvasEventArgs canvasArgs)
    {
        switch (canvasArgs.State)
        {
            case HPUICanvasState.INVALID or HPUICanvasState.NotStarted:
                Debug.Log(canvasArgs.State.ToString());
                break;
            case HPUICanvasState.Started:
                canvasArgs.SwipeStartRegion = GetInteractionRegion(canvasArgs.GesturePositions[^1]);;
                SetUIActive(false);
                hpuiRegions[canvasArgs.SwipeStartRegion].OnGestureStarted(canvasArgs);
                break;
            case HPUICanvasState.Processing:
                SetUIActive(false);
                hpuiRegions[canvasArgs.SwipeStartRegion ?? throw new InvalidOperationException()].OnGestureStarted(canvasArgs);
                break;
            case HPUICanvasState.Cancelled:
                foreach (KeyValuePair<Vector2Int?, HPUICanvasRegion> region in hpuiRegions)
                {
                    region.Value.DisableUI();
                }
                SetUIActive(true);
                break;
            case HPUICanvasState.Completed:
                canvasArgs.SwipeEndRegion = GetInteractionRegion(canvasArgs.GesturePositions[^1]);
                hpuiRegions[canvasArgs.SwipeStartRegion ?? throw new InvalidOperationException()].OnGestureEnded(canvasArgs);
                foreach (KeyValuePair<Vector2Int?, HPUICanvasRegion> region in hpuiRegions)
                {
                    region.Value.DisableUI();
                }
                SetUIActive(true);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void SetUIActive(bool active)
    {
        foreach (KeyValuePair<Vector2Int, GameObject> uiElement in layer1UIElements)
        {
            uiElement.Value.SetActive(active);
        }
    }

    private Vector2Int GetInteractionRegion(Vector2 touchPoint)
    {
        int regionX = -1, regionY = -1;
        for (int i = 0; i < xDivisions.Count-1; i++)
        {
            if (touchPoint.x > xDivisions[i] && touchPoint.x <= xDivisions[i + 1])
            {
                regionX = i;
                break;
            }
        }

        for (int j = 0; j < yDivisions.Count-1; j++)
        {
            if (touchPoint.y > yDivisions[j] && touchPoint.y <= yDivisions[j + 1])
            {
                regionY = j;
                break;
            }
        }

        return new Vector2Int(regionX, regionY);
    }


}
