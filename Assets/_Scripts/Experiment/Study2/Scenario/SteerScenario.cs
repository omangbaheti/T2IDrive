using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;


[System.Serializable]
public class SteerScenario :  MonoBehaviour, IScenario 
{
    public string Key => "Steer";
    public UnityEvent<bool> IsScenarioFinishedSuccessfully => isScenarioFinishedSuccessfully;
    public SplineContainer CurrentSpline => currentSpline;
    
    [SerializeField] private SplineContainer currentSpline;
    
    private UnityEvent<bool> isScenarioFinishedSuccessfully;
    
    public void InitializeScenario()
    {
        
    }

    public void StartScenario()
    {
        
    }

    public void TriggerScenarioEvent()
    {
    }

    public void ResetScenario()
    {
    }

    public void EndScenario()
    {
    }
}
