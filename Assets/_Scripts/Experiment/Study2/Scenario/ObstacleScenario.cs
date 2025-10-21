using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;


[System.Serializable]
public class ObstacleScenario : MonoBehaviour, IScenario
{
    public string Key => "Obstacle";
    public UnityEvent<bool> IsScenarioFinishedSuccessfully => isScenarioFinishedSuccessfully;
    public SplineContainer CurrentSpline => currentSpline;
    
    [SerializeField] private SplineContainer currentSpline;
    
    private UnityEvent<bool> isScenarioFinishedSuccessfully = new();
    

    public void InitializeScenario()
    {
        
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
