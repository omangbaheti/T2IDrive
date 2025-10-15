using UnityEngine;
using UnityEngine.Events;


[System.Serializable]
public class SteerScenario :  MonoBehaviour, IScenario 
{
    public UnityEvent<bool> IsScenarioFinishedSuccessfully()
    {
        throw new System.NotImplementedException();
    }

    public void InitializeScenario()
    {
        throw new System.NotImplementedException();
    }

    public void StartScenario()
    {
    }

    public void TriggerScenarioEvent()
    {
    }

    public void ResetScenario()
    {
        throw new System.NotImplementedException();
    }

    public void EndScenario()
    {
    }
}
