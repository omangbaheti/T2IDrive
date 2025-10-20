using UnityEngine;
using UnityEngine.Events;


[System.Serializable]
public class ObstacleScenario : MonoBehaviour, IScenario
{
    public string Key => "Obstacle";
    public UnityEvent<bool> IsScenarioFinishedSuccessfully => isScenarioFinishedSuccessfully;
    
    private UnityEvent<bool> isScenarioFinishedSuccessfully = new();

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
