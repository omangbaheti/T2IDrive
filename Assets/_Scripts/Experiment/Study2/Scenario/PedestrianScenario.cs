using UnityEngine;
using UnityEngine.Events;


[System.Serializable]
public class PedestrianScenario: MonoBehaviour, IScenario
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
