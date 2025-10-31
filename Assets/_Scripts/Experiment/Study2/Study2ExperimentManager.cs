using System;
using System.Collections.Generic;
using System.Linq;
using ArtificeToolkit.Attributes;
using TMPro;
using ubco.ovilab.HPUI;
using ubco.ovilab.uxf.extensions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Hands;
using UXF;
using static EulersCircuit;
using Random = Unity.Mathematics.Random;

public class Study2ExperimentManager : ExperimentManager<ScenarioBlockData>
{
    
    [SerializeField] private Transform handTransform;
    [SerializeField] private GestureLayoutSetup layoutSetup;
    [SerializeField] private Image iconDisplay;
    [SerializeField] private int trialsPerIconPerBlock = 2;
    [SerializeField] private Study2TrialManager study2TrialManager;
    [SerializeField] private List<TwoStepEulerConnection> EulerCircuit;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip failedClip;
    [SerializeField] private UIDisplayFlasher displayFlasher;
    [SerializeField] private List<(Vector2Int, Vector2Int)> exclusionList =  new()
    {
        // (Vector2Int.zero, Vector2Int.zero)
    };
    
    private XRHandSubsystem handSubsystem;
    private static Random rng;
    private XRHand activeHand;
    private string targetAction;
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
        List<MicrogestureAction> microgestureActions = layoutSetup.microGestureActions;
        
        foreach ((Vector2Int, Vector2Int) excludeGesture in exclusionList)
        {
            foreach (MicrogestureAction action in  microgestureActions)
            {
                if (action.startRegion == excludeGesture.Item1 && action.endRegion == excludeGesture.Item2)
                {
                    microgestureActions.Remove(action);
                }
                break;
            }
        }
        ShuffleList(microgestureActions, seed);

        foreach (MicrogestureAction action in microgestureActions)
        {
            Trial trial = block.CreateTrial();
            IconAction iconAction = action.SwipeActions.OfType<IconAction>().FirstOrDefault();
            if (iconAction != null)
            {
                trial.settings.SetValue(StudyLogs.TargetAction, iconAction.actionLabel);
            }
            else
            {
                Debug.LogError($"Something went wrong with Start Region{action.startRegion}, End Region {action.endRegion}");
            }
        }
    }

    protected override void OnBlockBegin(Block block)
    {
    }

    protected override void OnTrialBegin(Trial trial)
    {
        targetAction = trial.settings.GetString(StudyLogs.TargetAction);
        Sprite icon = layoutSetup.iconLayoutSetup.actionIconDict[targetAction];
        iconDisplay.sprite = icon;
    }

    [Button]
    public void HandleTrial(string inputAction)
    {
        bool result = inputAction == targetAction;
        string res = result ? "Successful" : "Failed"; 
        Session.instance.CurrentTrial.settings.SetValue(StudyLogs.InputAction, inputAction);
        Session.instance.CurrentTrial.settings.SetValue(StudyLogs.SuccessfulTrial, res);
        if (!result)
        {
            CancelTrial();
            return;
        }

        NextTrial();
    }

    [Button]
    public void HandleTrial()
    {
        // bool result = inputAction == targetAction;
        // string res = result ? "Successful" : "Failed"; 
        // Session.instance.CurrentTrial.settings.SetValue(StudyLogs.InputAction, inputAction);
        // Session.instance.CurrentTrial.settings.SetValue(StudyLogs.SuccessfulTrial, res);
        // if (!result)
        // {
        //     CancelTrial();
        //     return;
        // }

        NextTrial();
    }
    private void NextTrial(bool onlyStartNextTrial = false)
    {
        try
        {
            if (!onlyStartNextTrial && Session.instance.InTrial) Session.instance.EndCurrentTrial();
            Session.instance.BeginNextTrial();
            displayFlasher.Flash(new Color(0, 1, 0, 0.5f));
        }
        catch (NoSuchTrialException)
        {
            Debug.Log("Block ended (i think)");
        }
    }

    private void CancelTrial(bool insertImmediate = false)
    {
        Trial newTrial = Session.instance.CurrentBlock.CreateTrial();
        List<Trial> trials = Session.instance.CurrentBlock.trials;
        int currTrialIdx = trials.IndexOf(Session.instance.CurrentTrial);
        Settings currentTrialSettings = trials[currTrialIdx].settings;
        newTrial.settings.SetValue(StudyLogs.TargetAction, Session.instance.CurrentTrial.settings.GetString(StudyLogs.TargetAction));
        trials.Remove(newTrial);
        if (insertImmediate)
        {
            trials.Insert(currTrialIdx + 1, newTrial);
        }
        else
        {
            trials.Insert(trials.Count,  newTrial);
        }
        Debug.Log("Cancelling trial");
        AudioClip clipToPlay = failedClip;
        SoundManager.Instance.PlaySound(clipToPlay);
        displayFlasher.Flash(new Color(1,0,0,0.5f));
        NextTrial();
    }

    protected override void OnTrialEnd(Trial trial)
    {
        targetAction = null;
    }

    protected override void OnBlockEnd(Block block)
    {
        iconDisplay.sprite = null;
    }

    protected override void OnSessionEnd(Session session)
    {
        
    }
    
    private static List <T> ShuffleList<T>(List <T> list, int seed = 0)
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