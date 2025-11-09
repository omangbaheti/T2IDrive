using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ArtificeToolkit.Attributes;
using TMPro;
using ubco.ovilab.HPUI;
using ubco.ovilab.HPUI.Core;
using ubco.ovilab.HPUI.Interaction;
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
    public string TargetAction => targetAction;
    [SerializeField] private Transform handTransform;
    [SerializeField] private GestureLayoutSetup layoutSetup;
    [SerializeField] private Transform prompterDisplay;
    [SerializeField] private SpriteRenderer iconDisplay;
    [SerializeField] private int trialsPerIconPerBlock = 2;
    [SerializeField] private Study2TrialManager hpuiTrialManager;
    [SerializeField] private Study2TrialManager touchScreenTrialManager;
    [SerializeField] private List<Color> menuColors = new();
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip failedClip;
    [SerializeField] private HPUIInteractor indexInteractor;
    [SerializeField] private HPUIInteractor thumbInteractor;
    [SerializeField] private int practiceTrials;
    private Dictionary<Vector2Int, Color> interactionMappingColor = new();
    private HashSet<string> tapListActions =  new();
    private XRHandSubsystem handSubsystem;
    private static Random rng;
    private XRHand activeHand;
    private string targetAction;
    private string prevBlockName = "";

    private readonly HashSet<(Vector2Int, Vector2Int)> exclusionList =  new()
    {
        (new Vector2Int(0,0), new Vector2Int(0,0)),
        (new Vector2Int(0,1), new Vector2Int(0,1)),
        (new Vector2Int(0,2), new Vector2Int(0,2)),
        (new Vector2Int(1,0), new Vector2Int(1,0)),
        (new Vector2Int(1,1), new Vector2Int(1,1)),
        (new Vector2Int(1,2), new Vector2Int(1,2))
        
    };
    
    private readonly HashSet<(Vector2Int, Vector2Int)> tapList =  new()
    {
        (new Vector2Int(0,0), new Vector2Int(0,0)),
        (new Vector2Int(0,1), new Vector2Int(0,1)),
        (new Vector2Int(0,2), new Vector2Int(0,2)),
        (new Vector2Int(1,0), new Vector2Int(1,0)),
        (new Vector2Int(1,1), new Vector2Int(1,1)),
        (new Vector2Int(1,2), new Vector2Int(1,2))
    };
    private void Awake()
    {
        interactionMappingColor = new()
        {
            { new Vector2Int(1, 2), menuColors[0] },
            { new Vector2Int(1, 1), menuColors[1] },
            { new Vector2Int(1, 0), menuColors[2] },
            { new Vector2Int(0, 2), menuColors[3] },
            { new Vector2Int(0, 1), menuColors[4] },
            { new Vector2Int(0, 0), menuColors[5] },
        };
        hpuiTrialManager.interactionMappingColor =  interactionMappingColor;
        touchScreenTrialManager.interactionMappingColor =  interactionMappingColor;
    }

    protected override void OnSessionBegin(Session session)
    {
        hpuiTrialManager.ResetCanvasRegions();
        touchScreenTrialManager.ResetCanvasRegions();
        hpuiTrialManager.gameObject.SetActive(false); 
        touchScreenTrialManager.gameObject.SetActive(false); 
        //TIME
        session.settingsToLog.Add(StudyLogs.GestureStartTime);
        session.settingsToLog.Add(StudyLogs.GestureEndTime);
       
        //Trial Settings
        // session.settingsToLog.Add();
        session.settingsToLog.Add(StudyLogs.StartRegion);
        session.settingsToLog.Add(StudyLogs.EndRegion);
        session.settingsToLog.Add(StudyLogs.UIType);
        
        // HPUI Outputs 
        session.settingsToLog.Add(StudyLogs.GestureStartRegion);
        session.settingsToLog.Add(StudyLogs.GestureEndRegion);
        session.settingsToLog.Add(StudyLogs.SuccessfulTrial);

        Tracker[] trackerList = FindObjectsByType<Tracker>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        session.trackedObjects.AddRange(trackerList);
        
        List<XRHandSubsystem> handSubsystems = new();
        SubsystemManager.GetSubsystems(handSubsystems);

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
        if (prevBlockName != el.name)
        {
            ShuffleList(menuColors);
            interactionMappingColor = new()
            {
                { new Vector2Int(1, 2), menuColors[0] },
                { new Vector2Int(1, 1), menuColors[1] },
                { new Vector2Int(1, 0), menuColors[2] },
                { new Vector2Int(0, 2), menuColors[3] },
                { new Vector2Int(0, 1), menuColors[4] },
                { new Vector2Int(0, 0), menuColors[5] },
            };
            hpuiTrialManager.interactionMappingColor =  interactionMappingColor;
            touchScreenTrialManager.interactionMappingColor =  interactionMappingColor;
        }
        
        block.settings.SetValue(StudyLogs.BlockName, el.name);
        block.settings.SetValue(StudyLogs.UIType, el.UserInterface);
        int pid = Convert.ToInt32(Session.instance.ppid);
        int seed = pid * 397 ^ el.block_id;
        rng = new(seed: (uint)seed);
        layoutSetup.SetupLayout($"{int.Parse(Session.instance.ppid)%20+1}_{el.name}");
        List<MicrogestureAction> microgestureActions = layoutSetup.microGestureActions;
        tapListActions.Clear();
        foreach (MicrogestureAction action in microgestureActions)
        {
            if (tapList.Contains((action.startRegion, action.endRegion)))
            {
                tapListActions.Add(action.SwipeActions.OfType<IconAction>().FirstOrDefault()?.actionLabel);
            }
        }
        prompterDisplay.gameObject.SetActive(true);
        var billboard =  prompterDisplay.GetComponent<Billboard>();
        switch (el.UserInterface)
        {
            case "OnHand":
                hpuiTrialManager.gameObject.SetActive(true); 
                touchScreenTrialManager.gameObject.SetActive(false); 
                thumbInteractor.gameObject.SetActive(true);
                indexInteractor.gameObject.SetActive(false);
                hpuiTrialManager.InteractionMapping = InteractionMapping.Direct;
                hpuiTrialManager.SpawnCanvasRegions();
                hpuiTrialManager.SetPrompterLocation(prompterDisplay);
                billboard.isEnabled = true;
                break;
            case "Windshield":
                hpuiTrialManager.gameObject.SetActive(true); 
                touchScreenTrialManager.gameObject.SetActive(false); 
                thumbInteractor.gameObject.SetActive(true);
                indexInteractor.gameObject.SetActive(false);
                hpuiTrialManager.InteractionMapping = InteractionMapping.Indirect;
                hpuiTrialManager.SpawnCanvasRegions();
                hpuiTrialManager.SetPrompterLocation(prompterDisplay);
                billboard.isEnabled = false;
                break;
            case "TouchScreen":
                hpuiTrialManager.gameObject.SetActive(false); 
                touchScreenTrialManager.gameObject.SetActive(true); 
                thumbInteractor.gameObject.SetActive(false);
                indexInteractor.gameObject.SetActive(true);
                touchScreenTrialManager.InteractionMapping = InteractionMapping.Direct;
                touchScreenTrialManager.SpawnCanvasRegions();
                touchScreenTrialManager.SetPrompterLocation(prompterDisplay);
                billboard.isEnabled = false;
                break;
            case "Baseline":
                hpuiTrialManager.gameObject.SetActive(false); 
                touchScreenTrialManager.gameObject.SetActive(false); 
                hpuiTrialManager.ResetCanvasRegions();
                touchScreenTrialManager.ResetCanvasRegions();
                prompterDisplay.gameObject.SetActive(false);
                billboard.isEnabled = false;
                break;
            default:
                Debug.LogError($"Not a valid Condition: {el.UserInterface}");
                break;
        }
        
        ShuffleList(microgestureActions, seed);
        int counter = 0;
        foreach (MicrogestureAction action in microgestureActions)
        {
            if (exclusionList.Contains((action.startRegion, action.endRegion)))
            {
                continue; 
            }

            if (el.name.Contains("Practice") && counter > practiceTrials)
            {
                break;
            }
            Trial trial = block.CreateTrial();
            IconAction iconAction = action.SwipeActions.OfType<IconAction>().FirstOrDefault();
            if (iconAction != null)
            {
                trial.settings.SetValue(StudyLogs.StartRegion, StudyLogs.VectorToRegionDict[action.startRegion]);
                trial.settings.SetValue(StudyLogs.EndRegion, StudyLogs.VectorToRegionDict[action.endRegion]);
                trial.settings.SetValue(StudyLogs.TargetAction, iconAction.actionLabel);
                trial.settings.SetValue(StudyLogs.UIType, el.UserInterface);
            }
            else
            {
                Debug.LogError($"Something went wrong with Start Region{action.startRegion}, End Region {action.endRegion}");
            }
            counter++;
        }
    }

    protected override void OnBlockBegin(Block block)
    {
        
    }

    protected override void OnTrialBegin(Trial trial)
    {
        targetAction = trial.settings.GetString(StudyLogs.TargetAction);
        Sprite icon = layoutSetup.iconLayoutSetup.actionIconDict[targetAction];
        foreach (MicrogestureAction action in layoutSetup.microGestureActions)
        {
            if (action.SwipeActions.OfType<IconAction>().FirstOrDefault()?.actionLabel == targetAction)
            {
                Color color = interactionMappingColor[action.startRegion]; 
                Debug.Log($">>>>>>>>{action.startRegion}: {color}");
                prompterDisplay.GetChild(0).GetChild(0).GetComponent<HotSwapColor>().SetColor(color);
                // display.SetColor(color);
            }
        }
        iconDisplay.sprite = icon;
    }

    public void GestureStarted(HPUICanvasEventArgs args)
    {
        Debug.Log($"Gesture Started : {args.SwipeStartRegion}");
        if (!Session.instance.InTrial)
        {
            Debug.LogWarning("Participant Touched the finger surface when not in trial");
            return;
        }
        Trial CurrentTrial = Session.instance.CurrentTrial;
        CurrentTrial.settings.SetValue(StudyLogs.GestureStartTime, Time.time);
    }

    [Button]
    public void HandleTrial(string inputAction)
    {
        Debug.Log($"Handling Trial: {inputAction}");
        bool result = inputAction == targetAction;
        string res = result ? "Successful" : "Failed"; 
        Session.instance.CurrentTrial.settings.SetValue(StudyLogs.InputAction, inputAction);
        Session.instance.CurrentTrial.settings.SetValue(StudyLogs.SuccessfulTrial, res);
        Settings currentTrial = Session.instance.CurrentTrial.settings;
        currentTrial.SetValue(StudyLogs.GestureEndTime, Time.time);
        if (tapListActions.Contains(inputAction))
        {
            CancelTrialWithoutFeedback();
            return;
        }
        if (!result)
        {
            CancelTrial();
            return;
        }
        SoundManager.Instance.PlaySound(successClip);
        // displayFlasher.Flash(new Color(0, 1, 0, 0.5f));
        NextTrial();
    }
    private void NextTrial(bool onlyStartNextTrial = false)
    {
        try
        {
            if (!onlyStartNextTrial && Session.instance.InTrial) Session.instance.EndCurrentTrial();
            Session.instance.BeginNextTrial();
        }
        catch (NoSuchTrialException)
        {
            Debug.Log("Block ended (i think)");
        }
    }

    public void CancelTrial(bool insertImmediate = false)
    {
        Trial newTrial = Session.instance.CurrentBlock.CreateTrial();
        List<Trial> trials = Session.instance.CurrentBlock.trials;
        int currTrialIdx = trials.IndexOf(Session.instance.CurrentTrial);
        Settings currentTrialSettings = trials[currTrialIdx].settings;
        newTrial.settings = currentTrialSettings;
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
        // displayFlasher.Flash(new Color(1,0,0,0.5f));
        NextTrial();
    }
    
    
    public void CancelTrialWithoutFeedback()
    {
        Trial newTrial = Session.instance.CurrentBlock.CreateTrial();
        List<Trial> trials = Session.instance.CurrentBlock.trials;
        int currTrialIdx = trials.IndexOf(Session.instance.CurrentTrial);
        Settings currentTrialSettings = trials[currTrialIdx].settings;
        newTrial.settings = currentTrialSettings;
        newTrial.settings.SetValue(StudyLogs.TargetAction, Session.instance.CurrentTrial.settings.GetString(StudyLogs.TargetAction));
        trials.Remove(newTrial);
        trials.Insert(currTrialIdx + 1, newTrial);
        Debug.Log("Cancelling trial");
        NextTrial();
    }

    protected override void OnTrialEnd(Trial trial)
    {
        targetAction = null;
    }

    protected override void OnBlockEnd(Block block)
    {
        iconDisplay.sprite = null;
        hpuiTrialManager.ResetCanvasRegions();
        touchScreenTrialManager.ResetCanvasRegions();
        foreach (MicrogestureAction action in layoutSetup.microGestureActions)
        {
            if (exclusionList.Contains((action.startRegion, action.endRegion)))
            {
                continue; 
            }
            IconAction iconAction = action.SwipeActions.OfType<IconAction>().FirstOrDefault();
            iconAction.OnSwipeStarted.RemoveListener(GestureStarted);
        }
    }

    protected override void OnSessionEnd(Session session)
    {
        
    }
    
    private static void ShuffleList<T>(List<T> list, int seed = 0)
    {
        System.Random random = new System.Random(seed);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(0, i+1);
            (list[index: i], list[j]) = (list[j], list[i]);
        }
    }
}

public class ScenarioBlockData: BlockData
{
    public string UserInterface;
}