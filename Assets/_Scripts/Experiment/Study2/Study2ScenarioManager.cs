using System;
using System.Collections;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

public class Study2ScenarioManager : MonoBehaviour
{
    [SerializeField] private SelfDrivingManager selfDrivingCar;
    [SerializeField] private Blinds blinds;
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
        blinds = selfDrivingCar.GetComponentInChildren<Blinds>();
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
            StartCoroutine(ChangeScenario(scenarioName, scenario));
        }
        else
        {
            Debug.LogError($"No scenario found => {scenarioName}");
        }
        
    }

    private void ResetScenario( IScenario scenario)
    {
        scenario.ResetScenario();
    }
    
    public IEnumerator ChangeScenario(string scenarioName, IScenario scenario)
    {
        
        Debug.Log("Loading scenario " + scenarioName);
        currentScenario = scenario;
        currentScenario.InitializeScenario();
        blinds.MoveBlindsUp();
        yield return new WaitForSeconds(2f);
        Transform currentScenarioTransform = currentScenario.CurrentSpline.transform;
        Vector3 bezierKnotPos = currentScenarioTransform.TransformPoint(currentScenario.CurrentSpline.Spline[0].Position);
        selfDrivingCar.RB.position = bezierKnotPos + new Vector3(1, 1, -2); 
        selfDrivingCar.Spline = currentScenario.CurrentSpline;
        selfDrivingCar.SetupNewPath();
        blinds.MoveBlindsDown();
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