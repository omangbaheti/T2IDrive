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
    
    [SerializeField] private SplineContainer currentSpline;
    
    private UnityEvent<bool> isScenarioFinishedSuccessfully = new();
    
    
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
        //1. Load Second Self Driving Car
        //2. Traffic?
        
    }

    public void TriggerScenarioEvent()
    {
        
    }

    public void TriggerTakeOverRequest()
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
