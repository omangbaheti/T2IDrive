using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

public class Study2ScenarioManager : MonoBehaviour
{
    [SerializeField] private SelfDrivingManager selfDrivingCar;
    
    private Dictionary<string, IScenario> scenarios = new();
     
    
    [Header("Tests")] 
    [SerializeField] string testScenarioName;
    private IScenario currentScenario;
    private void Awake()
    {
        foreach (Transform child in transform)
        {
            if(child.TryGetComponent(out IScenario scenario))
            {
                scenarios[scenario.Key] = scenario;
            }
        } 
    }

    [Button]
    public void LoadScenarioTest()
    {
        LoadScenario(testScenarioName);  
            
    }
    public void LoadScenario(string scenarioName)
    {
        if (currentScenario != null)
        {
            ResetScenario(currentScenario);
        }

        if (scenarios.TryGetValue(scenarioName, out IScenario scenario))
        {
            currentScenario = scenario;
            currentScenario.InitializeScenario();
        }
        else
        {
            Debug.LogError($"No scenario found => {scenarioName}");
        }
        
            
    }

    private void ResetScenario(IScenario scenario)
    {
        scenario.ResetScenario();
    }
    
    public void ChangeScenario()
    {
        // TODO: Start a Coroutine to 
        // 1. Add Sliding Windows
        // 2. Change Car Position
        // 3. Tell Selfdriving manager the new spline and new starting point
        // 4. Change Status of ready button
    }

    public void OnScenarioComplete()
    {
        
    }
    
    
    
    
}