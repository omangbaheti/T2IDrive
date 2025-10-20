using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;


[System.Serializable]
public class PedestrianScenario: MonoBehaviour, IScenario
{
    public string Key => "Pedestrian";
    public UnityEvent<bool> IsScenarioFinishedSuccessfully => isScenarioFinishedSuccessfully;
    public SplineContainer CurrentSpline => currentSpline;
    
    [SerializeField] private SplineContainer currentSpline;
    private UnityEvent<bool> isScenarioFinishedSuccessfully = new();

    private void Start()
    {
        
        currentSpline = GetComponentInChildren<SplineContainer>();
        
    }

    public void InitializeScenario()
    {
        throw new System.NotImplementedException();
    }

    public void StartScenario()
    {
        throw new System.NotImplementedException();
    }

    public void TriggerScenarioEvent()
    {
        throw new System.NotImplementedException();
    }

    public void ResetScenario()
    {
        throw new System.NotImplementedException();
    }

    public void EndScenario()
    {
        throw new System.NotImplementedException();
    }
}
