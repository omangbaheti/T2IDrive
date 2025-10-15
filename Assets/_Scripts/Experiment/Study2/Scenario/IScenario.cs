using System;
using UnityEngine.Events;

public interface IScenario
{
    public UnityEvent<bool> IsScenarioFinishedSuccessfully();
    
    public void InitializeScenario();
    public void StartScenario();
    public void TriggerScenarioEvent();
    
    public void ResetScenario();
    public void EndScenario();
}


