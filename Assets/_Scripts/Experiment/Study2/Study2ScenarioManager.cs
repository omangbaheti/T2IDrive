using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

public class Study2ScenarioManager : MonoBehaviour
{
    private Dictionary<string, IScenario> scenarios = new();
    [SerializeField] private SelfDrivingManager selfDrivingCar;
    private IScenario currentScenario;

    [SerializeField] string testScenarioName;
    private void Awake()
    {
        foreach (Transform child in transform)
        {
            if(child.TryGetComponent(out IScenario scenario))
            {
                MonoBehaviour component = scenario as MonoBehaviour;
                switch (component)
                {
                    case BrakeScenario brakeScenario:
                        scenarios.Add("Brake", scenario);
                        break;
                    case SteerScenario steerScenario:
                        scenarios.Add("Steer", scenario);
                        break;
                    case PedestrianScenario pedestrianScenario:
                        scenarios.Add("Pedestrian", scenario);
                        break;
                    case ObstacleScenario obstacleScenario:
                        scenarios.Add("Obstacle", scenario);
                        break;
                    default:
                        Debug.LogWarning($"Unknown scenario type: {component?.GetType()}");
                        break;
                }
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