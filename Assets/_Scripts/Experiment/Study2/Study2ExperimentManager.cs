using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using ubco.ovilab.uxf.extensions;
using UnityEngine;
using UnityEngine.XR.Hands;
using UXF;
using static EulersCircuit;
using Random = Unity.Mathematics.Random;

public class Study2ExperimentManager : ExperimentManager<ScenarioBlockData>
{
    
    [SerializeField] private Transform handTransform;
    [SerializeField] private GestureLayoutSetup layoutSetup;
    [SerializeField] private TextMeshProUGUI iconDisplay;
    [SerializeField] private int trialsPerIconPerBlock = 2;
    [SerializeField] private Study2TrialManager study2TrialManager;
    [SerializeField] private List<TwoStepEulerConnection> EulerCircuit;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip failedClip;

    private XRHandSubsystem handSubsystem;
    private static Random rng;
    private UIDisplayFlasher displayFlasher;
    private XRHand activeHand;
    private List<(Vector2Int, Vector2Int)> exclusionList;
    protected override void OnSessionBegin(Session session)
    {
        //TIME
        session.settingsToLog.Add(StudyLogs.GestureStartTime);
        session.settingsToLog.Add(StudyLogs.GestureEndTime);
       
        //Trial Settings
        // session.settingsToLog.Add();
        session.settingsToLog.Add(StudyLogs.FingerType);
        session.settingsToLog.Add(StudyLogs.StartRegion);
        session.settingsToLog.Add(StudyLogs.EndRegion);
        session.settingsToLog.Add(StudyLogs.Mobility);
        
        // HPUI Outputs 
        session.settingsToLog.Add(StudyLogs.GestureStartRegion);
        session.settingsToLog.Add(StudyLogs.GestureEndRegion);
        session.settingsToLog.Add(StudyLogs.SuccessfulTrial);
       
        session.trackedObjects.AddRange(handTransform.GetComponentsInChildren<Tracker>());
        
        
        List<XRHandSubsystem> handSubsystems = new();
        SubsystemManager.GetSubsystems(handSubsystems);
        // rng = new Random(int.Parse(Session.instance.ppid));

        foreach (XRHandSubsystem subSystem in handSubsystems)
        {
            if (!subSystem.running) continue;
            handSubsystem = subSystem;
            break;
        }
        if (handSubsystem != null)
        {
            activeHand = handSubsystem.rightHand;
        }
        else
        {
            Debug.LogError("Hand Subsystem is null");
        }
        
    }

    protected override void ConfigureBlock(ScenarioBlockData el, Block block, bool lastBlockCancelled)
    {
        block.settings.SetValue(StudyLogs.BlockName, el.name);
        block.settings.SetValue(StudyLogs.UIType, el.UserInterface);
        int pid = Convert.ToInt32(Session.instance.ppid);
        int seed = pid * 397 ^ el.block_id;
        rng = new(seed: (uint)seed);
        // var layout = layoutSetup.microGestureActions.CopyTo(layout);
        foreach ((Vector2Int, Vector2Int) excludeGesture in exclusionList)
        {
            foreach (MicrogestureAction action in layoutSetup.microGestureActions)
            {
                if (action.startRegion == excludeGesture.Item1 && action.endRegion == excludeGesture.Item2)
                {
                    layoutSetup.microGestureActions.Remove(action);
                }
                break;
            }
        }
        layoutSetup.microGestureActions = ShuffleList(layoutSetup.microGestureActions, seed);
        
    }

    protected override void OnBlockBegin(Block block)
    {
        
    }

    protected override void OnTrialBegin(Trial trial)
    {
        
    }

    protected override void OnTrialEnd(Trial trial)
    {
        
    }

    protected override void OnBlockEnd(Block block)
    {
        
    }

    protected override void OnSessionEnd(Session session)
    {
        
    }
    
    private static List <T> ShuffleList<T>(List <T> list, int seed)
    {
        System.Random random = new System.Random(seed);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(0, i+1);
            (list[index: i], list[j]) = (list[j], list[i]);
        }
        return list;
    }
}

public class ScenarioBlockData: BlockData
{
    public string TakeOverScenario;
    public string UserInterface;
}