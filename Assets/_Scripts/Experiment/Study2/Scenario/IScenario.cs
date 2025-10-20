using System;
using UnityEngine.Events;
using UnityEngine.Splines;

public interface IScenario
{
    public string Key { get; }
    public UnityEvent<bool> IsScenarioFinishedSuccessfully{get;}
    public SplineContainer CurrentSpline{get;}
    
    public void InitializeScenario();
    public void StartScenario();
    public void TriggerScenarioEvent();
    
    public void ResetScenario();
    public void EndScenario();
}


