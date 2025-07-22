using System;
using System.Linq;
using ubco.ovilab.HPUI;
using ubco.ovilab.HPUI.Core;
using UnityEngine;
using UXF;

public class ComfortStudyExperimentManager : Singleton<ComfortStudyExperimentManager>
{
    public FingerRegions startRegion;
    public FingerRegions endRegion;

    private ComfortStudyTrialManager trialManager;
    [SerializeField] private GestureLayoutSetup layoutSetup;


    private void Start()
    {
        trialManager = FindAnyObjectByType<ComfortStudyTrialManager>();
        ConfigureExperiment();
    }

    private void ConfigureExperiment()
    {
        foreach (MicrogestureAction action in layoutSetup.microGestureActions)
        {
            foreach (IHPUISwipeAction swipe in action.SwipeActions.ToList().Where(swipe => swipe.GetType() == typeof(ExperimentHandler)))
            {
                action.SwipeActions.Remove(swipe);
            }

            ExperimentHandler handler = new()
            {
                startRegion = action.startRegion,
                endRegion = action.endRegion
            };
            handler.OnSwipeCompleted.AddListener(HandleTrial);
            action.SwipeActions.Add(handler);
        }
    }

    public void StartTrial()
    {
        NextTrial();
    }

    private void HandleTrial(HPUICanvasEventArgs args)
    {
        if (startRegion == FingerRegions.Invalid || endRegion == FingerRegions.Invalid)
        {
            Debug.LogWarning("Set the Start Region / End Region");
        }
        NextTrial();
    }

    private void NextTrial()
    {
        trialManager.SetCurrentTrialActive();
    }
}