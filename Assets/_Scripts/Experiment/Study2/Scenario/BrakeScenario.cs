using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;


[System.Serializable]
public class BrakeScenario : MonoBehaviour, IScenario
{
    public string Key => "Brake";

    public Transform environment;
    public BoxCollider endTrigger;
    public UnityEvent<bool> IsScenarioFinishedSuccessfully => isScenarioFinishedSuccessfully;
    public SplineContainer CurrentSpline => currentSpline;
    
    private UnityEvent<bool> isScenarioFinishedSuccessfully = new();
    
    [SerializeField] private SplineContainer currentSpline;

    private void Awake()
    {
        currentSpline = GetComponentInChildren<SplineContainer>();
    }

    public void InitializeScenario()
    {
        environment.gameObject.SetActive(true);
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
        // If car brakes successfully or collides into the car ahead
    }
}
